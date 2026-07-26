using DominoMajlisPRO.Features.RechargeCenter.Catalogs;
using DominoMajlisPRO.Features.RechargeCenter.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Core;

namespace DominoMajlisPRO.Features.RechargeCenter.Services;

public static class RechargeCatalogService
{
    private const string FileName = "recharge_catalog.json";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string StoragePath => Path.Combine(FileSystem.AppDataDirectory, FileName);

    public static async Task<RechargeCatalogModel> LoadAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var stored = await StoreCmsJsonRepository.LoadListAsync<RechargeCatalogModel>(StoragePath);
            var catalog = Repair(stored.FirstOrDefault(), RechargeDefaultCatalog.Create());
            await StoreCmsJsonRepository.SaveListAsync(StoragePath, new[] { catalog });
            return catalog;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static RechargeCatalogModel Repair(RechargeCatalogModel? current, RechargeCatalogModel defaults)
    {
        if (current == null) return defaults;
        current.Packages = MergeById(current.Packages, defaults.Packages, x => x.PackageId);
        current.Offers = MergeById(current.Offers, defaults.Offers, x => x.OfferId);
        current.PaymentMethods = MergeById(current.PaymentMethods, defaults.PaymentMethods, x => x.PaymentMethodId);
        current.FirstRechargeRewards = MergeById(current.FirstRechargeRewards, defaults.FirstRechargeRewards, x => x.RewardId);
        current.ProgressRewards = MergeById(current.ProgressRewards, defaults.ProgressRewards, x => x.RewardId);
        current.FaqItems ??= defaults.FaqItems;
        if (current.FaqItems.Count == 0) current.FaqItems = defaults.FaqItems;
        if (string.IsNullOrWhiteSpace(current.VipPlan?.PlanId)) current.VipPlan = defaults.VipPlan;
        if (string.IsNullOrWhiteSpace(current.StoreVersion)) current.StoreVersion = defaults.StoreVersion;
        if (string.IsNullOrWhiteSpace(current.StoreId)) current.StoreId = defaults.StoreId;
        NormalizeForProductionSafety(current, defaults);
        return current;
    }

    private static void NormalizeForProductionSafety(RechargeCatalogModel catalog, RechargeCatalogModel defaults)
    {
        foreach (var package in catalog.Packages)
        {
            var safeDefault = defaults.Packages.FirstOrDefault(x => x.PackageId == package.PackageId);
            package.InternalProductId = ValueOrFallback(package.InternalProductId, package.PackageId);
            package.AndroidProductId = ValueOrFallback(package.AndroidProductId, safeDefault?.AndroidProductId ?? $"dmpro.{package.PackageId}");
            package.FutureIosProductId = ValueOrFallback(package.FutureIosProductId, safeDefault?.FutureIosProductId ?? package.AndroidProductId);
            package.ProductType = ValueOrFallback(package.ProductType, "Consumable");
            package.ServerEntitlementType = ValueOrFallback(package.ServerEntitlementType, "Gems");
            package.CatalogVersion = ValueOrFallback(package.CatalogVersion, "1");
            package.RegionAvailability = ValueOrFallback(package.RegionAvailability, "Global");
            package.PriceText = ValueOrFallback(package.PlatformPriceText, "Google Play");
            package.CurrencyCode = "Platform";
            package.IsActive = package.IsVisible && package.IsActive;
        }

        foreach (var offer in catalog.Offers)
        {
            var safeDefault = defaults.Offers.FirstOrDefault(x => x.OfferId == offer.OfferId);
            offer.InternalProductId = ValueOrFallback(offer.InternalProductId, offer.OfferId);
            offer.AndroidProductId = ValueOrFallback(offer.AndroidProductId, safeDefault?.AndroidProductId ?? $"dmpro.{offer.OfferId}");
            offer.FutureIosProductId = ValueOrFallback(offer.FutureIosProductId, safeDefault?.FutureIosProductId ?? offer.AndroidProductId);
            offer.ProductType = ValueOrFallback(offer.ProductType, "Consumable");
            offer.ServerEntitlementType = ValueOrFallback(offer.ServerEntitlementType, "Bundle");
            offer.OldPriceText = string.Empty;
            offer.NewPriceText = "Google Play";
            offer.IsActive = offer.IsVisible && offer.IsActive;
        }

        catalog.VipPlan.InternalProductId = ValueOrFallback(catalog.VipPlan.InternalProductId, catalog.VipPlan.PlanId);
        catalog.VipPlan.AndroidProductId = ValueOrFallback(catalog.VipPlan.AndroidProductId, defaults.VipPlan.AndroidProductId);
        catalog.VipPlan.FutureIosProductId = ValueOrFallback(catalog.VipPlan.FutureIosProductId, defaults.VipPlan.FutureIosProductId);
        catalog.VipPlan.ProductType = ValueOrFallback(catalog.VipPlan.ProductType, "Subscription");
        catalog.VipPlan.ServerEntitlementType = ValueOrFallback(catalog.VipPlan.ServerEntitlementType, "Vip");
        catalog.VipPlan.MonthlyPriceText = "Google Play";

        foreach (var method in catalog.PaymentMethods)
            method.IsEnabled = string.Equals(method.PaymentMethodId, "google-play", StringComparison.OrdinalIgnoreCase);
    }

    private static List<T> MergeById<T>(IEnumerable<T>? current, IEnumerable<T> defaults, Func<T, string> idSelector)
    {
        var result = (current ?? Array.Empty<T>())
            .Where(x => !string.IsNullOrWhiteSpace(idSelector(x)))
            .GroupBy(idSelector, StringComparer.Ordinal)
            .Select(x => x.First())
            .ToList();
        var ids = result.Select(idSelector).ToHashSet(StringComparer.Ordinal);
        result.AddRange(defaults.Where(x => ids.Add(idSelector(x))));
        return result;
    }

    public static IEnumerable<RechargePackageModel> VisiblePackages(RechargeCatalogModel catalog) =>
        catalog.Packages.Where(x => x.IsVisible && x.IsActive).OrderBy(x => x.SortOrder);

    public static IEnumerable<RechargeOfferModel> VisibleOffers(RechargeCatalogModel catalog) =>
        catalog.Offers.Where(x => x.IsVisible && x.IsActive && x.EndsAtUtc > DateTime.UtcNow).OrderBy(x => x.SortOrder);

    public static IEnumerable<PaymentMethodModel> VisiblePaymentMethods(RechargeCatalogModel catalog) =>
        catalog.PaymentMethods.OrderBy(x => x.SortOrder);

    private static string ValueOrFallback(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
