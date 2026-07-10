using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DominoMajlisPRO.Backend.Configuration;

namespace DominoMajlisPRO.Backend.Authentication;

public sealed class SupabaseRecoveryOtpService
{
    public sealed record SecurityQuestionItem(string Id, string Question);

    public sealed record SecurityQuestionsChallengeResult(
        bool Success,
        string Message,
        string ChallengeToken,
        IReadOnlyList<SecurityQuestionItem> Questions);

    public sealed record SecurityAnswersVerificationResult(
        bool Success,
        string Message,
        string ResetToken);

    sealed class RecoveryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";

        [JsonPropertyName("challenge_token")]
        public string ChallengeToken { get; set; } = "";

        [JsonPropertyName("reset_token")]
        public string ResetToken { get; set; } = "";

        public List<SecurityQuestionResponse> Questions { get; set; } = new();
    }

    sealed class SecurityQuestionResponse
    {
        public string Id { get; set; } = "";
        public string Question { get; set; } = "";
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
        SendSimpleAsync(new
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
        SendSimpleAsync(new
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
        SendSimpleAsync(new
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

    public async Task<SecurityQuestionsChallengeResult> BeginSecurityQuestionsRecoveryAsync(
        string username,
        string email)
    {
        var response = await SendAsync(new
        {
            action = "get_security_questions",
            username = username.Trim(),
            email = email.Trim()
        });

        return new SecurityQuestionsChallengeResult(
            response.Success,
            response.Message,
            response.ChallengeToken,
            response.Questions
                .Select(item => new SecurityQuestionItem(item.Id, item.Question))
                .ToArray());
    }

    public async Task<SecurityAnswersVerificationResult> VerifySecurityAnswersAsync(
        string username,
        string email,
        string challengeToken,
        IReadOnlyList<(string QuestionId, string Answer)> answers)
    {
        var response = await SendAsync(new
        {
            action = "verify_security_answers",
            username = username.Trim(),
            email = email.Trim(),
            challenge_token = challengeToken,
            answers = answers.Select(item => new
            {
                question_id = item.QuestionId,
                answer = item.Answer.Trim()
            }).ToArray()
        });

        return new SecurityAnswersVerificationResult(
            response.Success,
            response.Message,
            response.ResetToken);
    }

    public Task<(bool Success, string Message)> ResetPasswordWithSecurityTokenAsync(
        string username,
        string email,
        string resetToken,
        string newPassword) =>
        SendSimpleAsync(new
        {
            action = "reset_password_with_security_token",
            username = username.Trim(),
            email = email.Trim(),
            reset_token = resetToken,
            new_password = newPassword
        });

    async Task<(bool Success, string Message)> SendSimpleAsync(object body)
    {
        var response = await SendAsync(body);
        return (response.Success, response.Message);
    }

    async Task<RecoveryResponse> SendAsync(object body)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
        {
            return new RecoveryResponse
            {
                Success = false,
                Message = "Supabase غير مهيأ داخل التطبيق."
            };
        }

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
            var httpResponse = await httpClient.SendAsync(request);
            string json = await httpResponse.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecoveryResponse>(json, JsonOptions) ?? new RecoveryResponse();

            if (string.IsNullOrWhiteSpace(result.Message))
            {
                result.Message = httpResponse.IsSuccessStatusCode
                    ? "تم تنفيذ العملية."
                    : "تعذر تنفيذ عملية الاسترداد.";
            }

            result.Success = httpResponse.IsSuccessStatusCode && result.Success;
            return result;
        }
        catch
        {
            return new RecoveryResponse
            {
                Success = false,
                Message = "تعذر الاتصال بخدمة الاسترداد. تحقق من الإنترنت ثم حاول مرة أخرى."
            };
        }
    }
}
