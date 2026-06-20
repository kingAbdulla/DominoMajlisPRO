using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class QuickActionsView : ContentView
{
    private readonly List<ActionCardThemeTarget> _themeTargets = new();

    public event EventHandler<StoreQuickActionEventArgs>? ActionRequested;

    public QuickActionsView()
    {
        FlowDirection = FlowDirection.RightToLeft;

        var grid = new Grid
        {
            ColumnSpacing = DeviceInfo.Idiom == DeviceIdiom.Phone ? 7 : 9,
            RowSpacing = 8
        };

        var columns = DeviceInfo.Idiom == DeviceIdiom.Phone ? 2 : 4;

        for (int i = 0; i < columns; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cards = new List<View>
        {
            CreateActionCard("🎁", "العروض اليومية", "خصومات كل يوم", StoreQuickAction.DailyOffers),
            CreateActionCard("🎡", "عجلة الحظ", "فرصتك للربح", StoreQuickAction.WheelOfFortune),
            CreateActionCard("🎫", "بطاقة الموسم", "جوائز حصرية", StoreQuickAction.SeasonPass),
            CreateActionCard("💎", "اشحن الآن", "احصل على المزيد", StoreQuickAction.TopUp)
        };

        for (int i = 0; i < cards.Count; i++)
            grid.Add(cards[i], i % columns, i / columns);

        Content = grid;

        ApplyTheme();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private View CreateActionCard(string icon, string title, string subtitle, StoreQuickAction action)
    {
        var theme = GalleryThemeEngine.Current;

        var iconLabel = new Label
        {
            Text = icon,
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 24 : 30,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var titleLabel = new Label
        {
            Text = title,
            FontFamily = "Tajawal-Regular",
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12.5 : 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = theme.TextPrimary,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalTextAlignment = TextAlignment.Center
        };

        var subtitleLabel = new Label
        {
            Text = subtitle,
            FontFamily = "Tajawal-Regular",
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 9.5 : 11,
            TextColor = theme.TextSecondary,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalTextAlignment = TextAlignment.Center
        };

        var textStack = new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                titleLabel,
                subtitleLabel
            }
        };

        var contentGrid = new Grid
        {
            FlowDirection = FlowDirection.RightToLeft,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = DeviceInfo.Idiom == DeviceIdiom.Phone ? 7 : 10
        };

        contentGrid.Add(iconLabel, 0, 0);
        contentGrid.Add(textStack, 1, 0);

        var card = new Border
        {
            Background = theme.ActionBackground,
            Stroke = theme.Stroke,
            StrokeThickness = 1,
            Padding = new Thickness(10, 8),
            MinimumHeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 58 : 68,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Shadow = CreateShadow(theme),
            Content = contentGrid
        };

        _themeTargets.Add(
            new ActionCardThemeTarget(
                card,
                titleLabel,
                subtitleLabel));

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => ActionRequested?.Invoke(this, new StoreQuickActionEventArgs(action));
        card.GestureRecognizers.Add(tap);

        return card;
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;

        foreach (var target in _themeTargets)
        {
            target.Card.Background = theme.ActionBackground;
            target.Card.Stroke = theme.Stroke;
            target.Card.Shadow = CreateShadow(theme);
            target.TitleLabel.TextColor = theme.TextPrimary;
            target.SubtitleLabel.TextColor = theme.TextSecondary;
        }
    }

    private static Shadow CreateShadow(GalleryTheme theme)
    {
        return new Shadow
        {
            Brush = new SolidColorBrush(theme.Glow),
            Radius = 14,
            Opacity = 0.22f,
            Offset = new Point(0, 4)
        };
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, GalleryTheme theme)
    {
        ApplyTheme();
    }

    private sealed record ActionCardThemeTarget(
        Border Card,
        Label TitleLabel,
        Label SubtitleLabel);
}

public enum StoreQuickAction
{
    WheelOfFortune,
    DailyOffers,
    TopUp,
    SeasonPass
}

public sealed class StoreQuickActionEventArgs(StoreQuickAction action) : EventArgs
{
    public StoreQuickAction Action { get; } = action;
}
