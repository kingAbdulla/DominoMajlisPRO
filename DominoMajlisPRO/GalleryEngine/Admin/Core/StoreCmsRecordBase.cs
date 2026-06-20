namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public enum StoreCmsStatus { Draft = 0, Published = 1, Hidden = 2 }

public abstract class StoreCmsRecordBase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public StoreCmsStatus Status { get; set; } = StoreCmsStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVisible { get; set; } = true;
}
