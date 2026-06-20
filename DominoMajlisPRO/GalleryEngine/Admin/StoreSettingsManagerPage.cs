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
    private readonly Entry _pageSize = new() { Keyboard = Keyboard.Numeric };
    private readonly Label _status = new() { HorizontalTextAlignment = TextAlignment.End };
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
    }

    private void BuildPage()
    {
        var back = new Button { Text = "‹", WidthRequest = 42, HeightRequest = 42 };
        back.Clicked += async (_, _) => await Navigation.PopAsync();
        var save = new Button { Text = "حفظ ونشر الإعدادات" };
        save.Clicked += async (_, _) => await SaveAsync();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 18, 16, 28),
                Spacing = 14,
                Children =
                {
                    back,
                    new Label { Text = "إعدادات المتجر", FontSize = 25, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End },
                    Row("تشغيل المتجر", _storeEnabled),
                    Row("إظهار وصل حديثاً", _newArrivals),
                    Row("إظهار العروض المحدودة", _limitedOffers),
                    Row("إظهار بطاقات الفئات", _categories),
                    new Label { Text = "عدد العناصر في الصفحة", HorizontalTextAlignment = TextAlignment.End },
                    _pageSize,
                    _status,
                    save
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

    private static View Row(string text, Switch toggle)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(new Label
        {
            Text = text,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End
        }, 0);
        grid.Add(toggle, 1);
        return grid;
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;
        Background = theme.Background;
        _pageSize.TextColor = theme.TextPrimary;
        _status.TextColor = theme.Gold;
    }
}
