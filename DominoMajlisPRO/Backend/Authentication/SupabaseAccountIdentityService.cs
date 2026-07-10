using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DominoMajlisPRO.Backend.Configuration;

namespace DominoMajlisPRO.Backend.Authentication;

public sealed class SupabaseAccountIdentityService
{
    public sealed class IdentityResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = "";
        public bool Available { get; init; }
        public bool Verified { get; init; }
        public string Username { get; init; } = "";
        public string UserId { get; init; } = "";
        public int RetryAfterSeconds { get; init; }
        public int ExpiresInSeconds { get; init; }
    }

    sealed class IdentityResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public bool Available { get; set; }
        public bool Verified { get; set; }
        public string Username { get; set; } = "";

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = "";

        [JsonPropertyName("retry_after_seconds")]
        public int RetryAfterSeconds { get; set; }

        [JsonPropertyName("expires_in_seconds")]
        public int ExpiresInSeconds { get; set; }
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    readonly HttpClient httpClient;

    public SupabaseAccountIdentityService()
        : this(new HttpClient())
    {
    }

    public SupabaseAccountIdentityService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task<IdentityResult> CheckUsernameAsync(string username) =>
        SendAsync(
            new { action = "check_username", username = username.Trim() },
            useAccessToken: false);

    public Task<IdentityResult> SuggestUsernameAsync(string source) =>
        SendAsync(
            new { action = "suggest_username", username = source.Trim() },
            useAccessToken: false);

    public Task<IdentityResult> RegisterAccountAsync(
        string username,
        string email,
        string password,
        string displayName) =>
        SendAsync(
            new
            {
                action = "register_account",
                username = username.Trim(),
                email = email.Trim(),
                password,
                display_name = displayName.Trim()
            },
            useAccessToken: false);

    public Task<IdentityResult> SyncPlayerIdentityAsync(
        string accessToken,
        string applicationUserId,
        string playerId) =>
        SendAsync(
            new
            {
                action = "sync_player_identity",
                application_user_id = applicationUserId.Trim(),
                player_id = playerId.Trim()
            },
            useAccessToken: true,
            accessToken);

    public Task<IdentityResult> RequestEmailVerificationOtpAsync(string accessToken) =>
        SendAsync(
            new { action = "request_email_verification_otp" },
            useAccessToken: true,
            accessToken);

    public Task<IdentityResult> VerifyEmailVerificationOtpAsync(
        string accessToken,
        string otp) =>
        SendAsync(
            new
            {
                action = "verify_email_verification_otp",
                otp = otp.Trim()
            },
            useAccessToken: true,
            accessToken);

    async Task<IdentityResult> SendAsync(
        object body,
        bool useAccessToken,
        string accessToken = "")
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return Failure("Supabase غير مهيأ داخل التطبيق.");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            SupabaseBackendConfiguration.ProjectUrl.TrimEnd('/') + "/functions/v1/account-identity");

        request.Headers.Add("apikey", SupabaseBackendConfiguration.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            useAccessToken ? accessToken : SupabaseBackendConfiguration.PublishableKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await httpClient.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            var payload = JsonSerializer.Deserialize<IdentityResponse>(json, JsonOptions);

            if (payload == null)
                return Failure("تعذر قراءة استجابة خدمة الهوية.");

            return new IdentityResult
            {
                Success = response.IsSuccessStatusCode && payload.Success,
                Message = string.IsNullOrWhiteSpace(payload.Message)
                    ? "تعذر تنفيذ عملية الهوية."
                    : payload.Message,
                Available = payload.Available,
                Verified = payload.Verified,
                Username = payload.Username,
                UserId = payload.UserId,
                RetryAfterSeconds = payload.RetryAfterSeconds,
                ExpiresInSeconds = payload.ExpiresInSeconds
            };
        }
        catch
        {
            return Failure("تعذر الاتصال بخدمة الهوية. تحقق من الإنترنت ثم حاول مرة أخرى.");
        }
    }

    static IdentityResult Failure(string message) => new()
    {
        Success = false,
        Message = message
    };
}
