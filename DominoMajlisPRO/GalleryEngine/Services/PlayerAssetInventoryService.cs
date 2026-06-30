using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerAssetInventoryService
{
    private static readonly HashSet<string> PlayerTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Avatar",
            "ProfileBackground",
            "Frame",
            "Effect",
            "PlayerNameEffect",
            "PlayerNameFrame",
            "Title"
        };

    public static async Task<IReadOnlyList<PlayerOwnedStoreItem>> GetInventoryForPlayerAsync(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException("PlayerId is required.", nameof(playerId));

        IReadOnlyList<PlayerOwnedStoreItem> loaded;
        try
        {
            loaded = await PlayerInventoryService.LoadOwnedAsync(playerId);
        }
        catch (Exception)
        {
            loaded = Array.Empty<PlayerOwnedStoreItem>();
        }

        var purchased = loaded.Where(IsValidOwnedPlayerAsset);
        return CreateDefaultAssets(playerId)
            .Concat(purchased)
            .GroupBy(item => $"{StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId)}\u001F{item.AssetId}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.IsEquipped).ThenByDescending(item => !Same(item.Source, "Default")).First())
            .ToList();
    }

    private static bool IsValidOwnedPlayerAsset(PlayerOwnedStoreItem? item)
    {
        if (item == null || !item.IsOwned || item.IsExpired || string.IsNullOrWhiteSpace(item.PlayerId) || string.IsNullOrWhiteSpace(item.AssetId))
            return false;

        return PlayerTypes.Contains(StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId));
    }

    public static Task<bool> AddPurchasedAssetAsync(string playerId, string assetId, string assetType, string source = "StorePurchase", string? productId = null, string? seasonId = null, string? collectionId = null)
    {
        _ = productId;
        ValidatePlayerType(assetType);
        return PlayerInventoryService.AddOwnedItemAsync(playerId, assetId, assetType, source, seasonId: seasonId, collectionId: collectionId);
    }

    public static async Task<bool> EquipAsync(string playerId, string assetId, string assetType)
    {
        ValidatePlayerType(assetType);
        var canonicalType = StoreAssetCatalogService.CanonicalTypeId(assetType);
        var defaultAvatar = AvatarService.GetById(assetId);
        if (Same(canonicalType, StoreProductAssetType.Avatar.ToString()) && defaultAvatar != null)
        {
            await PlayerProfileService.SetBuiltInAvatarAsync(playerId, defaultAvatar.Image);
            AppEvents.RaiseStoreEconomyChanged(playerId);
            AppEvents.RaisePlayerProfileChanged();
            return true;
        }

        return canonicalType is "Avatar" or "ProfileBackground" or "Frame" or "Effect" or "PlayerNameEffect" or "PlayerNameFrame"
            ? await StoreEquipService.EquipAsync(playerId, assetId)
            : await PlayerInventoryService.EquipItemAsync(playerId, assetId);
    }

    public static async Task<PlayerOwnedStoreItem?> GetEquippedAsync(string playerId, string assetType)
    {
        ValidatePlayerType(assetType);
        return (await GetInventoryForPlayerAsync(playerId)).FirstOrDefault(item =>
            item.IsEquipped && Same(StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId), assetType));
    }

    private static IEnumerable<PlayerOwnedStoreItem> CreateDefaultAssets(string playerId)
    {
        var acquiredAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        foreach (var avatar in AvatarService.GetAll())
        {
            yield return new PlayerOwnedStoreItem
            {
                InventoryItemId = $"DEFAULT-{playerId}-Avatar-{avatar.Id}",
                PlayerId = playerId,
                AssetId = avatar.Id,
                ItemId = avatar.Id,
                StoreTypeId = StoreProductAssetType.Avatar.ToString(),
                AssetType = StoreProductAssetType.Avatar.ToString(),
                PurchasedAt = acquiredAt,
                AcquiredAt = acquiredAt,
                Source = "Default",
                IsOwned = false
            };
        }
    }

    private static void ValidatePlayerType(string assetType)
    {
        if (!PlayerTypes.Contains(StoreAssetCatalogService.CanonicalTypeId(assetType)))
            throw new ArgumentException("AssetType is not a player cosmetic.", nameof(assetType));
    }

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
}
