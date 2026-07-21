using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class CategoriesSectionView : ContentView
{
    private readonly List<CategoryChipThemeTarget> _chipTargets = new();
    private readonly HorizontalStackLayout _chips;
    private Label? _titleLabel;
    private Label? _eyebrowLabel;
    private Label? _showAllLabel;
    private StoreView _selectedView = StoreView.Home;

    public event EventHandler<StoreCategorySelectedEventArgs>? CategorySelected;
    public event EventHandler? ShowAllRequested;

    public CategoriesSectionView()
    {
        FlowDirection = FlowDirection.RightToLeft;

        var root = new VerticalStackLayout
        {
            Spacing = 8
        };

        root.Children.Add(CreateHeader());

        var scroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never
        };

        _chips = new HorizontalStackLayout
        {
            Spacing = 7,
            Padding = new Thickness(2, 0),
            FlowDirection = FlowDirection.RightToLeft
        };

        scroll.Content = _chips;
        root.Children.Add(scroll);

        Content = root;

        ApplyTheme();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private View CreateHeader()
    {
        var theme = GalleryThemeEngine.Current;

        _titleLabel = new Label
        {
            Text = "الفئات",
            FontFamily = "Tajawal-Regular",
            FontSize = 19,
            FontAttributes = FontAttributes.Bold,
            TextColor = theme.TextPrimary,
            HorizontalTextAlignment = TextAlignment.End,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        _eyebrowLabel = new Label
        {
            Text = "CATEGORIES",
            FontFamily = "timesbi",
            FontSize = 10,
            TextColor = theme.TextMuted,
            HorizontalTextAlignment = TextAlignment.End,
            MaxLines = 1
        };

        var titleStack = new VerticalStackLayout
        {
            Spacing = 0,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                _titleLabel,
                _eyebrowLabel
            }
        };

        _showAllLabel = new Label
        {
            Text = "عرض الكل",
            FontFamily = "Tajawal-Regular",
            FontSize = 12.5,
            FontAttributes = FontAttributes.Bold,
            TextColor = theme.Accent,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var showAllTap = new TapGestureRecognizer();
        showAllTap.Tapped += (_, _) => ShowAllRequested?.Invoke(this, EventArgs.Empty);
        _showAllLabel.GestureRecognizers.Add(showAllTap);

        var grid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        grid.Add(_showAllLabel, 0, 0);
        grid.Add(titleStack, 1, 0);

        return grid;
    }

    private View CreateChip(string title, StoreView view, bool selected = false)
    {
        var theme = GalleryThemeEngine.Current;

        var label = new Label
        {
            Text = title,
            FontFamily = "Tajawal-Regular",
            FontSize = 12.5,
            FontAttributes = FontAttributes.Bold,
            TextColor = selected ? theme.Gold : theme.TextSecondary,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var chip = new Border
        {
            Background = selected
                ? theme.ActionBackground
                : theme.CardBackground,
            Stroke = selected ? theme.Accent : theme.Stroke,
            StrokeThickness = selected ? 1.1 : 0.85,
            Padding = new Thickness(14, 7),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Shadow = selected ? CreateSelectedShadow(theme) : null!,
            Content = label
        };

        _chipTargets.Add(new CategoryChipThemeTarget(chip, label, view, selected));

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Select(view, true);
        chip.GestureRecognizers.Add(tap);

        return chip;
    }

    public void Select(StoreView view, bool notify = false)
    {
        _selectedView = view;
        foreach (var target in _chipTargets)
            target.IsSelected = target.View == view;

        ApplyTheme();

        if (notify)
            CategorySelected?.Invoke(this, new StoreCategorySelectedEventArgs(view));
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;

        if (_titleLabel != null)
            _titleLabel.TextColor = theme.TextPrimary;

        if (_eyebrowLabel != null)
            _eyebrowLabel.TextColor = theme.TextMuted;

        if (_showAllLabel != null)
            _showAllLabel.TextColor = theme.Accent;

        foreach (var target in _chipTargets)
        {
            target.Chip.Background =
                target.IsSelected
                    ? theme.ActionBackground
                    : theme.CardBackground;

            target.Chip.Stroke =
                target.IsSelected
                    ? theme.Accent
                    : theme.Stroke;

            target.Chip.Shadow = target.IsSelected
                ? CreateSelectedShadow(theme)
                : null!;

            target.Label.TextColor =
                target.IsSelected
                    ? theme.Gold
                    : theme.TextSecondary;
        }
    }

    private static Shadow CreateSelectedShadow(GalleryTheme theme)
    {
        return new Shadow
        {
            Brush = new SolidColorBrush(theme.Glow),
            Radius = 12,
            Opacity = 0.20f,
            Offset = new Point(0, 3)
        };
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        StoreCategoriesAdminService.PublishedChanged -= OnCategoriesChanged;
        StoreCategoriesAdminService.PublishedChanged += OnCategoriesChanged;
        ApplyTheme();
        _ = RefreshCategoriesAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        StoreCategoriesAdminService.PublishedChanged -= OnCategoriesChanged;
    }

    private void OnCategoriesChanged() => _ = RefreshCategoriesAsync();

    private async Task RefreshCategoriesAsync()
    {
        var categories = await StoreAssetQueryService.LoadCategoriesAsync();
        var mapped = categories
            .Select(record => (Record: record, View: StoreTypeRegistry.Resolve(record.Category, record.NameEn, record.NameAr)?.TargetView))
            .Where(item => item.View != null)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _chips.Children.Clear();
            _chipTargets.Clear();
            IsVisible = true;
            _chips.Children.Add(CreateChip("الكل", StoreView.Home, _selectedView == StoreView.Home));

            foreach (var type in StoreTypeRegistry.DefaultCategoryTypes)
            {
                if (_chipTargets.Any(target => target.View == type.TargetView))
                    continue;

                _chips.Children.Add(CreateChip(type.ArabicName, type.TargetView, type.TargetView == _selectedView));
            }

            foreach (var item in mapped)
            {
                if (_chipTargets.Any(target => target.View == item.View!.Value))
                    continue;
                var title = string.IsNullOrWhiteSpace(item.Record.NameAr) ? item.Record.NameEn : item.Record.NameAr;
                _chips.Children.Add(CreateChip(title, item.View!.Value, item.View.Value == _selectedView));
            }
            ApplyTheme();
        });
    }

    private void OnThemeChanged(object? sender, GalleryTheme theme)
    {
        ApplyTheme();
    }

    private sealed class CategoryChipThemeTarget(
        Border chip,
        Label label,
        StoreView view,
        bool isSelected)
    {
        public Border Chip { get; } = chip;
        public Label Label { get; } = label;
        public StoreView View { get; } = view;
        public bool IsSelected { get; set; } = isSelected;
    }
}

public sealed class StoreCategorySelectedEventArgs(StoreView view) : EventArgs
{
    public StoreView View { get; } = view;
}
