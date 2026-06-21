namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargeVipPlanModel
{
    public string PlanId { get; set; } = string.Empty;
    public string Title { get; set; } = "DOMINO VIP";
    public string MonthlyPriceText { get; set; } = "$4.99 / شهر";
    public int DailyGems { get; set; }
    public int MonthlyCoins { get; set; }
    public int XpBonusPercent { get; set; }
    public bool IncludesExclusiveFrame { get; set; }
    public bool IncludesExclusiveTitle { get; set; }
    public bool IsVisible { get; set; } = true;
}
