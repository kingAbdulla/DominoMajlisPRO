namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public sealed class StoreAdminSection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Icon { get; set; } = "✦";
    public StoreCardTemplateType TemplateType { get; set; }
    public StoreTextLimitRule TextLimits { get; set; } = new();
    public StoreImageRule ImageRule { get; set; } = new();
    public int SortOrder { get; set; }
    public StoreContentStatus Status { get; set; } = StoreContentStatus.Published;
}

public sealed class StoreAdminContentItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int Price { get; set; }
    public string Currency { get; set; } = "Gems";
    public string Category { get; set; } = string.Empty;
    public string SeasonId { get; set; } = string.Empty;
    public StoreContentStatus Status { get; set; } = StoreContentStatus.Draft;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string GalleryItemId { get; set; } = string.Empty;
}
