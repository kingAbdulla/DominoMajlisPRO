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
        return current;
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
        catalog.Packages.Where(x => x.IsVisible).OrderBy(x => x.SortOrder);

    public static IEnumerable<RechargeOfferModel> VisibleOffers(RechargeCatalogModel catalog) =>
        catalog.Offers.Where(x => x.IsVisible && x.EndsAtUtc > DateTime.UtcNow).OrderBy(x => x.SortOrder);

    public static IEnumerable<PaymentMethodModel> VisiblePaymentMethods(RechargeCatalogModel catalog) =>
        catalog.PaymentMethods.OrderBy(x => x.SortOrder);
}
