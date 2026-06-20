using System.Text.Json.Serialization;

namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public enum StoreCategoryStatus
{
    Draft = 0,
    Published = 1,
    Hidden = 2
}

public sealed class StoreCategoryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string BannerPath { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#D4AF37";
    public string Category { get; set; } = string.Empty;
    public string Collection { get; set; } = string.Empty;
    public string SeasonId { get; set; } = string.Empty;

    [JsonPropertyName("ParentCategoryId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyParentCategoryId
    {
        get => null;
        set
        {
            if (string.IsNullOrWhiteSpace(Collection) && !string.IsNullOrWhiteSpace(value))
                Collection = value;
        }
    }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int ItemCount { get; set; }
    public StoreCategoryStatus Status { get; set; } = StoreCategoryStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}
