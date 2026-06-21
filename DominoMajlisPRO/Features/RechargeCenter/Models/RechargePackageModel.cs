namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargePackageModel
{
    public string PackageId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int GemsAmount { get; set; }
    public int BonusGems { get; set; }
    public string PriceText { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public bool IsMostPopular { get; set; }
    public bool IsBestValue { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string IconKey { get; set; } = "💎";
    public int TotalGems => GemsAmount + BonusGems;
    public string BonusText => BonusGems > 0 ? $"+{BonusGems:N0} مكافأة" : "بدون مكافأة";
    public string BadgeText => IsMostPopular ? "MOST POPULAR" : IsBestValue ? "BEST VALUE" : string.Empty;
}
