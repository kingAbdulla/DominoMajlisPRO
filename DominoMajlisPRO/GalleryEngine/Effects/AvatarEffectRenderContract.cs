using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public enum AvatarEffectRenderSurface
{
    DeveloperStudio,
    StoreCard,
    StorePreview,
    Inventory,
    PlayerAvatarRuntime
}

public sealed record AvatarEffectRenderContract(
    AvatarEffectRenderSurface Surface,
    double HostSize,
    double BaseScale,
    bool Lightweight,
    string DiagnosticName)
{
    public const double PremiumDefaultBaseScale = 1.42;
    public const double PremiumAvatarRuntimeBaseScale = 1.48;
    public const double PremiumStoreCardBaseScale = 1.38;
    public const double PremiumStudioBaseScale = 1.48;
    public const double PremiumPreviewBaseScale = 1.50;

    public static AvatarEffectRenderContract DeveloperStudio { get; } = new(
        AvatarEffectRenderSurface.DeveloperStudio,
        168,
        PremiumStudioBaseScale,
        false,
        "Developer Studio / exact runtime renderer");

    public static AvatarEffectRenderContract StoreCard { get; } = new(
        AvatarEffectRenderSurface.StoreCard,
        118,
        PremiumStoreCardBaseScale,
        true,
        "Store Card / lightweight runtime renderer");

    public static AvatarEffectRenderContract StorePreview { get; } = new(
        AvatarEffectRenderSurface.StorePreview,
        176,
        PremiumPreviewBaseScale,
        false,
        "Store Preview / full runtime renderer");

    public static AvatarEffectRenderContract Inventory { get; } = new(
        AvatarEffectRenderSurface.Inventory,
        148,
        PremiumAvatarRuntimeBaseScale,
        true,
        "Inventory / lightweight runtime renderer");

    public static AvatarEffectRenderContract PlayerAvatarRuntime { get; } = new(
        AvatarEffectRenderSurface.PlayerAvatarRuntime,
        156,
        PremiumAvatarRuntimeBaseScale,
        true,
        "Player Avatar Runtime / lightweight renderer");

    public static AvatarEffectRenderContract ResolveFor(CatalogAssetDisplay? effect, AvatarEffectRenderSurface surface)
    {
        var baseContract = surface switch
        {
            AvatarEffectRenderSurface.DeveloperStudio => DeveloperStudio,
            AvatarEffectRenderSurface.StoreCard => StoreCard,
            AvatarEffectRenderSurface.StorePreview => StorePreview,
            AvatarEffectRenderSurface.Inventory => Inventory,
            AvatarEffectRenderSurface.PlayerAvatarRuntime => PlayerAvatarRuntime,
            _ => PlayerAvatarRuntime
        };

        if (effect == null)
            return baseContract;

        var isPlayerEffect = effect.AssetType == StoreProductAssetType.Effect ||
            string.Equals(effect.StoreTypeId, StoreProductAssetType.Effect.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(effect.EquipTarget, "PlayerEffect", StringComparison.OrdinalIgnoreCase);

        if (!isPlayerEffect)
            return baseContract;

        var premiumBoost = ResolvePremiumBoost(effect);
        return baseContract with
        {
            BaseScale = Math.Clamp(baseContract.BaseScale + premiumBoost, 1.18, 1.78),
            HostSize = Math.Clamp(baseContract.HostSize + premiumBoost * 28, 96, 190)
        };
    }

    private static double ResolvePremiumBoost(CatalogAssetDisplay effect)
    {
        var text = $"{effect.AssetId} {effect.DisplayName} {effect.ArabicDisplayName} {effect.EffectType} {effect.Rarity}".ToLowerInvariant();
        if (text.Contains("legendary") || text.Contains("mythic") || text.Contains("immortal") || text.Contains("أسطوري"))
            return 0.16;
        if (text.Contains("royal") || text.Contains("dragon") || text.Contains("eagle") || text.Contains("lion") || text.Contains("diamond"))
            return 0.12;
        if (text.Contains("rare") || text.Contains("epic"))
            return 0.08;
        return 0.04;
    }
}
