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
        string nickname,
        IEnumerable<string>? securityQuestions = null)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return SupabaseAuthenticationResult.Failure("Supabase غير مهيأ داخل التطبيق.");

        username = username.Trim();
        nickname = nickname.Trim();
        var securityQuestionList = securityQuestions?
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .ToArray() ?? Array.Empty<string>();

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
                    display_name = nickname,
                    security_question_count = securityQuestionList.Length,
                    security_questions = securityQuestionList
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

        var verifiedUserResult = await GetCurrentUserAsync(session.AccessToken);
        if (!verifiedUserResult.IsSuccess || verifiedUserResult.Session == null)
            return SupabaseAuthenticationResult.Failure(verifiedUserResult.Message);

        session = new SupabaseAuthenticationSession
        {
            SupabaseUserId = verifiedUserResult.Session.SupabaseUserId,
            Email = verifiedUserResult.Session.Email,
            Username = verifiedUserResult.Session.Username,
            Nickname = verifiedUserResult.Session.Nickname,
            EmailConfirmed = verifiedUserResult.Session.EmailConfirmed,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            ExpiresAtUtc = session.ExpiresAtUtc
        };

        if (!session.EmailConfirmed)
            return SupabaseAuthenticationResult.Failure("لم تتم مزامنة تأكيد البريد بعد. ارجع إلى التطبيق وحاول تسجيل الدخول مرة أخرى بعد لحظات.");

        await SupabaseTokenStore.SaveAsync(session);

        return SupabaseAuthenticationResult.Success("تم تسجيل الدخول بنجاح.", session);
    }

    public async Task<SupabaseAuthenticationResult> GetCurrentUserAsync(string accessToken)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return SupabaseAuthenticationResult.Failure("Supabase غير مهيأ داخل التطبيق.");

        if (string.IsNullOrWhiteSpace(accessToken))
            return SupabaseAuthenticationResult.Failure("جلسة Supabase غير صالحة.");

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUri("/auth/v1/user"));

        request.Headers.Add("apikey", SupabaseBackendConfiguration.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return SupabaseAuthenticationResult.Failure(await ReadErrorAsync(response));

        var user = await ReadUserResponseAsync(response);
        if (user == null)
            return SupabaseAuthenticationResult.Failure("تعذر مزامنة حالة الحساب من Supabase.");

        var session = new SupabaseAuthenticationSession
        {
            SupabaseUserId = user.Id,
            Email = user.Email,
            Username = user.GetUsername(),
            Nickname = user.GetNickname(),
            EmailConfirmed = !string.IsNullOrWhiteSpace(user.EmailConfirmedAt),
            AccessToken = accessToken.Trim(),
            RefreshToken = "",
            ExpiresAtUtc = DateTime.UtcNow
        };

        return SupabaseAuthenticationResult.Success("تمت مزامنة حالة الحساب.", session);
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

    Uri BuildUri(string path) =>
        new(SupabaseBackendConfiguration.ProjectUrl.TrimEnd('/') + path);

    static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("msg", out var msg))
                return TranslateError(msg.GetString() ?? body);

            if (document.RootElement.TryGetProperty("message", out var message))
                return TranslateError(message.GetString() ?? body);

            if (document.RootElement.TryGetProperty("error_description", out var description))
                return TranslateError(description.GetString() ?? body);
        }
        catch
        {
            // ignore malformed payloads and use the raw body.
        }

        return TranslateError(body);
    }

    static string TranslateError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "تعذر تنفيذ العملية.";

        if (message.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase))
            return "اسم المستخدم أو كلمة السر غير صحيحة.";

        if (message.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
            return "يجب تأكيد البريد الإلكتروني قبل تسجيل الدخول.";

        if (message.Contains("User already registered", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("already been registered", StringComparison.OrdinalIgnoreCase))
            return "يوجد حساب مسجل مسبقاً بهذا البريد الإلكتروني.";

        if (message.Contains("Password should be", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("weak password", StringComparison.OrdinalIgnoreCase))
            return "كلمة السر ضعيفة. استخدم كلمة سر أقوى.";

        if (message.Contains("Email address", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            return "البريد الإلكتروني غير صالح.";

        return message;
    }

    static async Task<SupabaseAuthResponse?> ReadAuthResponseAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupabaseAuthResponse>(json, JsonOptions);
    }

    static async Task<SupabaseAuthResponse?> ReadRefreshResponseAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupabaseAuthResponse>(json, JsonOptions);
    }

    static async Task<SupabaseUserResponse?> ReadUserResponseAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupabaseUserResponse>(json, JsonOptions);
    }

    static SupabaseAuthenticationSession? ToSession(
        SupabaseAuthResponse? auth,
        string fallbackNickname = "",
        string fallbackRefreshToken = "")
    {
        if (auth?.User == null)
            return null;

        return new SupabaseAuthenticationSession
        {
            SupabaseUserId = auth.User.Id,
            Email = auth.User.Email,
            Username = auth.User.GetUsername(),
            Nickname = auth.User.GetNickname(fallbackNickname),
            EmailConfirmed = !string.IsNullOrWhiteSpace(auth.User.EmailConfirmedAt),
            AccessToken = auth.AccessToken ?? "",
            RefreshToken = auth.RefreshToken ?? fallbackRefreshToken,
            ExpiresAtUtc = auth.ExpiresIn > 0
                ? DateTime.UtcNow.AddSeconds(auth.ExpiresIn)
                : DateTime.UtcNow
        };
    }
}
