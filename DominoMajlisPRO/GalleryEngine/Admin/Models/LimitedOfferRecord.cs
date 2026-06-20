namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public enum LimitedOfferStatus
{
    Draft = 0,
    Published = 1,
    Hidden = 2,
    Expired = 3
}

public enum LimitedOfferCurrencyType
{
    Coins = 0,
    Gems = 1,
    Free = 2
}

public sealed class LimitedOfferRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProductId { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string StoreTypeId { get; set; } = string.Empty;
    public string OwnerScope { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int OriginalPrice { get; set; }
    public int DiscountPrice { get; set; }
    public int DiscountPercent { get; set; }
    public LimitedOfferCurrencyType CurrencyType { get; set; } = LimitedOfferCurrencyType.Gems;
    public bool IsFree { get; set; }
    public DateTime StartsAt { get; set; } = DateTime.Now;
    public DateTime EndsAt { get; set; } = DateTime.Now.AddDays(7);
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public LimitedOfferStatus Status { get; set; } = LimitedOfferStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}
