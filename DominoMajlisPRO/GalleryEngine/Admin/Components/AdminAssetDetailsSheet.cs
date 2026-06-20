using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin.Components;

public enum AdminAssetDetailsAction
{
    None,
    Edit,
    Hide,
    Restore,
    DeleteDraft,
    DeletePublished
}

public sealed class AdminAssetDetailsSheet : ContentPage
{
    private readonly TaskCompletionSource<AdminAssetDetailsAction> _result = new();

    private AdminAssetDetailsSheet(
        NewArrivalRecord record,
        bool published,
        string managerTitle)
    {
        BackgroundColor = Color.FromArgb("#CC000000");
        NavigationPage.SetHasNavigationBar(this, false);

        var image = new Image
        {
            HeightRequest = 190,
            Aspect = Aspect.AspectFit,
            Source = InventoryDisplayResolver.ResolveOptionalImageSource(record.ImagePath)
        };

        var title = new Label
        {
            Text = string.IsNullOrWhiteSpace(record.Title) ? record.AssetId : record.Title,
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#FFD86B"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        var subtitle = new Label
        {
            Text = managerTitle,
            FontSize = 12,
            TextColor = Color.FromArgb("#C8B98A"),
            HorizontalTextAlignment = TextAlignment.Center
        };

        var info = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Info("AssetId", record.AssetId),
                Info("Type", record.StoreTypeId),
                Info("Status", record.Status.ToString()),
                Info("Price", record.IsFree ? "Free" : $"{record.Price} {record.CurrencyType}"),
                Info("Category", record.Category),
                Info("Created", record.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))
            }
        };

        var edit = Button("تعديل", "#FFD86B", "#121212", AdminAssetDetailsAction.Edit);
        var close = Button("إغلاق", "#171717", "#F2E6C6", AdminAssetDetailsAction.None);

        var dangerAction = published
            ? AdminAssetDetailsAction.DeletePublished
            : AdminAssetDetailsAction.DeleteDraft;

        var danger = Button(
            published ? "حذف نهائي" : "حذف المسودة",
            "#3A1010",
            "#FFD6D6",
            dangerAction);

        Button stateButton;
        if (published && record.Status.ToString().Equals("Hidden", StringComparison.OrdinalIgnoreCase))
            stateButton = Button("استعادة النشر", "#123A20", "#D9FFE4", AdminAssetDetailsAction.Restore);
        else if (published)
            stateButton = Button("أرشفة", "#2B2415", "#FFE1A3", AdminAssetDetailsAction.Hide);
        else
            stateButton = Button("حفظ كمسودة", "#222222", "#CCCCCC", AdminAssetDetailsAction.None);

        var buttons = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10,
            RowSpacing = 10
        };

        buttons.Add(edit, 0, 0);
        buttons.Add(stateButton, 1, 0);
        buttons.Add(danger, 0, 1);
        buttons.Add(close, 1, 1);

        var card = new Border
        {
            Margin = 18,
            Padding = 18,
            Stroke = Color.FromArgb("#8A6A1F"),
            StrokeThickness = 1,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#090909"), 0),
                    new GradientStop(Color.FromArgb("#201106"), 1)
                }
            },
            StrokeShape = new RoundRectangle { CornerRadius = 26 },
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    subtitle,
                    image,
                    title,
                    info,
                    buttons
                }
            }
        };

        Content = new Grid
        {
            Children =
            {
                card
            },
            VerticalOptions = LayoutOptions.Center
        };
    }

    public static async Task<AdminAssetDetailsAction> ShowAsync(
        Page owner,
        NewArrivalRecord record,
        bool published,
        string managerTitle)
    {
        var sheet = new AdminAssetDetailsSheet(record, published, managerTitle);
        await owner.Navigation.PushModalAsync(sheet, false);
        return await sheet._result.Task;
    }

    private static Label Info(string label, string? value) =>
        new()
        {
            Text = $"{label}: {value}",
            FontSize = 13,
            TextColor = Color.FromArgb("#E8D9B5"),
            HorizontalTextAlignment = TextAlignment.Center
        };

    private Button Button(
        string text,
        string background,
        string foreground,
        AdminAssetDetailsAction action)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(background),
            TextColor = Color.FromArgb(foreground),
            CornerRadius = 16,
            FontAttributes = FontAttributes.Bold
        };

        button.Clicked += async (_, _) =>
        {
            _result.TrySetResult(action);
            await Navigation.PopModalAsync(false);
        };

        return button;
    }
}