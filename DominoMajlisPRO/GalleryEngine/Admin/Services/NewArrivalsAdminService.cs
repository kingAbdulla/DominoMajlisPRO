using System.Text.RegularExpressions;
using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class NewArrivalsAdminService
{
    private const string FileName = "gallery_new_arrivals_admin.json";
    public static event Action? PublishedChanged;
    public static void NotifyPublishedChanged() => PublishedChanged?.Invoke();

    public static async Task<NewArrivalRecord> SaveDraftAsync(NewArrivalRecord record)
    {
        var records = await LoadRecordsAsync();
        EnsureAssetId(record);
        EnsureNoCollision(record, records);
        
        var assetId = GetAssetId(record);
        var existing = records.FirstOrDefault(item => item.Status == NewArrivalStatus.Draft && SameAssetId(item, assetId));
        var saved = PrepareForSave(record, existing);
        saved.Status = NewArrivalStatus.Draft;
        saved.PublishedAt = null;

        records.RemoveAll(item => item.Status == NewArrivalStatus.Draft && SameAssetId(item, assetId));
        records.Add(saved);
        await SaveRecordsAsync(records);
        return saved;
    }

    public static Task<NewArrivalRecord> SaveDraft(NewArrivalRecord record) => SaveDraftAsync(record);

    public static async Task<IReadOnlyList<NewArrivalRecord>> LoadAllDraftsAsync() =>
        DistinctLatest((await LoadRecordsAsync()).Where(item => item.Status == NewArrivalStatus.Draft));

    public static Task<IReadOnlyList<NewArrivalRecord>> LoadAllDrafts() => LoadAllDraftsAsync();

    public static async Task<NewArrivalRecord?> LoadDraftByIdAsync(string assetId) =>
        string.IsNullOrWhiteSpace(assetId)
            ? null
            : (await LoadRecordsAsync())
                .Where(item => item.Status == NewArrivalStatus.Draft && SameAssetId(item, assetId))
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefault();

    public static Task<NewArrivalRecord?> LoadDraftById(string assetId) => LoadDraftByIdAsync(assetId);

    public static async Task DeleteDraftAsync(string assetId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return;
        var records = await LoadRecordsAsync();
        records.RemoveAll(item => item.Status == NewArrivalStatus.Draft && SameAssetId(item, assetId));
        await SaveRecordsAsync(records);
    }

    public static Task DeleteDraft(string assetId) => DeleteDraftAsync(assetId);

    public static async Task<NewArrivalRecord> PublishAsync(NewArrivalRecord record)
    {
        if (!ValidateForPublish(record, out var message))
            throw new InvalidOperationException(message);

        var records = await LoadRecordsAsync();
        EnsureAssetId(record);
        EnsureNoCollision(record, records);

        var assetId = GetAssetId(record);
        var existing = records
            .Where(item => SameAssetId(item, assetId))
            .OrderBy(item => item.Status == NewArrivalStatus.Published ? 0 : 1)
            .ThenByDescending(item => item.UpdatedAt)
            .FirstOrDefault();
        var saved = PrepareForSave(record, existing);
        saved.Status = NewArrivalStatus.Published;
        saved.PublishedAt = DateTime.UtcNow;

        records.RemoveAll(item => SameAssetId(item, assetId));
        records.Add(saved);
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
        return saved;
    }

    public static Task<NewArrivalRecord> Publish(NewArrivalRecord record) => PublishAsync(record);

    public static async Task<NewArrivalRecord> UpdatePublishedAsync(NewArrivalRecord record)
    {
        if (!ValidateForPublish(record, out var message))
            throw new InvalidOperationException(message);

        var records = await LoadRecordsAsync();
        EnsureAssetId(record);
        EnsureNoCollision(record, records);

        var assetId = GetAssetId(record);
        var existing = records.FirstOrDefault(item => item.Status == NewArrivalStatus.Published && SameAssetId(item, assetId))
            ?? throw new InvalidOperationException("تعذر العثور على العنصر المنشور");
        record.CreatedAt = existing.CreatedAt;
        record.PublishedAt = existing.PublishedAt;
        record.UpdatedAt = DateTime.UtcNow;
        record.Status = NewArrivalStatus.Published;
        records.RemoveAll(item => SameAssetId(item, assetId));
        records.Add(record);
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
        return record;
    }

    public static async Task<IReadOnlyList<NewArrivalRecord>> LoadPublishedAsync()
    {
        var published = (await LoadRecordsAsync()).Where(item => item.Status == NewArrivalStatus.Published);
        return StoreCmsOrderingEngine
            .ThenByPublishedDescending(
                StoreCmsOrderingEngine.ByFeaturedAndSortOrder(published, item => item.IsFeatured, item => item.SortOrder),
                item => item.PublishedAt,
                item => item.UpdatedAt)
            .DistinctBy(GetAssetId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static Task<IReadOnlyList<NewArrivalRecord>> LoadPublished() => LoadPublishedAsync();

    public static async Task<IReadOnlyList<NewArrivalRecord>> LoadManagedAsync() =>
        DistinctLatest((await LoadRecordsAsync()).Where(item => item.Status != NewArrivalStatus.Draft));

    public static async Task<IReadOnlyList<NewArrivalRecord>> LoadAuditAsync() =>
        (await LoadRecordsAsync())
            .OrderByDescending(item => item.UpdatedAt)
            .ToList();

    public static async Task HidePublishedAsync(string assetId)
    {
        var records = await LoadRecordsAsync();
        var item = records.FirstOrDefault(record => record.Status == NewArrivalStatus.Published && SameAssetId(record, assetId));
        if (item == null)
            return;
        item.Status = NewArrivalStatus.Hidden;
        item.UpdatedAt = DateTime.UtcNow;
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
    }

    public static Task HidePublished(string assetId) => HidePublishedAsync(assetId);

    public static async Task RestorePublishedAsync(string assetId)
    {
        var records = await LoadRecordsAsync();
        var item = records.FirstOrDefault(record =>
            record.Status == NewArrivalStatus.Hidden && SameAssetId(record, assetId));
        if (item == null)
            return;

        item.Status = NewArrivalStatus.Published;
        item.UpdatedAt = DateTime.UtcNow;
        item.PublishedAt ??= DateTime.UtcNow;
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
    }

    public static async Task DeletePublishedAsync(string assetId)
    {
        var records = await LoadRecordsAsync();
        records.RemoveAll(item => item.Status != NewArrivalStatus.Draft && SameAssetId(item, assetId));
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
    }

    public static Task DeletePublished(string assetId) => DeletePublishedAsync(assetId);

    public static async Task<NewArrivalRecord?> CreateDraftFromPublishedAsync(string assetId)
    {
        var records = await LoadRecordsAsync();
        var published = records.FirstOrDefault(item => item.Status != NewArrivalStatus.Draft && SameAssetId(item, assetId));
        if (published == null)
            return null;

        var identity = GetAssetId(published);
        var existingDraft = records
            .Where(item => item.Status == NewArrivalStatus.Draft && SameAssetId(item, identity))
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefault();
        if (existingDraft != null)
            return existingDraft;

        return await SaveDraftAsync(new NewArrivalRecord
        {
            Id = published.Id,
            ProductId = published.ProductId,
            AssetId = identity,
            StoreTypeId = published.StoreTypeId,
            OwnerScope = published.OwnerScope,
            ColorHex = published.ColorHex,
            Title = published.Title,
            Subtitle = published.Subtitle,
            Description = published.Description,
            ButtonText = published.ButtonText,
            ImagePath = published.ImagePath,
            Category = published.Category,
            EffectType = published.EffectType,
            AnimationType = published.AnimationType,
            DurationMilliseconds = published.DurationMilliseconds,
            EquipTarget = published.EquipTarget,
            BundleAssetIds = published.BundleAssetIds?.ToList() ?? new List<string>(),
            DiscountPercent = published.DiscountPercent,
            Price = published.Price,
            CurrencyType = published.CurrencyType,
            IsFree = published.IsFree,
            IsFeatured = published.IsFeatured,
            SortOrder = published.SortOrder,
            Status = NewArrivalStatus.Draft
        });
    }

    public static Task<NewArrivalRecord?> CreateDraftFromPublished(string assetId) =>
        CreateDraftFromPublishedAsync(assetId);

    public static bool ValidateForPublish(NewArrivalRecord record) => ValidateForPublish(record, out _);

    public static bool ValidateForPublish(NewArrivalRecord record, out string message)
    {
        if (string.IsNullOrWhiteSpace(record.Title))
        {
            message = "يرجى إكمال الحقول المطلوبة قبل النشر";
            return false;
        }
        if (!StoreProductAssetTypeCatalog.Validate(record.StoreTypeId, record.AssetId, record.OwnerScope, record.ImagePath, record.ColorHex, out message))
            return false;
        if (record.CurrencyType != NewArrivalCurrencyType.Free && record.Price <= 0)
        {
            message = "السعر مطلوب عند اختيار العملات أو الجواهر";
            return false;
        }
        if (record.SortOrder < 0)
        {
            message = "ترتيب العرض يجب أن يكون رقماً صالحاً";
            return false;
        }
        message = string.Empty;
        return true;
    }

    public static bool IsMalformed(NewArrivalRecord record) =>
        !StoreProductAssetTypeCatalog.Validate(record.StoreTypeId, record.AssetId, record.OwnerScope, record.ImagePath, record.ColorHex, out _);

    public static string GetAssetId(NewArrivalRecord record) =>
        string.IsNullOrWhiteSpace(record.AssetId) ? record.Id : record.AssetId;

    public static string GetStoragePath() => Path.Combine(StoreAdminService.GetAdminStorageRoot(), FileName);

    private static NewArrivalRecord PrepareForSave(NewArrivalRecord record, NewArrivalRecord? existing)
    {
        EnsureAssetId(record);
        record.CreatedAt = existing?.CreatedAt ?? (record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt);
        record.UpdatedAt = DateTime.UtcNow;
        return record;
    }

    private static void EnsureAssetId(NewArrivalRecord record)
    {
        var productId = !string.IsNullOrWhiteSpace(record.ProductId)
            ? record.ProductId.Trim()
            : !string.IsNullOrWhiteSpace(record.Id)
                ? record.Id.Trim()
                : Guid.NewGuid().ToString();
        record.ProductId = productId;
        record.Id = productId;
        
        if (string.IsNullOrWhiteSpace(record.AssetId))
        {
            record.AssetId = GenerateCanonicalAssetId(record.StoreTypeId, record.Title);
        }
        else
        {
            record.AssetId = record.AssetId.Trim();
        }

        record.StoreTypeId = record.StoreTypeId?.Trim() ?? string.Empty;
        record.OwnerScope = record.OwnerScope?.Trim() ?? string.Empty;
        record.ColorHex = record.ColorHex?.Trim() ?? string.Empty;
        record.IsFree = record.Price == 0 || record.CurrencyType == NewArrivalCurrencyType.Free;
    }

    private static string GetCanonicalPrefix(string storeTypeId)
    {
        return storeTypeId?.Trim().ToLower() switch
        {
            "avatar" => "AVATAR",
            "profilebackground" => "PROFILE_BACKGROUND",
            "frame" => "FRAME",
            "effect" => "EFFECT",
            "emblem" => "EMBLEM",
            "emblembackground" => "EMBLEM_BACKGROUND",
            "teamcolor" => "TEAM_COLOR",
            "teameffect" => "TEAM_EFFECT",
            _ => "ASSET"
        };
    }

    private static string GenerateCanonicalAssetId(string storeTypeId, string title)
    {
        var prefix = GetCanonicalPrefix(storeTypeId);
        
        var slug = Regex.Replace(title?.Trim() ?? "", @"[^a-zA-Z0-9\s]", "");
        slug = Regex.Replace(slug, @"\s+", "_").ToUpper().Trim('_');

        if (string.IsNullOrWhiteSpace(slug))
            slug = "UNNAMED_ASSET";

        return $"{prefix}_{slug}";
    }

    private static void EnsureNoCollision(NewArrivalRecord record, IEnumerable<NewArrivalRecord> allRecords)
    {
        var assetId = GetAssetId(record);
        var collision = allRecords.FirstOrDefault(r => SameAssetId(r, assetId) && r.ProductId != record.ProductId);
        if (collision != null)
        {
            throw new InvalidOperationException($"المعرف {assetId} مستخدم بالفعل لعنصر آخر. يرجى تغيير اسم العنصر أو نوعه لتوليد معرف فريد.");
        }
    }

    private static bool SameAssetId(NewArrivalRecord record, string assetId) =>
        string.Equals(GetAssetId(record), assetId, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<NewArrivalRecord> DistinctLatest(IEnumerable<NewArrivalRecord> records) => records
        .OrderByDescending(item => item.UpdatedAt)
        .DistinctBy(GetAssetId, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static async Task<List<NewArrivalRecord>> LoadRecordsAsync()
    {
        var records = await StoreCmsJsonRepository.LoadListAsync<NewArrivalRecord>(GetStoragePath());
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

    private static Task SaveRecordsAsync(IReadOnlyList<NewArrivalRecord> records) =>
        StoreCmsJsonRepository.SaveListAsync(GetStoragePath(), records);
}