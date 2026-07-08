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
        string nickname)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return SupabaseAuthenticationResult.Failure("Supabase غير مهيأ داخل التطبيق.");

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

    public void SignOut()
    {
        SupabaseTokenStore.Clear();
    }

    async Task<HttpResponseMessage> SendAuthRequestAsync(
        HttpMethod method,
        string path,
        object body)
    {
        var request = new HttpRequestMessage(
            method,
            BuildUri(path));

        request.Headers.Add("apikey", SupabaseBackendConfiguration.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            SupabaseBackendConfiguration.PublishableKey);

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

    static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();

        try
        {
            var error = JsonSerializer.Deserialize<SupabaseAuthError>(json, JsonOptions);
            return error?.BestMessage ?? "فشل الاتصال بخدمة Supabase.";
        }
        catch
        {
            return string.IsNullOrWhiteSpace(json)
                ? "فشل الاتصال بخدمة Supabase."
                : json;
        }
    }

    static SupabaseAuthenticationSession? ToSession(SupabaseAuthResponse? auth)
    {
        if (auth?.User == null)
            return null;

        return new SupabaseAuthenticationSession
        {
            SupabaseUserId = auth.User.Id,
            Email = auth.User.Email,
            Nickname = auth.User.GetNickname(),
            EmailConfirmed = !string.IsNullOrWhiteSpace(auth.User.EmailConfirmedAt),
            AccessToken = auth.AccessToken,
            RefreshToken = auth.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(auth.ExpiresIn, 0))
        };
    }
}
