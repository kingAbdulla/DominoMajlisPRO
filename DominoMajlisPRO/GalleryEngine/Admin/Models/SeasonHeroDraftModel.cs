// LEGACY MIGRATION: Kept only for backward compatibility. Current Season CMS uses CurrentSeasonRecord.
namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public sealed class SeasonHeroDraftModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SeasonName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);
    public StoreContentStatus Status { get; set; } = StoreContentStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}

