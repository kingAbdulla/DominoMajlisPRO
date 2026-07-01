using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class GamePage
{
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null)
            return;

        AppEvents.TeamAssetsChanged -= OnGameNameTypographyTeamAssetsChanged;
        AppEvents.TeamsChanged -= OnGameNameTypographyTeamsChanged;
        AppEvents.TeamAssetsChanged += OnGameNameTypographyTeamAssetsChanged;
        AppEvents.TeamsChanged += OnGameNameTypographyTeamsChanged;
        _ = ApplyGameNameTypographyAsync();
    }

    async void OnGameNameTypographyTeamAssetsChanged(string teamId) => await ApplyGameNameTypographyAsync();
    async void OnGameNameTypographyTeamsChanged() => await ApplyGameNameTypographyAsync();

    async Task ApplyGameNameTypographyAsync()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await NameTypographyRuntime.ApplyTeamAsync(Team1Name, team1Id);
                await NameTypographyRuntime.ApplyTeamAsync(Team2Name, team2Id);
            });

            await NameTypographyPageScanner.ApplyDelayedAsync(this);
        }
        catch
        {
            // Typography runtime must never crash GamePage.
        }
    }
}
