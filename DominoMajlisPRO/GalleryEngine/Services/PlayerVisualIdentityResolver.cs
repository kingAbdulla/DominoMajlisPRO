using System.Collections.Concurrent;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerVisualIdentityResolver
{
    private static readonly ConcurrentDictionary<string, Task<PlayerVisualIdentity>> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    static PlayerVisualIdentityResolver()
    {
        AppEvents.PlayerProfileChanged += ClearCache;
        AppEvents.StoreEconomyChanged += _ => ClearCache();
        AppEvents.StoreProgressChanged += _ => ClearCache();
        AppEvents.TeamAssetsChanged += _ => ClearCache();
    }

    public static async Task<PlayerVisualIdentity> ResolveAsync(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return Empty(string.Empty);

        var cacheKey = playerId.Trim();
        var resolveTask = Cache.GetOrAdd(cacheKey, ResolveUncachedAsync);
        try
        {
            return await resolveTask;
        }
        catch
        {
            Cache.TryRemove(cacheKey, out _);
            throw;
        }
    }

    public static void ClearCache() => Cache.Clear();

    private static async Task<PlayerVisualIdentity> ResolveUncachedAsync(string playerId)
    {

        var players = await PlayerProfileService.LoadPlayersAsync();
        string resolvedPlayerId = playerId.Trim();
        var matchById = players.FirstOrDefault(p => string.Equals(p.PlayerId, resolvedPlayerId, StringComparison.OrdinalIgnoreCase));
        if (matchById == null)
        {
            var byName = players.FirstOrDefault(p => PlayerIdentityService.NormalizePlayerName(p.PlayerName) == PlayerIdentityService.NormalizePlayerName(resolvedPlayerId));
            if (byName != null)
                resolvedPlayerId = byName.PlayerId;
        }

        var catalogTask = StoreAssetCatalogService.LoadAsync();
        var session = await ApplicationUserService.EnsureCurrentSessionAsync();
        var appUserId = session.ApplicationUserId ?? string.Empty;
        _ = appUserId;
        var inventoryTask = PlayerAssetInventoryService.GetInventoryForPlayerAsync(resolvedPlayerId);
        await Task.WhenAll(catalogTask, inventoryTask);

        var equipped = inventoryTask.Result.Where(item => item.IsOwned && !item.IsExpired && item.IsEquipped).ToList();
        var catalog = catalogTask.Result;
        return new PlayerVisualIdentity(
            resolvedPlayerId,
            Resolve(equipped, catalog, "Avatar"),
            Resolve(equipped, catalog, "ProfileBackground"),
            Resolve(equipped, catalog, "Frame"),
            Resolve(equipped, catalog, "Effect"),
            Resolve(equipped, catalog, "PlayerNameEffect"),
            Resolve(equipped, catalog, "PlayerNameFrame"),
            Resolve(equipped, catalog, "Title"));
    }

    public static async Task<IReadOnlyDictionary<string, PlayerVisualIdentity>> ResolveManyAsync(IEnumerable<string?> playerIds)
    {
        var ids = playerIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var identities = await Task.WhenAll(ids.Select(ResolveAsync));
        return identities.ToDictionary(item => item.PlayerId, StringComparer.OrdinalIgnoreCase);
    }

    private static CatalogAssetDisplay? Resolve(IReadOnlyList<PlayerOwnedStoreItem> inventory, IReadOnlyList<CatalogAssetDisplay> catalog, string assetType)
    {
        var strictItem = inventory.FirstOrDefault(candidate =>
            string.Equals(StoreAssetCatalogService.CanonicalTypeId(candidate.StoreTypeId), assetType, StringComparison.OrdinalIgnoreCase) &&
            StoreAssetCatalogService.Resolve(catalog, candidate.AssetId, assetType) != null);

        if (strictItem != null)
            return StoreAssetCatalogService.Resolve(catalog, strictItem.AssetId, assetType);

        var fallbackItem = inventory.FirstOrDefault(candidate => StoreAssetCatalogService.Resolve(catalog, candidate.AssetId, assetType) != null);
        return fallbackItem == null ? null : StoreAssetCatalogService.Resolve(catalog, fallbackItem.AssetId, assetType);
    }

    private static PlayerVisualIdentity Empty(string playerId) => new(playerId, null, null, null, null, null, null, null);
}
