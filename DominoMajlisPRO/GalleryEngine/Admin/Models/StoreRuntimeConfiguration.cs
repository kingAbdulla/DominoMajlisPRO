namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public sealed class StoreRuntimeConfiguration
{
    public string Id { get; set; } = "runtime";
    public bool IsStoreEnabled { get; set; } = true;
    public bool ShowNewArrivals { get; set; } = true;
    public bool ShowLimitedOffers { get; set; } = true;
    public bool ShowBrowseCategories { get; set; } = true;
    public int PageSize { get; set; } = 12;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
