using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerEffectEngine
{
    const string AnimationName = "DominoPlayerEffect";
    const string DefaultLegacyEffectImage = "fire_gold.png";

    enum EffectPreset
    {
        Glow,
        Aura,
        Ring,
        Pulse,
        Lightning,
        Fire,
        Ice,
        Shadow,
        Royal,
        Diamond
    }

    sealed record EffectDefinition(
        EffectPreset Preset,
        Color PrimaryColor,
        Color SecondaryColor,
        double Opacity,
        double ScaleBoost,
        uint Duration,
        bool UseLegacyImage);

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

        var definition = ResolveDefinition(effect);

        overlay.InputTransparent = true;
        overlay.IsVisible = true;
        overlay.Rotation = 0;
        overlay.Opacity = definition.Opacity;
        overlay.Scale = baseScale + definition.ScaleBoost;

        // v2 foundation: prefer procedural glow/aura/ring behavior.
        // Legacy PNG remains supported only as a compatibility fallback.
        overlay.Source = definition.UseLegacyImage
            ? ResolveLegacyImage(effect)
            : null;

        overlay.BackgroundColor = ResolveBackgroundColor(definition);
        overlay.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(definition.PrimaryColor),
            Radius = ResolveRadius(definition),
            Opacity = ResolveShadowOpacity(definition)
        };

        StartAnimation(overlay, definition, baseScale);
    }

    static EffectDefinition ResolveDefinition(CatalogAssetDisplay effect)
    {
        var key = BuildEffectKey(effect);
        var preset = ResolvePreset(key);
        var color = ResolveColor(key, preset);
        var secondary = ResolveSecondaryColor(preset, color);

        return new EffectDefinition(
            preset,
            color,
            secondary,
            ResolveBaseOpacity(preset),
            ResolveScaleBoost(preset),
            ResolveDuration(preset),
            ShouldUseLegacyImage(effect, key));
    }

    static string BuildEffectKey(CatalogAssetDisplay effect) =>
        $"{effect.AssetId} {effect.DisplayName} {effect.ArabicDisplayName} {effect.EffectType} {effect.AnimationType}"
            .ToLowerInvariant();

    static EffectPreset ResolvePreset(string key)
    {
        if (key.Contains("lightning") || key.Contains("برق"))
            return EffectPreset.Lightning;
        if (key.Contains("diamond") || key.Contains("ماسي"))
            return EffectPreset.Diamond;
        if (key.Contains("shadow") || key.Contains("ظل"))
            return EffectPreset.Shadow;
        if (key.Contains("royal") || key.Contains("ملكي"))
            return EffectPreset.Royal;
        if (key.Contains("ring") || key.Contains("حلقة"))
            return EffectPreset.Ring;
        if (key.Contains("aura") || key.Contains("هالة"))
            return EffectPreset.Aura;
        if (key.Contains("pulse") || key.Contains("نبض"))
            return EffectPreset.Pulse;
        if (key.Contains("ice") || key.Contains("جليد"))
            return EffectPreset.Ice;
        if (key.Contains("fire") || key.Contains("نار"))
            return EffectPreset.Fire;

        return EffectPreset.Glow;
    }

    static Color ResolveColor(string key, EffectPreset preset)
    {
        if (key.Contains("blue") || key.Contains("أزرق"))
            return Color.FromArgb("#4FC3F7");
        if (key.Contains("purple") || key.Contains("بنفسجي"))
            return Color.FromArgb("#B56CFF");
        if (key.Contains("red") || key.Contains("أحمر"))
            return Color.FromArgb("#FF5252");
        if (key.Contains("green") || key.Contains("أخضر"))
            return Color.FromArgb("#00C853");
        if (key.Contains("white") || key.Contains("أبيض"))
            return Color.FromArgb("#FFFFFF");
        if (key.Contains("silver") || key.Contains("فضي"))
            return Color.FromArgb("#D8D8D8");

        return preset switch
        {
            EffectPreset.Lightning => Color.FromArgb("#8DEBFF"),
            EffectPreset.Diamond => Color.FromArgb("#7DE3FF"),
            EffectPreset.Shadow => Color.FromArgb("#2B2B2B"),
            EffectPreset.Royal => Color.FromArgb("#D4AF37"),
            EffectPreset.Ice => Color.FromArgb("#A7F3FF"),
            EffectPreset.Fire => Color.FromArgb("#FF8A00"),
            _ => Color.FromArgb("#D4AF37")
        };
    }

    static Color ResolveSecondaryColor(EffectPreset preset, Color primary) =>
        preset switch
        {
            EffectPreset.Shadow => Color.FromArgb("#101010"),
            EffectPreset.Fire => Color.FromArgb("#FFCC66"),
            EffectPreset.Ice => Color.FromArgb("#E8FBFF"),
            EffectPreset.Lightning => Color.FromArgb("#FFFFFF"),
            EffectPreset.Diamond => Color.FromArgb("#FFFFFF"),
            _ => primary
        };

    static bool ShouldUseLegacyImage(CatalogAssetDisplay effect, string key)
    {
        if (key.Contains("legacy") || key.Contains("png"))
            return true;

        return !string.IsNullOrWhiteSpace(effect.PreviewImage) &&
               (key.Contains("sprite") || key.Contains("image"));
    }

    static ImageSource ResolveLegacyImage(CatalogAssetDisplay effect) =>
        string.IsNullOrWhiteSpace(effect.PreviewImage)
            ? DefaultLegacyEffectImage
            : effect.PreviewImage;

    static Color ResolveBackgroundColor(EffectDefinition definition) =>
        definition.Preset switch
        {
            EffectPreset.Ring => definition.PrimaryColor.WithAlpha(0.11f),
            EffectPreset.Aura => definition.PrimaryColor.WithAlpha(0.16f),
            EffectPreset.Pulse => definition.PrimaryColor.WithAlpha(0.13f),
            EffectPreset.Shadow => definition.PrimaryColor.WithAlpha(0.20f),
            _ => Colors.Transparent
        };

    static float ResolveRadius(EffectDefinition definition) =>
        definition.Preset switch
        {
            EffectPreset.Lightning => 36,
            EffectPreset.Royal => 34,
            EffectPreset.Diamond => 34,
            EffectPreset.Aura => 38,
            EffectPreset.Ring => 28,
            EffectPreset.Shadow => 22,
            _ => 30
        };

    static float ResolveShadowOpacity(EffectDefinition definition) =>
        definition.Preset switch
        {
            EffectPreset.Shadow => 0.85f,
            EffectPreset.Ring => 0.70f,
            EffectPreset.Aura => 0.78f,
            _ => 0.75f
        };

    static double ResolveBaseOpacity(EffectPreset preset) =>
        preset switch
        {
            EffectPreset.Shadow => 0.52,
            EffectPreset.Ring => 0.72,
            EffectPreset.Aura => 0.64,
            EffectPreset.Pulse => 0.66,
            _ => 0.74
        };

    static double ResolveScaleBoost(EffectPreset preset) =>
        preset switch
        {
            EffectPreset.Ring => 0.03,
            EffectPreset.Aura => 0.08,
            EffectPreset.Pulse => 0.06,
            EffectPreset.Lightning => 0.09,
            _ => 0.05
        };

    static uint ResolveDuration(EffectPreset preset) =>
        preset switch
        {
            EffectPreset.Lightning => 420,
            EffectPreset.Royal => 2400,
            EffectPreset.Diamond => 760,
            EffectPreset.Shadow => 1300,
            EffectPreset.Aura => 1800,
            EffectPreset.Ring => 1200,
            EffectPreset.Pulse => 900,
            _ => 1000
        };

    static void StartAnimation(
        Image overlay,
        EffectDefinition definition,
        double baseScale)
    {
        switch (definition.Preset)
        {
            case EffectPreset.Lightning:
                new Animation(v =>
                {
                    overlay.Opacity = v < 0.5 ? 0.34 : 1;
                    overlay.Scale = baseScale + definition.ScaleBoost + (v < 0.5 ? 0.02 : 0.16);
                    overlay.Rotation = -6 + (12 * v);
                }, 0, 1).Commit(overlay, AnimationName, 16, definition.Duration, Easing.Linear, null, () => overlay.IsVisible);
                return;

            case EffectPreset.Royal:
            case EffectPreset.Ring:
                new Animation(v =>
                {
                    overlay.Opacity = definition.Opacity + (0.18 * v);
                    overlay.Scale = baseScale + definition.ScaleBoost + (0.05 * v);
                    overlay.Rotation = 360 * v;
                }, 0, 1).Commit(overlay, AnimationName, 16, definition.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
                return;

            case EffectPreset.Aura:
            case EffectPreset.Pulse:
                new Animation(v =>
                {
                    overlay.Opacity = definition.Opacity + (0.22 * v);
                    overlay.Scale = baseScale + definition.ScaleBoost + (0.12 * v);
                    overlay.Rotation = 0;
                }, 0, 1).Commit(overlay, AnimationName, 16, definition.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
                return;

            case EffectPreset.Shadow:
                new Animation(v =>
                {
                    overlay.Opacity = definition.Opacity + (0.12 * v);
                    overlay.Scale = baseScale + definition.ScaleBoost + (0.04 * v);
                    overlay.Rotation = 0;
                }, 0, 1).Commit(overlay, AnimationName, 16, definition.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
                return;

            default:
                new Animation(v =>
                {
                    overlay.Opacity = definition.Opacity + (0.2 * v);
                    overlay.Scale = baseScale + definition.ScaleBoost + (0.08 * v);
                    overlay.Rotation = 3 - (6 * v);
                }, 0, 1).Commit(overlay, AnimationName, 16, definition.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
                return;
        }
    }

    static void Clear(Image overlay)
    {
        overlay.CancelAnimations();
        overlay.Source = null;
        overlay.BackgroundColor = Colors.Transparent;
        overlay.IsVisible = false;
        overlay.Opacity = 1;
        overlay.Scale = 1;
        overlay.Rotation = 0;
        overlay.Shadow = null;
    }
}
