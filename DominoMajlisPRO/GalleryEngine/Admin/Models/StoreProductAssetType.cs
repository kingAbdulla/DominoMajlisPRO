namespace DominoMajlisPRO.GalleryEngine.Admin.Models;

public enum StoreProductAssetType
{
    Avatar,
    ProfileBackground,
    Frame,
    Effect,
    TeamEffect,
    PlayerNameEffect,
    TeamNameEffect,
    PlayerNameFrame,
    TeamNameFrame,
    Title,
    Emblem,
    TeamLivingEmblem,
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
        StoreProductAssetType.PlayerNameEffect or
        StoreProductAssetType.PlayerNameFrame or
        StoreProductAssetType.TeamEffect or
        StoreProductAssetType.Title or
        StoreProductAssetType.Badge or
        StoreProductAssetType.SeasonReward => StoreProductOwnerScope.Player,

        StoreProductAssetType.Emblem or
        StoreProductAssetType.TeamLivingEmblem or
        StoreProductAssetType.TeamColor or
        StoreProductAssetType.EmblemBackground or
        StoreProductAssetType.TeamNameEffect or
        StoreProductAssetType.TeamNameFrame => StoreProductOwnerScope.Team,

        _ => StoreProductOwnerScope.None
    };

    public static bool IsInventory(StoreProductAssetType type) =>
        GetOwnerScope(type) != StoreProductOwnerScope.None;

    public static bool RequiresImagePayload(StoreProductAssetType type) => type is
        StoreProductAssetType.Avatar or
        StoreProductAssetType.ProfileBackground or
        StoreProductAssetType.Emblem or
        StoreProductAssetType.TeamLivingEmblem or
        StoreProductAssetType.EmblemBackground or
        StoreProductAssetType.Frame;

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

        if (RequiresImagePayload(type) && string.IsNullOrWhiteSpace(imagePath))
        {
            message = "صورة الأصل مطلوبة لهذا النوع";
            return false;
        }

        if (type == StoreProductAssetType.TeamColor && !IsValidColor(colorHex))
        {
            message = "لون الفريق مطلوب ويجب أن يكون بصيغة Hex صحيحة";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool IsValidColor(string? value)
    {
        var token = value?.Trim();
        if (string.IsNullOrWhiteSpace(token) || token[0] != '#')
            return false;

        var hex = token[1..];
        return (hex.Length == 6 || hex.Length == 8) && hex.All(Uri.IsHexDigit);
    }
}
