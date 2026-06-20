namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed class TeamIdentityModel
{
    public string TeamId { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public string? EmblemAssetId { get; init; }
    public string EmblemImagePath { get; init; } = string.Empty;
    public string? EmblemBackgroundAssetId { get; init; }
    public string EmblemBackgroundSource { get; init; } = string.Empty;
    public string TeamColorHex { get; init; } = string.Empty;
    public bool HasCustomEmblem { get; init; }
    public bool HasCustomEmblemBackground { get; init; }
    public bool HasTeamColor { get; init; }
    public DateTime ResolvedAt { get; init; } = DateTime.UtcNow;
}
