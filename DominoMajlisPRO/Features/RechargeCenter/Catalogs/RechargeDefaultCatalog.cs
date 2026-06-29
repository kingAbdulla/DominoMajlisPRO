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
                Package("gems-80", 80, 0, "$0.99", 1),
                Package("gems-325", 325, 25, "$1.99", 2),
                Package("gems-660", 660, 60, "$4.99", 3),
                Package("gems-1800", 1800, 180, "$9.99", 4, popular: true),
                Package("gems-3850", 3850, 385, "$19.99", 5, bestValue: true),
                Package("gems-8100", 8100, 810, "$39.99", 6)
            ],
            Offers =
            [
                new RechargeOfferModel
                {
                    OfferId = "offer-ramadan",
                    Title = "رمضان كريم",
                    Subtitle = "عرض موسمي محدود",
                    GemsAmount = 3300,
                    BonusText = "+300 مكافأة",
                    DiscountText = "خصم 40%",
                    OldPriceText = "$24.99",
                    NewPriceText = "$14.99",
                    EndsAtUtc = now.AddDays(7),
                    SortOrder = 1,
                    ThemeKey = "ruby"
                },
                new RechargeOfferModel
                {
                    OfferId = "offer-current-season",
                    Title = "الموسم الحالي",
                    Subtitle = "جواهر الموسم",
                    GemsAmount = 1980,
                    BonusText = "+180 مكافأة",
                    DiscountText = "خصم 30%",
                    OldPriceText = "$14.99",
                    NewPriceText = "$9.99",
                    EndsAtUtc = now.AddDays(14),
                    SortOrder = 2,
                    ThemeKey = "purple"
                },
                new RechargeOfferModel
                {
                    OfferId = "offer-coins",
                    Title = "عرض العملات",
                    Subtitle = "باقة العملات",
                    CoinsAmount = 8000,
                    BonusText = "+1,500 مكافأة",
                    DiscountText = "خصم 20%",
                    OldPriceText = "$6.99",
                    NewPriceText = "$4.99",
                    EndsAtUtc = now.AddDays(3),
                    SortOrder = 3,
                    ThemeKey = "gold"
                }
            ],
            VipPlan = new RechargeVipPlanModel
            {
                PlanId = "domino-vip-monthly",
                Title = "DOMINO VIP",
                MonthlyPriceText = "$4.99 / شهر",
                DailyGems = 150,
                MonthlyCoins = 50000,
                XpBonusPercent = 20,
                IncludesExclusiveFrame = true,
                IncludesExclusiveTitle = true
            },
            FirstRechargeRewards =
            [
                Reward("first-avatar", "صورة رمزية حصرية", "Avatar", "🧔"),
                Reward("first-frame", "إطار حصري", "Frame", "🖼️"),
                Reward("first-emblem", "شعار ملكي", "Emblem", "♛"),
                Reward("first-effect", "مؤثر بنفسجي", "Effect", "✨")
            ],
            ProgressRewards =
            [
                new RechargeProgressRewardModel { RewardId = "progress-300", RequiredGems = 300, Title = "20,000 عملة", IconKey = "🪙" },
                new RechargeProgressRewardModel { RewardId = "progress-500", RequiredGems = 500, Title = "إطار حصري", IconKey = "🖼️" },
                new RechargeProgressRewardModel { RewardId = "progress-1000", RequiredGems = 1000, Title = "100 جوهرة", IconKey = "💎" }
            ],
            PaymentMethods =
            [
                Payment("google-play", "Google Play", "▶", 1, "google-play-billing"),
                Payment("visa", "VISA", "VISA", 2, "card-gateway"),
                Payment("mastercard", "MasterCard", "MC", 3, "card-gateway"),
                Payment("apple-pay", "Apple Pay", "", 4, "apple-pay"),
                Payment("zain-cash", "Zain Cash", "Zain", 5, "zain-cash"),
                Payment("qi-card", "Qi Card", "Qi", 6, "qi-card")
            ],
            FaqItems =
            [
                Faq("كيف أستلم الجواهر؟", "تُضاف الجواهر إلى محفظتك فور اكتمال عملية الدفع في بيئة Sandbox. عند ربط بوابة حقيقية ستضاف فقط بعد موافقة المزود.", 1),
                Faq("هل الدفع حقيقي الآن؟", "الحالي Sandbox آمن للتطوير. الربط الحقيقي يحتاج مفاتيح التاجر الرسمية من Zain Cash أو بوابة بطاقات مثل MasterCard/VISA.", 2),
                Faq("كم يستغرق الشحن؟", "يتم تحديث الرصيد وسجل المشتريات مباشرة بعد موافقة عملية الدفع.", 3)
            ],
            StoreVersion = "2.1.0",
            StoreId = "DM-PRO-001"
        };
    }

    private static RechargePackageModel Package(string id, int gems, int bonus, string price, int order, bool popular = false, bool bestValue = false) =>
        new()
        {
            PackageId = id,
            Title = $"{gems:N0} Gems",
            GemsAmount = gems,
            BonusGems = bonus,
            PriceText = price,
            SortOrder = order,
            IsMostPopular = popular,
            IsBestValue = bestValue
        };

    private static RechargeRewardModel Reward(string id, string title, string type, string icon) =>
        new() { RewardId = id, Title = title, RewardType = type, IconKey = icon };

    private static PaymentMethodModel Payment(string id, string name, string icon, int order, string providerKey) =>
        new()
        {
            PaymentMethodId = id,
            Name = name,
            IconKey = icon,
            SortOrder = order,
            ProviderKey = providerKey,
            IsEnabled = true,
            IsProductionReady = false,
            StatusText = "Sandbox جاهز"
        };

    private static RechargeFaqItemModel Faq(string question, string answer, int order) =>
        new() { Question = question, Answer = answer, SortOrder = order };
}
