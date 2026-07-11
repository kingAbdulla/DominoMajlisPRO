using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Pages;

internal sealed class WheelOfFortunePage : ContentPage
{
    private readonly VerticalStackLayout _body = new()
    {
        Padding = new Thickness(18, 18, 18, 28),
        Spacing = 14
    };

    private readonly Grid _wheel = new()
    {
        WidthRequest = 240,
        HeightRequest = 240,
        HorizontalOptions = LayoutOptions.Center
    };

    private readonly Label _status = Text("محاولة مجانية يومياً لكل لاعب، والمحاولة الإضافية بـ 10 جواهر.");
    private readonly Label _accountState = Text("جارٍ تحميل حالة المحاولة...");
    private readonly Label _history = Text("لا يوجد سجل دوران بعد.");
    private readonly Button _spinButton = Action("ابدأ");

    public WheelOfFortunePage()
    {
        Title = "عجلة الحظ";
        FlowDirection = FlowDirection.RightToLeft;
        NavigationPage.SetHasNavigationBar(this, false);
        Background = GalleryThemeEngine.Current.Background;

        BuildWheel();
        _spinButton.Clicked += OnSpin;

        _body.Children.Add(Header());
        _body.Children.Add(_wheel);
        _body.Children.Add(Text(string.Join(" • ", StoreFeatureService.RewardsPool.Select(reward => reward.DisplayName))));
        _body.Children.Add(_status);
        _body.Children.Add(_accountState);
        _body.Children.Add(_spinButton);
        _body.Children.Add(_history);
        Content = new ScrollView { Content = _body };

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private View Header()
    {
        var back = new Button
        {
            Text = "‹",
            WidthRequest = 42,
            HeightRequest = 42,
            CornerRadius = 14,
            BackgroundColor = Colors.Transparent,
            BorderColor = GalleryThemeEngine.Current.Stroke,
            TextColor = GalleryThemeEngine.Current.Gold,
            FontSize = 24
        };
        back.Clicked += async (_, _) => await Navigation.PopAsync();

        var title = Text("عجلة الحظ");
        title.FontSize = 26;
        title.FontAttributes = FontAttributes.Bold;

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(back, 0);
        grid.Add(title, 1);
        return grid;
    }

    private void BuildWheel()
    {
        var surface = new Border
        {
            WidthRequest = 220,
            HeightRequest = 220,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Background = GalleryThemeEngine.Current.CardBackground,
            Stroke = GalleryThemeEngine.Current.Gold,
            StrokeThickness = 6,
            StrokeShape = new RoundRectangle { CornerRadius = 110 },
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(GalleryThemeEngine.Current.Glow),
                Radius = 24,
                Opacity = 0.35f
            }
        };
        _wheel.Add(surface);

        var labels = StoreFeatureService.RewardsPool.Take(6).Select(reward => reward.DisplayName).ToArray();
        var positions = new[]
        {
            new Point(0.5, 0.08),
            new Point(0.82, 0.28),
            new Point(0.82, 0.72),
            new Point(0.5, 0.92),
            new Point(0.18, 0.72),
            new Point(0.18, 0.28)
        };

        for (var index = 0; index < labels.Length; index++)
        {
            var label = Text(labels[index]);
            label.FontSize = 11;
            label.WidthRequest = 76;
            label.TranslationX = (positions[index].X - 0.5) * 150;
            label.TranslationY = (positions[index].Y - 0.5) * 150;
            _wheel.Add(label);
        }

        _wheel.Add(new Label
        {
            Text = "◆",
            FontSize = 32,
            TextColor = GalleryThemeEngine.Current.Gold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            InputTransparent = true
        });
    }

    private async void OnSpin(object? sender, EventArgs e)
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (!owner.HasPlayerProfile || string.IsNullOrWhiteSpace(owner.PlayerId))
        {
            await DisplayAlertAsync("تنبيه", "يلزم اختيار حساب لاعب لاستخدام العجلة.", "حسناً");
            return;
        }

        _spinButton.IsEnabled = false;
        _status.Text = "تدور العجلة...";
        try
        {
            var result = await StoreFeatureService.SpinWheelAsync(owner.PlayerId);
            if (result.Success)
            {
                await _wheel.RotateToAsync(
                    _wheel.Rotation + 1440 + Random.Shared.Next(0, 360),
                    1800,
                    Easing.CubicOut);
            }

            _status.Text = result.Message;
            await RefreshAsync();
        }
        finally
        {
            _spinButton.IsEnabled = true;
        }
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        AppEvents.StoreEconomyChanged -= OnPlayerStoreChanged;
        AppEvents.StoreEconomyChanged += OnPlayerStoreChanged;
        AppEvents.PlayerProfileChanged -= OnPlayerProfileChanged;
        AppEvents.PlayerProfileChanged += OnPlayerProfileChanged;
        await RefreshAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        AppEvents.StoreEconomyChanged -= OnPlayerStoreChanged;
        AppEvents.PlayerProfileChanged -= OnPlayerProfileChanged;
    }

    private void OnPlayerStoreChanged(string playerId) => _ = RefreshForPlayerAsync(playerId);
    private void OnPlayerProfileChanged() => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (!owner.HasPlayerProfile || string.IsNullOrWhiteSpace(owner.PlayerId))
        {
            _accountState.Text = "يلزم حساب لاعب لاستخدام العجلة.";
            _history.Text = "لا يوجد سجل دوران.";
            _spinButton.IsEnabled = false;
            return;
        }

        await RefreshForPlayerAsync(owner.PlayerId);
    }

    private async Task RefreshForPlayerAsync(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return;

        var wallet = await PlayerWalletService.GetOrCreateAsync(playerId);
        var freeSpin = await StoreFeatureService.CanUseFreeSpinAsync(playerId);
        var history = await StoreFeatureService.GetWheelHistoryAsync(playerId, 5);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _accountState.Text = freeSpin
                ? $"المحاولة اليومية متاحة • 💎 {wallet.Gems:N0} • 🪙 {wallet.Coins:N0}"
                : $"المحاولة التالية مقابل 10 جواهر • 💎 {wallet.Gems:N0} • 🪙 {wallet.Coins:N0}";
            _spinButton.Text = freeSpin ? "ابدأ مجاناً" : "دور مقابل 10 جواهر";
            _spinButton.IsEnabled = freeSpin || wallet.Gems >= 10;
            _history.Text = history.Count == 0
                ? "لا يوجد سجل دوران بعد."
                : string.Join("\n", history.Select(item =>
                    $"{item.RewardName} • {item.SpunAtUtc.ToLocalTime():yyyy/MM/dd HH:mm}"));
        });
    }

    private static Label Text(string value) => new()
    {
        Text = value,
        FontFamily = "Tajawal-Regular",
        FontSize = 15,
        TextColor = GalleryThemeEngine.Current.TextPrimary,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        LineBreakMode = LineBreakMode.WordWrap
    };

    private static Button Action(string text) => new()
    {
        Text = text,
        FontFamily = "Tajawal-Regular",
        FontAttributes = FontAttributes.Bold,
        BackgroundColor = GalleryThemeEngine.Current.Accent,
        TextColor = Colors.Black,
        CornerRadius = 14,
        HeightRequest = 48
    };
}
