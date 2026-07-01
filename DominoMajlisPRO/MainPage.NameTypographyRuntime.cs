using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO;

public partial class MainPage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null)
            return;

        AppEvents.PlayerProfileChanged -= OnNameTypographyRuntimeChanged;
        AppEvents.TeamsChanged -= OnNameTypographyRuntimeChanged;
        AppEvents.TeamAssetsChanged -= OnNameTypographyTeamAssetsChanged;
        AppEvents.StoreEconomyChanged -= OnNameTypographyStoreChanged;

        AppEvents.PlayerProfileChanged += OnNameTypographyRuntimeChanged;
        AppEvents.TeamsChanged += OnNameTypographyRuntimeChanged;
        AppEvents.TeamAssetsChanged += OnNameTypographyTeamAssetsChanged;
        AppEvents.StoreEconomyChanged += OnNameTypographyStoreChanged;

        _ = ApplyMainNameTypographyAsync();
    }

    async void OnNameTypographyRuntimeChanged() => await ApplyMainNameTypographyAsync();
    async void OnNameTypographyTeamAssetsChanged(string teamId) => await ApplyMainNameTypographyAsync();
    async void OnNameTypographyStoreChanged(string playerId) => await ApplyMainNameTypographyAsync();

    async Task ApplyMainNameTypographyAsync()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var user = await ApplicationUserService.GetCurrentUserAsync();
                if (!string.IsNullOrWhiteSpace(user.PlayerId))
                    await NameTypographyRuntime.ApplyPlayerAsync(HeaderPlayerNameLabel, user.PlayerId);

                if (selectedTeam1 != null)
                    await NameTypographyRuntime.ApplyTeamAsync(PreviewTeam1NameLabel, selectedTeam1.TeamId);

                if (selectedTeam2 != null)
                    await NameTypographyRuntime.ApplyTeamAsync(PreviewTeam2NameLabel, selectedTeam2.TeamId);
            });
        }
        catch
        {
            // Typography runtime must never crash the main page.
        }
    }
}
