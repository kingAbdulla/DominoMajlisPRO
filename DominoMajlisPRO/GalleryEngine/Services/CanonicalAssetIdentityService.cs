using System.Text.RegularExpressions;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class CanonicalAssetIdentityService
{
    public static string GenerateCanonicalAssetId(string storeTypeId, string title)
    {
        var prefix = GetCanonicalPrefix(storeTypeId);
        
        var slug = Regex.Replace(title?.Trim() ?? "", @"[^a-zA-Z0-9\s]", "");
        slug = Regex.Replace(slug, @"\s+", "_").ToUpper();
        slug = Regex.Replace(slug, @"_+", "_").Trim('_');

        if (string.IsNullOrWhiteSpace(slug))
            slug = "UNNAMED_ASSET";

        return $"{prefix}_{slug}";
    }

    public static string NormalizeForComparison(string? assetId)
    {
        return assetId?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static bool SameAssetId(string? left, string? right)
    {
        return string.Equals(NormalizeForComparison(left), NormalizeForComparison(right), StringComparison.Ordinal);
    }

    public static bool IsCanonicalAssetId(string? assetId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return false;
        
        // Pattern: PREFIX_SLUG
        // Basic check: at least one underscore, not empty slug
        return assetId.Contains('_') && !assetId.EndsWith('_');
    }

    public static void ValidateCanonicalAssetId(string? assetId)
    {
        if (!IsCanonicalAssetId(assetId))
        {
            throw new InvalidOperationException($"المعرف {assetId} ليس معرفاً أصلياً (Canonical) صالحاً.");
        }
    }

    public static string GenerateUniqueCanonicalAssetId(
        string storeTypeId,
        string title,
        IEnumerable<string> existingAssetIds)
    {
        var normalizedExisting = new HashSet<string>(
            existingAssetIds.Select(NormalizeForComparison),
            StringComparer.Ordinal);

        var baseId = GenerateCanonicalAssetId(storeTypeId, title);

        if (!normalizedExisting.Contains(NormalizeForComparison(baseId)))
            return baseId;

        var counter = 2;
        while (true)
        {
            var candidate = $"{baseId}_{counter}";
            if (!normalizedExisting.Contains(NormalizeForComparison(candidate)))
                return candidate;
            counter++;
        }
    }

    private static string GetCanonicalPrefix(string storeTypeId)
    {
        return storeTypeId?.Trim().ToLower() switch
        {
            "avatar" => "AVATAR",
            "profilebackground" => "PROFILE_BACKGROUND",
            "frame" => "FRAME",
            "effect" => "EFFECT",
            "emblem" => "EMBLEM",
            "emblembackground" => "EMBLEM_BACKGROUND",
            "teamcolor" => "TEAM_COLOR",
            "teameffect" => "TEAM_EFFECT",
            _ => "ASSET"
        };
    }
}
