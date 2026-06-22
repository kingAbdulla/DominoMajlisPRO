using System.Runtime.CompilerServices;
using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerEffectEngine
{
    const string AnimationName = "DominoPlayerEffect";
    const string ProceduralAnimationName = "DominoPlayerProceduralEffect";
    const string DefaultLegacyEffectImage = "fire_gold.png";
    const string MainHeaderEffectOverlayStyleId = "MainHeaderAvatarEffectOverlay";

    static readonly ConditionalWeakTable<Image, ProceduralOverlayState> ProceduralStates = new();

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

        var definition = CreateDefinition(effect, baseScale);
        var render = CreateRenderProfile(definition);

        if (!render.UseLegacyImage && TryApplyProcedural(overlay, definition, render))
            return;

        HideProceduralOverlay(overlay);

        bool isMainHeaderOverlay =
            string.Equals(
                overlay.StyleId,
                MainHeaderEffectOverlayStyleId,
                StringComparison.Ordinal);

        overlay.InputTransparent = true;
        overlay.IsVisible = true;
        overlay.Rotation = 0;
        overlay.Opacity = render.Opacity;
        overlay.Scale = render.Scale;

        overlay.Source = render.UseLegacyImage
            ? ResolveLegacyImage(render)
            : null;

        overlay.BackgroundColor = isMainHeaderOverlay
            ? Colors.Transparent
            : CreateBackgroundColor(definition, render);

        overlay.Shadow = isMainHeaderOverlay
            ? null
            : new Shadow
            {
                Brush = new SolidColorBrush(render.PrimaryColor),
                Radius = render.ShadowRadius,
                Opacity = render.ShadowOpacity
            };

        StartAnimation(overlay, definition, render);
    }

    public static EffectDefinitionModel CreateDefinition(
        CatalogAssetDisplay effect,
        double baseScale = 1.18)
    {
        ArgumentNullException.ThrowIfNull(effect);

        var key = BuildEffectKey(effect);
        var presetId = ResolvePresetId(effect, key);
        var preset = EffectPresetCatalog.ResolvePreset(presetId);
        var animationId = ResolveAnimationId(effect.AnimationType, preset.DefaultAnimationId);
        var primaryColor = ResolveColorPresetId(
            effect.PrimaryColorPresetId,
            ResolvePrimaryColorPresetId(key, presetId));
        var secondaryColor = ResolveColorPresetId(
            effect.SecondaryColorPresetId,
            ResolveSecondaryColorPresetId(key, presetId, primaryColor));
        var layers = ResolveLayers(effect.EffectLayerIds, preset.DefaultLayers);
        var opacity = effect.EffectOpacity > 0 ? effect.EffectOpacity : preset.DefaultOpacity;
        var scale = effect.EffectScale > 0
            ? effect.EffectScale
            : Math.Max(0.1, baseScale + (preset.DefaultScale - 1.18));
        var speed = effect.EffectSpeed > 0 ? effect.EffectSpeed : preset.DefaultSpeed;
        var intensity = effect.EffectIntensity > 0 ? effect.EffectIntensity : preset.DefaultIntensity;

        return new EffectDefinitionModel(
            effect.AssetId,
            EffectOwnerScope.Player,
            preset.PresetId,
            animationId,
            primaryColor,
            secondaryColor,
            layers,
            opacity,
            scale,
            speed,
            intensity,
            effect.DurationMilliseconds,
            effect.CustomPrimaryColorHex,
            effect.CustomSecondaryColorHex,
            ShouldUseLegacyImage(effect, key)
                ? ResolveLegacyImagePath(effect)
                : string.Empty);
    }

    public static EffectRenderProfile CreateRenderProfile(EffectDefinitionModel definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

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

    public static Color CreateBackgroundColor(
        EffectDefinitionModel definition,
        EffectRenderProfile render)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(render);

        if (definition.Layers.Contains(EffectLayerId.Aura))
            return render.SecondaryColor.WithAlpha(0.16f);
        if (definition.Layers.Contains(EffectLayerId.Ring))
            return render.PrimaryColor.WithAlpha(0.11f);
        if (definition.Layers.Contains(EffectLayerId.Pulse))
            return render.PrimaryColor.WithAlpha(0.13f);
        if (definition.Layers.Contains(EffectLayerId.Shadow))
            return render.PrimaryColor.WithAlpha(0.20f);

        return definition.PresetId switch
        {
            EffectPresetId.Ring => render.PrimaryColor.WithAlpha(0.11f),
            EffectPresetId.Aura => render.PrimaryColor.WithAlpha(0.16f),
            EffectPresetId.Pulse => render.PrimaryColor.WithAlpha(0.13f),
            EffectPresetId.Shadow => render.PrimaryColor.WithAlpha(0.20f),
            _ => Colors.Transparent
        };
    }

    static bool TryApplyProcedural(
        Image overlay,
        EffectDefinitionModel definition,
        EffectRenderProfile render)
    {
        var state = EnsureProceduralOverlay(overlay);
        if (state?.View == null)
            return false;

        overlay.CancelAnimations();
        overlay.Source = null;
        overlay.BackgroundColor = Colors.Transparent;
        overlay.Shadow = null;
        overlay.IsVisible = false;
        overlay.Opacity = 1;
        overlay.Scale = 1;
        overlay.Rotation = 0;

        state.Drawable.Configure(definition, render);
        state.Drawable.AnimationProgress = 0;

        var view = state.View;
        view.CancelAnimations();
        view.InputTransparent = true;
        view.IsVisible = true;
        view.BackgroundColor = Colors.Transparent;
        view.Opacity = render.Opacity;
        view.Scale = render.Scale;
        view.Rotation = 0;
        view.Invalidate();

        StartProceduralAnimation(view, state.Drawable, definition, render);
        return true;
    }

    static ProceduralOverlayState? EnsureProceduralOverlay(Image overlay)
    {
        if (overlay.Parent is not Layout parent)
            return null;

        var state = ProceduralStates.GetValue(
            overlay,
            _ => new ProceduralOverlayState(new ProceduralEffectDrawable()));

        if (state.View?.Parent is Layout oldParent && !ReferenceEquals(oldParent, parent))
            oldParent.Children.Remove(state.View);

        if (state.View == null)
        {
            state.View = new GraphicsView
            {
                Drawable = state.Drawable,
                InputTransparent = true,
                IsVisible = false,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
        }

        if (overlay.WidthRequest > 0)
            state.View.WidthRequest = overlay.WidthRequest;
        if (overlay.HeightRequest > 0)
            state.View.HeightRequest = overlay.HeightRequest;

        if (state.View.Parent == null)
        {
            parent.Children.Add(state.View);
            state.View.ZIndex = overlay.ZIndex + 1;
        }

        return state;
    }

    static double ResolveEffectHostSize(Image overlay)
    {
        var width = overlay.Width > 1 ? overlay.Width : overlay.WidthRequest;
        var height = overlay.Height > 1 ? overlay.Height : overlay.HeightRequest;
        var iconSize = Math.Max(24, Math.Min(
            width > 1 ? width : 72,
            height > 1 ? height : 72));

        return Math.Clamp(iconSize * 1.35, 42, 220);
    }

    static void HideProceduralOverlay(Image overlay)
    {
        if (!ProceduralStates.TryGetValue(overlay, out var state) || state.View == null)
            return;

        state.View.CancelAnimations();
        state.Drawable.Configure(null, null);
        state.Drawable.AnimationProgress = 0;
        state.View.BackgroundColor = Colors.Transparent;
        state.View.IsVisible = false;
        state.View.Opacity = 1;
        state.View.Scale = 1;
        state.View.Rotation = 0;
        state.View.Invalidate();
    }

    static void StartProceduralAnimation(
        GraphicsView view,
        ProceduralEffectDrawable drawable,
        EffectDefinitionModel definition,
        EffectRenderProfile render)
    {
        if (definition.AnimationId == EffectAnimationId.None)
            return;

        new Animation(v =>
        {
            drawable.AnimationProgress = v;
            view.Opacity = definition.AnimationId == EffectAnimationId.Flash && v >= 0.5
                ? 1
                : render.Opacity;
            view.Rotation = definition.AnimationId is EffectAnimationId.Rotate or EffectAnimationId.Orbit
                ? 360 * v
                : definition.AnimationId == EffectAnimationId.Lightning
                    ? -6 + (12 * v)
                    : 0;
            view.Scale = render.Scale + ResolveProceduralScaleBoost(definition, v);
            view.Invalidate();
        }, 0, 1).Commit(
            view,
            ProceduralAnimationName,
            16,
            render.Duration,
            Easing.SinInOut,
            null,
            () => view.IsVisible);
    }

    static double ResolveProceduralScaleBoost(
        EffectDefinitionModel definition,
        double progress)
    {
        return definition.AnimationId switch
        {
            EffectAnimationId.Lightning => progress < 0.5
                ? 0.02
                : 0.16 * definition.Intensity,
            EffectAnimationId.Pulse or EffectAnimationId.Breathing => 0.12 * progress * definition.Intensity,
            EffectAnimationId.Fade => 0.04 * progress * definition.Intensity,
            EffectAnimationId.Flash => 0.10 * progress * definition.Intensity,
            EffectAnimationId.Rotate or EffectAnimationId.Orbit => 0.05 * progress * definition.Intensity,
            _ => 0.08 * progress * definition.Intensity
        };
    }

    static string BuildEffectKey(CatalogAssetDisplay effect) =>
        $"{effect.AssetId} {effect.DisplayName} {effect.ArabicDisplayName} {effect.EffectType} {effect.AnimationType}"
            .ToLowerInvariant();

    static EffectPresetId ResolvePresetId(CatalogAssetDisplay effect, string key)
    {
        if (Enum.TryParse<EffectPresetId>(effect.EffectType, true, out var explicitPreset))
            return explicitPreset;

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

    static EffectAnimationId ResolveAnimationId(
        string? value,
        EffectAnimationId fallback) =>
        Enum.TryParse<EffectAnimationId>(value, true, out var parsed)
            ? parsed
            : fallback;

    static EffectColorPresetId ResolveColorPresetId(
        string? value,
        EffectColorPresetId fallback) =>
        Enum.TryParse<EffectColorPresetId>(value, true, out var parsed)
            ? parsed
            : fallback;

    static IReadOnlyList<EffectLayerId> ResolveLayers(
        IReadOnlyList<string>? layerIds,
        IReadOnlyList<EffectLayerId> fallback)
    {
        var parsed = layerIds?
            .Select(item => Enum.TryParse<EffectLayerId>(item, true, out var layer)
                ? layer
                : (EffectLayerId?)null)
            .Where(item => item != null)
            .Select(item => item!.Value)
            .Distinct()
            .ToList()
            ?? new List<EffectLayerId>();

        return parsed.Count == 0 ? fallback : parsed;
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

    static float ResolveRadius(EffectDefinitionModel definition)
    {
        var layerBoost = definition.Layers.Contains(EffectLayerId.Glow) ? 6 : 0;
        return definition.PresetId switch
        {
            EffectPresetId.Lightning => 36 + layerBoost,
            EffectPresetId.Royal => 34 + layerBoost,
            EffectPresetId.Diamond => 34 + layerBoost,
            EffectPresetId.Aura => 38 + layerBoost,
            EffectPresetId.Ring => 28 + layerBoost,
            EffectPresetId.Shadow => 22,
            _ => 30 + layerBoost
        };
    }

    static float ResolveShadowOpacity(EffectDefinitionModel definition)
    {
        var opacityBoost = definition.Layers.Contains(EffectLayerId.Glow) ? 0.08f : 0f;
        var value = definition.PresetId switch
        {
            EffectPresetId.Shadow => 0.85f,
            EffectPresetId.Ring => 0.70f,
            EffectPresetId.Aura => 0.78f,
            _ => 0.75f
        };

        return Math.Clamp(value + opacityBoost, 0.05f, 1f);
    }

    static uint ResolveDuration(EffectDefinitionModel definition)
    {
        var baseDuration = definition.DurationMilliseconds > 0
            ? definition.DurationMilliseconds
            : definition.PresetId switch
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
        HideProceduralOverlay(overlay);
        overlay.Source = null;
        overlay.BackgroundColor = Colors.Transparent;
        overlay.IsVisible = false;
        overlay.Opacity = 1;
        overlay.Scale = 1;
        overlay.Rotation = 0;
        overlay.Shadow = null;
    }

    sealed class ProceduralOverlayState
    {
        public ProceduralOverlayState(ProceduralEffectDrawable drawable)
        {
            Drawable = drawable;
        }

        public ProceduralEffectDrawable Drawable { get; }
        public GraphicsView? View { get; set; }
    }
}
