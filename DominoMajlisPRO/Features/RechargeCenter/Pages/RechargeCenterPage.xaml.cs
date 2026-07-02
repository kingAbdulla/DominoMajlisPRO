using DominoMajlisPRO.Features.RechargeCenter.Models;

namespace DominoMajlisPRO.Features.RechargeCenter.Pages;

public partial class RechargeCenterPage : ContentPage
{
    private readonly RechargeCenterViewModel _viewModel = new();
    private bool _initialized;

    public RechargeCenterPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
        SizeChanged += OnPageSizeChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_initialized) return;
        _initialized = true;
        var error = await _viewModel.InitializeAsync();
        if (!string.IsNullOrWhiteSpace(error))
        {
            await DisplayAlert("مركز الشحن", error, "حسناً");
            await GoBackAsync();
        }
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        var compact = Width > 0 && Width < 760;
        ConfigureHeaderGrid(compact);
        ConfigureTwoColumnGrid(OffersVipGrid, compact);
        ConfigureTwoColumnGrid(RewardsGrid, compact);
        ConfigureThreeColumnGrid(LowerGrid, compact);
    }

    private void ConfigureHeaderGrid(bool compact)
    {
        HeaderGrid.ColumnDefinitions.Clear();
        HeaderGrid.RowDefinitions.Clear();
        if (compact)
        {
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            HeaderGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            HeaderGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            HeaderGrid.SetColumn(HeaderGrid.Children[1], 0);
            HeaderGrid.SetColumnSpan(HeaderGrid.Children[1], 2);
            HeaderGrid.SetRow(HeaderGrid.Children[1], 0);
            HeaderGrid.SetColumn(HeaderGrid.Children[0], 0);
            HeaderGrid.SetRow(HeaderGrid.Children[0], 1);
            HeaderGrid.SetColumn(HeaderGrid.Children[2], 1);
            HeaderGrid.SetRow(HeaderGrid.Children[2], 1);
        }
        else
        {
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            HeaderGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (var index = 0; index < HeaderGrid.Children.Count; index++)
            {
                HeaderGrid.SetColumn(HeaderGrid.Children[index], index);
                HeaderGrid.SetColumnSpan(HeaderGrid.Children[index], 1);
                HeaderGrid.SetRow(HeaderGrid.Children[index], 0);
            }
        }
    }

    private static void ConfigureTwoColumnGrid(Grid grid, bool compact)
    {
        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Clear();
        if (compact)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            if (grid.Children.Count > 1)
            {
                grid.SetColumn(grid.Children[1], 0);
                grid.SetRow(grid.Children[1], 1);
            }
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            if (grid.Children.Count > 1)
            {
                grid.SetColumn(grid.Children[1], 1);
                grid.SetRow(grid.Children[1], 0);
            }
        }
    }

    private static void ConfigureThreeColumnGrid(Grid grid, bool compact)
    {
        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Clear();
        if (compact)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for (var index = 0; index < 3; index++)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                if (grid.Children.Count > index)
                {
                    grid.SetColumn(grid.Children[index], 0);
                    grid.SetRow(grid.Children[index], index);
                }
            }
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(0.8, GridUnitType.Star)));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (var index = 0; index < Math.Min(3, grid.Children.Count); index++)
            {
                grid.SetColumn(grid.Children[index], index);
                grid.SetRow(grid.Children[index], 0);
            }
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await GoBackAsync();

    private async Task GoBackAsync()
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
    }

    private async void OnHistoryClicked(object? sender, EventArgs e)
    {
        var history = await _viewModel.GetHistoryAsync();
        var message = history.Count == 0
            ? "لا توجد عمليات شراء حتى الآن."
            : string.Join("\n\n", history.Take(12).Select(x => $"{x.ItemTitle}  •  {x.PriceText}\n{x.Status}  •  {x.CreatedAtText}\nطريقة الدفع: {x.PaymentMethodId}\nالمرجع: {ShortRef(x.ProviderTransactionId)}"));
        await DisplayAlert("سجل المشتريات", message, "إغلاق");
    }

    private async void OnAddCoinsClicked(object? sender, EventArgs e)
    {
        var coinOffer = _viewModel.Offers.FirstOrDefault(x => x.CoinsAmount > 0);
        if (coinOffer != null)
        {
            await PageScroll.ScrollToAsync(OffersVipGrid, ScrollToPosition.Start, true);
            await DisplayAlert("شحن العملات", $"أقرب خيار شحن عملات متاح الآن هو: {coinOffer.Title} مقابل {coinOffer.NewPriceText}.", "حسناً");
            return;
        }

        await DisplayAlert("شحن العملات", "لا توجد باقة عملات منشورة حالياً.", "حسناً");
    }

    private async void OnAddGemsClicked(object? sender, EventArgs e)
    {
        await PageScroll.ScrollToAsync(PackagesSection, ScrollToPosition.Start, true);
    }

    private async void OnPackageClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: RechargePackageModel package }) return;
        var confirmed = await DisplayAlert(
            "تأكيد الشراء",
            $"شراء {package.TotalGems:N0} جوهرة مقابل {package.PriceText}?\nطريقة الدفع: {_viewModel.SelectedPaymentText}",
            "تأكيد",
            "إلغاء");
        if (!confirmed) return;
        await ShowResultAsync(await _viewModel.PurchasePackageAsync(package));
    }

    private async void OnOfferClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: RechargeOfferModel offer }) return;
        var confirmed = await DisplayAlert(
            "تأكيد العرض",
            $"شراء {offer.Title} مقابل {offer.NewPriceText}?\nطريقة الدفع: {_viewModel.SelectedPaymentText}",
            "تأكيد",
            "إلغاء");
        if (!confirmed) return;
        await ShowResultAsync(await _viewModel.PurchaseOfferAsync(offer));
    }

    private async void OnVipClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlert(
            "DOMINO VIP",
            $"تفعيل الاشتراك مقابل {_viewModel.VipPlan.MonthlyPriceText}?\nطريقة الدفع: {_viewModel.SelectedPaymentText}",
            "اشترك الآن",
            "إلغاء");
        if (!confirmed) return;
        await ShowResultAsync(await _viewModel.SubscribeVipAsync());
    }

    private async void OnFirstRechargeClicked(object? sender, EventArgs e) =>
        await ShowResultAsync(await _viewModel.ClaimFirstRechargeAsync());

    private async void OnProgressRewardClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: RechargeProgressRewardModel reward }) return;
        await ShowResultAsync(await _viewModel.ClaimProgressAsync(reward));
    }

    private async void OnPaymentMethodTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not PaymentMethodModel method) return;
        if (!method.IsEnabled)
        {
            await DisplayAlert("طريقة الدفع", "طريقة الدفع هذه غير متاحة حالياً.", "حسناً");
            return;
        }
        _viewModel.SelectPaymentMethod(method);
        var message = method.IsProductionReady
            ? $"تم اختيار {method.Name}."
            : $"تم اختيار {method.Name} في وضع Sandbox. الدفع الحقيقي يتطلب بيانات التاجر من المزود.";
        await DisplayAlert("طريقة الدفع", message, "حسناً");
    }

    private void OnFaqTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is RechargeFaqItemModel faq)
            faq.IsExpanded = !faq.IsExpanded;
    }

    private async void OnSupportClicked(object? sender, EventArgs e) =>
        await DisplayAlert(
            "الدعم الفني",
            "احتفظ بصورة من سجل العملية ورقم المرجع. عند ربط بوابة الدفع الحقيقية سيتم إرسال رقم العملية إلى الدعم تلقائياً.",
            "حسناً");

    private async Task ShowResultAsync(RechargeOperationResult result)
    {
        var message = result.Message;
        if (result.Payment != null && !string.IsNullOrWhiteSpace(result.Payment.ProviderTransactionId))
            message += $"\n\nرقم العملية: {ShortRef(result.Payment.ProviderTransactionId)}";
        await DisplayAlert(result.Success ? "تمت العملية" : "تعذر إكمال العملية", message, "حسناً");
    }

    private static string ShortRef(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "-";
        return value.Length <= 18 ? value : value[^18..];
    }
}
