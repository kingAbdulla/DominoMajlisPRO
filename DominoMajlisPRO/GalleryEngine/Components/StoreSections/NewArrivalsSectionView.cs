using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class NewArrivalsSectionView : StoreProductsSectionBase
{
    private IReadOnlyList<NewArrivalCard> _availableItems =
        Array.Empty<NewArrivalCard>();
    private readonly StoreProductActionSheet _actionSheet = new();
    private readonly string? _storeTypeFilter;
    private int _visibleItemCount = 6;

    public event EventHandler? ShowAllRequested;
    public event EventHandler<int>? AvailableItemCountChanged;

    public NewArrivalsSectionView()
        : this("وصل حديثاً", "NEW ARRIVALS", null)
    {
    }

    public NewArrivalsSectionView(
        string title,
        string subtitle,
        string? storeTypeFilter)
        : base(title, subtitle, "عرض الكل")
    {
        _storeTypeFilter = storeTypeFilter;
        AttachShowAllTap();
        Loaded += OnCmsLoaded;
        Unloaded += OnCmsUnloaded;
    }

    public new void Bind(List<GalleryItem> items)
    {
        _ = items;
        _ = RefreshFromCmsAsync();
    }

    public void SetVisibleItemCount(int count)
    {
        _visibleItemCount = Math.Max(0, count);
        BuildTappableCards(_availableItems.Take(_visibleItemCount).ToList());
    }

    private void OnCmsLoaded(object? sender, EventArgs e)
    {
        NewArrivalsAdminService.PublishedChanged -= OnPublishedChanged;
        NewArrivalsAdminService.PublishedChanged += OnPublishedChanged;
        _ = RefreshFromCmsAsync();
    }

    private void OnCmsUnloaded(object? sender, EventArgs e)
    {
        NewArrivalsAdminService.PublishedChanged -= OnPublishedChanged;
    }

    private void OnPublishedChanged()
    {
        _ = RefreshFromCmsAsync();
    }

    private async Task RefreshFromCmsAsync()
    {
        var published = await StoreAssetQueryService.LoadNewArrivalsAsync();
        var scoped = string.IsNullOrWhiteSpace(_storeTypeFilter)
            ? published
            : published
                .Where(record => string.Equals(
                    StoreAssetCatalogService.CanonicalTypeId(record.StoreTypeId),
                    _storeTypeFilter,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        var items = new List<NewArrivalCard>(scoped.Count);
        foreach (var record in scoped)
        {
            var productId = string.IsNullOrWhiteSpace(record.ProductId) ? record.Id.Trim() : record.ProductId.Trim();
            var assetId = NewArrivalsAdminService.GetAssetId(record).Trim();
            var storeTypeId =
                await StoreInventoryRouteResolver.ResolveStoreTypeIdAsync(
                    assetId,
                    record.StoreTypeId);
            var isFree = record.Price == 0 ||
                record.CurrencyType == NewArrivalCurrencyType.Free;
            items.Add(new NewArrivalCard(
                ToGalleryItem(record),
                productId,
                assetId,
                storeTypeId,
                record.Price,
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
        if (Content is not VerticalStackLayout { Children.Count: > 0 } section ||
            section.Children[0] is not Grid header ||
            header.Children.FirstOrDefault() is not Label actionLabel)
            return;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => ShowAllRequested?.Invoke(this, EventArgs.Empty);
        actionLabel.GestureRecognizers.Add(tap);
    }

    private void BuildTappableCards(IReadOnlyList<NewArrivalCard> items)
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

    private void OpenActionSheet(NewArrivalCard card)
    {
        var item = card.Item;
        var isFree = card.IsFree || item.Price == 0;
        var price = isFree ? "مجاني" : string.Equals(item.Currency, "Coins", StringComparison.OrdinalIgnoreCase) ? $"🪙 {item.Price}" : $"💎 {item.Price}";
        _actionSheet.Show(this, item.Image, item.Name, "وصل حديثاً", item.Description, price, "غير مملوك",
            "اقتناء", true, () => Task.CompletedTask, () => Task.CompletedTask,
            previewKind: ResolvePreviewKind(card.StoreTypeId),
            inventoryAssetId: card.AssetId,
            inventoryStoreTypeId: card.StoreTypeId,
            inventoryIsFree: isFree,
            inventoryProductId: card.ProductId,
            inventoryPrice: card.Price,
            inventoryCurrencyMetadata: card.CurrencyMetadata);
    }

    private static StoreProductPreviewKind ResolvePreviewKind(
        string storeTypeId)
    {
        var canonicalType =
            StoreAssetCatalogService.CanonicalTypeId(storeTypeId);
        if (string.Equals(
                canonicalType,
                StoreProductAssetType.Frame.ToString(),
                StringComparison.OrdinalIgnoreCase))
        {
            return StoreProductPreviewKind.Frame;
        }

        return string.Equals(
            canonicalType,
            StoreProductAssetType.Effect.ToString(),
            StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    canonicalType,
                    StoreProductAssetType.TeamEffect.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                ? StoreProductPreviewKind.Effect
                : StoreProductPreviewKind.Generic;
    }

    private static void AttachCardTap(PremiumGalleryCard card, Action action)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => action();
        card.GestureRecognizers.Add(tap);
    }

    private static GalleryItem ToGalleryItem(NewArrivalRecord record)
    {
        return new GalleryItem
        {
            Id = NewArrivalsAdminService.GetAssetId(record),
            Name = record.Title,
            Subtitle = record.Subtitle,
            Description = record.Description,
            Category = record.Category,
            Image = record.ImagePath,
            Price = record.CurrencyType == NewArrivalCurrencyType.Free ? 0 : record.Price,
            Currency = record.CurrencyType.ToString(),
            IsNew = true
        };
    }

    private sealed record NewArrivalCard(
        GalleryItem Item,
        string ProductId,
        string AssetId,
        string StoreTypeId,
        int Price,
        bool IsFree,
        string CurrencyMetadata);
}
