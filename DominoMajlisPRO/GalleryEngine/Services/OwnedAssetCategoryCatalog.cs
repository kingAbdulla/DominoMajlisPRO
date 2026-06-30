namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed record OwnedAssetCategory(string Group, string DisplayName, string AssetType, bool IsTeamAsset);

public static class OwnedAssetCategoryCatalog
{
    public static IReadOnlyList<OwnedAssetCategory> All { get; } =
        new[]
        {
            new OwnedAssetCategory("PLAYER ASSETS", "Avatars", "Avatar", false),
            new OwnedAssetCategory("PLAYER ASSETS", "Profile Backgrounds", "ProfileBackground", false),
            new OwnedAssetCategory("PLAYER ASSETS", "Frames", "Frame", false),
            new OwnedAssetCategory("PLAYER ASSETS", "Effects", "Effect", false),
            new OwnedAssetCategory("PLAYER ASSETS", "Name Effects", "PlayerNameEffect", false),
            new OwnedAssetCategory("PLAYER ASSETS", "Name Frames", "PlayerNameFrame", false),
            new OwnedAssetCategory("PLAYER ASSETS", "Titles", "Title", false),
            new OwnedAssetCategory("GROUP ASSETS", "Emblems", "Emblem", true),
            new OwnedAssetCategory("GROUP ASSETS", "Colors", "TeamColor", true),
            new OwnedAssetCategory("GROUP ASSETS", "Emblem Backgrounds", "EmblemBackground", true)
        };

    public static OwnedAssetCategory Get(string assetType) =>
        All.First(category => string.Equals(category.AssetType, assetType, StringComparison.OrdinalIgnoreCase));
}
