using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public enum StoreBottomTab
{
    Store,
    Rewards,
    Offers,
    Account
}

public sealed class StoreBottomTabRequestedEventArgs(StoreBottomTab tab) : EventArgs
{
    public StoreBottomTab Tab { get; } = tab;
}

public class StoreBottomNavigationView : ContentView
{
    public event EventHandler<StoreBottomTabRequestedEventArgs>? TabRequested;

    private readonly Border _root;
    private readonly List<NavThemeTarget> _targets = new();
    private StoreBottomTab _selectedTab = StoreBottomTab.Store;

    public StoreBottomNavigationView()
    {
        FlowDirection = FlowDirection.RightToLeft;

        var grid = new Grid
        {
            ColumnSpacing = 6,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        grid.Children.Add(CreateNavItem("🛍️", "المتجر", StoreBottomTab.Store, 0));
        grid.Children.Add(CreateNavItem("🎁", "مكافآت", StoreBottomTab.Rewards, 1));
        grid.Children.Add(CreateNavItem("🏷️", "العروض", StoreBottomTab.Offers, 2));
        grid.Children.Add(CreateNavItem("👤", "حسابي", StoreBottomTab.Account, 3));

        _root = new Border
        {
            Margin = new Thickness(12, 0, 12, 14),
            Padding = new Thickness(8, 7),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Content = grid
        };

        Content = _root;

        ApplyTheme();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void SelectTab(StoreBottomTab tab)
    {
        if (_selectedTab == tab)
            return;

        _selectedTab = tab;
        ApplyTheme();
    }

    private View CreateNavItem(string icon, string title, StoreBottomTab tab, int column)
    {
        var iconLabel = new Label
        {
            Text = icon,
            FontSize = 18,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var titleLabel = new Label
        {
            Text = title,
            FontFamily = "Tajawal-Regular",
            FontSize = 12,
            FontAttributes = FontAttributes.None,
            HorizontalTextAlignment = TextAlignment.Center,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 1,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                iconLabel,
                titleLabel
            }
        };

        var item = new Border
        {
            Padding = new Thickness(6, 5),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = stack
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => TabRequested?.Invoke(this, new StoreBottomTabRequestedEventArgs(tab));
        item.GestureRecognizers.Add(tap);

        _targets.Add(new NavThemeTarget(item, iconLabel, titleLabel, tab));

        Grid.SetColumn(item, column);
        return item;
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
            Opacity = 0.22f,
            Offset = new Point(0, 4)
        };

        foreach (var target in _targets)
        {
            var isSelected = target.Tab == _selectedTab;
            target.Item.StrokeThickness = isSelected ? 1 : 0;
            target.Icon.FontSize = isSelected ? 20 : 18;
            target.Label.FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None;

            target.Item.Background = isSelected
                ? theme.CardBackground
                : new SolidColorBrush(Colors.Transparent);

            target.Item.Stroke = isSelected
                ? theme.Accent
                : Colors.Transparent;

            target.Item.Shadow = isSelected
                ? new Shadow
                {
                    Brush = new SolidColorBrush(theme.Glow),
                    Radius = 12,
                    Opacity = 0.22f,
                    Offset = new Point(0, 2)
                }
                : null!;

            target.Label.TextColor = isSelected
                ? theme.Gold
                : theme.TextSecondary;
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

    private void OnThemeChanged(
        object? sender,
        DominoMajlisPRO.GalleryEngine.Services.GalleryTheme theme)
    {
        ApplyTheme();
    }

    private sealed record NavThemeTarget(Border Item, Label Icon, Label Label, StoreBottomTab Tab);
}
