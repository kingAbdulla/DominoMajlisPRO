using System.Text;
using System.Text.Json;
using DominoMajlisPRO.Backend.Configuration;

namespace DominoMajlisPRO.Backend.Profiles;

public sealed class UsernameRegistryService
{
    sealed class RegistryResponse
    {
        public bool Success { get; set; }
        public bool Available { get; set; }
        public string Username { get; set; } = "";
        public string ReservationToken { get; set; } = "";
        public string Message { get; set; } = "";
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    readonly HttpClient httpClient = new();

    public Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> CheckAsync(string username) =>
        SendAsync(new { action = "check", username = username.Trim() });

    public Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> SuggestAsync(string baseName) =>
        SendAsync(new { action = "suggest", base_name = baseName.Trim() });

    public Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> ReserveAsync(string username) =>
        SendAsync(new { action = "reserve", username = username.Trim() });

    public Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> ActivateAsync(
        string username,
        string reservationToken,
        string supabaseUserId,
        string applicationUserId,
        string playerId) =>
        SendAsync(new
        {
            action = "activate",
            username = username.Trim(),
            reservation_token = reservationToken.Trim(),
            supabase_user_id = supabaseUserId.Trim(),
            application_user_id = applicationUserId.Trim(),
            player_id = playerId.Trim()
        });

    public Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> ReleaseAsync(
        string username,
        string reservationToken) =>
        SendAsync(new
        {
            action = "release",
            username = username.Trim(),
            reservation_token = reservationToken.Trim()
        });

    async Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> SendAsync(object body)
    {
        if (!SupabaseBackendConfiguration.IsConfigured)
            return (false, false, "", "", "Supabase غير مهيأ داخل التطبيق.");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            SupabaseBackendConfiguration.ProjectUrl.TrimEnd('/') + "/functions/v1/username-registry");

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
            using var response = await httpClient.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RegistryResponse>(json, JsonOptions);

            return (
                response.IsSuccessStatusCode && result?.Success == true,
                result?.Available == true,
                result?.Username ?? "",
                result?.ReservationToken ?? "",
                result?.Message ?? "تعذر تنفيذ عملية اسم المستخدم.");
        }
        catch
        {
            return (false, false, "", "", "تعذر الاتصال بخدمة أسماء المستخدمين.");
        }
    }
}
