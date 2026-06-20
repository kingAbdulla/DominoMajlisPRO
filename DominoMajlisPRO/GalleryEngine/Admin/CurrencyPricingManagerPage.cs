using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Canonical;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class CurrencyPricingManagerPage : ContentPage
{
    private readonly Picker _recordsPicker = new() { Title = "إعداد محفوظ" };
    private readonly Picker _currencyPicker = new() { Title = "العملة" };
    private readonly Picker _kindPicker = new() { Title = "نوع التسعير" };
    private readonly Entry _amountEntry = new() { Placeholder = "كمية الحزمة", Keyboard = Keyboard.Numeric };
    private readonly Entry _priceEntry = new() { Placeholder = "السعر", Keyboard = Keyboard.Numeric };
    private readonly Entry _discountEntry = new() { Placeholder = "نسبة الخصم", Keyboard = Keyboard.Numeric };
    private readonly Picker _seasonPicker = new() { Title = "SeasonId" };
    private readonly Picker _offerPicker = new() { Title = "OfferId" };
    private readonly Entry _regionEntry = new() { Placeholder = "RegionCode", Text = "GLOBAL" };
    private readonly Label _validation = new() { FontSize = 11, IsVisible = false, HorizontalTextAlignment = TextAlignment.End };
    private readonly Border _formPanel = new() { Padding = 12, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 18 } };
    private List<StorePricingConfiguration> _records = new();
    private StorePricingConfiguration? _current;

    public CurrencyPricingManagerPage()
    {
        BackgroundColor = Color.FromArgb("#030303");
        FlowDirection = FlowDirection.RightToLeft;
        NavigationPage.SetHasNavigationBar(this, false);
        _currencyPicker.SetOptions(CanonicalStoreCatalog.Currencies());
        _seasonPicker.SetOptions(CanonicalStoreCatalog.Seasons());
        _kindPicker.ItemsSource = new[] { "GemPack", "CoinPack", "SeasonPricing", "OfferPricing", "RegionalPricing" };
        _recordsPicker.SelectedIndexChanged += OnRecordSelected;
        BuildPage();
        ApplyTheme();
        ClearFields();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        await RefreshAsync();
        ApplyTheme();
    }

    protected override void OnDisappearing()
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    private void BuildPage()
    {
        var back = new Border { WidthRequest = 42, HeightRequest = 42, StrokeThickness = 1, StrokeShape = new RoundRectangle { CornerRadius = 16 }, Content = new Label { Text = "‹", FontSize = 28, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } };
        back.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Navigation.PopAsync()) });
        var heading = new VerticalStackLayout { Spacing = 1, Children = { new Label { Text = "العملات والأسعار", FontFamily = "Tajawal-Regular", FontSize = 25, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End }, new Label { Text = "إدارة الحزم والأسعار والخصومات بدون توجيه مخزون", FontFamily = "Tajawal-Regular", FontSize = 12, HorizontalTextAlignment = TextAlignment.End } } };
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 12 }; header.Add(back, 0); header.Add(heading, 1);
        _formPanel.Content = new VerticalStackLayout { Spacing = 10, Children = { _recordsPicker, _currencyPicker, _kindPicker, _amountEntry, _priceEntry, _discountEntry, _seasonPicker, _offerPicker, _regionEntry, _validation } };
        var save = new Button { Text = "حفظ إعداد التسعير" }; save.Clicked += async (_, _) => await SaveAsync();
        var clear = new Button { Text = "إعداد جديد" }; clear.Clicked += (_, _) => ClearFields();
        var actions = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 }; actions.Add(clear, 0); actions.Add(save, 1);
        Content = new ScrollView { VerticalScrollBarVisibility = ScrollBarVisibility.Never, Content = new VerticalStackLayout { Padding = new Thickness(16, 18, 16, 28), Spacing = 14, Children = { header, _formPanel, actions } } };
    }

    private async Task RefreshAsync()
    {
        _records = (await StorePricingAdminService.LoadAsync()).OrderByDescending(item => item.UpdatedAt).ToList();
        var offers = (await LimitedOffersAdminService.LoadManagedOffersAsync())
            .Select(item => new CanonicalOption(
                LimitedOffersAdminService.GetAssetId(item),
                string.IsNullOrWhiteSpace(item.Title) ? LimitedOffersAdminService.GetAssetId(item) : item.Title));
        _offerPicker.SetOptions([new CanonicalOption("None", "بدون عرض"), .. offers]);
        _recordsPicker.ItemsSource = _records.Select(item => $"{item.PricingKind} • {item.Currency} • {item.RegionCode}").ToList();
    }

    private void OnRecordSelected(object? sender, EventArgs e)
    {
        if (_recordsPicker.SelectedIndex < 0 || _recordsPicker.SelectedIndex >= _records.Count) return;
        _current = _records[_recordsPicker.SelectedIndex];
        _currencyPicker.SelectCanonicalId(_current.Currency); _kindPicker.SelectedItem = _current.PricingKind; _amountEntry.Text = _current.Amount.ToString(); _priceEntry.Text = _current.Price.ToString(); _discountEntry.Text = _current.DiscountPercent.ToString(); _seasonPicker.SelectCanonicalId(_current.SeasonId); _offerPicker.SelectCanonicalId(string.IsNullOrWhiteSpace(_current.OfferId) ? "None" : _current.OfferId); _regionEntry.Text = _current.RegionCode;
    }

    private async Task SaveAsync()
    {
        if (_currencyPicker.SelectedIndex < 0 || _kindPicker.SelectedIndex < 0 || !int.TryParse(_amountEntry.Text, out var amount) || !decimal.TryParse(_priceEntry.Text, out var price) || amount < 0 || price < 0) { ShowError("أكمل العملة ونوع التسعير والكمية والسعر"); return; }
        _ = int.TryParse(_discountEntry.Text, out var discount);
        var offerId = _offerPicker.SelectedCanonicalId();
        var configuration = new StorePricingConfiguration { Id = _current?.Id ?? Guid.NewGuid().ToString(), Currency = _currencyPicker.SelectedCanonicalId(), PricingKind = _kindPicker.SelectedItem?.ToString() ?? "GemPack", Amount = amount, Price = price, DiscountPercent = Math.Clamp(discount, 0, 100), SeasonId = _seasonPicker.SelectedCanonicalId(), OfferId = offerId == "None" ? string.Empty : offerId, RegionCode = string.IsNullOrWhiteSpace(_regionEntry.Text) ? "GLOBAL" : _regionEntry.Text.Trim().ToUpperInvariant() };
        await StorePricingAdminService.SaveAsync(configuration);
        _current = configuration; _validation.IsVisible = false; await RefreshAsync();
    }

    private void ClearFields()
    {
        _current = null; _recordsPicker.SelectedIndex = -1; _currencyPicker.SelectCanonicalId("Gems"); _kindPicker.SelectedItem = "GemPack"; _amountEntry.Text = _priceEntry.Text = _discountEntry.Text = string.Empty; _seasonPicker.SelectCanonicalId("None"); _offerPicker.SelectCanonicalId("None"); _regionEntry.Text = "GLOBAL"; _validation.IsVisible = false;
    }

    private void ShowError(string message) { _validation.Text = message; _validation.IsVisible = true; }
    private void OnThemeChanged(object? sender, GalleryTheme theme) => ApplyTheme();
    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current; _formPanel.Background = theme.ActionBackground; _formPanel.Stroke = theme.Stroke; _validation.TextColor = Color.FromArgb("#D84A4A");
        foreach (var entry in new[] { _amountEntry, _priceEntry, _discountEntry, _regionEntry }) { entry.TextColor = theme.TextPrimary; entry.PlaceholderColor = theme.TextMuted; }
        foreach (var picker in new[] { _recordsPicker, _currencyPicker, _kindPicker, _seasonPicker, _offerPicker }) { picker.TextColor = theme.TextPrimary; picker.TitleColor = theme.TextMuted; }
    }
}
