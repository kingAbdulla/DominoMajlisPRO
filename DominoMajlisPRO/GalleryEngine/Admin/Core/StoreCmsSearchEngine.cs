namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public static class StoreCmsSearchEngine
{
    public static IEnumerable<T> SearchText<T>(IEnumerable<T> records, string? query, params Func<T, string?>[] selectors) { if (string.IsNullOrWhiteSpace(query)) return records; return records.Where(item => selectors.Any(selector => selector(item)?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)); }
    public static IEnumerable<T> SearchCategory<T>(IEnumerable<T> records, string? category, Func<T, string?> selector) => string.IsNullOrWhiteSpace(category) ? records : records.Where(item => string.Equals(selector(item), category, StringComparison.OrdinalIgnoreCase));
    public static IEnumerable<T> SearchStatus<T, TStatus>(IEnumerable<T> records, TStatus? status, Func<T, TStatus> selector) where TStatus : struct, Enum => status.HasValue ? records.Where(item => EqualityComparer<TStatus>.Default.Equals(selector(item), status.Value)) : records;
    public static IEnumerable<T> FilterFeatured<T>(IEnumerable<T> records, bool? featured, Func<T, bool> selector) => featured.HasValue ? records.Where(item => selector(item) == featured.Value) : records;
}
