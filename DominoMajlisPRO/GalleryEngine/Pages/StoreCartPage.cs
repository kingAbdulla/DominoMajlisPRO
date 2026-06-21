using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Pages;

public sealed class StoreCartPage : ContentPage
{
    readonly VerticalStackLayout _itemsHost = new() { Spacing = 12 };
    readonly Label _summaryLabel = LabelText("جاري تحميل السلة", 14, false);
    string? _playerId;

    public StoreCartPage()
    {
        Title = "سلة الشراء";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb("#030303");
        NavigationPage.SetHasNavigationBar(this, false);

        var backButton = ButtonText("رجوع");
        backButton.Clicked += async (_, _) => await Navigation.PopAsync();

        var confirmButton = ButtonText("تأكيد عملية الشراء");
        confirmButton.Clicked += async (_, _) => await DisplayAlert("سلة الشراء", "لا توجد عناصر لتأكيدها حالياً.", "حسناً");

        var clearButton = ButtonText("حذف العناصر");
        clearButton.Clicked += async (_, _) => await DisplayAlert("سلة الشراء", "السلة فارغة حالياً.", "حسناً");

        var cancelButton = ButtonText("إلغاء");
        cancelButton.Clicked += async (_, _) => await Navigation.PopAsync();

        Content = new Grid
        {
            Padding = new Thickness(16, 18, 16, 20),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children =
            {
                Header(backButton),
                Body(),
                Footer(confirmButton, clearButton, cancelButton)
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    View Header(Button backButton)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };
        grid.Add(backButton, 0, 0);
        grid.Add(LabelText("سلة الشراء", 24, true), 1, 0);
        Grid.SetRow(grid, 0);
        return grid;
    }

    View Body()
    {
        var view = new ScrollView { Content = _itemsHost };
        Grid.SetRow(view, 1);
        return view;
    }

    View Footer(Button confirmButton, Button clearButton, Button cancelButton)
    {
        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10
        };
        actions.Add(clearButton, 0, 0);
        actions.Add(cancelButton, 1, 0);

        var footer = new VerticalStackLayout
        {
            Spacing = 10,
            Children = { _summaryLabel, confirmButton, actions }
        };
        Grid.SetRow(footer, 2);
        return footer;
    }

    async Task LoadAsync()
    {
        _itemsHost.Children.Clear();
        _playerId = await PlayerStoreIdentityService.GetDeviceIdentityPlayerIdAsync();

        if (string.IsNullOrWhiteSpace(_playerId))
        {
            _itemsHost.Children.Add(Card("لا يوجد لاعب نشط", "يجب فتح حساب اللاعب أولاً حتى ترتبط السلة بـ PlayerId."));
            _summaryLabel.Text = "السلة غير مرتبطة بلاعب حالياً.";
            return;
        }

        var wallet = await PlayerStoreIdentityService.GetWalletAsync(_playerId);
        _itemsHost.Children.Add(Card("السلة فارغة", "اختر عناصر من المتجر ثم عد إلى السلة."));
        _summaryLabel.Text = $"PlayerId: {_playerId}\nالرصيد: {wallet.Coins:N0} عملة / {wallet.Gems:N0} جوهرة";
    }

    static Border Card(string title, string details) => new()
    {
        Padding = 16,
        Background = Color.FromArgb("#15120D"),
        Stroke = Color.FromArgb("#B98A3A"),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 18 },
        Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children = { LabelText(title, 18, true), LabelText(details, 13, false) }
        }
    };

    static Label LabelText(string text, double size, bool bold) => new()
    {
        Text = text,
        FontFamily = "Tajawal-Regular",
        FontSize = size,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        TextColor = Color.FromArgb("#E7C979"),
        HorizontalTextAlignment = TextAlignment.Center
    };

    static Button ButtonText(string text) => new()
    {
        Text = text,
        FontFamily = "Tajawal-Regular",
        FontAttributes = FontAttributes.Bold,
        BackgroundColor = Color.FromArgb("#D4AF37"),
        TextColor = Color.FromArgb("#090806"),
        CornerRadius = 16,
        HeightRequest = 46
    };
}
