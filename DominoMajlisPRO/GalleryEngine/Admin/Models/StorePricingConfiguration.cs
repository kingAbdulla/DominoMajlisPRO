namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public sealed class StorePricingConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Currency { get; set; } = "Gems";
    public string PricingKind { get; set; } = "GemPack";
    public int Amount { get; set; }
    public decimal Price { get; set; }
    public int DiscountPercent { get; set; }
    public string SeasonId { get; set; } = string.Empty;
    public string OfferId { get; set; } = string.Empty;
    public string RegionCode { get; set; } = "GLOBAL";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
