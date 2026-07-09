using System.Text;
using System.Text.Json;
using DominoMajlisPRO.Backend.Configuration;

namespace DominoMajlisPRO.Backend.Authentication;

public sealed class SupabaseRecoveryOtpService
{
    sealed class RecoveryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    readonly HttpClient httpClient;

    public SupabaseRecoveryOtpService()
        : this(new HttpClient())
    {
    }

    public SupabaseRecoveryOtpService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task<(bool Success, string Message)> RequestEmailOtpAsync(
        string username,
        string email) =>
        SendAsync(new
        {
            action = "request_email_otp",
            username = username.Trim(),
            email = email.Trim()
        });

    public Task<(bool Success, string Message)> VerifyEmailOtpAndResetPasswordAsync(
        string username,
        string email,
        string otp,
        string newPassword) =>
        SendAsync(new
        {
            action = "verify_email_otp_reset",
            username = username.Trim(),
            email = email.Trim(),
            otp = otp.Trim(),
            new_password = newPassword
        });

    public Task<(bool Success, string Message)> RegisterSecurityQuestionsAsync(
        string username,
        string email,
        IReadOnlyList<(string Question, string Answer)> questions) =>
        SendAsync(new
        {
            action = "register_security_questions",
            username = username.Trim(),
            email = email.Trim(),
            questions = questions.Select(item => new
            {
                question = item.Question.Trim(),
                answer = item.Answer.Trim()
            }).ToArray()
        });

    public Task<(bool Success, string Message)> VerifySecurityQuestionsAndResetPasswordAsync(
        string username,
        string email,
        IReadOnlyList<(string Question, string Answer)> questions,
        string newPassword) =>
        SendAsync(new
        {
            action = "verify_security_questions_reset",
            username = username.Trim(),
            email = email.Trim(),
            questions = questions.Select(item => new
            {
                question = item.Question.Trim(),
                answer = item.Answer.Trim()
            }).ToArray(),
            new_password = newPassword
        });

    async Task<(bool Success, string Message)> SendAsync(object body)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return (false, "Supabase غير مهيأ داخل التطبيق.");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            SupabaseBackendConfiguration.ProjectUrl.TrimEnd('/') + "/functions/v1/account-recovery");

        request.Headers.Add("apikey", SupabaseBackendConfiguration.PublishableKey);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            SupabaseBackendConfiguration.PublishableKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await httpClient.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecoveryResponse>(json, JsonOptions);

            string message = result?.Message ??
                             (response.IsSuccessStatusCode
                                ? "تم تنفيذ العملية."
                                : "تعذر تنفيذ عملية الاسترداد.");

            return (response.IsSuccessStatusCode && result?.Success == true, message);
        }
        catch
        {
            return (false, "تعذر الاتصال بخدمة الاسترداد. تحقق من الإنترنت ثم حاول مرة أخرى.");
        }
    }
}
