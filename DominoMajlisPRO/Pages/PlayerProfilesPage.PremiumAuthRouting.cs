namespace DominoMajlisPRO.Pages;

public partial class PlayerProfilesPage
{
    async void OnOpenPremiumAuthClicked(object? sender, EventArgs e)
    {
        // Force the same premium authentication gateway used on first launch.
        // This prevents the legacy local-account picker from exposing saved
        // player/account names when the user presses تسجيل الدخول from the
        // Player Profiles account hub.
        await DominoMajlisPRO.Services.ApplicationUserService.LogoutAsync();
        Application.Current!.MainPage = new NavigationPage(new PremiumAuthPage());
    }
}
