using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public abstract class StoreProductsSectionBase : ContentView
{
    private readonly Grid _productsGrid;
    private readonly Label _arabicTitle;
    private readonly Label _englishTitle;
    private readonly Label _actionLabel;

    protected StoreProductsSectionBase(string title, string englishTitle, string actionText)
    {
        FlowDirection = FlowDirection.RightToLeft;

        _productsGrid = new Grid
        {
            ColumnSpacing = 8,
            RowSpacing = 10
        };

        _arabicTitle = new Label
        {
            Text = title,
            FontFamily = "Tajawal-Regular",
            FontSize = 19,
            FontAttributes = FontAttributes.Bold,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.End
        };

        _englishTitle = new Label
        {
            Text = englishTitle,
            FontFamily = "timesbi",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, -3, 0, 0),
            HorizontalTextAlignment = TextAlignment.End
        };

        _actionLabel = new Label
        {
            Text = actionText,
            FontFamily = "Tajawal-Regular",
            FontSize = 12.5,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            MaxLines = 1
        };

        Content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CreateHeader(),
                _productsGrid
            }
        };

        ApplyTheme();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void Bind(List<GalleryItem> items)
    {
        Build(items);
    }

    protected void Build(List<GalleryItem> items)
    {
        _productsGrid.Children.Clear();
        _productsGrid.RowDefinitions.Clear();
        _productsGrid.ColumnDefinitions.Clear();

        const int columns = 3;

        for (int i = 0; i < columns; i++)
            _productsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        int rows = (int)Math.Ceiling(items.Count / (double)columns);

        for (int i = 0; i < rows; i++)
            _productsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < items.Count; i++)
        {
            var card = new PremiumGalleryCard();
            card.Bind(items[i]);

            _productsGrid.Add(card, i % columns, i / columns);
        }
    }

    private View CreateHeader()
    {
        var titleStack = new VerticalStackLayout
        {
            Spacing = 0,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                _arabicTitle,
                _englishTitle
            }
        };

        var grid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        if (!string.IsNullOrWhiteSpace(_actionLabel.Text))
            grid.Add(_actionLabel, 0, 0);

        grid.Add(titleStack, 1, 0);

        return grid;
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;

        _arabicTitle.TextColor = theme.TextPrimary;
        _englishTitle.TextColor = theme.TextMuted;
        _actionLabel.TextColor = theme.Accent;
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
}


