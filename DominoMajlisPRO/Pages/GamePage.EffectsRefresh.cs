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
                await TeamEffectEngine.ApplyAroundAsync(Team1Emblem, team1Id, 1.18, lightweight: true);

            if (!string.IsNullOrWhiteSpace(team2Id))
                await TeamEffectEngine.ApplyAroundAsync(Team2Emblem, team2Id, 1.18, lightweight: true);
        }
        catch
        {
        }
    }
}
