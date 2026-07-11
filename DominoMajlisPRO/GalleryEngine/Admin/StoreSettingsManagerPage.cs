using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class StoreSettingsManagerPage : ContentPage
{
    private readonly Switch _storeEnabled = new();
    private readonly Switch _newArrivals = new();
    private readonly Switch _limitedOffers = new();
    private readonly Switch _categories = new();
    private readonly Entry _pageSize = new()
    {
        Keyboard = Keyboard.Numeric,
        HorizontalTextAlignment = TextAlignment.End,
        FontFamily = "Tajawal-Regular"
    };

    private readonly Label _status = new()
    {
        HorizontalTextAlignment = TextAlignment.End,
        FontFamily = "Tajawal-Regular"
    };

    private readonly List<Label> _labels = new();
    private readonly List<Border> _cards = new();
    private Button _backButton = null!;
    private Button _saveButton = null!;
    private StoreRuntimeConfiguration _configuration = new();

    public StoreSettingsManagerPage()
    {
        Title = "إعدادات المتجر";
        FlowDirection = FlowDirection.RightToLeft;
        NavigationPage.SetHasNavigationBar(this, false);
        BuildPage();
        ApplyTheme();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _configuration = await StoreRuntimeConfigurationService.LoadAsync();
        _storeEnabled.IsToggled = _configuration.IsStoreEnabled;
        _newArrivals.IsToggled = _configuration.ShowNewArrivals;
        _limitedOffers.IsToggled = _configuration.ShowLimitedOffers;
        _categories.IsToggled = _configuration.ShowBrowseCategories;
        _pageSize.Text = _configuration.PageSize.ToString();
        ApplyTheme();
    }

    private void BuildPage()
    {
        _backButton = new Button
        {
            Text = "‹",
            WidthRequest = 42,
            HeightRequest = 42,
            CornerRadius = 14,
            FontSize = 24
        };
        _backButton.Clicked += async (_, _) => await Navigation.PopAsync();

        _saveButton = new Button
        {
            Text = "حفظ ونشر الإعدادات",
            HeightRequest = 46,
            CornerRadius = 12,
            FontFamily = "Tajawal-Regular",
            FontAttributes = FontAttributes.Bold
        };
        _saveButton.Clicked += async (_, _) => await SaveAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 14,
                Children =
                {
                    _backButton,
                    TextLabel("إعدادات المتجر", 25, true),
                    Row("تشغيل المتجر", _storeEnabled),
                    Row("إظهار وصل حديثاً", _newArrivals),
                    Row("إظهار العروض المحدودة", _limitedOffers),
                    Row("إظهار بطاقات الفئات", _categories),
                    TextLabel("عدد العناصر في الصفحة", 13, false),
                    _pageSize,
                    _status,
                    _saveButton
                }
            }
        };
    }

    private async Task SaveAsync()
    {
        _ = int.TryParse(_pageSize.Text, out var pageSize);
        _configuration.IsStoreEnabled = _storeEnabled.IsToggled;
        _configuration.ShowNewArrivals = _newArrivals.IsToggled;
        _configuration.ShowLimitedOffers = _limitedOffers.IsToggled;
        _configuration.ShowBrowseCategories = _categories.IsToggled;
        _configuration.PageSize = pageSize <= 0 ? 12 : pageSize;
        await StoreRuntimeConfigurationService.SaveAsync(_configuration);
        _status.Text = "تم حفظ الإعدادات ونشرها فوراً";
    }

    private View Row(string text, Switch toggle)
    {
        var grid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(TextLabel(text, 13, false), 0);
        grid.Add(toggle, 1);

        var card = new Border
        {
            StrokeThickness = 1,
            Padding = new Thickness(12, 10),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = grid
        };
        _cards.Add(card);
        return card;
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;
        Background = theme.Background;
        foreach (var label in _labels)
            label.TextColor = theme.TextPrimary;
        foreach (var card in _cards)
        {
            card.Background = theme.CardBackground;
            card.Stroke = theme.Stroke;
        }

        if (_backButton != null)
        {
            _backButton.BackgroundColor = Colors.Transparent;
            _backButton.BorderColor = theme.Stroke;
            _backButton.TextColor = theme.Gold;
        }

        if (_saveButton != null)
        {
            _saveButton.BackgroundColor = theme.Accent;
            _saveButton.TextColor = Colors.Black;
        }

        _pageSize.BackgroundColor = Colors.Transparent;
        _pageSize.TextColor = theme.TextPrimary;
        _pageSize.PlaceholderColor = theme.TextMuted;
        _status.TextColor = theme.Gold;
    }

    private Label TextLabel(string text, double size, bool bold)
    {
        var label = new Label
        {
            Text = text,
            FontFamily = "Tajawal-Regular",
            FontSize = size,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalTextAlignment = TextAlignment.Center,
            MaxLines = 2,
            LineBreakMode = LineBreakMode.WordWrap
        };
        _labels.Add(label);
        return label;
    }
}
