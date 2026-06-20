namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public static class StoreCmsOrderingEngine
{
    public static IOrderedEnumerable<T> ByFeaturedAndSortOrder<T>(IEnumerable<T> records, Func<T, bool> featured, Func<T, int> sortOrder) => records.OrderByDescending(featured).ThenBy(sortOrder);
    public static IOrderedEnumerable<T> ThenByPublishedDescending<T>(IOrderedEnumerable<T> records, Func<T, DateTime?> publishedAt, Func<T, DateTime> updatedAt) => records.ThenByDescending(item => publishedAt(item) ?? updatedAt(item));
    public static IEnumerable<T> ByCustom<T>(IEnumerable<T> records, IComparer<T> comparer) => records.OrderBy(item => item, comparer);
}
