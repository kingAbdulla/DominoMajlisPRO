using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.Pages;

public partial class GamePage
{
    private bool _effectsRefreshHooked;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (_effectsRefreshHooked || Handler == null)
            return;

        _effectsRefreshHooked = true;
        Loaded += OnEffectsRefreshLoaded;
    }

    private async void OnEffectsRefreshLoaded(object? sender, EventArgs e)
    {
        try
        {
            await Task.Delay(120);

            if (!string.IsNullOrWhiteSpace(team1Id))
            {
                await TeamEffectEngine.ApplyAroundAsync(Team1Emblem, team1Id, 1.18, lightweight: true);
                await BindTeamNamePlateAsync(Team1NamePlate, Team1Name, team1Id);
            }

            if (!string.IsNullOrWhiteSpace(team2Id))
            {
                await TeamEffectEngine.ApplyAroundAsync(Team2Emblem, team2Id, 1.18, lightweight: true);
                await BindTeamNamePlateAsync(Team2NamePlate, Team2Name, team2Id);
            }
        }
        catch
        {
        }
    }

    private static async Task BindTeamNamePlateAsync(
        GalleryEngine.Components.RuntimeNamePlateView plate,
        Label fallback,
        string teamId)
    {
        var identity = await TeamNameTypographyResolver.ResolveAsync(teamId);
        plate.OwnerId = teamId;
        plate.DisplayText = fallback.Text;
        plate.IsVisible = identity.HasVisual;
        fallback.IsVisible = !identity.HasVisual;
    }
}
