using System.Text.Json;
using System.Text.Json.Serialization;

namespace DominoMajlisPRO.Backend.Authentication;

public sealed class SupabaseAuthenticationResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = "";
    public SupabaseAuthenticationSession? Session { get; init; }

    public static SupabaseAuthenticationResult Success(
        string message,
        SupabaseAuthenticationSession? session = null) =>
        new()
        {
            IsSuccess = true,
            Message = message,
            Session = session
        };

    public static SupabaseAuthenticationResult Failure(string message) =>
        new()
        {
            IsSuccess = false,
            Message = message
        };
}

public sealed class SupabaseAuthenticationSession
{
    public string SupabaseUserId { get; init; } = "";
    public string Email { get; init; } = "";
    public string Nickname { get; init; } = "";
    public bool EmailConfirmed { get; init; }
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAtUtc { get; init; }
}

sealed class SupabaseAuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("user")]
    public SupabaseAuthUser? User { get; set; }
}

sealed class SupabaseAuthUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("email_confirmed_at")]
    public string? EmailConfirmedAt { get; set; }

    [JsonPropertyName("user_metadata")]
    public Dictionary<string, JsonElement>? UserMetadata { get; set; }

    public string GetNickname()
    {
        if (UserMetadata == null)
            return "";

        return GetMetadataString("nickname") ??
               GetMetadataString("display_name") ??
               GetMetadataString("name") ??
               "";
    }

    string? GetMetadataString(string key)
    {
        if (!UserMetadata.TryGetValue(key, out var value))
            return null;

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;
    }
}

sealed class SupabaseAuthError
{
    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    [JsonPropertyName("message")]
    public string? MessageAlt { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    public string BestMessage =>
        ErrorDescription ?? MessageAlt ?? Message ?? Error ?? "فشل الاتصال بخدمة التوثيق.";
}
