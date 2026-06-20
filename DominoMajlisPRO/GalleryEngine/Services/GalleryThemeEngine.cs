using Microsoft.Maui.Controls.Shapes;
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiPoint = Microsoft.Maui.Graphics.Point;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class GalleryThemeEngine
{
    private static GalleryTheme? _currentTheme;

    public static event EventHandler<GalleryTheme>? ThemeChanged;

    public static GalleryTheme Current =>
        _currentTheme ?? CreateFallbackTheme();

    public static async Task<GalleryTheme> BuildThemeFromSeasonImageAsync(string imageName)
    {
        var dominant = await ImageColorExtractor.ExtractDominantColorAsync(imageName);

        var theme = new GalleryTheme
        {
            Dominant = dominant,

            Background = CreatePageBackground(dominant),
            HeroBackground = await ImageColorExtractor.CreateSoftGradientAsync(imageName),
            CardBackground = CreateCardGradient(dominant),
            ActionBackground = CreateActionGradient(dominant),

            Accent = CreateAccent(dominant),
            AccentSoft = CreateAccent(dominant).WithAlpha(0.55f),
            Stroke = CreateStroke(dominant),
            Glow = CreateGlow(dominant),

            Gold = MauiColor.FromArgb("#FFD76A"),
            TextPrimary = MauiColor.FromArgb("#FFE8A3"),
            TextSecondary = MauiColor.FromArgb("#C8B58A"),
            TextMuted = MauiColor.FromArgb("#8F7A55")
        };

        _currentTheme = theme;
        NotifyThemeChanged();
        return theme;
    }

    public static void SetTheme(GalleryTheme theme)
    {
        _currentTheme = theme;
        NotifyThemeChanged();
    }

    public static void Reset()
    {
        _currentTheme = CreateFallbackTheme();
        NotifyThemeChanged();
    }

    private static void NotifyThemeChanged()
    {
        ThemeChanged?.Invoke(null, Current);
    }

    private static GalleryTheme CreateFallbackTheme()
    {
        var fallback = MauiColor.FromArgb("#B8873C");

        return new GalleryTheme
        {
            Dominant = fallback,
            Background = CreatePageBackground(fallback),
            HeroBackground = CreateCardGradient(fallback),
            CardBackground = CreateCardGradient(fallback),
            ActionBackground = CreateActionGradient(fallback),
            Accent = MauiColor.FromArgb("#D4AE62"),
            AccentSoft = MauiColor.FromArgb("#D4AE62").WithAlpha(0.55f),
            Stroke = MauiColor.FromArgb("#8A642E"),
            Glow = MauiColor.FromArgb("#B8873C").WithAlpha(0.28f),
            Gold = MauiColor.FromArgb("#FFD76A"),
            TextPrimary = MauiColor.FromArgb("#FFE8A3"),
            TextSecondary = MauiColor.FromArgb("#C8B58A"),
            TextMuted = MauiColor.FromArgb("#8F7A55")
        };
    }

    private static Brush CreatePageBackground(MauiColor color)
    {
        var dark = Darken(Desaturate(color, 0.75), 0.08);
        var middle = Darken(Desaturate(color, 0.65), 0.13);
        var accent = Darken(Desaturate(color, 0.50), 0.20);

        return new LinearGradientBrush
        {
            StartPoint = new MauiPoint(0, 0),
            EndPoint = new MauiPoint(1, 1),
            GradientStops =
            {
                new GradientStop(MauiColor.FromArgb("#030303"), 0f),
                new GradientStop(dark, 0.42f),
                new GradientStop(middle, 0.76f),
                new GradientStop(accent.WithAlpha(0.35f), 1f)
            }
        };
    }

    private static Brush CreateCardGradient(MauiColor color)
    {
        var dark = Darken(Desaturate(color, 0.62), 0.16);
        var middle = Darken(Desaturate(color, 0.45), 0.31);
        var accent = Darken(Desaturate(color, 0.24), 0.58);

        return new LinearGradientBrush
        {
            StartPoint = new MauiPoint(0, 0),
            EndPoint = new MauiPoint(1, 1),
            GradientStops =
            {
                new GradientStop(dark, 0f),
                new GradientStop(middle, 0.52f),
                new GradientStop(accent.WithAlpha(0.60f), 1f)
            }
        };
    }

    private static Brush CreateActionGradient(MauiColor color)
    {
        var dark = Darken(Desaturate(color, 0.70), 0.12);
        var accent = Darken(Desaturate(color, 0.38), 0.42);

        return new LinearGradientBrush
        {
            StartPoint = new MauiPoint(0, 0),
            EndPoint = new MauiPoint(1, 1),
            GradientStops =
            {
                new GradientStop(MauiColor.FromArgb("#080808"), 0f),
                new GradientStop(dark, 0.55f),
                new GradientStop(accent.WithAlpha(0.42f), 1f)
            }
        };
    }

    private static MauiColor CreateAccent(MauiColor color)
    {
        var accent = Desaturate(color, 0.18);
        return Lighten(accent, 1.18);
    }

    private static MauiColor CreateStroke(MauiColor color)
    {
        var stroke = Desaturate(color, 0.28);
        return Lighten(stroke, 0.95);
    }

    private static MauiColor CreateGlow(MauiColor color)
    {
        return Lighten(Desaturate(color, 0.20), 1.05).WithAlpha(0.28f);
    }

    private static MauiColor Desaturate(MauiColor color, double amount)
    {
        var gray = (color.Red + color.Green + color.Blue) / 3.0f;
        var factor = (float)amount;

        return new MauiColor(
            (float)(color.Red + (gray - color.Red) * factor),
            (float)(color.Green + (gray - color.Green) * factor),
            (float)(color.Blue + (gray - color.Blue) * factor),
            1f);
    }

    private static MauiColor Darken(MauiColor color, double factor)
    {
        var f = (float)factor;

        return new MauiColor(
            Math.Clamp(color.Red * f, 0f, 1f),
            Math.Clamp(color.Green * f, 0f, 1f),
            Math.Clamp(color.Blue * f, 0f, 1f),
            1f);
    }

    private static MauiColor Lighten(MauiColor color, double factor)
    {
        var f = (float)factor;

        return new MauiColor(
            Math.Clamp(color.Red * f, 0f, 1f),
            Math.Clamp(color.Green * f, 0f, 1f),
            Math.Clamp(color.Blue * f, 0f, 1f),
            1f);
    }
}

public sealed class GalleryTheme
{
    public MauiColor Dominant { get; set; }

    public Brush Background { get; set; } = new SolidColorBrush(MauiColor.FromArgb("#030303"));
    public Brush HeroBackground { get; set; } = new SolidColorBrush(MauiColor.FromArgb("#050505"));
    public Brush CardBackground { get; set; } = new SolidColorBrush(MauiColor.FromArgb("#050505"));
    public Brush ActionBackground { get; set; } = new SolidColorBrush(MauiColor.FromArgb("#080808"));

    public MauiColor Accent { get; set; }
    public MauiColor AccentSoft { get; set; }
    public MauiColor Stroke { get; set; }
    public MauiColor Glow { get; set; }

    public MauiColor Gold { get; set; }
    public MauiColor TextPrimary { get; set; }
    public MauiColor TextSecondary { get; set; }
    public MauiColor TextMuted { get; set; }
}

