using Microsoft.Maui.Controls.Shapes;
using GalleryTheme = DominoMajlisPRO.GalleryEngine.Services.GalleryTheme;
using GalleryThemeEngine = DominoMajlisPRO.GalleryEngine.Services.GalleryThemeEngine;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Components.StoreSections;

public class PremiumStoreHeaderView : ContentView
{
    public event EventHandler? SeasonSwitchRequested;
    public event EventHandler? CartRequested;
    public event EventHandler? CoinsRequested;
    public event EventHandler? GemsRequested;
    public event EventHandler? IdentityRequested;

    private readonly Border _headerCard;
    private readonly Border _identityCard;
    private readonly Border _seasonButton;
    private readonly Border _avatarFrame;
    private readonly Border _memberBadge;
    private readonly Grid _cartButton;
    private readonly Border _cartCountBadge;
    private readonly Border _walletPanel;

    private Label _playerNameLabel = null!;

    private readonly Label _avatarLabel;
    private readonly Label _crownLabel;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _identityTitleLabel;
    private readonly Label _memberLabel;
    private readonly Label _seasonButtonLabel;
    private readonly Label _cartLabel;
    private readonly Label _cartCountLabel;
    private readonly Label _coinsLabel;
    private readonly Label _gemsLabel;
    private readonly Label _coinsIconLabel;
    private readonly Label _gemsIconLabel;
    private readonly Label _coinsPlusLabel;
    private readonly Label _gemsPlusLabel;
    private readonly ProgressBar _collectionProgress;
    private readonly Label _collectionProgressLabel;
    private readonly Label _collectionTitleLabel;
    private readonly Image _avatarImage;

    private readonly bool _isPhone;

    public PremiumStoreHeaderView()
    {
        FlowDirection = FlowDirection.RightToLeft;
        _isPhone = DeviceInfo.Idiom == DeviceIdiom.Phone;

        _crownLabel = CreateLabel("♛", _isPhone ? 18 : 24);
        _titleLabel = CreateTextLabel("متجر دومينو", _isPhone ? 24 : 32, true);
        _subtitleLabel = CreateTextLabel("كل ما يميزك في مكان واحد", _isPhone ? 10.5 : 13, false);

        _cartLabel = CreateLabel("🛒", _isPhone ? 22 : 28);
        _cartCountLabel = CreateTextLabel("0", _isPhone ? 8 : 10, true, "CinzelDecorative-Bold");

        _coinsIconLabel = CreateWalletIcon("🪙");
        _gemsIconLabel = CreateWalletIcon("💎");
        _coinsLabel = CreateWalletValue("0");
        _gemsLabel = CreateWalletValue("0");
        _coinsPlusLabel = CreateWalletPlus();
        _gemsPlusLabel = CreateWalletPlus();

        _identityTitleLabel = CreateTextLabel("الهوية الرسمية", _isPhone ? 11 : 13, true);
        _memberLabel = CreateTextLabel("ضيف", _isPhone ? 9 : 10, true);

        _avatarLabel = CreateLabel("♟", _isPhone ? 18 : 22);
        _avatarImage = new Image { Aspect = Aspect.AspectFill, IsVisible = false };
        _seasonButtonLabel = CreateTextLabel("تبديل الموسم", _isPhone ? 12 : 14, true);
        _collectionProgress = new ProgressBar { Progress = 0, HeightRequest = 5 };
        _collectionProgressLabel = CreateTextLabel("0 / 0", _isPhone ? 9 : 10, false);
        _collectionTitleLabel = CreateTextLabel("المقتنيات", _isPhone ? 10 : 12, true);

        _cartCountBadge = CreateCartCountBadge();
        _cartButton = CreateCartButton();
        _walletPanel = CreateWalletPanel();

        _avatarFrame = CreateAvatarFrame();
        _memberBadge = CreateMemberBadge();
        _identityCard = CreateIdentityCard();
        _seasonButton = CreateSeasonButton();

        _headerCard = new Border
        {
            StrokeThickness = 1,
            Padding = _isPhone ? new Thickness(10, 8) : new Thickness(16, 12),
            StrokeShape = new RoundRectangle { CornerRadius = _isPhone ? 18 : 22 },
            Content = CreateHeaderCardContent()
        };

        Content = new VerticalStackLayout
        {
            Spacing = 7,
            Children =
            {
                _headerCard,
                CreateBelowHeaderRow()
            }
        };

        ApplyTheme();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void ApplyTheme()
    {
        var theme = GalleryThemeEngine.Current;

        _headerCard.Background = theme.ActionBackground;
        _headerCard.Stroke = theme.Stroke;
        _headerCard.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(theme.Glow),
            Radius = 14,
            Opacity = 0.20f,
            Offset = new Point(0, 2)
        };

        _walletPanel.Background = theme.CardBackground;
        _walletPanel.Stroke = theme.Stroke;

        _cartCountBadge.Background = new SolidColorBrush(theme.Accent);
        _cartCountBadge.Stroke = theme.Gold;

        _identityCard.Background = theme.CardBackground;
        _identityCard.Stroke = theme.Stroke;

        _seasonButton.Background = theme.CardBackground;
        _seasonButton.Stroke = theme.Stroke;

        _avatarFrame.Background = theme.ActionBackground;
        _avatarFrame.Stroke = theme.Gold;

        _memberBadge.Background = theme.ActionBackground;
        _memberBadge.Stroke = theme.Stroke;

        _crownLabel.TextColor = theme.Gold;
        _titleLabel.TextColor = theme.TextPrimary;
        _subtitleLabel.TextColor = theme.TextSecondary;

        _cartCountLabel.TextColor = theme.TextPrimary;
        _coinsLabel.TextColor = theme.TextPrimary;
        _gemsLabel.TextColor = theme.TextPrimary;
        _coinsIconLabel.TextColor = theme.Gold;
        _gemsIconLabel.TextColor = theme.Gold;
        _coinsPlusLabel.TextColor = theme.Gold;
        _gemsPlusLabel.TextColor = theme.Gold;

        _avatarLabel.TextColor = theme.Gold;
        _identityTitleLabel.TextColor = theme.TextMuted;
        _playerNameLabel.TextColor = theme.TextPrimary;
        _memberLabel.TextColor = theme.TextSecondary;
        _seasonButtonLabel.TextColor = theme.Gold;
        _collectionProgress.ProgressColor = theme.Gold;
        _collectionProgressLabel.TextColor = theme.TextMuted;
        _collectionTitleLabel.TextColor = theme.TextSecondary;
    }

    private View CreateHeaderCardContent()
    {
        var brandStack = new VerticalStackLayout
        {
            Spacing = _isPhone ? 2 : 4,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                _crownLabel,
                _titleLabel,
                _subtitleLabel
            }
        };

        var actionRow = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight,
            ColumnSpacing = _isPhone ? 8 : 14,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        actionRow.Add(_seasonButton, 0, 0);
        actionRow.Add(new BoxView { Opacity = 0 }, 1, 0);
        actionRow.Add(_cartButton, 2, 0);

        return new VerticalStackLayout
        {
            Spacing = _isPhone ? 3 : 5,
            Children = { brandStack, actionRow }
        };
    }

    private View CreateBelowHeaderRow()
    {
        var row = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight,
            ColumnSpacing = 8,
            ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } }
        };

        row.Add(_identityCard, 0, 0);
        row.Add(_walletPanel, 1, 0);

        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                row,
                new VerticalStackLayout
                {
                    Spacing = 1,
                    Children = { _collectionTitleLabel, _collectionProgressLabel, _collectionProgress }
                }
            }
        };
    }

    private Grid CreateCartButton()
    {
        var grid = new Grid
        {
            WidthRequest = _isPhone ? 48 : 58,
            HeightRequest = _isPhone ? 48 : 58,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };

        grid.Children.Add(_cartLabel);
        grid.Children.Add(_cartCountBadge);

        _cartCountBadge.HorizontalOptions = LayoutOptions.End;
        _cartCountBadge.VerticalOptions = LayoutOptions.Start;
        _cartCountBadge.Margin = new Thickness(0, 2, 2, 0);

        AddTap(grid, () =>
        {
            if (CartRequested is not null)
                CartRequested.Invoke(this, EventArgs.Empty);
            else
                _ = ShowCartPlaceholderAsync();
        });

        return grid;
    }

    private Border CreateWalletPanel()
    {
        var grid = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight,
            RowSpacing = _isPhone ? 2 : 3,
            ColumnSpacing = _isPhone ? 5 : 7,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        grid.Add(_coinsIconLabel, 0, 0);
        grid.Add(_coinsLabel, 1, 0);
        grid.Add(_coinsPlusLabel, 2, 0);

        grid.Add(_gemsIconLabel, 0, 1);
        grid.Add(_gemsLabel, 1, 1);
        grid.Add(_gemsPlusLabel, 2, 1);

        var coinsTapTarget = new Grid { BackgroundColor = Colors.Transparent };
        Grid.SetColumnSpan(coinsTapTarget, 3);
        AddTap(coinsTapTarget, () => CoinsRequested?.Invoke(this, EventArgs.Empty));
        grid.Add(coinsTapTarget, 0, 0);

        var gemsTapTarget = new Grid { BackgroundColor = Colors.Transparent };
        Grid.SetColumnSpan(gemsTapTarget, 3);
        AddTap(gemsTapTarget, () => GemsRequested?.Invoke(this, EventArgs.Empty));
        grid.Add(gemsTapTarget, 0, 1);

        var border = new Border
        {
            WidthRequest = _isPhone ? 98 : 122,
            StrokeThickness = 0.8,
            Padding = _isPhone ? new Thickness(8, 5) : new Thickness(10, 6),
            StrokeShape = new RoundRectangle { CornerRadius = _isPhone ? 14 : 18 },
            Content = grid,
            VerticalOptions = LayoutOptions.Center
        };

        return border;
    }

    private Border CreateIdentityCard()
    {
        _playerNameLabel = CreateTextLabel("اختر لاعباً", _isPhone ? 11 : 13, true);

        var textStack = new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                _playerNameLabel,
                _memberBadge
            }
        };

        var content = new Grid
        {
            FlowDirection = FlowDirection.RightToLeft,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 7,
        };

        content.Add(_avatarFrame, 0, 0);
        content.Add(textStack, 1, 0);

        var border = new Border
        {
            MinimumWidthRequest = _isPhone ? 170 : 220,
            Padding = _isPhone ? new Thickness(7, 5) : new Thickness(8, 6),
            StrokeThickness = 0.8,
            StrokeShape = new RoundRectangle { CornerRadius = _isPhone ? 14 : 18 },
            Content = content
        };

        AddTap(border, () => IdentityRequested?.Invoke(this, EventArgs.Empty));

        return border;
    }

    private Border CreateSeasonButton()
    {
        var border = new Border
        {
            WidthRequest = _isPhone ? 126 : 150,
            HeightRequest = _isPhone ? 40 : 44,
            StrokeThickness = 0.9,
            Padding = new Thickness(10, 4),
            StrokeShape = new RoundRectangle { CornerRadius = _isPhone ? 15 : 18 },
            Content = _seasonButtonLabel
        };

        AddTap(border, () => SeasonSwitchRequested?.Invoke(this, EventArgs.Empty));

        return border;
    }

    private Border CreateAvatarFrame()
    {
        var size = _isPhone ? 32 : 40;

        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            StrokeThickness = 1.1,
            StrokeShape = new RoundRectangle { CornerRadius = size / 2d },
            Content = new Grid { Children = { _avatarLabel, _avatarImage } }
        };
    }

    private Border CreateMemberBadge()
    {
        return new Border
        {
            Padding = _isPhone ? new Thickness(7, 1) : new Thickness(8, 2),
            StrokeThickness = 0.7,
            StrokeShape = new RoundRectangle { CornerRadius = _isPhone ? 8 : 10 },
            Content = _memberLabel
        };
    }

    private Border CreateCartCountBadge()
    {
        return new Border
        {
            WidthRequest = _isPhone ? 16 : 19,
            HeightRequest = _isPhone ? 16 : 19,
            StrokeThickness = 0.6,
            StrokeShape = new RoundRectangle { CornerRadius = _isPhone ? 8 : 10 },
            Content = _cartCountLabel
        };
    }

    private Label CreateLabel(string text, double fontSize) => new()
    {
        Text = text,
        FontSize = fontSize,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    private Label CreateTextLabel(string text, double fontSize, bool bold, string fontFamily = "Tajawal-Regular") => new()
    {
        Text = text,
        FontFamily = fontFamily,
        FontSize = fontSize,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        MaxLines = 1,
        LineBreakMode = LineBreakMode.TailTruncation,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    private Label CreateWalletIcon(string text) => CreateLabel(text, _isPhone ? 12 : 15);

    private Label CreateWalletValue(string text) => CreateTextLabel(
        text,
        _isPhone ? 11 : 14,
        true,
        "CinzelDecorative-Bold");

    private Label CreateWalletPlus() => CreateTextLabel("+", _isPhone ? 12 : 15, true);

    private static void AddTap(View view, Action action)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => action();
        view.GestureRecognizers.Add(tap);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        GalleryThemeEngine.ThemeChanged += OnThemeChanged;
        AppEvents.StoreEconomyChanged -= OnStoreEconomyChanged;
        AppEvents.StoreEconomyChanged += OnStoreEconomyChanged;
        AppEvents.PlayerProfileChanged -= OnPlayerProfileChanged;
        AppEvents.PlayerProfileChanged += OnPlayerProfileChanged;
        AvatarsAdminService.PublishedChanged -= OnCollectiblesChanged;
        AvatarsAdminService.PublishedChanged += OnCollectiblesChanged;
        BackgroundsAdminService.PublishedChanged -= OnCollectiblesChanged;
        BackgroundsAdminService.PublishedChanged += OnCollectiblesChanged;
        ApplyTheme();
        _ = RefreshStoreIdentityAsync();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        GalleryThemeEngine.ThemeChanged -= OnThemeChanged;
        AppEvents.StoreEconomyChanged -= OnStoreEconomyChanged;
        AppEvents.PlayerProfileChanged -= OnPlayerProfileChanged;
        AvatarsAdminService.PublishedChanged -= OnCollectiblesChanged;
        BackgroundsAdminService.PublishedChanged -= OnCollectiblesChanged;
    }

    private void OnThemeChanged(object? sender, GalleryTheme theme)
    {
        ApplyTheme();
    }

    private void OnStoreEconomyChanged(string playerId) => _ = RefreshStoreIdentityAsync();
    private void OnPlayerProfileChanged() => _ = RefreshStoreIdentityAsync();
    private void OnCollectiblesChanged() => _ = RefreshStoreIdentityAsync();

    private static Task ShowCartPlaceholderAsync()
    {
        var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        return page?.DisplayAlert("سلة المشتريات", "سلة المشتريات فارغة حالياً", "حسناً") ?? Task.CompletedTask;
    }

    private async Task RefreshStoreIdentityAsync()
    {
        var identity = await HonorIdentityService.LoadAsync();
        _seasonButton.IsVisible = identity.IsActivated && identity.Role == HonorRoleType.Developer;

        if (string.IsNullOrWhiteSpace(identity.PlayerId))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _playerNameLabel.Text = "اختر لاعباً";
                _memberLabel.Text = identity.IsActivated ? RoleLabel(identity.Role) : "ضيف";
                _avatarImage.IsVisible = false;
                _avatarLabel.IsVisible = true;
                _coinsLabel.Text = "0";
                _gemsLabel.Text = "0";
                _collectionProgress.Progress = 0;
                _collectionProgressLabel.Text = "0 / 0   0%";
            });
            return;
        }

        var wallet = await PlayerStoreIdentityService.GetWalletAsync(identity.PlayerId);
        var progress = await PlayerStoreIdentityService.GetCollectionProgressAsync(identity.PlayerId);
        var profile = await PlayerProfileService.GetPlayerByIdAsync(identity.PlayerId);
        var ratio = progress.TotalPublished == 0 ? 0 : progress.TotalOwned / (double)progress.TotalPublished;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _playerNameLabel.Text = string.IsNullOrWhiteSpace(profile?.PlayerName) ? "اختر لاعباً" : profile.PlayerName;
            _memberLabel.Text = ResolveRoleLabel(identity.Role, profile);
            _avatarImage.Source =
                profile == null
                    ? null
                    : PlayerProfileService.GetPlayerImageSource(profile);
            _avatarImage.IsVisible = _avatarImage.Source != null;
            _avatarLabel.IsVisible = !_avatarImage.IsVisible;
            _coinsLabel.Text = wallet.Coins.ToString("N0");
            _gemsLabel.Text = wallet.Gems.ToString("N0");
            _collectionProgress.Progress = ratio;
            var percent = progress.TotalPublished == 0 ? 0 : (int)Math.Round(ratio * 100);
            _collectionProgressLabel.Text = $"{progress.TotalOwned} / {progress.TotalPublished}   {percent}%";
        });
    }

    private static string RoleLabel(HonorRoleType role) => role switch
    {
        HonorRoleType.Developer => "مطور",
        HonorRoleType.Founder => "مؤسس",
        HonorRoleType.Honor => "عضو شرف",
        _ => "لاعب"
    };

    private static string ResolveRoleLabel(HonorRoleType role, PlayerProfileModel? profile)
    {
        if (role != HonorRoleType.None)
            return RoleLabel(role);
        return profile?.ProfileStatus switch
        {
            PlayerProfileStatus.Developer => "مطور",
            PlayerProfileStatus.Founder => "مؤسس",
            PlayerProfileStatus.Honor => "عضو شرف",
            _ => "لاعب"
        };
    }

    private static string ResolveAvatarPath(PlayerProfileModel? profile)
    {
        if (profile == null) return string.Empty;
        if (profile.UseCustomAvatar && !string.IsNullOrWhiteSpace(profile.AvatarPath)) return profile.AvatarPath;
        if (!string.IsNullOrWhiteSpace(profile.ProfileImagePath)) return profile.ProfileImagePath;
        if (!string.IsNullOrWhiteSpace(profile.AvatarImage)) return profile.AvatarImage;
        return profile.BuiltInAvatar;
    }
}
