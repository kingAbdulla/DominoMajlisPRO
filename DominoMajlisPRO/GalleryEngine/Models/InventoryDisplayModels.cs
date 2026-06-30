using DominoMajlisPRO.GalleryEngine.Admin.Models;

namespace DominoMajlisPRO.GalleryEngine.Models;

public sealed record CatalogAssetDisplay(
    string AssetId,
    StoreProductAssetType AssetType,
    StoreProductOwnerScope OwnerScope,
    string DisplayName,
    string ArabicDisplayName,
    string PreviewImage,
    string ColorHex,
    IReadOnlyList<string> ProductIds,
    string EffectType = "",
    string AnimationType = "",
    int DurationMilliseconds = 0,
    string EquipTarget = "",
    string PrimaryColorPresetId = "",
    string SecondaryColorPresetId = "",
    string CustomPrimaryColorHex = "",
    string CustomSecondaryColorHex = "",
    IReadOnlyList<string>? EffectLayerIdsValue = null,
    float EffectOpacity = 1,
    float EffectScale = 1,
    float EffectSpeed = 1,
    float EffectIntensity = 1,
    TypographyIdentityPreset? TypographyPresetValue = null)
{
    public IReadOnlyList<string> EffectLayerIds =>
        EffectLayerIdsValue ?? Array.Empty<string>();

    public bool HasDisplayMetadata =>
        !string.IsNullOrWhiteSpace(DisplayName) &&
        (!string.IsNullOrWhiteSpace(PreviewImage) ||
         !string.IsNullOrWhiteSpace(ColorHex) ||
         TypographyPresetValue != null);

    public TypographyIdentityPreset TypographyPreset =>
        (TypographyPresetValue ?? TypographyIdentityPreset.CreateDefault()).Normalized();
}

public sealed record ResolvedInventoryDisplay(
    string ProductId,
    string AssetId,
    string AssetType,
    string DisplayName,
    string ArabicDisplayName,
    string PreviewImage,
    string ColorHex,
    bool IsOwned,
    bool IsEquipped,
    bool IsTeamAsset,
    bool HasCatalogDisplayMetadata);

public sealed record MissingCatalogDisplayMetadata(
    string ProductId,
    string AssetId,
    string AssetType);

public sealed record StoreProductAssetReference(
    string ProductId,
    string AssetId,
    string AssetType);

public sealed record InventoryCollectionSnapshot(
    IReadOnlyList<ResolvedInventoryDisplay> Items,
    IReadOnlyList<StoreProgressCount> ByAssetType,
    int TotalOwned,
    int TotalAvailable,
    double CompletionPercent,
    IReadOnlyList<MissingCatalogDisplayMetadata> MissingMetadata);

public sealed record PlayerVisualIdentity(
    string PlayerId,
    CatalogAssetDisplay? Avatar,
    CatalogAssetDisplay? ProfileBackground,
    CatalogAssetDisplay? Frame,
    CatalogAssetDisplay? Effect,
    CatalogAssetDisplay? PlayerNameEffect,
    CatalogAssetDisplay? PlayerNameFrame,
    CatalogAssetDisplay? Title);

public sealed record StoreCheckoutResult(
    bool Success,
    string Message,
    bool WasAdded,
    bool WasEquipped);
