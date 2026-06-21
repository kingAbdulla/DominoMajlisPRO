using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerEffectEngine
{
    const string AnimationName = "DominoPlayerEffect";

    public static void Apply(
        Image overlay,
        CatalogAssetDisplay? effect,
        double baseScale = 1.18)
    {
        overlay.CancelAnimations();

        if (effect == null)
        {
            Clear(overlay);
            return;
        }

        var key =
            $"{effect.AssetId} {effect.DisplayName} {effect.ArabicDisplayName} {effect.EffectType} {effect.AnimationType}"
                .ToLowerInvariant();

        overlay.Source = string.IsNullOrWhiteSpace(effect.PreviewImage)
            ? "fire_gold.png"
            : effect.PreviewImage;

        overlay.InputTransparent = true;
        overlay.IsVisible = true;
        overlay.Scale = baseScale;
        overlay.Rotation = 0;

        var color = ResolveColor(key);
        overlay.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(color),
            Radius = ResolveRadius(key),
            Opacity = 0.75f
        };

        StartAnimation(overlay, key, baseScale);
    }

    static void StartAnimation(Image overlay, string key, double baseScale)
    {
        if (key.Contains("lightning") || key.Contains("برق"))
        {
            new Animation(v =>
            {
                overlay.Opacity = v < 0.5 ? 0.35 : 1;
                overlay.Scale = baseScale + (v < 0.5 ? 0.02 : 0.16);
                overlay.Rotation = -6 + (12 * v);
            }, 0, 1).Commit(overlay, AnimationName, 16, 420, Easing.Linear, null, () => overlay.IsVisible);
            return;
        }

        if (key.Contains("royal") || key.Contains("ملكي"))
        {
            new Animation(v =>
            {
                overlay.Opacity = 0.62 + (0.28 * v);
                overlay.Scale = baseScale + (0.06 * v);
                overlay.Rotation = 360 * v;
            }, 0, 1).Commit(overlay, AnimationName, 16, 2400, Easing.SinInOut, null, () => overlay.IsVisible);
            return;
        }

        if (key.Contains("diamond") || key.Contains("ماسي"))
        {
            new Animation(v =>
            {
                overlay.Opacity = 0.55 + (0.35 * v);
                overlay.Scale = baseScale + (0.1 * v);
                overlay.Rotation = 4 - (8 * v);
            }, 0, 1).Commit(overlay, AnimationName, 16, 760, Easing.SinInOut, null, () => overlay.IsVisible);
            return;
        }

        if (key.Contains("shadow") || key.Contains("ظل"))
        {
            new Animation(v =>
            {
                overlay.Opacity = 0.42 + (0.2 * v);
                overlay.Scale = baseScale + (0.04 * v);
                overlay.Rotation = 0;
            }, 0, 1).Commit(overlay, AnimationName, 16, 1300, Easing.SinInOut, null, () => overlay.IsVisible);
            return;
        }

        new Animation(v =>
        {
            overlay.Opacity = 0.68 + (0.25 * v);
            overlay.Scale = baseScale + (0.1 * v);
            overlay.Rotation = 3 - (6 * v);
        }, 0, 1).Commit(overlay, AnimationName, 16, 900, Easing.SinInOut, null, () => overlay.IsVisible);
    }

    static Color ResolveColor(string key)
    {
        if (key.Contains("lightning") || key.Contains("برق"))
            return Color.FromArgb("#8DEBFF");
        if (key.Contains("diamond") || key.Contains("ماسي"))
            return Color.FromArgb("#7DE3FF");
        if (key.Contains("shadow") || key.Contains("ظل"))
            return Color.FromArgb("#2B2B2B");
        if (key.Contains("royal") || key.Contains("ملكي"))
            return Color.FromArgb("#D4AF37");
        return Color.FromArgb("#FF8A00");
    }

    static float ResolveRadius(string key)
    {
        if (key.Contains("lightning") || key.Contains("برق"))
            return 34;
        if (key.Contains("royal") || key.Contains("ملكي"))
            return 30;
        return 24;
    }

    static void Clear(Image overlay)
    {
        overlay.CancelAnimations();
        overlay.Source = null;
        overlay.IsVisible = false;
        overlay.Opacity = 1;
        overlay.Scale = 1;
        overlay.Rotation = 0;
        overlay.Shadow = null;
    }
}