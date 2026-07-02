using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Components;

public sealed class IdentityPlateView : ContentView
{
    private readonly GraphicsView _materialLayer;
    private readonly GraphicsView _particleLayer;
    private readonly Border _frame;
    private readonly Label _text;
    private readonly NameSurfaceDrawable _surfaceDrawable = new();
    private readonly NameSurfaceParticleDrawable _particleDrawable = new();
    private uint _animationToken;
    private double _progress;

    public IdentityPlateView()
    {
        FlowDirection = FlowDirection.RightToLeft;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;

        _materialLayer = new GraphicsView
        {
            Drawable = _surfaceDrawable,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        _particleLayer = new GraphicsView
        {
            Drawable = _particleDrawable,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

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

        Content = new Grid
        {
            Children =
            {
                _materialLayer,
                _frame,
                _particleLayer
            }
        };

        Loaded += (_, _) =>
        {
            if (_text.Text?.Length > 0)
                StartMotion((_surfaceDrawable.ActivePreset ?? TypographyIdentityPreset.CreateDefault()).Normalized());
        };
        Unloaded += (_, _) => StopMotion();
        Bind("اسم اللاعب", TypographyIdentityPreset.CreateDefault());
    }

    public void Bind(string text, TypographyIdentityPreset? preset)
    {
        StopMotion();
        var normalized = (preset ?? TypographyIdentityPreset.CreateDefault()).Normalized();
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
        _frame.TranslationX = 0;
        _frame.TranslationY = 0;
        _frame.Stroke = hasFrame ? primary.WithAlpha((float)Math.Clamp(0.35 + normalized.Intensity * 0.3, 0.35, 0.9)) : Colors.Transparent;
        _frame.StrokeThickness = hasFrame ? normalized.FrameThickness : 0;
        _frame.Padding = hasFrame ? new Thickness(10 + normalized.FrameThickness, 3, 10 + normalized.FrameThickness, 4) : new Thickness(0, 1);
        _frame.Background = hasFrame ? CreateFrameBackground(normalized, secondary) : new SolidColorBrush(Colors.Transparent);
        _frame.Shadow = BuildShadow(normalized, primary, hasFrame);

        _progress = 0;
        ConfigureDrawables(normalized);
        StartMotion(normalized);
    }

    private void ConfigureDrawables(TypographyIdentityPreset preset)
    {
        _surfaceDrawable.Configure(preset, _progress);
        _particleDrawable.Configure(preset, _progress);
        _materialLayer.Invalidate();
        _particleLayer.Invalidate();
    }

    private Shadow? BuildShadow(TypographyIdentityPreset preset, Color primary, bool hasFrame)
    {
        var hasShadow = hasFrame ||
            !string.Equals(preset.ShadowPreset, "None", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(preset.LightingPreset, "None", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(preset.ParticlePreset, "None", StringComparison.OrdinalIgnoreCase);

        if (!hasShadow)
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

        if (!ShouldAnimate(preset))
            return;

        var token = ++_animationToken;
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            while (token == _animationToken && Handler != null)
            {
                var duration = (uint)Math.Clamp(900 / Math.Max(0.45, preset.Speed), 320, 1600);
                _progress = (_progress + 0.125) % 1;
                ConfigureDrawables(preset);

                var motion = preset.MotionPreset.Trim();
                if (motion is "Breath" or "Breathing")
                {
                    await _frame.ScaleToAsync(1.0 + Math.Min(0.055, preset.Intensity * 0.035), duration, Easing.SinInOut);
                    await _frame.ScaleToAsync(1.0, duration, Easing.SinInOut);
                    continue;
                }

                if (motion is "Pulse" or "Heartbeat" or "ShockPulse")
                {
                    await _text.ScaleToAsync(1.0 + Math.Min(0.09, preset.Intensity * 0.055), duration / 2, Easing.CubicOut);
                    await _text.ScaleToAsync(1.0, duration / 2, Easing.CubicIn);
                    continue;
                }

                var offset = ResolveMotionOffset(motion, preset.Intensity);
                if (Math.Abs(offset.X) > 0.1 || Math.Abs(offset.Y) > 0.1)
                    await _frame.TranslateToAsync(offset.X, offset.Y, duration, Easing.SinInOut);

                await _frame.FadeToAsync(Math.Clamp(preset.Opacity * 0.76, 0.35, 1), duration, Easing.SinInOut);

                if (Math.Abs(offset.X) > 0.1 || Math.Abs(offset.Y) > 0.1)
                    await _frame.TranslateToAsync(0, 0, duration, Easing.SinInOut);

                await _frame.FadeToAsync(preset.Opacity, duration, Easing.SinInOut);
            }
        });
    }

    private static bool ShouldAnimate(TypographyIdentityPreset preset) =>
        !string.Equals(preset.MotionPreset, "None", StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(preset.ParticlePreset, "None", StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(preset.DistortionPreset, "None", StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(preset.ReflectionPreset, "None", StringComparison.OrdinalIgnoreCase) ||
        preset.LightingPreset is "SoftShine" or "TopSheen" or "MovingHighlight" or "MetallicSweep" or "Aurora" or "EnergyCore";

    private void StopMotion()
    {
        _animationToken++;
        _text.CancelAnimations();
        _frame.CancelAnimations();
        _materialLayer.CancelAnimations();
        _particleLayer.CancelAnimations();
    }

    private static Brush CreateFrameBackground(TypographyIdentityPreset preset, Color secondary)
    {
        var alpha = preset.MaterialPreset switch
        {
            "EmeraldGlass" or "Emerald" => 0.42f,
            "RubyLacquer" => 0.48f,
            "PearlSteel" or "Pearl" => 0.38f,
            "Obsidian" => 0.68f,
            "CarbonFiber" => 0.58f,
            "Diamond" or "Crystal" or "NeonGlass" or "Ice" => 0.32f,
            "Lava" => 0.62f,
            "LiquidMetal" or "RealMetallicGold" => 0.46f,
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

    private static Point ResolveMotionOffset(string motion, double intensity)
    {
        var amount = Math.Clamp(intensity * 2.2, 1, 5);
        return motion switch
        {
            "Floating" or "Wind" => new Point(0, -amount),
            "OrganicMotion" or "LiquidMotion" => new Point(amount * 0.45, -amount * 0.35),
            "EnergyWave" or "MagneticDrift" => new Point(amount, 0),
            "Gravity" => new Point(0, amount),
            "HeatDistortion" or "ShockPulse" => new Point(amount * 0.35, amount * 0.15),
            _ => new Point(0, 0)
        };
    }
}

internal sealed class NameSurfaceDrawable : IDrawable
{
    public TypographyIdentityPreset? ActivePreset { get; private set; }
    private float _progress;

    public void Configure(TypographyIdentityPreset preset, double progress)
    {
        ActivePreset = preset.Normalized();
        _progress = (float)(progress % 1);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var preset = ActivePreset;
        if (preset == null || dirtyRect.Width <= 1 || dirtyRect.Height <= 1)
            return;

        var primary = Color.FromArgb(preset.PrimaryColor);
        var secondary = Color.FromArgb(preset.SecondaryColor);
        var intensity = (float)Math.Clamp(preset.Intensity, 0.2, 1.8);
        var sweep = dirtyRect.Left + dirtyRect.Width * _progress;

        canvas.SaveState();
        canvas.Alpha = (float)Math.Clamp(preset.Opacity, 0.35, 1);
        DrawShadow(canvas, dirtyRect, preset, primary, intensity);
        DrawMaterial(canvas, dirtyRect, preset, primary, secondary, intensity);
        DrawLighting(canvas, dirtyRect, preset, primary, secondary, sweep, intensity);
        DrawReflection(canvas, dirtyRect, preset, primary, sweep, intensity);
        DrawDistortion(canvas, dirtyRect, preset, secondary, intensity);
        canvas.RestoreState();
    }

    private static void DrawMaterial(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color primary, Color secondary, float intensity)
    {
        canvas.FillColor = MaterialColor(preset.MaterialPreset, primary, secondary).WithAlpha(0.06f + (0.04f * intensity));
        canvas.FillRoundedRectangle(rect.Inflate(-1, -1), 13);

        if (preset.MaterialPreset is "CarbonFiber" or "Obsidian")
        {
            canvas.StrokeColor = primary.WithAlpha(0.10f);
            canvas.StrokeSize = 1;
            for (var x = rect.Left; x < rect.Right; x += 8)
                canvas.DrawLine(x, rect.Top, x + rect.Height, rect.Bottom);
        }
    }

    private static void DrawLighting(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color primary, Color secondary, float sweep, float intensity)
    {
        if (preset.LightingPreset is "None" or "LowContrast")
            return;

        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize = 2.4f + (1.8f * intensity);
        canvas.StrokeColor = primary.WithAlpha(0.26f + (0.22f * intensity));
        canvas.DrawLine(sweep - 34, rect.Top + 5, sweep + 14, rect.Bottom - 5);

        if (preset.LightingPreset is "Aurora" or "CosmicReflection" or "EnergyCore")
        {
            canvas.StrokeColor = secondary.WithAlpha(0.22f + (0.18f * intensity));
            canvas.DrawLine(rect.Left + 8, rect.Top + (rect.Height * 0.35f), rect.Right - 8, rect.Top + (rect.Height * 0.63f));
        }
    }

    private static void DrawShadow(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color primary, float intensity)
    {
        if (preset.ShadowPreset == "None")
            return;

        canvas.FillColor = primary.WithAlpha(preset.ShadowPreset == "Glow" ? 0.12f : 0.075f);
        canvas.FillRoundedRectangle(new RectF(rect.Left + 1, rect.Top + 3, rect.Width - 2, rect.Height - 3), 14 + (4 * intensity));
    }

    private static void DrawReflection(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color primary, float sweep, float intensity)
    {
        if (preset.ReflectionPreset == "None")
            return;

        canvas.StrokeSize = 1.4f + intensity;
        canvas.StrokeColor = Colors.White.WithAlpha(0.12f + (0.10f * intensity));
        canvas.DrawLine(rect.Left + 12, rect.Top + 6, rect.Right - 12, rect.Top + 6);
        canvas.StrokeColor = primary.WithAlpha(0.18f);
        canvas.DrawLine(sweep - 18, rect.Top + 8, sweep + 24, rect.Top + 8);
    }

    private static void DrawDistortion(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color secondary, float intensity)
    {
        if (preset.DistortionPreset == "None")
            return;

        canvas.StrokeColor = secondary.WithAlpha(0.14f + (0.08f * intensity));
        canvas.StrokeSize = 1.1f;
        var amp = preset.DistortionPreset is "Heat" or "Ripple" or "Shockwave" ? 4f : 2.2f;
        for (var y = rect.Top + 8; y < rect.Bottom; y += 9)
        {
            var phase = y * 0.1f;
            canvas.DrawLine(rect.Left + 6, y, rect.Right - 6, y + MathF.Sin(phase) * amp);
        }
    }

    private static Color MaterialColor(string material, Color primary, Color secondary) => material switch
    {
        "Obsidian" or "Shadow" => Colors.Black,
        "Ice" or "Crystal" or "Diamond" => Colors.White,
        "Lava" => Color.FromArgb("#FF4A12"),
        "Emerald" or "EmeraldGlass" => Color.FromArgb("#18D37E"),
        "NeonGlass" => Color.FromArgb("#2DF7FF"),
        "Pearl" or "PearlSteel" => Color.FromArgb("#FFF6E3"),
        _ => secondary.WithAlpha(0.8f)
    };
}

internal sealed class NameSurfaceParticleDrawable : IDrawable
{
    private TypographyIdentityPreset _preset = TypographyIdentityPreset.CreateDefault();
    private float _progress;

    public void Configure(TypographyIdentityPreset preset, double progress)
    {
        _preset = preset.Normalized();
        _progress = (float)(progress % 1);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_preset.ParticlePreset == "None")
            return;

        var primary = Color.FromArgb(_preset.PrimaryColor);
        var secondary = Color.FromArgb(_preset.SecondaryColor);
        var count = _preset.ParticlePreset is "Galaxy" or "Stars" or "Snow" ? 18 : 10;
        var intensity = (float)Math.Clamp(_preset.Intensity, 0.2, 1.8);

        canvas.SaveState();
        for (var i = 0; i < count; i++)
        {
            var phase = i / (float)count;
            var x = dirtyRect.Left + dirtyRect.Width * ((phase + _progress) % 1);
            var wave = 0.5f + 0.5f * MathF.Sin((_progress + phase) * MathF.PI * 2);
            var y = dirtyRect.Top + 5 + (dirtyRect.Height - 10) * wave;
            var size = ParticleSize(_preset.ParticlePreset, intensity, phase);
            canvas.FillColor = (i % 2 == 0 ? primary : secondary).WithAlpha(0.18f + (0.38f * wave));
            canvas.FillCircle(x, y, size);
        }
        canvas.RestoreState();
    }

    private static float ParticleSize(string preset, float intensity, float phase) => preset switch
    {
        "Pixels" => 1.5f,
        "CrystalShards" => 2.8f + intensity,
        "Fire" or "Embers" or "Spark" or "Lightning" => 2.2f + (1.4f * intensity * phase),
        _ => 1.8f + (1.1f * intensity)
    };
}
