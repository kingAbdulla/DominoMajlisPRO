namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed class PlayerOwnedStoreItem
{
    public string InventoryItemId { get; set; } = Guid.NewGuid().ToString();
    // The application user who owns this record. Added for ownership isolation.
    public string ApplicationUserId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string StoreTypeId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string? ProductId { get; set; }
    public DateTime PurchasedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsOwned { get; set; } = true;
    public bool IsEquipped { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? ExpireAt { get; set; }
    public string? SeasonId { get; set; }
    public string? CollectionId { get; set; }

    // Legacy JSON compatibility. Inventory identity is AssetId only.
    public string ItemId { get; set; } = string.Empty;
    public StoreItemType ItemType { get; set; }
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    public string SourcePurchaseId { get; set; } = string.Empty;
    public DateTime? EquippedAt { get; set; }
}
