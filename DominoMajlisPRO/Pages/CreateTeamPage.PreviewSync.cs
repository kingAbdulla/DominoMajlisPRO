using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class CreateTeamPage
{
    private bool _previewSyncHooked;
    private bool _teamEffectVisualSyncRunning;
    private int _nameTypographySyncVersion;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (_previewSyncHooked || Handler == null)
            return;

        _previewSyncHooked = true;
        TeamNameEntry.TextChanged += OnPreviewIdentityTextChanged;
        Player1Entry.TextChanged += OnPreviewIdentityTextChanged;
        Player2Entry.TextChanged += OnPreviewIdentityTextChanged;
        TeamEffectCarousel.SelectionChanged += OnTeamEffectVisualSelectionChanged;

        Dispatcher.Dispatch(RefreshCreateTeamVisualPipeline);

        var runs = 0;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(250), () =>
        {
            runs++;
            RefreshCreateTeamVisualPipeline();
            return Handler != null && runs < 16;
        });
    }

    private void OnPreviewIdentityTextChanged(object? sender, TextChangedEventArgs e) =>
        RefreshCreateTeamVisualPipeline();

    private void OnTeamEffectVisualSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        _ = SyncTeamEffectChoicesAndPreviewAsync();

    private void RefreshCreateTeamVisualPipeline()
    {
        UpdatePreviewIdentityLabelsSafely();
        _ = ApplyPreviewNameTypographyAsync();
        _ = UpdatePreviewAvatarsAsync();
        _ = SyncTeamEffectChoicesAndPreviewAsync();
    }

    private void UpdatePreviewIdentityLabelsSafely()
    {
        try
        {
            PreviewTeamName.Text = string.IsNullOrWhiteSpace(TeamNameEntry.Text)
                ? "اسم الفريق"
                : TeamNameEntry.Text.Trim();

            var player1 = string.IsNullOrWhiteSpace(Player1Entry.Text)
                ? "اللاعب الأول"
                : Player1Entry.Text.Trim();
            var player2 = string.IsNullOrWhiteSpace(Player2Entry.Text)
                ? "اللاعب الثاني"
                : Player2Entry.Text.Trim();

            PreviewPlayer1.Text = player1;
            PreviewPlayer2.Text = isTeamMode ? player2 : string.Empty;
            PreviewPlayer2.IsVisible = isTeamMode;
            PreviewPlayer2Host.IsVisible = isTeamMode;
            PreviewMode.Text = isTeamMode ? "فريق" : "فردي";
            SaveButtonText.Text = IsEditMode ? "تعديل الفريق" : "إنشاء الفريق";
            RefreshValidationPanel();
        }
        catch
        {
            // Visual sync must never block CreateTeam editing.
        }
    }

    private async Task ApplyPreviewNameTypographyAsync()
    {
        var version = Interlocked.Increment(ref _nameTypographySyncVersion);
        try
        {
            var teamTask = TeamNameTypographyResolver.ResolveAsync(CurrentTeam?.TeamId);
            var player1Task = ResolvePreviewPlayerNameTypographyAsync(Player1Entry.Text);
            var player2Task = isTeamMode
                ? ResolvePreviewPlayerNameTypographyAsync(Player2Entry.Text)
                : Task.FromResult<NameTypographyIdentity?>(null);

            await Task.WhenAll(teamTask, player1Task, player2Task);
            if (version != _nameTypographySyncVersion)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ApplyTypographyToLabel(PreviewTeamName, teamTask.Result, 28, Color.FromArgb("#F2C46D"), true);
                ApplyTypographyToLabel(PreviewPlayer1, player1Task.Result, 13, Colors.White, false);
                ApplyTypographyToLabel(PreviewPlayer2, player2Task.Result, 13, Colors.White, false);
            });
        }
        catch
        {
            // Typography is visual-only here; saving and validation stay independent.
        }
    }

    private static async Task<NameTypographyIdentity?> ResolvePreviewPlayerNameTypographyAsync(string? text)
    {
        var player = await ResolvePlayerFromPreviewTextAsync(text);
        return string.IsNullOrWhiteSpace(player?.PlayerId)
            ? null
            : await PlayerNameTypographyResolver.ResolveAsync(player.PlayerId);
    }

    private static async Task<PlayerProfileModel?> ResolvePlayerFromPreviewTextAsync(string? text)
    {
        var value = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var byId = await PlayerProfileService.GetPlayerByIdAsync(value);
        if (byId != null)
            return byId;

        return await PlayerProfileService.GetPlayerByNameAsync(value);
    }

    private static void ApplyTypographyToLabel(
        Label label,
        NameTypographyIdentity? identity,
        double fallbackFontSize,
        Color fallbackColor,
        bool bold)
    {
        label.FontFamily = "Tajawal-Regular";
        label.FontSize = fallbackFontSize;
        label.FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None;
        label.TextColor = fallbackColor;
        label.Opacity = 1;

        var preset = identity?.ResolvePreset();
        if (preset == null)
            return;

        var normalized = preset.Normalized();
        label.FontFamily = string.IsNullOrWhiteSpace(normalized.FontFamily)
            ? "Tajawal-Regular"
            : normalized.FontFamily;
        label.FontSize = Math.Clamp(normalized.FontSize * normalized.Scale, 11, bold ? 30 : 18);
        label.TextColor = Color.FromArgb(normalized.PrimaryColor);
        label.Opacity = normalized.Opacity;
    }

    private async Task SyncTeamEffectChoicesAndPreviewAsync()
    {
        if (_teamEffectVisualSyncRunning)
            return;

        _teamEffectVisualSyncRunning = true;
        try
        {
            var catalog = await StoreAssetCatalogService.LoadAsync();
            var ownerIds = await ResolveCurrentTeamEffectOwnerIdsAsync();
            var effects = new List<TeamEffectCarouselItem>();

            foreach (var ownerId in ownerIds)
            {
                var owned = await PlayerInventoryService.LoadOwnedAsync(ownerId);
                foreach (var item in owned)
                {
                    var effect = ResolveTeamEffectFromCatalogForCreateTeam(catalog, item.AssetId, item.StoreTypeId);
                    if (effect == null)
                        continue;

                    if (effects.Any(existing =>
                            string.Equals(existing.AssetId, item.AssetId, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(existing.OwnerPlayerId, ownerId, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    effects.Add(new TeamEffectCarouselItem(
                        item.AssetId,
                        string.IsNullOrWhiteSpace(effect.DisplayName) ? item.AssetId : effect.DisplayName,
                        effect,
                        ownerId));
                }
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var previousSelection = selectedTeamEffectAssetId;
                teamEffectItems = effects.Count == 0
                    ? new List<TeamEffectCarouselItem>()
                    : new[] { new TeamEffectCarouselItem(string.Empty, "بدون تأثير", null) }.Concat(effects).ToList();

                TeamEffectCarousel.ItemsSource = teamEffectItems;
                TeamEffectSection.IsVisible = teamEffectItems.Count > 1;

                var selected = teamEffectItems.FirstOrDefault(item =>
                                   string.Equals(item.AssetId, previousSelection, StringComparison.OrdinalIgnoreCase))
                               ?? teamEffectItems.FirstOrDefault(item => item.IsNone)
                               ?? teamEffectItems.FirstOrDefault();

                if (selected == null || selected.Effect == null)
                {
                    selectedTeamEffectAssetId = string.Empty;
                    selectedTeamEffectOwnerPlayerId = string.Empty;
                    IdentityEffectRenderer.ApplyAround(PreviewEmblem, null);
                    return;
                }

                selectedTeamEffectAssetId = selected.AssetId;
                selectedTeamEffectOwnerPlayerId = selected.OwnerPlayerId;
                TeamEffectCarousel.SelectedItem = selected;
                IdentityEffectRenderer.ApplyAround(
                    PreviewEmblem,
                    selected.Effect,
                    TeamEffectEngine.DefaultTeamEffectScale,
                    lightweight: true);
            });
        }
        catch
        {
            // Effect synchronization is visual-only here; save validation remains in OnSaveClicked.
        }
        finally
        {
            _teamEffectVisualSyncRunning = false;
        }
    }

    private async Task<IReadOnlyList<string>> ResolveCurrentTeamEffectOwnerIdsAsync()
    {
        var result = new List<string>();
        await AddPlayerIdFromEntryAsync(Player1Entry.Text);
        if (isTeamMode)
            await AddPlayerIdFromEntryAsync(Player2Entry.Text);

        if (CurrentTeam != null)
        {
            AddId(CurrentTeam.Player1Id);
            if (isTeamMode)
                AddId(CurrentTeam.Player2Id);
        }

        return result;

        async Task AddPlayerIdFromEntryAsync(string? text)
        {
            var player = await ResolvePlayerFromPreviewTextAsync(text);
            AddId(player?.PlayerId);
        }

        void AddId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;
            if (!result.Contains(id.Trim(), StringComparer.OrdinalIgnoreCase))
                result.Add(id.Trim());
        }
    }

    private static CatalogAssetDisplay? ResolveTeamEffectFromCatalogForCreateTeam(
        IReadOnlyList<CatalogAssetDisplay> catalog,
        string? assetId,
        string? storeTypeId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return null;

        var canonicalTypeId = StoreAssetCatalogService.CanonicalTypeId(storeTypeId);
        if (string.Equals(canonicalTypeId, StoreProductAssetType.TeamEffect.ToString(), StringComparison.OrdinalIgnoreCase))
            return StoreAssetCatalogService.Resolve(catalog, assetId, StoreProductAssetType.TeamEffect.ToString());

        var legacy = StoreAssetCatalogService.Resolve(catalog, assetId, StoreProductAssetType.Effect.ToString());
        if (legacy == null)
            return null;

        return legacy.AssetType == StoreProductAssetType.TeamEffect ||
               string.Equals(legacy.EquipTarget, "TeamEffect", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(legacy.EquipTarget, "Team", StringComparison.OrdinalIgnoreCase)
            ? legacy
            : null;
    }
}
