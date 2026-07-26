using DominoMajlisPRO.Features.RechargeCenter.Models;
using DominoMajlisPRO.Backend;
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

    public static async Task<RechargeOperationResult> PurchasePackageAsync(
        string playerId,
        RechargePackageModel package,
        string paymentMethodId)
    {
        if (package == null || !package.IsVisible || !package.IsActive)
            return new(false, "الباقة غير متاحة حالياً.", await CurrentWalletAsync(playerId), RechargeBillingState.ProductUnavailable);

        return await BlockRealMoneyPurchaseUntilVerifiedAsync(
            playerId,
            package.Title,
            package.EffectiveDisplayPrice,
            package.EffectiveInternalProductId,
            package.AndroidProductId,
            "GemPackage",
            paymentMethodId);
    }

    public static async Task<RechargeOperationResult> PurchaseOfferAsync(
        string playerId,
        RechargeOfferModel offer,
        string paymentMethodId)
    {
        if (offer == null || !offer.IsVisible || !offer.IsActive || offer.EndsAtUtc <= DateTime.UtcNow)
            return new(false, "انتهى هذا العرض أو لم يعد متاحاً.", await CurrentWalletAsync(playerId), RechargeBillingState.ProductUnavailable);

        return await BlockRealMoneyPurchaseUntilVerifiedAsync(
            playerId,
            offer.Title,
            offer.NewPriceText,
            string.IsNullOrWhiteSpace(offer.InternalProductId) ? offer.OfferId : offer.InternalProductId,
            offer.AndroidProductId,
            "PaidOffer",
            paymentMethodId);
    }

    public static async Task<RechargeOperationResult> SubscribeVipAsync(
        string playerId,
        RechargeVipPlanModel plan,
        string paymentMethodId)
    {
        if (plan == null || !plan.IsVisible)
            return new(false, "اشتراك VIP غير متاح حالياً.", await CurrentWalletAsync(playerId), RechargeBillingState.ProductUnavailable);

        return await BlockRealMoneyPurchaseUntilVerifiedAsync(
            playerId,
            plan.Title,
            plan.MonthlyPriceText,
            string.IsNullOrWhiteSpace(plan.InternalProductId) ? plan.PlanId : plan.InternalProductId,
            plan.AndroidProductId,
            "VipSubscription",
            paymentMethodId);
    }

    public static async Task<IReadOnlyList<PurchaseHistoryItemModel>> GetHistoryAsync(string playerId)
    {
        var history = await StoreCmsJsonRepository.LoadListAsync<PurchaseHistoryItemModel>(HistoryPath);
        return history.Where(x => Same(x.PlayerId, playerId)).OrderByDescending(x => x.CreatedAtUtc).ToList();
    }

    public static async Task<int> GetTotalPurchasedGemsAsync(string playerId) =>
        (await GetHistoryAsync(playerId))
        .Where(x => Same(x.TransactionCategory, "RealMoneyPurchase") && Same(x.PurchaseState, "EntitlementGranted"))
        .Sum(x => Math.Max(0, x.GemsGranted));

    public static async Task<RechargeOperationResult> ClaimFirstRechargeAsync(
        string playerId,
        IReadOnlyList<RechargeRewardModel> rewards)
    {
        var hasGrantedPurchase = (await GetHistoryAsync(playerId))
            .Any(x => Same(x.TransactionCategory, "RealMoneyPurchase") && Same(x.PurchaseState, "EntitlementGranted"));
        if (!hasGrantedPurchase)
            return new(false, "أكمل عملية شحن مؤكدة من الخادم لفتح مكافآت أول شحن.", await CurrentWalletAsync(playerId), RechargeBillingState.VerificationPending);

        return await ClaimAsync(playerId, "first-recharge", "تم استلام مكافآت أول عملية شحن.", async () =>
        {
            await RechargeWalletService.AddPromotionalGemsAsync(playerId, 100, "FirstRechargeReward");
        });
    }

    public static async Task<RechargeOperationResult> ClaimProgressRewardAsync(
        string playerId,
        RechargeProgressRewardModel reward)
    {
        var total = await GetTotalPurchasedGemsAsync(playerId);
        if (total < reward.RequiredGems)
            return new(false, $"تحتاج إلى شحن مؤكد قدره {reward.RequiredGems:N0} جوهرة لفتح المكافأة.", await CurrentWalletAsync(playerId), RechargeBillingState.VerificationPending);

        return await ClaimAsync(playerId, reward.RewardId, $"تم استلام: {reward.Title}", async () =>
        {
            if (reward.RewardId == "progress-300") await RechargeWalletService.AddPromotionalCoinsAsync(playerId, 20000, "RechargeProgressReward");
            if (reward.RewardId == "progress-1000") await RechargeWalletService.AddPromotionalGemsAsync(playerId, 100, "RechargeProgressReward");
        });
    }

    public static async Task<HashSet<string>> GetClaimedRewardIdsAsync(string playerId)
    {
        var claims = await LoadClaimsAsync();
        return new HashSet<string>(
            GetClaimState(claims, playerId).ClaimedRewardIds,
            StringComparer.Ordinal);
    }

    public static async Task<RechargeOperationResult> RefreshAuthoritativeWalletAsync(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return new(false, "يجب اختيار ملف لاعب قبل تحديث المحفظة.", null, RechargeBillingState.AccountMismatch);

        var response = await new CommerceApiClient().LoadWalletAsync();
        if (!response.Success || response.Data == null)
            return new(false, response.Message, await CurrentWalletAsync(playerId), RechargeBillingState.ServerMaintenance);

        var wallet = await RechargeWalletService.SyncAuthoritativeAsync(
            playerId,
            response.Data.Coins,
            response.Data.Gems,
            response.Data.Version);

        return new(true, "تم تحديث المحفظة من الخادم.", wallet, RechargeBillingState.Verified);
    }

    private static async Task<RechargeOperationResult> BlockRealMoneyPurchaseUntilVerifiedAsync(
        string playerId,
        string itemTitle,
        string displayPrice,
        string internalProductId,
        string platformProductId,
        string productKind,
        string paymentMethodId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return new(false, "يجب اختيار ملف لاعب قبل الشحن.", null, RechargeBillingState.AccountMismatch);

        var wallet = await CurrentWalletAsync(playerId);
        var flags = await new FeatureFlagApiClient().LoadFlagsAsync();
        if (!flags.RealMoneyPurchasesEnabled)
        {
            var reference = await SavePurchaseAsync(
                playerId,
                itemTitle,
                displayPrice,
                internalProductId,
                platformProductId,
                paymentMethodId,
                "FeatureDisabled",
                "RealMoneyPurchasesKillSwitch",
                productKind);

            return new(
                false,
                "الشراء الحقيقي متوقف من الخادم حالياً. لم يتم إضافة أي جواهر أو عملات.",
                wallet,
                RechargeBillingState.NotConfigured,
                reference);
        }

        var supportReference = await SavePurchaseAsync(
            playerId,
            itemTitle,
            displayPrice,
            internalProductId,
            platformProductId,
            paymentMethodId,
            "VerificationBlocked",
            "ServerVerificationUnavailable",
            productKind);

        return new(
            false,
            "تم إيقاف الشراء الحقيقي مؤقتاً: لا توجد بوابة Google Play وخادم تحقق موثوقان لهذا المنتج. لم يتم إضافة أي جواهر أو عملات.",
            wallet,
            RechargeBillingState.NotConfigured,
            supportReference);
    }

    private static async Task<RechargeOperationResult> ClaimAsync(
        string playerId,
        string rewardId,
        string successMessage,
        Func<Task> grant)
    {
        await Gate.WaitAsync();
        try
        {
            var claims = await LoadClaimsAsync();
            var state = GetClaimState(claims, playerId);
            if (state.ClaimedRewardIds.Contains(rewardId))
                return new(false, "تم استلام هذه المكافأة مسبقاً.", await CurrentWalletAsync(playerId));
            await grant();
            state.ClaimedRewardIds.Add(rewardId);
            await StoreCmsJsonRepository.SaveListAsync(ClaimsPath, claims);
            return new(true, successMessage, await RechargeWalletService.GetOrCreateAsync(playerId));
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<string> SavePurchaseAsync(
        string playerId,
        string itemTitle,
        string price,
        string internalProductId,
        string platformProductId,
        string paymentMethodId,
        string purchaseState,
        string failureCategory,
        string productKind)
    {
        await Gate.WaitAsync();
        try
        {
            var owner = await ApplicationUserService.EnsureCurrentSessionAsync();
            var history = await StoreCmsJsonRepository.LoadListAsync<PurchaseHistoryItemModel>(HistoryPath);
            var internalPurchaseId = $"iap-{Guid.NewGuid():N}";
            history.Add(new PurchaseHistoryItemModel
            {
                PurchaseId = internalPurchaseId,
                InternalPurchaseId = internalPurchaseId,
                ApplicationUserId = owner.ApplicationUserId ?? string.Empty,
                PlayerId = playerId,
                ItemTitle = itemTitle,
                InternalProductId = internalProductId,
                Platform = "GooglePlay",
                PlatformProductId = platformProductId,
                PriceText = price,
                Status = purchaseState,
                PurchaseState = purchaseState,
                TransactionCategory = "RealMoneyPurchase",
                FailureCategory = failureCategory,
                IdempotencyKey = $"{playerId}:{internalProductId}:{internalPurchaseId}",
                CreatedAtUtc = DateTime.UtcNow,
                GemsGranted = 0,
                CoinsGranted = 0,
                PaymentMethodId = paymentMethodId,
                AuditMetadata = $"ProductKind={productKind};ClientGrantBlocked=True;RequiresServerVerification=True"
            });
            await StoreCmsJsonRepository.SaveListAsync(HistoryPath, history);
            return internalPurchaseId;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static Task<RechargeWalletModel> CurrentWalletAsync(string playerId) =>
        string.IsNullOrWhiteSpace(playerId)
            ? Task.FromResult(new RechargeWalletModel())
            : RechargeWalletService.GetOrCreateAsync(playerId);

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

    private static bool Same(string? left, string? right) => string.Equals(left, right, StringComparison.Ordinal);
}
