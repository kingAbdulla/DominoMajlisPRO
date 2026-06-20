namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public sealed record StoreCmsStatistics(int Draft, int Published, int Hidden, int Featured, int Visible)
{
    public int Total => Draft + Published + Hidden;
    public static StoreCmsStatistics Calculate<T>(IEnumerable<T> records, Func<T, StoreCmsStatus> status, Func<T, bool> featured, Func<T, bool> visible)
    {
        var items = records.ToList();
        return new(items.Count(item => status(item) == StoreCmsStatus.Draft), items.Count(item => status(item) == StoreCmsStatus.Published), items.Count(item => status(item) == StoreCmsStatus.Hidden), items.Count(featured), items.Count(visible));
    }
}
