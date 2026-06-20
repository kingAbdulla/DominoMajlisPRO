namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed record TeamAssetPayloadModel
{
    public required string TeamAssetId { get; init; }
    public required string TeamAssetTypeId { get; init; }
    public string? ArabicDisplayName { get; init; }
    public string? EnglishDisplayName { get; init; }
    public string? ImagePath { get; init; }
    public string? ColorHex { get; init; }
    public string? BackgroundImagePath { get; init; }
    public string? BackgroundColorHex { get; init; }
}
