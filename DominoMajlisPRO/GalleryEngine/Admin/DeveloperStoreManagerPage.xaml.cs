using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Services;
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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

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
            Children =
            {
                titleLabel,
                subtitleLabel,
                metaLabel
            }
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
            Content = grid
        };

        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await OpenSectionAsync(section))
        });

        card.WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 170 : 220;
        card.MinimumHeightRequest = 92;
        card.Margin = new Thickness(0, 0, 0, 10);

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
        if (section.Id == "inventory-audit")
        {
            await Navigation.PushAsync(new InventoryAuditPage());
            return;
        }

        if (section.Id == "current-season")
        {
            await Navigation.PushAsync(new CurrentSeasonEditorPage());
            return;
        }

        if (section.Id == "new-arrivals")
        {
            await Navigation.PushAsync(new NewArrivalsEditorPage());
            return;
        }

        if (section.Id == "limited-offers")
        {
            await Navigation.PushAsync(new LimitedOffersEditorPage());
            return;
        }

        if (section.Id == "categories")
        {
            await Navigation.PushAsync(new StoreCategoriesEditorPage());
            return;
        }

        if (section.Id == "avatars")
        {
            await Navigation.PushAsync(new AvatarsEditorPage());
            return;
        }

        if (section.Id == "backgrounds")
        {
            await Navigation.PushAsync(new BackgroundsEditorPage());
            return;
        }

        if (section.Id == "emblems")
        {
            await Navigation.PushAsync(new EmblemsManagerPage());
            return;
        }

        if (section.Id == "living-emblems")
        {
            await Navigation.PushAsync(new LivingEmblemsManagerPage());
            return;
        }

        if (section.Id == "emblem-backgrounds")
        {
            await Navigation.PushAsync(new EmblemBackgroundsManagerPage());
            return;
        }

        if (section.Id == "team-colors")
        {
            await Navigation.PushAsync(new TeamColorsManagerPage());
            return;
        }

        if (section.Id == "effects")
        {
            await Navigation.PushAsync(new EffectsManagerPage());
            return;
        }

        if (section.Id == "frames")
        {
            await Navigation.PushAsync(new FramesManagerPage());
            return;
        }

        if (section.Id == "titles")
        {
            await Navigation.PushAsync(new TitlesManagerPage());
            return;
        }

        if (section.Id == "bundles")
        {
            await Navigation.PushAsync(new BundlesManagerPage());
            return;
        }

        if (section.Id == "currency-pricing")
        {
            await Navigation.PushAsync(new CurrencyPricingManagerPage());
            return;
        }

        if (section.Id == "product-cards")
        {
            await Navigation.PushAsync(new NewArrivalsEditorPage());
            return;
        }

        if (section.Id == "category-cards")
        {
            await Navigation.PushAsync(new StoreCategoriesEditorPage());
            return;
        }

        if (section.Id == "store-settings")
        {
            await Navigation.PushAsync(new StoreSettingsManagerPage());
            return;
        }

        await DisplayAlert(
            section.Title,
            "سيتم بناء هذا القسم في المرحلة التالية",
            "حسناً");
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




