namespace DominoMajlisPRO.Backend.Authentication;

public static class SupabaseTokenStore
{
    const string AccessTokenKey = "supabase_access_token";
    const string RefreshTokenKey = "supabase_refresh_token";
    const string UserIdKey = "supabase_user_id";
    const string EmailKey = "supabase_email";
    const string NicknameKey = "supabase_nickname";
    const string ExpiresAtKey = "supabase_expires_at_utc";

    public static async Task SaveAsync(SupabaseAuthenticationSession session)
    {
        await SecureStorage.SetAsync(AccessTokenKey, session.AccessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, session.RefreshToken);
        await SecureStorage.SetAsync(UserIdKey, session.SupabaseUserId);
        await SecureStorage.SetAsync(EmailKey, session.Email);
        await SecureStorage.SetAsync(NicknameKey, session.Nickname);
        await SecureStorage.SetAsync(ExpiresAtKey, session.ExpiresAtUtc.ToString("O"));
    }

    public static async Task<SupabaseAuthenticationSession?> LoadAsync()
    {
        string accessToken = await SecureStorage.GetAsync(AccessTokenKey) ?? "";
        string refreshToken = await SecureStorage.GetAsync(RefreshTokenKey) ?? "";
        string userId = await SecureStorage.GetAsync(UserIdKey) ?? "";
        string email = await SecureStorage.GetAsync(EmailKey) ?? "";
        string nickname = await SecureStorage.GetAsync(NicknameKey) ?? "";
        string expiresAtValue = await SecureStorage.GetAsync(ExpiresAtKey) ?? "";

        if (string.IsNullOrWhiteSpace(accessToken) ||
            string.IsNullOrWhiteSpace(refreshToken) ||
            string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        DateTime.TryParse(
            expiresAtValue,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var expiresAtUtc);

        return new SupabaseAuthenticationSession
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            SupabaseUserId = userId,
            Email = email,
            Nickname = nickname,
            EmailConfirmed = true,
            ExpiresAtUtc = expiresAtUtc == default ? DateTime.UtcNow : expiresAtUtc
        };
    }

    public static void Clear()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        SecureStorage.Remove(UserIdKey);
        SecureStorage.Remove(EmailKey);
        SecureStorage.Remove(NicknameKey);
        SecureStorage.Remove(ExpiresAtKey);
    }
}
