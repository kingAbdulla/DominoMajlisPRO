using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed class EffectsLayerBuilderView : ContentView
{
    readonly VerticalStackLayout _itemsContainer = new()
    {
        Spacing = 6
    };

    readonly Dictionary<string, CheckBox> _checkboxes = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? SelectionChanged;

    public EffectsLayerBuilderView()
    {
        FlowDirection = FlowDirection.RightToLeft;

        var title = new Label
        {
            Text = "طبقات التأثير",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.End
        };

        var subtitle = new Label
        {
            Text = "اختر الطبقات التي يتكون منها التأثير",
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.End
        };

        Content = new Border
        {
            Padding = 12,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    title,
                    subtitle,
                    _itemsContainer
                }
            }
        };

        foreach (var option in EffectsStudioCatalog.Layers())
            AddLayerOption(option.CanonicalId, option.DisplayName);

        ApplyTheme(GalleryThemeEngine.Current, title, subtitle);
        GalleryThemeEngine.ThemeChanged += (_, theme) => ApplyTheme(theme, title, subtitle);
    }

    public IReadOnlyList<string> SelectedLayerIds =>
        _checkboxes
            .Where(item => item.Value.IsChecked)
            .Select(item => item.Key)
            .ToList();

    public void SelectLayers(IEnumerable<string>? layerIds)
    {
        var selected = new HashSet<string>(
            layerIds ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in _checkboxes)
            item.Value.IsChecked = selected.Contains(item.Key);
    }

    public void SelectDefaults()
    {
        SelectLayers(new[] { "Glow", "Aura" });
    }

    void AddLayerOption(string layerId, string displayName)
    {
        var checkBox = new CheckBox
        {
            VerticalOptions = LayoutOptions.Center,
            Color = Color.FromArgb("#D4AF37")
        };
        checkBox.CheckedChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);

        var label = new Label
        {
            Text = displayName,
            FontSize = 13,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End
        };

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8
        };
        row.Add(checkBox, 0, 0);
        row.Add(label, 1, 0);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => checkBox.IsChecked = !checkBox.IsChecked;
        row.GestureRecognizers.Add(tap);

        _itemsContainer.Children.Add(row);
        _checkboxes[layerId] = checkBox;
    }

    void ApplyTheme(GalleryTheme theme, Label title, Label subtitle)
    {
        if (Content is Border border)
        {
            border.Background = theme.ActionBackground;
            border.Stroke = theme.Stroke;
        }

        title.TextColor = theme.TextPrimary;
        subtitle.TextColor = theme.TextMuted;

        foreach (var row in _itemsContainer.Children.OfType<Grid>())
        {
            foreach (var label in row.Children.OfType<Label>())
                label.TextColor = theme.TextPrimary;
        }
    }
}
