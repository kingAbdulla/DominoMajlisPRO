namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public enum StoreProductAssetType
{
    Avatar,
    ProfileBackground,
    Frame,
    Effect,
    Title,
    Emblem,
    TeamColor,
    EmblemBackground,
    Badge,
    SeasonReward,
    Bundle,
    CurrencyPack,
    NonInventoryOffer
}

public enum StoreProductOwnerScope
{
    Player,
    Team,
    None
}

public static class StoreProductAssetTypeCatalog
{
    public static IReadOnlyList<string> CanonicalTypeIds { get; } =
        Enum.GetNames<StoreProductAssetType>();

    public static bool TryResolve(string? value, out StoreProductAssetType type) =>
        Enum.TryParse(value?.Trim(), ignoreCase: false, out type) &&
        string.Equals(value?.Trim(), type.ToString(), StringComparison.Ordinal);

    public static StoreProductOwnerScope GetOwnerScope(StoreProductAssetType type) => type switch
    {
        StoreProductAssetType.Avatar or
        StoreProductAssetType.ProfileBackground or
        StoreProductAssetType.Frame or
        StoreProductAssetType.Effect or
        StoreProductAssetType.Title or
        StoreProductAssetType.Badge or
        StoreProductAssetType.SeasonReward => StoreProductOwnerScope.Player,

        StoreProductAssetType.Emblem or
        StoreProductAssetType.TeamColor or
        StoreProductAssetType.EmblemBackground => StoreProductOwnerScope.Team,

        _ => StoreProductOwnerScope.None
    };

    public static bool IsInventory(StoreProductAssetType type) =>
        GetOwnerScope(type) != StoreProductOwnerScope.None;

    public static bool RequiresImagePayload(StoreProductAssetType type) => type is
        StoreProductAssetType.Avatar or
        StoreProductAssetType.ProfileBackground or
        StoreProductAssetType.Emblem or
        StoreProductAssetType.EmblemBackground or
        StoreProductAssetType.Frame or
        StoreProductAssetType.Effect;

    public static bool Validate(
        string? storeTypeId,
        string? assetId,
        string? ownerScope,
        string? imagePath,
        string? colorHex,
        out string message)
    {
        if (!TryResolve(storeTypeId, out var type))
        {
            message = "نوع الأصل مطلوب ويجب اختياره من القائمة المعتمدة";
            return false;
        }

        var expectedOwner = GetOwnerScope(type);
        if (!Enum.TryParse<StoreProductOwnerScope>(ownerScope?.Trim(), false, out var actualOwner) ||
            actualOwner != expectedOwner)
        {
            message = "نطاق المالك غير صالح لنوع الأصل المحدد";
            return false;
        }

        if (IsInventory(type) && string.IsNullOrWhiteSpace(assetId))
        {
            message = "معرّف الأصل AssetId مطلوب للأصول القابلة للاقتناء";
            return false;
        }

        if (type == StoreProductAssetType.TeamColor && !IsValidColorHex(colorHex))
        {
            message = "لون الفريق يتطلب قيمة ColorHex صحيحة مثل #FFD700";
            return false;
        }

        if (RequiresImagePayload(type) && !IsValidImagePayload(imagePath))
        {
            message = "نوع الأصل المحدد يتطلب صورة أو حمولة مرئية صالحة";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool IsValidColorHex(string? value)
    {
        var token = value?.Trim();
        if (string.IsNullOrWhiteSpace(token) || token[0] != '#')
            return false;

        var hex = token[1..];
        return (hex.Length == 6 || hex.Length == 8) &&
               hex.All(Uri.IsHexDigit);
    }

    private static bool IsValidImagePayload(string? value)
    {
        var token = value?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return false;
        if (File.Exists(token))
            return true;

        var extension = Path.GetExtension(token);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".svg", StringComparison.OrdinalIgnoreCase);
    }
}

public static class StoreManagerAssetTypeScopes
{
    public static IReadOnlyList<string> AllSupportedProductTypes { get; } =
        StoreProductAssetTypeCatalog.CanonicalTypeIds;

    public static IReadOnlyList<string> ForSection(string sectionId) => sectionId switch
    {
        "avatars" => Types(StoreProductAssetType.Avatar),
        "backgrounds" => Types(StoreProductAssetType.ProfileBackground),
        "emblems" => Types(
            StoreProductAssetType.Emblem,
            StoreProductAssetType.EmblemBackground),
        "emblem-backgrounds" => Types(StoreProductAssetType.EmblemBackground),
        "effects" => Types(StoreProductAssetType.Effect),
        "frames" => Types(StoreProductAssetType.Frame),
        "titles" => Types(StoreProductAssetType.Title),
        "team-colors" => Types(StoreProductAssetType.TeamColor),
        "badges" => Types(StoreProductAssetType.Badge),
        "bundles" => Types(StoreProductAssetType.Bundle),
        "currency-pricing" or "top-up" => Types(StoreProductAssetType.CurrencyPack),
        "new-arrivals" or "current-season" or "limited-offers" =>
            AllSupportedProductTypes,
        "categories" => Array.Empty<string>(),
        _ => Array.Empty<string>()
    };

    public static bool IsAllowed(string sectionId, string? storeTypeId) =>
        StoreProductAssetTypeCatalog.TryResolve(storeTypeId, out var type) &&
        ForSection(sectionId).Contains(type.ToString(), StringComparer.Ordinal);

    private static IReadOnlyList<string> Types(params StoreProductAssetType[] types) =>
        types.Select(type => type.ToString()).ToArray();
}
