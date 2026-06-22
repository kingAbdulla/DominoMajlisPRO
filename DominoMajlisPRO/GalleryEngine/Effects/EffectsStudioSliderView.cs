using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed class EffectsStudioSliderView : ContentView
{
    readonly Label _titleLabel = new()
    {
        FontSize = 13,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.End
    };

    readonly Label _valueLabel = new()
    {
        FontSize = 12,
        HorizontalTextAlignment = TextAlignment.Start
    };

    readonly Slider _slider = new();

    bool _suppress;

    public event EventHandler<double>? ValueChanged;

    public EffectsStudioSliderView(
        string title,
        double minimum,
        double maximum,
        double value)
    {
        FlowDirection = FlowDirection.RightToLeft;
        _titleLabel.Text = title;
        _slider.Minimum = minimum;
        _slider.Maximum = maximum;
        _slider.Value = value;
        UpdateValueLabel(value);

        _slider.ValueChanged += (_, e) =>
        {
            if (_suppress)
                return;

            UpdateValueLabel(e.NewValue);
            ValueChanged?.Invoke(this, e.NewValue);
        };

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        header.Add(_titleLabel, 0, 0);
        header.Add(_valueLabel, 1, 0);

        Content = new Border
        {
            Padding = 10,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    header,
                    _slider
                }
            }
        };

        ApplyTheme(GalleryThemeEngine.Current);
        GalleryThemeEngine.ThemeChanged += (_, theme) => ApplyTheme(theme);
    }

    public double Value
    {
        get => _slider.Value;
        set
        {
            var next = Math.Clamp(value, _slider.Minimum, _slider.Maximum);
            _suppress = true;
            _slider.Value = next;
            _suppress = false;
            UpdateValueLabel(next);
        }
    }

    public string TextValue => Value.ToString("0.##");

    void UpdateValueLabel(double value) =>
        _valueLabel.Text = value.ToString("0.##");

    void ApplyTheme(GalleryTheme theme)
    {
        if (Content is Border border)
        {
            border.Background = theme.ActionBackground;
            border.Stroke = theme.Stroke;
        }

        _titleLabel.TextColor = theme.TextPrimary;
        _valueLabel.TextColor = theme.TextMuted;
        _slider.MinimumTrackColor = theme.Accent;
        _slider.MaximumTrackColor = theme.Stroke;
        _slider.ThumbColor = theme.Accent;
    }
}
