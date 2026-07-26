namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargeOfferModel
{
    public string OfferId { get; set; } = string.Empty;
    public string InternalProductId { get; set; } = string.Empty;
    public string AndroidProductId { get; set; } = string.Empty;
    public string FutureIosProductId { get; set; } = string.Empty;
    public string ProductType { get; set; } = "Consumable";
    public string ServerEntitlementType { get; set; } = "Bundle";
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int GemsAmount { get; set; }
    public int CoinsAmount { get; set; }
    public string BonusText { get; set; } = string.Empty;
    public string DiscountText { get; set; } = string.Empty;
    public string OldPriceText { get; set; } = string.Empty;
    public string NewPriceText { get; set; } = string.Empty;
    public DateTime EndsAtUtc { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string ThemeKey { get; set; } = "gold";

    public string RewardText => GemsAmount > 0 ? $"{GemsAmount:N0} gems" : $"{CoinsAmount:N0} coins";
}
