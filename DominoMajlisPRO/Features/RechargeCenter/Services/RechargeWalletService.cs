using DominoMajlisPRO.Features.RechargeCenter.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Features.RechargeCenter.Services;

public static class RechargeWalletService
{
    private const string FileName = "recharge_wallets.json";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string StoragePath => Path.Combine(FileSystem.AppDataDirectory, FileName);

    public static async Task<RechargeWalletModel> GetOrCreateAsync(string playerId)
    {
        Validate(playerId);
        await Gate.WaitAsync();
        try
        {
            var snapshots = await StoreCmsJsonRepository.LoadListAsync<RechargeWalletModel>(StoragePath);
            var snapshot = snapshots.FirstOrDefault(x => Same(x.PlayerId, playerId));
            var wallet = await PlayerWalletService.GetOrCreateAsync(playerId);

            snapshot = ToRechargeWallet(wallet.PlayerId, wallet.Coins, wallet.Gems, wallet.UpdatedAt);
            snapshots.RemoveAll(x => Same(x.PlayerId, playerId));
            snapshots.Add(snapshot);
            await StoreCmsJsonRepository.SaveListAsync(StoragePath, snapshots);
            return snapshot;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static Task<RechargeWalletModel> AddPromotionalCoinsAsync(string playerId, int amount, string reason) =>
        CreditAsync(playerId, amount, 0, reason);

    public static Task<RechargeWalletModel> AddPromotionalGemsAsync(string playerId, int amount, string reason) =>
        CreditAsync(playerId, 0, amount, reason);

    public static async Task<RechargeWalletModel> SyncAuthoritativeAsync(
        string playerId,
        int coins,
        int gems,
        string sourceVersion)
    {
        Validate(playerId);
        await Gate.WaitAsync();
        try
        {
            var wallet = await PlayerWalletService.SetAuthoritativeAsync(playerId, coins, gems, sourceVersion);
            var snapshots = await StoreCmsJsonRepository.LoadListAsync<RechargeWalletModel>(StoragePath);
            snapshots.RemoveAll(x => Same(x.PlayerId, playerId));
            var snapshot = ToRechargeWallet(playerId, wallet.Coins, wallet.Gems, wallet.UpdatedAt);
            snapshots.Add(snapshot);
            await StoreCmsJsonRepository.SaveListAsync(StoragePath, snapshots);
            AppEvents.RaiseStoreEconomyChanged(playerId);
            return snapshot;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<RechargeWalletModel> CreditAsync(string playerId, int coins, int gems, string reason)
    {
        Validate(playerId);
        if (coins < 0 || gems < 0) throw new ArgumentOutOfRangeException(nameof(coins));
        await Gate.WaitAsync();
        try
        {
            var wallet = await PlayerWalletService.CreditAsync(playerId, coins, gems);
            var snapshots = await StoreCmsJsonRepository.LoadListAsync<RechargeWalletModel>(StoragePath);
            snapshots.RemoveAll(x => Same(x.PlayerId, playerId));
            var snapshot = ToRechargeWallet(playerId, wallet.Coins, wallet.Gems, wallet.UpdatedAt);
            snapshots.Add(snapshot);
            await StoreCmsJsonRepository.SaveListAsync(StoragePath, snapshots);
            AppEvents.RaiseStoreEconomyChanged(playerId);
            return snapshot;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<bool> DeductCoinsAsync(string playerId, int amount) =>
        await DeductAsync(playerId, amount, true);

    public static async Task<bool> DeductGemsAsync(string playerId, int amount) =>
        await DeductAsync(playerId, amount, false);

    private static async Task<bool> DeductAsync(string playerId, int amount, bool coins)
    {
        Validate(playerId);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var currency = coins
            ? GalleryEngine.Models.StorePurchaseCurrencyType.Coins
            : GalleryEngine.Models.StorePurchaseCurrencyType.Gems;
        var result = await PlayerWalletService.TryDebitAsync(playerId, currency, amount);
        if (!result.Success) return false;
        await GetOrCreateAsync(playerId);
        AppEvents.RaiseStoreEconomyChanged(playerId);
        return true;
    }

    private static RechargeWalletModel ToRechargeWallet(string playerId, int coins, int gems, DateTime updatedAt) =>
        new() { PlayerId = playerId, Coins = Math.Max(0, coins), Gems = Math.Max(0, gems), UpdatedAtUtc = updatedAt };

    private static void Validate(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("PlayerId is required.", nameof(playerId));
    }

    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
}
