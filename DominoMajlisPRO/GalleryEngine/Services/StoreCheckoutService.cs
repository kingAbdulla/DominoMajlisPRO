using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class StoreCheckoutService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<StoreCheckoutResult> PurchaseAsync(
        InventoryProductContext product)
    {
        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (owner.IsGhost)
            return Failure("A player account is required.");
        if (!owner.HasPlayerProfile ||
            string.IsNullOrWhiteSpace(owner.PlayerId))
        {
            return Failure("The current account has no player profile.");
        }
        if (!InventoryRouter.IsAvailable(product))
        {
            return Failure(
                InventoryRouter.GetAvailabilityMessage(product) ??
                "This offer is not currently available.");
        }

        var route = InventoryRouter.Resolve(product);
        if (route.OwnerScope == InventoryOwnerScope.Unsupported)
            return Failure("Unsupported inventory asset type.");
        if (product.Price is null or <= 0)
            return Failure("A valid paid price is required.");
        if (!TryCurrency(product.CurrencyMetadata, out var currency))
            return Failure("The product currency is not canonical.");

        await Gate.WaitAsync();
        try
        {
            if (route.OwnerScope == InventoryOwnerScope.Team)
            {
                var teamId = await ResolveActiveTeamIdAsync(owner.PlayerId);
                if (string.IsNullOrWhiteSpace(teamId))
                    return Failure("A team identity is required.");

                if (await TeamAssetInventoryService.IsOwnedAsync(
                        teamId,
                        product.AssetId,
                        route.StoreTypeId))
                {
                    return Failure("Item is already owned.");
                }
            }
            else if (await PlayerInventoryService.IsOwnedAsync(
                         owner.PlayerId,
                         product.AssetId))
            {
                return Failure("Item is already owned.");
            }

            var debit = await PlayerWalletService.TryDebitAsync(
                owner.PlayerId,
                currency,
                product.Price.Value);
            if (!debit.Success)
                return Failure("Insufficient wallet balance.");

            bool added;
            bool equipped = false;
            try
            {
                if (route.OwnerScope == InventoryOwnerScope.Team)
                {
                    var teamId = await ResolveActiveTeamIdAsync(owner.PlayerId);
                    if (string.IsNullOrWhiteSpace(teamId))
                    {
                        await RefundAsync(owner.PlayerId, currency, product.Price.Value);
                        return Failure("A team identity is required.");
                    }

                    added =
                        await TeamAssetInventoryService.AddOwnedAssetAsync(
                        teamId,
                        product.AssetId,
                        route.StoreTypeId,
                        "StorePurchase",
                        seasonId: product.SeasonId,
                        collectionId: product.CollectionId);
                    if (added && route.Equipable)
                        equipped = await TeamAssetInventoryService.EquipAsync(
                            teamId,
                            product.AssetId,
                            route.StoreTypeId);
                }
                else
                {
                    added = await PlayerInventoryService
                        .AddOwnedItemWithoutNotificationAsync(
                            owner.PlayerId,
                            product.AssetId,
                            route.StoreTypeId,
                            "StorePurchase",
                            seasonId: product.SeasonId,
                            collectionId: product.CollectionId);
                    if (added && route.Equipable)
                    {
                        try
                        {
                            equipped = route.EquipTarget is
                                InventoryEquipTarget.Avatar or
                                InventoryEquipTarget.ProfileBackground or
                                InventoryEquipTarget.Frame or
                                InventoryEquipTarget.Effect
                                    ? await StoreEquipService.EquipAsync(
                                        owner.PlayerId,
                                        product.AssetId)
                                    : await PlayerInventoryService
                                        .EquipItemWithoutNotificationAsync(
                                            owner.PlayerId,
                                            product.AssetId);
                        }
                        catch
                        {
                            // Acquisition is already persisted; a later
                            // explicit equip remains available in the UI.
                            equipped = false;
                        }
                    }
                }
            }
            catch
            {
                await RefundAsync(owner.PlayerId, currency, product.Price.Value);
                return Failure("Purchase could not be completed.");
            }

            if (!added)
            {
                await RefundAsync(owner.PlayerId, currency, product.Price.Value);
                return Failure("Item could not be added to inventory.");
            }

            AppEvents.RaiseStoreEconomyChanged(owner.PlayerId);
            return new StoreCheckoutResult(
                true,
                "Purchase completed.",
                true,
                equipped);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static bool TryCurrency(
        string? value,
        out StorePurchaseCurrencyType currency)
    {
        if (string.Equals(
                value?.Trim(),
                "Coins",
                StringComparison.OrdinalIgnoreCase))
        {
            currency = StorePurchaseCurrencyType.Coins;
            return true;
        }
        if (string.Equals(
                value?.Trim(),
                "Gems",
                StringComparison.OrdinalIgnoreCase))
        {
            currency = StorePurchaseCurrencyType.Gems;
            return true;
        }

        currency = StorePurchaseCurrencyType.Free;
        return false;
    }

    private static Task<PlayerWalletModel> RefundAsync(
        string playerId,
        StorePurchaseCurrencyType currency,
        int amount) =>
        currency == StorePurchaseCurrencyType.Coins
            ? PlayerWalletService.CreditAsync(playerId, coins: amount)
            : PlayerWalletService.CreditAsync(playerId, gems: amount);

    private static async Task<string?> ResolveActiveTeamIdAsync(
        string playerId)
    {
        var player = await PlayerProfileService.GetPlayerByIdAsync(playerId);
        var ids = (player?.CurrentTeamIds ?? string.Empty)
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();
        if (ids.Count == 1)
            return ids[0];
        if (ids.Count > 1)
            return null;
        var team = await TeamProfileService.GetTeamByPlayerIdAsync(playerId);
        return string.IsNullOrWhiteSpace(team?.TeamId)
            ? null
            : team.TeamId.Trim();
    }

    private static StoreCheckoutResult Failure(string message) =>
        new(false, message, false, false);
}
