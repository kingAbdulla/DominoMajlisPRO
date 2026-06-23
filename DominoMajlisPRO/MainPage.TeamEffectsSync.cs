using System.ComponentModel;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO;

public partial class MainPage
{
    private bool _teamEffectsSyncHooked;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (_teamEffectsSyncHooked || Handler == null)
            return;

        _teamEffectsSyncHooked = true;
        PreviewTeam1Logo.PropertyChanged += OnPreviewTeamLogoChanged;
        PreviewTeam2Logo.PropertyChanged += OnPreviewTeamLogoChanged;
        _ = RefreshPreviewTeamEffectsAfterLayoutAsync();
    }

    private void OnPreviewTeamLogoChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Image.Source))
            _ = RefreshPreviewTeamEffectsAfterLayoutAsync();
    }

    private async Task RefreshPreviewTeamEffectsAfterLayoutAsync()
    {
        try
        {
            await Task.Delay(120);

            if (selectedTeam1 != null && !string.IsNullOrWhiteSpace(selectedTeam1.TeamId))
                await TeamEffectEngine.ApplyAroundAsync(PreviewTeam1Logo, selectedTeam1.TeamId, 1.18, lightweight: true);

            if (selectedTeam2 != null && !string.IsNullOrWhiteSpace(selectedTeam2.TeamId))
                await TeamEffectEngine.ApplyAroundAsync(PreviewTeam2Logo, selectedTeam2.TeamId, 1.18, lightweight: true);
        }
        catch
        {
        }
    }
}
