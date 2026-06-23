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

            // The preview card is visually RTL. Keep player 1 in the right slot and player 2 in the left slot.
            PreviewPlayer1.Text = isTeamMode ? player2 : player1;
            PreviewPlayer2.Text = isTeamMode ? player1 : string.Empty;
            PreviewPlayer2.IsVisible = isTeamMode;

            PreviewMode.Text = isTeamMode ? "فريق" : "فردي";
            SaveButtonText.Text = IsEditMode ? "تحديث الفريق" : "إنشاء الفريق";
        }
        catch
        {
            // Preview text sync is visual-only and must never block CreateTeamPage.
        }
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
            // Effect choice synchronization is visual-only here; save validation remains in OnSaveClicked.
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
            var value = text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return;

            var byId = await PlayerProfileService.GetPlayerByIdAsync(value);
            if (byId != null)
            {
                AddId(byId.PlayerId);
                return;
            }

            var byName = await PlayerProfileService.GetPlayerByNameAsync(value);
            if (byName != null)
                AddId(byName.PlayerId);
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
        {
            var teamTyped = StoreAssetCatalogService.Resolve(catalog, assetId, StoreProductAssetType.TeamEffect.ToString());
            if (teamTyped != null)
                return teamTyped;
        }

        var legacy = StoreAssetCatalogService.Resolve(catalog, assetId, StoreProductAssetType.Effect.ToString());
        if (legacy == null)
            return null;

        return legacy.AssetType == StoreProductAssetType.TeamEffect ||
               string.Equals(legacy.EquipTarget, "TeamEffect", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(legacy.EquipTarget, "Team", StringComparison.OrdinalIgnoreCase)
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
