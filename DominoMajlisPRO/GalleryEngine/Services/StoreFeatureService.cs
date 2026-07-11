using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed class WheelClaimRecord
{
    public string PlayerId { get; set; } = string.Empty;
    public DateTime LastFreeSpinUtc { get; set; }
}

public enum WheelRewardType
{
    Coins,
    Gems,
    Asset,
    XpBonus
}

public sealed record WheelRewardDefinition(
    string RewardId,
    string DisplayName,
    WheelRewardType RewardType,
    int Amount,
    int ProbabilityWeight,
    string AssetId = "",
    string AssetType = "");

public sealed class WheelSpinHistoryRecord
{
    public string SpinId { get; set; } = Guid.NewGuid().ToString("N");
    public string PlayerId { get; set; } = string.Empty;
    public string RewardId { get; set; } = string.Empty;
    public string RewardName { get; set; } = string.Empty;
    public WheelRewardType RewardType { get; set; }
    public int Amount { get; set; }
    public string AssetId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public bool WasFreeSpin { get; set; }
    public DateTime SpunAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed record WheelSpinResult(
    bool Success,
    string Message,
    WheelRewardDefinition? Reward,
    bool WasFreeSpin);

public static class StoreFeatureService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly SemaphoreSlim WheelGate = new(1, 1);
    private static string PathFor(string file) => Path.Combine(FileSystem.AppDataDirectory, file);

    public static IReadOnlyList<WheelRewardDefinition> RewardsPool { get; } =
    [
        new("coins-25", "25 عملة", WheelRewardType.Coins, 25, 24),
        new("coins-50", "50 عملة", WheelRewardType.Coins, 50, 28),
        new("coins-100", "100 عملة", WheelRewardType.Coins, 100, 18),
        new("gems-5", "5 جواهر", WheelRewardType.Gems, 5, 15),
        new("gems-10", "10 جواهر", WheelRewardType.Gems, 10, 10),
        new("catalog-cosmetic", "عنصر تجميلي", WheelRewardType.Asset, 1, 5)
    ];

    public static async Task<bool> CanUseFreeSpinAsync(string playerId)
    {
        var claims = await StoreCmsJsonRepository.LoadListAsync<WheelClaimRecord>(PathFor("wheel_claims.json"));
        var claim = claims.FirstOrDefault(x => Same(x.PlayerId, playerId));
        return claim == null || claim.LastFreeSpinUtc.Date < DateTime.UtcNow.Date;
    }

    public static async Task<IReadOnlyList<WheelSpinHistoryRecord>> GetWheelHistoryAsync(
        string playerId,
        int take = 20)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return Array.Empty<WheelSpinHistoryRecord>();

        var records = await StoreCmsJsonRepository.LoadListAsync<WheelSpinHistoryRecord>(
            PathFor("wheel_spin_history.json"));
        return records
            .Where(record => Same(record.PlayerId, playerId))
            .OrderByDescending(record => record.SpunAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToList();
    }

    public static async Task<WheelSpinResult> SpinWheelAsync(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return new(false, "PlayerId مطلوب.", null, false);

        await WheelGate.WaitAsync();
        try
        {
            var eligiblePool = await BuildEligibleRewardsPoolAsync(playerId);
            if (eligiblePool.Count == 0)
                return new(false, "لا توجد جوائز متاحة حالياً.", null, false);

            var freeSpin = await CanUseFreeSpinAsync(playerId);
            var paidSpinDebited = false;
            if (!freeSpin)
            {
                var debit = await PlayerWalletService.TryDebitAsync(
                    playerId,
                    StorePurchaseCurrencyType.Gems,
                    10);
                if (!debit.Success)
                    return new(false, "لا توجد جواهر كافية.", null, false);
                paidSpinDebited = true;
            }

            try
            {
                var reward = SelectWeightedReward(eligiblePool);
                if (reward == null)
                    return new(false, "لا توجد جوائز متاحة حالياً.", null, false);

                if (!await GrantWheelRewardAsync(playerId, reward))
                    throw new InvalidOperationException("Reward grant failed.");

                await SaveWheelHistoryAsync(playerId, reward, freeSpin);
                if (freeSpin)
                    await MarkFreeSpinAsync(playerId);

                AppEvents.RaiseStoreEconomyChanged(playerId);
                return new(true, $"الجائزة: {reward.DisplayName}", reward, freeSpin);
            }
            catch
            {
                if (paidSpinDebited)
                    await PlayerWalletService.CreditAsync(playerId, gems: 10);
                throw;
            }
        }
        catch
        {
            return new(false, "تعذر إكمال دوران العجلة بأمان.", null, false);
        }
        finally
        {
            WheelGate.Release();
        }
    }

    private static async Task MarkFreeSpinAsync(string playerId)
    {
        await Gate.WaitAsync();
        try
        {
            var path = PathFor("wheel_claims.json");
            var claims = await StoreCmsJsonRepository.LoadListAsync<WheelClaimRecord>(path);
            var claim = claims.FirstOrDefault(x => Same(x.PlayerId, playerId));
            if (claim == null)
            {
                claim = new WheelClaimRecord { PlayerId = playerId };
                claims.Add(claim);
            }

            claim.LastFreeSpinUtc = DateTime.UtcNow;
            await StoreCmsJsonRepository.SaveListAsync(path, claims);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static WheelRewardDefinition? SelectWeightedReward(IReadOnlyList<WheelRewardDefinition> pool)
    {
        var totalWeight = pool.Sum(reward => Math.Max(0, reward.ProbabilityWeight));
        if (totalWeight <= 0)
            return null;

        var roll = Random.Shared.Next(1, totalWeight + 1);
        var running = 0;
        foreach (var reward in pool)
        {
            running += Math.Max(0, reward.ProbabilityWeight);
            if (roll <= running)
                return reward;
        }

        return pool[^1];
    }

    private static async Task<IReadOnlyList<WheelRewardDefinition>> BuildEligibleRewardsPoolAsync(string playerId)
    {
        var pool = RewardsPool
            .Where(reward => reward.ProbabilityWeight > 0)
            .ToList();
        var assetTemplate = pool.FirstOrDefault(reward => reward.RewardType == WheelRewardType.Asset);
        if (assetTemplate == null)
            return pool;

        var catalog = await StoreAssetCatalogService.LoadAsync();
        var inventory = await PlayerInventoryService.GetInventoryForPlayerAsync(playerId);
        var ownedIds = inventory
            .Where(item => item.IsOwned && !item.IsExpired)
            .Select(item => item.AssetId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var asset = catalog
            .Where(item =>
                item.OwnerScope == StoreProductOwnerScope.Player &&
                item.HasDisplayMetadata &&
                !ownedIds.Contains(item.AssetId) &&
                item.AssetType is
                    StoreProductAssetType.Avatar or
                    StoreProductAssetType.ProfileBackground or
                    StoreProductAssetType.Frame or
                    StoreProductAssetType.Effect or
                    StoreProductAssetType.Title)
            .OrderBy(item => item.AssetType)
            .ThenBy(item => item.AssetId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        pool.Remove(assetTemplate);
        if (asset != null)
        {
            pool.Add(assetTemplate with
            {
                DisplayName = string.IsNullOrWhiteSpace(asset.ArabicDisplayName)
                    ? asset.DisplayName
                    : asset.ArabicDisplayName,
                AssetId = asset.AssetId,
                AssetType = asset.AssetType.ToString()
            });
        }

        return pool;
    }

    private static async Task<bool> GrantWheelRewardAsync(
        string playerId,
        WheelRewardDefinition reward)
    {
        switch (reward.RewardType)
        {
            case WheelRewardType.Coins:
                await PlayerWalletService.CreditAsync(playerId, coins: reward.Amount);
                return true;
            case WheelRewardType.Gems:
                await PlayerWalletService.CreditAsync(playerId, gems: reward.Amount);
                return true;
            case WheelRewardType.Asset:
                if (string.IsNullOrWhiteSpace(reward.AssetId) ||
                    string.IsNullOrWhiteSpace(reward.AssetType))
                    return false;
                return await PlayerInventoryService.AddOwnedItemAsync(
                    playerId,
                    reward.AssetId,
                    reward.AssetType,
                    "LuckyWheel");
            case WheelRewardType.XpBonus:
                return false;
            default:
                return false;
        }
    }

    private static async Task SaveWheelHistoryAsync(
        string playerId,
        WheelRewardDefinition reward,
        bool wasFreeSpin)
    {
        var path = PathFor("wheel_spin_history.json");
        var records = await StoreCmsJsonRepository.LoadListAsync<WheelSpinHistoryRecord>(path);
        records.Add(new WheelSpinHistoryRecord
        {
            PlayerId = playerId.Trim(),
            RewardId = reward.RewardId,
            RewardName = reward.DisplayName,
            RewardType = reward.RewardType,
            Amount = reward.Amount,
            AssetId = reward.AssetId,
            AssetType = reward.AssetType,
            WasFreeSpin = wasFreeSpin,
            SpunAtUtc = DateTime.UtcNow
        });
        await StoreCmsJsonRepository.SaveListAsync(
            path,
            records
                .OrderByDescending(record => record.SpunAtUtc)
                .Take(500)
                .ToList());
    }

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
}
