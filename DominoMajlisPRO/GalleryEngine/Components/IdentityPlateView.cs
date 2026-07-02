using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components;

public sealed class IdentityPlateView : ContentView
{
    private readonly Border _frame;
    private readonly Label _text;
    private TypographyIdentityPreset? _activePreset;
    private uint _animationToken;

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
        Unloaded += (_, _) => StopMotion();
        Bind("اسم اللاعب", TypographyIdentityPreset.CreateDefault());
    }

    public void Bind(string text, TypographyIdentityPreset? preset)
    {
        StopMotion();
        var normalized = (preset ?? TypographyIdentityPreset.CreateDefault()).Normalized();
        _activePreset = normalized;
        var primary = Color.FromArgb(normalized.PrimaryColor);
        var secondary = Color.FromArgb(normalized.SecondaryColor);
        var hasFrame = !string.Equals(normalized.FrameStylePreset, "None", StringComparison.OrdinalIgnoreCase);

        _text.Text = string.IsNullOrWhiteSpace(text) ? "اسم اللاعب" : text.Trim();
        _text.FontFamily = normalized.FontFamily;
        _text.FontSize = Math.Clamp(normalized.FontSize * normalized.Scale, 11, 34);
        _text.TextColor = primary;
        _text.Opacity = normalized.Opacity;
        _text.Scale = 1;
        _text.TranslationX = 0;
        _text.TranslationY = 0;
        _frame.Scale = 1;
        _frame.Rotation = 0;
        _frame.Opacity = normalized.Opacity;

        _frame.Stroke = hasFrame ? primary.WithAlpha((float)Math.Clamp(0.35 + normalized.Intensity * 0.3, 0.35, 0.9)) : Colors.Transparent;
        _frame.StrokeThickness = hasFrame ? normalized.FrameThickness : 0;
        _frame.Padding = hasFrame ? new Thickness(10 + normalized.FrameThickness, 3, 10 + normalized.FrameThickness, 4) : new Thickness(0, 1);
        _frame.Background = hasFrame ? CreateFrameBackground(normalized, secondary) : new SolidColorBrush(Colors.Transparent);
        _frame.Shadow = BuildShadow(normalized, primary, hasFrame);

        StartMotion(normalized);
    }

    private Shadow? BuildShadow(TypographyIdentityPreset preset, Color primary, bool hasFrame)
    {
        var glowMotion = string.Equals(preset.MotionPreset, "Breath", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(preset.ParticlePreset, "Dust", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(preset.LightingPreset, "SoftShine", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(preset.LightingPreset, "TopSheen", StringComparison.OrdinalIgnoreCase);

        if (!hasFrame && !glowMotion)
            return null;

        return new Shadow
        {
            Brush = new SolidColorBrush(primary.WithAlpha((float)Math.Clamp(0.20 + preset.Intensity * 0.18, 0.20, 0.55))),
            Radius = (float)Math.Clamp(8 + preset.Intensity * 9, 8, 24),
            Opacity = (float)Math.Clamp(0.25 + preset.Intensity * 0.18, 0.25, 0.55),
            Offset = new Point(0, 2)
        };
    }

    private void StartMotion(TypographyIdentityPreset preset)
    {
        if (preset.Speed <= 0)
            return;

        var motion = preset.MotionPreset?.Trim() ?? "None";
        var particles = preset.ParticlePreset?.Trim() ?? "None";
        var lighting = preset.LightingPreset?.Trim() ?? "None";
        var shouldAnimate = !string.Equals(motion, "None", StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(particles, "None", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(lighting, "SoftShine", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(lighting, "TopSheen", StringComparison.OrdinalIgnoreCase);
        if (!shouldAnimate)
            return;

        var token = ++_animationToken;
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            while (token == _animationToken && Handler != null)
            {
                var duration = (uint)Math.Clamp(900 / Math.Max(0.45, preset.Speed), 320, 1600);
                if (string.Equals(motion, "Breath", StringComparison.OrdinalIgnoreCase))
                {
                    await _frame.ScaleToAsync(1.0 + Math.Min(0.055, preset.Intensity * 0.035), duration, Easing.SinInOut);
                    await _frame.ScaleToAsync(1.0, duration, Easing.SinInOut);
                }
                else if (string.Equals(motion, "Pulse", StringComparison.OrdinalIgnoreCase))
                {
                    await _text.ScaleToAsync(1.0 + Math.Min(0.075, preset.Intensity * 0.045), duration / 2, Easing.CubicOut);
                    await _text.ScaleToAsync(1.0, duration / 2, Easing.CubicIn);
                }
                else if (string.Equals(particles, "Dust", StringComparison.OrdinalIgnoreCase))
                {
                    await _text.TranslateToAsync(1.2, -0.8, duration / 2, Easing.SinInOut);
                    await _text.TranslateToAsync(-1.0, 0.7, duration / 2, Easing.SinInOut);
                    await _text.TranslateToAsync(0, 0, duration / 2, Easing.SinInOut);
                }
                else
                {
                    await _frame.FadeToAsync(Math.Clamp(preset.Opacity * 0.72, 0.35, 1), duration, Easing.SinInOut);
                    await _frame.FadeToAsync(preset.Opacity, duration, Easing.SinInOut);
                }
            }
        });
    }

    private void StopMotion()
    {
        _animationToken++;
        _text.CancelAnimations();
        _frame.CancelAnimations();
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
