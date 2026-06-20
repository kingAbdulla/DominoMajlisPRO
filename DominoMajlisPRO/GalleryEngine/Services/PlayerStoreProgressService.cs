using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerStoreProgressService
{
    public static async Task<PlayerStoreProgressModel> CalculateAsync(
        string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException(
                "PlayerId is required.",
                nameof(playerId));

        var team = await TeamProfileService.GetTeamByPlayerIdAsync(playerId);
        var snapshot = await InventoryDisplayResolver.ResolveAsync(
            playerId,
            team?.TeamId);
        var counts = snapshot.ByAssetType;
        return new PlayerStoreProgressModel
        {
            PlayerId = playerId,
            TotalOwned = snapshot.TotalOwned,
            TotalAvailable = snapshot.TotalAvailable,
            CompletionPercent = snapshot.CompletionPercent,
            OwnedAvatars = Owned(counts, "Avatar"),
            OwnedBackgrounds = Owned(counts, "ProfileBackground"),
            OwnedFrames = Owned(counts, "Frame"),
            OwnedEffects = Owned(counts, "Effect"),
            OwnedBadges = Owned(counts, "Badge"),
            OwnedEmblems = Owned(counts, "Emblem"),
            OwnedBundles = 0,
            ByItemType = counts,
            ByRarity = Array.Empty<StoreProgressCount>(),
            ByCollection = Array.Empty<StoreProgressCount>()
        };
    }

    private static int Owned(
        IEnumerable<StoreProgressCount> counts,
        string assetType) =>
        counts.FirstOrDefault(item =>
            string.Equals(
                item.Key,
                assetType,
                StringComparison.OrdinalIgnoreCase))?.Owned ?? 0;
}
