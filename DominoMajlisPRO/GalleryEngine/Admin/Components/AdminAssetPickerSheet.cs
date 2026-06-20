using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin.Components;

public sealed class AdminAssetPickerSheet : ContentPage
{
    private readonly TaskCompletionSource<NewArrivalRecord?> _result = new();

    private AdminAssetPickerSheet(
        IReadOnlyList<NewArrivalRecord> records,
        string title)
    {
        BackgroundColor = Color.FromArgb("#CC000000");
        NavigationPage.SetHasNavigationBar(this, false);

        var header = new Label
        {
            Text = title,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFD86B"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        var list = new VerticalStackLayout
        {
            Spacing = 10
        };

        foreach (var record in records)
            list.Children.Add(CreateCard(record));

        var close = new Button
        {
            Text = "إغلاق",
            BackgroundColor = Color.FromArgb("#171717"),
            TextColor = Color.FromArgb("#F2E6C6"),
            CornerRadius = 16
        };

        close.Clicked += async (_, _) =>
        {
            _result.TrySetResult(null);
            await Navigation.PopModalAsync(false);
        };

        var panel = new Border
        {
            Margin = 18,
            Padding = 16,
            Stroke = Color.FromArgb("#8A6A1F"),
            StrokeThickness = 1,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#080808"), 0),
                    new GradientStop(Color.FromArgb("#241006"), 1)
                }
            },
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    header,
                    new ScrollView
                    {
                        HeightRequest = 430,
                        Content = list
                    },
                    close
                }
            }
        };

        Content = new Grid
        {
            VerticalOptions = LayoutOptions.Center,
            Children = { panel }
        };
    }

    public static async Task<NewArrivalRecord?> ShowAsync(
        Page owner,
        IReadOnlyList<NewArrivalRecord> records,
        string title)
    {
        var sheet = new AdminAssetPickerSheet(records, title);
        await owner.Navigation.PushModalAsync(sheet, false);
        return await sheet._result.Task;
    }

    private View CreateCard(NewArrivalRecord record)
    {
        var image = new Image
        {
            WidthRequest = 72,
            HeightRequest = 72,
            Aspect = Aspect.AspectFit,
            Source = InventoryDisplayResolver.ResolveOptionalImageSource(record.ImagePath)
        };

        var name = new Label
        {
            Text = string.IsNullOrWhiteSpace(record.Title) ? record.AssetId : record.Title,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFD86B")
        };

        var meta = new Label
        {
            Text = $"{record.StoreTypeId} • {record.Status} • {(record.IsFree ? "Free" : record.Price.ToString())}",
            FontSize = 12,
            TextColor = Color.FromArgb("#D7C69B")
        };

        var assetId = new Label
        {
            Text = record.AssetId,
            FontSize = 10,
            TextColor = Color.FromArgb("#8F8060"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var detailsLayout = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                name,
                meta,
                assetId
            }
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };

        grid.Add(image, 0, 0);
        grid.Add(detailsLayout, 1, 0);

        var card = new Border
        {
            Padding = 10,
            Stroke = Color.FromArgb("#6F5018"),
            StrokeThickness = 1,
            Background = Color.FromArgb("#15100A"),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = grid
        };

        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                _result.TrySetResult(record);
                await Navigation.PopModalAsync(false);
            })
        });

        return card;
    }
}