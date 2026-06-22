using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class LimitedOffersSectionView : StoreProductsSectionBase
{
    private IReadOnlyList<LimitedOfferCard> _availableItems =
        Array.Empty<LimitedOfferCard>();
    private readonly StoreProductActionSheet _actionSheet = new();
    private int _visibleItemCount = 6;

    public event EventHandler? ShowAllRequested;
    public event EventHandler<int>? AvailableItemCountChanged;

    public LimitedOffersSectionView() : base("عرض لفترة محدودة", "LIMITED OFFERS", "عرض الكل")
    {
        AttachShowAllTap();
        Loaded += OnCmsLoaded; Unloaded += OnCmsUnloaded;
    }

    public new void Bind(List<GalleryItem> items) { _ = items; _ = RefreshAsync(); }
    public void SetVisibleItemCount(int count) { _visibleItemCount = Math.Max(0, count); BuildTappableCards(_availableItems.Take(_visibleItemCount).ToList()); }
    private void OnCmsLoaded(object? sender, EventArgs e) { LimitedOffersAdminService.PublishedChanged -= OnPublishedChanged; LimitedOffersAdminService.PublishedChanged += OnPublishedChanged; _ = RefreshAsync(); }
    private void OnCmsUnloaded(object? sender, EventArgs e) => LimitedOffersAdminService.PublishedChanged -= OnPublishedChanged;
    private void OnPublishedChanged() => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        var offers = await StoreAssetQueryService.LoadActiveOffersAsync();
        var items = new List<LimitedOfferCard>(offers.Count);
        foreach (var record in offers)
        {
            var productId = string.IsNullOrWhiteSpace(record.ProductId) ? record.Id.Trim() : record.ProductId.Trim();
            var assetId = LimitedOffersAdminService.GetAssetId(record).Trim();
            var storeTypeId =
                await StoreInventoryRouteResolver.ResolveStoreTypeIdAsync(
                    assetId,
                    record.StoreTypeId);
            var isFree = record.DiscountPrice == 0 ||
                record.CurrencyType == LimitedOfferCurrencyType.Free;
            items.Add(new LimitedOfferCard(
                ToGalleryItem(record),
                productId,
                assetId,
                storeTypeId,
                record.DiscountPrice,
                isFree,
                record.CurrencyType.ToString()));
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _availableItems = items;
            AvailableItemCountChanged?.Invoke(this, items.Count);
            BuildTappableCards(items.Take(_visibleItemCount).ToList());
        });
    }

    private void AttachShowAllTap()
    {
        if (Content is not VerticalStackLayout { Children.Count: > 0 } section || section.Children[0] is not Grid header || header.Children.FirstOrDefault() is not Label actionLabel)
            return;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => ShowAllRequested?.Invoke(this, EventArgs.Empty);
        actionLabel.GestureRecognizers.Add(tap);
    }

    private void BuildTappableCards(IReadOnlyList<LimitedOfferCard> items)
    {
        if (Content is not VerticalStackLayout section || section.Children.Count < 2 || section.Children[1] is not Grid grid)
            return;

        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        for (var column = 0; column < 3; column++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (var row = 0; row < Math.Ceiling(items.Count / 3d); row++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var index = 0; index < items.Count; index++)
        {
            var cardItem = items[index];
            var card = new PremiumGalleryCard();
            card.Bind(cardItem.Item);
            AttachCardTap(card, () => OpenActionSheet(cardItem));
            grid.Add(card, index % 3, index / 3);
        }
    }

    private void OpenActionSheet(LimitedOfferCard card)
    {
        var item = card.Item;
        var isFree = card.IsFree || item.Price == 0;
        var price = isFree ? "مجاني" : string.Equals(item.Currency, "Coins", StringComparison.OrdinalIgnoreCase) ? $"🪙 {item.Price}" : $"💎 {item.Price}";
        _actionSheet.Show(
            this,
            item.Image,
            item.Name,
            "عرض محدود",
            item.Description,
            price,
            "غير مملوك",
            "اقتناء",
            true,
            () => Task.CompletedTask,
            () => Task.CompletedTask,
            previewKind: ResolvePreviewKind(card.StoreTypeId, card.AssetId, item.Name, item.Description),
            inventoryAssetId: card.AssetId,
            inventoryStoreTypeId: card.StoreTypeId,
            inventoryIsFree: isFree,
            inventoryProductId: card.ProductId,
            inventoryPrice: card.Price,
            inventoryCurrencyMetadata: card.CurrencyMetadata);
    }

    private static StoreProductPreviewKind ResolvePreviewKind(
        string storeTypeId,
        string assetId,
        string name,
        string description)
    {
        var canonical = StoreAssetCatalogService.CanonicalTypeId(storeTypeId);
        if (string.Equals(canonical, "Effect", StringComparison.OrdinalIgnoreCase))
            return StoreProductPreviewKind.Effect;

        var key = $"{storeTypeId} {assetId} {name} {description}".ToLowerInvariant();
        return key.Contains("effect") ||
               key.Contains("effact") ||
               key.Contains("تأثير") ||
               key.Contains("تاثير")
            ? StoreProductPreviewKind.Effect
            : StoreProductPreviewKind.Generic;
    }

    private static void AttachCardTap(PremiumGalleryCard card, Action action)
    {
        var cardTap = new TapGestureRecognizer();
        cardTap.Tapped += (_, _) => action();
        card.GestureRecognizers.Add(cardTap);
        if (card.Content is View root)
        {
            var rootTap = new TapGestureRecognizer();
            rootTap.Tapped += (_, _) => action();
            root.GestureRecognizers.Add(rootTap);
        }
    }

    private static GalleryItem ToGalleryItem(LimitedOfferRecord record) => new()
    {
        Id = LimitedOffersAdminService.GetAssetId(record), Name = record.Title, Subtitle = record.Subtitle, Description = record.Description,
        Category = record.Category, Image = record.ImagePath, Price = record.CurrencyType == LimitedOfferCurrencyType.Free ? 0 : record.DiscountPrice,
        OldPrice = record.CurrencyType == LimitedOfferCurrencyType.Free ? null : record.OriginalPrice, Currency = record.CurrencyType.ToString(),
        IsLimited = true, LimitedUntil = record.EndsAt
    };

    private sealed record LimitedOfferCard(
        GalleryItem Item,
        string ProductId,
        string AssetId,
        string StoreTypeId,
        int Price,
        bool IsFree,
        string CurrencyMetadata);
}

