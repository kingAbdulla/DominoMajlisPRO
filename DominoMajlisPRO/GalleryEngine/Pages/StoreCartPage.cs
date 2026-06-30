using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Pages;

internal sealed class StoreCartPage : StoreFeaturePageBase
{
    private string _playerId = string.Empty;

    public StoreCartPage() : base("سلة المشتريات")
    {
        Loaded += async (_, _) => await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        while (Body.Children.Count > 1)
            Body.Children.RemoveAt(Body.Children.Count - 1);

        var owner = await ApplicationUserService.GetCurrentStoreOwnerAsync();
        if (!owner.HasPlayerProfile || string.IsNullOrWhiteSpace(owner.PlayerId))
        {
            Body.Children.Add(Text("سجّل الدخول بحساب لاعب أولاً."));
            Body.Children.Add(Action("إلغاء العملية", async (_, _) => await Navigation.PopAsync()));
            return;
        }

        _playerId = owner.PlayerId.Trim();
        var catalog = GalleryService.GetCatalog();
        var ids = await StoreFeatureService.GetCartAsync(_playerId);
        var selected = catalog.Items
            .Where(item => ids.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        Body.Children.Add(Text(selected.Count == 0
            ? "السلة فارغة."
            : $"العناصر: {selected.Count}   الإجمالي: {selected.Sum(item => item.Price):N0}"));

        foreach (var item in selected)
        {
            Body.Children.Add(Action($"حذف — {item.Name}", async (_, _) =>
            {
                await StoreFeatureService.RemoveFromCartAsync(_playerId, item.Id);
                await ReloadAsync();
            }));
        }

        if (selected.Count > 0)
            Body.Children.Add(Action("تأكيد الشراء", async (_, _) => await CheckoutAsync(selected)));

        Body.Children.Add(Action("إلغاء العملية", async (_, _) => await Navigation.PopAsync()));
    }

    private async Task CheckoutAsync(IReadOnlyList<GalleryItem> items)
    {
        var purchased = 0;
        foreach (var item in items)
        {
            var result = await StoreCheckoutService.PurchaseAsync(Product(item));
            if (!result.Success)
                continue;

            purchased++;
            await StoreFeatureService.RemoveFromCartAsync(_playerId, item.Id);
        }

        await DisplayAlertAsync(
            "سلة المشتريات",
            purchased == items.Count
                ? "تم تأكيد الشراء وإضافة العناصر إلى مقتنياتك."
                : $"تم شراء {purchased} من {items.Count} عنصر.",
            "حسنًا");
        await ReloadAsync();
    }
}
