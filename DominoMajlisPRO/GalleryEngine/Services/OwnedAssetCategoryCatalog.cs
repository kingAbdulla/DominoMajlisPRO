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
            new OwnedAssetCategory("PLAYER ASSETS", "Player Name Effects", "PlayerNameEffect", false),
            new OwnedAssetCategory("PLAYER ASSETS", "Player Name Frames", "PlayerNameFrame", false),
            new OwnedAssetCategory("PLAYER ASSETS", "Titles", "Title", false),
            new OwnedAssetCategory("TEAM ASSETS", "Emblems", "Emblem", true),
            new OwnedAssetCategory("TEAM ASSETS", "Team Colors", "TeamColor", true),
            new OwnedAssetCategory("TEAM ASSETS", "Emblem Backgrounds", "EmblemBackground", true),
            new OwnedAssetCategory("TEAM ASSETS", "Team Name Effects", "TeamNameEffect", true),
            new OwnedAssetCategory("TEAM ASSETS", "Team Name Frames", "TeamNameFrame", true)
        };

    public static OwnedAssetCategory Get(string assetType) =>
        All.First(category => string.Equals(category.AssetType, assetType, StringComparison.OrdinalIgnoreCase));
}
