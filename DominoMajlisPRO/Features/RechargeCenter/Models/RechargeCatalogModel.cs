namespace DominoMajlisPRO.Features.RechargeCenter.Models;

public sealed class RechargeCatalogModel
{
    public List<RechargePackageModel> Packages { get; set; } = new();
    public List<RechargeOfferModel> Offers { get; set; } = new();
    public RechargeVipPlanModel VipPlan { get; set; } = new();
    public List<RechargeRewardModel> FirstRechargeRewards { get; set; } = new();
    public List<RechargeProgressRewardModel> ProgressRewards { get; set; } = new();
    public List<PaymentMethodModel> PaymentMethods { get; set; } = new();
    public List<RechargeFaqItemModel> FaqItems { get; set; } = new();
    public string StoreVersion { get; set; } = "2.0.0";
    public string StoreId { get; set; } = "DM-PRO-001";
}
