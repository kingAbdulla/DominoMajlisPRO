using DominoMajlisPRO.Backend.Authentication;

namespace DominoMajlisPRO.Backend.Profiles;

/// <summary>
/// Compatibility facade for the registration UI.
/// Username availability and creation now use account-identity as the single
/// source of truth, preventing partial reservations in a separate registry.
/// </summary>
public sealed class UsernameRegistryService
{
    readonly SupabaseAccountIdentityService identityService = new();

    public async Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> CheckAsync(
        string username)
    {
        var result = await identityService.CheckUsernameAsync(username);
        return (
            result.Success,
            result.Available,
            string.IsNullOrWhiteSpace(result.Username) ? username.Trim() : result.Username,
            "",
            NormalizeAvailabilityMessage(result.Message, result.Available));
    }

    public async Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> SuggestAsync(
        string baseName)
    {
        var result = await identityService.SuggestUsernameAsync(baseName);
        return (
            result.Success,
            result.Success && result.Available,
            result.Username,
            "",
            result.Success
                ? "✓ تم توليد اسم مستخدم متاح."
                : result.Message);
    }

    /// <summary>
    /// The account-identity Edge Function performs the real atomic username
    /// claim while creating the Supabase account. No early server reservation
    /// is created here.
    /// </summary>
    public async Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> ReserveAsync(
        string username)
    {
        var check = await identityService.CheckUsernameAsync(username);
        if (!check.Success || !check.Available)
        {
            return (
                false,
                false,
                username.Trim(),
                "",
                NormalizeAvailabilityMessage(check.Message, false));
        }

        return (
            true,
            true,
            username.Trim(),
            "atomic-account-identity",
            "✓ اسم المستخدم متاح وجاهز لإنشاء الحساب.");
    }

    /// <summary>
    /// Activation is already completed atomically by account-identity during
    /// RegisterAccountAsync. This method remains for compatibility with the UI
    /// pipeline and must not create a second username record.
    /// </summary>
    public Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> ActivateAsync(
        string username,
        string reservationToken,
        string supabaseUserId,
        string applicationUserId,
        string playerId) =>
        Task.FromResult((
            true,
            true,
            username.Trim(),
            reservationToken,
            "تم ربط اسم المستخدم بالحساب."));

    public Task<(bool Success, bool Available, string Username, string ReservationToken, string Message)> ReleaseAsync(
        string username,
        string reservationToken) =>
        Task.FromResult((
            true,
            true,
            username.Trim(),
            "",
            "لا يوجد حجز وسيط يحتاج إلى تحرير."));

    static string NormalizeAvailabilityMessage(string message, bool available)
    {
        if (available)
            return "✓ اسم المستخدم متاح.";

        if (string.IsNullOrWhiteSpace(message))
            return "✕ اسم المستخدم مستخدم بالفعل.";

        if (message.Contains("محجوز", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("مستخدم", StringComparison.OrdinalIgnoreCase))
        {
            return "✕ اسم المستخدم مستخدم بالفعل.";
        }

        return message;
    }
}
