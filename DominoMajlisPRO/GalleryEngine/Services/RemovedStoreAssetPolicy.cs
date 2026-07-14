namespace DominoMajlisPRO.GalleryEngine.Services;

public static class RemovedStoreAssetPolicy
{
    public static bool IsRemoved(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = Path.GetFileNameWithoutExtension(value.Trim());
        const string obsoletePreviewPrefix = "preview_";
        if (!normalized.StartsWith(obsoletePreviewPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffix = normalized[obsoletePreviewPrefix.Length..];
        return string.Equals(suffix, "fram", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(suffix, "frame", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRemoved(string? assetId, string? imagePath) =>
        IsRemoved(assetId) || IsRemoved(imagePath);
}
