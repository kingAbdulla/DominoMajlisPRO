using DominoMajlisPRO.Features.RechargeCenter.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Features.RechargeCenter.Services;

public static class RechargePurchaseService
{
    private const string HistoryFileName = "recharge_purchase_history.json";
    private const string ClaimsFileName = "recharge_claimed_rewards.json";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string HistoryPath => Path.Combine(FileSystem.AppDataDirectory, HistoryFileName);
    private static string ClaimsPath => Path.Combine(FileSystem.AppDataDirectory, ClaimsFileName);

    public static async Task<RechargeOperationResult> PurchasePackageAsync(string playerId, RechargePackageModel package, string paymentMethodId)
    {
        if (package == null || !package.IsVisible)
            return new(false, "الباقة غير متاحة.");

        var payment = await RechargePaymentGatewayService.AuthorizeAsync(playerId, package.Title, package.PriceText, paymentMethodId);
        if (!payment.Approved)
            return new(false, payment.Message, null, payment);

        var wallet = await RechargeWalletService.AddGemsAsync(playerId, package.TotalGems);
        await SavePurchaseAsync(playerId, package.Title, package.PriceText, package.TotalGems, 0, payment);
        AppEvents.RaiseStoreEconomyChanged(playerId);
        return new(true, $"{payment.Message}\n\nتمت إضافة {package.TotalGems:N0} جوهرة إلى محفظتك.", wallet, payment);
    }

    public static async Task<RechargeOperationResult> PurchaseOfferAsync(string playerId, RechargeOfferModel offer, string paymentMethodId)
    {
        if (offer == null || !offer.IsVisible || offer.EndsAtUtc <= DateTime.UtcNow)
            return new(false, "انتهى هذا العرض أو لم يعد متاحاً.");

        var payment = await RechargePaymentGatewayService.AuthorizeAsync(playerId, offer.Title, offer.NewPriceText, paymentMethodId);
        if (!payment.Approved)
            return new(false, payment.Message, null, payment);

        RechargeWalletModel wallet;
        if (offer.GemsAmount > 0)
            wallet = await RechargeWalletService.AddGemsAsync(playerId, offer.GemsAmount);
        else
            wallet = await RechargeWalletService.AddCoinsAsync(playerId, offer.CoinsAmount);

        await SavePurchaseAsync(playerId, offer.Title, offer.NewPriceText, offer.GemsAmount, offer.CoinsAmount, payment);
        AppEvents.RaiseStoreEconomyChanged(playerId);
        return new(true, $"{payment.Message}\n\nتم شراء {offer.Title} وتحديث المحفظة.", wallet, payment);
    }

    public static async Task<RechargeOperationResult> SubscribeVipAsync(string playerId, RechargeVipPlanModel plan, string paymentMethodId)
    {
        var payment = await RechargePaymentGatewayService.AuthorizeAsync(playerId, plan.Title, plan.MonthlyPriceText, paymentMethodId);
        if (!payment.Approved)
            return new(false, payment.Message, null, payment);

        await Gate.WaitAsync();
        try
        {
            var claims = await LoadClaimsAsync();
            var state = GetClaimState(claims, playerId);
            if (state.VipSubscribed)
                return new(false, "اشتراك DOMINO VIP مفعّل بالفعل.");
            state.VipSubscribed = true;
            await StoreCmsJsonRepository.SaveListAsync(ClaimsPath, claims);
        }
        finally
        {
            Gate.Release();
        }

        var wallet = await RechargeWalletService.AddGemsAsync(playerId, plan.DailyGems);
        await SavePurchaseAsync(playerId, plan.Title, plan.MonthlyPriceText, plan.DailyGems, 0, payment);
        AppEvents.RaiseStoreEconomyChanged(playerId);
        return new(true, $"{payment.Message}\n\nتم تفعيل VIP وإضافة {plan.DailyGems:N0} جوهرة يومية أولى.", wallet, payment);
    }

    public static async Task<IReadOnlyList<PurchaseHistoryItemModel>> GetHistoryAsync(string playerId)
    {
        var history = await StoreCmsJsonRepository.LoadListAsync<PurchaseHistoryItemModel>(HistoryPath);
        return history.Where(x => Same(x.PlayerId, playerId)).OrderByDescending(x => x.CreatedAtUtc).ToList();
    }

    public static async Task<int> GetTotalPurchasedGemsAsync(string playerId) =>
        (await GetHistoryAsync(playerId)).Sum(x => Math.Max(0, x.GemsGranted));

    public static async Task<RechargeOperationResult> ClaimFirstRechargeAsync(string playerId, IReadOnlyList<RechargeRewardModel> rewards)
    {
        if ((await GetHistoryAsync(playerId)).Count == 0)
            return new(false, "أكمل أول عملية شحن لفتح هذه المكافآت.");
        return await ClaimAsync(playerId, "first-recharge", "تم استلام مكافآت أول عملية شحن.", async () =>
        {
            await RechargeWalletService.AddGemsAsync(playerId, 100);
        });
    }

    public static async Task<RechargeOperationResult> ClaimProgressRewardAsync(string playerId, RechargeProgressRewardModel reward)
    {
        var total = await GetTotalPurchasedGemsAsync(playerId);
        if (total < reward.RequiredGems)
            return new(false, $"تحتاج إلى شراء {reward.RequiredGems:N0} جوهرة لفتح المكافأة.");
        return await ClaimAsync(playerId, reward.RewardId, $"تم استلام: {reward.Title}", async () =>
        {
            if (reward.RewardId == "progress-300") await RechargeWalletService.AddCoinsAsync(playerId, 20000);
            if (reward.RewardId == "progress-1000") await RechargeWalletService.AddGemsAsync(playerId, 100);
        });
    }

    public static async Task<RechargeOperationResult> GrantWheelRewardAsync(string playerId)
    {
        var random = Random.Shared.Next(0, 100);
        if (random < 65)
        {
            var coins = Random.Shared.Next(500, 2501);
            var wallet = await RechargeWalletService.AddCoinsAsync(playerId, coins);
            await SaveSystemRewardAsync(playerId, "عجلة الحظ", $"+{coins:N0} عملة", 0, coins);
            AppEvents.RaiseStoreEconomyChanged(playerId);
            return new(true, $"ربحت {coins:N0} عملة من عجلة الحظ.", wallet);
        }
        else
        {
            var gems = Random.Shared.Next(5, 31);
            var wallet = await RechargeWalletService.AddGemsAsync(playerId, gems);
            await SaveSystemRewardAsync(playerId, "عجلة الحظ", $"+{gems:N0} جوهرة", gems, 0);
            AppEvents.RaiseStoreEconomyChanged(playerId);
            return new(true, $"ربحت {gems:N0} جوهرة من عجلة الحظ.", wallet);
        }
    }

    public static async Task<HashSet<string>> GetClaimedRewardIdsAsync(string playerId)
    {
        var claims = await LoadClaimsAsync();
        return new HashSet<string>(GetClaimState(claims, playerId).ClaimedRewardIds, StringComparer.Ordinal);
    }

    private static async Task<RechargeOperationResult> ClaimAsync(string playerId, string rewardId, string successMessage, Func<Task> grant)
    {
        await Gate.WaitAsync();
        try
        {
            var claims = await LoadClaimsAsync();
            var state = GetClaimState(claims, playerId);
            if (state.ClaimedRewardIds.Contains(rewardId))
                return new(false, "تم استلام هذه المكافأة مسبقاً.");
            await grant();
            state.ClaimedRewardIds.Add(rewardId);
            await StoreCmsJsonRepository.SaveListAsync(ClaimsPath, claims);
            AppEvents.RaiseStoreEconomyChanged(playerId);
            return new(true, successMessage, await RechargeWalletService.GetOrCreateAsync(playerId));
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task SavePurchaseAsync(string playerId, string itemTitle, string price, int gems, int coins, PaymentTransactionResult payment)
    {
        await SaveHistoryAsync(new PurchaseHistoryItemModel
        {
            PurchaseId = $"purchase-{Guid.NewGuid():N}",
            PlayerId = playerId,
            ItemTitle = itemTitle,
            PriceText = price,
            Status = payment.Status,
            CreatedAtUtc = DateTime.UtcNow,
            GemsGranted = gems,
            CoinsGranted = coins,
            PaymentMethodId = payment.PaymentMethodId,
            ProviderTransactionId = payment.ProviderTransactionId,
            IsSandboxPayment = payment.IsSandbox
        });
    }

    private static Task SaveSystemRewardAsync(string playerId, string itemTitle, string price, int gems, int coins) =>
        SaveHistoryAsync(new PurchaseHistoryItemModel
        {
            PurchaseId = $"reward-{Guid.NewGuid():N}",
            PlayerId = playerId,
            ItemTitle = itemTitle,
            PriceText = price,
            Status = "مكافأة",
            CreatedAtUtc = DateTime.UtcNow,
            GemsGranted = gems,
            CoinsGranted = coins,
            PaymentMethodId = "system-reward",
            ProviderTransactionId = "system",
            IsSandboxPayment = false
        });

    private static async Task SaveHistoryAsync(PurchaseHistoryItemModel item)
    {
        await Gate.WaitAsync();
        try
        {
            var history = await StoreCmsJsonRepository.LoadListAsync<PurchaseHistoryItemModel>(HistoryPath);
            history.Add(item);
            await StoreCmsJsonRepository.SaveListAsync(HistoryPath, history);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static Task<List<RechargeClaimState>> LoadClaimsAsync() =>
        StoreCmsJsonRepository.LoadListAsync<RechargeClaimState>(ClaimsPath);

    private static RechargeClaimState GetClaimState(List<RechargeClaimState> claims, string playerId)
    {
        var state = claims.FirstOrDefault(x => Same(x.PlayerId, playerId));
        if (state != null) return state;
        state = new RechargeClaimState { PlayerId = playerId };
        claims.Add(state);
        return state;
    }

    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
}
