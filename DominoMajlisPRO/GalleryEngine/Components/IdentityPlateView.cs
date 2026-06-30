using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components;

public sealed class IdentityPlateView : ContentView
{
    public static readonly BindableProperty DisplayTextProperty =
        BindableProperty.Create(nameof(DisplayText), typeof(string), typeof(IdentityPlateView), "اسم اللاعب", propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty PresetProperty =
        BindableProperty.Create(nameof(Preset), typeof(TypographyIdentityPreset), typeof(IdentityPlateView), TypographyIdentityPreset.CreateDefault(), propertyChanged: OnVisualPropertyChanged);

    private readonly Border _frame;
    private readonly Label _text;

    public IdentityPlateView()
    {
        FlowDirection = FlowDirection.RightToLeft;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;

        _text = new Label
        {
            FontFamily = TypographyFontCatalog.DefaultFontFamily,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center
        };

        _frame = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            Padding = 0,
            Content = _text
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

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((IdentityPlateView)bindable).Apply();

    private void Apply()
    {
        var preset = (Preset ?? TypographyIdentityPreset.CreateDefault()).Normalized();
        var text = string.IsNullOrWhiteSpace(DisplayText) ? "اسم اللاعب" : DisplayText.Trim();
        var primary = Color.FromArgb(preset.PrimaryColor);
        var secondary = Color.FromArgb(preset.SecondaryColor);
        var hasFrame = !string.Equals(preset.FrameStylePreset, "None", StringComparison.OrdinalIgnoreCase);

        _text.Text = text;
        _text.FontFamily = preset.FontFamily;
        _text.FontSize = Math.Clamp(preset.FontSize * preset.Scale, 11, 34);
        _text.TextColor = primary;
        _text.Opacity = preset.Opacity;

        _frame.Stroke = hasFrame ? primary.WithAlpha((float)Math.Clamp(0.35 + preset.Intensity * 0.3, 0.35, 0.9)) : Colors.Transparent;
        _frame.StrokeThickness = hasFrame ? preset.FrameThickness : 0;
        _frame.Background = hasFrame ? CreateFrameBackground(preset, secondary) : new SolidColorBrush(Colors.Transparent);
        _frame.Padding = hasFrame ? new Thickness(10 + preset.FrameThickness, 3, 10 + preset.FrameThickness, 4) : new Thickness(0, 1);
        _frame.Shadow = hasFrame
            ? new Shadow
            {
                Brush = new SolidColorBrush(primary.WithAlpha(0.32f)),
                Radius = (float)Math.Clamp(8 + preset.Intensity * 6, 8, 18),
                Opacity = 0.38f,
                Offset = new Point(0, 2)
            }
            : null;
    }

    private static Brush CreateFrameBackground(TypographyIdentityPreset preset, Color secondary)
    {
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
