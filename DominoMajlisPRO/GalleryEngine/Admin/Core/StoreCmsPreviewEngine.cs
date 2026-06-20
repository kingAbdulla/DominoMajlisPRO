using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public sealed record StoreCmsPreviewData(string Title, string Subtitle, string Description, string ImagePath, string Meta);
public static class StoreCmsPreviewEngine
{
    public static StoreCmsPreviewData Build(string? title, string? subtitle, string? description, string? imagePath, string? meta = null) => new(title?.Trim() ?? string.Empty, subtitle?.Trim() ?? string.Empty, description?.Trim() ?? string.Empty, imagePath?.Trim() ?? string.Empty, meta?.Trim() ?? string.Empty);
    public static ImageSource? ResolveImageSource(string? imagePath) =>
        InventoryDisplayResolver.ResolveOptionalImageSource(imagePath);
}
