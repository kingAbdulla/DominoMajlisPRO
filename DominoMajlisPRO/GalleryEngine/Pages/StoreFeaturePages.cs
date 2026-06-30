using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Pages;

internal abstract class StoreFeaturePageBase : ContentPage
{
    protected readonly VerticalStackLayout Body = new() { Padding = 18, Spacing = 12 };
    protected StoreFeaturePageBase(string title)
    {
        Title = title;
        BackgroundColor = Color.FromArgb("#080705");
        FlowDirection = FlowDirection.RightToLeft;
        Body.Children.Add(new Label { Text = title, FontFamily = "Tajawal-Regular", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E7C979"), HorizontalTextAlignment = TextAlignment.Center });
        Content = new ScrollView { Content = Body };
    }
    protected static Label Text(string value) => new() { Text = value, FontFamily = "Tajawal-Regular", FontSize = 15, TextColor = Color.FromArgb("#F3E5BD"), HorizontalTextAlignment = TextAlignment.Center };
    protected static Button Action(string text, EventHandler clicked)
    {
        var button = new Button { Text = text, FontFamily = "Tajawal-Regular", FontAttributes = FontAttributes.Bold, BackgroundColor = Color.FromArgb("#D4AF5A"), TextColor = Colors.Black, CornerRadius = 14 };
        button.Clicked += clicked;
        return button;
    }
    protected static string TypeFor(GalleryItem item)
    {
        var value = $"{item.Category} {item.Name}".ToLowerInvariant();
        if (value.Contains("avatar") || value.Contains("صورة")) return "Avatar";
        if (value.Contains("background") || value.Contains("خلف")) return "ProfileBackground";
        if (value.Contains("frame") || value.Contains("إطار")) return "Frame";
        if (value.Contains("effect") || value.Contains("مؤثر")) return "Effect";
        if (value.Contains("title") || value.Contains("لقب")) return "Title";
        if (value.Contains("badge") || value.Contains("شارة")) return "Badge";
        return "Avatar";
    }
    protected static InventoryProductContext Product(GalleryItem item) =>
        new(item.Id, item.Id, TypeFor(item), item.Price, item.Price <= 0, item.Currency, $"{item.Price} {item.Currency}", item.SeasonId);
}

#if false
internal sealed class StoreCartPage : StoreFeaturePageBase
{
    private string _playerId = string.Empty;
    public StoreCartPage() : base("سلة الشراء") { Loaded += async (_, _) => await ReloadAsync(); }
    private async Task ReloadAsync()
    {
        while (Body.Children.Count > 1)
            Body.Children.RemoveAt(Body.Children.Count - 1);
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (!owner.HasPlayerProfile) { Body.Children.Add(Text("سجّل الدخول بحساب لاعب أولاً.")); return; }
        _playerId = owner.PlayerId;
        var catalog = GalleryService.GetCatalog();
        var ids = await StoreFeatureService.GetCartAsync(_playerId);
        var selected = catalog.Items.Where(x => ids.Contains(x.Id, StringComparer.OrdinalIgnoreCase)).ToList();
        Body.Children.Add(Text(selected.Count == 0 ? "السلة فارغة. اختر من العناصر المتاحة أدناه." : $"العناصر: {selected.Count}   الإجمالي: {selected.Sum(x => x.Price):N0}"));
        foreach (var item in selected)
        {
            var remove = Action($"حذف — {item.Name}", async (_, _) => { await StoreFeatureService.RemoveFromCartAsync(_playerId, item.Id); await ReloadAsync(); });
            Body.Children.Add(remove);
        }
        if (selected.Count > 0) Body.Children.Add(Action("إتمام الشراء", async (_, _) => await CheckoutAsync(selected)));
        Body.Children.Add(Text("إضافة عنصر إلى السلة"));
        foreach (var item in catalog.Items.Where(x => !ids.Contains(x.Id, StringComparer.OrdinalIgnoreCase)).Take(12))
            Body.Children.Add(Action($"+ {item.Name} — {item.Price} {item.Currency}", async (_, _) => { await StoreFeatureService.AddToCartAsync(_playerId, item.Id); await ReloadAsync(); }));
    }
    private async Task CheckoutAsync(List<GalleryItem> items)
    {
        var purchased = 0;
        foreach (var item in items)
        {
            var result = await StoreCheckoutService.PurchaseAsync(Product(item));
            if (result.Success) { purchased++; await StoreFeatureService.RemoveFromCartAsync(_playerId, item.Id); }
        }
        await DisplayAlertAsync("سلة الشراء", $"تم شراء {purchased} من {items.Count} عنصر.", "حسنًا");
        await ReloadAsync();
    }
}

#endif

internal sealed class WheelOfFortunePage : StoreFeaturePageBase
{
    private readonly Label _status = Text("محاولة مجانية يوميًا لكل PlayerId، والمحاولة الإضافية بـ 10 جواهر.");
    private readonly Grid _wheel = new()
    {
        WidthRequest = 240,
        HeightRequest = 240,
        HorizontalOptions = LayoutOptions.Center
    };
    private readonly Button _spinButton;
    private readonly Label _accountState =
        Text("جارٍ تحميل حالة المحاولة…");
    private readonly Label _history =
        Text("لا يوجد سجل دوران بعد.");

    public WheelOfFortunePage() : base("عجلة الحظ")
    {
        BuildWheel();
        Body.Children.Add(_wheel);
        Body.Children.Add(Text(string.Join(
            " • ",
            StoreFeatureService.RewardsPool.Select(reward =>
                reward.DisplayName))));
        Body.Children.Add(_status);
        Body.Children.Add(_accountState);
        _spinButton = Action("ابدأ", OnSpin);
        Body.Children.Add(_spinButton);
        Body.Children.Add(_history);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void BuildWheel()
    {
        var surface = new Border
        {
            WidthRequest = 220,
            HeightRequest = 220,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            BackgroundColor = Color.FromArgb("#17120A"),
            Stroke = Color.FromArgb("#D4AF5A"),
            StrokeThickness = 6,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 110
            },
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#D4AF5A")),
                Radius = 24,
                Opacity = 0.35f
            }
        };
        _wheel.Add(surface);

        var labels = StoreFeatureService.RewardsPool
            .Take(6)
            .Select(reward => reward.DisplayName)
            .ToArray();
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
            label.HorizontalTextAlignment = TextAlignment.Center;
            label.AnchorX = positions[index].X;
            label.AnchorY = positions[index].Y;
            label.TranslationX = (positions[index].X - 0.5) * 150;
            label.TranslationY = (positions[index].Y - 0.5) * 150;
            _wheel.Add(label);
        }

        _wheel.Add(new Label
        {
            Text = "◆",
            FontSize = 32,
            TextColor = Color.FromArgb("#FFD76A"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            InputTransparent = true
        });
    }

    private async void OnSpin(object? sender, EventArgs e)
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (!owner.HasPlayerProfile)
        {
            await DisplayAlertAsync("تنبيه", "يلزم حساب لاعب.", "حسنًا");
            return;
        }

        _spinButton.IsEnabled = false;
        _status.Text = "تدور العجلة…";
        try
        {
            var result =
                await StoreFeatureService.SpinWheelAsync(owner.PlayerId);
            if (!result.Success)
            {
                _status.Text = result.Message;
                await RefreshAsync();
                return;
            }

            await _wheel.RotateToAsync(
                _wheel.Rotation + 1440 + Random.Shared.Next(0, 360),
                1800,
                Easing.CubicOut);
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

    private void OnPlayerStoreChanged(string playerId) =>
        _ = RefreshForPlayerAsync(playerId);

    private void OnPlayerProfileChanged() => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (!owner.HasPlayerProfile ||
            string.IsNullOrWhiteSpace(owner.PlayerId))
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
        var freeSpin =
            await StoreFeatureService.CanUseFreeSpinAsync(playerId);
        var history =
            await StoreFeatureService.GetWheelHistoryAsync(playerId, 5);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _accountState.Text = freeSpin
                ? $"المحاولة اليومية متاحة • 💎 {wallet.Gems:N0} • 🪙 {wallet.Coins:N0}"
                : $"المحاولة التالية مقابل 10 جواهر • 💎 {wallet.Gems:N0} • 🪙 {wallet.Coins:N0}";
            _spinButton.Text = freeSpin ? "ابدأ مجانًا" : "دور مقابل 10 جواهر";
            _spinButton.IsEnabled = freeSpin || wallet.Gems >= 10;
            _history.Text = history.Count == 0
                ? "لا يوجد سجل دوران بعد."
                : string.Join(
                    "\n",
                    history.Select(item =>
                        $"{item.RewardName} • {item.SpunAtUtc.ToLocalTime():yyyy/MM/dd HH:mm}"));
        });
    }
}

internal sealed class SeasonPassPage : StoreFeaturePageBase
{
    public SeasonPassPage() : base("بطاقة الموسم") { Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        var season = GalleryService.GetCurrentSeason();
        Body.Children.Add(Text($"الموسم: {season?.Title ?? "غير محدد"}\nالمسار المجاني • المسار المميز • المكافآت"));
        if (!owner.HasPlayerProfile || season == null) return;
        var owned = await StoreFeatureService.HasPremiumPassAsync(owner.PlayerId, season.Id);
        Body.Children.Add(Text(owned ? "PremiumSeasonPass = true" : "السعر: 250 جوهرة"));
        if (!owned) Body.Children.Add(Action("شراء Premium", async (_, _) =>
        {
            var debit = await PlayerWalletService.TryDebitAsync(owner.PlayerId, StorePurchaseCurrencyType.Gems, 250);
            if (!debit.Success) { await DisplayAlertAsync("الرصيد", "لا توجد جواهر كافية.", "حسنًا"); return; }
            await StoreFeatureService.SavePremiumPassAsync(owner.PlayerId, season.Id);
            await PlayerInventoryService.AddOwnedItemAsync(owner.PlayerId, $"season-pass-{season.Id}", "Badge", "SeasonPass", seasonId: season.Id);
            await DisplayAlertAsync("بطاقة الموسم", "تم تفعيل المسار المميز.", "حسنًا");
        }));
    }
}

internal sealed class ExclusiveContentPage : StoreFeaturePageBase
{
    public ExclusiveContentPage() : base("محتوى حصري") { Loaded += (_, _) => LoadItems(); }
    private void LoadItems()
    {
        foreach (var item in GalleryService.GetCatalog().Items.Where(x => x.IsLimited || x.Rarity.Contains("rare", StringComparison.OrdinalIgnoreCase) || x.Rarity.Contains("نادر")).Take(12))
            Body.Children.Add(Action($"{item.Name} — {item.Price} {item.Currency}", async (_, _) =>
            {
                var result = await StoreCheckoutService.PurchaseAsync(Product(item));
                await DisplayAlertAsync("محتوى حصري", result.Success ? "تم الشراء والإضافة إلى مقتنياتي." : result.Message, "حسنًا");
            }));
    }
}

internal sealed class SupportDeveloperPage : StoreFeaturePageBase
{
    public SupportDeveloperPage() : base("ادعم المطور")
    {
        AddPackage("small", "دعم صغير", 250);
        AddPackage("gold", "دعم ذهبي", 1000);
        AddPackage("legendary", "دعم أسطوري", 2500);
    }
    private void AddPackage(string id, string title, int coins) => Body.Children.Add(Action($"{title} — شراء محاكى", async (_, _) =>
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (!owner.HasPlayerProfile) return;
        await StoreFeatureService.SaveSupportPurchaseAsync(owner.PlayerId, id);
        await PlayerWalletService.CreditAsync(owner.PlayerId, coins: coins, gems: id == "legendary" ? 25 : 5);
        await PlayerInventoryService.AddOwnedItemAsync(owner.PlayerId, $"supporter-{id}", "Badge", "SupportDeveloper");
        await DisplayAlertAsync("شكرًا لدعمك", $"تم حفظ العملية ومنح لقب {title}.", "حسنًا");
    }));
}

internal sealed class AccountSecurityStorePage : StoreFeaturePageBase
{
    public AccountSecurityStorePage() : base("آمن حسابك") { Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    {
        var session = await ApplicationUserService.EnsureCurrentSessionAsync();
        Body.Children.Add(Text($"PlayerId: {session.PlayerId ?? "—"}\nApplicationUserId: {session.ApplicationUserId ?? "—"}\nحالة الحساب: {session.Role}\nآخر نشاط: {DateTime.Now:yyyy/MM/dd HH:mm}"));
        Body.Children.Add(Action("إنشاء Recovery Code", async (_, _) =>
            await DisplayAlertAsync("Recovery Code", $"DM-{Guid.NewGuid():N}".Substring(0, 15).ToUpperInvariant(), "إغلاق")));
        Body.Children.Add(Action("عرض Security Log", async (_, _) =>
            await DisplayAlertAsync("Security Log", "تم فتح الصفحة من حساب المستخدم الحالي دون كشف بيانات حساسة.", "إغلاق")));
    }
}

internal sealed class SeasonSelectorPage : StoreFeaturePageBase
{
    public SeasonSelectorPage(Func<GallerySeason, Task> selected) : base("اختيار الموسم")
    {
        var now = DateTime.Now;
        foreach (var season in GalleryService.GetSeasons())
        {
            var state = season.StartDate > now ? "قريبًا" : season.EndDate < now ? "مؤرشف" : "الحالي";
            Body.Children.Add(Action($"{season.Title} — {state}", async (_, _) => { await selected(season); await Navigation.PopAsync(); }));
        }
    }
}
