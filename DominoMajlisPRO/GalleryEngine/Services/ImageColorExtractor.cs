using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiPoint = Microsoft.Maui.Graphics.Point;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class ImageColorExtractor
{
    private static readonly Dictionary<string, Brush> Cache = new();

    public static async Task<Brush> CreateSoftGradientAsync(string imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName))
            return CreateSoftGradient(MauiColor.FromArgb("#B8873C"));

        if (Cache.TryGetValue(imageName, out var cached))
            return cached;

        var color = await ExtractDominantColorAsync(imageName);
        var brush = CreateSoftGradient(color);

        Cache[imageName] = brush;
        return brush;
    }

    public static async Task<MauiColor> ExtractDominantColorAsync(string imageName)
    {
        await Task.Yield();

#if ANDROID
        try
        {
            Android.Graphics.Bitmap? bitmap = null;

            if (global::System.IO.File.Exists(imageName))
            {
                bitmap = Android.Graphics.BitmapFactory.DecodeFile(imageName);
            }
            else
            {
                var context = Android.App.Application.Context;
                var resourceName = global::System.IO.Path
                    .GetFileNameWithoutExtension(imageName)
                    .ToLowerInvariant();

                var resourceId =
                    context.Resources?.GetIdentifier(resourceName, "drawable", context.PackageName) ?? 0;

                if (resourceId == 0)
                    resourceId = context.Resources?.GetIdentifier(resourceName, "mipmap", context.PackageName) ?? 0;

                if (resourceId != 0)
                    bitmap = Android.Graphics.BitmapFactory.DecodeResource(context.Resources, resourceId);
            }

            if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
                return MauiColor.FromArgb("#B8873C");

            using (bitmap)
            {
                double rTotal = 0;
                double gTotal = 0;
                double bTotal = 0;
                double weightTotal = 0;

                var stepX = global::System.Math.Max(1, bitmap.Width / 44);
                var stepY = global::System.Math.Max(1, bitmap.Height / 44);

                for (var y = 0; y < bitmap.Height; y += stepY)
                {
                    for (var x = 0; x < bitmap.Width; x += stepX)
                    {
                        var pixel = bitmap.GetPixel(x, y);

                        var alpha = (pixel >> 24) & 0xFF;
                        if (alpha < 80)
                            continue;

                        var r = (pixel >> 16) & 0xFF;
                        var g = (pixel >> 8) & 0xFF;
                        var b = pixel & 0xFF;

                        var max = global::System.Math.Max(r, global::System.Math.Max(g, b));
                        var min = global::System.Math.Min(r, global::System.Math.Min(g, b));

                        var brightness = (r + g + b) / 3.0;
                        var saturation = max == 0 ? 0 : (max - min) / (double)max;

                        if (brightness < 28 || brightness > 238)
                            continue;

                        var weight = 0.55 + saturation;

                        rTotal += r * weight;
                        gTotal += g * weight;
                        bTotal += b * weight;
                        weightTotal += weight;
                    }
                }

                if (weightTotal <= 0)
                    return MauiColor.FromArgb("#B8873C");

                var finalR = ClampByte(rTotal / weightTotal);
                var finalG = ClampByte(gTotal / weightTotal);
                var finalB = ClampByte(bTotal / weightTotal);

                return SoftenColor(MauiColor.FromRgb(finalR, finalG, finalB));
            }
        }
        catch
        {
            return MauiColor.FromArgb("#B8873C");
        }
#else
        return MauiColor.FromArgb("#B8873C");
#endif
    }

    private static Brush CreateSoftGradient(MauiColor dominant)
    {
        var isRedDominant =
            dominant.Red > dominant.Green * 1.35f &&
            dominant.Red > dominant.Blue * 1.35f;

        MauiColor dark;
        MauiColor middle;
        MauiColor accent;

        if (isRedDominant)
        {
            dark = Darken(Desaturate(dominant, 0.50), 0.22);
            middle = Darken(Desaturate(dominant, 0.38), 0.42);
            accent = Darken(Desaturate(dominant, 0.25), 0.68);
        }
        else
        {
            dark = Darken(Desaturate(dominant, 0.62), 0.16);
            middle = Darken(Desaturate(dominant, 0.45), 0.31);
            accent = Darken(Desaturate(dominant, 0.24), 0.58);
        }

        return new LinearGradientBrush
        {
            StartPoint = new MauiPoint(0, 0),
            EndPoint = new MauiPoint(1, 1),
            GradientStops =
        {
            new GradientStop(dark, 0f),
            new GradientStop(middle, 0.50f),
            new GradientStop(accent.WithAlpha(isRedDominant ? 0.68f : 0.60f), 1f)
        }
        };
    }

    private static MauiColor SoftenColor(MauiColor color)
    {
        var isRedDominant =
            color.Red > color.Green * 1.35f &&
            color.Red > color.Blue * 1.35f;

        if (isRedDominant)
            return Darken(Desaturate(color, 0.28), 0.92);

        return Darken(Desaturate(color, 0.36), 0.88);
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
            global::System.Math.Clamp(color.Red * f, 0f, 1f),
            global::System.Math.Clamp(color.Green * f, 0f, 1f),
            global::System.Math.Clamp(color.Blue * f, 0f, 1f),
            1f);
    }

    private static byte ClampByte(double value)
    {
        return (byte)global::System.Math.Clamp(
            (int)global::System.Math.Round(value),
            0,
            255);
    }
}