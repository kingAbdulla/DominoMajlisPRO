using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerNameTypographyResolver
{
    public static async Task<NameTypographyIdentity> ResolveAsync(string? playerId)
    {
        var normalizedPlayerId = playerId?.Trim() ?? string.Empty;
        if (normalizedPlayerId.Length == 0)
            return new NameTypographyIdentity(string.Empty, null, null);

        var catalogTask = StoreAssetCatalogService.LoadAsync();
        var inventoryTask =
            PlayerAssetInventoryService.GetInventoryForPlayerAsync(
                normalizedPlayerId);
        await Task.WhenAll(catalogTask, inventoryTask);

        var equipped = inventoryTask.Result
            .Where(item => item.IsOwned && !item.IsExpired && item.IsEquipped)
            .ToList();
        var catalog = catalogTask.Result;
        return new NameTypographyIdentity(
            normalizedPlayerId,
            Resolve(
                equipped,
                catalog,
                StoreProductAssetType.PlayerNameEffect),
            Resolve(
                equipped,
                catalog,
                StoreProductAssetType.PlayerNameFrame));
    }

    public static async Task<IReadOnlyDictionary<string, NameTypographyIdentity>>
        ResolveManyAsync(IEnumerable<string?> playerIds)
    {
        var ids = playerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var identities = await Task.WhenAll(ids.Select(ResolveAsync));
        return identities.ToDictionary(
            item => item.OwnerId,
            StringComparer.OrdinalIgnoreCase);
    }

    private static CatalogAssetDisplay? Resolve(
        IReadOnlyList<Models.PlayerOwnedStoreItem> inventory,
        IReadOnlyList<CatalogAssetDisplay> catalog,
        StoreProductAssetType type)
    {
        var item = inventory.FirstOrDefault(candidate =>
            string.Equals(
                StoreAssetCatalogService.CanonicalTypeId(candidate.StoreTypeId),
                type.ToString(),
                StringComparison.OrdinalIgnoreCase));
        return item == null
            ? null
            : StoreAssetCatalogService.Resolve(catalog, item.AssetId, type.ToString());
    }
}

public static class TeamNameTypographyResolver
{
    public static async Task<NameTypographyIdentity> ResolveAsync(string? teamId)
    {
        var normalizedTeamId = teamId?.Trim() ?? string.Empty;
        if (normalizedTeamId.Length == 0)
            return new NameTypographyIdentity(string.Empty, null, null);

        var catalogTask = StoreAssetCatalogService.LoadAsync();
        var effectTask = TeamAssetInventoryService.GetEquippedAsync(
            normalizedTeamId,
            StoreProductAssetType.TeamNameEffect.ToString());
        var frameTask = TeamAssetInventoryService.GetEquippedAsync(
            normalizedTeamId,
            StoreProductAssetType.TeamNameFrame.ToString());
        await Task.WhenAll(catalogTask, effectTask, frameTask);

        var catalog = catalogTask.Result;
        return new NameTypographyIdentity(
            normalizedTeamId,
            Resolve(
                catalog,
                effectTask.Result,
                StoreProductAssetType.TeamNameEffect),
            Resolve(
                catalog,
                frameTask.Result,
                StoreProductAssetType.TeamNameFrame));
    }

    public static async Task<IReadOnlyDictionary<string, NameTypographyIdentity>>
        ResolveManyAsync(IEnumerable<string?> teamIds)
    {
        var ids = teamIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var identities = await Task.WhenAll(ids.Select(ResolveAsync));
        return identities.ToDictionary(
            item => item.OwnerId,
            StringComparer.OrdinalIgnoreCase);
    }

    private static CatalogAssetDisplay? Resolve(
        IReadOnlyList<CatalogAssetDisplay> catalog,
        TeamOwnedAssetItem? item,
        StoreProductAssetType type) =>
        item == null
            ? null
            : StoreAssetCatalogService.Resolve(
                catalog,
                item.TeamAssetId,
                type.ToString());
}
