using System.Text.Encodings.Web;
using System.Text.Json;
using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Admin.Services;

public static class CurrentSeasonAdminService
{
    private const string FileName = "gallery_current_season_admin.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static event Action<CurrentSeasonRecord?>? PublishedChanged;

    public static async Task<IReadOnlyList<CurrentSeasonRecord>> LoadAllAsync() =>
        await LoadRecordsAsync();

    public static async Task<IReadOnlyList<CurrentSeasonRecord>> LoadAllDraftsAsync() =>
        (await LoadRecordsAsync())
            .Where(record => record.Status == StoreContentStatus.Draft)
            .OrderByDescending(record => record.UpdatedAt)
            .ToList();

    public static Task<IReadOnlyList<CurrentSeasonRecord>> LoadAllDrafts() => LoadAllDraftsAsync();

    public static async Task<CurrentSeasonRecord?> LoadLatestDraftAsync() =>
        (await LoadAllDraftsAsync()).FirstOrDefault();

    public static async Task<CurrentSeasonRecord?> LoadDraftByIdAsync(string id) =>
        string.IsNullOrWhiteSpace(id)
            ? null
            : (await LoadRecordsAsync()).FirstOrDefault(record =>
                record.Status == StoreContentStatus.Draft && SameIdentity(record, id));

    public static Task<CurrentSeasonRecord?> LoadDraftById(string id) => LoadDraftByIdAsync(id);

    public static async Task<CurrentSeasonRecord?> LoadManagedByIdAsync(string id) =>
        string.IsNullOrWhiteSpace(id)
            ? null
            : (await LoadRecordsAsync()).FirstOrDefault(record =>
                record.Status != StoreContentStatus.Draft && SameIdentity(record, id));

    public static async Task<CurrentSeasonRecord?> LoadPublishedAsync() =>
        (await LoadPublishedRecordsAsync())
            .OrderByDescending(record => record.PublishedAt ?? record.UpdatedAt)
            .FirstOrDefault();

    public static async Task<IReadOnlyList<CurrentSeasonRecord>> LoadPublishedRecordsAsync() =>
        (await LoadRecordsAsync())
            .Where(record => record.Status == StoreContentStatus.Published)
            .OrderBy(record => record.SortOrder)
            .ThenByDescending(record => record.PublishedAt ?? record.UpdatedAt)
            .ToList();

    public static async Task<IReadOnlyList<CurrentSeasonRecord>> LoadManagedAsync() =>
        (await LoadRecordsAsync())
            .Where(record => record.Status != StoreContentStatus.Draft)
            .OrderByDescending(record => record.PublishedAt ?? record.UpdatedAt)
            .ToList();

    public static async Task<CurrentSeasonRecord> SaveDraftAsync(CurrentSeasonRecord record)
    {
        var records = await LoadRecordsAsync();
        EnsureIdentity(record);
        var existing = records.FirstOrDefault(item => SameIdentity(item, GetIdentity(record)));
        var saved = PrepareForSave(record, existing);
        saved.Status = StoreContentStatus.Draft;
        saved.PublishedAt = null;

        RemoveIdentity(records, GetIdentity(saved));
        records.Add(saved);
        await SaveRecordsAsync(records);
        if (existing?.Status == StoreContentStatus.Published)
            PublishedChanged?.Invoke(null);
        return saved;
    }

    public static Task<CurrentSeasonRecord> SaveDraft(CurrentSeasonRecord record) => SaveDraftAsync(record);

    public static async Task<CurrentSeasonRecord> PublishAsync(CurrentSeasonRecord record)
    {
        var records = await LoadRecordsAsync();
        EnsureIdentity(record);
        var existing = records.FirstOrDefault(item => SameIdentity(item, GetIdentity(record)));
        var saved = PrepareForSave(record, existing);
        saved.Status = StoreContentStatus.Published;
        saved.IsVisible = true;
        saved.PublishedAt = DateTime.UtcNow;

        RemoveIdentity(records, GetIdentity(saved));
        records.Add(saved);
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke(saved);
        return saved;
    }

    public static Task<CurrentSeasonRecord> Publish(CurrentSeasonRecord record) => PublishAsync(record);

    public static async Task<CurrentSeasonRecord> UpdateManagedAsync(CurrentSeasonRecord record)
    {
        if (!ValidateForPublish(record, out var message))
            throw new InvalidOperationException(message);

        var records = await LoadRecordsAsync();
        EnsureIdentity(record);
        var identity = GetIdentity(record);
        var existing = records.FirstOrDefault(item =>
            item.Status != StoreContentStatus.Draft && SameIdentity(item, identity))
            ?? throw new InvalidOperationException("تعذر العثور على الموسم المحدد");

        var saved = PrepareForSave(record, existing);
        saved.Status = existing.Status;
        saved.IsVisible = existing.Status == StoreContentStatus.Published && existing.IsVisible;
        saved.PublishedAt = existing.PublishedAt;

        RemoveIdentity(records, identity);
        records.Add(saved);
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke(saved.Status == StoreContentStatus.Published ? saved : null);
        return saved;
    }

    public static async Task HidePublishedAsync(string id)
    {
        var records = await LoadRecordsAsync();
        var published = records.FirstOrDefault(item =>
            item.Status == StoreContentStatus.Published && SameIdentity(item, id));
        if (published == null)
            return;

        published.Status = StoreContentStatus.Hidden;
        published.IsVisible = false;
        published.UpdatedAt = DateTime.UtcNow;
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke(null);
    }

    public static async Task HidePublishedAsync()
    {
        var published = await LoadPublishedAsync();
        if (published != null)
            await HidePublishedAsync(GetIdentity(published));
    }

    public static Task HidePublished() => HidePublishedAsync();

    public static async Task DeletePublishedAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var records = await LoadRecordsAsync();
        records.RemoveAll(item => item.Status != StoreContentStatus.Draft && SameIdentity(item, id));
        await SaveRecordsAsync(records);
        PublishedChanged?.Invoke(null);
    }

    public static async Task DeletePublishedAsync()
    {
        var published = await LoadPublishedAsync();
        if (published != null)
            await DeletePublishedAsync(GetIdentity(published));
    }

    public static async Task DeleteDraftAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var records = await LoadRecordsAsync();
        records.RemoveAll(item => item.Status == StoreContentStatus.Draft && SameIdentity(item, id));
        await SaveRecordsAsync(records);
    }

    public static Task DeleteDraft(string id) => DeleteDraftAsync(id);

    public static bool ValidateForPublish(CurrentSeasonRecord record) =>
        ValidateForPublish(record, out _);

    public static bool ValidateForPublish(CurrentSeasonRecord record, out string message)
    {
        if (string.IsNullOrWhiteSpace(record.Title) ||
            string.IsNullOrWhiteSpace(record.Subtitle) ||
            string.IsNullOrWhiteSpace(record.Description) ||
            string.IsNullOrWhiteSpace(record.ButtonText) ||
            string.IsNullOrWhiteSpace(record.ImagePath))
        {
            message = "يرجى إكمال جميع بيانات الموسم قبل النشر";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public static string GetIdentity(CurrentSeasonRecord record) =>
        string.IsNullOrWhiteSpace(record.SeasonId) ? record.Id : record.SeasonId;

    public static string GetStoragePath() =>
        Path.Combine(StoreAdminService.GetAdminStorageRoot(), FileName);

    private static CurrentSeasonRecord PrepareForSave(CurrentSeasonRecord record, CurrentSeasonRecord? existing)
    {
        EnsureIdentity(record);
        record.CreatedAt = existing?.CreatedAt ?? (record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt);
        record.UpdatedAt = DateTime.UtcNow;
        return record;
    }

    private static void EnsureIdentity(CurrentSeasonRecord record)
    {
        var identity = !string.IsNullOrWhiteSpace(record.SeasonId)
            ? record.SeasonId.Trim()
            : !string.IsNullOrWhiteSpace(record.Id)
                ? record.Id.Trim()
                : Guid.NewGuid().ToString();
        record.SeasonId = identity;
        record.Id = identity;
    }

    private static bool SameIdentity(CurrentSeasonRecord record, string id) =>
        string.Equals(GetIdentity(record), id, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(record.Id, id, StringComparison.OrdinalIgnoreCase);

    private static void RemoveIdentity(List<CurrentSeasonRecord> records, string id) =>
        records.RemoveAll(item => SameIdentity(item, id));

    private static async Task<List<CurrentSeasonRecord>> LoadRecordsAsync()
    {
        var records = await StoreCmsJsonRepository.LoadListAsync<CurrentSeasonRecord>(GetStoragePath(), MigrateLegacyRoot);
        var migrated = false;
        foreach (var record in records)
        {
            var previousId = record.Id;
            var previousSeasonId = record.SeasonId;
            EnsureIdentity(record);
            migrated |= previousId != record.Id || previousSeasonId != record.SeasonId;
        }

        if (migrated)
            await SaveRecordsAsync(records);
        return records;
    }

    private static List<CurrentSeasonRecord> MigrateLegacyRoot(JsonElement root)
    {
        if (root.TryGetProperty("Records", out var recordsElement) && recordsElement.ValueKind == JsonValueKind.Array)
            return recordsElement.Deserialize<List<CurrentSeasonRecord>>(JsonOptions) ?? [];

        var records = new List<CurrentSeasonRecord>();
        if (root.TryGetProperty("LatestDraft", out var draftElement) && draftElement.ValueKind == JsonValueKind.Object)
        {
            var draft = draftElement.Deserialize<CurrentSeasonRecord>(JsonOptions);
            if (draft != null)
                records.Add(draft);
        }

        if (root.TryGetProperty("Published", out var publishedElement) && publishedElement.ValueKind == JsonValueKind.Object)
        {
            var published = publishedElement.Deserialize<CurrentSeasonRecord>(JsonOptions);
            if (published != null)
                records.Add(published);
        }

        if (records.Count > 0)
            return records;

        var legacy = root.Deserialize<CurrentSeasonRecord>(JsonOptions);
        if (legacy != null)
            records.Add(legacy);
        return records;
    }

    private static Task SaveRecordsAsync(IReadOnlyList<CurrentSeasonRecord> records) =>
        StoreCmsJsonRepository.SaveListAsync(GetStoragePath(), records);
}
