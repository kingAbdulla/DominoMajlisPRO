namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargePackageModel
{
    public string PackageId { get; set; } = string.Empty;
    public string InternalProductId { get; set; } = string.Empty;
    public string AndroidProductId { get; set; } = string.Empty;
    public string FutureIosProductId { get; set; } = string.Empty;
    public string ProductType { get; set; } = "Consumable";
    public string ServerEntitlementType { get; set; } = "Gems";
    public string CatalogVersion { get; set; } = "1";
    public string RegionAvailability { get; set; } = "Global";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int GemsAmount { get; set; }
    public int BonusGems { get; set; }
    public string PriceText { get; set; } = string.Empty;
    public string PlatformPriceText { get; set; } = string.Empty;
    public string PlatformTitle { get; set; } = string.Empty;
    public string PlatformDescription { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public bool IsMostPopular { get; set; }
    public bool IsBestValue { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string IconKey { get; set; } = "Gems";

    public int TotalGems => GemsAmount + BonusGems;
    public string EffectiveInternalProductId => string.IsNullOrWhiteSpace(InternalProductId) ? PackageId : InternalProductId;
    public string EffectiveDisplayPrice => !string.IsNullOrWhiteSpace(PlatformPriceText) ? PlatformPriceText : PriceText;
    public string BonusText => BonusGems > 0 ? $"+{BonusGems:N0} bonus" : "No bonus";
    public string BadgeText => IsMostPopular ? "MOST POPULAR" : IsBestValue ? "BEST VALUE" : string.Empty;
}
