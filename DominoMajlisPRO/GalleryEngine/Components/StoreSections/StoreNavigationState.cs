using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public enum StoreView
{
    Home,
    BrowseCategories,
    NewArrivals,
    LimitedOffers,
    Avatars,
    Backgrounds,
    Frames,
    Titles,
    Badges,
    Effects,
    Bundles,
    Rewards,
    Wallet,
    SeasonPass
}

public sealed class StoreNavigationState : INotifyPropertyChanged
{
    public const int PageSize = 12;
    public const string EmptyStateMessage = "لا توجد عناصر منشورة حالياً";

    private StoreView _currentView = StoreView.Home;
    private StoreView? _selectedCategory;
    private int _visibleItemCount;
    private int _availableItemCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public StoreView CurrentView => _currentView;
    public StoreView? SelectedCategory => _selectedCategory;
    public int VisibleItemCount => _visibleItemCount;
    public bool CanShowMore => _currentView is not (StoreView.Home or StoreView.BrowseCategories) && _visibleItemCount < _availableItemCount;

    public void SwitchTo(StoreView view)
    {
        _currentView = view;
        _selectedCategory = IsProductSection(view) ? view : null;
        _visibleItemCount = view == StoreView.Home
            ? 0
            : Math.Min(PageSize, _availableItemCount);

        NotifyStateChanged();
    }

    public void ShowMore()
    {
        if (!CanShowMore)
            return;

        _visibleItemCount = Math.Min(_visibleItemCount + PageSize, _availableItemCount);
        OnPropertyChanged(nameof(VisibleItemCount));
        OnPropertyChanged(nameof(CanShowMore));
    }

    // Callers pass the count after applying their Published + Visible filters.
    public void SetAvailableItemCount(int publishedVisibleItemCount)
    {
        _availableItemCount = Math.Max(0, publishedVisibleItemCount);
        _visibleItemCount = _currentView == StoreView.Home
            ? 0
            : _visibleItemCount == 0
                ? Math.Min(PageSize, _availableItemCount)
                : Math.Min(_visibleItemCount, _availableItemCount);

        OnPropertyChanged(nameof(VisibleItemCount));
        OnPropertyChanged(nameof(CanShowMore));
    }

    public void SetAvailableItems<T>(
        IEnumerable<T> items,
        Func<T, bool> isPublished,
        Func<T, bool> isVisible)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(isPublished);
        ArgumentNullException.ThrowIfNull(isVisible);

        SetAvailableItemCount(items.Count(item => isPublished(item) && isVisible(item)));
    }

    private static bool IsProductSection(StoreView view) =>
        view == StoreView.NewArrivals || StoreTypeRegistry.IsPurchasableView(view);

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CurrentView));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(VisibleItemCount));
        OnPropertyChanged(nameof(CanShowMore));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
