using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components;

public sealed class IdentityPlateView : ContentView
{
    public static readonly BindableProperty DisplayTextProperty =
        BindableProperty.Create(
            nameof(DisplayText),
            typeof(string),
            typeof(IdentityPlateView),
            "اسم اللاعب",
            propertyChanged: (bindable, _, _) => ((IdentityPlateView)bindable).Apply());

    public static readonly BindableProperty PresetProperty =
        BindableProperty.Create(
            nameof(Preset),
            typeof(TypographyIdentityPreset),
            typeof(IdentityPlateView),
            TypographyIdentityPreset.CreateDefault(),
            propertyChanged: (bindable, _, _) => ((IdentityPlateView)bindable).Apply());

    private readonly Border _frame;
    private readonly Label _shadow;
    private readonly Label _text;
    private readonly Grid _layers;

    public IdentityPlateView()
    {
        FlowDirection = FlowDirection.RightToLeft;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        IsClippedToBounds = true;

        _shadow = CreateLabel();
        _shadow.TranslationY = 1.2;
        _shadow.Opacity = 0.45;
        _shadow.TextColor = Colors.Black;

        _text = CreateLabel();

        _layers = new Grid
        {
            Padding = new Thickness(10, 3),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto) },
            RowDefinitions = { new RowDefinition(GridLength.Auto) },
            Children = { _shadow, _text }
        };

        _frame = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            StrokeThickness = 1.4,
            Padding = 0,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            Content = _layers
        };

        Content = _frame;
        Apply();
    }

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public TypographyIdentityPreset Preset
    {
        get => (TypographyIdentityPreset)GetValue(PresetProperty);
        set => SetValue(PresetProperty, value);
    }

    public void Bind(string text, TypographyIdentityPreset? preset)
    {
        DisplayText = string.IsNullOrWhiteSpace(text) ? "اسم اللاعب" : text.Trim();
        Preset = preset ?? TypographyIdentityPreset.CreateDefault();
        Apply();
    }

    private void Apply()
    {
        if (_text == null)
            return;

        var preset = (Preset ?? TypographyIdentityPreset.CreateDefault()).Normalized();
        var text = string.IsNullOrWhiteSpace(DisplayText) ? "اسم اللاعب" : DisplayText.Trim();
        var primary = Color.FromArgb(preset.PrimaryColor);
        var secondary = Color.FromArgb(preset.SecondaryColor);
        var fontSize = Math.Clamp(preset.FontSize * preset.Scale, 11, 34);

        _text.Text = text;
        _shadow.Text = text;
        _text.FontFamily = preset.FontFamily;
        _shadow.FontFamily = preset.FontFamily;
        _text.FontSize = fontSize;
        _shadow.FontSize = fontSize;
        _text.TextColor = primary;
        _text.Opacity = preset.Opacity;
        var hasFrame = preset.FrameStylePreset != "None";
        _frame.Stroke = hasFrame
            ? primary.WithAlpha((float)Math.Clamp(0.35 + preset.Intensity * 0.3, 0.35, 0.9))
            : Colors.Transparent;
        _frame.StrokeThickness = hasFrame ? preset.FrameThickness : 0;
        _frame.Background = hasFrame
            ? CreateBackground(preset, secondary)
            : new SolidColorBrush(Colors.Transparent);
        _frame.Shadow = hasFrame
            ? new Shadow
            {
                Brush = new SolidColorBrush(primary.WithAlpha(0.32f)),
                Radius = (float)Math.Clamp(8 + preset.Intensity * 6, 8, 18),
                Opacity = 0.38f,
                Offset = new Point(0, 2)
            }
            : null;
        _layers.Padding = hasFrame
            ? new Thickness(10 + preset.FrameThickness, 3, 10 + preset.FrameThickness, 4)
            : new Thickness(0, 1);
    }

    private static Label CreateLabel() => new()
    {
        FontFamily = TypographyFontCatalog.DefaultFontFamily,
        FontSize = 18,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        MaxLines = 1,
        LineBreakMode = LineBreakMode.TailTruncation,
        HorizontalOptions = LayoutOptions.Start
    };

    private static Brush CreateBackground(TypographyIdentityPreset preset, Color secondary)
    {
        if (preset.FrameStylePreset == "None")
            return new SolidColorBrush(Colors.Transparent);

        var alpha = preset.MaterialPreset switch
        {
            "EmeraldGlass" => 0.42f,
            "RubyLacquer" => 0.48f,
            "PearlSteel" => 0.38f,
            _ => 0.52f
        };

        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(secondary.WithAlpha(alpha), 0),
                new GradientStop(Color.FromArgb("#060606").WithAlpha(0.66f), 0.64f),
                new GradientStop(secondary.WithAlpha(alpha * 0.75f), 1)
            }
        };
    }
}
