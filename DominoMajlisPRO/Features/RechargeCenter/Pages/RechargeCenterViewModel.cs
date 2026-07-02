using System.ComponentModel;
using System.Runtime.CompilerServices;
using DominoMajlisPRO.Features.RechargeCenter.Models;
using DominoMajlisPRO.Features.RechargeCenter.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Features.RechargeCenter.Pages;

public sealed class RechargeCenterViewModel : INotifyPropertyChanged
{
    private RechargeWalletModel _wallet = new();
    private string _selectedPaymentMethodId = "google-play";
    private int _totalPurchasedGems;
    private bool _isBusy;

    public string PlayerId { get; private set; } = string.Empty;
    public RechargeCatalogModel Catalog { get; private set; } = new();
    public IReadOnlyList<RechargePackageModel> Packages { get; private set; } = [];
    public IReadOnlyList<RechargeOfferModel> Offers { get; private set; } = [];
    public IReadOnlyList<PaymentMethodModel> PaymentMethods { get; private set; } = [];
    public IReadOnlyList<PurchaseHistoryItemModel> RecentPurchases { get; private set; } = [];
    public IReadOnlyList<RechargeProgressRewardModel> ProgressRewards => Catalog.ProgressRewards;
    public IReadOnlyList<RechargeRewardModel> FirstRechargeRewards => Catalog.FirstRechargeRewards;
    public IReadOnlyList<RechargeFaqItemModel> FaqItems => Catalog.FaqItems;
    public RechargeVipPlanModel VipPlan => Catalog.VipPlan;
    public RechargeWalletModel Wallet
    {
        get => _wallet;
        private set { _wallet = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoinsText)); OnPropertyChanged(nameof(GemsText)); }
    }
    public string CoinsText => Wallet.Coins.ToString("N0");
    public string GemsText => Wallet.Gems.ToString("N0");
    public int TotalPurchasedGems
    {
        get => _totalPurchasedGems;
        private set { _totalPurchasedGems = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); OnPropertyChanged(nameof(ProgressValue)); }
    }
    public string ProgressText => $"{TotalPurchasedGems:N0} / 1,000";
    public double ProgressValue => Math.Clamp(TotalPurchasedGems / 1000d, 0, 1);
    public string SelectedPaymentMethodId
    {
        get => _selectedPaymentMethodId;
        private set { _selectedPaymentMethodId = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedPaymentText)); }
    }
    public string SelectedPaymentText =>
        PaymentMethods.FirstOrDefault(x => x.PaymentMethodId == SelectedPaymentMethodId)?.Name ?? "غير محدد";
    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    public async Task<string?> InitializeAsync()
    {
        if (IsBusy) return null;
        IsBusy = true;
        try
        {
            var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
            if (string.IsNullOrWhiteSpace(owner.PlayerId))
                return "اختر أو أنشئ ملف لاعب قبل فتح مركز الشحن.";
            PlayerId = owner.PlayerId;
            Catalog = await RechargeCatalogService.LoadAsync();
            Packages = RechargeCatalogService.VisiblePackages(Catalog).ToList();
            Offers = RechargeCatalogService.VisibleOffers(Catalog).ToList();
            PaymentMethods = RechargeCatalogService.VisiblePaymentMethods(Catalog).ToList();
            Wallet = await RechargeWalletService.GetOrCreateAsync(PlayerId);
            await RefreshPurchaseStateAsync();
            RaiseCatalogProperties();
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            return "تعذر تحميل مركز الشحن بأمان. حاول مرة أخرى.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SelectPaymentMethod(PaymentMethodModel method)
    {
        if (method.IsEnabled) SelectedPaymentMethodId = method.PaymentMethodId;
    }

    public async Task<RechargeOperationResult> PurchasePackageAsync(RechargePackageModel package) =>
        await ExecuteAndRefreshAsync(() => RechargePurchaseService.PurchasePackageAsync(PlayerId, package, SelectedPaymentMethodId));

    public async Task<RechargeOperationResult> PurchaseOfferAsync(RechargeOfferModel offer) =>
        await ExecuteAndRefreshAsync(() => RechargePurchaseService.PurchaseOfferAsync(PlayerId, offer, SelectedPaymentMethodId));

    public async Task<RechargeOperationResult> SubscribeVipAsync() =>
        await ExecuteAndRefreshAsync(() => RechargePurchaseService.SubscribeVipAsync(PlayerId, VipPlan, SelectedPaymentMethodId));

    public async Task<RechargeOperationResult> ClaimFirstRechargeAsync() =>
        await ExecuteAndRefreshAsync(() => RechargePurchaseService.ClaimFirstRechargeAsync(PlayerId, FirstRechargeRewards));

    public async Task<RechargeOperationResult> ClaimProgressAsync(RechargeProgressRewardModel reward) =>
        await ExecuteAndRefreshAsync(() => RechargePurchaseService.ClaimProgressRewardAsync(PlayerId, reward));

    public async Task<IReadOnlyList<PurchaseHistoryItemModel>> GetHistoryAsync() =>
        await RechargePurchaseService.GetHistoryAsync(PlayerId);

    private async Task<RechargeOperationResult> ExecuteAndRefreshAsync(Func<Task<RechargeOperationResult>> action)
    {
        if (IsBusy) return new(false, "يرجى انتظار اكتمال العملية الحالية.");
        IsBusy = true;
        try
        {
            var result = await action();
            if (result.Wallet != null) Wallet = result.Wallet;
            await RefreshPurchaseStateAsync();
            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or OverflowException)
        {
            return new(false, "لم تكتمل العملية. لم يتم خصم أو فقدان أي رصيد.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshPurchaseStateAsync()
    {
        var history = await RechargePurchaseService.GetHistoryAsync(PlayerId);
        RecentPurchases = history.Take(3).ToList();
        TotalPurchasedGems = history.Sum(x => Math.Max(0, x.GemsGranted));
        var claimed = await RechargePurchaseService.GetClaimedRewardIdsAsync(PlayerId);
        foreach (var reward in Catalog.FirstRechargeRewards)
            reward.IsClaimed = claimed.Contains("first-recharge");
        foreach (var reward in Catalog.ProgressRewards)
            reward.IsClaimed = claimed.Contains(reward.RewardId);
        OnPropertyChanged(nameof(RecentPurchases));
        OnPropertyChanged(nameof(ProgressRewards));
        OnPropertyChanged(nameof(FirstRechargeRewards));
    }

    private void RaiseCatalogProperties()
    {
        OnPropertyChanged(nameof(Catalog));
        OnPropertyChanged(nameof(Packages));
        OnPropertyChanged(nameof(Offers));
        OnPropertyChanged(nameof(PaymentMethods));
        OnPropertyChanged(nameof(VipPlan));
        OnPropertyChanged(nameof(ProgressRewards));
        OnPropertyChanged(nameof(FirstRechargeRewards));
        OnPropertyChanged(nameof(FaqItems));
        OnPropertyChanged(nameof(SelectedPaymentText));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
