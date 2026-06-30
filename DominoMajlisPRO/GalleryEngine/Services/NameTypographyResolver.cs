using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed record NameTypographyIdentity(
    string OwnerId,
    CatalogAssetDisplay? NameEffect,
    CatalogAssetDisplay? NameFrame)
{
    public TypographyIdentityPreset? ResolvePreset()
    {
        var effect = NameEffect?.TypographyPreset;
        var frame = NameFrame?.TypographyPreset;
        if (effect == null && frame == null)
            return null;

        var merged = (effect ?? TypographyIdentityPreset.CreateDefault()).Normalized();
        if (frame == null)
            return merged;

        var framePreset = frame.Normalized();
        merged.FrameStylePreset = framePreset.FrameStylePreset;
        merged.FrameThickness = framePreset.FrameThickness;
        merged.SecondaryColor = framePreset.SecondaryColor;
        return merged.Normalized();
    }
}

public static class NameTypographyResolver
{
    public static async Task<NameTypographyIdentity?> ResolvePlayerAsync(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return null;

        var effectTask = ResolveEquippedPlayerAssetAsync(playerId, StoreProductAssetType.PlayerNameEffect.ToString());
        var frameTask = ResolveEquippedPlayerAssetAsync(playerId, StoreProductAssetType.PlayerNameFrame.ToString());
        await Task.WhenAll(effectTask, frameTask);
        return new NameTypographyIdentity(playerId, effectTask.Result, frameTask.Result);
    }

    public static async Task<NameTypographyIdentity?> ResolveTeamAsync(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return null;

        var effectTask = ResolveEquippedTeamAssetAsync(teamId, StoreProductAssetType.TeamNameEffect.ToString());
        var frameTask = ResolveEquippedTeamAssetAsync(teamId, StoreProductAssetType.TeamNameFrame.ToString());
        await Task.WhenAll(effectTask, frameTask);
        return new NameTypographyIdentity(teamId, effectTask.Result, frameTask.Result);
    }

    private static async Task<CatalogAssetDisplay?> ResolveEquippedPlayerAssetAsync(string playerId, string assetType)
    {
        var equipped = await PlayerAssetInventoryService.GetEquippedAsync(playerId, assetType);
        return equipped == null ? null : await StoreAssetCatalogService.ResolveAsync(equipped.AssetId, assetType);
    }

    private static async Task<CatalogAssetDisplay?> ResolveEquippedTeamAssetAsync(string teamId, string assetType)
    {
        var equipped = await TeamAssetInventoryService.GetEquippedAsync(teamId, assetType);
        return equipped == null ? null : await StoreAssetCatalogService.ResolveAsync(equipped.TeamAssetId, assetType);
    }
}
