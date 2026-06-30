using DominoMajlisPRO.GalleryEngine.Models;
using Microsoft.Maui.Controls;

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
}
