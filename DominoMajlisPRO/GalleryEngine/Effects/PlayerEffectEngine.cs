using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerEffectEngine
{
    const string AnimationName = "DominoPlayerEffect";
    const string DefaultLegacyEffectImage = "fire_gold.png";

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

        var definition = ResolveDefinition(effect, baseScale);
        var render = CreateRenderProfile(definition);

        overlay.InputTransparent = true;
        overlay.IsVisible = true;
        overlay.Rotation = 0;
        overlay.Opacity = render.Opacity;
        overlay.Scale = render.Scale;

        overlay.Source = render.UseLegacyImage
            ? ResolveLegacyImage(render)
            : null;

        overlay.BackgroundColor = ResolveBackgroundColor(definition, render);
        overlay.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(render.PrimaryColor),
            Radius = render.ShadowRadius,
            Opacity = render.ShadowOpacity
        };

        StartAnimation(overlay, definition, render);
    }

    static EffectDefinitionModel ResolveDefinition(
        CatalogAssetDisplay effect,
        double baseScale)
    {
        var key = BuildEffectKey(effect);
        var presetId = ResolvePresetId(key);
        var preset = EffectPresetCatalog.ResolvePreset(presetId);
        var primaryColor = ResolvePrimaryColorPresetId(key, presetId);
        var secondaryColor = ResolveSecondaryColorPresetId(key, presetId, primaryColor);

        return new EffectDefinitionModel(
            effect.AssetId,
            EffectOwnerScope.Player,
            preset.PresetId,
            preset.DefaultAnimationId,
            primaryColor,
            secondaryColor,
            preset.DefaultLayers,
            preset.DefaultOpacity,
            Math.Max(0.1, baseScale + (preset.DefaultScale - 1.18)),
            preset.DefaultSpeed,
            preset.DefaultIntensity,
            LegacyImagePath: ShouldUseLegacyImage(effect, key)
                ? ResolveLegacyImagePath(effect)
                : string.Empty);
    }

    static EffectRenderProfile CreateRenderProfile(EffectDefinitionModel definition)
    {
        var primary = EffectPresetCatalog.ResolveColor(
            definition.PrimaryColorPresetId,
            definition.CustomPrimaryColorHex);
        var secondary = EffectPresetCatalog.ResolveColor(
            definition.SecondaryColorPresetId,
            definition.CustomSecondaryColorHex);

        return new EffectRenderProfile(
            primary,
            secondary,
            Math.Clamp(definition.Opacity, 0.05, 1.0),
            Math.Clamp(definition.Scale, 0.1, 3.0),
            ResolveDuration(definition),
            ResolveRadius(definition),
            ResolveShadowOpacity(definition),
            !string.IsNullOrWhiteSpace(definition.LegacyImagePath),
            definition.LegacyImagePath);
    }

    static string BuildEffectKey(CatalogAssetDisplay effect) =>
        $"{effect.AssetId} {effect.DisplayName} {effect.ArabicDisplayName} {effect.EffectType} {effect.AnimationType}"
            .ToLowerInvariant();

    static EffectPresetId ResolvePresetId(string key)
    {
        if (key.Contains("lightning") || key.Contains("برق"))
            return EffectPresetId.Lightning;
        if (key.Contains("diamond") || key.Contains("ماسي"))
            return EffectPresetId.Diamond;
        if (key.Contains("shadow") || key.Contains("ظل"))
            return EffectPresetId.Shadow;
        if (key.Contains("royal") || key.Contains("ملكي"))
            return EffectPresetId.Royal;
        if (key.Contains("ring") || key.Contains("حلقة"))
            return EffectPresetId.Ring;
        if (key.Contains("aura") || key.Contains("هالة"))
            return EffectPresetId.Aura;
        if (key.Contains("pulse") || key.Contains("نبض"))
            return EffectPresetId.Pulse;
        if (key.Contains("ice") || key.Contains("جليد"))
            return EffectPresetId.Ice;
        if (key.Contains("fire") || key.Contains("نار"))
            return EffectPresetId.Fire;

        return EffectPresetId.Glow;
    }

    static EffectColorPresetId ResolvePrimaryColorPresetId(
        string key,
        EffectPresetId presetId)
    {
        if (key.Contains("blue") || key.Contains("أزرق"))
            return EffectColorPresetId.Sapphire;
        if (key.Contains("purple") || key.Contains("بنفسجي"))
            return EffectColorPresetId.Purple;
        if (key.Contains("red") || key.Contains("أحمر"))
            return EffectColorPresetId.Ruby;
        if (key.Contains("green") || key.Contains("أخضر"))
            return EffectColorPresetId.Emerald;
        if (key.Contains("white") || key.Contains("أبيض"))
            return EffectColorPresetId.Silver;
        if (key.Contains("silver") || key.Contains("فضي"))
            return EffectColorPresetId.Silver;

        return presetId switch
        {
            EffectPresetId.Lightning => EffectColorPresetId.Sapphire,
            EffectPresetId.Diamond => EffectColorPresetId.Ice,
            EffectPresetId.Shadow => EffectColorPresetId.Shadow,
            EffectPresetId.Ice => EffectColorPresetId.Ice,
            EffectPresetId.Fire => EffectColorPresetId.Fire,
            _ => EffectColorPresetId.Gold
        };
    }

    static EffectColorPresetId ResolveSecondaryColorPresetId(
        string key,
        EffectPresetId presetId,
        EffectColorPresetId primaryColorPresetId)
    {
        if (key.Contains("rainbow") || key.Contains("قوس"))
            return EffectColorPresetId.Rainbow;

        return presetId switch
        {
            EffectPresetId.Shadow => EffectColorPresetId.Shadow,
            EffectPresetId.Fire => EffectColorPresetId.Gold,
            EffectPresetId.Ice => EffectColorPresetId.Silver,
            EffectPresetId.Lightning => EffectColorPresetId.Silver,
            EffectPresetId.Diamond => EffectColorPresetId.Silver,
            _ => primaryColorPresetId
        };
    }

    static bool ShouldUseLegacyImage(CatalogAssetDisplay effect, string key)
    {
        if (key.Contains("legacy") || key.Contains("png"))
            return true;

        return !string.IsNullOrWhiteSpace(effect.PreviewImage) &&
               (key.Contains("sprite") || key.Contains("image"));
    }

    static string ResolveLegacyImagePath(CatalogAssetDisplay effect) =>
        string.IsNullOrWhiteSpace(effect.PreviewImage)
            ? DefaultLegacyEffectImage
            : effect.PreviewImage;

    static ImageSource ResolveLegacyImage(EffectRenderProfile render) =>
        string.IsNullOrWhiteSpace(render.LegacyImagePath)
            ? DefaultLegacyEffectImage
            : render.LegacyImagePath;

    static Color ResolveBackgroundColor(
        EffectDefinitionModel definition,
        EffectRenderProfile render) =>
        definition.PresetId switch
        {
            EffectPresetId.Ring => render.PrimaryColor.WithAlpha(0.11f),
            EffectPresetId.Aura => render.PrimaryColor.WithAlpha(0.16f),
            EffectPresetId.Pulse => render.PrimaryColor.WithAlpha(0.13f),
            EffectPresetId.Shadow => render.PrimaryColor.WithAlpha(0.20f),
            _ => Colors.Transparent
        };

    static float ResolveRadius(EffectDefinitionModel definition) =>
        definition.PresetId switch
        {
            EffectPresetId.Lightning => 36,
            EffectPresetId.Royal => 34,
            EffectPresetId.Diamond => 34,
            EffectPresetId.Aura => 38,
            EffectPresetId.Ring => 28,
            EffectPresetId.Shadow => 22,
            _ => 30
        };

    static float ResolveShadowOpacity(EffectDefinitionModel definition) =>
        definition.PresetId switch
        {
            EffectPresetId.Shadow => 0.85f,
            EffectPresetId.Ring => 0.70f,
            EffectPresetId.Aura => 0.78f,
            _ => 0.75f
        };

    static uint ResolveDuration(EffectDefinitionModel definition)
    {
        var baseDuration = definition.PresetId switch
        {
            EffectPresetId.Lightning => 420,
            EffectPresetId.Royal => 2400,
            EffectPresetId.Diamond => 760,
            EffectPresetId.Shadow => 1300,
            EffectPresetId.Aura => 1800,
            EffectPresetId.Ring => 1200,
            EffectPresetId.Pulse => 900,
            _ => 1000
        };

        var speed = Math.Clamp(definition.Speed, 0.25, 3.0);
        return (uint)Math.Clamp(baseDuration / speed, 180, 4000);
    }

    static void StartAnimation(
        Image overlay,
        EffectDefinitionModel definition,
        EffectRenderProfile render)
    {
        switch (definition.AnimationId)
        {
            case EffectAnimationId.Lightning:
                new Animation(v =>
                {
                    overlay.Opacity = v < 0.5 ? 0.34 : 1;
                    overlay.Scale = render.Scale + (v < 0.5 ? 0.02 : 0.16 * definition.Intensity);
                    overlay.Rotation = -6 + (12 * v);
                }, 0, 1).Commit(overlay, AnimationName, 16, render.Duration, Easing.Linear, null, () => overlay.IsVisible);
                return;

            case EffectAnimationId.Rotate:
            case EffectAnimationId.Orbit:
                new Animation(v =>
                {
                    overlay.Opacity = render.Opacity + (0.18 * v);
                    overlay.Scale = render.Scale + (0.05 * v * definition.Intensity);
                    overlay.Rotation = 360 * v;
                }, 0, 1).Commit(overlay, AnimationName, 16, render.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
                return;

            case EffectAnimationId.Pulse:
            case EffectAnimationId.Breathing:
                new Animation(v =>
                {
                    overlay.Opacity = render.Opacity + (0.22 * v);
                    overlay.Scale = render.Scale + (0.12 * v * definition.Intensity);
                    overlay.Rotation = 0;
                }, 0, 1).Commit(overlay, AnimationName, 16, render.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
                return;

            case EffectAnimationId.Fade:
                new Animation(v =>
                {
                    overlay.Opacity = render.Opacity + (0.12 * v);
                    overlay.Scale = render.Scale + (0.04 * v * definition.Intensity);
                    overlay.Rotation = 0;
                }, 0, 1).Commit(overlay, AnimationName, 16, render.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
                return;

            case EffectAnimationId.Flash:
                new Animation(v =>
                {
                    overlay.Opacity = v < 0.5 ? render.Opacity : 1;
                    overlay.Scale = render.Scale + (0.10 * v * definition.Intensity);
                    overlay.Rotation = 4 - (8 * v);
                }, 0, 1).Commit(overlay, AnimationName, 16, render.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
                return;

            case EffectAnimationId.None:
                overlay.Opacity = render.Opacity;
                overlay.Scale = render.Scale;
                overlay.Rotation = 0;
                return;

            default:
                new Animation(v =>
                {
                    overlay.Opacity = render.Opacity + (0.2 * v);
                    overlay.Scale = render.Scale + (0.08 * v * definition.Intensity);
                    overlay.Rotation = 3 - (6 * v);
                }, 0, 1).Commit(overlay, AnimationName, 16, render.Duration, Easing.SinInOut, null, () => overlay.IsVisible);
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
