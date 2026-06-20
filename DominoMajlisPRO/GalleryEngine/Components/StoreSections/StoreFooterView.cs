using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class StoreFooterView : ContentView
{
    private readonly Border _root;
    private readonly List<FooterThemeTarget> _targets = new();

    public StoreFooterView()
    {
        FlowDirection = FlowDirection.RightToLeft;

        var grid = new Grid
        {
            ColumnSpacing = 0,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        grid.Add(CreateFooterItem("💎", "محتوى حصري", "غير متوفر في أي مكان آخر"), 0, 0);
        grid.Add(CreateFooterItem("🤍", "ادعم المطور", "ساعدنا لنقدم لك الأفضل"), 1, 0);
        grid.Add(CreateFooterItem("🛡️", "آمن 100%", "حسابك وعملياتك آمنة"), 2, 0);

        _root = new Border
        {
            Padding = new Thickness(10, 11),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = grid
        };

        Content = _root;

        ApplyTheme();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private View CreateFooterItem(string icon, string title, string subtitle)
    {
        var theme = GalleryThemeEngine.Current;

        var iconLabel = new Label
        {
            Text = icon,
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 22 : 28,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var titleLabel = new Label
        {
            Text = title,
            FontFamily = "Tajawal-Regular",
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12.5 : 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = theme.TextPrimary,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var subtitleLabel = new Label
        {
            Text = subtitle,
            FontFamily = "Tajawal-Regular",
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 9.5 : 11.5,
            TextColor = theme.TextSecondary,
            MaxLines = 2,
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            Padding = new Thickness(4, 0),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                iconLabel,
                titleLabel,
                subtitleLabel
            }
        };

        _targets.Add(new FooterThemeTarget(titleLabel, subtitleLabel));

        return stack;
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;

        _root.Background = theme.ActionBackground;
        _root.Stroke = theme.Stroke;
        _root.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(theme.Glow),
            Radius = 16,
            Opacity = 0.20f,
            Offset = new Point(0, 4)
        };

        foreach (var target in _targets)
        {
            target.Title.TextColor = theme.TextPrimary;
            target.Subtitle.TextColor = theme.TextSecondary;
        }
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

    private sealed record FooterThemeTarget(Label Title, Label Subtitle);
}
