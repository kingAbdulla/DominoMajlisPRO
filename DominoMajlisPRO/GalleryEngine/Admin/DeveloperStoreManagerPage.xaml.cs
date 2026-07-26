using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.GalleryEngine.Admin;

public partial class DeveloperStoreManagerPage : ContentPage
{
    private readonly List<AdminSectionCardTarget> _cardTargets = new();

    public DeveloperStoreManagerPage()
    {
        InitializeComponent();
        FlowDirection = FlowDirection.RightToLeft;

        BuildSections();
        ApplyTheme();
        IsVisible = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await DeveloperAuthorizationGuard.EnsurePageAuthorizedAsync(this, nameof(DeveloperStoreManagerPage)))
            return;

        IsVisible = true;
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    protected override void OnDisappearing()
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }

    private void BuildSections()
    {
        SectionsContainer.Children.Clear();
        _cardTargets.Clear();

        foreach (var section in StoreAdminService.GetSections().OrderBy(x => x.SortOrder))
            SectionsContainer.Children.Add(CreateSectionCard(section));
    }

    private View CreateSectionCard(StoreAdminSection section)
    {
        var theme = GalleryThemeEngine.Current;
        var iconLabel = new Label
        {
            Text = section.Icon,
            FontSize = 24,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = theme.Gold
        };

        var titleLabel = new Label
        {
            Text = section.Title,
            FontFamily = "Tajawal-Regular",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.End,
            TextColor = theme.TextPrimary
        };

        var subtitleLabel = new Label
        {
            Text = section.Subtitle,
            FontFamily = "Tajawal-Regular",
            FontSize = 10.5,
            MaxLines = 2,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.End,
            TextColor = theme.TextSecondary
        };

        var metaLabel = new Label
        {
            Text = BuildMetaText(section),
            FontFamily = "Tajawal-Regular",
            FontSize = 9.5,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.End,
            TextColor = theme.TextMuted
        };

        var textStack = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children = { titleLabel, subtitleLabel, metaLabel }
        };

        var grid = new Grid
        {
            FlowDirection = FlowDirection.RightToLeft,
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        grid.Add(iconLabel, 0, 0);
        grid.Add(textStack, 1, 0);

        var card = new Border
        {
            Padding = new Thickness(12, 10),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Background = theme.CardBackground,
            Stroke = theme.Stroke,
            Content = grid,
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 170 : 220,
            MinimumHeightRequest = 92,
            Margin = new Thickness(0, 0, 0, 10)
        };

        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await OpenSectionAsync(section))
        });

        _cardTargets.Add(new AdminSectionCardTarget(card, iconLabel, titleLabel, subtitleLabel, metaLabel));
        return card;
    }

    private static string BuildMetaText(StoreAdminSection section)
    {
        var ratio = section.ImageRule.AspectRatio <= 0
            ? "مرن"
            : section.ImageRule.AspectRatio.ToString("0.##");

        return $"{section.TemplateType} • صورة {ratio} • نص محدود";
    }

    private async Task OpenSectionAsync(StoreAdminSection section)
    {
        if (!await DeveloperAuthorizationGuard.IsAuthorizedAsync(
                nameof(DeveloperStoreManagerPage),
                $"Open {section.Id}"))
            return;

        switch (section.Id)
        {
            case "inventory-audit":
                await NavigationGuardService.PushOnceAsync(Navigation, new InventoryAuditPage());
                return;
            case "current-season":
                await NavigationGuardService.PushOnceAsync(Navigation, new CurrentSeasonEditorPage());
                return;
            case "new-arrivals":
                await NavigationGuardService.PushOnceAsync(Navigation, new NewArrivalsEditorPage());
                return;
            case "limited-offers":
                await NavigationGuardService.PushOnceAsync(Navigation, new LimitedOffersEditorPage());
                return;
            case "categories":
                await NavigationGuardService.PushOnceAsync(Navigation, new StoreCategoriesEditorPage());
                return;
            case "avatars":
                await NavigationGuardService.PushOnceAsync(Navigation, new AvatarsEditorPage());
                return;
            case "backgrounds":
                await NavigationGuardService.PushOnceAsync(Navigation, new BackgroundsEditorPage());
                return;
            case "emblems":
                await NavigationGuardService.PushOnceAsync(Navigation, new EmblemsManagerPage());
                return;
            case "emblem-backgrounds":
                await NavigationGuardService.PushOnceAsync(Navigation, new EmblemBackgroundsManagerPage());
                return;
            case "team-colors":
                await NavigationGuardService.PushOnceAsync(Navigation, new TeamColorsManagerPage());
                return;
            case "effects":
                await NavigationGuardService.PushOnceAsync(Navigation, new EffectsManagerPage());
                return;
            case "typography":
            case "name-effects":
                await NavigationGuardService.PushOnceAsync(Navigation, new TypographyManagerPage());
                return;
            case "frames":
                await NavigationGuardService.PushOnceAsync(Navigation, new FramesManagerPage());
                return;
            case "titles":
                await NavigationGuardService.PushOnceAsync(Navigation, new TitlesManagerPage());
                return;
            case "bundles":
                await NavigationGuardService.PushOnceAsync(Navigation, new BundlesManagerPage());
                return;
            case "currency-pricing":
                await NavigationGuardService.PushOnceAsync(Navigation, new CurrencyPricingManagerPage());
                return;
            case "product-cards":
                await NavigationGuardService.PushOnceAsync(Navigation, new NewArrivalsEditorPage());
                return;
            case "category-cards":
                await NavigationGuardService.PushOnceAsync(Navigation, new StoreCategoriesEditorPage());
                return;
            case "store-settings":
                await NavigationGuardService.PushOnceAsync(Navigation, new StoreSettingsManagerPage());
                return;
        }

        await DisplayAlert(section.Title, "سيتم بناء هذا القسم في المرحلة التالية", "حسناً");
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;

        BackButtonFrame.Background = theme.CardBackground;
        BackButtonFrame.Stroke = theme.Stroke;
        BackButtonLabel.TextColor = theme.Gold;
        TitleLabel.TextColor = theme.TextPrimary;
        SubtitleLabel.TextColor = theme.TextSecondary;
        InfoPanel.Background = theme.ActionBackground;
        InfoPanel.Stroke = theme.Stroke;
        InfoLabel.TextColor = theme.TextSecondary;

        foreach (var target in _cardTargets)
        {
            target.Card.Background = theme.CardBackground;
            target.Card.Stroke = theme.Stroke;
            target.Card.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(theme.Glow),
                Radius = 12,
                Opacity = 0.18f,
                Offset = new Point(0, 3)
            };
            target.IconLabel.TextColor = theme.Gold;
            target.TitleLabel.TextColor = theme.TextPrimary;
            target.SubtitleLabel.TextColor = theme.TextSecondary;
            target.MetaLabel.TextColor = theme.TextMuted;
        }
    }

    private void OnThemeChanged(object? sender, GalleryTheme theme)
    {
        ApplyTheme();
    }

    private sealed record AdminSectionCardTarget(
        Border Card,
        Label IconLabel,
        Label TitleLabel,
        Label SubtitleLabel,
        Label MetaLabel);
}
