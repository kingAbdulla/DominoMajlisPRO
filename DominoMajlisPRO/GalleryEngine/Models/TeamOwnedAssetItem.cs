namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed class TeamOwnedAssetItem
{
    public string TeamInventoryItemId { get; set; } = Guid.NewGuid().ToString();
    // The application user who added/acquired this team asset (ownership isolation)
    public string ApplicationUserId { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string TeamAssetId { get; set; } = string.Empty;
    public string TeamAssetTypeId { get; set; } = string.Empty;
    public bool IsOwned { get; set; } = true;
    public bool IsEquipped { get; set; }
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "TeamAsset";
    public string? SeasonId { get; set; }
    public string? CollectionId { get; set; }
}
