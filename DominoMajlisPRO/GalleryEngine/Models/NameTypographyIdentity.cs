using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed record NameTypographyIdentity(
    string OwnerId,
    CatalogAssetDisplay? Effect,
    CatalogAssetDisplay? Frame)
{
    public bool HasVisual => Effect != null || Frame != null;

    public TypographyIdentityPreset? ResolvePreset()
    {
        if (Frame != null && Effect != null)
            return Merge(EnsureActiveNameEffect(Effect), Frame.TypographyPreset);
        return Frame?.TypographyPreset ?? (Effect == null ? null : EnsureActiveNameEffect(Effect));
    }

    private static TypographyIdentityPreset Merge(
        TypographyIdentityPreset effect,
        TypographyIdentityPreset frame)
    {
        var merged = frame.Normalized();
        var normalizedEffect = effect.Normalized();
        merged.FontFamily = normalizedEffect.FontFamily;
        merged.FontSize = normalizedEffect.FontSize;
        merged.MaterialPreset = normalizedEffect.MaterialPreset;
        merged.LightingPreset = normalizedEffect.LightingPreset;
        merged.DepthPreset = normalizedEffect.DepthPreset;
        merged.MotionPreset = normalizedEffect.MotionPreset;
        merged.ParticlePreset = normalizedEffect.ParticlePreset;
        merged.PrimaryColor = normalizedEffect.PrimaryColor;
        merged.Opacity = Math.Min(merged.Opacity, normalizedEffect.Opacity);
        merged.Scale = normalizedEffect.Scale;
        merged.Speed = normalizedEffect.Speed;
        merged.Intensity = Math.Max(merged.Intensity, normalizedEffect.Intensity);
        return merged.Normalized();
    }

    private static TypographyIdentityPreset EnsureActiveNameEffect(CatalogAssetDisplay effect)
    {
        var preset = effect.TypographyPreset.Normalized();
        if (effect.AssetType is not (StoreProductAssetType.PlayerNameEffect or StoreProductAssetType.TeamNameEffect))
            return preset;

        var hasMotion = !IsNone(preset.MotionPreset) ||
                        !IsNone(preset.ParticlePreset) ||
                        !IsNone(preset.DistortionPreset) ||
                        preset.LightingPreset is "MovingHighlight" or "MetallicSweep" or "LightningSweep" or "Aurora";
        if (hasMotion)
            return preset;

        preset.LightingPreset = "MovingHighlight";
        preset.MotionPreset = "Breath";
        preset.ParticlePreset = "TinySparks";
        preset.Intensity = Math.Max(preset.Intensity, 0.85);
        preset.Speed = Math.Max(preset.Speed, 1.05);
        return preset.Normalized();
    }

    private static bool IsNone(string? value) =>
        string.Equals(value?.Trim(), "None", StringComparison.OrdinalIgnoreCase);
}
