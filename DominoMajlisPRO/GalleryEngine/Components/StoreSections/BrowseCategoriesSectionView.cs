using DominoMajlisPRO.GalleryEngine.Models;

using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class BrowseCategoriesSectionView : StoreProductsSectionBase
{
    private IReadOnlyList<CategoryCard> _availableCategories = Array.Empty<CategoryCard>();
    private bool _showAll;

    public event EventHandler? ShowAllRequested;
    public event EventHandler<StoreCategorySelectedEventArgs>? CategorySelected;
    public event EventHandler<int>? AvailableItemCountChanged;

    public BrowseCategoriesSectionView()
        : base("تصفح الفئات", "BROWSE CATEGORIES", "عرض الكل")
    {
        AttachShowAllTap();
        Loaded += OnCmsLoaded;
        Unloaded += OnCmsUnloaded;
    }

    public new void Bind(List<GalleryItem> items)
    {
        _ = items;
        _ = RefreshFromCmsAsync();
    }

    private void OnCmsLoaded(object? sender, EventArgs e)
    {
        Admin.Services.StoreCategoriesAdminService.PublishedChanged -= OnPublishedChanged;
        Admin.Services.StoreCategoriesAdminService.PublishedChanged += OnPublishedChanged;
        _ = RefreshFromCmsAsync();
    }

    private void OnCmsUnloaded(object? sender, EventArgs e) => Admin.Services.StoreCategoriesAdminService.PublishedChanged -= OnPublishedChanged;
    private void OnPublishedChanged() => _ = RefreshFromCmsAsync();

    private async Task RefreshFromCmsAsync()
    {
        var published = await StoreAssetQueryService.LoadCategoriesAsync();
        var categories = published
            .Select(record => new CategoryCard(ToGalleryItem(record), ResolveView(record)))
            .Where(category => category.View != null)
            .ToList();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _availableCategories = categories;
            IsVisible = categories.Count > 0;
            AvailableItemCountChanged?.Invoke(this, categories.Count);
            BuildTappableCards(_showAll ? categories : categories.Take(6).ToList());
        });
    }

    public void ShowAllCategories()
    {
        _showAll = true;
        BuildTappableCards(_availableCategories);
    }

    private void AttachShowAllTap()
    {
        if (Content is not VerticalStackLayout { Children.Count: > 0 } section || section.Children[0] is not Grid header || header.Children.FirstOrDefault() is not Label actionLabel)
            return;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => ShowAllRequested?.Invoke(this, EventArgs.Empty);
        actionLabel.GestureRecognizers.Add(tap);
    }

    private void BuildTappableCards(IReadOnlyList<CategoryCard> categories)
    {
        if (Content is not VerticalStackLayout section || section.Children.Count < 2 || section.Children[1] is not Grid grid)
            return;

        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();
        for (var column = 0; column < 3; column++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (var row = 0; row < Math.Ceiling(categories.Count / 3d); row++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var index = 0; index < categories.Count; index++)
        {
            var category = categories[index];
            var card = new PremiumGalleryCard();
            card.Bind(category.Item);
            if (category.View is StoreView view)
                AttachCardTap(card, () => CategorySelected?.Invoke(this, new StoreCategorySelectedEventArgs(view)));
            grid.Add(card, index % 3, index / 3);
        }
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


    private static GalleryItem ToGalleryItem(Admin.Models.StoreCategoryRecord record) => new()
    {
        Id = record.Id,
        Name = string.IsNullOrWhiteSpace(record.Collection) ? (string.IsNullOrWhiteSpace(record.NameAr) ? record.NameEn : record.NameAr) : record.Collection,
        Subtitle = record.Category,
        Description = record.Description,
        Category = record.Category,
        Image = string.IsNullOrWhiteSpace(record.BannerPath) ? record.IconPath : record.BannerPath,
        Price = 0,
        Currency = "Free"
    };

    private static StoreView? ResolveView(Admin.Models.StoreCategoryRecord record) =>
        StoreTypeRegistry.Resolve(record.Category, record.NameEn, record.NameAr)?.TargetView;

    private sealed record CategoryCard(GalleryItem Item, StoreView? View);
}
