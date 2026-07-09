using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DominoMajlisPRO.Backend.Configuration;

namespace DominoMajlisPRO.Backend.Authentication;

public sealed class SupabaseAuthenticationService
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    readonly HttpClient httpClient;

    public SupabaseAuthenticationService()
        : this(new HttpClient())
    {
    }

    public SupabaseAuthenticationService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<SupabaseAuthenticationResult> SignUpAsync(
        string email,
        string password,
        string username,
        string nickname)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return SupabaseAuthenticationResult.Failure("Supabase غير مهيأ داخل التطبيق.");

        username = username.Trim();
        nickname = nickname.Trim();

        var response = await SendAuthRequestAsync(
            HttpMethod.Post,
            "/auth/v1/signup",
            new
            {
                email = email.Trim(),
                password,
                data = new
                {
                    username,
                    nickname,
                    display_name = nickname
                }
            });

        if (!response.IsSuccessStatusCode)
            return SupabaseAuthenticationResult.Failure(await ReadErrorAsync(response));

        var auth = await ReadAuthResponseAsync(response);

        if (auth?.User == null)
            return SupabaseAuthenticationResult.Success("تم إنشاء الحساب. تحقق من بريدك الإلكتروني قبل تسجيل الدخول.");

        return SupabaseAuthenticationResult.Success(
            "تم إنشاء الحساب. تحقق من بريدك الإلكتروني قبل تسجيل الدخول.",
            ToSession(auth));
    }

    public async Task<SupabaseAuthenticationResult> SignInAsync(
        string email,
        string password)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return SupabaseAuthenticationResult.Failure("Supabase غير مهيأ داخل التطبيق.");

        var response = await SendAuthRequestAsync(
            HttpMethod.Post,
            "/auth/v1/token?grant_type=password",
            new
            {
                email = email.Trim(),
                password
            });

        if (!response.IsSuccessStatusCode)
            return SupabaseAuthenticationResult.Failure(await ReadErrorAsync(response));

        var auth = await ReadAuthResponseAsync(response);
        var session = ToSession(auth);

        if (session == null)
            return SupabaseAuthenticationResult.Failure("تعذر إنشاء جلسة Supabase.");

        if (!session.EmailConfirmed)
            return SupabaseAuthenticationResult.Failure("يجب تأكيد البريد الإلكتروني قبل تسجيل الدخول.");

        await SupabaseTokenStore.SaveAsync(session);

        return SupabaseAuthenticationResult.Success("تم تسجيل الدخول بنجاح.", session);
    }

    public async Task<SupabaseAuthenticationResult> RefreshSessionAsync(
        string refreshToken,
        string fallbackNickname = "")
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return SupabaseAuthenticationResult.Failure("Supabase غير مهيأ داخل التطبيق.");

        if (string.IsNullOrWhiteSpace(refreshToken))
            return SupabaseAuthenticationResult.Failure("انتهت الجلسة. يرجى تسجيل الدخول من جديد.");

        refreshToken = refreshToken.Trim();

        var response = await SendAuthRequestAsync(
            HttpMethod.Post,
            "/auth/v1/token?grant_type=refresh_token",
            new
            {
                refresh_token = refreshToken
            });

        if (!response.IsSuccessStatusCode)
            return SupabaseAuthenticationResult.Failure(await ReadErrorAsync(response));

        var refresh = await ReadRefreshResponseAsync(response);
        var session = ToSession(refresh, fallbackNickname, refreshToken);

        if (session == null)
            return SupabaseAuthenticationResult.Failure("تعذر تجديد جلسة Supabase.");

        await SupabaseTokenStore.SaveAsync(session);
        return SupabaseAuthenticationResult.Success("تم تجديد الجلسة.", session);
    }

    public async Task<SupabaseAuthenticationResult> EnsureFreshSessionAsync(
        SupabaseAuthenticationSession session)
    {
        if (session.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(2) &&
            !string.IsNullOrWhiteSpace(session.AccessToken))
        {
            return SupabaseAuthenticationResult.Success("الجلسة صالحة.", session);
        }

        return await RefreshSessionAsync(
            session.RefreshToken,
            session.Nickname);
    }

    public async Task<SupabaseAuthenticationResult> SendPasswordResetAsync(string email)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return SupabaseAuthenticationResult.Failure("Supabase غير مهيأ داخل التطبيق.");

        var response = await SendAuthRequestAsync(
            HttpMethod.Post,
            "/auth/v1/recover",
            new
            {
                email = email.Trim()
            });

        if (!response.IsSuccessStatusCode)
            return SupabaseAuthenticationResult.Failure(await ReadErrorAsync(response));

        return SupabaseAuthenticationResult.Success("تم إرسال رابط استعادة كلمة المرور إلى البريد الإلكتروني.");
    }

    public async Task<SupabaseAuthenticationResult> UpdateNicknameAsync(
        string accessToken,
        string nickname)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return SupabaseAuthenticationResult.Failure("Supabase غير مهيأ داخل التطبيق.");

        nickname = nickname.Trim();

        var response = await SendUserRequestAsync(
            HttpMethod.Put,
            "/auth/v1/user",
            accessToken,
            new
            {
                data = new
                {
                    nickname,
                    display_name = nickname
                }
            });

        if (!response.IsSuccessStatusCode)
            return SupabaseAuthenticationResult.Failure(await ReadErrorAsync(response));

        var user = await ReadUserResponseAsync(response);
        var session = user == null
            ? null
            : new SupabaseAuthenticationSession
            {
                SupabaseUserId = user.Id,
                Email = user.Email,
                Username = user.GetUsername(),
                Nickname = user.GetNickname(),
                EmailConfirmed = !string.IsNullOrWhiteSpace(user.EmailConfirmedAt),
                AccessToken = accessToken,
                RefreshToken = "",
                ExpiresAtUtc = DateTime.UtcNow
            };

        return SupabaseAuthenticationResult.Success(
            "تم تحديث الاسم الظاهر بنجاح.",
            session);
    }

    public void SignOut()
    {
        SupabaseTokenStore.Clear();
    }

    async Task<HttpResponseMessage> SendAuthRequestAsync(
        HttpMethod method,
        string path,
        object body)
    {
        return await SendRequestAsync(
            method,
            path,
            SupabaseBackendConfiguration.PublishableKey,
            body);
    }

    async Task<HttpResponseMessage> SendUserRequestAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object body)
    {
        return await SendRequestAsync(
            method,
            path,
            accessToken,
            body);
    }

    async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string path,
        string bearerToken,
        object body)
    {
        var request = new HttpRequestMessage(
            method,
            BuildUri(path));

        request.Headers.Add("apikey", SupabaseBackendConfiguration.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            bearerToken);

        string json = JsonSerializer.Serialize(body);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return await httpClient.SendAsync(request);
    }

    static string BuildUri(string path) =>
        SupabaseBackendConfiguration.ProjectUrl.TrimEnd('/') + path;

    static async Task<SupabaseAuthResponse?> ReadAuthResponseAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<SupabaseAuthResponse>(json, JsonOptions);
    }

    static async Task<SupabaseRefreshResponse?> ReadRefreshResponseAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<SupabaseRefreshResponse>(json, JsonOptions);
    }

    static async Task<SupabaseAuthUser?> ReadUserResponseAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<SupabaseAuthUserResponse>(json, JsonOptions)?.ToUser();
    }

    static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();

        try
        {
            var error = JsonSerializer.Deserialize<SupabaseAuthError>(json, JsonOptions);
            return TranslateSupabaseError(error?.BestMessage ?? json);
        }
        catch
        {
            return TranslateSupabaseError(json);
        }
    }

    static string TranslateSupabaseError(string? message)
    {
        string raw = message?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(raw))
            return "فشل الاتصال بخدمة الحسابات.";

        string lower = raw.ToLowerInvariant();

        if (lower.Contains("invalid login credentials") ||
            lower.Contains("invalid_grant") ||
            lower.Contains("invalid credentials"))
            return "اسم المستخدم أو كلمة المرور غير صحيحة.";

        if (lower.Contains("email not confirmed") ||
            lower.Contains("email_not_confirmed"))
            return "يجب تأكيد البريد الإلكتروني قبل تسجيل الدخول.";

        if (lower.Contains("user already registered") ||
            lower.Contains("already registered") ||
            lower.Contains("already exists"))
            return "هذا البريد الإلكتروني مسجل مسبقاً.";

        if (lower.Contains("password") && lower.Contains("weak"))
            return "كلمة المرور ضعيفة. استخدم كلمة مرور أقوى.";

        if (lower.Contains("rate limit") || lower.Contains("too many"))
            return "تم تنفيذ محاولات كثيرة. انتظر قليلاً ثم حاول مرة أخرى.";

        if (lower.Contains("network") || lower.Contains("timeout") || lower.Contains("connection"))
            return "تعذر الاتصال بالخادم. تحقق من الإنترنت ثم حاول مرة أخرى.";

        if (lower.Contains("token") && lower.Contains("expired"))
            return "انتهت الجلسة. يرجى تسجيل الدخول من جديد.";

        if (lower.Contains("invalid jwt"))
            return "انتهت الجلسة أو أصبحت غير صالحة. يرجى تسجيل الدخول من جديد.";

        return raw.StartsWith("{")
            ? "تعذر إكمال العملية. تحقق من البيانات وحاول مرة أخرى."
            : raw;
    }

    static SupabaseAuthenticationSession? ToSession(SupabaseAuthResponse? auth)
    {
        if (auth?.User == null)
            return null;

        return new SupabaseAuthenticationSession
        {
            SupabaseUserId = auth.User.Id,
            Email = auth.User.Email,
            Username = auth.User.GetUsername(),
            Nickname = auth.User.GetNickname(),
            EmailConfirmed = !string.IsNullOrWhiteSpace(auth.User.EmailConfirmedAt),
            AccessToken = auth.AccessToken,
            RefreshToken = auth.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(auth.ExpiresIn, 0))
        };
    }

    static SupabaseAuthenticationSession? ToSession(
        SupabaseRefreshResponse? refresh,
        string fallbackNickname,
        string fallbackRefreshToken)
    {
        if (refresh?.User == null)
            return null;

        string nickname = refresh.User.GetNickname();
        if (string.IsNullOrWhiteSpace(nickname))
            nickname = fallbackNickname.Trim();

        string nextRefreshToken = string.IsNullOrWhiteSpace(refresh.RefreshToken)
            ? fallbackRefreshToken.Trim()
            : refresh.RefreshToken.Trim();

        return new SupabaseAuthenticationSession
        {
            SupabaseUserId = refresh.User.Id,
            Email = refresh.User.Email,
            Username = refresh.User.GetUsername(),
            Nickname = nickname,
            EmailConfirmed = !string.IsNullOrWhiteSpace(refresh.User.EmailConfirmedAt),
            AccessToken = refresh.AccessToken,
            RefreshToken = nextRefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(refresh.ExpiresIn, 0))
        };
    }
}
