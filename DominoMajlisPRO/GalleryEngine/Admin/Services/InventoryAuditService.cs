using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class InventoryAuditService
{
    public static async Task<InventoryAuditReport> ScanAsync()
    {
        var catalog = await LoadCatalogAsync();
        var arrivalsTask = StoreCmsJsonRepository.LoadListAsync<NewArrivalRecord>(
            NewArrivalsAdminService.GetStoragePath());
        var offersTask = StoreCmsJsonRepository.LoadListAsync<LimitedOfferRecord>(
            LimitedOffersAdminService.GetStoragePath());
        await Task.WhenAll(arrivalsTask, offersTask);

        var products = new List<ProductSnapshot>();
        products.AddRange(arrivalsTask.Result
            .Where(item => item.Status == NewArrivalStatus.Published)
            .Select(item => new ProductSnapshot(
                InventoryAuditProductSource.NewArrival,
                item.Title,
                ProductId(item.ProductId, item.Id),
                item.AssetId,
                item.StoreTypeId,
                item.OwnerScope,
                item.ImagePath,
                item.ColorHex)));
        products.AddRange(offersTask.Result
            .Where(item => item.Status == LimitedOfferStatus.Published)
            .Select(item => new ProductSnapshot(
                InventoryAuditProductSource.LimitedOffer,
                item.Title,
                ProductId(item.ProductId, item.Id),
                item.AssetId,
                item.StoreTypeId,
                item.OwnerScope,
                item.ImagePath,
                item.ColorHex)));

        var duplicateIds = products
            .Where(item => !string.IsNullOrWhiteSpace(item.AssetId))
            .GroupBy(item => item.AssetId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = products
            .Select(item => Audit(item, catalog, duplicateIds))
            .OrderBy(item => item.IsHealthy)
            .ThenBy(item => item.ProductName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var summary = new CatalogHealthSummary(
            items.Count,
            items.Count(item => item.IsHealthy),
            items.Count(item => !item.IsHealthy),
            items.Count(item => item.Status == InventoryAuditStatus.DuplicateAssetId),
            items.Count(item => item.Status == InventoryAuditStatus.MissingAsset),
            items.Count(item => item.Status == InventoryAuditStatus.MissingAssetType));
        return new InventoryAuditReport(items, catalog, summary);
    }

    public static async Task<int> RepairAllSafeItemsAsync()
    {
        var report = await ScanAsync();
        var safe = report.Items.Where(item => item.CanRepairSafely).ToList();
        var repaired = 0;
        foreach (var item in safe)
        {
            var match = item.SafeMatches[0];
            if (await SaveAsync(item, match.AssetType, match.AssetId, match.OwnerScope))
                repaired++;
        }
        return repaired;
    }

    public static async Task<bool> SaveAsync(
        InventoryAuditItem item,
        StoreProductAssetType assetType,
        string assetId,
        StoreProductOwnerScope ownerScope)
    {
        var catalog = await LoadCatalogAsync();
        var selected = catalog
            .Where(asset => Same(asset.AssetId, assetId) && asset.AssetType == assetType)
            .ToList();
        if (selected.Count != 1)
            throw new InvalidOperationException("Select one registered AssetId that belongs to the chosen Asset Type.");
        if (selected[0].OwnerScope != ownerScope)
            throw new InvalidOperationException("OwnerScope must match the selected registered asset.");

        if (item.Source == InventoryAuditProductSource.NewArrival)
        {
            var records = await StoreCmsJsonRepository.LoadListAsync<NewArrivalRecord>(
                NewArrivalsAdminService.GetStoragePath());
            var record = records.FirstOrDefault(candidate =>
                candidate.Status == NewArrivalStatus.Published &&
                Same(ProductId(candidate.ProductId, candidate.Id), item.ProductId));
            if (record == null) return false;
            Apply(record, selected[0]);
            await StoreCmsJsonRepository.SaveListAsync(NewArrivalsAdminService.GetStoragePath(), records);
            NewArrivalsAdminService.NotifyPublishedChanged();
            return true;
        }

        var offers = await StoreCmsJsonRepository.LoadListAsync<LimitedOfferRecord>(
            LimitedOffersAdminService.GetStoragePath());
        var offer = offers.FirstOrDefault(candidate =>
            candidate.Status == LimitedOfferStatus.Published &&
            Same(ProductId(candidate.ProductId, candidate.Id), item.ProductId));
        if (offer == null) return false;
        Apply(offer, selected[0]);
        await StoreCmsJsonRepository.SaveListAsync(LimitedOffersAdminService.GetStoragePath(), offers);
        LimitedOffersAdminService.NotifyPublishedChanged();
        return true;
    }

    private static async Task<IReadOnlyList<RegisteredStoreAsset>> LoadCatalogAsync()
    {
        var assets = await StoreAssetCatalogService.LoadAsync();
        return assets.Select(item => new RegisteredStoreAsset(
                item.AssetId,
                item.AssetType,
                item.OwnerScope,
                item.HasDisplayMetadata
                    ? item.DisplayName
                    : StoreAssetCatalogService.IncompleteDisplayName,
                item.PreviewImage,
                item.ColorHex))
            .ToList();
    }

    private static InventoryAuditItem Audit(
        ProductSnapshot product,
        IReadOnlyList<RegisteredStoreAsset> catalog,
        IReadOnlySet<string> duplicateIds)
    {
        var exactMatches = catalog
            .Where(asset => Same(asset.AssetId, product.AssetId))
            .ToList();
        var safeMatches = exactMatches.Count > 0
            ? exactMatches
            : catalog.Where(asset =>
                    !string.IsNullOrWhiteSpace(product.ImagePath) &&
                    SamePayload(asset.ImagePath, product.ImagePath))
                .ToList();

        var status = InventoryAuditStatus.Valid;
        if (!StoreProductAssetTypeCatalog.TryResolve(product.StoreTypeId, out var type))
            status = InventoryAuditStatus.MissingAssetType;
        else if (StoreProductAssetTypeCatalog.IsInventory(type) && exactMatches.Count == 0)
            status = InventoryAuditStatus.MissingAsset;
        else if (!Enum.TryParse<StoreProductOwnerScope>(product.OwnerScope?.Trim(), false, out var owner) ||
                 owner != StoreProductAssetTypeCatalog.GetOwnerScope(type))
            status = InventoryAuditStatus.InvalidOwnerScope;
        else if (!string.IsNullOrWhiteSpace(product.AssetId) && duplicateIds.Contains(product.AssetId.Trim()))
            status = InventoryAuditStatus.DuplicateAssetId;
        else if (StoreProductAssetTypeCatalog.IsInventory(type) &&
                 exactMatches.Count(asset => asset.AssetType == type) != 1)
            status = InventoryAuditStatus.UnsupportedPayload;
        else if (!StoreProductAssetTypeCatalog.Validate(
                     product.StoreTypeId,
                     product.AssetId,
                     product.OwnerScope ?? "",
                     product.ImagePath,
                     product.ColorHex,
                     out _))
            status = InventoryAuditStatus.UnsupportedPayload;

        return new InventoryAuditItem(
            product.Source,
            string.IsNullOrWhiteSpace(product.Name) ? "(Unnamed product)" : product.Name,
            product.ProductId,
            product.AssetId,
            product.StoreTypeId,
            product.OwnerScope ?? "",
            product.ImagePath,
            product.ColorHex,
            status,
            StatusText(status),
            safeMatches);
    }

    private static void Apply(NewArrivalRecord record, RegisteredStoreAsset asset)
    {
        record.AssetId = asset.AssetId;
        record.StoreTypeId = asset.AssetType.ToString();
        record.OwnerScope = asset.OwnerScope.ToString();
        if (asset.AssetType == StoreProductAssetType.TeamColor)
            record.ColorHex = asset.ColorHex;
        if (string.IsNullOrWhiteSpace(record.ImagePath) && !string.IsNullOrWhiteSpace(asset.ImagePath))
            record.ImagePath = asset.ImagePath;
        record.UpdatedAt = DateTime.UtcNow;
    }

    private static void Apply(LimitedOfferRecord record, RegisteredStoreAsset asset)
    {
        record.AssetId = asset.AssetId;
        record.StoreTypeId = asset.AssetType.ToString();
        record.OwnerScope = asset.OwnerScope.ToString();
        if (asset.AssetType == StoreProductAssetType.TeamColor)
            record.ColorHex = asset.ColorHex;
        if (string.IsNullOrWhiteSpace(record.ImagePath) && !string.IsNullOrWhiteSpace(asset.ImagePath))
            record.ImagePath = asset.ImagePath;
        record.UpdatedAt = DateTime.UtcNow;
    }

    private static string ProductId(string? productId, string? fallback) =>
        !string.IsNullOrWhiteSpace(productId)
            ? productId.Trim()
            : fallback?.Trim() ?? string.Empty;

    private static string StatusText(InventoryAuditStatus status) => status switch
    {
        InventoryAuditStatus.Valid => "Valid",
        InventoryAuditStatus.MissingAssetType => "Missing Asset Type",
        InventoryAuditStatus.MissingAsset => "Missing Asset",
        InventoryAuditStatus.InvalidOwnerScope => "Invalid OwnerScope",
        InventoryAuditStatus.DuplicateAssetId => "Duplicate AssetId",
        _ => "Unsupported Payload"
    };

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool SamePayload(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;
        var leftFile = Path.GetFileName(left.Trim());
        var rightFile = Path.GetFileName(right.Trim());
        return Same(left, right) || (!string.IsNullOrWhiteSpace(leftFile) && Same(leftFile, rightFile));
    }

    private sealed record ProductSnapshot(
        InventoryAuditProductSource Source,
        string Name,
        string ProductId,
        string AssetId,
        string StoreTypeId,
        string OwnerScope,
        string ImagePath,
        string ColorHex);
}

