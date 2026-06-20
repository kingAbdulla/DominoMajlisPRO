using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

internal static class StoreInventoryRouteResolver
{
    public static async Task<string> ResolveStoreTypeIdAsync(
        string assetId,
        string? declaredStoreTypeId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return "Unsupported";

        if (StoreProductAssetTypeCatalog.TryResolve(declaredStoreTypeId, out var canonicalType))
            return canonicalType.ToString();

        var teamPayload = TeamAssetPayloadCatalog.Resolve(assetId);
        if (teamPayload != null)
            return teamPayload.TeamAssetTypeId;

        var avatarsTask = StoreAssetQueryService.LoadAvatarsAsync();
        var backgroundsTask = StoreAssetQueryService.LoadBackgroundsAsync();
        await Task.WhenAll(avatarsTask, backgroundsTask);

        if (avatarsTask.Result.Any(item => SameId(item.Id, assetId)))
            return StoreProductAssetType.Avatar.ToString();

        if (backgroundsTask.Result.Any(item => SameId(item.Id, assetId)))
            return StoreProductAssetType.ProfileBackground.ToString();

        return "Unsupported";
    }

    private static bool SameId(string? left, string? right) =>
        string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
