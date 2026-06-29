using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerEffectEngine
{
    const string DefaultLegacyEffectImage = "fire_gold.png";

    public static void Apply(
        Image overlay,
        CatalogAssetDisplay? effect,
        double baseScale = AvatarEffectRenderContract.PremiumAvatarRuntimeBaseScale) =>
        IdentityEffectRenderer.Apply(overlay, effect, baseScale);

    public static void Stop(Image overlay) =>
        IdentityEffectRenderer.Clear(overlay);

    public static EffectDefinitionModel CreateDefinition(
        CatalogAssetDisplay effect,
        double baseScale = AvatarEffectRenderContract.PremiumDefaultBaseScale)
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
            : ResolvePremiumDefaultScale(baseScale, preset);
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
            Math.Clamp(definition.Opacity, 0.12, 0.92),
            Math.Clamp(definition.Scale, 0.72, 3.0),
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

    static double ResolvePremiumDefaultScale(double baseScale, EffectPresetCatalogItem effectPreset)
    {
        var premiumScale = Math.Max(1.22, baseScale + (effectPreset.DefaultScale - 1.18));
        return Math.Clamp(premiumScale, 0.72, 2.15);
    }

    static float ResolveRadius(EffectDefinitionModel definition)
    {
        var layerBoost = definition.Layers.Contains(EffectLayerId.Glow) ? 8 : 0;
        return definition.PresetId switch
        {
            EffectPresetId.Lightning => 42 + layerBoost,
            EffectPresetId.Royal => 40 + layerBoost,
            EffectPresetId.Diamond => 42 + layerBoost,
            EffectPresetId.Aura => 44 + layerBoost,
            EffectPresetId.Ring => 34 + layerBoost,
            EffectPresetId.Shadow => 28,
            _ => 36 + layerBoost
        };
    }

    static float ResolveShadowOpacity(EffectDefinitionModel definition)
    {
        var opacityBoost = definition.Layers.Contains(EffectLayerId.Glow) ? 0.10f : 0f;
        var value = definition.PresetId switch
        {
            EffectPresetId.Shadow => 0.85f,
            EffectPresetId.Ring => 0.76f,
            EffectPresetId.Aura => 0.82f,
            _ => 0.78f
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
}
