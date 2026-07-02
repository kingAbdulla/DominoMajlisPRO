using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Components.StoreSections;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed record StoreAcquireResult(bool IsOwned, bool IsEquipped, bool WasAdded, bool VisualApplied);

public static class StoreEquipService
{
    public static async Task<StoreAcquireResult> AcquireFreeAsync(string playerId, string assetId, string storeTypeId, string? seasonId = null, string? collectionId = null)
    {
        ValidateIdentity(playerId, assetId, storeTypeId);
        var wasOwned = await PlayerInventoryService.IsOwnedAsync(playerId, assetId);
        var wasAdded = wasOwned || await PlayerInventoryService.AddOwnedItemWithoutNotificationAsync(playerId, assetId, storeTypeId, "FreeAcquire", seasonId: seasonId, collectionId: collectionId);

        if (!wasAdded)
            return new StoreAcquireResult(false, false, false, false);

        var equipCapable = IsEquipCapable(storeTypeId);
        var equipped = false;
        var visualApplied = false;
        if (equipCapable)
        {
            equipped = await PlayerInventoryService.EquipItemWithoutNotificationAsync(playerId, assetId);
            if (equipped)
                visualApplied = await ApplyVisualAsync(playerId, assetId, storeTypeId);
        }

        AppEvents.RaiseStoreEconomyChanged(playerId);
        RaiseVisualChanged(playerId, visualApplied, storeTypeId);

        return new StoreAcquireResult(true, equipped, !wasOwned, visualApplied);
    }

    public static async Task<bool> EquipAsync(string playerId, string assetId)
    {
        ValidateIdentity(playerId, assetId);
        var owned = (await PlayerInventoryService.GetInventoryForPlayerAsync(playerId)).FirstOrDefault(item =>
            item.IsOwned && !item.IsExpired && SameId(item.AssetId, assetId));
        if (owned == null || !IsEquipCapable(owned.StoreTypeId))
            return false;

        var equipped = await PlayerInventoryService.EquipItemWithoutNotificationAsync(playerId, assetId);
        if (!equipped)
            return false;

        var visualApplied = await ApplyVisualAsync(playerId, assetId, owned.StoreTypeId);
        AppEvents.RaiseStoreEconomyChanged(playerId);
        RaiseVisualChanged(playerId, visualApplied, owned.StoreTypeId);
        return true;
    }

    public static Task<bool> UnequipAsync(string playerId, string assetId) => PlayerInventoryService.UnequipItemAsync(playerId, assetId);

    public static async Task<PlayerOwnedStoreItem?> GetEquippedAsync(string playerId, string storeTypeId)
    {
        ValidateIdentity(playerId, "lookup", storeTypeId);
        return (await PlayerInventoryService.GetInventoryForPlayerAsync(playerId)).FirstOrDefault(item =>
            item.IsOwned && !item.IsExpired && item.IsEquipped && SameId(item.StoreTypeId, storeTypeId));
    }

    public static async Task<bool> IsEquippedAsync(string playerId, string assetId)
    {
        ValidateIdentity(playerId, assetId);
        return (await PlayerInventoryService.GetInventoryForPlayerAsync(playerId)).Any(item =>
            item.IsOwned && !item.IsExpired && item.IsEquipped && SameId(item.AssetId, assetId));
    }

    public static bool IsEquipCapable(string? storeTypeId) =>
        SameId(storeTypeId, StoreTypeRegistry.Avatar.TypeId) ||
        SameId(storeTypeId, StoreProductAssetType.ProfileBackground.ToString()) ||
        SameId(storeTypeId, StoreProductAssetType.Frame.ToString()) ||
        SameId(storeTypeId, StoreProductAssetType.Effect.ToString()) ||
        SameId(storeTypeId, StoreProductAssetType.PlayerNameEffect.ToString()) ||
        SameId(storeTypeId, StoreProductAssetType.PlayerNameFrame.ToString()) ||
        SameId(storeTypeId, StoreProductAssetType.TeamNameEffect.ToString()) ||
        SameId(storeTypeId, StoreProductAssetType.TeamNameFrame.ToString()) ||
        SameId(storeTypeId, StoreProductAssetType.Title.ToString());

    private static async Task<bool> ApplyVisualAsync(string playerId, string assetId, string storeTypeId)
    {
        if (!SameId(storeTypeId, StoreTypeRegistry.Avatar.TypeId))
            return IsEquipCapable(storeTypeId);

        var avatar = (await AvatarsAdminService.LoadPublishedAsync()).FirstOrDefault(item => SameId(item.Id, assetId));
        var player = await PlayerProfileService.GetPlayerByIdAsync(playerId);
        if (avatar == null || player == null)
            return false;

        var imagePath = string.IsNullOrWhiteSpace(avatar.ThumbnailPath) ? avatar.ImagePath : avatar.ThumbnailPath;
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;
        await PlayerProfileService.SetBuiltInAvatarAsync(playerId, imagePath);
        return true;
    }

    private static void RaiseVisualChanged(string playerId, bool visualApplied, string? storeTypeId)
    {
        if (!visualApplied)
            return;

        AppEvents.RaiseStoreProgressChanged(playerId);
        AppEvents.RaisePlayerProfileChanged();

        if (SameId(storeTypeId, StoreProductAssetType.TeamNameEffect.ToString()) ||
            SameId(storeTypeId, StoreProductAssetType.TeamNameFrame.ToString()))
        {
            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var team = await TeamProfileService.GetTeamByPlayerIdAsync(playerId);
                if (!string.IsNullOrWhiteSpace(team?.TeamId))
                    AppEvents.RaiseTeamAssetsChanged(team.TeamId);
                AppEvents.RaiseTeamsChanged();
            });
        }
    }

    private static bool SameId(string? left, string? right) => CanonicalAssetIdentityService.SameAssetId(left, right);

    private static void ValidateIdentity(string playerId, string assetId, string? storeTypeId = null)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException("PlayerId is required.", nameof(playerId));
        if (string.IsNullOrWhiteSpace(assetId))
            throw new ArgumentException("AssetId is required.", nameof(assetId));
        if (storeTypeId != null && string.IsNullOrWhiteSpace(storeTypeId))
            throw new ArgumentException("StoreTypeId is required.", nameof(storeTypeId));
    }
}
