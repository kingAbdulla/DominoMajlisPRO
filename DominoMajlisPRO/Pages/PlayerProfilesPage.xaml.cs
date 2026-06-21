using DominoMajlisPRO.GalleryEngine.Components.StoreSections;
using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class PlayerProfilesPage : ContentPage
{
    List<PlayerProfileModel> allPlayers = new();
    bool canManagePlayers = false;
    ApplicationUserModel? currentUser;
    bool accountHubExpanded = true;
    bool collectionExpanded = true;

    public PlayerProfilesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.PlayerProfileChanged -= OnPlayerProfileChanged;
        AppEvents.PlayerProfileChanged += OnPlayerProfileChanged;

        AppEvents.DataChanged -= OnPlayerProfileChanged;
        AppEvents.DataChanged += OnPlayerProfileChanged;

        AppEvents.CurrentUserChanged -= OnCurrentUserChanged;
        AppEvents.CurrentUserChanged += OnCurrentUserChanged;

        AppEvents.StoreProgressChanged -= OnStoreProgressChanged;
        AppEvents.StoreProgressChanged += OnStoreProgressChanged;

        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        StoreAssetQueryService.PublishedContentChanged -= OnPublishedContentChanged;
        StoreAssetQueryService.PublishedContentChanged += OnPublishedContentChanged;

        await RefreshIdentityAsync();
        await RefreshCollectionProgressAsync();
        await LoadPlayersAsync();
    }

    async void OnPlayerProfileChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await LoadPlayersAsync();
        });
    }

    async void OnCurrentUserChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await RefreshIdentityAsync();
            await RefreshCollectionProgressAsync();
            if (InventoryOverlay.IsVisible)
                await LoadInventoryAsync();
            await LoadPlayersAsync();
        });
    }

    async void OnStoreProgressChanged(string playerId)
    {
        if (!SameId(currentUser?.PlayerId, playerId))
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await RefreshCollectionProgressAsync();
            await RefreshAccountAvatarAsync();
            if (InventoryOverlay.IsVisible)
                await LoadInventoryAsync();
        });
    }

    async void OnTeamAssetsChanged(string teamId)
    {
        if (string.IsNullOrWhiteSpace(currentUser?.PlayerId))
            return;

        var team =
            await TeamProfileService.GetTeamByPlayerIdAsync(
                currentUser.PlayerId);

        if (!SameId(team?.TeamId, teamId))
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await RefreshCollectionProgressAsync();
            if (InventoryOverlay.IsVisible)
                await LoadInventoryAsync();
        });
    }

    async void OnPublishedContentChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await RefreshCollectionProgressAsync();
            await RefreshAccountAvatarAsync();
            if (InventoryOverlay.IsVisible)
                await LoadInventoryAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        AppEvents.PlayerProfileChanged -= OnPlayerProfileChanged;
        AppEvents.DataChanged -= OnPlayerProfileChanged;
        AppEvents.CurrentUserChanged -= OnCurrentUserChanged;
        AppEvents.StoreProgressChanged -= OnStoreProgressChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        StoreAssetQueryService.PublishedContentChanged -= OnPublishedContentChanged;
    }

    async Task RefreshIdentityAsync()
    {
        currentUser =
            await ApplicationUserService.EnsureCurrentSessionAsync();

        string role = GetRoleDisplayName(currentUser.Role);
        string sessionState =
            currentUser.Role == ApplicationUserRole.Ghost
                ? "ضيف"
                : "متصل";

        AccountDisplayNameLabel.Text =
            string.IsNullOrWhiteSpace(currentUser.DisplayName)
                ? role
                : currentUser.DisplayName;
        AccountRoleSessionLabel.Text =
            $"{role} • {sessionState}";
        IdentityUserIdLabel.Text =
            ShortId(currentUser.ApplicationUserId);
        IdentityPlayerIdLabel.Text =
            DisplayId(currentUser.PlayerId);

        bool isGhost =
            currentUser.Role == ApplicationUserRole.Ghost;
        bool hasPlayerId =
            !string.IsNullOrWhiteSpace(currentUser.PlayerId);

        RegisteredAccountControls.IsVisible = !isGhost;
        GuestAccountControls.IsVisible = isGhost;
        MissingPlayerIdLabel.IsVisible = !isGhost && !hasPlayerId;

        await RefreshAccountAvatarAsync();
    }

    async Task RefreshAccountAvatarAsync()
    {
        const string fallbackAvatar = "normal_avatar_1.png";

        if (string.IsNullOrWhiteSpace(currentUser?.PlayerId))
        {
            AccountAvatarImage.Source = fallbackAvatar;
            AccountAvatarFrameOverlay.IsVisible = false;
            AccountAvatarEffectOverlay.IsVisible = false;
            AccountProfileBackgroundImage.IsVisible = false;
            return;
        }

        var identity =
            await PlayerVisualIdentityResolver.ResolveAsync(
                currentUser.PlayerId);
        string avatarPath =
            identity.Avatar?.PreviewImage ?? string.Empty;
        ApplyIdentityImage(
            AccountProfileBackgroundImage,
            identity.ProfileBackground?.PreviewImage);
        var frameSource =
            ToOptionalImageSource(identity.Frame?.PreviewImage);
        AccountAvatarFrameOverlay.Source = frameSource;
        AccountAvatarFrameOverlay.IsVisible = frameSource != null;
        ApplyPlayerEffectOverlay(
            AccountAvatarEffectOverlay,
            identity.Effect);
        AccountAvatarFrame.Stroke = identity.Frame == null
            ? Color.FromArgb("#D4AF37")
            : Colors.Transparent;
        AccountAvatarFrame.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#F2C14E")),
                Radius = 18,
                Opacity = 0.55f
            };
        if (identity.Title != null)
        {
            AccountRoleSessionLabel.Text =
                $"{AccountRoleSessionLabel.Text} • {identity.Title.DisplayName}";
        }

        if (string.IsNullOrWhiteSpace(avatarPath))
        {
            var profile =
                await PlayerProfileService.GetPlayerByIdAsync(
                    currentUser.PlayerId);

            if (profile != null)
            {
                avatarPath =
                    profile.UseCustomAvatar &&
                    !string.IsNullOrWhiteSpace(profile.AvatarPath)
                        ? profile.AvatarPath
                        : !string.IsNullOrWhiteSpace(profile.ProfileImagePath)
                            ? profile.ProfileImagePath
                            : !string.IsNullOrWhiteSpace(profile.AvatarImage)
                                ? profile.AvatarImage
                                : profile.BuiltInAvatar;
            }
        }

        AccountAvatarImage.Source =
            InventoryDisplayResolver.ResolveImageSource(
                avatarPath,
                fallbackAvatar);
    }

        void ApplyPlayerEffectOverlay(
        Image effectOverlay,
        DominoMajlisPRO.GalleryEngine.Models.CatalogAssetDisplay? effect)
    {
        PlayerEffectEngine.Apply(effectOverlay, effect, 1.18);
    }

    async Task RefreshCollectionProgressAsync()
    {
        try
        {
            string playerId = currentUser?.PlayerId ?? "";
            string? teamId = null;
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                var team =
                    await TeamProfileService.GetTeamByPlayerIdAsync(playerId);
                teamId = team?.TeamId;
            }

            var snapshot = await InventoryDisplayResolver.ResolveAsync(
                playerId,
                teamId);
            var counts = snapshot.ByAssetType
                .Select(item => new CollectionCount(
                    item.Key,
                    item.Owned,
                    item.Total))
                .ToList();
            int total = Math.Max(0, snapshot.TotalAvailable);
            int owned = Math.Min(
                Math.Max(0, snapshot.TotalOwned),
                total);
            int remaining = Math.Max(0, total - owned);
            double progress = total == 0
                ? 0
                : Math.Clamp((double)owned / total, 0, 1);

            CollectionTotalLabel.Text = $"{owned} / {total}";
            CollectionPercentLabel.Text = $"{progress * 100:0}%";
            CollectionRemainingLabel.Text = $"المتبقي: {remaining}";
            CollectionProgressBar.Progress = progress;

            SetBreakdown(
                AvatarsProgressLabel,
                "Avatars",
                counts,
                StoreTypeRegistry.Avatar.TypeId);
            SetBreakdown(
                BackgroundsProgressLabel,
                "Profile Backgrounds",
                counts,
                StoreProductAssetType.ProfileBackground.ToString());
            SetBreakdown(
                EmblemsProgressLabel,
                "Emblems",
                counts,
                StoreProductAssetType.Emblem.ToString());
            SetBreakdown(
                TeamColorsProgressLabel,
                "Team Colors",
                counts,
                StoreProductAssetType.TeamColor.ToString());
            SetBreakdown(
                EmblemBackgroundsProgressLabel,
                "Emblem Backgrounds",
                counts,
                StoreProductAssetType.EmblemBackground.ToString());

            var frame = FindCount(
                counts,
                StoreProductAssetType.Frame.ToString());
            var effect = FindCount(
                counts,
                StoreProductAssetType.Effect.ToString());
            FramesEffectsProgressLabel.Text =
                $"Frames / Effects {frame.Owned + effect.Owned}/{frame.Total + effect.Total}";
        }
        catch
        {
            SetEmptyCollectionProgress();
        }
    }

    static void SetBreakdown(
        Label label,
        string title,
        IEnumerable<CollectionCount> counts,
        string typeId)
    {
        var count = FindCount(counts, typeId);
        label.Text = $"{title} {count.Owned}/{count.Total}";
    }

    static CollectionCount FindCount(
        IEnumerable<CollectionCount> counts,
        string typeId) =>
        counts.FirstOrDefault(item =>
            SameId(item.TypeId, typeId))
        ?? new CollectionCount(typeId, 0, 0);

    void SetEmptyCollectionProgress()
    {
        CollectionTotalLabel.Text = "0 / 0";
        CollectionPercentLabel.Text = "0%";
        CollectionRemainingLabel.Text = "المتبقي: 0";
        CollectionProgressBar.Progress = 0;
        AvatarsProgressLabel.Text = "Avatars 0/0";
        BackgroundsProgressLabel.Text = "Profile Backgrounds 0/0";
        EmblemsProgressLabel.Text = "Emblems 0/0";
        TeamColorsProgressLabel.Text = "Team Colors 0/0";
        EmblemBackgroundsProgressLabel.Text =
            "Emblem Backgrounds 0/0";
        FramesEffectsProgressLabel.Text = "Frames / Effects 0/0";
    }

    void OnAccountHubHeaderTapped(object? sender, TappedEventArgs e)
    {
        accountHubExpanded = !accountHubExpanded;
        AccountHubContent.IsVisible = accountHubExpanded;
        AccountHubArrowLabel.Text = accountHubExpanded ? "âŒƒ" : "âŒ„";
    }

    void OnCollectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        collectionExpanded = !collectionExpanded;
        CollectionContent.IsVisible = collectionExpanded;
        CollectionArrowLabel.Text = collectionExpanded ? "âŒƒ" : "âŒ„";
    }

    async void OnCreateIdentityClicked(object? sender, EventArgs e)
    {
        await CreateIdentityAsync();
    }

    async Task CreateIdentityAsync()
    {
        currentUser =
            await ApplicationUserService.EnsureCurrentSessionAsync();

        if (currentUser.Role != ApplicationUserRole.Ghost)
        {
            try
            {
                await ApplicationUserService
                    .EnsureCurrentUserPlayerProfileAsync();
                await RefreshIdentityAsync();
                await RefreshCollectionProgressAsync();
                await LoadPlayersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "???�?�?� ?�?�?� ?�?�?�?�??",
                    ex.Message,
                    "حسناً");
            }

            return;
        }

        string? playerName = await DisplayPromptAsync(
            "إنشاء هوية لاعب",
            "أدخل اسم اللاعب:",
            "إنشاء",
            "إلغاء",
            maxLength: 40);

        if (string.IsNullOrWhiteSpace(playerName))
            return;

        try
        {
            await ApplicationUserService
                .UpgradeGhostToMemberAsync(playerName);
            await ApplicationUserService
                .EnsureCurrentUserPlayerProfileAsync();
            await RefreshIdentityAsync();
            await RefreshCollectionProgressAsync();
            await LoadPlayersAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "تعذر إنشاء الهوية",
                ex.Message,
                "حسناً");
        }
    }

    async void OnSwitchIdentityClicked(object? sender, EventArgs e)
    {
        await ShowLocalUserSwitcherAsync();
    }

    async void OnLoginIdentityClicked(object? sender, EventArgs e)
    {
        await ShowLocalUserSwitcherAsync();
    }

    async Task ShowLocalUserSwitcherAsync()
    {
        var activeUser =
            await ApplicationUserService.EnsureCurrentSessionAsync();
        currentUser = activeUser;

        var users =
            await ApplicationUserService.GetAllUsersAsync();

        var choices = users
            .Where(user =>
                !string.Equals(
                    user.ApplicationUserId,
                    activeUser.ApplicationUserId,
                    StringComparison.OrdinalIgnoreCase))
            .Select(user => new
            {
                User = user,
                Label =
                    $"{user.DisplayName} • {GetRoleDisplayName(user.Role)} • {ShortId(user.ApplicationUserId)}"
            })
            .ToList();

        if (choices.Count == 0)
        {
            await DisplayAlertAsync(
                "تسجيل الدخول",
                "?�?� ???�?�?� ?�?�?�?�?�?? ?�?�???�?�?� ?�?�?� ?�?�?� ?�?�?�?�?�?�.",
                "حسناً");
            return;
        }

        string? selected = await DisplayActionSheetAsync(
            "?�?�???� ?�?�?�?�?�?� ?�?�???�?�?�?�",
            "إلغاء",
            null,
            choices.Select(item => item.Label).ToArray());

        var selection = choices.FirstOrDefault(item =>
            string.Equals(item.Label, selected, StringComparison.Ordinal));

        if (selection != null)
        {
            await ApplicationUserService.SwitchUserAsync(
                selection.User.ApplicationUserId);
        }
    }

    async void OnLogoutIdentityClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(
            "تسجيل الخروج",
            "?�?????� ?�?�?�?�?? ?�?�?�?�?�?� ???�?�?� ?�?�?� ?????�?�?? ?�?�?�?�???� ?�?� ?�???�?�?�?? ?�?�?�?�?�?�.",
            "تسجيل الخروج",
            "إلغاء");

        if (!confirm)
            return;

        await ApplicationUserService.LogoutAsync();
        currentUser = await ApplicationUserService.EnsureGhostUserAsync();
        await RefreshIdentityAsync();
        await RefreshCollectionProgressAsync();
        await LoadPlayersAsync();
    }

    async void OnContinueGuestClicked(object? sender, EventArgs e)
    {
        currentUser = await ApplicationUserService.EnsureGhostUserAsync();
        await RefreshIdentityAsync();
        await RefreshCollectionProgressAsync();
    }

    async void OnViewProfileClicked(object? sender, EventArgs e)
    {
        currentUser =
            await ApplicationUserService.EnsureCurrentSessionAsync();

        if (currentUser.Role == ApplicationUserRole.Ghost)
        {
            AccountHubContent.IsVisible = true;
            accountHubExpanded = true;
            AccountHubArrowLabel.Text = "âŒƒ";
            return;
        }

        try
        {
            var profile =
                await ApplicationUserService
                    .EnsureCurrentUserPlayerProfileAsync();

            if (profile == null ||
                string.IsNullOrWhiteSpace(profile.PlayerId))
            {
                MissingPlayerIdLabel.IsVisible = true;
                return;
            }

            await RefreshIdentityAsync();
            await RefreshCollectionProgressAsync();
            await LoadPlayersAsync();

            await Navigation.PushAsync(
                new PlayerDetailsPage(profile.PlayerId));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "تعذر فتح الملف الشخصي",
                ex.Message,
                "حسناً");
        }
    }

    async void OnMyItemsTapped(object? sender, TappedEventArgs e)
    {
        InventoryOverlay.IsVisible = true;
        await LoadInventoryAsync();
    }

    void OnCloseInventoryClicked(object? sender, EventArgs e)
    {
        InventoryOverlay.IsVisible = false;
    }

    async Task LoadInventoryAsync()
    {
        InventoryItemsContainer.Children.Clear();
        currentUser =
            await ApplicationUserService.EnsureCurrentSessionAsync();

        if (currentUser.Role == ApplicationUserRole.Ghost ||
            string.IsNullOrWhiteSpace(currentUser.PlayerId))
        {
            InventoryOwnerLabel.Text =
                "سجّل الدخول إلى حساب لاعب لعرض المقتنيات.";
            InventoryItemsContainer.Children.Add(
                CreateInventoryEmptyLabel("?�?� ???�?�?� ?�?�???�???�?? ?�?�???�?�?� ?�?�?�?�????."));
            return;
        }

        string playerId = currentUser.PlayerId;
        InventoryOwnerLabel.Text =
            currentUser.DisplayName;

        var snapshot = await InventoryDisplayResolver.ResolveAsync(
            playerId);
        foreach (var category in OwnedAssetCategoryCatalog.All
                     .Where(category => !category.IsTeamAsset))
        {
            AddInventorySection(
                category.Group,
                category.DisplayName,
                category.AssetType,
                playerId,
                null,
                snapshot.Items);
        }
    }

    void AddInventorySection(
        string group,
        string title,
        string typeId,
        string playerId,
        string? teamId,
        IReadOnlyList<ResolvedInventoryDisplay> assets)
    {
        var matching = assets
            .Where(item => SameId(item.AssetType, typeId))
            .ToList();

        AddInventorySectionHeader(group, title);
        if (matching.Count == 0)
        {
            InventoryItemsContainer.Children.Add(
                CreateInventoryEmptyLabel("لا توجد عناصر مملوكة."));
            return;
        }

        foreach (var item in matching)
        {
            InventoryItemsContainer.Children.Add(
                CreateInventoryCard(
                    item,
                    item.IsEquipped,
                    async () =>
                    {
                        bool equipped =
                            await PlayerAssetInventoryService.EquipAsync(
                                playerId,
                                item.AssetId,
                                item.AssetType);

                        if (equipped)
                        {
                            await RefreshAccountAvatarAsync();
                            await RefreshCollectionProgressAsync();
                            await LoadInventoryAsync();
                        }
                    }));
        }
    }

    void AddTeamInventorySection(
        string title,
        string typeId,
        string? teamId,
        IReadOnlyList<TeamOwnedAssetItem> assets)
    {
        var matching = assets
            .Where(item => SameId(item.TeamAssetTypeId, typeId))
            .ToList();

        AddInventorySectionHeader("TEAM ASSETS", title);
        if (matching.Count == 0)
        {
            InventoryItemsContainer.Children.Add(
                CreateInventoryEmptyLabel("لا توجد عناصر مملوكة."));
            return;
        }

        foreach (var item in matching)
        {
            var teamPayload = TeamAssetPayloadCatalog.Resolve(
                item.TeamAssetId,
                item.TeamAssetTypeId);
            var payload = new InventoryPayload(
                item.TeamAssetId,
                StoreAssetCatalogService.IncompleteDisplayName,
                teamPayload?.ImagePath ?? "",
                teamPayload?.ColorHex ??
                teamPayload?.BackgroundColorHex ??
                "",
                StoreAssetCatalogService.CanonicalTypeId(
                    item.TeamAssetTypeId));

            InventoryItemsContainer.Children.Add(
                CreateInventoryCard(
                    payload,
                    item.IsEquipped,
                    async () =>
                    {
                        if (string.IsNullOrWhiteSpace(teamId))
                            return;

                        bool equipped =
                            await TeamAssetInventoryService.EquipAsync(
                                teamId,
                                item.TeamAssetId,
                                item.TeamAssetTypeId);

                        if (equipped)
                            await LoadInventoryAsync();
                    }));
        }
    }

    void AddInventorySectionHeader(string group, string title)
    {
        InventoryItemsContainer.Children.Add(
            new VerticalStackLayout
            {
                Margin = new Thickness(0, 6, 0, 0),
                Spacing = 1,
                Children =
                {
                    new Label
                    {
                        Text = group,
                        TextColor = Color.FromArgb("#8E7B50"),
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = title,
                        TextColor = Color.FromArgb("#D4AF37"),
                        FontSize = 17,
                        FontAttributes = FontAttributes.Bold
                    }
                }
            });
    }

    View CreateInventoryCard(
        ResolvedInventoryDisplay item,
        bool isEquipped,
        Func<Task> equip) =>
        CreateInventoryCard(
            new InventoryPayload(
                item.AssetId,
                item.DisplayName,
                item.PreviewImage,
                item.ColorHex,
                item.AssetType),
            isEquipped,
            equip);

    View CreateInventoryCard(
        InventoryPayload payload,
        bool isEquipped,
        Func<Task> equip)
    {
        var visual = new Border
        {
            WidthRequest = 54,
            HeightRequest = 54,
            BackgroundColor = ParseColor(payload.ColorHex, "#181818"),
            Stroke = Color.FromArgb("#4B4025"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 }
        };

        visual.Content = string.IsNullOrWhiteSpace(payload.ImagePath)
            ? new Label
            {
                Text = "â—†",
                TextColor = Color.FromArgb("#D4AF37"),
                FontSize = 22,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
            : new Image
            {
                Source = ToImageSource(payload.ImagePath),
                Aspect = Aspect.AspectFill
            };

        var details = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = payload.DisplayName,
                    TextColor = Colors.White,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    LineBreakMode = LineBreakMode.TailTruncation
                },
                new Label
                {
                    Text = $"{payload.AssetType} • {(isEquipped ? "Equipped" : "Owned")}",
                    TextColor = Color.FromArgb("#999999"),
                    FontSize = 10
                }
            }
        };

        var action = new Button
        {
            Text = isEquipped ? "مجهز" : "تجهيز",
            IsEnabled = !isEquipped,
            BackgroundColor = isEquipped
                ? Color.FromArgb("#3B321D")
                : Color.FromArgb("#D4AF37"),
            TextColor = isEquipped
                ? Color.FromArgb("#D4AF37")
                : Colors.Black,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(12, 7)
        };
        action.Clicked += async (_, _) => await equip();

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        grid.Add(visual, 0, 0);
        grid.Add(details, 1, 0);
        grid.Add(action, 2, 0);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#151515"),
            Stroke = isEquipped
                ? Color.FromArgb("#D4AF37")
                : Color.FromArgb("#333333"),
            StrokeThickness = isEquipped ? 1.4 : 1,
            Padding = 10,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = grid
        };
    }

    static Label CreateInventoryEmptyLabel(string text) =>
        new()
        {
            Text = text,
            TextColor = Color.FromArgb("#777777"),
            FontSize = 12,
            Margin = new Thickness(4, 0, 4, 4)
        };

    static ImageSource ToImageSource(string path) =>
        InventoryDisplayResolver.ResolveImageSource(path);

    static ImageSource? ToOptionalImageSource(string? path) =>
        InventoryDisplayResolver.ResolveOptionalImageSource(path);

    static void ApplyIdentityImage(Image image, string? imagePath)
    {
        var source = ToOptionalImageSource(imagePath);
        image.Source = source;
        image.IsVisible = source != null;
    }

    static Image? CreateIdentityImage(
        string? imagePath,
        double opacity)
    {
        var source = ToOptionalImageSource(imagePath);
        return source == null
            ? null
            : new Image
            {
                Source = source,
                Aspect = Aspect.AspectFill,
                Opacity = opacity,
                InputTransparent = true
            };
    }

    static void AddIdentityOverlay(Grid container, string? imagePath)
    {
        var source = ToOptionalImageSource(imagePath);
        if (source == null)
            return;

        container.Add(
            new Image
            {
                Source = source,
                Aspect = Aspect.AspectFit,
                InputTransparent = true
            });
    }

    static Color ParseColor(string value, string fallback)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value) ||
                   value.Equals(
                       "Transparent",
                       StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb(fallback)
                : Color.FromArgb(value);
        }
        catch
        {
            return Color.FromArgb(fallback);
        }
    }

    static string TeamInventoryTypeId(string assetType) => assetType switch
    {
        "Emblem" => TeamAssetTypes.Emblem.TeamAssetTypeId,
        "TeamColor" => TeamAssetPayloadCatalog.TeamColorTypeId,
        "EmblemBackground" =>
            TeamAssetTypes.EmblemBackground.TeamAssetTypeId,
        _ => assetType
    };

    static string GetRoleDisplayName(ApplicationUserRole role) =>
        role switch
        {
            ApplicationUserRole.Developer => "Developer",
            ApplicationUserRole.Founder => "Founder",
            ApplicationUserRole.Honor => "Honor",
            ApplicationUserRole.Member => "Member",
            _ => "Ghost"
        };

    static string ShortId(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "—"
            : value.Length <= 12
                ? value
                : value[..12];

    static string DisplayId(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    static bool SameId(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(
            left.Trim(),
            right.Trim(),
            StringComparison.OrdinalIgnoreCase);

    sealed record CollectionCount(
        string TypeId,
        int Owned,
        int Total);

    sealed record InventoryPayload(
        string AssetId,
        string DisplayName,
        string ImagePath,
        string ColorHex,
        string AssetType = "Unknown");

    async Task LoadPlayersAsync()
    {
        canManagePlayers =
            await PlayerManagementService.CanManagePlayersAsync();

        await PlayerTeamSyncService.SyncPlayersFromTeamsAsync();

        allPlayers =
            await PlayerProfileService.LoadPlayersAsync();

        await RenderPlayers(allPlayers);
    }

    async Task RenderPlayers(List<PlayerProfileModel> players)
    {
        PlayersContainer.Children.Clear();

        if (canManagePlayers)
            PlayersContainer.Children.Add(CreateAdminActionsCard());

        if (players.Count == 0)
        {
            PlayersContainer.Children.Add(
                new Label
                {
                    Text = "لا يوجد لاعبون بعد",
                    TextColor = Colors.White,
                    FontSize = 18,
                    HorizontalTextAlignment = TextAlignment.Center
                });

            return;
        }

        var identities =
            await PlayerVisualIdentityResolver.ResolveManyAsync(
                players.Select(player => player.PlayerId));
        int rank = 1;

        foreach (var player in players.OrderByDescending(x => x.Wins))
        {
            identities.TryGetValue(
                player.PlayerId,
                out var identity);
            PlayersContainer.Children.Add(
                CreatePlayerCard(rank, player, identity));

            rank++;
        }
    }

    View CreateAdminActionsCard()
    {
        Border card =
            new()
            {
                BackgroundColor = Color.FromArgb("#120808"),
                Stroke = Color.FromArgb("#C62828"),
                StrokeThickness = 1.4,
                Padding = 12,
                StrokeShape = new RoundRectangle { CornerRadius = 22 }
            };

        VerticalStackLayout layout =
            new()
            {
                Spacing = 8
            };

        layout.Children.Add(
            new Label
            {
                Text = "أدوات إدارة اللاعبين",
                TextColor = Color.FromArgb("#D4AF37"),
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            });

        Button deleteAllButton =
            new()
            {
                Text = "حذف جميع اللاعبين",
                BackgroundColor = Color.FromArgb("#C62828"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 16,
                HeightRequest = 46
            };

        deleteAllButton.Clicked += OnDeleteAllPlayersClicked;

        layout.Children.Add(deleteAllButton);

        card.Content = layout;

        return card;
    }

    View CreatePlayerCard(
        int rank,
        PlayerProfileModel player,
        PlayerVisualIdentity? identity)
    {
        PlayerEngine.Normalize(player);

        var rankResult =
            PlayerRankService.Calculate(player.PlayerXP);

        Border card =
            new()
            {
                BackgroundColor = Color.FromArgb("#0B0B0B"),
                Stroke = Color.FromArgb(PlayerEngine.GetStatusColor(player.ProfileStatus)),
                StrokeThickness = 1.2,
                Padding = 12,
                StrokeShape = new RoundRectangle { CornerRadius = 24 }
            };
        Grid root =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 105 },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 12,
                FlowDirection = FlowDirection.RightToLeft
            };
        var backgroundImage =
            CreateIdentityImage(
                identity?.ProfileBackground?.PreviewImage,
                0.34);
        if (backgroundImage != null)
        {
            Grid.SetColumnSpan(backgroundImage, 2);
            root.Children.Add(backgroundImage);
        }

        var avatarSection = CreateAvatarSection(player, identity);
        Grid.SetColumn(avatarSection, 0);
        root.Children.Add(avatarSection);

        var infoSection =
            CreatePlayerInfoSection(
                rank,
                player,
                rankResult,
                identity);
        Grid.SetColumn(infoSection, 1);
        root.Children.Add(infoSection);

        card.Content = root;

        return card;
    }

    View CreateAvatarSection(
        PlayerProfileModel player,
        PlayerVisualIdentity? identity)
    {
        string statusColor =
            PlayerEngine.GetStatusColor(player.ProfileStatus);

        var avatar = new Grid();
        avatar.Add(
            new Image
            {
                Source =
                    ToOptionalImageSource(identity?.Avatar?.PreviewImage) ??
                    ResolvePlayerAvatarSource(player),
                Aspect = Aspect.AspectFill,
                WidthRequest = 92,
                HeightRequest = 92
            });
        AddIdentityOverlay(avatar, identity?.Frame?.PreviewImage);
        AddIdentityOverlay(avatar, identity?.Effect?.PreviewImage);

        return new Border
        {
            WidthRequest = 92,
            HeightRequest = 92,
            BackgroundColor = Color.FromArgb("#151515"),
            Stroke = identity?.Frame == null
                ? Color.FromArgb(statusColor)
                : Colors.Transparent,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(
                        Color.FromArgb("#F2C14E")),
                    Radius = 18,
                    Opacity = 0.55f
                },
            Content = avatar
        };
    }

    static ImageSource ResolvePlayerAvatarSource(
        PlayerProfileModel player)
    {
        return PlayerProfileService.GetPlayerImageSource(player);
    }

    View CreatePlayerInfoSection(
        int rank,
        PlayerProfileModel player,
        PlayerRankResult rankResult,
        PlayerVisualIdentity? identity)
    {
        VerticalStackLayout info =
            new()
            {
                Spacing = 7
            };

        info.Children.Add(CreatePlayerHeader(rank, player));
        info.Children.Add(
            CreateStatusRankLabel(player, rankResult, identity));
        info.Children.Add(CreateXpLabel(player));
        info.Children.Add(CreateRankProgress(player, rankResult));
        info.Children.Add(CreateStatsLabel(player));
        info.Children.Add(CreateWinRateLabel(player, rankResult));
        info.Children.Add(CreatePlayerButtons(player));

        return info;
    }

    View CreatePlayerHeader(int rank, PlayerProfileModel player)
    {
        return new Label
        {
            Text = $"#{rank}  {player.PlayerName}",
            TextColor = Colors.White,
            FontSize = 19,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };
    }

    View CreateStatusRankLabel(
        PlayerProfileModel player,
        PlayerRankResult rankResult,
        PlayerVisualIdentity? identity)
    {
        string statusText =
            PlayerEngine.GetStatusDisplayName(player.ProfileStatus);

        string statusColor =
            PlayerEngine.GetStatusColor(player.ProfileStatus);

        return new Label
        {
            Text = identity?.Title == null
                ? $"{statusText}  •  {rankResult.DisplayName}"
                : $"{statusText}  •  {rankResult.DisplayName}  •  {identity.Title.DisplayName}",
            TextColor = Color.FromArgb(statusColor),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        };
    }

    View CreateXpLabel(PlayerProfileModel player)
    {
        return new Label
        {
            Text = $"Level {player.PlayerLevel}   |   XP {player.PlayerXP}   |   Legacy {player.LegacyScore}",
            TextColor = Color.FromArgb("#D4AF37"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        };
    }

    View CreateRankProgress(
        PlayerProfileModel player,
        PlayerRankResult rankResult)
    {
        string statusColor =
            PlayerEngine.GetStatusColor(player.ProfileStatus);

        return new ProgressBar
        {
            Progress = rankResult.Progress,
            ProgressColor = Color.FromArgb(statusColor),
            BackgroundColor = Color.FromArgb("#2A2A2A"),
            HeightRequest = 8
        };
    }

    View CreateStatsLabel(PlayerProfileModel player)
    {
        return new Label
        {
            Text = $"المباريات: {player.TotalMatches}   |   الفوز: {player.Wins}   |   الخسارة: {player.Losses}",
            TextColor = Color.FromArgb("#CCCCCC"),
            FontSize = 12
        };
    }

    View CreateWinRateLabel(
        PlayerProfileModel player,
        PlayerRankResult rankResult)
    {
        return new Label
        {
            Text = $"Win Rate: {player.WinRate:0.##}%   |   متبقي للترقية: {rankResult.RemainingXP} XP",
            TextColor = Color.FromArgb("#C8B58A"),
            FontSize = 12
        };
    }

    View CreatePlayerButtons(PlayerProfileModel player)
    {
        HorizontalStackLayout buttons =
            new()
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Start
            };

        Button detailsButton =
            new()
            {
                Text = "التفاصيل",
                BackgroundColor = Color.FromArgb("#D4AF37"),
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                Padding = new Thickness(10, 4)
            };

        detailsButton.Clicked += async (s, e) =>
        {
            await Navigation.PushAsync(
                new PlayerDetailsPage(player.PlayerId));
        };

        buttons.Children.Add(detailsButton);

        if (canManagePlayers)
        {
            Button deleteButton =
                new()
                {
                    Text = "حذف",
                    BackgroundColor = Color.FromArgb("#C62828"),
                    TextColor = Colors.White,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 12,
                    Padding = new Thickness(10, 4)
                };

            deleteButton.Clicked += async (s, e) =>
            {
                await DeleteSinglePlayerAsync(player);
            };

            buttons.Children.Add(deleteButton);
        }

        return buttons;
    }

    async Task DeleteSinglePlayerAsync(PlayerProfileModel player)
    {
        bool confirm =
            await DisplayAlert(
                "حذف اللاعب",
                $"هل تريد حذف اللاعب ({player.PlayerName}) نهائياً؟\n\nسيتم حذف ملف اللاعب وإزالة PlayerId من الفرق الحالية.\n\nسجل المباريات التاريخي لن يتم حذفه.",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        await PlayerManagementService.DeletePlayerAsync(player.PlayerId);

        await LoadPlayersAsync();

        await DisplayAlert(
            "تم",
            "تم حذف اللاعب بنجاح",
            "حسناً");
    }

    async void OnDeleteAllPlayersClicked(object? sender, EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "حذف جميع اللاعبين",
                "سيتم حذف جميع ملفات اللاعبين وإزالة PlayerId من الفرق الحالية.\n\nسجل المباريات التاريخي لن يتم حذفه.\n\nهل تريد المتابعة؟",
                "متابعة",
                "إلغاء");

        if (!confirm)
            return;

        string typed =
            await DisplayPromptAsync(
                "تأكيد نهائي",
                "اكتب DELETE PLAYERS لتأكيد الحذف:",
                "حذف",
                "إلغاء");

        if (typed != "DELETE PLAYERS")
        {
            await DisplayAlert(
                "تم الإلغاء",
                "لم يتم حذف اللاعبين.",
                "حسناً");

            return;
        }

        await PlayerManagementService.DeleteAllPlayersAsync();

        await LoadPlayersAsync();

        await DisplayAlert(
            "تم",
            "تم حذف جميع اللاعبين بنجاح",
            "حسناً");
    }

    async void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        string text =
            e.NewTextValue?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            await RenderPlayers(allPlayers);
            return;
        }

        await RenderPlayers(
            allPlayers
                .Where(x =>
                    x.PlayerName.Contains(
                        text,
                        StringComparison.OrdinalIgnoreCase))
                .ToList());
    }

    async void OnBackTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}







