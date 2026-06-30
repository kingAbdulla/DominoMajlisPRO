using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components;

public sealed class IdentityPlateView : ContentView
{
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
        Bind("اسم اللاعب", TypographyIdentityPreset.CreateDefault());
    }

    public void Bind(string text, TypographyIdentityPreset? preset)
    {
        var normalized = (preset ?? TypographyIdentityPreset.CreateDefault()).Normalized();
        var primary = Color.FromArgb(normalized.PrimaryColor);
        var secondary = Color.FromArgb(normalized.SecondaryColor);
        var hasFrame = !string.Equals(normalized.FrameStylePreset, "None", StringComparison.OrdinalIgnoreCase);

        _text.Text = string.IsNullOrWhiteSpace(text) ? "اسم اللاعب" : text.Trim();
        _text.FontFamily = normalized.FontFamily;
        _text.FontSize = Math.Clamp(normalized.FontSize * normalized.Scale, 11, 34);
        _text.TextColor = primary;
        _text.Opacity = normalized.Opacity;

        _frame.Stroke = hasFrame ? primary.WithAlpha((float)Math.Clamp(0.35 + normalized.Intensity * 0.3, 0.35, 0.9)) : Colors.Transparent;
        _frame.StrokeThickness = hasFrame ? normalized.FrameThickness : 0;
        _frame.Padding = hasFrame ? new Thickness(10 + normalized.FrameThickness, 3, 10 + normalized.FrameThickness, 4) : new Thickness(0, 1);
        _frame.Background = hasFrame ? CreateFrameBackground(normalized, secondary) : new SolidColorBrush(Colors.Transparent);
        _frame.Shadow = hasFrame
            ? new Shadow
            {
                Brush = new SolidColorBrush(primary.WithAlpha(0.32f)),
                Radius = (float)Math.Clamp(8 + normalized.Intensity * 6, 8, 18),
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
