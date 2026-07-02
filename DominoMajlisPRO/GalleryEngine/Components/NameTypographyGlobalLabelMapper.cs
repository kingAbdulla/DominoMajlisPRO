namespace DominoMajlisPRO.GalleryEngine.Components;

[Obsolete("Use RuntimePlayerNameSurfaceView or RuntimeTeamNameSurfaceView with PlayerId/TeamId binding. This mapper is intentionally disabled.")]
public static class NameTypographyGlobalLabelMapper
{
    public static void Initialize()
    {
        // Intentionally no-op. Runtime typography must bind by PlayerId/TeamId, not visible text.
    }
}
