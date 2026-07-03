using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class BackgroundsAdminService
{
    private const string FileName = "gallery_backgrounds_admin.json";
    public static event Action? PublishedChanged;

    public static async Task<BackgroundRecord> SaveDraftAsync(BackgroundRecord record)
    {
        var hadExistingId = !string.IsNullOrWhiteSpace(record.Id);
        var records = await LoadRecordsAsync();
        var existing = records.FirstOrDefault(item => item.Id == record.Id && item.Status == BackgroundStatus.Draft);
        Prepare(record, existing, hadExistingId);
        EnsureNoCollision(record, records, hadExistingId);
        StoreCmsPublishEngine.SaveDraft(records, record, item => item.Id, SetStatus, (item, value) => item.UpdatedAt = value);
        record.PublishedAt = null;
        await SaveRecordsAsync(records);
        return record;
    }

    public static Task<BackgroundRecord> SaveDraft(BackgroundRecord record) => SaveDraftAsync(record);
    public static async Task<IReadOnlyList<BackgroundRecord>> LoadAllDraftsAsync() => Normalize(await LoadRecordsAsync()).Where(item => item.Status == BackgroundStatus.Draft).OrderByDescending(item => item.UpdatedAt).ToList();
    public static Task<IReadOnlyList<BackgroundRecord>> LoadAllDrafts() => LoadAllDraftsAsync();
    public static async Task<BackgroundRecord?> LoadDraftByIdAsync(string id) => Normalize(await LoadRecordsAsync()).FirstOrDefault(item => item.Id == id && item.Status == BackgroundStatus.Draft);
    public static Task<BackgroundRecord?> LoadDraftById(string id) => LoadDraftByIdAsync(id);

    public static async Task DeleteDraftAsync(string id)
    {
        var records = await LoadRecordsAsync();
        var draft = records.FirstOrDefault(item => item.Id == id && item.Status == BackgroundStatus.Draft);
        if (draft != null) records.Remove(draft);
        await SaveRecordsAsync(records);
    }

    public static Task DeleteDraft(string id) => DeleteDraftAsync(id);

    public static async Task<BackgroundRecord> PublishAsync(BackgroundRecord record)
    {
        EnsureValid(record);
        var hadExistingId = !string.IsNullOrWhiteSpace(record.Id);
        var records = await LoadRecordsAsync();
        var existing = records.FirstOrDefault(item => item.Id == record.Id);
        Prepare(record, existing, hadExistingId);
        EnsureNoCollision(record, records, hadExistingId);
        StoreCmsPublishEngine.Publish(records, record, item => item.Id, SetStatus, (item, value) => item.UpdatedAt = value, (item, value) => item.PublishedAt = value);
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
        return record;
    }

    public static Task<BackgroundRecord> Publish(BackgroundRecord record) => PublishAsync(record);

    public static async Task<BackgroundRecord> UpdatePublishedAsync(BackgroundRecord record)
    {
        EnsureValid(record);
        var hadExistingId = !string.IsNullOrWhiteSpace(record.Id);
        var records = await LoadRecordsAsync();
        var existing = records.FirstOrDefault(item => item.Id == record.Id && item.Status != BackgroundStatus.Draft)
            ?? throw new InvalidOperationException("تعذر العثور على الخلفية المنشورة");
        Prepare(record, existing, hadExistingId);
        EnsureNoCollision(record, records, hadExistingId);
        record.CreatedAt = existing.CreatedAt;
        record.PublishedAt = existing.PublishedAt;
        record.Status = BackgroundStatus.Published;
        StoreCmsPublishEngine.UpdatePublished(records, record, item => item.Id, (item, value) => item.UpdatedAt = value);
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
        return record;
    }

    public static Task<BackgroundRecord> UpdatePublished(BackgroundRecord record) => UpdatePublishedAsync(record);
    public static async Task<IReadOnlyList<BackgroundRecord>> LoadPublishedAsync() => Order(Normalize(await LoadRecordsAsync()).Where(item => item.Status == BackgroundStatus.Published)).ToList();
    public static Task<IReadOnlyList<BackgroundRecord>> LoadPublished() => LoadPublishedAsync();
    public static async Task<IReadOnlyList<BackgroundRecord>> LoadManagedAsync() => Normalize(await LoadRecordsAsync()).Where(item => item.Status != BackgroundStatus.Draft).OrderByDescending(item => item.UpdatedAt).ToList();

    public static async Task HidePublishedAsync(string id)
    {
        var records = await LoadRecordsAsync();
        if (!StoreCmsPublishEngine.Hide(records.Where(item => item.Status == BackgroundStatus.Published), id, item => item.Id, SetStatus)) return;
        records.First(item => item.Id == id).UpdatedAt = DateTime.UtcNow;
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
    }

    public static Task HidePublished(string id) => HidePublishedAsync(id);

    public static async Task DeletePublishedAsync(string id)
    {
        var records = await LoadRecordsAsync();
        var item = records.FirstOrDefault(record => record.Id == id && record.Status != BackgroundStatus.Draft);
        if (item == null || !StoreCmsPublishEngine.Delete(records, id, record => record.Id)) return;
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke();
    }

    public static Task DeletePublished(string id) => DeletePublishedAsync(id);

    public static async Task<BackgroundRecord?> CreateDraftFromPublishedAsync(string id)
    {
        var source = Normalize(await LoadRecordsAsync()).FirstOrDefault(item => item.Id == id && item.Status == BackgroundStatus.Published);
        if (source == null) return null;
        var draft = Clone(source);
        draft.Id = Guid.NewGuid().ToString();
        draft.CreatedAt = DateTime.UtcNow;
        draft.PublishedAt = null;
        draft.Status = BackgroundStatus.Draft;
        return await SaveDraftAsync(draft);
    }

    public static Task<BackgroundRecord?> CreateDraftFromPublished(string id) => CreateDraftFromPublishedAsync(id);

    public static async Task<IReadOnlyList<BackgroundRecord>> SearchAsync(string? query = null, string? category = null, BackgroundRarity? rarity = null, BackgroundStatus? status = null, bool? featured = null)
    {
        IEnumerable<BackgroundRecord> records = Normalize(await LoadRecordsAsync());
        records = StoreCmsSearchEngine.SearchText(records, query, item => item.NameAr, item => item.NameEn, item => item.Description, item => item.Tag, item => item.Collection);
        records = StoreCmsSearchEngine.SearchCategory(records, category, item => item.CategoryId);
        records = StoreCmsSearchEngine.SearchStatus(records, status, item => item.Status);
        records = StoreCmsSearchEngine.FilterFeatured(records, featured, item => item.IsFeatured);
        if (rarity.HasValue) records = records.Where(item => item.Rarity == rarity.Value);
        return Order(records).ToList();
    }

    public static Task<IReadOnlyList<BackgroundRecord>> Search(string? query = null, string? category = null, BackgroundRarity? rarity = null, BackgroundStatus? status = null, bool? featured = null) => SearchAsync(query, category, rarity, status, featured);
    public static async Task<StoreCmsStatistics> GetStatisticsAsync() => StoreCmsStatistics.Calculate(Normalize(await LoadRecordsAsync()), item => (StoreCmsStatus)(int)item.Status, item => item.IsFeatured, item => item.Status == BackgroundStatus.Published);

    public static bool ValidateForPublish(BackgroundRecord record, out string message)
    {
        var result = new StoreCmsValidationResult();
        if (string.IsNullOrWhiteSpace(record.NameAr) && string.IsNullOrWhiteSpace(record.NameEn)) result.Add("Name", "اسم الخلفية مطلوب");
        StoreCmsValidationEngine.ValidateImage(result, record.ImagePath, "صورة الخلفية مطلوبة");
        if (string.IsNullOrWhiteSpace(record.CategoryId) && string.IsNullOrWhiteSpace(record.Collection)) result.Add("Category", "التصنيف أو المجموعة مطلوبة");
        StoreCmsPricingEngine.Validate(result, record.Price, record.IsFree || record.CurrencyType == BackgroundCurrencyType.Free);
        if (record.SortOrder < 0) result.Add("SortOrder", "ترتيب العرض يجب أن يكون رقماً صالحاً");
        message = result.FirstMessage;
        return result.IsValid;
    }

    public static string GetStoragePath() => Path.Combine(StoreAdminService.GetAdminStorageRoot(), FileName);
    private static void SetStatus(BackgroundRecord item, StoreCmsStatus status) => item.Status = (BackgroundStatus)(int)status;
    private static IEnumerable<BackgroundRecord> Order(IEnumerable<BackgroundRecord> records) => StoreCmsOrderingEngine.ByCustom(records, Comparer<BackgroundRecord>.Create((left, right) =>
    {
        var value = right.IsFeatured.CompareTo(left.IsFeatured);
        if (value != 0) return value;
        value = left.FeaturedPriority.CompareTo(right.FeaturedPriority);
        if (value != 0) return value;
        value = right.Rarity.CompareTo(left.Rarity);
        if (value != 0) return value;
        value = left.SortOrder.CompareTo(right.SortOrder);
        return value != 0 ? value : Nullable.Compare(right.PublishedAt, left.PublishedAt);
    }));
    private static void EnsureValid(BackgroundRecord record) { if (!ValidateForPublish(record, out var message)) throw new InvalidOperationException(message); }
    private static void EnsureNoCollision(BackgroundRecord record, IEnumerable<BackgroundRecord> allRecords, bool hadExistingId)
    {
        if (hadExistingId) return;
        var collision = allRecords.Any(item => CanonicalAssetIdentityService.SameAssetId(item.Id, record.Id));
        if (collision) throw new InvalidOperationException($"Duplicate background asset id: {record.Id}");
    }
    private static void Prepare(BackgroundRecord record, BackgroundRecord? existing, bool hadExistingId) { if (hadExistingId) record.Id = record.Id.Trim(); else record.Id = CanonicalAssetIdentityService.GenerateCanonicalAssetId("ProfileBackground", !string.IsNullOrWhiteSpace(record.NameAr) ? record.NameAr : record.NameEn); record.CreatedAt = existing?.CreatedAt ?? (record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt); record.UpdatedAt = DateTime.UtcNow; }
    private static BackgroundRecord Clone(BackgroundRecord source) => new() { NameAr = source.NameAr, NameEn = source.NameEn, Description = source.Description, ImagePath = source.ImagePath, ThumbnailPath = source.ThumbnailPath, CategoryId = source.CategoryId, Collection = source.Collection, Rarity = source.Rarity, CurrencyType = source.CurrencyType, Price = source.Price, IsFree = source.IsFree, UnlockType = source.UnlockType, UnlockRequirement = source.UnlockRequirement, Tag = source.Tag, IsAnimated = source.IsAnimated, IsLimited = source.IsLimited, IsFeatured = source.IsFeatured, FeaturedPriority = source.FeaturedPriority, SortOrder = source.SortOrder, SeasonId = source.SeasonId, EventId = source.EventId, CollectionId = source.CollectionId, Version = source.Version };
    private static IEnumerable<BackgroundRecord> Normalize(IEnumerable<BackgroundRecord> records) { foreach (var record in records) { if (record.CurrencyType == BackgroundCurrencyType.Free) record.IsFree = true; if (record.IsFree) record.Price = 0; yield return record; } }
    private static Task<List<BackgroundRecord>> LoadRecordsAsync() => StoreCmsJsonRepository.LoadListAsync<BackgroundRecord>(GetStoragePath());
    private static Task SaveRecordsAsync(IReadOnlyList<BackgroundRecord> records) => StoreCmsJsonRepository.SaveListAsync(GetStoragePath(), records);
}
