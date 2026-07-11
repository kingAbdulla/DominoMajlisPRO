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
            return Merge(Effect.TypographyPreset, Frame.TypographyPreset);
        return (Frame ?? Effect)?.TypographyPreset;
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
}
