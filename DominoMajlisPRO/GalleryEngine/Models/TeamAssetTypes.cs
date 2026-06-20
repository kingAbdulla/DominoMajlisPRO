namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed record TeamAssetTypeDefinition(
    string TeamAssetTypeId,
    string ArabicName,
    string EnglishName);

public static class TeamAssetTypes
{
    public static readonly TeamAssetTypeDefinition Emblem =
        new("TeamEmblem", "شعار الفريق", "Team Emblem");

    public static readonly TeamAssetTypeDefinition EmblemBackground =
        new("EmblemBackground", "خلفية الشعار", "Emblem Background");

    public static readonly TeamAssetTypeDefinition Banner =
        new("TeamBanner", "راية الفريق", "Team Banner");

    public static readonly TeamAssetTypeDefinition Effect =
        new("TeamEffect", "مؤثر الفريق", "Team Effect");

    public static IReadOnlyList<TeamAssetTypeDefinition> All { get; } =
    [
        Emblem,
        EmblemBackground,
        Banner,
        Effect
    ];
}
