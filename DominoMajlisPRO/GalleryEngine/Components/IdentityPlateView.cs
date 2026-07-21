using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
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

    public static readonly BindableProperty RenderingContextProperty =
        BindableProperty.Create(
            nameof(RenderingContext),
            typeof(NameSurfaceRenderingContext),
            typeof(IdentityPlateView),
            NameSurfaceRenderingContext.TeamProfile,
            propertyChanged: (bindable, _, _) => ((IdentityPlateView)bindable).Apply());

    private readonly Border _frame;
    private readonly GraphicsView _frameLayer;
    private readonly Label _shadow;
    private readonly Label _text;
    private readonly Label _highlight;
    private readonly Grid _layers;
    private readonly BoxView[] _particles;
    private bool _animationEnabled;
    private IDisposable? _clockSubscription;
    private TypographyIdentityPreset _activePreset = TypographyIdentityPreset.CreateDefault();
    private double _animationEpoch;

    public IdentityPlateView()
    {
        FlowDirection = FlowDirection.RightToLeft;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        IsClippedToBounds = true;

        _shadow = CreateLabel();
        _shadow.TranslationY = 1.2;
        _shadow.Opacity = 0.45;
        _shadow.TextColor = Colors.Black;

        _text = CreateLabel();

        _highlight = CreateLabel();
        _highlight.TextColor = Colors.White;
        _highlight.InputTransparent = true;

        _frameLayer = new GraphicsView
        {
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            IsVisible = false
        };

        _particles = Enumerable.Range(0, 3)
            .Select(index => new BoxView
            {
                WidthRequest = index == 1 ? 3 : 2,
                HeightRequest = index == 1 ? 3 : 2,
                CornerRadius = 2,
                HorizontalOptions = index switch
                {
                    0 => LayoutOptions.Start,
                    1 => LayoutOptions.Center,
                    _ => LayoutOptions.End
                },
                VerticalOptions = LayoutOptions.End,
                IsVisible = false,
                InputTransparent = true
            })
            .ToArray();

        _layers = new Grid
        {
            Padding = new Thickness(10, 3),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto) },
            Children = { _frameLayer, _shadow, _text, _highlight, _particles[0], _particles[1], _particles[2] }
        };

        _frame = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            StrokeThickness = 1.4,
            Padding = 0,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            Content = _layers
        };

        Content = _frame;
        Loaded += (_, _) => EnsureClock();
        Unloaded += (_, _) => StopClock();
        SizeChanged += (_, _) => Apply();
        Apply();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent == null)
            StopClock();
        else
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

    public NameSurfaceRenderingContext RenderingContext
    {
        get => (NameSurfaceRenderingContext)GetValue(RenderingContextProperty);
        set => SetValue(RenderingContextProperty, value);
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
        var materialColor = ResolveMaterialColor(primary, preset.MaterialPreset, preset.Brightness);
        var hasFrame = preset.FrameStylePreset != "None";
        var requestedFontSize = Math.Clamp(preset.FontSize * preset.Scale, 8, 34);
        var availableWidth = Width > 0
            ? Math.Max(20, Width - (hasFrame ? 24 + preset.FrameThickness * 2 : 4))
            : double.PositiveInfinity;
        var fittedFontSize = double.IsPositiveInfinity(availableWidth)
            ? requestedFontSize
            : availableWidth / Math.Max(1, text.Length * 0.68);
        var lengthFittedFontSize = requestedFontSize * Math.Min(1, 5.2 / Math.Max(5.2, text.Length));
        var fontSize = Math.Clamp(
            Math.Min(requestedFontSize, Math.Min(fittedFontSize, lengthFittedFontSize)),
            8,
            34);

        _text.Text = text;
        _shadow.Text = text;
        _highlight.Text = text;
        _text.FontFamily = preset.FontFamily;
        _shadow.FontFamily = preset.FontFamily;
        _highlight.FontFamily = preset.FontFamily;
        _text.FontSize = fontSize;
        _shadow.FontSize = fontSize;
        _highlight.FontSize = fontSize;
        _text.TextColor = materialColor;
        _highlight.TextColor = ResolveHighlightColor(preset.MaterialPreset, preset.LightingPreset);
        _text.Opacity = preset.Opacity;
        _shadow.TranslationY = 0.6 + preset.Depth * 2.4;
        _shadow.Opacity = Math.Clamp(0.18 + preset.Depth * 0.52 + preset.MicroNoise * 0.08, 0.18, 0.72);
        _highlight.Opacity = ResolveHighlightOpacity(preset);
        _highlight.TranslationY = -Math.Clamp(0.2 + preset.Gloss * 0.55, 0.2, 0.85);
        _highlight.ScaleX = ResolveLightingScale(preset.LightingPreset);
        _frame.Stroke = Colors.Transparent;
        _frame.StrokeThickness = 0;
        _frame.Background = new SolidColorBrush(Colors.Transparent);
        _frameLayer.IsVisible = hasFrame;
        _frameLayer.Drawable = hasFrame
            ? new NameFrameDrawable(preset.FrameStylePreset, materialColor, secondary, preset.FrameThickness, preset.Intensity)
            : null;
        _frameLayer.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(materialColor.WithAlpha(0.32f)),
                Radius = (float)Math.Clamp(8 + preset.Intensity * 6, 8, 18),
                Opacity = hasFrame ? 0.38f : 0,
                Offset = new Point(0, 2)
            };
        _frameLayer.Invalidate();
        _layers.Padding = new Thickness(10, 3, 10, 4);

        StartMotion(preset, materialColor);
    }

    private void StartMotion(TypographyIdentityPreset preset, Color primary)
    {
        StopClock();
        Scale = 1;
        TranslationY = 0;
        _text.TranslationX = 0;
        _shadow.TranslationX = 0;
        _highlight.TranslationX = 0;

        var hasMotion = !string.Equals(preset.MotionPreset, "None", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(preset.DistortionPreset, "None", StringComparison.OrdinalIgnoreCase);
        var hasParticles = !string.Equals(preset.ParticlePreset, "None", StringComparison.OrdinalIgnoreCase);
        _animationEnabled = hasMotion || hasParticles;
        _activePreset = preset;

        var particleBudget = RenderingContext switch
        {
            NameSurfaceRenderingContext.DeveloperPreview or NameSurfaceRenderingContext.Store => 3,
            NameSurfaceRenderingContext.CreateTeam or NameSurfaceRenderingContext.PlayerProfile or
                NameSurfaceRenderingContext.TeamProfile or NameSurfaceRenderingContext.Victory => 2,
            _ => 1
        };
        for (var index = 0; index < _particles.Length; index++)
        {
            var particle = _particles[index];
            particle.Color = ResolveParticleColor(preset.ParticlePreset, primary);
            particle.IsVisible = hasParticles && index < particleBudget;
            particle.Opacity = particle.IsVisible ? 0.2 : 0;
            particle.TranslationY = 0;
        }

        if (!_animationEnabled || Parent == null)
            return;
        EnsureClock();
    }

    private void EnsureClock()
    {
        if (_clockSubscription != null || !_animationEnabled || !IsVisible || Parent == null)
            return;
        _animationEpoch = -1;
        _clockSubscription = SharedAnimationClock.Subscribe(ApplyAnimationFrame);
    }

    private void StopClock()
    {
        _clockSubscription?.Dispose();
        _clockSubscription = null;
    }

    private void ApplyAnimationFrame(double sharedElapsed)
    {
        if (!_animationEnabled || !IsVisible || Parent == null)
        {
            StopClock();
            return;
        }
        if (_animationEpoch < 0) _animationEpoch = sharedElapsed;
        var preset = _activePreset;
        var time = (sharedElapsed - _animationEpoch) * Math.Clamp(preset.Speed, 0.5, 2);
        var wave = Math.Sin(time * Math.PI * 2 / 1.8);
        var intensity = Math.Clamp(preset.Intensity, 0.2, 1.6);
        Scale = 1;
        TranslationY = 0;
        _text.TranslationX = 0;
        _shadow.TranslationX = 0;
        _highlight.TranslationX = 0;
        _highlight.Opacity = ResolveHighlightOpacity(preset);
        var motion = preset.MotionPreset;
        if (motion is "SoftShine" or "MetallicSweep" or "EnergyWave" or "Wind")
        {
            var offset = wave * 3.2 * intensity;
            _text.TranslationX = offset;
            _shadow.TranslationX = offset * 0.65;
            _highlight.TranslationX = -offset * Math.Clamp(preset.Reflection, 0.1, 1);
            Scale = 1;
            TranslationY = 0;
        }
        else if (motion is "Floating" or "OrganicMotion")
        {
            TranslationY = wave * 1.4 * intensity;
            Scale = 1 + Math.Abs(wave) * 0.006 * intensity;
        }
        else if (motion is "Heartbeat")
        {
            var pulse = Math.Pow(Math.Max(0, wave), 5);
            Scale = 1 + pulse * 0.035 * intensity;
        }
        else if (motion is "ShockPulse")
        {
            var pulse = Math.Pow(Math.Max(0, wave), 7);
            Scale = 1 + pulse * 0.05 * intensity;
            _highlight.Opacity = Math.Clamp(preset.Reflection * 0.3 + pulse * 0.45, 0, 0.7);
        }
        else if (motion is "Gravity" or "MagneticDrift" or "LiquidMotion" or "HeatDistortion")
        {
            TranslationY = wave * 1.2 * intensity;
            _text.TranslationX = Math.Sin(time * 3.7) * 1.5 * intensity;
            _highlight.TranslationX = -_text.TranslationX;
        }
        else if (motion != "None")
        {
            Scale = 1 + (wave + 1) * 0.009 * intensity;
            TranslationY = -Math.Abs(wave) * 0.8 * intensity;
        }

        if (preset.LightingPreset is "MovingHighlight" or "MetallicSweep" or "LightningSweep")
        {
            _highlight.TranslationX += Math.Sin(time * 4.8) * 3.2 * intensity;
        }
        else if (preset.LightingPreset is "Aurora" or "CosmicReflection")
        {
            _highlight.TranslationX += Math.Sin(time * 2.1) * 2.2 * intensity;
            _highlight.TranslationY += Math.Cos(time * 1.6) * 0.7 * intensity;
        }
        else if (preset.LightingPreset is "FireReflection" or "IceReflection")
        {
            _highlight.Opacity = Math.Clamp(_highlight.Opacity + Math.Abs(wave) * 0.18, 0, 0.82);
        }

        var distortion = preset.DistortionPreset;
        if (distortion != "None")
        {
            var distortionScale = distortion is "Shockwave" or "GravityLens" ? 0.018 : 0.009;
            Scale *= 1 + wave * distortionScale * intensity;
            if (distortion is "Heat" or "Ripple" or "Refraction" or "ChromaticAberration")
                _highlight.TranslationX += Math.Sin(time * 7.5) * 1.8 * intensity;
        }

        for (var index = 0; index < _particles.Length; index++)
        {
            var particle = _particles[index];
            if (!particle.IsVisible) continue;
            var phase = (time * 0.58 + index * 0.31) % 1;
            var particleMotion = ParticleMotionMultiplier(preset.ParticlePreset);
            particle.Opacity = Math.Sin(phase * Math.PI) * ParticleOpacity(preset.ParticlePreset);
            particle.TranslationY = -phase * (8 + index * 3) * intensity * particleMotion.Y;
            particle.TranslationX = Math.Sin((time + index) * particleMotion.XFrequency) * particleMotion.X * intensity;
        }
    }

    private static Label CreateLabel() => new()
    {
        FontFamily = TypographyFontCatalog.DefaultFontFamily,
        FontSize = 18,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        MaxLines = 1,
        LineBreakMode = LineBreakMode.NoWrap,
        HorizontalOptions = LayoutOptions.Fill
    };

    private static Color AdjustBrightness(Color color, double brightness)
    {
        var multiplier = 0.72 + Math.Clamp(brightness, 0, 1) * 0.52;
        return new Color(
            (float)Math.Clamp(color.Red * multiplier, 0, 1),
            (float)Math.Clamp(color.Green * multiplier, 0, 1),
            (float)Math.Clamp(color.Blue * multiplier, 0, 1),
            color.Alpha);
    }

    private static Color ResolveMaterialColor(Color source, string material, double brightness)
    {
        var adjusted = AdjustBrightness(source, brightness);
        return material switch
        {
            "RealMetallicGold" => Blend(adjusted, Color.FromArgb("#FFE08A"), 0.32),
            "Obsidian" => Blend(adjusted, Color.FromArgb("#17191D"), 0.58),
            "CarbonFiber" => Blend(adjusted, Color.FromArgb("#34373B"), 0.48),
            "Diamond" => Blend(adjusted, Color.FromArgb("#F7FCFF"), 0.52),
            "Crystal" => Blend(adjusted, Color.FromArgb("#C9F3FF"), 0.38),
            "Lava" => Blend(adjusted, Color.FromArgb("#FF4A16"), 0.44),
            "Ice" => Blend(adjusted, Color.FromArgb("#8BE8FF"), 0.46),
            "LiquidMetal" or "PearlSteel" => Blend(adjusted, Color.FromArgb("#D7DEE6"), 0.38),
            "EmeraldGlass" => Blend(adjusted, Color.FromArgb("#28D98B"), 0.38),
            "NeonGlass" => Blend(adjusted, Color.FromArgb("#73F4FF"), 0.34),
            "RoyalBronze" => Blend(adjusted, Color.FromArgb("#D48B3A"), 0.36),
            "AncientStone" => Blend(adjusted, Color.FromArgb("#AAA28F"), 0.42),
            "IvoryInk" => Blend(adjusted, Color.FromArgb("#FFF0CA"), 0.45),
            "RubyLacquer" => Blend(adjusted, Color.FromArgb("#C6192E"), 0.48),
            _ => adjusted
        };
    }

    private static Color ResolveHighlightColor(string material, string lighting) => lighting switch
    {
        "FireReflection" => Color.FromArgb("#FFB45C"),
        "IceReflection" => Color.FromArgb("#DDFBFF"),
        "CosmicReflection" or "Aurora" => Color.FromArgb("#D9B7FF"),
        "EnergyCore" or "InnerGlow" => Color.FromArgb("#BFFFF6"),
        "RoyalShine" => Color.FromArgb("#FFE7A0"),
        _ => material switch
        {
            "Lava" => Color.FromArgb("#FFD18A"),
            "Ice" or "Crystal" => Color.FromArgb("#E8FCFF"),
            "EmeraldGlass" => Color.FromArgb("#B9FFE2"),
            "Obsidian" or "CarbonFiber" => Color.FromArgb("#AFC0D0"),
            _ => Colors.White
        }
    };

    private static double ResolveHighlightOpacity(TypographyIdentityPreset preset)
    {
        var baseOpacity = preset.Reflection * preset.Specular * (1 - preset.Roughness) * 0.28;
        var boost = preset.LightingPreset switch
        {
            "MovingHighlight" or "MetallicSweep" or "LightningSweep" => 0.08,
            "EnergyCore" or "InnerGlow" => 0.07,
            "RoyalShine" or "TopSheen" => 0.06,
            "LowContrast" => -0.12,
            _ => 0
        };
        return Math.Clamp(baseOpacity + boost, 0, 0.32);
    }

    private static double ResolveLightingScale(string lighting) => lighting switch
    {
        "MovingHighlight" or "MetallicSweep" or "LightningSweep" => 0.52,
        "EnergyCore" or "InnerGlow" => 1.14,
        "LowContrast" => 0.76,
        _ => 1
    };

    private static Color ResolveParticleColor(string particlePreset, Color primary) => particlePreset switch
    {
        "Fire" or "Ash" or "Embers" or "FireEmbers" => Color.FromArgb("#FF8A2A"),
        "Lightning" or "LightningDust" => Color.FromArgb("#BCEBFF"),
        "Snow" or "IceCrystals" or "CrystalShards" => Color.FromArgb("#E8FCFF"),
        "Magic" or "Runes" or "Galaxy" or "CosmicMotes" => Color.FromArgb("#C189FF"),
        "Leaves" => Color.FromArgb("#74D66B"),
        "WaterDrops" => Color.FromArgb("#7FD9FF"),
        "Sand" => Color.FromArgb("#E5C16D"),
        "Petals" => Color.FromArgb("#FF9FD1"),
        _ => primary
    };

    private static double ParticleOpacity(string particlePreset) => particlePreset switch
    {
        "Smoke" or "Dust" => 0.42,
        "Snow" or "Stars" or "Magic" or "Galaxy" => 0.92,
        "Fire" or "Lightning" or "Embers" or "RoyalGlints" => 0.82,
        _ => 0.68
    };

    private static (double X, double Y, double XFrequency) ParticleMotionMultiplier(string particlePreset) => particlePreset switch
    {
        "Smoke" => (2.4, 0.58, 1.2),
        "Fire" or "Embers" or "Ash" => (1.2, 1.35, 3.4),
        "Lightning" or "LightningDust" => (3.1, 0.9, 8.5),
        "Snow" or "Petals" or "Leaves" => (2.6, 0.52, 1.8),
        "Galaxy" or "Stars" or "CosmicMotes" => (2.0, 0.82, 2.6),
        "WaterDrops" => (0.8, 1.5, 1.4),
        _ => (1.4, 1, 2.4)
    };

    private static Color Blend(Color left, Color right, double amount)
    {
        var t = Math.Clamp(amount, 0, 1);
        return new Color(
            (float)(left.Red + (right.Red - left.Red) * t),
            (float)(left.Green + (right.Green - left.Green) * t),
            (float)(left.Blue + (right.Blue - left.Blue) * t),
            left.Alpha);
    }

    private sealed class NameFrameDrawable(
        string style,
        Color primary,
        Color secondary,
        double thickness,
        double intensity) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 2 || dirtyRect.Height <= 2)
                return;

            var inset = (float)Math.Clamp(1.5 + thickness * 0.4, 1.5, 3.5);
            var rect = new RectF(
                dirtyRect.Left + inset,
                dirtyRect.Top + inset,
                dirtyRect.Width - inset * 2,
                dirtyRect.Height - inset * 2);
            var radius = style is "Cyber" or "Electric" ? 7f : Math.Min(13f, rect.Height / 2f);
            canvas.FillColor = style switch
            {
                "Shadow" => Colors.Black.WithAlpha(0.34f),
                "Flame" => Blend(secondary, Color.FromArgb("#6D1D08"), 0.55).WithAlpha(0.46f),
                "Frozen" or "Crystal" => Blend(secondary, Color.FromArgb("#BFEFFF"), 0.42).WithAlpha(0.34f),
                "Galaxy" => Blend(secondary, Color.FromArgb("#321A68"), 0.58).WithAlpha(0.42f),
                "Arabian" or "Royal" => Blend(secondary, Color.FromArgb("#7A4F12"), 0.38).WithAlpha(0.46f),
                _ => secondary.WithAlpha(0.42f)
            };
            canvas.FillRoundedRectangle(rect, radius);
            canvas.StrokeColor = style switch
            {
                "Flame" => Color.FromArgb("#FF7A1C"),
                "Frozen" or "Crystal" => Color.FromArgb("#DDFBFF"),
                "Galaxy" => Color.FromArgb("#B88CFF"),
                "Shadow" => Color.FromArgb("#686868"),
                _ => primary.WithAlpha((float)Math.Clamp(0.5 + intensity * 0.24, 0.5, 0.92))
            };
            canvas.StrokeSize = (float)Math.Clamp(thickness, 0.8, 4);
            canvas.StrokeDashPattern = style switch
            {
                "Electric" or "Cyber" => [7, 3, 2, 3],
                "Frozen" or "Crystal" => [4, 2],
                "Dragon" or "Arabian" => [10, 3, 2, 3],
                "Galaxy" => [2, 3],
                _ => null
            };
            canvas.DrawRoundedRectangle(rect, radius);

            var inner = new RectF(rect.Left + 3, rect.Top + 3, rect.Width - 6, rect.Height - 6);
            canvas.StrokeDashPattern = null;
            canvas.StrokeSize = Math.Max(0.7f, (float)thickness * 0.45f);
            canvas.StrokeColor = Colors.White.WithAlpha(style is "Shadow" ? 0.08f : 0.24f);
            canvas.DrawRoundedRectangle(inner, Math.Max(3, radius - 3));

            if (style is "Dragon" or "Flame" or "Royal" or "Arabian" or "Galaxy")
            {
                canvas.StrokeSize = Math.Max(0.7f, (float)thickness * 0.38f);
                canvas.StrokeColor = primary.WithAlpha(0.38f);
                var midY = rect.Center.Y;
                canvas.DrawLine(rect.Left + 8, midY, rect.Left + 24, rect.Top + 5);
                canvas.DrawLine(rect.Right - 8, midY, rect.Right - 24, rect.Bottom - 5);
            }
        }
    }
}
