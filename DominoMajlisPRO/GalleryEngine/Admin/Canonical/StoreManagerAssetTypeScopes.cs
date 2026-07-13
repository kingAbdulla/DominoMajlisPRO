using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Canonical;

/// <summary>
/// Central mapping between Developer Store Manager sections and the exact
/// canonical asset types they are allowed to manage. This prevents a section
/// from loading or editing products owned by another identity surface.
/// </summary>
public static class StoreManagerAssetTypeScopes
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Scopes =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["name-effects"] = Set(
                StoreProductAssetType.PlayerNameEffect,
                StoreProductAssetType.TeamNameEffect,
                StoreProductAssetType.PlayerNameFrame,
                StoreProductAssetType.TeamNameFrame),

            ["typography"] = Set(
                StoreProductAssetType.PlayerNameEffect,
                StoreProductAssetType.TeamNameEffect,
                StoreProductAssetType.PlayerNameFrame,
                StoreProductAssetType.TeamNameFrame),

            ["effects"] = Set(
                StoreProductAssetType.Effect,
                StoreProductAssetType.TeamEffect),

            ["frames"] = Set(StoreProductAssetType.Frame),

            ["emblems"] = Set(StoreProductAssetType.Emblem),

            ["emblem-backgrounds"] = Set(StoreProductAssetType.EmblemBackground),
            ["team-colors"] = Set(StoreProductAssetType.TeamColor),
            ["avatars"] = Set(StoreProductAssetType.Avatar),
            ["backgrounds"] = Set(StoreProductAssetType.ProfileBackground),
            ["titles"] = Set(StoreProductAssetType.Title),
            ["bundles"] = Set(StoreProductAssetType.Bundle)
        };

    public static IReadOnlySet<string> ForSection(string? sectionId)
    {
        var key = sectionId?.Trim();
        return key != null && Scopes.TryGetValue(key, out var scope)
            ? scope
            : Empty;
    }

    public static bool Contains(string? sectionId, string? storeTypeId) =>
        !string.IsNullOrWhiteSpace(storeTypeId) &&
        ForSection(sectionId).Contains(storeTypeId.Trim());

    private static IReadOnlySet<string> Set(params StoreProductAssetType[] types) =>
        new HashSet<string>(
            types.Select(type => type.ToString()),
            StringComparer.Ordinal);

    private static IReadOnlySet<string> Empty { get; } =
        new HashSet<string>(StringComparer.Ordinal);
}
