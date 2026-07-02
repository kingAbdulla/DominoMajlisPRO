namespace DominoMajlisPRO.GalleryEngine.Components;

[Obsolete("Use RuntimePlayerNameSurfaceView or RuntimeTeamNameSurfaceView with PlayerId/TeamId binding. Visible text scanning is intentionally disabled.")]
public static class NameTypographyPageScanner
{
    public static Task ApplyAsync(Element root) => Task.CompletedTask;

    public static Task ApplyDelayedAsync(Element root, int delayMs = 120) => Task.CompletedTask;
}
