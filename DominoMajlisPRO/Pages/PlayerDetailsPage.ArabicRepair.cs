using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class PlayerDetailsPage
{
    bool _arabicRepairTimerStarted;
    int _avatarEffectRepairVersion;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        CenterNameSurfaceOnly();
        _ = ApplyEquippedEffectAroundAvatarAsync();

        if (Handler == null || _arabicRepairTimerStarted)
            return;

        _arabicRepairTimerStarted = true;
        int runs = 0;

        Dispatcher.StartTimer(
            TimeSpan.FromMilliseconds(350),
            () =>
            {
                runs++;
                RepairStaticArabicUiText();
                RepairAllVisibleText(this);
                CenterNameSurfaceOnly();
                _ = ApplyEquippedEffectAroundAvatarAsync();
                return Handler != null && runs < 12;
            });
    }

    async Task ApplyEquippedEffectAroundAvatarAsync()
    {
        var version = Interlocked.Increment(ref _avatarEffectRepairVersion);
        try
        {
            var effect = await ResolveEquippedPlayerEffectDirectAsync(_playerId);
            if (version != _avatarEffectRepairVersion || Handler == null)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AvatarEffectOverlay.ZIndex = Math.Max(PlayerAvatarImage.ZIndex + 2, 5);
                AvatarEffectOverlay.WidthRequest = AvatarFrame.WidthRequest > 0 ? AvatarFrame.WidthRequest : 155;
                AvatarEffectOverlay.HeightRequest = AvatarFrame.HeightRequest > 0 ? AvatarFrame.HeightRequest : 155;
                AvatarEffectOverlay.HorizontalOptions = LayoutOptions.Center;
                AvatarEffectOverlay.VerticalOptions = LayoutOptions.Center;
                PlayerEffectEngine.Apply(AvatarEffectOverlay, effect, 1.18);
            });
        }
        catch
        {
        }
    }

    static async Task<CatalogAssetDisplay?> ResolveEquippedPlayerEffectDirectAsync(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return null;

        var inventory = await PlayerInventoryService.GetInventoryForPlayerAsync(playerId.Trim());
        var equipped = inventory
            .Where(item => item.IsOwned && !item.IsExpired && item.IsEquipped)
            .ToList();

        if (equipped.Count == 0)
            return null;

        var catalog = await StoreAssetCatalogService.LoadAsync();

        foreach (var item in equipped)
        {
            var canonical = StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId);
            if (!string.Equals(canonical, "Effect", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(canonical, "PlayerEffect", StringComparison.OrdinalIgnoreCase))
                continue;

            var effect = StoreAssetCatalogService.Resolve(catalog, item.AssetId, "Effect")
                         ?? StoreAssetCatalogService.Resolve(catalog, item.AssetId, canonical)
                         ?? catalog.FirstOrDefault(asset =>
                             CanonicalAssetIdentityService.SameAssetId(asset.AssetId, item.AssetId));

            if (effect != null)
                return effect;
        }

        return null;
    }

    void CenterNameSurfaceOnly()
    {
        try
        {
            PlayerNameLabel.HorizontalOptions = LayoutOptions.Center;
            PlayerNameLabel.HorizontalTextAlignment = TextAlignment.Center;
            PlayerNameLabel.Margin = new Thickness(0, -2, 0, 0);

            if (PlayerNameLabel.Parent is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is RuntimePlayerNameSurfaceView surface)
                    {
                        surface.HorizontalOptions = LayoutOptions.Center;
                        surface.VerticalOptions = LayoutOptions.Center;
                        surface.HeightRequest = 26;
                        surface.MaximumWidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 170;
                        surface.Margin = new Thickness(0, -2, 0, 0);
                    }
                }
            }
        }
        catch
        {
        }
    }

    void RepairStaticArabicUiText()
    {
        if (currentPlayer == null)
            return;

        var rank = PlayerRankService.Calculate(currentPlayer.PlayerXP);

        if (IsBrokenArabicText(RankProgressLabel.Text))
        {
            RankProgressLabel.Text =
                $"المتبقي للرتبة التالية: {rank.RemainingXP} XP";
        }

        IdentityLastUpdateLabel.Text =
            $"آخر تحديث للهوية: {currentPlayer.LastUpdatedAt:yyyy/MM/dd HH:mm}";

        if (IsBrokenArabicText(CurrentTeamsLabel.Text))
            CurrentTeamsLabel.Text = "لا يوجد";

        LastRankHistoryLabel.Text =
            CleanHistoryValue(GetLastHistoryValue(currentPlayer.RankHistory));

        LastXPHistoryLabel.Text =
            CleanHistoryValue(GetLastHistoryValue(currentPlayer.XPHistory));

        LastAchievementHistoryLabel.Text =
            CleanHistoryValue(GetLastHistoryValue(currentPlayer.AchievementHistory));
    }

    static void RepairAllVisibleText(Element element)
    {
        if (element is Label label)
        {
            label.Text = RepairKnownBrokenText(label.Text);
        }
        else if (element is Button button)
        {
            button.Text = RepairKnownBrokenText(button.Text);
        }

        foreach (var child in element.LogicalChildren)
            RepairAllVisibleText(child);
    }

    static string RepairKnownBrokenText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value ?? string.Empty;

        var text = value.Trim();

        return text switch
        {
            "âœ“" => "✓",
            "ًں”’" => "🔒",
            "?��?" => "لا يوجد",
            "?�€�?" => "لا يوجد",
            _ => text
        };
    }

    static string CleanHistoryValue(string? value)
    {
        if (IsBrokenArabicText(value) || string.IsNullOrWhiteSpace(value))
            return "لا يوجد";

        return value.Trim();
    }

    static bool IsBrokenArabicText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Contains('�') ||
               value.Contains("?�", StringComparison.Ordinal) ||
               value.Contains("â", StringComparison.Ordinal) ||
               value.Contains("Ã", StringComparison.Ordinal) ||
               value.Contains("ط", StringComparison.Ordinal) ||
               value.Contains("ظ", StringComparison.Ordinal);
    }
}
