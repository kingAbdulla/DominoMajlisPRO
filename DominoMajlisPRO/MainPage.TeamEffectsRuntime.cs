using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DominoMajlisPRO;

public partial class MainPage
{
    static MainPage()
    {
        if (Application.Current != null)
            Application.Current.PageAppearing += OnMainPageAppearingForTeamEffects;
    }

    private static void OnMainPageAppearingForTeamEffects(object? sender, Page page)
    {
        if (page is MainPage mainPage)
            _ = mainPage.RefreshPreviewTeamEffectsAfterRenderAsync();
    }

    private async Task RefreshPreviewTeamEffectsAfterRenderAsync()
    {
        try
        {
            await Task.Delay(180);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (selectedTeam1 != null)
                    await TeamEffectEngine.ApplyAroundAsync(PreviewTeam1Logo, selectedTeam1.TeamId, 1.18, lightweight: true);

                if (selectedTeam2 != null)
                    await TeamEffectEngine.ApplyAroundAsync(PreviewTeam2Logo, selectedTeam2.TeamId, 1.18, lightweight: true);
            });
        }
        catch
        {
            // Visual refresh must never block MainPage rendering.
        }
    }
}
