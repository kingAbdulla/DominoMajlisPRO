using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DominoMajlisPRO.Pages;

public partial class GamePage
{
    static GamePage()
    {
        if (Application.Current != null)
            Application.Current.PageAppearing += OnGamePageAppearingForTeamEffects;
    }

    private static void OnGamePageAppearingForTeamEffects(object? sender, Page page)
    {
        if (page is GamePage gamePage)
            _ = gamePage.RefreshScoreboardTeamEffectsAfterRenderAsync();
    }

    private async Task RefreshScoreboardTeamEffectsAfterRenderAsync()
    {
        try
        {
            await Task.Delay(180);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await TeamEffectEngine.ApplyAroundAsync(Team1Emblem, team1Id, 1.18, lightweight: true);
                await TeamEffectEngine.ApplyAroundAsync(Team2Emblem, team2Id, 1.18, lightweight: true);
            });
        }
        catch
        {
            // Visual refresh must never block active match rendering.
        }
    }
}
