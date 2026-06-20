using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class AvatarsAdminService
{
    private const string FileName = "gallery_avatars_admin.json";
    public static event Action? PublishedChanged;

    public static async Task<AvatarRecord> SaveDraftAsync(AvatarRecord record)
    {
        var records = await LoadRecordsAsync(); var existing = records.FirstOrDefault(item => item.Id == record.Id && item.Status == AvatarStatus.Draft); Prepare(record, existing);
        StoreCmsPublishEngine.SaveDraft(records, record, item => item.Id, (item, status) => item.Status = (AvatarStatus)(int)status, (item, updated) => item.UpdatedAt = updated); record.PublishedAt = null;
        await SaveRecordsAsync(records); return record;
    }
    public static Task<AvatarRecord> SaveDraft(AvatarRecord record) => SaveDraftAsync(record);
    public static async Task<IReadOnlyList<AvatarRecord>> LoadAllDraftsAsync() => Normalize(await LoadRecordsAsync()).Where(item => item.Status == AvatarStatus.Draft).OrderByDescending(item => item.UpdatedAt).ToList();
    public static Task<IReadOnlyList<AvatarRecord>> LoadAllDrafts() => LoadAllDraftsAsync();
    public static async Task<AvatarRecord?> LoadDraftByIdAsync(string id) => Normalize(await LoadRecordsAsync()).FirstOrDefault(item => item.Id == id && item.Status == AvatarStatus.Draft);
    public static Task<AvatarRecord?> LoadDraftById(string id) => LoadDraftByIdAsync(id);
    public static async Task DeleteDraftAsync(string id) { var records = await LoadRecordsAsync(); var draft = records.FirstOrDefault(item => item.Id == id && item.Status == AvatarStatus.Draft); if (draft != null) records.Remove(draft); await SaveRecordsAsync(records); }
    public static Task DeleteDraft(string id) => DeleteDraftAsync(id);

    public static async Task<AvatarRecord> PublishAsync(AvatarRecord record)
    {
        EnsureValid(record); var records = await LoadRecordsAsync(); var existing = records.FirstOrDefault(item => item.Id == record.Id); Prepare(record, existing);
        StoreCmsPublishEngine.Publish(records, record, item => item.Id, (item, status) => item.Status = (AvatarStatus)(int)status, (item, updated) => item.UpdatedAt = updated, (item, published) => item.PublishedAt = published);
        await SaveRecordsAsync(records); PublishedChanged?.Invoke(); return record;
    }
    public static Task<AvatarRecord> Publish(AvatarRecord record) => PublishAsync(record);
    public static async Task<AvatarRecord> UpdatePublishedAsync(AvatarRecord record)
    {
        EnsureValid(record); var records = await LoadRecordsAsync(); var existing = records.FirstOrDefault(item => item.Id == record.Id && item.Status != AvatarStatus.Draft) ?? throw new InvalidOperationException("تعذر العثور على الصورة المنشورة");
        record.CreatedAt = existing.CreatedAt; record.PublishedAt = existing.PublishedAt; record.Status = AvatarStatus.Published;
        StoreCmsPublishEngine.UpdatePublished(records, record, item => item.Id, (item, updated) => item.UpdatedAt = updated);
        await SaveRecordsAsync(records); PublishedChanged?.Invoke(); return record;
    }
    public static Task<AvatarRecord> UpdatePublished(AvatarRecord record) => UpdatePublishedAsync(record);
    public static async Task<IReadOnlyList<AvatarRecord>> LoadPublishedAsync() => Order(Normalize(await LoadRecordsAsync()).Where(item => item.Status == AvatarStatus.Published)).ToList();
    public static Task<IReadOnlyList<AvatarRecord>> LoadPublished() => LoadPublishedAsync();
    public static async Task<IReadOnlyList<AvatarRecord>> LoadManagedAsync() => Normalize(await LoadRecordsAsync()).Where(item => item.Status != AvatarStatus.Draft).OrderByDescending(item => item.UpdatedAt).ToList();
    public static async Task HidePublishedAsync(string id) { var records = await LoadRecordsAsync(); if (StoreCmsPublishEngine.Hide(records.Where(item => item.Status == AvatarStatus.Published), id, item => item.Id, (item, status) => item.Status = (AvatarStatus)(int)status)) { var item = records.First(record => record.Id == id); item.UpdatedAt = DateTime.UtcNow; await SaveRecordsAsync(records); PublishedChanged?.Invoke(); } }
    public static Task HidePublished(string id) => HidePublishedAsync(id);
    public static async Task DeletePublishedAsync(string id) { var records = await LoadRecordsAsync(); var item = records.FirstOrDefault(record => record.Id == id && record.Status != AvatarStatus.Draft); if (item != null && StoreCmsPublishEngine.Delete(records, id, record => record.Id)) { await SaveRecordsAsync(records); PublishedChanged?.Invoke(); } }
    public static Task DeletePublished(string id) => DeletePublishedAsync(id);
    public static async Task<AvatarRecord?> CreateDraftFromPublishedAsync(string id) { var source = (await LoadRecordsAsync()).FirstOrDefault(item => item.Id == id && item.Status == AvatarStatus.Published); if (source == null) return null; var draft = Clone(source); draft.Id = Guid.NewGuid().ToString(); draft.CreatedAt = DateTime.UtcNow; draft.PublishedAt = null; draft.Status = AvatarStatus.Draft; return await SaveDraftAsync(draft); }
    public static Task<AvatarRecord?> CreateDraftFromPublished(string id) => CreateDraftFromPublishedAsync(id);

    public static async Task<IReadOnlyList<AvatarRecord>> SearchAsync(string? query = null, string? category = null, AvatarRarity? rarity = null, AvatarStatus? status = null, bool? featured = null)
    {
        IEnumerable<AvatarRecord> records = Normalize(await LoadRecordsAsync()); records = StoreCmsSearchEngine.SearchText(records, query, item => item.NameAr, item => item.NameEn, item => item.Description, item => item.Tag, item => item.Collection); records = StoreCmsSearchEngine.SearchCategory(records, category, item => item.CategoryId); records = StoreCmsSearchEngine.SearchStatus(records, status, item => item.Status); records = StoreCmsSearchEngine.FilterFeatured(records, featured, item => item.IsFeatured); if (rarity.HasValue) records = records.Where(item => item.Rarity == rarity.Value); return Order(records).ToList();
    }
    public static Task<IReadOnlyList<AvatarRecord>> Search(string? query = null, string? category = null, AvatarRarity? rarity = null, AvatarStatus? status = null, bool? featured = null) => SearchAsync(query, category, rarity, status, featured);
    public static async Task<StoreCmsStatistics> GetStatisticsAsync() { var records = Normalize(await LoadRecordsAsync()); return StoreCmsStatistics.Calculate(records, item => (StoreCmsStatus)(int)item.Status, item => item.IsFeatured, item => item.Status == AvatarStatus.Published); }

    public static bool ValidateForPublish(AvatarRecord record, out string message)
    {
        var result = new StoreCmsValidationResult();
        if (string.IsNullOrWhiteSpace(record.NameAr) && string.IsNullOrWhiteSpace(record.NameEn)) result.Add("Name", "اسم الصورة مطلوب");
        StoreCmsValidationEngine.ValidateImage(result, record.ImagePath, "صورة اللاعب مطلوبة");
        if (string.IsNullOrWhiteSpace(record.CategoryId) && string.IsNullOrWhiteSpace(record.Collection)) result.Add("Category", "التصنيف أو المجموعة مطلوبة");
        StoreCmsPricingEngine.Validate(result, record.Price, record.IsFree || record.CurrencyType == AvatarCurrencyType.Free);
        if (record.SortOrder < 0) result.Add("SortOrder", "ترتيب العرض يجب أن يكون رقماً صالحاً");
        message = result.FirstMessage; return result.IsValid;
    }

    public static string GetStoragePath() => Path.Combine(StoreAdminService.GetAdminStorageRoot(), FileName);
    private static IEnumerable<AvatarRecord> Order(IEnumerable<AvatarRecord> records) => StoreCmsOrderingEngine.ByCustom(records, Comparer<AvatarRecord>.Create((left, right) => { var value = right.IsFeatured.CompareTo(left.IsFeatured); if (value != 0) return value; value = left.FeaturedPriority.CompareTo(right.FeaturedPriority); if (value != 0) return value; value = right.Rarity.CompareTo(left.Rarity); if (value != 0) return value; value = left.SortOrder.CompareTo(right.SortOrder); return value != 0 ? value : Nullable.Compare(right.PublishedAt, left.PublishedAt); }));
    private static void EnsureValid(AvatarRecord record) { if (!ValidateForPublish(record, out var message)) throw new InvalidOperationException(message); }
    private static void Prepare(AvatarRecord record, AvatarRecord? existing) { if (string.IsNullOrWhiteSpace(record.Id)) record.Id = Guid.NewGuid().ToString(); record.CreatedAt = existing?.CreatedAt ?? (record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt); record.UpdatedAt = DateTime.UtcNow; }
    private static AvatarRecord Clone(AvatarRecord source) => new() { NameAr = source.NameAr, NameEn = source.NameEn, Description = source.Description, ImagePath = source.ImagePath, ThumbnailPath = source.ThumbnailPath, CategoryId = source.CategoryId, Collection = source.Collection, Rarity = source.Rarity, CurrencyType = source.CurrencyType, Price = source.Price, IsFree = source.IsFree, UnlockType = source.UnlockType, UnlockRequirement = source.UnlockRequirement, Tag = source.Tag, GenderOrStyle = source.GenderOrStyle, IsAnimated = source.IsAnimated, IsLimited = source.IsLimited, IsFeatured = source.IsFeatured, FeaturedPriority = source.FeaturedPriority, SortOrder = source.SortOrder, SeasonId = source.SeasonId, EventId = source.EventId, CollectionId = source.CollectionId, AnimationId = source.AnimationId, FrameId = source.FrameId, GlowEffect = source.GlowEffect, Version = source.Version };
    private static IEnumerable<AvatarRecord> Normalize(IEnumerable<AvatarRecord> records) { foreach (var record in records) { if (record.CurrencyType == AvatarCurrencyType.Free) { record.IsFree = true; record.Price = 0; } if (record.IsFree) record.Price = 0; yield return record; } }
    private static Task<List<AvatarRecord>> LoadRecordsAsync() => StoreCmsJsonRepository.LoadListAsync<AvatarRecord>(GetStoragePath());
    private static Task SaveRecordsAsync(IReadOnlyList<AvatarRecord> records) => StoreCmsJsonRepository.SaveListAsync(GetStoragePath(), records);
}
