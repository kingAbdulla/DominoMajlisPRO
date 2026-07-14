using DominoMajlisPRO.GalleryEngine.Admin.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public sealed class ArchivedStoreItemsPage : ContentPage
{
    private readonly VerticalStackLayout _items = new() { Spacing = 10 };
    private readonly Label _empty = new()
    {
        Text = "لا توجد منشورات مؤرشفة",
        TextColor = Color.FromArgb("#9D927D"),
        HorizontalTextAlignment = TextAlignment.Center,
        Margin = new Thickness(0, 30)
    };

    public ArchivedStoreItemsPage()
    {
        Title = "المنشورات المؤرشفة";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb("#050607");
        var contentScroll = new ScrollView
        {
            Margin = new Thickness(0, 14, 0, 0),
            Content = _items
        };
        Grid.SetRow(contentScroll, 1);

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            },
            Padding = new Thickness(14, 18),
            Children =
            {
                new Label
                {
                    Text = "المنشورات المؤرشفة",
                    FontFamily = "Tajawal-Regular",
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F2C46D"),
                    HorizontalTextAlignment = TextAlignment.End
                },
                contentScroll
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var arrivals = await NewArrivalsAdminService.LoadArchivedAsync();
        var offers = await LimitedOffersAdminService.LoadArchivedAsync();
        _items.Children.Clear();

        foreach (var item in arrivals)
            _items.Children.Add(BuildCard(item.Title, item.AssetId, false));
        foreach (var item in offers)
            _items.Children.Add(BuildCard(item.Title, item.AssetId, true));

        _empty.IsVisible = arrivals.Count + offers.Count == 0;
        _items.Children.Add(_empty);
    }

    private View BuildCard(string title, string assetId, bool isOffer)
    {
        var restore = Button("استعادة", "#D9B44A", Colors.Black);
        var delete = Button("حذف نهائي", "#6E1717", Color.FromArgb("#FFE0D2"));
        restore.Clicked += async (_, _) =>
        {
            if (isOffer)
                await LimitedOffersAdminService.RestoreArchivedAsync(assetId);
            else
                await NewArrivalsAdminService.RestoreArchivedAsync(assetId);
            await RefreshAsync();
        };
        delete.Clicked += async (_, _) =>
        {
            var confirmed = await DisplayAlertAsync("حذف نهائي", "لن يمكن استعادة هذا المنشور بعد الحذف. هل تريد المتابعة؟", "حذف", "إلغاء");
            if (!confirmed)
                return;
            if (isOffer)
                await LimitedOffersAdminService.DeletePublishedAsync(assetId);
            else
                await NewArrivalsAdminService.DeletePublishedAsync(assetId);
            await RefreshAsync();
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        grid.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(title) ? assetId : title,
            TextColor = Color.FromArgb("#F7E8C2"),
            FontFamily = "Tajawal-Regular",
            MaxLines = 2,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalTextAlignment = TextAlignment.Center
        }, 0);
        grid.Add(restore, 1);
        grid.Add(delete, 2);
        return new Border
        {
            Padding = 10,
            Stroke = Color.FromArgb("#76521B"),
            BackgroundColor = Color.FromArgb("#101214"),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = grid
        };
    }

    private static Button Button(string text, string background, Color foreground) => new()
    {
        Text = text,
        FontFamily = "Tajawal-Regular",
        FontSize = 11,
        TextColor = foreground,
        BackgroundColor = Color.FromArgb(background),
        CornerRadius = 9,
        MinimumHeightRequest = 38,
        Padding = new Thickness(10, 4)
    };
}
