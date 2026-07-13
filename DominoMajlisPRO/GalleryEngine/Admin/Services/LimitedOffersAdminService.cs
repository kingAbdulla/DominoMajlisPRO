using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class LimitedOffersAdminService
{
    private const string FileName = "gallery_limited_offers_admin.json";
    public static event Action? PublishedChanged;
    public static void NotifyPublishedChanged() => PublishedChanged?.Invoke();

    public static async Task<LimitedOfferRecord> SaveDraftAsync(LimitedOfferRecord record)
    {
        var records = await LoadRecordsAsync();
        EnsureAssetId(record);
        var assetId = GetAssetId(record);
        var existing = records.FirstOrDefault(item => item.Status == LimitedOfferStatus.Draft && SameAssetId(item, assetId));
        var saved = PrepareForSave(record, existing);
        saved.Status = LimitedOfferStatus.Draft;
        saved.PublishedAt = null;
        records.RemoveAll(item => item.Status == LimitedOfferStatus.Draft && SameAssetId(item, assetId));
        records.Add(saved);
        await SaveRecordsAsync(records);
        return saved;
    }

    public static Task<LimitedOfferRecord> SaveDraft(LimitedOfferRecord record) => SaveDraftAsync(record);

    public static async Task<IReadOnlyList<LimitedOfferRecord>> LoadAllDraftsAsync() =>
        (await LoadRecordsAsync())
            .Where(item => item.Status == LimitedOfferStatus.Draft)
            .OrderByDescending(item => item.UpdatedAt)
            .DistinctBy(GetAssetId, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static Task<IReadOnlyList<LimitedOfferRecord>> LoadAllDrafts() => LoadAllDraftsAsync();

    public static async Task<LimitedOfferRecord?> LoadDraftByIdAsync(string assetId) =>
        string.IsNullOrWhiteSpace(assetId)
            ? null
            : (await LoadRecordsAsync())
                .Where(item => item.Status == LimitedOfferStatus.Draft && SameAssetId(item, assetId))
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefault();

    public static Task<LimitedOfferRecord?> LoadDraftById(string assetId) => LoadDraftByIdAsync(assetId);

    public static async Task DeleteDraftAsync(string assetId)
    {
        var records = await LoadRecordsAsync();
        records.RemoveAll(item => item.Status == LimitedOfferStatus.Draft && SameAssetId(item, assetId));
        await SaveRecordsAsync(records);
    }

    public static Task DeleteDraft(string assetId) => DeleteDraftAsync(assetId);

    public static async Task<LimitedOfferRecord> PublishAsync(LimitedOfferRecord record)
    {
        if (!ValidateForPublish(record, out var message))
            throw new InvalidOperationException(message);
        var records = await LoadRecordsAsync();
        EnsureAssetId(record);
        var assetId = GetAssetId(record);
        var existing = records
            .Where(item => SameAssetId(item, assetId))
            .OrderBy(item => item.Status == LimitedOfferStatus.Published ? 0 : 1)
            .ThenByDescending(item => item.UpdatedAt)
            .FirstOrDefault();
        var saved = PrepareForSave(record, existing);
        saved.Status = LimitedOfferStatus.Published;
        saved.PublishedAt = DateTime.UtcNow;
        records.RemoveAll(item => SameAssetId(item, assetId));
        records.Add(saved);
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
        return saved;
    }

    public static Task<LimitedOfferRecord> Publish(LimitedOfferRecord record) => PublishAsync(record);

    public static async Task<LimitedOfferRecord> UpdatePublishedAsync(LimitedOfferRecord record)
    {
        if (!ValidateForPublish(record, out var message))
            throw new InvalidOperationException(message);
        var records = await LoadRecordsAsync();
        EnsureAssetId(record);
        var assetId = GetAssetId(record);
        var existing = records.FirstOrDefault(item => item.Status == LimitedOfferStatus.Published && SameAssetId(item, assetId))
            ?? throw new InvalidOperationException("تعذر العثور على العرض المنشور");
        record.CreatedAt = existing.CreatedAt;
        record.PublishedAt = existing.PublishedAt;
        record.UpdatedAt = DateTime.UtcNow;
        record.Status = LimitedOfferStatus.Published;
        records.RemoveAll(item => SameAssetId(item, assetId));
        records.Add(record);
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
        return record;
    }

    public static async Task<IReadOnlyList<LimitedOfferRecord>> LoadPublishedAsync() =>
        Order((await LoadRecordsAsync()).Where(item => item.Status == LimitedOfferStatus.Published))
            .DistinctBy(GetAssetId, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static Task<IReadOnlyList<LimitedOfferRecord>> LoadPublished() => LoadPublishedAsync();

    public static async Task<IReadOnlyList<LimitedOfferRecord>> LoadActivePublishedAsync()
    {
        var now = DateTime.Now;
        return Order((await LoadRecordsAsync()).Where(item =>
                item.Status == LimitedOfferStatus.Published && item.StartsAt <= now && item.EndsAt >= now))
            .DistinctBy(GetAssetId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static async Task<IReadOnlyList<LimitedOfferRecord>> LoadManagedOffersAsync() =>
        (await LoadRecordsAsync())
            .Where(item => item.Status != LimitedOfferStatus.Draft)
            .OrderByDescending(item => item.UpdatedAt)
            .DistinctBy(GetAssetId, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static async Task HidePublishedAsync(string assetId)
    {
        await SetStatusAsync(assetId, LimitedOfferStatus.Hidden);
        PublishedChanged?.Invoke();
    }

    public static Task HidePublished(string assetId) => HidePublishedAsync(assetId);

    public static async Task ExpireOfferAsync(string assetId)
    {
        await SetStatusAsync(assetId, LimitedOfferStatus.Expired);
        PublishedChanged?.Invoke();
    }

    public static Task ExpireOffer(string assetId) => ExpireOfferAsync(assetId);

    public static async Task DeletePublishedAsync(string assetId)
    {
        var records = await LoadRecordsAsync();
        records.RemoveAll(item => item.Status != LimitedOfferStatus.Draft && SameAssetId(item, assetId));
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
    }

    public static Task DeletePublished(string assetId) => DeletePublishedAsync(assetId);

    public static async Task DeleteAllRecordsAsync()
    {
        await SaveRecordsAsync(Array.Empty<LimitedOfferRecord>());
        PublishedChanged?.Invoke();
    }

    public static async Task<LimitedOfferRecord?> CreateDraftFromPublishedAsync(string assetId)
    {
        var records = await LoadRecordsAsync();
        var source = records.FirstOrDefault(item => item.Status != LimitedOfferStatus.Draft && SameAssetId(item, assetId));
        if (source == null)
            return null;

        var identity = GetAssetId(source);
        var existing = records
            .Where(item => item.Status == LimitedOfferStatus.Draft && SameAssetId(item, identity))
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefault();
        if (existing != null)
            return existing;

        return await SaveDraftAsync(new LimitedOfferRecord
        {
            Id = source.Id,
            ProductId = source.ProductId,
            AssetId = identity,
            StoreTypeId = source.StoreTypeId,
            OwnerScope = source.OwnerScope,
            ColorHex = source.ColorHex,
            Title = source.Title,
            Subtitle = source.Subtitle,
            Description = source.Description,
            ButtonText = source.ButtonText,
            ImagePath = source.ImagePath,
            Category = source.Category,
            OriginalPrice = source.OriginalPrice,
            DiscountPrice = source.DiscountPrice,
            DiscountPercent = source.DiscountPercent,
            CurrencyType = source.CurrencyType,
            IsFree = source.IsFree,
            StartsAt = source.StartsAt,
            EndsAt = source.EndsAt,
            IsFeatured = source.IsFeatured,
            SortOrder = source.SortOrder,
            Status = LimitedOfferStatus.Draft
        });
    }

    public static Task<LimitedOfferRecord?> CreateDraftFromPublished(string assetId) =>
        CreateDraftFromPublishedAsync(assetId);

    public static bool ValidateForPublish(LimitedOfferRecord record, out string message)
    {
        if (string.IsNullOrWhiteSpace(record.Title))
        { message = "يرجى إكمال الحقول المطلوبة قبل النشر"; return false; }
        if (!StoreProductAssetTypeCatalog.Validate(record.StoreTypeId, record.AssetId, record.OwnerScope, record.ImagePath, record.ColorHex, out message))
            return false;
        if (record.CurrencyType != LimitedOfferCurrencyType.Free && (record.OriginalPrice <= 0 || record.DiscountPrice < 0))
        { message = "الأسعار مطلوبة عند اختيار العملات أو الجواهر"; return false; }
        if (record.CurrencyType != LimitedOfferCurrencyType.Free && record.DiscountPrice >= record.OriginalPrice)
        { message = "سعر الخصم يجب أن يكون أقل من السعر الأصلي"; return false; }
        if (record.StartsAt >= record.EndsAt)
        { message = "وقت البداية يجب أن يسبق وقت النهاية"; return false; }
        if (record.EndsAt <= DateTime.Now)
        { message = "وقت نهاية العرض يجب أن يكون في المستقبل"; return false; }
        if (record.SortOrder < 0)
        { message = "ترتيب العرض يجب أن يكون رقماً صالحاً"; return false; }
        message = string.Empty;
        return true;
    }

    public static bool IsMalformed(LimitedOfferRecord record) =>
        !StoreProductAssetTypeCatalog.Validate(record.StoreTypeId, record.AssetId, record.OwnerScope, record.ImagePath, record.ColorHex, out _);

    public static string GetAssetId(LimitedOfferRecord record) =>
        string.IsNullOrWhiteSpace(record.AssetId) ? record.Id : record.AssetId;

    public static string GetStoragePath() => Path.Combine(StoreAdminService.GetAdminStorageRoot(), FileName);

    private static IOrderedEnumerable<LimitedOfferRecord> Order(IEnumerable<LimitedOfferRecord> records) =>
        StoreCmsOrderingEngine.ByFeaturedAndSortOrder(records, item => item.IsFeatured, item => item.SortOrder)
            .ThenBy(item => item.EndsAt)
            .ThenByDescending(item => item.PublishedAt ?? item.UpdatedAt);

    private static LimitedOfferRecord PrepareForSave(LimitedOfferRecord record, LimitedOfferRecord? existing)
    {
        EnsureAssetId(record);
        record.CreatedAt = existing?.CreatedAt ?? (record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt);
        record.UpdatedAt = DateTime.UtcNow;
        return record;
    }

    private static void EnsureAssetId(LimitedOfferRecord record)
    {
        var productId = !string.IsNullOrWhiteSpace(record.ProductId)
            ? record.ProductId.Trim()
            : !string.IsNullOrWhiteSpace(record.Id)
                ? record.Id.Trim()
                : Guid.NewGuid().ToString();
        record.ProductId = productId;
        record.Id = productId;
        record.AssetId = record.AssetId?.Trim() ?? string.Empty;
        record.StoreTypeId = record.StoreTypeId?.Trim() ?? string.Empty;
        record.OwnerScope = record.OwnerScope?.Trim() ?? string.Empty;
        record.ColorHex = record.ColorHex?.Trim() ?? string.Empty;
        record.IsFree = record.DiscountPrice == 0 || record.CurrencyType == LimitedOfferCurrencyType.Free;
    }

    private static bool SameAssetId(LimitedOfferRecord record, string assetId) =>
        string.Equals(GetAssetId(record), assetId, StringComparison.OrdinalIgnoreCase);

    private static async Task SetStatusAsync(string assetId, LimitedOfferStatus status)
    {
        var records = await LoadRecordsAsync();
        var item = records.FirstOrDefault(record => record.Status != LimitedOfferStatus.Draft && SameAssetId(record, assetId));
        if (item == null)
            return;
        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;
        await SaveRecordsAsync(records);
    }

    private static async Task<List<LimitedOfferRecord>> LoadRecordsAsync()
    {
        var records = await StoreCmsJsonRepository.LoadListAsync<LimitedOfferRecord>(GetStoragePath());
        var migrated = false;
        foreach (var record in records)
        {
            var oldId = record.Id;
            var oldAssetId = record.AssetId;
            var oldProductId = record.ProductId;
            EnsureAssetId(record);
            migrated |= oldId != record.Id || oldAssetId != record.AssetId || oldProductId != record.ProductId;
        }
        if (migrated)
            await SaveRecordsAsync(records);
        return records;
    }

    private static Task SaveRecordsAsync(IReadOnlyList<LimitedOfferRecord> records) =>
        StoreCmsJsonRepository.SaveListAsync(GetStoragePath(), records);
}
