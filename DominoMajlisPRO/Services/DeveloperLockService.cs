using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class DeveloperLockService
{
    static readonly string FilePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "developer_lock.json");

    const int Pbkdf2Iterations = 150000;
    const int RecoveryCodeCount = 5;

    public static async Task InitializeAsync()
    {
        if (File.Exists(FilePath))
            return;

        var model =
            new DeveloperLockModel
            {
                IsEnabled = false,
                IsSetupCompleted = false,
                DeveloperId = "",
                Username = "",
                PasswordHash = "",
                PasswordSalt = "",
                DeviceFingerprint = "",
                RecoveryCodeHashes = new List<string>(),
                CreatedAt = DateTime.MinValue,
                LastLoginAt = DateTime.MinValue,
                LastPasswordChange = DateTime.MinValue
            };

        await SaveAsync(model);
    }

    public static async Task<DeveloperLockModel> LoadAsync()
    {
        await InitializeAsync();

        string json =
            await File.ReadAllTextAsync(FilePath);

        return JsonSerializer.Deserialize<DeveloperLockModel>(json)
               ?? new DeveloperLockModel();
    }

    public static async Task SaveAsync(DeveloperLockModel model)
    {
        string json =
            JsonSerializer.Serialize(
                model,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(FilePath, json);
    }

    public static async Task<bool> HasDeveloperAccountAsync()
    {
        var model =
            await LoadAsync();

        return
            model.IsSetupCompleted &&
            model.IsEnabled &&
            !string.IsNullOrWhiteSpace(model.DeveloperId) &&
            !string.IsNullOrWhiteSpace(model.Username) &&
            !string.IsNullOrWhiteSpace(model.PasswordHash) &&
            !string.IsNullOrWhiteSpace(model.PasswordSalt);
    }

    public static async Task<(bool Success, string Message, List<string> RecoveryCodes)>
        SetupFirstDeveloperAsync(
            string username,
            string password,
            string confirmPassword)
    {
        var current =
            await LoadAsync();

        if (current.IsSetupCompleted)
        {
            return
                (
                    false,
                    "تم إنشاء حساب المطور مسبقاً.",
                    new List<string>()
                );
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return
                (
                    false,
                    "أدخل اسم مستخدم المطور.",
                    new List<string>()
                );
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return
                (
                    false,
                    "كلمة مرور المطور يجب أن تكون 8 أحرف على الأقل.",
                    new List<string>()
                );
        }

        if (password != confirmPassword)
        {
            return
                (
                    false,
                    "كلمة المرور وتأكيدها غير متطابقين.",
                    new List<string>()
                );
        }

        string salt =
            GenerateSalt();

        string passwordHash =
            HashPasswordWithSalt(password, salt);

        List<string> recoveryCodes =
            GenerateRecoveryCodes(RecoveryCodeCount);

        List<string> recoveryHashes =
            recoveryCodes
                .Select(code => HashRecoveryCode(code))
                .ToList();

        var model =
            new DeveloperLockModel
            {
                IsEnabled = true,
                IsSetupCompleted = true,
                DeveloperId = $"DEV-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
                Username = username.Trim(),
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                DeviceFingerprint = GetDeviceFingerprint(),
                RecoveryCodeHashes = recoveryHashes,
                CreatedAt = DateTime.Now,
                LastLoginAt = DateTime.Now,
                LastPasswordChange = DateTime.Now
            };

        await SaveAsync(model);

        await EnsureDeveloperHonorIdentityAsync(
            model.Username,
            "DEVELOPER_ACCOUNT_SETUP");

        await SecurityLogService.AddAsync(
            "SECURITY",
            "تم إنشاء حساب المطور الأساسي",
            $"Developer Account Created: {model.DeveloperId}",
            "High",
            true);

        return
            (
                true,
                "تم إنشاء حساب المطور بنجاح. تم توليد 5 أكواد استرداد. احفظ الملف الآن.",
                recoveryCodes
            );
    }

    public static async Task<bool> VerifyLoginAsync(
        string username,
        string password)
    {
        var model =
            await LoadAsync();

        if (!model.IsSetupCompleted || !model.IsEnabled)
            return false;

        if (!model.Username.Equals(
                username.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(model.PasswordSalt))
            return false;

        string hash =
            HashPasswordWithSalt(
                password,
                model.PasswordSalt);

        bool valid =
            SafeEqualsHex(
                model.PasswordHash,
                hash);

        if (!valid)
            return false;

        model.LastLoginAt =
            DateTime.Now;

        await SaveAsync(model);

        await EnsureDeveloperHonorIdentityAsync(
            model.Username,
            "DEVELOPER_ACCOUNT_LOGIN");

        await SecurityLogService.AddAsync(
            "SECURITY",
            "تم تسجيل دخول المطور",
            $"Developer Login: {model.DeveloperId}",
            "High",
            true);

        return true;
    }

    public static async Task<(bool Success, string Message)>
        ChangePasswordAsync(
            string username,
            string oldPassword,
            string newPassword,
            string confirmNewPassword)
    {
        bool valid =
            await VerifyLoginAsync(
                username,
                oldPassword);

        if (!valid)
        {
            return
                (
                    false,
                    "اسم المستخدم أو كلمة المرور الحالية غير صحيحة."
                );
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return
                (
                    false,
                    "كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل."
                );
        }

        if (newPassword != confirmNewPassword)
        {
            return
                (
                    false,
                    "كلمة المرور الجديدة وتأكيدها غير متطابقين."
                );
        }

        var model =
            await LoadAsync();

        string newSalt =
            GenerateSalt();

        model.PasswordSalt =
            newSalt;

        model.PasswordHash =
            HashPasswordWithSalt(
                newPassword,
                newSalt);

        model.LastPasswordChange =
            DateTime.Now;

        await SaveAsync(model);

        await SecurityLogService.AddAsync(
            "SECURITY",
            "تم تغيير كلمة مرور المطور",
            "Developer Password Changed",
            "High",
            true);

        return
            (
                true,
                "تم تغيير كلمة مرور المطور بنجاح."
            );
    }

    public static async Task<(bool Success, string Message)>
        ResetPasswordWithRecoveryCodeAsync(
            string username,
            string recoveryCode,
            string newPassword,
            string confirmNewPassword)
    {
        var model =
            await LoadAsync();

        if (!model.IsSetupCompleted || !model.IsEnabled)
        {
            return
                (
                    false,
                    "لا يوجد حساب مطور مفعّل. استخدم استيراد Developer Vault."
                );
        }

        if (!model.Username.Equals(
                username.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return
                (
                    false,
                    "اسم المستخدم غير صحيح."
                );
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return
                (
                    false,
                    "كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل."
                );
        }

        if (newPassword != confirmNewPassword)
        {
            return
                (
                    false,
                    "كلمة المرور الجديدة وتأكيدها غير متطابقين."
                );
        }

        string recoveryHash =
            HashRecoveryCode(
                recoveryCode.Trim());

        int index =
            model.RecoveryCodeHashes.FindIndex(x =>
                x.Equals(
                    recoveryHash,
                    StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            return
                (
                    false,
                    "كود الاسترداد غير صحيح أو مستخدم مسبقاً."
                );
        }

        model.RecoveryCodeHashes.RemoveAt(index);

        string newSalt =
            GenerateSalt();

        model.PasswordSalt =
            newSalt;

        model.PasswordHash =
            HashPasswordWithSalt(
                newPassword,
                newSalt);

        model.LastPasswordChange =
            DateTime.Now;

        await SaveAsync(model);

        await EnsureDeveloperHonorIdentityAsync(
            model.Username,
            "DEVELOPER_RECOVERY_CODE");

        await SecurityLogService.AddAsync(
            "SECURITY",
            "تمت إعادة تعيين كلمة مرور المطور بكود استرداد",
            "Developer Password Reset By Recovery Code",
            "High",
            true);

        return
            (
                true,
                "تمت إعادة تعيين كلمة مرور المطور بنجاح."
            );
    }

    public static async Task<List<string>> RegenerateRecoveryCodesAsync(
        string username,
        string password)
    {
        bool valid =
            await VerifyLoginAsync(
                username,
                password);

        if (!valid)
            throw new Exception("اسم المستخدم أو كلمة المرور غير صحيحة.");

        var model =
            await LoadAsync();

        List<string> recoveryCodes =
            GenerateRecoveryCodes(RecoveryCodeCount);

        model.RecoveryCodeHashes =
            recoveryCodes
                .Select(code => HashRecoveryCode(code))
                .ToList();

        await SaveAsync(model);

        await SecurityLogService.AddAsync(
            "SECURITY",
            "تم توليد 5 أكواد استرداد جديدة للمطور",
            "Developer Recovery Codes Regenerated",
            "High",
            true);

        return recoveryCodes;
    }

    public static async Task EnsureDeveloperHonorIdentityAsync(
        string displayName,
        string activationSource)
    {
        var identity =
            await HonorIdentityService.LoadAsync();

        if (identity.IsActivated &&
            identity.Role == HonorRoleType.Developer)
        {
            return;
        }

        var developerIdentity =
            new HonorIdentityModel
            {
                Role = HonorRoleType.Developer,
                IsActivated = true,
                ActivationKey = activationSource,
                ActivationDate = DateTime.Now,
                DeviceId = GetDeviceFingerprint(),
                DisplayName = displayName.Trim()
            };

        await HonorIdentityService.SaveAsync(
            developerIdentity);
    }

    public static async Task ClearDeveloperAccountAsync()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);

        await InitializeAsync();

        await SecurityLogService.AddAsync(
            "SECURITY",
            "تم حذف حساب المطور من الجهاز",
            "Developer Account Cleared",
            "High",
            true);
    }

    // =========================
    // OLD CODE COMPATIBILITY
    // =========================

    public static string HashPassword(string password)
    {
        using var sha =
            SHA256.Create();

        byte[] bytes =
            sha.ComputeHash(
                Encoding.UTF8.GetBytes(password));

        return Convert.ToHexString(bytes);
    }

    public static async Task<bool> VerifyPasswordAsync(string password)
    {
        var model =
            await LoadAsync();

        if (string.IsNullOrWhiteSpace(model.PasswordSalt))
        {
            string legacyHash =
                HashPassword(password);

            return model.PasswordHash == legacyHash;
        }

        string hash =
            HashPasswordWithSalt(
                password,
                model.PasswordSalt);

        return model.PasswordHash == hash;
    }

    public static async Task SetPasswordAsync(string password)
    {
        var model =
            await LoadAsync();

        string salt =
            GenerateSalt();

        model.PasswordSalt =
            salt;

        model.PasswordHash =
            HashPasswordWithSalt(
                password,
                salt);

        model.LastPasswordChange =
            DateTime.Now;

        model.IsEnabled =
            true;

        model.IsSetupCompleted =
            true;

        if (string.IsNullOrWhiteSpace(model.DeveloperId))
            model.DeveloperId = $"DEV-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";

        if (string.IsNullOrWhiteSpace(model.Username))
            model.Username = "Developer";

        if (string.IsNullOrWhiteSpace(model.DeviceFingerprint))
            model.DeviceFingerprint = GetDeviceFingerprint();

        if (model.CreatedAt == DateTime.MinValue)
            model.CreatedAt = DateTime.Now;

        await SaveAsync(model);

        await EnsureDeveloperHonorIdentityAsync(
            model.Username,
            "DEVELOPER_PASSWORD_SET");

        await SecurityLogService.AddAsync(
            "SECURITY",
            "تم تغيير كلمة مرور المطور",
            "Developer Lock Password Changed",
            "High",
            true);
    }

    // =========================
    // HELPERS
    // =========================

    static string GenerateSalt()
    {
        byte[] bytes =
            RandomNumberGenerator.GetBytes(32);

        return Convert.ToHexString(bytes);
    }

    static string HashPasswordWithSalt(
        string password,
        string salt)
    {
        using var derive =
            new Rfc2898DeriveBytes(
                password,
                Convert.FromHexString(salt),
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256);

        return Convert.ToHexString(
            derive.GetBytes(32));
    }

    static List<string> GenerateRecoveryCodes(int count)
    {
        List<string> codes =
            new();

        for (int i = 0; i < count; i++)
        {
            string part1 =
                RandomHex(4);

            string part2 =
                RandomHex(4);

            string code =
                $"DMP-REC-{part1}-{part2}";

            codes.Add(code);
        }

        return codes;
    }

    static string HashRecoveryCode(string code)
    {
        using var sha =
            SHA256.Create();

        byte[] bytes =
            sha.ComputeHash(
                Encoding.UTF8.GetBytes(
                    code.Trim().ToUpperInvariant()));

        return Convert.ToHexString(bytes);
    }

    static string RandomHex(int length)
    {
        int byteCount =
            Math.Max(4, length);

        byte[] bytes =
            RandomNumberGenerator.GetBytes(byteCount);

        return Convert
            .ToHexString(bytes)
            .Substring(0, length)
            .ToUpperInvariant();
    }

    static bool SafeEqualsHex(
        string leftHex,
        string rightHex)
    {
        try
        {
            byte[] left =
                Convert.FromHexString(leftHex);

            byte[] right =
                Convert.FromHexString(rightHex);

            return CryptographicOperations.FixedTimeEquals(
                left,
                right);
        }
        catch
        {
            return false;
        }
    }

    static string GetDeviceFingerprint()
    {
        try
        {
            return
                $"{DeviceInfo.Manufacturer}-{DeviceInfo.Model}-{DeviceInfo.Platform}";
        }
        catch
        {
            return "UnknownDevice";
        }
    }
}