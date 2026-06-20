using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Helpers;

public static class GalleryThemeBrushFactory
{
    public static Brush CreateCardBackground(GalleryTheme? theme)
    {
        var start = Parse(theme?.CardBackgroundStart, "#17110A");
        var mid = Parse(theme?.SecondaryColor, "#7A3E12");
        var end = Parse(theme?.CardBackgroundEnd, "#050505");

        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(start, 0.00f),
                new GradientStop(mid.WithAlpha(0.42f), 0.46f),
                new GradientStop(end, 1.00f)
            }
        };
    }

    public static Brush CreateImageAtmosphere(GalleryTheme? theme)
    {
        var primary = Parse(theme?.PrimaryColor, "#D8A63A");
        var accent = Parse(theme?.AccentColor, "#FFB84A");
        var secondary = Parse(theme?.SecondaryColor, "#7A3E12");

        return new RadialGradientBrush
        {
            Center = new Point(0.50, 0.42),
            Radius = 0.86,
            GradientStops =
            {
                new GradientStop(accent.WithAlpha(0.42f), 0.00f),
                new GradientStop(primary.WithAlpha(0.25f), 0.34f),
                new GradientStop(secondary.WithAlpha(0.16f), 0.62f),
                new GradientStop(Color.FromArgb("#00000000"), 1.00f)
            }
        };
    }

    public static Brush CreateSoftOverlay(GalleryTheme? theme)
    {
        var accent = Parse(theme?.AccentColor, "#FFB84A");
        var secondary = Parse(theme?.SecondaryColor, "#7A3E12");

        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(accent.WithAlpha(0.18f), 0.00f),
                new GradientStop(Color.FromArgb("#00000000"), 0.45f),
                new GradientStop(secondary.WithAlpha(0.22f), 1.00f)
            }
        };
    }

    public static Color GetBorderColor(GalleryTheme? theme)
    {
        return Parse(theme?.BorderColor, "#D4AE62");
    }

    public static Color GetGlowColor(GalleryTheme? theme)
    {
        return Parse(theme?.GlowColor, "#FFB84A");
    }

    private static Color Parse(string? value, string fallback)
    {
        try
        {
            return Color.FromArgb(string.IsNullOrWhiteSpace(value) ? fallback : value);
        }
        catch
        {
            return Color.FromArgb(fallback);
        }
    }
}