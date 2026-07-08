using System.Security.Cryptography;
using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PremiumAccountAuthService
{
    public sealed record RegisterResult(
        ApplicationUserModel User,
        string RecoveryCode);

    sealed class CredentialState
    {
        public List<LocalAccountCredential> Accounts { get; set; } = new();
    }

    sealed class LocalAccountCredential
    {
        public string ApplicationUserId { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string Username { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string PasswordSalt { get; set; } = "";
        public string RecoveryCodeHash { get; set; } = "";
        public string RecoveryCodeSalt { get; set; } = "";
        public string Email { get; set; } = "";
        public string SecurityQuestion { get; set; } = "";
        public string SecurityAnswerHash { get; set; } = "";
        public string SecurityAnswerSalt { get; set; } = "";
        public int Age { get; set; }
        public string Gender { get; set; } = "";
        public string AcceptedTermsVersion { get; set; } = CurrentTermsVersion;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; }
    }

    const string CredentialsFileName = "local_account_credentials.json";
    const string CurrentTermsVersion = "18-plus-legal-consent-v2";
    const int Pbkdf2Iterations = 180_000;
    const int HashSizeBytes = 32;
    const int SaltSizeBytes = 16;
    const int MaxFailedAttempts = 5;

    static readonly SemaphoreSlim Gate = new(1, 1);
    static readonly Dictionary<string, FailedLoginState> FailedLogins =
        new(StringComparer.OrdinalIgnoreCase);

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    static string CredentialsFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, CredentialsFileName);

    public static string GenericLoginError =>
        "اسم المستخدم أو كلمة السر غير صحيحة";

    public static string PasswordPolicyText =>
        "كلمة السر يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف كبير، حرف صغير، رقم، ورمز.";

    public static async Task<RegisterResult> RegisterAsync(
        string username,
        string nickname,
        string password,
        string confirmPassword,
        int age,
        string gender,
        string email,
        string securityQuestion,
        string securityAnswer,
        bool acceptedAge,
        bool acceptedPrivacy,
        bool acceptedTerms,
        bool acceptedCredentialResponsibility)
    {
        username = NormalizeUsername(username);
        nickname = Safe(nickname);
        gender = Safe(gender);
        email = NormalizeEmail(email);
        securityQuestion = Safe(securityQuestion);
        securityAnswer = NormalizeSecurityAnswer(securityAnswer);

        ValidateRegistration(
            username,
            nickname,
            password,
            confirmPassword,
            age,
            gender,
            email,
            securityQuestion,
            securityAnswer,
            acceptedAge,
            acceptedPrivacy,
            acceptedTerms,
            acceptedCredentialResponsibility);

        await Gate.WaitAsync();
        try
        {
            var state = await LoadStateAsync();

            if (state.Accounts.Any(account =>
                    string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("اسم الدخول مستخدم مسبقاً. اختر اسم دخول آخر.");
            }

            var user = await ApplicationUserService.RegisterMemberAsync(nickname);
            string recoveryCode = GenerateRecoveryCode();
            byte[] passwordSalt = GenerateSalt();
            byte[] recoverySalt = GenerateSalt();
            byte[] securitySalt = GenerateSalt();

            state.Accounts.Add(new LocalAccountCredential
            {
                ApplicationUserId = user.ApplicationUserId,
                PlayerId = user.PlayerId,
                Username = username,
                Nickname = nickname,
                PasswordSalt = Convert.ToBase64String(passwordSalt),
                PasswordHash = HashSecret(password, passwordSalt),
                RecoveryCodeSalt = Convert.ToBase64String(recoverySalt),
                RecoveryCodeHash = HashSecret(recoveryCode, recoverySalt),
                Email = email,
                SecurityQuestion = securityQuestion,
                SecurityAnswerSalt = Convert.ToBase64String(securitySalt),
                SecurityAnswerHash = HashSecret(securityAnswer, securitySalt),
                Age = age,
                Gender = gender,
                AcceptedTermsVersion = CurrentTermsVersion,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await SaveStateAsync(state);
            ClearFailedLogin(username);

            return new RegisterResult(user, recoveryCode);
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<ApplicationUserModel> LoginAsync(
        string username,
        string password)
    {
        username = NormalizeUsername(username);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(GenericLoginError);

        if (IsTemporarilyLocked(username, out var remaining))
        {
            throw new InvalidOperationException(
                $"تم قفل المحاولة مؤقتاً. حاول بعد {Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} دقيقة.");
        }

        await Gate.WaitAsync();
        try
        {
            var state = await LoadStateAsync();
            var credential = state.Accounts.FirstOrDefault(account =>
                string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase));

            if (credential == null || !VerifySecret(password, credential.PasswordSalt, credential.PasswordHash))
            {
                RegisterFailedLogin(username);
                throw new InvalidOperationException(GenericLoginError);
            }

            credential.LastLoginAt = DateTime.UtcNow;
            credential.UpdatedAt = DateTime.UtcNow;
            await SaveStateAsync(state);

            await ApplicationUserService.SwitchUserAsync(credential.ApplicationUserId);
            ClearFailedLogin(username);

            return await ApplicationUserService.GetCurrentUserAsync();
        }
        finally
        {
            Gate.Release();
        }
    }

    static void ValidateRegistration(
        string username,
        string nickname,
        string password,
        string confirmPassword,
        int age,
        string gender,
        string email,
        string securityQuestion,
        string securityAnswer,
        bool acceptedAge,
        bool acceptedPrivacy,
        bool acceptedTerms,
        bool acceptedCredentialResponsibility)
    {
        if (!acceptedAge || !acceptedPrivacy || !acceptedTerms || !acceptedCredentialResponsibility)
            throw new InvalidOperationException("يجب الموافقة على جميع بنود الحماية والاستخدام قبل إنشاء الحساب.");

        if (age < 18)
            throw new InvalidOperationException("التطبيق مخصص لمن هم بعمر 18 سنة أو أكثر فقط.");

        if (string.IsNullOrWhiteSpace(username) || username.Length < 4 || username.Length > 32)
            throw new InvalidOperationException("اسم الدخول يجب أن يكون بين 4 و32 حرفاً.");

        if (!username.All(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '-'))
            throw new InvalidOperationException("اسم الدخول يسمح بالحروف والأرقام والرموز _ . - فقط.");

        if (string.IsNullOrWhiteSpace(nickname) || nickname.Length > 40)
            throw new InvalidOperationException("الاسم الظاهر مطلوب ويجب ألا يتجاوز 40 حرفاً.");

        if (string.IsNullOrWhiteSpace(gender))
            throw new InvalidOperationException("اختر الجنس لإكمال إنشاء الحساب.");

        if (!string.IsNullOrWhiteSpace(email) &&
            (!email.Contains('@', StringComparison.Ordinal) || email.Length > 120))
        {
            throw new InvalidOperationException("البريد الإلكتروني الاختياري غير صالح.");
        }

        if (string.IsNullOrWhiteSpace(securityQuestion) || securityQuestion.Length < 6 || securityQuestion.Length > 120)
            throw new InvalidOperationException("سؤال الأمان مطلوب ويجب أن يكون واضحاً.");

        if (string.IsNullOrWhiteSpace(securityAnswer) || securityAnswer.Length < 3 || securityAnswer.Length > 80)
            throw new InvalidOperationException("إجابة سؤال الأمان مطلوبة ويجب ألا تقل عن 3 أحرف.");

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            throw new InvalidOperationException("كلمتا السر غير متطابقتين.");

        if (!IsStrongPassword(password))
            throw new InvalidOperationException(PasswordPolicyText);
    }

    static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    static string NormalizeUsername(string value) =>
        Safe(value).ToLowerInvariant();

    static string NormalizeEmail(string value) =>
        Safe(value).ToLowerInvariant();

    static string NormalizeSecurityAnswer(string value) =>
        Safe(value).ToLowerInvariant();

    static string Safe(string? value) =>
        value?.Trim() ?? "";

    static byte[] GenerateSalt()
    {
        byte[] salt = new byte[SaltSizeBytes];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    static string HashSecret(string secret, byte[] salt)
    {
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            secret,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes);

        return Convert.ToBase64String(hash);
    }

    static bool VerifySecret(string secret, string saltBase64, string expectedHashBase64)
    {
        try
        {
            byte[] salt = Convert.FromBase64String(saltBase64);
            byte[] expected = Convert.FromBase64String(expectedHashBase64);
            byte[] actual = Convert.FromBase64String(HashSecret(secret, salt));

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }

    static string GenerateRecoveryCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[14];
        chars[0] = 'D';
        chars[1] = 'M';
        chars[2] = '-';
        chars[7] = '-';

        Span<byte> bytes = stackalloc byte[10];
        RandomNumberGenerator.Fill(bytes);

        int byteIndex = 0;
        for (int i = 3; i < chars.Length; i++)
        {
            if (chars[i] == '-')
                continue;

            chars[i] = alphabet[bytes[byteIndex++] % alphabet.Length];
        }

        return new string(chars);
    }

    static bool IsTemporarilyLocked(string username, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;

        if (!FailedLogins.TryGetValue(username, out var state))
            return false;

        if (state.LockedUntilUtc <= DateTime.UtcNow)
            return false;

        remaining = state.LockedUntilUtc - DateTime.UtcNow;
        return true;
    }

    static void RegisterFailedLogin(string username)
    {
        if (!FailedLogins.TryGetValue(username, out var state))
        {
            state = new FailedLoginState();
            FailedLogins[username] = state;
        }

        state.Count++;
        state.LastAttemptUtc = DateTime.UtcNow;

        if (state.Count >= MaxFailedAttempts)
            state.LockedUntilUtc = DateTime.UtcNow.AddMinutes(5);
    }

    static void ClearFailedLogin(string username) =>
        FailedLogins.Remove(username);

    static async Task<CredentialState> LoadStateAsync()
    {
        if (!File.Exists(CredentialsFilePath))
            return new CredentialState();

        try
        {
            string json = await File.ReadAllTextAsync(CredentialsFilePath);
            return JsonSerializer.Deserialize<CredentialState>(json, JsonOptions) ??
                   new CredentialState();
        }
        catch
        {
            return new CredentialState();
        }
    }

    static async Task SaveStateAsync(CredentialState state)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        string json = JsonSerializer.Serialize(state, JsonOptions);
        string tempPath = CredentialsFilePath + ".tmp." + Guid.NewGuid().ToString("N");

        await File.WriteAllTextAsync(tempPath, json);

        if (File.Exists(CredentialsFilePath))
            File.Delete(CredentialsFilePath);

        File.Move(tempPath, CredentialsFilePath);
    }

    sealed class FailedLoginState
    {
        public int Count { get; set; }
        public DateTime LastAttemptUtc { get; set; }
        public DateTime LockedUntilUtc { get; set; }
    }
}
