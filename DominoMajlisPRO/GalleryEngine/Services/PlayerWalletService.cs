using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class PlayerWalletService
{
    private const string FileName = "player_wallets.json";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<PlayerWalletModel> GetOrCreateAsync(string playerId)
    {
        ValidatePlayerId(playerId);
        await Gate.WaitAsync();
        try
        {
            var wallets = await LoadAsync();
            var wallet = wallets.FirstOrDefault(item => item.PlayerId == playerId);
            if (wallet != null) return wallet;
            wallet = new PlayerWalletModel { PlayerId = playerId, Coins = 0, Gems = 0 };
            wallets.Add(wallet);
            await SaveAsync(wallets);
            return wallet;
        }
        finally { Gate.Release(); }
    }

    public static async Task<PlayerWalletModel> CreditAsync(string playerId, int coins = 0, int gems = 0)
    {
        ValidatePlayerId(playerId);
        if (coins < 0 || gems < 0) throw new ArgumentOutOfRangeException(nameof(coins), "Wallet credit cannot be negative.");
        await Gate.WaitAsync();
        try
        {
            var wallets = await LoadAsync();
            var wallet = GetOrCreate(wallets, playerId);
            wallet.Coins = checked(wallet.Coins + coins);
            wallet.Gems = checked(wallet.Gems + gems);
            wallet.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(wallets);
            AppEvents.RaiseWalletChanged(playerId);
            return wallet;
        }
        finally { Gate.Release(); }
    }

    internal static async Task<(bool Success, PlayerWalletModel Wallet)> TryDebitAsync(string playerId, StorePurchaseCurrencyType currency, int amount)
    {
        ValidatePlayerId(playerId);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        await Gate.WaitAsync();
        try
        {
            var wallets = await LoadAsync();
            var wallet = GetOrCreate(wallets, playerId);
            var enough = currency switch
            {
                StorePurchaseCurrencyType.Coins => wallet.Coins >= amount,
                StorePurchaseCurrencyType.Gems => wallet.Gems >= amount,
                _ => true
            };
            if (!enough)
            {
                await SaveAsync(wallets);
                return (false, wallet);
            }
            if (currency == StorePurchaseCurrencyType.Coins) wallet.Coins -= amount;
            if (currency == StorePurchaseCurrencyType.Gems) wallet.Gems -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(wallets);
            AppEvents.RaiseWalletChanged(playerId);
            return (true, wallet);
        }
        finally { Gate.Release(); }
    }

    private static PlayerWalletModel GetOrCreate(List<PlayerWalletModel> wallets, string playerId)
    {
        var wallet = wallets.FirstOrDefault(item => item.PlayerId == playerId);
        if (wallet != null) return wallet;
        wallet = new PlayerWalletModel { PlayerId = playerId };
        wallets.Add(wallet);
        return wallet;
    }

    private static void ValidatePlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("PlayerId is required.", nameof(playerId));
    }

    private static string StoragePath => Path.Combine(FileSystem.AppDataDirectory, FileName);
    private static Task<List<PlayerWalletModel>> LoadAsync() => StoreCmsJsonRepository.LoadListAsync<PlayerWalletModel>(StoragePath);
    private static Task SaveAsync(IReadOnlyList<PlayerWalletModel> records) => StoreCmsJsonRepository.SaveListAsync(StoragePath, records);
}
