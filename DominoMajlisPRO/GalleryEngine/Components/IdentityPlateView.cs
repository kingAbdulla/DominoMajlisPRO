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
    private IDispatcherTimer? _animationTimer;
    private TypographyIdentityPreset? _activePreset;
    private uint _animationToken;
    private double _progress;
    private DateTime _lastFrameUtc;

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
            if (_activePreset != null)
                StartMotion(_activePreset);
        };
        Unloaded += (_, _) => StopMotion();
        Bind("اسم اللاعب", null);
    }

    public void Bind(string text, TypographyIdentityPreset? preset)
    {
        StopMotion();
        _text.Text = string.IsNullOrWhiteSpace(text) ? "اسم اللاعب" : text.Trim();

        if (preset == null)
        {
            _activePreset = null;
            BindPlainText();
            return;
        }

        var normalized = preset.Normalized();
        _activePreset = normalized;
        var textColor = Color.FromArgb(normalized.TextColor);
        var frameColor = Color.FromArgb(normalized.FrameColor);
        var secondary = Color.FromArgb(normalized.SecondaryColor);
        var hasFrame = !string.Equals(normalized.FrameStylePreset, "None", StringComparison.OrdinalIgnoreCase);

        _text.FontFamily = normalized.FontFamily;
        _text.FontSize = Math.Clamp(normalized.FontSize * normalized.Scale, 11, 34);
        _text.TextColor = textColor;
        _text.Opacity = normalized.Opacity;
        _text.Scale = 1;
        _text.TranslationX = 0;
        _text.TranslationY = 0;

        _frame.Scale = 1;
        _frame.Rotation = 0;
        _frame.Opacity = normalized.Opacity;
        _frame.TranslationX = 0;
        _frame.TranslationY = 0;
        _frame.Stroke = hasFrame ? frameColor.WithAlpha((float)Math.Clamp(0.35 + normalized.Intensity * 0.3, 0.35, 0.9)) : Colors.Transparent;
        _frame.StrokeThickness = hasFrame ? normalized.FrameThickness : 0;
        _frame.Padding = hasFrame ? new Thickness(10 + normalized.FrameThickness, 3, 10 + normalized.FrameThickness, 4) : new Thickness(0, 1);
        _frame.Background = hasFrame ? CreateFrameBackground(normalized, secondary) : new SolidColorBrush(Colors.Transparent);
        _frame.Shadow = BuildShadow(normalized, Color.FromArgb(normalized.ShadowColor), hasFrame);

        _progress = 0;
        ConfigureDrawables(normalized);
        StartMotion(normalized);
    }

    private void BindPlainText()
    {
        _text.FontFamily = TypographyFontCatalog.DefaultFontFamily;
        _text.FontSize = 18;
        _text.TextColor = Colors.White;
        _text.Opacity = 1;
        _text.Scale = 1;
        _text.TranslationX = 0;
        _text.TranslationY = 0;
        _frame.Scale = 1;
        _frame.Opacity = 1;
        _frame.TranslationX = 0;
        _frame.TranslationY = 0;
        _frame.Stroke = Colors.Transparent;
        _frame.StrokeThickness = 0;
        _frame.Padding = new Thickness(0, 1);
        _frame.Background = new SolidColorBrush(Colors.Transparent);
        _frame.Shadow = null;
        _surfaceDrawable.Clear();
        _particleDrawable.Clear();
        _materialLayer.Invalidate();
        _particleLayer.Invalidate();
    }

    private void ConfigureDrawables(TypographyIdentityPreset preset)
    {
        _surfaceDrawable.Configure(preset, _progress);
        _particleDrawable.Configure(preset, _progress);
        _materialLayer.Invalidate();
        _particleLayer.Invalidate();
    }

    private Shadow? BuildShadow(TypographyIdentityPreset preset, Color shadowColor, bool hasFrame)
    {
        var hasShadow = hasFrame ||
            !string.Equals(preset.ShadowPreset, "None", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(preset.LightingPreset, "None", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(preset.ParticlePreset, "None", StringComparison.OrdinalIgnoreCase);

        if (!hasShadow)
            return null;

        return new Shadow
        {
            Brush = new SolidColorBrush(shadowColor.WithAlpha((float)Math.Clamp(0.20 + preset.Intensity * 0.18, 0.20, 0.55))),
            Radius = (float)Math.Clamp(8 + preset.Intensity * 9, 8, 24),
            Opacity = (float)Math.Clamp(0.25 + preset.Intensity * 0.18, 0.25, 0.55),
            Offset = new Point(0, 2)
        };
    }

    private void StartMotion(TypographyIdentityPreset preset)
    {
        if (preset.Speed <= 0 || !HasMotionPreset(preset))
            return;

        var token = ++_animationToken;
        _lastFrameUtc = DateTime.UtcNow;
        _animationTimer?.Stop();
        _animationTimer = Dispatcher.CreateTimer();
        _animationTimer.Interval = TimeSpan.FromMilliseconds(16);
        _animationTimer.Tick += (_, _) =>
        {
            if (token != _animationToken || Handler == null)
            {
                _animationTimer?.Stop();
                return;
            }

            var now = DateTime.UtcNow;
            var delta = Math.Clamp((now - _lastFrameUtc).TotalSeconds, 0.001, 0.05);
            _lastFrameUtc = now;
            _progress = (_progress + (delta * 0.24 * Math.Clamp(preset.Speed, 0.45, 2.4))) % 1;
            ConfigureDrawables(preset);
            ApplyLayerMotion(preset);
        };
        _animationTimer.Start();
    }

    private static bool HasMotionPreset(TypographyIdentityPreset preset)
    {
        var motion = preset.MotionPreset?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(motion) &&
               !string.Equals(motion, "None", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(motion, "Flat", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(motion, "Static", StringComparison.OrdinalIgnoreCase);
    }

    private void StopMotion()
    {
        _animationToken++;
        _text.CancelAnimations();
        _frame.CancelAnimations();
        _materialLayer.CancelAnimations();
        _particleLayer.CancelAnimations();
        _animationTimer?.Stop();
        _animationTimer = null;
    }

    private void ApplyLayerMotion(TypographyIdentityPreset preset)
    {
        var wave = Math.Sin(_progress * Math.PI * 2);
        var smoothWave = (1 - Math.Cos(_progress * Math.PI * 2)) * 0.5;
        var motion = preset.MotionPreset.Trim();
        _frame.Scale = motion is "Breath" or "Breathing" or "OrganicMotion"
            ? 1 + Math.Min(0.028, preset.Intensity * 0.018) * wave
            : 1;
        _text.Scale = motion is "Pulse" or "Heartbeat" or "ShockPulse" or "EnergyWave"
            ? 1 + Math.Min(0.038, preset.Intensity * 0.026) * smoothWave
            : 1;
        var offset = motion is "Floating" or "Wind" or "Gravity" or "MagneticDrift" or "LiquidMotion"
            ? ResolveMotionOffset(motion, preset.Intensity)
            : new Point(0, 0);
        _frame.TranslationX = offset.X * wave;
        _frame.TranslationY = offset.Y * wave;
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
        var amount = Math.Clamp(intensity * 1.45, 0.8, 3.2);
        return motion switch
        {
            "Floating" or "Wind" or "LiquidMotion" => new Point(0, -amount),
            "Gravity" => new Point(0, amount),
            "MagneticDrift" => new Point(amount, 0),
            _ => new Point(0, 0)
        };
    }
}

internal sealed class NameSurfaceDrawable : IDrawable
{
    private TypographyIdentityPreset? _preset;
    private float _progress;

    public void Configure(TypographyIdentityPreset preset, double progress)
    {
        _preset = preset.Normalized();
        _progress = (float)(progress % 1);
    }

    public void Clear() => _preset = null;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var preset = _preset;
        if (preset == null || dirtyRect.Width <= 1 || dirtyRect.Height <= 1)
            return;

        var primary = Color.FromArgb(preset.PrimaryColor);
        var secondary = Color.FromArgb(preset.SecondaryColor);
        var lighting = Color.FromArgb(preset.LightingColor);
        var reflection = Color.FromArgb(preset.ReflectionColor);
        var shadow = Color.FromArgb(preset.ShadowColor);
        var intensity = (float)Math.Clamp(preset.Intensity, 0.2, 1.8);
        var sweep = dirtyRect.Left + dirtyRect.Width * _progress;

        canvas.SaveState();
        canvas.Alpha = (float)Math.Clamp(preset.Opacity, 0.35, 1);
        DrawShadow(canvas, dirtyRect, preset, shadow, intensity);
        DrawMaterial(canvas, dirtyRect, preset, primary, secondary, intensity);
        DrawLighting(canvas, dirtyRect, preset, lighting, secondary, sweep, intensity);
        DrawReflection(canvas, dirtyRect, preset, reflection, sweep, intensity);
        DrawDistortion(canvas, dirtyRect, preset, lighting, intensity, _progress);
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

    private static void DrawLighting(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color lighting, Color secondary, float sweep, float intensity)
    {
        if (preset.LightingPreset is "None" or "LowContrast")
            return;

        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize = 2.4f + (1.8f * intensity);
        canvas.StrokeColor = lighting.WithAlpha(0.26f + (0.22f * intensity));
        canvas.DrawLine(sweep - 34, rect.Top + 5, sweep + 14, rect.Bottom - 5);

        if (preset.LightingPreset is "Aurora" or "CosmicReflection" or "EnergyCore")
        {
            canvas.StrokeColor = secondary.WithAlpha(0.22f + (0.18f * intensity));
            canvas.DrawLine(rect.Left + 8, rect.Top + (rect.Height * 0.35f), rect.Right - 8, rect.Top + (rect.Height * 0.63f));
        }
    }

    private static void DrawShadow(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color shadow, float intensity)
    {
        if (preset.ShadowPreset == "None")
            return;

        canvas.FillColor = shadow.WithAlpha(preset.ShadowPreset == "Glow" ? 0.12f : 0.075f);
        canvas.FillRoundedRectangle(new RectF(rect.Left + 1, rect.Top + 3, rect.Width - 2, rect.Height - 3), 14 + (4 * intensity));
    }

    private static void DrawReflection(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color reflection, float sweep, float intensity)
    {
        if (preset.ReflectionPreset == "None")
            return;

        canvas.StrokeSize = 1.4f + intensity;
        canvas.StrokeColor = reflection.WithAlpha(0.12f + (0.10f * intensity));
        canvas.DrawLine(rect.Left + 12, rect.Top + 6, rect.Right - 12, rect.Top + 6);
        canvas.StrokeColor = reflection.WithAlpha(0.18f);
        canvas.DrawLine(sweep - 18, rect.Top + 8, sweep + 24, rect.Top + 8);
    }

    private static void DrawDistortion(ICanvas canvas, RectF rect, TypographyIdentityPreset preset, Color lighting, float intensity, float progress)
    {
        if (preset.DistortionPreset == "None")
            return;

        canvas.StrokeColor = lighting.WithAlpha(0.14f + (0.08f * intensity));
        canvas.StrokeSize = 1.1f;
        var amp = preset.DistortionPreset is "Heat" or "Ripple" or "Shockwave" ? 4f : 2.2f;
        for (var y = rect.Top + 8; y < rect.Bottom; y += 9)
        {
            var phase = (progress * MathF.PI * 2) + (y * 0.1f);
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
    private TypographyIdentityPreset? _preset;
    private float _progress;

    public void Configure(TypographyIdentityPreset preset, double progress)
    {
        _preset = preset.Normalized();
        _progress = (float)(progress % 1);
    }

    public void Clear() => _preset = null;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var preset = _preset;
        if (preset == null || preset.ParticlePreset == "None")
            return;

        var primary = Color.FromArgb(preset.ParticleColor);
        var secondary = Color.FromArgb(preset.SecondaryColor);
        var count = preset.ParticlePreset is "Galaxy" or "Stars" or "Snow" ? 18 : 10;
        var intensity = (float)Math.Clamp(preset.Intensity, 0.2, 1.8);

        canvas.SaveState();
        for (var i = 0; i < count; i++)
        {
            var phase = i / (float)count;
            var x = dirtyRect.Left + dirtyRect.Width * ((phase + _progress) % 1);
            var wave = 0.5f + 0.5f * MathF.Sin((_progress + phase) * MathF.PI * 2);
            var y = dirtyRect.Top + 5 + (dirtyRect.Height - 10) * wave;
            var size = ParticleSize(preset.ParticlePreset, intensity, phase);
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
