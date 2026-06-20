using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Components.StoreSections;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Pages;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Pages;

public partial class GalleryPage : ContentPage
{
    public Components.StoreSections.StoreNavigationState StoreNavigation { get; } = new();

    private GalleryCatalog? _catalog;
    private GallerySeason? _season;
    private List<GalleryItem> _items = new();
    private int _seasonSwitchIndex;
    private AvatarsSectionView? _avatarsSection;
    private BackgroundsSectionView? _backgroundsSection;
    private NewArrivalsSectionView? _newArrivalsFullSection;
    private LimitedOffersSectionView? _limitedOffersFullSection;
    private BrowseCategoriesSectionView? _browseCategoriesFullSection;

    public GalleryPage()
    {
        InitializeComponent();
        _ = LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        _catalog = GalleryService.GetCatalog();

        var currentSeason = GalleryService.GetCurrentSeason();

        if (currentSeason == null)
            return;

        await ApplySeasonAsync(currentSeason);
    }

    public async Task SwitchSeasonAsync(GallerySeason season)
    {
        if (season == null)
            return;

        if (_season?.Id == season.Id)
            return;

        await ApplySeasonAsync(season);
    }

    public async Task SwitchSeasonByIdAsync(string seasonId)
    {
        if (string.IsNullOrWhiteSpace(seasonId))
            return;

        _catalog ??= GalleryService.GetCatalog();

        var season = _catalog
            .Seasons
            .FirstOrDefault(x => x.Id == seasonId);

        if (season == null)
            return;

        await SwitchSeasonAsync(season);
    }

    private async Task ApplySeasonAsync(GallerySeason season)
    {
        _catalog ??= GalleryService.GetCatalog();
        _season = season;

        _items = _catalog?.Items
            .Where(x => x.SeasonId == _season.Id)
            .ToList() ?? new List<GalleryItem>();

        var seasonImage = GetSeasonThemeImage();

        var theme = await GalleryThemeEngine.BuildThemeFromSeasonImageAsync(seasonImage);
        Background = theme.Background;

        HeroSlider.BindSeason(_season);
        BindSections();
    }

    private void BindSections()
    {
        NewArrivalsSection.Bind(Normalize(_items.Where(x => x.IsNew).Take(6).ToList(), 6));
        LimitedOffersSection.Bind(Normalize(_items.Where(x => x.IsLimited).Take(6).ToList(), 6));
        BrowseCategoriesSection.Bind(Normalize(_items.Take(6).ToList(), 6));
    }

    private async void OnQuickActionRequested(object? sender, StoreQuickActionEventArgs e)
    {
        switch (e.Action)
        {
            case StoreQuickAction.WheelOfFortune:
                await ShowPlaceholderAsync("عجلة الحظ", "ميزة عجلة الحظ قيد التجهيز");
                break;

            case StoreQuickAction.DailyOffers:
                CategoriesSection.Select(StoreView.LimitedOffers);
                await SwitchStoreViewAsync(StoreView.LimitedOffers);
                break;

            case StoreQuickAction.TopUp:
                await ShowWalletPlaceholderAsync();
                break;

            case StoreQuickAction.SeasonPass:
                await ShowPlaceholderAsync("بطاقة الموسم", "بطاقة الموسم قيد التجهيز");
                break;
        }
    }

    private async Task ShowWalletPlaceholderAsync()
    {
        var wallet = await PlayerStoreIdentityService.GetDeviceWalletAsync();
        var balance = wallet == null
            ? null
            : $"🪙 العملات: {wallet.Coins}     💎 الجواهر: {wallet.Gems}";

        await ShowPlaceholderAsync("المحفظة", "خيارات الشحن قيد التجهيز", balance);
    }

    private async Task ShowPlaceholderAsync(string title, string message, string? balance = null)
    {
        PlaceholderTitleLabel.Text = title;
        PlaceholderMessageLabel.Text = message;
        PlaceholderBalanceLabel.Text = balance ?? string.Empty;
        PlaceholderBalanceLabel.IsVisible = !string.IsNullOrWhiteSpace(balance);
        PlaceholderOverlay.IsVisible = true;
        PlaceholderOverlay.Opacity = 0;
        await PlaceholderOverlay.FadeToAsync(1, 120);
    }

    private async void OnClosePlaceholderClicked(object? sender, EventArgs e)
    {
        await PlaceholderOverlay.FadeToAsync(0, 100);
        PlaceholderOverlay.IsVisible = false;
    }

    private void OnPlaceholderBackdropTapped(object? sender, TappedEventArgs e)
    {
        // The premium overlay is modal; only its explicit close button dismisses it.
    }

    private async void OnBottomTabRequested(object? sender, StoreBottomTabRequestedEventArgs e)
    {
        BottomNavigation.SelectTab(e.Tab);

        switch (e.Tab)
        {
            case StoreBottomTab.Store:
                CategoriesSection.Select(StoreView.Home);
                await SwitchStoreViewAsync(StoreView.Home);
                break;

            case StoreBottomTab.Offers:
                CategoriesSection.Select(StoreView.LimitedOffers);
                await SwitchStoreViewAsync(StoreView.LimitedOffers);
                break;

            case StoreBottomTab.Rewards:
                await ShowPlaceholderAsync("المكافآت", "قسم المكافآت قيد التجهيز");
                break;

            case StoreBottomTab.Account:
                await OpenAccountAsync();
                break;
        }
    }

    private async Task OpenAccountAsync()
    {
        try
        {
            await Navigation.PushAsync(new PlayerProfilesPage());
        }
        catch (InvalidOperationException)
        {
            await ShowStoreIdentityPlaceholderAsync();
        }
    }

    private async Task ShowStoreIdentityPlaceholderAsync()
    {
        var identity = await HonorIdentityService.LoadAsync();
        var playerName = "غير محدد";
        var role = ResolveRoleLabel(identity.Role);
        var coins = 0;
        var gems = 0;
        var progressText = "0 / 0   0%";

        if (!string.IsNullOrWhiteSpace(identity.PlayerId))
        {
            var profile = await PlayerProfileService.GetPlayerByIdAsync(identity.PlayerId);
            var wallet = await PlayerStoreIdentityService.GetWalletAsync(identity.PlayerId);
            var progress = await PlayerStoreIdentityService.GetCollectionProgressAsync(identity.PlayerId);
            playerName = string.IsNullOrWhiteSpace(profile?.PlayerName) ? "غير محدد" : profile.PlayerName;
            coins = wallet.Coins;
            gems = wallet.Gems;
            var percent = progress.TotalPublished == 0
                ? 0
                : (int)Math.Round(progress.TotalOwned * 100d / progress.TotalPublished);
            progressText = $"{progress.TotalOwned} / {progress.TotalPublished}   {percent}%";
        }

        var details = $"{playerName}\n{role}\n🪙 {coins:N0}     💎 {gems:N0}\nالمقتنيات: {progressText}";
        await ShowPlaceholderAsync("حسابي", "صفحة الحساب قيد الربط", details);
    }

    private static string ResolveRoleLabel(HonorRoleType role) => role switch
    {
        HonorRoleType.Developer => "مطور",
        HonorRoleType.Founder => "مؤسس",
        HonorRoleType.Honor => "عضو شرف",
        _ => "لاعب"
    };

    private async void OnCategorySelected(object? sender, StoreCategorySelectedEventArgs e)
    {
        await SwitchStoreViewAsync(e.View);
    }

    private async void OnNewArrivalsShowAllRequested(object? sender, EventArgs e)
    {
        CategoriesSection.Select(StoreView.Home);
        await SwitchStoreViewAsync(StoreView.NewArrivals);
    }

    private async void OnLimitedOffersShowAllRequested(object? sender, EventArgs e)
    {
        await SwitchStoreViewAsync(StoreView.LimitedOffers);
    }

    private async void OnBrowseCategoriesShowAllRequested(object? sender, EventArgs e)
    {
        CategoriesSection.Select(StoreView.Home);
        await SwitchStoreViewAsync(StoreView.BrowseCategories);
    }

    private async void OnBrowseCategorySelected(object? sender, StoreCategorySelectedEventArgs e)
    {
        CategoriesSection.Select(e.View);
        await SwitchStoreViewAsync(e.View);
    }

    private async Task SwitchStoreViewAsync(StoreView view)
    {
        StoreNavigation.SetAvailableItemCount(0);
        StoreNavigation.SwitchTo(view);
        BottomNavigation.SelectTab(view == StoreView.LimitedOffers
            ? StoreBottomTab.Offers
            : StoreBottomTab.Store);

        if (view == StoreView.Home)
        {
            await SectionContentRoot.FadeToAsync(0, 80);
            SectionContentRoot.IsVisible = false;
            NewArrivalsSection.IsVisible = true;
            LimitedOffersSection.IsVisible = true;
            BrowseCategoriesSection.IsVisible = true;
            HomeTopContent.IsVisible = true;
            HomeContent.IsVisible = true;
            HomeTopContent.Opacity = 0;
            HomeContent.Opacity = 0;
            await Task.WhenAll(
                HomeTopContent.FadeToAsync(1, 100),
                HomeContent.FadeToAsync(1, 100));
            return;
        }

        HomeTopContent.IsVisible = false;
        HomeContent.IsVisible = false;
        ConfigureSelectedSection(view);
        SectionContentRoot.IsVisible = true;
        SectionContentRoot.Opacity = 0;
        await SectionContentRoot.FadeToAsync(1, 120);
    }

    private void ConfigureSelectedSection(StoreView view)
    {
        SectionMessageCard.IsVisible = false;
        SelectedSectionHost.IsVisible = true;
        ShowMoreButton.IsVisible = false;

        switch (view)
        {
            case StoreView.NewArrivals:
                _newArrivalsFullSection ??= CreateNewArrivalsSection();
                SelectedSectionHost.Content = _newArrivalsFullSection;
                break;
            case StoreView.LimitedOffers:
                _limitedOffersFullSection ??= CreateLimitedOffersSection();
                SelectedSectionHost.Content = _limitedOffersFullSection;
                break;
            case StoreView.BrowseCategories:
                _browseCategoriesFullSection ??= CreateBrowseCategoriesSection();
                SelectedSectionHost.Content = _browseCategoriesFullSection;
                break;
            case StoreView.Avatars:
                _avatarsSection ??= CreateAvatarsSection();
                SelectedSectionHost.Content = _avatarsSection;
                break;
            case StoreView.Backgrounds:
                _backgroundsSection ??= CreateBackgroundsSection();
                SelectedSectionHost.Content = _backgroundsSection;
                break;
            default:
                SelectedSectionHost.Content = null;
                SelectedSectionHost.IsVisible = false;
                ShowSectionMessage($"قسم {GetCategoryName(view)} قيد التجهيز");
                break;
        }
    }

    private NewArrivalsSectionView CreateNewArrivalsSection()
    {
        var section = new NewArrivalsSectionView();
        section.SetVisibleItemCount(StoreNavigationState.PageSize);
        section.AvailableItemCountChanged += (_, count) => OnAvailableItemCountChanged(StoreView.NewArrivals, count);
        return section;
    }

    private LimitedOffersSectionView CreateLimitedOffersSection()
    {
        var section = new LimitedOffersSectionView();
        section.SetVisibleItemCount(StoreNavigationState.PageSize);
        section.AvailableItemCountChanged += (_, count) => OnAvailableItemCountChanged(StoreView.LimitedOffers, count);
        return section;
    }

    private BrowseCategoriesSectionView CreateBrowseCategoriesSection()
    {
        var section = new BrowseCategoriesSectionView();
        section.ShowAllCategories();
        section.AvailableItemCountChanged += (_, count) => OnAvailableItemCountChanged(StoreView.BrowseCategories, count);
        section.CategorySelected += OnBrowseCategorySelected;
        return section;
    }

    private AvatarsSectionView CreateAvatarsSection()
    {
        var section = new AvatarsSectionView();
        section.AvailableItemCountChanged += (_, count) => OnAvailableItemCountChanged(StoreView.Avatars, count);
        return section;
    }

    private BackgroundsSectionView CreateBackgroundsSection()
    {
        var section = new BackgroundsSectionView();
        section.AvailableItemCountChanged += (_, count) => OnAvailableItemCountChanged(StoreView.Backgrounds, count);
        return section;
    }

    private void OnAvailableItemCountChanged(StoreView view, int count)
    {
        if (StoreNavigation.CurrentView != view)
            return;

        StoreNavigation.SetAvailableItemCount(count);
        var hasItems = count > 0;
        SelectedSectionHost.IsVisible = hasItems;
        SectionMessageCard.IsVisible = !hasItems;
        SectionMessageLabel.Text = hasItems ? string.Empty : StoreNavigationState.EmptyStateMessage;
        ShowMoreButton.IsVisible = StoreNavigation.CanShowMore;
    }

    private void OnShowMoreClicked(object? sender, EventArgs e)
    {
        StoreNavigation.ShowMore();

        switch (StoreNavigation.CurrentView)
        {
            case StoreView.NewArrivals:
                _newArrivalsFullSection?.SetVisibleItemCount(StoreNavigation.VisibleItemCount);
                break;
            case StoreView.LimitedOffers:
                _limitedOffersFullSection?.SetVisibleItemCount(StoreNavigation.VisibleItemCount);
                break;
            case StoreView.Avatars:
                _avatarsSection?.SetVisibleItemCount(StoreNavigation.VisibleItemCount);
                break;
            case StoreView.Backgrounds:
                _backgroundsSection?.SetVisibleItemCount(StoreNavigation.VisibleItemCount);
                break;
        }

        ShowMoreButton.IsVisible = StoreNavigation.CanShowMore;
    }

    private void ShowSectionMessage(string message)
    {
        SectionMessageLabel.Text = message;
        SectionMessageCard.IsVisible = true;
    }

    private static string GetCategoryName(StoreView view) => view switch
    {
        StoreView.Frames => "الإطارات",
        StoreView.Titles => "الألقاب",
        StoreView.Badges => "الشارات",
        StoreView.Effects => "المؤثرات",
        StoreView.Bundles => "الحزم",
        _ => string.Empty
    };

    private async void OnSeasonSwitchTestClicked(object? sender, EventArgs e)
    {
        await SwitchToNextSeasonForTestAsync();
    }

    private async void OnCartRequested(object? sender, EventArgs e)
    {
        await DisplayAlert("سلة الشراء", "سيتم فتح سلة الشراء لاحقًا.", "حسنًا");
    }

    private async void OnCoinsRequested(object? sender, EventArgs e)
    {
        await DisplayAlert("العملات", "سيتم فتح صفحة شحن العملات لاحقًا.", "حسنًا");
    }

    private async void OnGemsRequested(object? sender, EventArgs e)
    {
        await DisplayAlert("الجواهر", "سيتم فتح صفحة شحن الجواهر لاحقًا.", "حسنًا");
    }

    private async void OnIdentityRequested(object? sender, EventArgs e)
    {
        await DisplayAlert("هوية اللاعب", "سيتم ربط الهوية بملف اللاعب لاحقًا.", "حسنًا");
    }

    // TEMP DEV: Season switch test control. Remove before production.
    private async Task SwitchToNextSeasonForTestAsync()
    {
        _catalog ??= GalleryService.GetCatalog();

        var seasons = _catalog
            .Seasons
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToList();

        if (seasons.Count <= 1)
        {
            await DisplayAlert(
                "تنبيه",
                "لا توجد مواسم أخرى للتجربة",
                "حسنًا");

            return;
        }

        var currentIndex = seasons.FindIndex(x => x.Id == _season?.Id);

        _seasonSwitchIndex =
            currentIndex >= 0
                ? (currentIndex + 1) % seasons.Count
                : (_seasonSwitchIndex + 1) % seasons.Count;

        await SwitchSeasonAsync(seasons[_seasonSwitchIndex]);
    }

    private string GetSeasonThemeImage()
    {
        if (_season != null)
        {
            var seasonType = _season.GetType();

            string[] possibleNames =
            {
                "Image",
                "HeroImage",
                "BannerImage",
                "BackgroundImage",
                "SeasonImage",
                "ThemeImage"
            };

            foreach (var name in possibleNames)
            {
                var property = seasonType.GetProperty(name);
                var value = property?.GetValue(_season)?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        var firstItemImage = _items
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Image))
            ?.Image;

        return string.IsNullOrWhiteSpace(firstItemImage)
            ? "gallery_lion.png"
            : firstItemImage;
    }

    private List<GalleryItem> Normalize(List<GalleryItem> source, int count)
    {
        var result = source.ToList();
        var fallback = _items.FirstOrDefault();

        while (result.Count < count && fallback != null)
        {
            result.Add(fallback);
        }

        return result.Take(count).ToList();
    }
}
