namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public enum InventoryAuditStatus
{
    Valid,
    MissingAssetType,
    MissingAsset,
    InvalidOwnerScope,
    DuplicateAssetId,
    UnsupportedPayload
}

public enum InventoryAuditProductSource
{
    NewArrival,
    LimitedOffer
}

public sealed record RegisteredStoreAsset(
    string AssetId,
    StoreProductAssetType AssetType,
    StoreProductOwnerScope OwnerScope,
    string DisplayName,
    string ImagePath,
    string ColorHex);

public sealed record InventoryAuditItem(
    InventoryAuditProductSource Source,
    string ProductName,
    string ProductId,
    string AssetId,
    string CurrentAssetType,
    string OwnerScope,
    string ImagePath,
    string ColorHex,
    InventoryAuditStatus Status,
    string StatusText,
    IReadOnlyList<RegisteredStoreAsset> SafeMatches)
{
    public bool IsHealthy => Status == InventoryAuditStatus.Valid;
    public bool CanRepairSafely =>
        !IsHealthy &&
        Status != InventoryAuditStatus.DuplicateAssetId &&
        SafeMatches.Count == 1;
}

public sealed record CatalogHealthSummary(
    int Products,
    int Healthy,
    int Malformed,
    int DuplicateAssetIds,
    int MissingAssets,
    int MissingAssetTypes)
{
    public double HealthyPercent => Products == 0 ? 100 : Healthy * 100d / Products;
}

public sealed record InventoryAuditReport(
    IReadOnlyList<InventoryAuditItem> Items,
    IReadOnlyList<RegisteredStoreAsset> Catalog,
    CatalogHealthSummary Summary);
