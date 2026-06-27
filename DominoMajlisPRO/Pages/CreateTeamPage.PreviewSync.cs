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
    private int _teamEffectSyncVersion;

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
        RepairCreateTeamArabicText(this);
        UpdatePreviewIdentityLabelsSafely();
        QueueTeamEffectSliderRefresh();
    }

    private void QueueTeamEffectSliderRefresh()
    {
        var version = Interlocked.Increment(ref _teamEffectSyncVersion);
        _ = SyncTeamEffectChoicesAndPreviewAsync(version);
        _ = DelayedTeamEffectSliderRefreshAsync(version, 160);
        _ = DelayedTeamEffectSliderRefreshAsync(version, 420);
        _ = DelayedTeamEffectSliderRefreshAsync(version, 900);
    }

    private async Task DelayedTeamEffectSliderRefreshAsync(int version, int delayMs)
    {
        await Task.Delay(delayMs);
        if (version != _teamEffectSyncVersion || Handler == null)
            return;
        await SyncTeamEffectChoicesAndPreviewAsync(version);
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

            PreviewPlayer1.Text = isTeamMode ? player2 : player1;
            PreviewPlayer2.Text = isTeamMode ? player1 : string.Empty;
            PreviewPlayer2.IsVisible = isTeamMode;

            PreviewMode.Text = isTeamMode ? "فريق" : "فردي";
            SaveButtonText.Text = IsEditMode ? "تحديث الفريق" : "إنشاء الفريق";
        }
        catch
        {
        }
    }

    private Task SyncTeamEffectChoicesAndPreviewAsync() =>
        SyncTeamEffectChoicesAndPreviewAsync(_teamEffectSyncVersion);

    private async Task SyncTeamEffectChoicesAndPreviewAsync(int syncVersion)
    {
        if (_teamEffectVisualSyncRunning)
            return;

        _teamEffectVisualSyncRunning = true;
        try
        {
            var catalog = await StoreAssetCatalogService.LoadAsync();
            var owners = await ResolveCurrentTeamEffectOwnersAsync();
            var effects = new List<TeamEffectCarouselItem>();

            foreach (var owner in owners)
            {
                var owned = await PlayerInventoryService.LoadOwnedAsync(owner.PlayerId);
                foreach (var item in owned.Where(item => item.IsOwned && !item.IsExpired))
                {
                    var effect = ResolveTeamEffectFromCatalogForCreateTeam(catalog, item.AssetId, item.StoreTypeId);
                    if (effect == null)
                        continue;

                    if (effects.Any(existing =>
                            CanonicalAssetIdentityService.SameAssetId(existing.AssetId, item.AssetId) &&
                            string.Equals(existing.OwnerPlayerId, owner.PlayerId, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var name = string.IsNullOrWhiteSpace(effect.DisplayName) ? item.AssetId : effect.DisplayName;
                    effects.Add(new TeamEffectCarouselItem(
                        item.AssetId,
                        $"{name} · {owner.PlayerName}",
                        effect,
                        owner.PlayerId,
                        item.ApplicationUserId));
                }
            }

            if (syncVersion != _teamEffectSyncVersion || Handler == null)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var previousSelection = selectedTeamEffectAssetId;
                var previousOwner = selectedTeamEffectOwnerPlayerId;
                var orderedEffects = effects
                    .GroupBy(item => $"{CanonicalAssetIdentityService.NormalizeForComparison(item.AssetId)}|{item.OwnerPlayerId}", StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                teamEffectItems = orderedEffects.Count == 0
                    ? new List<TeamEffectCarouselItem>()
                    : new[] { new TeamEffectCarouselItem(string.Empty, "بدون تأثير", null) }
                        .Concat(orderedEffects)
                        .ToList();

                TeamEffectCarousel.ItemsSource = teamEffectItems;
                TeamEffectSection.IsVisible = teamEffectItems.Count > 1;

                var selected = teamEffectItems.FirstOrDefault(item =>
                                   CanonicalAssetIdentityService.SameAssetId(item.AssetId, previousSelection) &&
                                   (string.IsNullOrWhiteSpace(previousOwner) ||
                                    string.Equals(item.OwnerPlayerId, previousOwner, StringComparison.OrdinalIgnoreCase)))
                               ?? teamEffectItems.FirstOrDefault(item => item.IsNone)
                               ?? teamEffectItems.FirstOrDefault();

                if (selected == null || selected.Effect == null)
                {
                    selectedTeamEffectAssetId = string.Empty;
                    selectedTeamEffectOwnerPlayerId = string.Empty;
                    selectedTeamEffectOwnerApplicationUserId = string.Empty;
                    ClearPreviewLivingTeamEffectHost();
                    IdentityEffectRenderer.Clear(PreviewTeamEffectOverlay);
                    if (selected != null && !ReferenceEquals(TeamEffectCarousel.SelectedItem, selected))
                        TeamEffectCarousel.SelectedItem = selected;
                    return;
                }

                selectedTeamEffectAssetId = selected.AssetId;
                selectedTeamEffectOwnerPlayerId = selected.OwnerPlayerId;
                selectedTeamEffectOwnerApplicationUserId = selected.OwnerApplicationUserId;
                if (!ReferenceEquals(TeamEffectCarousel.SelectedItem, selected))
                    TeamEffectCarousel.SelectedItem = selected;

                IdentityEffectRenderer.Clear(PreviewTeamEffectOverlay);
                ApplyPreviewLivingTeamEffect(selected);
            });
        }
        catch
        {
        }
        finally
        {
            _teamEffectVisualSyncRunning = false;
        }
    }

    private async Task<IReadOnlyList<(string PlayerId, string PlayerName)>> ResolveCurrentTeamEffectOwnersAsync()
    {
        var result = new List<(string PlayerId, string PlayerName)>();
        await AddPlayerFromEntryAsync(Player1Entry.Text);
        if (isTeamMode)
            await AddPlayerFromEntryAsync(Player2Entry.Text);

        if (CurrentTeam != null)
        {
            await AddPlayerFromIdAsync(CurrentTeam.Player1Id);
            if (isTeamMode)
                await AddPlayerFromIdAsync(CurrentTeam.Player2Id);
        }

        return result;

        async Task AddPlayerFromEntryAsync(string? text)
        {
            var value = text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return;

            var byId = await PlayerProfileService.GetPlayerByIdAsync(value);
            if (byId != null)
            {
                AddOwner(byId.PlayerId, byId.PlayerName);
                return;
            }

            var byName = await PlayerProfileService.GetPlayerByNameAsync(value);
            if (byName != null)
                AddOwner(byName.PlayerId, byName.PlayerName);
        }

        async Task AddPlayerFromIdAsync(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;
            var player = await PlayerProfileService.GetPlayerByIdAsync(id.Trim());
            AddOwner(id.Trim(), player?.PlayerName ?? id.Trim());
        }

        void AddOwner(string? id, string? name)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;
            var playerId = id.Trim();
            if (result.Any(item => string.Equals(item.PlayerId, playerId, StringComparison.OrdinalIgnoreCase)))
                return;
            result.Add((playerId, string.IsNullOrWhiteSpace(name) ? playerId : name.Trim()));
        }
    }

    private static CatalogAssetDisplay? ResolveTeamEffectFromCatalogForCreateTeam(
        IReadOnlyList<CatalogAssetDisplay> catalog,
        string? assetId,
        string? storeTypeId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return null;

        if (CanonicalAssetIdentityService.SameAssetId(assetId, StoreAssetCatalogService.LivingProductionDefaultEmblemAssetId) ||
            CanonicalAssetIdentityService.SameAssetId(assetId, "teameffect_living_filament_backend_probe"))
        {
            return null;
        }

        var teamTyped = StoreAssetCatalogService.Resolve(catalog, assetId, StoreProductAssetType.TeamEffect.ToString());
        if (teamTyped != null)
            return teamTyped;

        var legacy = StoreAssetCatalogService.Resolve(catalog, assetId, StoreProductAssetType.Effect.ToString());
        if (legacy == null)
            return null;

        var canonicalTypeId = StoreAssetCatalogService.CanonicalTypeId(storeTypeId);
        return legacy.AssetType == StoreProductAssetType.TeamEffect ||
               string.Equals(legacy.EquipTarget, "TeamEffect", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(legacy.EquipTarget, "Team", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(canonicalTypeId, StoreProductAssetType.Effect.ToString(), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(canonicalTypeId, StoreProductAssetType.TeamEffect.ToString(), StringComparison.OrdinalIgnoreCase)
            ? legacy
            : null;
    }

    private static void RepairCreateTeamArabicText(Element element)
    {
        if (element is Label label)
            label.Text = RepairCreateTeamArabic(label.Text);
        else if (element is Button button)
            button.Text = RepairCreateTeamArabic(button.Text);
        else if (element is Entry entry)
            entry.Placeholder = RepairCreateTeamArabic(entry.Placeholder);
        else if (element is Picker picker)
            picker.Title = RepairCreateTeamArabic(picker.Title);

        foreach (var child in element.LogicalChildren)
            RepairCreateTeamArabicText(child);
    }

    private static string RepairCreateTeamArabic(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value ?? string.Empty;

        return value
            .Replace("ط¨ط¯ظˆظ† ط®ظ„ظپظٹط©", "بدون خلفية", StringComparison.Ordinal)
            .Replace("ط¨ط¯ظˆظ† طھط£ط«ظٹط±", "بدون تأثير", StringComparison.Ordinal)
            .Replace("ط§ط³ظ… ط§ظ„ظپط±ظٹظ‚", "اسم الفريق", StringComparison.Ordinal)
            .Replace("ط§ظ„ظ„ط§ط¹ط¨ ط§ظ„ط£ظˆظ„", "اللاعب الأول", StringComparison.Ordinal)
            .Replace("ط§ظ„ظ„ط§ط¹ط¨ ط§ظ„ط«ط§ظ†ظٹ", "اللاعب الثاني", StringComparison.Ordinal)
            .Replace("ظپط±ظٹظ‚", "فريق", StringComparison.Ordinal)
            .Replace("ظپط±ط¯ظٹ", "فردي", StringComparison.Ordinal)
            .Replace("ط¥ظ†ط´ط§ط، ط§ظ„ظپط±ظٹظ‚", "إنشاء الفريق", StringComparison.Ordinal)
            .Replace("طھط­ط¯ظٹط« ط§ظ„ظپط±ظٹظ‚", "تحديث الفريق", StringComparison.Ordinal)
            .Replace("طھظ†ط¨ظٹظ‡", "تنبيه", StringComparison.Ordinal)
            .Replace("ط­ط³ظ†ط§ظ‹", "حسناً", StringComparison.Ordinal)
            .Replace("طھظ…", "تم", StringComparison.Ordinal)
            .Replace("ظ…ظ…طھط§ط²", "ممتاز", StringComparison.Ordinal)
            .Replace("ط¥ظ„ط؛ط§ط،", "إلغاء", StringComparison.Ordinal)
            .Replace("ظ†ط¹ظ…", "نعم", StringComparison.Ordinal);
    }
}
