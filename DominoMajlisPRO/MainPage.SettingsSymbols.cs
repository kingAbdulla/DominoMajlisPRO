using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.LivingVisualPlatform.Controls;
using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Services;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO;

public partial class MainPage
{
    bool _settingsSymbolsTimerStarted;
    bool _mainTeamEffectsTimerStarted;
    LivingVisualHost? _mainTeam1LivingHost;
    LivingVisualHost? _mainTeam2LivingHost;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        ApplyMainHeaderAvatarShape();
        _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
        _ = RefreshMainPreviewTeamEffectsAsync();
        _ = RefreshMainPreviewLivingEmblemsAsync();

        if (Handler == null || _settingsSymbolsTimerStarted)
            return;

        _settingsSymbolsTimerStarted = true;
        int runs = 0;

        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(300), () =>
        {
            runs++;
            ApplyMainHeaderAvatarShape();
            _ = ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync();
            _ = RefreshMainPreviewTeamEffectsAsync();
            _ = RefreshMainPreviewLivingEmblemsAsync();
            NormalizeSettingsSymbols();
            RepairVisibleMainPageText(this);
            return Handler != null && runs < 16;
        });

        if (!_mainTeamEffectsTimerStarted)
        {
            _mainTeamEffectsTimerStarted = true;
            int effectRuns = 0;

            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(500), () =>
            {
                effectRuns++;
                _ = RefreshMainPreviewTeamEffectsAsync();
                _ = RefreshMainPreviewLivingEmblemsAsync();
                return Handler != null && effectRuns < 8;
            });
        }
    }

    async Task RefreshMainPreviewTeamEffectsAsync()
    {
        try
        {
            if (selectedTeam1 != null && !string.IsNullOrWhiteSpace(selectedTeam1.TeamId))
                await TeamEffectEngine.ApplyAroundAsync(
                    PreviewTeam1Logo,
                    selectedTeam1.TeamId,
                    1.18,
                    lightweight: true);

            if (selectedTeam2 != null && !string.IsNullOrWhiteSpace(selectedTeam2.TeamId))
                await TeamEffectEngine.ApplyAroundAsync(
                    PreviewTeam2Logo,
                    selectedTeam2.TeamId,
                    1.18,
                    lightweight: true);
        }
        catch
        {
            // Main preview effects are visual-only and must never block the home page.
        }
    }

    async Task RefreshMainPreviewLivingEmblemsAsync()
    {
        _mainTeam1LivingHost = await ApplyMainLivingEmblemAsync(
            selectedTeam1,
            PreviewTeam1Logo,
            _mainTeam1LivingHost,
            LivingVisualDisplayLocation.MainPageTeamSelector);

        _mainTeam2LivingHost = await ApplyMainLivingEmblemAsync(
            selectedTeam2,
            PreviewTeam2Logo,
            _mainTeam2LivingHost,
            LivingVisualDisplayLocation.MainPageTeamSelector);
    }

    async Task<LivingVisualHost?> ApplyMainLivingEmblemAsync(
        TeamProfileModel? team,
        Image targetImage,
        LivingVisualHost? existingHost,
        LivingVisualDisplayLocation displayLocation)
    {
        if (team == null ||
            string.IsNullOrWhiteSpace(team.EmblemAssetId) ||
            targetImage.Parent is not Grid parent)
        {
            RemoveMainLivingHost(existingHost);
            targetImage.IsVisible = true;
            return null;
        }

        var manifest = await new StoreCatalogLivingVisualManifestProvider()
            .GetManifestAsync(team.EmblemAssetId);
        if (manifest == null)
        {
            RemoveMainLivingHost(existingHost);
            targetImage.IsVisible = true;
            return null;
        }

        var owner = await ResolveTeamLivingOwnerAsync(team, team.EmblemAssetId);
        if (owner == null)
        {
            RemoveMainLivingHost(existingHost);
            targetImage.IsVisible = true;
            return null;
        }

        if (existingHost != null && existingHost.Parent == parent)
        {
            existingHost.AssetId = team.EmblemAssetId;
            existingHost.StaticFallbackImage = manifest.StaticFallbackImage;
            existingHost.ApplicationUserId = owner.ApplicationUserId;
            existingHost.PlayerId = owner.PlayerId;
            existingHost.TeamId = team.TeamId;
            existingHost.DisplayLocation = displayLocation;
            targetImage.IsVisible = false;
            return existingHost;
        }

        RemoveMainLivingHost(existingHost);
        var host = new LivingVisualHost
        {
            AssetId = team.EmblemAssetId,
            StaticFallbackImage = manifest.StaticFallbackImage,
            ApplicationUserId = owner.ApplicationUserId,
            PlayerId = owner.PlayerId,
            TeamId = team.TeamId,
            DisplayLocation = displayLocation,
            WidthRequest = targetImage.WidthRequest,
            HeightRequest = targetImage.HeightRequest,
            HorizontalOptions = targetImage.HorizontalOptions,
            VerticalOptions = targetImage.VerticalOptions,
            InputTransparent = true,
            ZIndex = targetImage.ZIndex + 1
        };

        Grid.SetRow(host, Grid.GetRow(targetImage));
        Grid.SetColumn(host, Grid.GetColumn(targetImage));
        Grid.SetRowSpan(host, Grid.GetRowSpan(targetImage));
        Grid.SetColumnSpan(host, Grid.GetColumnSpan(targetImage));
        parent.Children.Add(host);
        targetImage.IsVisible = false;
        return host;
    }

    static void RemoveMainLivingHost(LivingVisualHost? host)
    {
        if (host?.Parent is Grid parent)
            parent.Children.Remove(host);
    }

    static async Task<PlayerOwnedStoreItem?> ResolveTeamLivingOwnerAsync(TeamProfileModel team, string assetId)
    {
        var playerIds = new[] { team.Player1Id, team.Player2Id }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var playerId in playerIds)
        {
            var owned = await PlayerInventoryService.LoadOwnedAsync(playerId);
            var match = owned.FirstOrDefault(item =>
                item.IsOwned &&
                !item.IsExpired &&
                CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId) &&
                string.Equals(
                    StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId),
                    StoreProductAssetType.TeamLivingEmblem.ToString(),
                    StringComparison.OrdinalIgnoreCase));

            if (match != null)
                return match;
        }

        return null;
    }

    void NormalizeSettingsSymbols()
    {
        SetSymbol(DataArrow, isDataExpanded);
        SetSymbol(SystemArrow, isSystemExpanded);
        SetSymbol(HonorsArrow, isHonorsExpanded);
        SetSymbol(SupportArrow, isSupportExpanded);
        SetSymbol(AboutArrow, isAboutExpanded);
        SetSymbol(SecurityArrow, isSecurityExpanded);
    }

    static void RepairVisibleMainPageText(Element element)
    {
        if (element is Label label)
        {
            label.Text = RepairKnownBrokenMainText(label.Text);
        }
        else if (element is Button button)
        {
            button.Text = RepairKnownBrokenMainText(button.Text);
        }
        else if (element is Entry entry)
        {
            entry.Placeholder = RepairKnownBrokenMainText(entry.Placeholder);
        }
        else if (element is Editor editor)
        {
            editor.Placeholder = RepairKnownBrokenMainText(editor.Placeholder);
        }

        foreach (var child in element.LogicalChildren)
            RepairVisibleMainPageText(child);
    }

    static string RepairKnownBrokenMainText(string? value)
    {
        var text = NormalizeBrokenUiText(value);

        if (string.IsNullOrWhiteSpace(text))
            return text;

        if (!LooksBrokenMainText(text))
            return text;

        return "غير متاح";
    }

    static bool LooksBrokenMainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains("?", StringComparison.Ordinal) ||
               value.Contains("â", StringComparison.Ordinal) ||
               value.Contains("Ã", StringComparison.Ordinal) ||
               value.Contains("أƒ", StringComparison.Ordinal);
    }

    static void SetSymbol(Label? label, bool isExpanded)
    {
        if (label == null)
            return;

        label.Text = isExpanded ? "-" : "+";
    }
}
