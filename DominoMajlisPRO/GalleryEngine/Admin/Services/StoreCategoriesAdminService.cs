using System.Text.Encodings.Web;
using System.Text.Json;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Core;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class StoreCategoriesAdminService
{
    private const string FileName = "gallery_store_categories_admin.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static event Action? PublishedChanged;

    public static async Task<StoreCategoryRecord> SaveDraftAsync(StoreCategoryRecord record)
    {
        var records = await LoadRecordsAsync();
        var existing = records.FirstOrDefault(item => item.Id == record.Id && item.Status == StoreCategoryStatus.Draft);
        Prepare(record, existing); record.Status = StoreCategoryStatus.Draft; record.PublishedAt = null;
        records.RemoveAll(item => item.Id == record.Id); records.Add(record); await SaveRecordsAsync(records); return record;
    }
    public static Task<StoreCategoryRecord> SaveDraft(StoreCategoryRecord record) => SaveDraftAsync(record);
    public static async Task<IReadOnlyList<StoreCategoryRecord>> LoadAllDraftsAsync() => (await LoadRecordsAsync()).Where(item => item.Status == StoreCategoryStatus.Draft).OrderByDescending(item => item.UpdatedAt).ToList();
    public static Task<IReadOnlyList<StoreCategoryRecord>> LoadAllDrafts() => LoadAllDraftsAsync();
    public static async Task<StoreCategoryRecord?> LoadDraftByIdAsync(string id) => (await LoadRecordsAsync()).FirstOrDefault(item => item.Id == id && item.Status == StoreCategoryStatus.Draft);
    public static Task<StoreCategoryRecord?> LoadDraftById(string id) => LoadDraftByIdAsync(id);
    public static async Task DeleteDraftAsync(string id) { var records = await LoadRecordsAsync(); records.RemoveAll(item => item.Id == id && item.Status == StoreCategoryStatus.Draft); await SaveRecordsAsync(records); }
    public static Task DeleteDraft(string id) => DeleteDraftAsync(id);

    public static async Task<StoreCategoryRecord> PublishAsync(StoreCategoryRecord record)
    {
        if (!ValidateForPublish(record, out var message)) throw new InvalidOperationException(message);
        var records = await LoadRecordsAsync(); var existing = records.FirstOrDefault(item => item.Id == record.Id);
        Prepare(record, existing); record.Status = StoreCategoryStatus.Published; record.PublishedAt = DateTime.UtcNow;
        records.RemoveAll(item => item.Id == record.Id); records.Add(record); await SaveRecordsAsync(records); PublishedChanged?.Invoke(); return record;
    }
    public static Task<StoreCategoryRecord> Publish(StoreCategoryRecord record) => PublishAsync(record);
    public static async Task<IReadOnlyList<StoreCategoryRecord>> LoadPublishedAsync() => Order((await LoadRecordsAsync()).Where(item => item.Status == StoreCategoryStatus.Published && item.IsVisible)).ToList();
    public static Task<IReadOnlyList<StoreCategoryRecord>> LoadPublished() => LoadPublishedAsync();
    public static async Task<IReadOnlyList<StoreCategoryRecord>> LoadManagedAsync() => (await LoadRecordsAsync()).Where(item => item.Status != StoreCategoryStatus.Draft).OrderByDescending(item => item.UpdatedAt).ToList();

    public static async Task<StoreCategoryRecord> UpdatePublishedAsync(StoreCategoryRecord record)
    {
        if (!ValidateForPublish(record, out var message)) throw new InvalidOperationException(message);
        var records = await LoadRecordsAsync();
        var existing = records.FirstOrDefault(item => item.Id == record.Id && item.Status == StoreCategoryStatus.Published) ?? throw new InvalidOperationException("تعذر العثور على التصنيف المنشور");
        record.CreatedAt = existing.CreatedAt; record.PublishedAt = existing.PublishedAt; record.UpdatedAt = DateTime.UtcNow; record.Status = StoreCategoryStatus.Published;
        records.Remove(existing); records.Add(record); await SaveRecordsAsync(records); PublishedChanged?.Invoke(); return record;
    }
    public static Task<StoreCategoryRecord> UpdatePublished(StoreCategoryRecord record) => UpdatePublishedAsync(record);
    public static async Task HidePublishedAsync(string id) { var records = await LoadRecordsAsync(); var item = records.FirstOrDefault(record => record.Id == id && record.Status == StoreCategoryStatus.Published); if (item == null) return; item.Status = StoreCategoryStatus.Hidden; item.UpdatedAt = DateTime.UtcNow; await SaveRecordsAsync(records); PublishedChanged?.Invoke(); }
    public static Task HidePublished(string id) => HidePublishedAsync(id);
    public static async Task DeletePublishedAsync(string id) { var records = await LoadRecordsAsync(); records.RemoveAll(item => item.Id == id && item.Status != StoreCategoryStatus.Draft); await SaveRecordsAsync(records); PublishedChanged?.Invoke(); }
    public static Task DeletePublished(string id) => DeletePublishedAsync(id);

    public static async Task<StoreCategoryRecord?> CreateDraftFromPublishedAsync(string id)
    {
        var source = (await LoadRecordsAsync()).FirstOrDefault(item => item.Id == id && item.Status == StoreCategoryStatus.Published);
        if (source == null) return null;
        return await SaveDraftAsync(new StoreCategoryRecord { NameAr = source.NameAr, NameEn = source.NameEn, Description = source.Description, IconPath = source.IconPath, BannerPath = source.BannerPath, AccentColor = source.AccentColor, Category = source.Category, Collection = source.Collection, SeasonId = source.SeasonId, DisplayOrder = source.DisplayOrder, IsVisible = source.IsVisible, IsFeatured = source.IsFeatured, ItemCount = source.ItemCount });
    }
    public static Task<StoreCategoryRecord?> CreateDraftFromPublished(string id) => CreateDraftFromPublishedAsync(id);

    public static bool ValidateForPublish(StoreCategoryRecord record, out string message)
    {
        if (string.IsNullOrWhiteSpace(record.Category)) { message = "Category is required"; return false; }
        if (string.IsNullOrWhiteSpace(record.Collection)) { message = "Collection is required"; return false; }
        if (string.IsNullOrWhiteSpace(record.NameAr) && string.IsNullOrWhiteSpace(record.NameEn)) { message = "اسم التصنيف مطلوب"; return false; }
        if (string.IsNullOrWhiteSpace(record.IconPath)) { message = "أيقونة التصنيف مطلوبة"; return false; }
        if (record.DisplayOrder < 0) { message = "ترتيب العرض يجب أن يكون رقماً صالحاً"; return false; }
        if (!IsValidColor(record.AccentColor)) record.AccentColor = "#D4AF37";
        message = string.Empty; return true;
    }

    public static string GetStoragePath() => Path.Combine(StoreAdminService.GetAdminStorageRoot(), FileName);
    private static IOrderedEnumerable<StoreCategoryRecord> Order(IEnumerable<StoreCategoryRecord> records) => StoreCmsOrderingEngine.ThenByPublishedDescending(StoreCmsOrderingEngine.ByFeaturedAndSortOrder(records, item => item.IsFeatured, item => item.DisplayOrder), item => item.PublishedAt, item => item.UpdatedAt);
    private static void Prepare(StoreCategoryRecord record, StoreCategoryRecord? existing) { Normalize(record); if (string.IsNullOrWhiteSpace(record.Id)) record.Id = Guid.NewGuid().ToString(); record.CreatedAt = existing?.CreatedAt ?? (record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt); record.UpdatedAt = DateTime.UtcNow; }
    private static bool IsValidColor(string? value) { try { _ = Color.FromArgb(value ?? string.Empty); return !string.IsNullOrWhiteSpace(value); } catch { return false; } }
    private static async Task<List<StoreCategoryRecord>> LoadRecordsAsync()
    {
        var records = await StoreCmsJsonRepository.LoadListAsync<StoreCategoryRecord>(GetStoragePath());
        foreach (var record in records) Normalize(record);
        return records;
    }
    private static void Normalize(StoreCategoryRecord record)
    {
        record.Category = record.Category?.Trim() ?? string.Empty;
        record.Collection = record.Collection?.Trim() ?? string.Empty;
        record.SeasonId = record.SeasonId?.Trim() ?? string.Empty;
    }
    private static Task SaveRecordsAsync(IReadOnlyList<StoreCategoryRecord> records) => StoreCmsJsonRepository.SaveListAsync(GetStoragePath(), records);
}
