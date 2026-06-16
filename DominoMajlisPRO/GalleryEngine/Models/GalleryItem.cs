namespace DominoMajlisPRO.GalleryEngine.Models;

public class GalleryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string SeasonId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Category { get; set; } = "عام";

    public string Rarity { get; set; } = "عادي";

    public string Image { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Lore { get; set; } = string.Empty;

    public int Price { get; set; }

    public string Currency { get; set; } = "Gems";

    public bool IsNew { get; set; }

    public bool IsLimited { get; set; }

    public bool IsOwned { get; set; }

    public int? OldPrice { get; set; }

    public DateTime? LimitedUntil { get; set; }

    public List<string> RelatedItemIds { get; set; } = new();
}