namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public static class StoreCmsPublishEngine
{
    public static T SaveDraft<T>(IList<T> records, T record, Func<T, string> id, Action<T, StoreCmsStatus> setStatus, Action<T, DateTime> setUpdatedAt) { Replace(records, record, id); setStatus(record, StoreCmsStatus.Draft); setUpdatedAt(record, DateTime.UtcNow); return record; }
    public static T Publish<T>(IList<T> records, T record, Func<T, string> id, Action<T, StoreCmsStatus> setStatus, Action<T, DateTime> setUpdatedAt, Action<T, DateTime?> setPublishedAt) { Replace(records, record, id); setStatus(record, StoreCmsStatus.Published); var now = DateTime.UtcNow; setUpdatedAt(record, now); setPublishedAt(record, now); return record; }
    public static T UpdatePublished<T>(IList<T> records, T record, Func<T, string> id, Action<T, DateTime> setUpdatedAt) { Replace(records, record, id); setUpdatedAt(record, DateTime.UtcNow); return record; }
    public static bool Hide<T>(IEnumerable<T> records, string recordId, Func<T, string> id, Action<T, StoreCmsStatus> setStatus) { var record = records.FirstOrDefault(item => id(item) == recordId); if (record is null) return false; setStatus(record, StoreCmsStatus.Hidden); return true; }
    public static bool Delete<T>(IList<T> records, string recordId, Func<T, string> id) { var record = records.FirstOrDefault(item => id(item) == recordId); return record is not null && records.Remove(record); }
    public static T CreateDraftFromPublished<T>(T published, Func<T, T> clone, Action<T, StoreCmsStatus> setStatus) { var draft = clone(published); setStatus(draft, StoreCmsStatus.Draft); return draft; }
    private static void Replace<T>(IList<T> records, T record, Func<T, string> id) { var existing = records.FirstOrDefault(item => id(item) == id(record)); if (existing is not null) records.Remove(existing); records.Add(record); }
}
