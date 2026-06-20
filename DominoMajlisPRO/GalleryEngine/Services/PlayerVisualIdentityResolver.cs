using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerVisualIdentityResolver
{
    public static async Task<PlayerVisualIdentity> ResolveAsync(
        string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return Empty(string.Empty);

        // Ensure we prefer PlayerId lookups; playerId may be a name in legacy records, so attempt by id first then fallback to name.
        var players = await PlayerProfileService.LoadPlayersAsync();
        string resolvedPlayerId = playerId.Trim();
        var matchById = players.FirstOrDefault(p => string.Equals(p.PlayerId, resolvedPlayerId, StringComparison.OrdinalIgnoreCase));
        if (matchById == null)
        {
            // fallback to name normalization for legacy data
            var byName = players.FirstOrDefault(p => PlayerIdentityService.NormalizePlayerName(p.PlayerName) == PlayerIdentityService.NormalizePlayerName(resolvedPlayerId));
            if (byName != null)
                resolvedPlayerId = byName.PlayerId;
        }

        var catalogTask = StoreAssetCatalogService.LoadAsync();
        var session = await ApplicationUserService.EnsureCurrentSessionAsync();
        var appUserId = session.ApplicationUserId ?? string.Empty;
        var inventoryTask =
            PlayerAssetInventoryService.GetInventoryForPlayerAsync(resolvedPlayerId);
        await Task.WhenAll(catalogTask, inventoryTask);

        var equipped = inventoryTask.Result
            .Where(item =>
                item.IsOwned &&
                !item.IsExpired &&
                item.IsEquipped)
            .ToList();
        var catalog = catalogTask.Result;
        return new PlayerVisualIdentity(
            resolvedPlayerId,
            Resolve(equipped, catalog, "Avatar"),
            Resolve(equipped, catalog, "ProfileBackground"),
            Resolve(equipped, catalog, "Frame"),
            Resolve(equipped, catalog, "Effect"),
            Resolve(equipped, catalog, "Title"));
    }

    public static async Task<IReadOnlyDictionary<string, PlayerVisualIdentity>>
        ResolveManyAsync(IEnumerable<string?> playerIds)
    {
        var ids = playerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var identities = await Task.WhenAll(ids.Select(ResolveAsync));
        return identities.ToDictionary(
            item => item.PlayerId,
            StringComparer.OrdinalIgnoreCase);
    }

    private static CatalogAssetDisplay? Resolve(
        IReadOnlyList<PlayerOwnedStoreItem> inventory,
        IReadOnlyList<CatalogAssetDisplay> catalog,
        string assetType)
    {
        var item = inventory.FirstOrDefault(candidate =>
            string.Equals(
                StoreAssetCatalogService.CanonicalTypeId(
                    candidate.StoreTypeId),
                assetType,
                StringComparison.OrdinalIgnoreCase));
        return item == null
            ? null
            : StoreAssetCatalogService.Resolve(
                catalog,
                item.AssetId,
                assetType);
    }

    private static PlayerVisualIdentity Empty(string playerId) =>
        new(playerId, null, null, null, null, null);
}
