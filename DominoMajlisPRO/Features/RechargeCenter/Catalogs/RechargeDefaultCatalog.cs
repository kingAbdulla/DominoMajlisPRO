using DominoMajlisPRO.Features.RechargeCenter.Models;

namespace DominoMajlisPRO.Features.RechargeCenter.Catalogs;

public static class RechargeDefaultCatalog
{
    public static RechargeCatalogModel Create()
    {
        var now = DateTime.UtcNow;
        return new RechargeCatalogModel
        {
            Packages =
            [
                Package("small-gem-pack", "dmpro.gems.small", 80, 0, 1),
                Package("medium-gem-pack", "dmpro.gems.medium", 325, 25, 2),
                Package("large-gem-pack", "dmpro.gems.large", 660, 60, 3),
                Package("premium-gem-pack", "dmpro.gems.premium", 1800, 180, 4, popular: true),
                Package("royal-gem-pack", "dmpro.gems.royal", 3850, 385, 5, bestValue: true),
                Package("legend-gem-pack", "dmpro.gems.legend", 8100, 810, 6)
            ],
            Offers =
            [
                new RechargeOfferModel
                {
                    OfferId = "season-gem-bundle",
                    InternalProductId = "season-gem-bundle",
                    AndroidProductId = "dmpro.bundle.season_gems",
                    FutureIosProductId = "dmpro.bundle.season_gems",
                    Title = "Season Gem Bundle",
                    Subtitle = "Limited seasonal bundle",
                    GemsAmount = 3300,
                    BonusText = "+300 bonus",
                    DiscountText = "Limited",
                    OldPriceText = "",
                    NewPriceText = "Google Play",
                    EndsAtUtc = now.AddDays(7),
                    SortOrder = 1,
                    ThemeKey = "ruby"
                },
                new RechargeOfferModel
                {
                    OfferId = "current-season-pack",
                    InternalProductId = "current-season-pack",
                    AndroidProductId = "dmpro.bundle.current_season",
                    FutureIosProductId = "dmpro.bundle.current_season",
                    Title = "Current Season Pack",
                    Subtitle = "Season gems and bonuses",
                    GemsAmount = 1980,
                    BonusText = "+180 bonus",
                    DiscountText = "Season",
                    OldPriceText = "",
                    NewPriceText = "Google Play",
                    EndsAtUtc = now.AddDays(14),
                    SortOrder = 2,
                    ThemeKey = "purple"
                },
                new RechargeOfferModel
                {
                    OfferId = "coin-support-pack",
                    InternalProductId = "coin-support-pack",
                    AndroidProductId = "dmpro.bundle.coins",
                    FutureIosProductId = "dmpro.bundle.coins",
                    Title = "Coin Support Pack",
                    Subtitle = "Coin bundle",
                    CoinsAmount = 8000,
                    BonusText = "+1,500 bonus",
                    DiscountText = "Limited",
                    OldPriceText = "",
                    NewPriceText = "Google Play",
                    EndsAtUtc = now.AddDays(3),
                    SortOrder = 3,
                    ThemeKey = "gold"
                }
            ],
            VipPlan = new RechargeVipPlanModel
            {
                PlanId = "domino-vip-monthly",
                InternalProductId = "domino-vip-monthly",
                AndroidProductId = "dmpro.vip.monthly",
                FutureIosProductId = "dmpro.vip.monthly",
                Title = "DOMINO VIP",
                MonthlyPriceText = "Google Play",
                DailyGems = 150,
                MonthlyCoins = 50000,
                XpBonusPercent = 20,
                IncludesExclusiveFrame = true,
                IncludesExclusiveTitle = true
            },
            FirstRechargeRewards =
            [
                Reward("first-avatar", "Exclusive avatar", "Avatar", "Avatar"),
                Reward("first-frame", "Exclusive frame", "Frame", "Frame"),
                Reward("first-emblem", "Royal emblem", "Emblem", "Emblem"),
                Reward("first-effect", "Purple effect", "Effect", "Effect")
            ],
            ProgressRewards =
            [
                new RechargeProgressRewardModel { RewardId = "progress-300", RequiredGems = 300, Title = "20,000 coins", IconKey = "Coins" },
                new RechargeProgressRewardModel { RewardId = "progress-500", RequiredGems = 500, Title = "Exclusive frame", IconKey = "Frame" },
                new RechargeProgressRewardModel { RewardId = "progress-1000", RequiredGems = 1000, Title = "100 gems", IconKey = "Gems" }
            ],
            PaymentMethods =
            [
                Payment("google-play", "Google Play", "Play", 1, enabled: true),
                Payment("visa", "VISA", "Card", 2, enabled: false),
                Payment("mastercard", "MasterCard", "Card", 3, enabled: false),
                Payment("apple-pay", "Apple Pay", "Apple", 4, enabled: false),
                Payment("zain-cash", "Zain Cash", "Cash", 5, enabled: false),
                Payment("qi-card", "Qi Card", "Qi", 6, enabled: false)
            ],
            FaqItems =
            [
                Faq("How are gems delivered?", "Gems are delivered only after the backend verifies the platform transaction and grants the entitlement once.", 1),
                Faq("Can the app collect card details?", "No. Digital purchases must use the official platform billing UI. The app never stores card numbers or CVV values.", 2),
                Faq("Why is billing unavailable?", "Google Play products, a verification backend, and secure server credentials must be configured before real purchases can grant balance.", 3)
            ],
            StoreVersion = "3.0.0-commerce-safe",
            StoreId = "DM-PRO-001"
        };
    }

    private static RechargePackageModel Package(
        string internalId,
        string platformId,
        int gems,
        int bonus,
        int order,
        bool popular = false,
        bool bestValue = false) =>
        new()
        {
            PackageId = internalId,
            InternalProductId = internalId,
            AndroidProductId = platformId,
            FutureIosProductId = platformId,
            Title = $"{gems:N0} Gems",
            Description = "Consumable gem entitlement verified by the backend.",
            GemsAmount = gems,
            BonusGems = bonus,
            PriceText = "Google Play",
            SortOrder = order,
            IsMostPopular = popular,
            IsBestValue = bestValue
        };

    private static RechargeRewardModel Reward(string id, string title, string type, string icon) =>
        new() { RewardId = id, Title = title, RewardType = type, IconKey = icon };

    private static PaymentMethodModel Payment(string id, string name, string icon, int order, bool enabled) =>
        new() { PaymentMethodId = id, Name = name, IconKey = icon, SortOrder = order, IsEnabled = enabled };

    private static RechargeFaqItemModel Faq(string question, string answer, int order) =>
        new() { Question = question, Answer = answer, SortOrder = order };
}
