using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Components.StoreSections;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Pages;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.Features.RechargeCenter.Services;

namespace DominoMajlisPRO.GalleryEngine.Pages;

public partial class GalleryPage : ContentPage
{
    public Components.StoreSections.StoreNavigationState StoreNavigation { get; } = new();

    private GalleryCatalog? _catalog;
    private GallerySeason? _season;
    private List<GalleryItem> _items = new();
    private AvatarsSectionView? _avatarsSection;
    private BackgroundsSectionView? _backgroundsSection;
    private NewArrivalsSectionView? _newArrivalsFullSection;
    private NewArrivalsSectionView? _framesSection;
    private NewArrivalsSectionView? _effectsSection;
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

        var currentSeason = await StoreAssetQueryService.LoadCurrentSeasonAsync();

        if (currentSeason == null)
        {
            ShowNoPublishedSeason();
            BindSections();
            return;
        }

        await ApplySeasonAsync(ToGallerySeason(currentSeason));
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

        _items = new List<GalleryItem>();

        var seasonImage = GetSeasonThemeImage();

        var theme = await GalleryThemeEngine.BuildThemeFromSeasonImageAsync(seasonImage);
        Background = theme.Background;

        HeroSlider.IsVisible = true;
        SeasonEmptyCard.IsVisible = false;
        HeroSlider.BindSeason(_season);
        BindSections();
    }

    private void ShowNoPublishedSeason()
    {
        _season = null;
        _items = new List<GalleryItem>();
        Background = GalleryThemeEngine.Current.Background;
        HeroSlider.IsVisible = false;
        SeasonEmptyCard.IsVisible = true;
        SeasonEmptyLabel.Text = "لم يتم نشر موسم حالياً";
    }

    private static GallerySeason ToGallerySeason(CurrentSeasonRecord record)
    {
        var identity = CurrentSeasonAdminService.GetIdentity(record);
        return new GallerySeason
        {
            Id = string.IsNullOrWhiteSpace(identity) ? record.Id : identity,
            Title = record.Title,
            Subtitle = record.Subtitle,
            Description = record.Description,
            ButtonText = string.IsNullOrWhiteSpace(record.ButtonText)
                ? "عرض التفاصيل"
                : record.ButtonText.Trim(),
            BackgroundImage = record.ImagePath,
            CharacterImage = record.ImagePath,
            StartDate = record.StartsAt ?? DateTime.Now,
            EndDate = record.EndsAt ?? DateTime.Now.AddDays(30)
        };
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
                await Navigation.PushAsync(new WheelOfFortunePage());
                break;

            case StoreQuickAction.DailyOffers:
                CategoriesSection.Select(StoreView.LimitedOffers);
                await SwitchStoreViewAsync(StoreView.LimitedOffers);
                break;

            case StoreQuickAction.TopUp:
                await RechargeNavigationService.OpenAsync(Navigation);
                break;

            case StoreQuickAction.SeasonPass:
                break;
        }
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
            await Shell.Current.Navigation.PushAsync(new PlayerProfilesPage());
        }
    }

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
            case StoreView.Frames:
                _framesSection ??= CreateFramesSection();
                SelectedSectionHost.Content = _framesSection;
                break;
            case StoreView.Effects:
                _effectsSection ??= CreateEffectsSection();
                SelectedSectionHost.Content = _effectsSection;
                break;
            default:
                SelectedSectionHost.Content = null;
                SelectedSectionHost.IsVisible = false;
                ShowSectionMessage(StoreNavigationState.EmptyStateMessage);
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

    private NewArrivalsSectionView CreateEffectsSection()
    {
        var section = new NewArrivalsSectionView(
            "المؤثرات",
            "EFFECTS",
            StoreProductAssetType.Effect.ToString());
        section.SetVisibleItemCount(StoreNavigationState.PageSize);
        section.AvailableItemCountChanged += (_, count) =>
            OnAvailableItemCountChanged(StoreView.Effects, count);
        return section;
    }

    private NewArrivalsSectionView CreateFramesSection()
    {
        var section = new NewArrivalsSectionView(
            "الإطارات",
            "FRAMES",
            StoreProductAssetType.Frame.ToString());
        section.SetVisibleItemCount(StoreNavigationState.PageSize);
        section.AvailableItemCountChanged += (_, count) =>
            OnAvailableItemCountChanged(StoreView.Frames, count);
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
            case StoreView.Frames:
                _framesSection?.SetVisibleItemCount(StoreNavigation.VisibleItemCount);
                break;
            case StoreView.Effects:
                _effectsSection?.SetVisibleItemCount(StoreNavigation.VisibleItemCount);
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

    private async void OnSeasonSwitchRequested(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new CurrentSeasonEditorPage());
    }

    private async void OnCoinsRequested(object? sender, EventArgs e)
    {
        await RechargeNavigationService.OpenAsync(Navigation);
    }

    private async void OnGemsRequested(object? sender, EventArgs e)
    {
        await RechargeNavigationService.OpenAsync(Navigation);
    }

    private async void OnIdentityRequested(object? sender, EventArgs e)
    {
        await OpenAccountAsync();
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
