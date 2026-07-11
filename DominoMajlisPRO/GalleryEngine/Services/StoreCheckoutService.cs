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
                return Failure("الرصيد غير كافٍ لإتمام الشراء.");

            var added = await PlayerInventoryService.AddOwnedItemWithoutNotificationAsync(
                owner.PlayerId,
                product.AssetId,
                route.StoreTypeId,
                "StorePurchase",
                seasonId: product.SeasonId,
                collectionId: product.CollectionId);

            var equipped = false;
            if (added && route.OwnerScope == InventoryOwnerScope.Player && route.Equipable)
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

            if (!added)
                return Failure("تعذر إضافة العنصر إلى المقتنيات.");

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

    private static StoreCheckoutResult Failure(string message) =>
        new(false, message, false, false);
}
