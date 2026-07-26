using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class DeveloperAuthorizationGuard
{
    public const string DeveloperRoleName = "Developer";

    public static async Task<bool> IsAuthorizedAsync(string surface, string operation)
    {
        try
        {
            var user = await ApplicationUserService.GetCurrentUserAsync();
            var authorized = user.Role == ApplicationUserRole.Developer &&
                             !string.IsNullOrWhiteSpace(user.ApplicationUserId) &&
                             !user.IsTemporary;

            if (!authorized)
            {
                await LogDeniedAsync(surface, operation, user);
            }

            return authorized;
        }
        catch (Exception ex)
        {
            await SecurityLogService.AddAsync(
                "Authorization",
                "Developer access denied",
                $"Surface: {surface}\nOperation: {operation}\nReason: {ex.GetType().Name}",
                "Warning",
                isPermanent: true);

            return false;
        }
    }

    public static async Task RequireDeveloperAsync(string surface, string operation)
    {
        if (!await IsAuthorizedAsync(surface, operation))
            throw new UnauthorizedAccessException("Developer authorization is required.");
    }

    public static async Task<bool> EnsurePageAuthorizedAsync(Page page, string surface)
    {
        if (await IsAuthorizedAsync(surface, "Open page"))
            return true;

        await page.DisplayAlertAsync("غير مصرح", "هذه الصفحة متاحة للمطور فقط.", "حسناً");

        if (page.Navigation?.NavigationStack?.Count > 1)
            await page.Navigation.PopAsync();

        return false;
    }

    static Task LogDeniedAsync(
        string surface,
        string operation,
        ApplicationUserModel user) =>
        SecurityLogService.AddAsync(
            "Authorization",
            "Developer access denied",
            $"Surface: {surface}\nOperation: {operation}\nApplicationUserId: {user.ApplicationUserId}\nPlayerId: {user.PlayerId}\nRole: {user.Role}",
            "Warning",
            isPermanent: true);
}
