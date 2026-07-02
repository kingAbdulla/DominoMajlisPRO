using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerEffectEngine
{
    public static void Apply(
        Image overlay,
        CatalogAssetDisplay? effect,
        double baseScale = 1.18) =>
        IdentityEffectRenderer.Apply(overlay, effect, baseScale);

    public static void Stop(Image overlay) =>
        IdentityEffectRenderer.Clear(overlay);

    public static EffectDefinitionModel CreateDefinition(CatalogAssetDisplay effect, double baseScale = 1.18)
    {
        var key = $"{effect.AssetId} {effect.DisplayName} {effect.EffectType}".ToLowerInvariant();
        var preset = key.Contains("ice") ? EffectPresetId.Ice :
            key.Contains("lightning") || key.Contains("bolt") ? EffectPresetId.Lightning :
            key.Contains("fire") || key.Contains("flame") ? EffectPresetId.Fire :
            key.Contains("shadow") ? EffectPresetId.Shadow :
            key.Contains("diamond") ? EffectPresetId.Diamond :
            key.Contains("royal") || key.Contains("gold") ? EffectPresetId.Royal :
            EffectPresetId.Glow;
        var animation = Enum.TryParse<EffectAnimationId>(effect.AnimationType?.Trim(), true, out var parsedAnimation)
            ? parsedAnimation
            : EffectAnimationId.Breathing;

        return new EffectDefinitionModel(
            effect.AssetId,
            EffectOwnerScope.Player,
            preset,
            animation,
            EffectColorPresetId.Custom,
            EffectColorPresetId.Custom,
            new[] { EffectLayerId.Glow, EffectLayerId.Aura },
            Math.Clamp(effect.EffectOpacity <= 0 ? 1.0 : effect.EffectOpacity, .05, 1.0),
            Math.Clamp((effect.EffectScale <= 0 ? 1.0 : effect.EffectScale) * baseScale, .5, 2.4),
            Math.Clamp(effect.EffectSpeed <= 0 ? 1.0 : effect.EffectSpeed, .1, 4.0),
            Math.Clamp(effect.EffectIntensity <= 0 ? 1.0 : effect.EffectIntensity, .1, 3.0),
            effect.DurationMilliseconds,
            effect.CustomPrimaryColorHex,
            effect.CustomSecondaryColorHex,
            effect.PreviewImage);
    }

    public static EffectRenderProfile CreateRenderProfile(EffectDefinitionModel definition)
    {
        var primary = Parse(definition.CustomPrimaryColorHex, definition.PresetId switch
        {
            EffectPresetId.Ice => "#79DFFF",
            EffectPresetId.Lightning => "#E8FBFF",
            EffectPresetId.Fire => "#FF6B18",
            EffectPresetId.Shadow => "#3F355C",
            EffectPresetId.Diamond => "#B7F2FF",
            EffectPresetId.Royal => "#FFD45A",
            _ => "#FFD45A"
        });
        var secondary = Parse(definition.CustomSecondaryColorHex, definition.PresetId switch
        {
            EffectPresetId.Ice => "#FFFFFF",
            EffectPresetId.Lightning => "#69CFFF",
            EffectPresetId.Fire => "#FFD04A",
            EffectPresetId.Shadow => "#161222",
            EffectPresetId.Diamond => "#FFFFFF",
            EffectPresetId.Royal => "#FFF1A8",
            _ => "#FFF1A8"
        });

        return new EffectRenderProfile(
            primary,
            secondary,
            definition.Opacity,
            definition.Scale,
            (uint)Math.Max(240, definition.DurationMilliseconds <= 0 ? 1200 : definition.DurationMilliseconds),
            16f * (float)definition.Intensity,
            .35f,
            !string.IsNullOrWhiteSpace(definition.LegacyImagePath),
            definition.LegacyImagePath);
    }

    private static Color Parse(string? value, string fallbackHex)
    {
        var token = string.IsNullOrWhiteSpace(value) ? fallbackHex : value.Trim();
        if (token.Length is 7 or 9 && token[0] == '#' && token[1..].All(Uri.IsHexDigit))
            return Color.FromArgb(token);

        return Colors.Gold;
    }
}
