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
            return Failure("يجب اختيار حساب لاعب قبل الشراء.");
        if (!owner.HasPlayerProfile || string.IsNullOrWhiteSpace(owner.PlayerId))
            return Failure("الحساب الحالي غير مرتبط بملف لاعب.");

        var route = InventoryRouter.Resolve(product);
        if (route.OwnerScope == InventoryOwnerScope.Unsupported)
            return Failure("نوع هذا المنتج غير مدعوم حالياً.");
        if (!await ValidateExplicitTeamContextAsync(product, owner.PlayerId, route))
            return Failure("يجب اختيار فريق صالح قبل تجهيز هذا العنصر.");
        if (product.Price is null or <= 0)
            return Failure("سعر المنتج غير صالح.");
        if (!TryCurrency(product.CurrencyMetadata, out var currency))
            return Failure("عملة المنتج غير صحيحة.");

        await Gate.WaitAsync();
        try
        {
            if (await PlayerInventoryService.IsOwnedAsync(owner.PlayerId, product.AssetId))
                return Failure("هذا العنصر مملوك مسبقاً.");

            var debit = await PlayerWalletService.TryDebitAsync(
                owner.PlayerId,
                currency,
                product.Price.Value);
            if (!debit.Success)
                return Failure("الرصيد غير كاف لإتمام الشراء.");

            var added = await PlayerInventoryService.AddOwnedItemWithoutNotificationAsync(
                owner.PlayerId,
                product.AssetId,
                route.StoreTypeId,
                "StorePurchase",
                seasonId: product.SeasonId,
                collectionId: product.CollectionId);

            if (!added)
            {
                await RefundDebitAsync(owner.PlayerId, currency, product.Price.Value);
                return Failure("تعذر إضافة العنصر إلى المقتنيات، وتمت إعادة الرصيد.");
            }

            var equipped = false;
            if (route.OwnerScope == InventoryOwnerScope.Team)
            {
                if (string.IsNullOrWhiteSpace(product.TeamId))
                {
                    await RefundDebitAsync(owner.PlayerId, currency, product.Price.Value);
                    return Failure("يجب اختيار الفريق قبل تجهيز هذا العنصر.");
                }

                await TeamAssetInventoryService.AddOwnedAssetAsync(
                    product.TeamId,
                    product.AssetId,
                    route.StoreTypeId,
                    "StorePurchase",
                    seasonId: product.SeasonId,
                    collectionId: product.CollectionId);

                equipped = await TeamAssetInventoryService.EquipAsync(
                    product.TeamId,
                    product.AssetId,
                    route.StoreTypeId);

                if (equipped)
                    AppEvents.RaiseTeamIdentityChanged(product.TeamId);
            }
            else if (route.OwnerScope == InventoryOwnerScope.Player && route.Equipable)
            {
                if (route.EquipTarget == InventoryEquipTarget.TeamEffect)
                {
                    if (string.IsNullOrWhiteSpace(product.TeamId))
                    {
                        await RefundDebitAsync(owner.PlayerId, currency, product.Price.Value);
                        return Failure("يجب اختيار الفريق قبل تجهيز هذا العنصر.");
                    }

                    await PlayerInventoryService.EquipItemWithoutNotificationAsync(
                        owner.PlayerId,
                        product.AssetId);

                    equipped = await TeamEffectEngine.EquipAsync(
                        owner.PlayerId,
                        product.TeamId,
                        product.AssetId);
                }
                else if (route.EquipTarget is InventoryEquipTarget.TeamNameEffect or InventoryEquipTarget.TeamNameFrame)
                {
                    if (string.IsNullOrWhiteSpace(product.TeamId))
                    {
                        await RefundDebitAsync(owner.PlayerId, currency, product.Price.Value);
                        return Failure("يجب اختيار الفريق قبل تجهيز هذا العنصر.");
                    }

                    await PlayerInventoryService.EquipItemWithoutNotificationAsync(
                        owner.PlayerId,
                        product.AssetId);

                    await TeamAssetInventoryService.AddOwnedAssetAsync(
                        product.TeamId,
                        product.AssetId,
                        route.StoreTypeId,
                        $"Player:{owner.PlayerId}",
                        seasonId: product.SeasonId,
                        collectionId: product.CollectionId);

                    equipped = await TeamAssetInventoryService.EquipAsync(
                        product.TeamId,
                        product.AssetId,
                        route.StoreTypeId);

                    if (equipped)
                        AppEvents.RaiseTeamIdentityChanged(product.TeamId);
                }
                else
                {
                    equipped = route.EquipTarget is
                        InventoryEquipTarget.Avatar or
                        InventoryEquipTarget.ProfileBackground or
                        InventoryEquipTarget.Frame or
                        InventoryEquipTarget.Effect
                            ? await StoreEquipService.EquipAsync(owner.PlayerId, product.AssetId)
                            : await PlayerInventoryService.EquipItemWithoutNotificationAsync(
                                owner.PlayerId,
                                product.AssetId);
                }
            }

            AppEvents.RaiseStoreEconomyChanged(owner.PlayerId);
            return new StoreCheckoutResult(true, "تم الشراء بنجاح.", true, equipped);
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
        if (string.Equals(value?.Trim(), "Coins", StringComparison.OrdinalIgnoreCase))
        {
            currency = StorePurchaseCurrencyType.Coins;
            return true;
        }

        if (string.Equals(value?.Trim(), "Gems", StringComparison.OrdinalIgnoreCase))
        {
            currency = StorePurchaseCurrencyType.Gems;
            return true;
        }

        currency = StorePurchaseCurrencyType.Free;
        return false;
    }

    private static async Task RefundDebitAsync(
        string playerId,
        StorePurchaseCurrencyType currency,
        int amount)
    {
        if (amount <= 0) return;
        if (currency == StorePurchaseCurrencyType.Coins)
            await PlayerWalletService.CreditAsync(playerId, coins: amount);
        else if (currency == StorePurchaseCurrencyType.Gems)
            await PlayerWalletService.CreditAsync(playerId, gems: amount);
    }

    private static StoreCheckoutResult Failure(string message) =>
        new(false, message, false, false);

    private static async Task<bool> ValidateExplicitTeamContextAsync(
        InventoryProductContext product,
        string playerId,
        InventoryRoute route)
    {
        if (!RequiresTeamContext(route))
            return true;

        if (string.IsNullOrWhiteSpace(product.TeamId))
            return false;

        var team = await TeamProfileService.GetTeamByIdAsync(product.TeamId.Trim());
        return team != null && TeamEffectEngine.IsManagedBy(team, playerId);
    }

    private static bool RequiresTeamContext(InventoryRoute route) =>
        route.OwnerScope == InventoryOwnerScope.Team ||
        route.EquipTarget is InventoryEquipTarget.TeamEffect or
            InventoryEquipTarget.TeamNameEffect or
            InventoryEquipTarget.TeamNameFrame;
}
