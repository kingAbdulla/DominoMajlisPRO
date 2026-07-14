using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.GalleryEngine.Components.StoreSections;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.Pages;

public partial class PlayerDetailsPage : ContentPage
{
    readonly string _playerId;

    PlayerProfileModel? currentPlayer;
    AvatarItemModel? selectedAvatar;
    string selectedCategory = "All";
    List<AvatarItemModel> availableAvatars = new();
    HashSet<string> ownedStoreAvatarIds =
        new(StringComparer.OrdinalIgnoreCase);
    string equippedStoreAvatarId = "";
    const int TimelinePageSize = 10;
    int timelineVisibleCount = TimelinePageSize;
    IReadOnlyList<PlayerTimelineItemModel> timelineItems =
        Array.Empty<PlayerTimelineItemModel>();

    public PlayerDetailsPage(string playerId)
    {
        InitializeComponent();
        _playerId = playerId;
    }
    void BuildAchievements(PlayerProfileModel player)
    {
        AchievementsContainer.Children.Clear();

        var achievements =
            PlayerAchievementService.GetAchievements(player);

        foreach (var achievement in achievements)
        {
            AchievementsContainer.Children.Add(
                CreateAchievementCard(achievement));
        }
    }

    View CreateAchievementCard(PlayerAchievementModel achievement)
    {
        Color strokeColor =
            achievement.IsUnlocked
                ? Color.FromArgb("#D4AF37")
                : Color.FromArgb("#444444");

        Color textColor =
            achievement.IsUnlocked
                ? Color.FromArgb("#D4AF37")
                : Color.FromArgb("#888888");

        double opacity =
            achievement.IsUnlocked ? 1.0 : 0.35;

        return new Border
        {
            BackgroundColor = Color.FromArgb("#151515"),
            Stroke = strokeColor,
            StrokeThickness = 1.2,
            Padding = 10,
            StrokeShape =
                new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 18
                },

            Content =
                new Grid
                {
                    ColumnDefinitions =
                    {
                    new ColumnDefinition { Width = 52 },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 60 }
                    },
                    ColumnSpacing = 10,
                    Children =
                    {
                    new Image
                    {
                        Source = achievement.Icon,
                        WidthRequest = 42,
                        HeightRequest = 42,
                        Aspect = Aspect.AspectFit,
                        Opacity = opacity,
                        VerticalOptions = LayoutOptions.Center
                    },

                    CreateAchievementTextBlock(achievement, textColor),

                    CreateAchievementStatusBlock(achievement)
                    }
                }
        };
    }

    View CreateAchievementTextBlock(
        PlayerAchievementModel achievement,
        Color textColor)
    {
        VerticalStackLayout layout =
            new()
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center
            };

        layout.Children.Add(
            new Label
            {
                Text = achievement.Title,
                TextColor = textColor,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.End,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        layout.Children.Add(
            new Label
            {
                Text = achievement.Description,
                TextColor = Color.FromArgb("#AAAAAA"),
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.End,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.WordWrap
            });

        Grid.SetColumn(layout, 1);

        return layout;
    }

    View CreateAchievementStatusBlock(
        PlayerAchievementModel achievement)
    {
        VerticalStackLayout layout =
            new()
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

        layout.Children.Add(
            new Label
            {
                Text = achievement.IsUnlocked ? "✓" : "🔒",
                TextColor =
                    achievement.IsUnlocked
                        ? Color.FromArgb("#00C853")
                        : Color.FromArgb("#777777"),
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            });

        layout.Children.Add(
            new Label
            {
                Text = achievement.ProgressText,
                TextColor = Color.FromArgb("#D4AF37"),
                FontSize = 10,
                HorizontalTextAlignment = TextAlignment.Center
            });

        Grid.SetColumn(layout, 2);

        return layout;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.CurrentUserChanged -= OnCurrentUserChanged;
        AppEvents.CurrentUserChanged += OnCurrentUserChanged;
        AppEvents.StoreProgressChanged -= OnStoreProgressChanged;
        AppEvents.StoreProgressChanged += OnStoreProgressChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        StoreAssetQueryService.PublishedContentChanged -= OnPublishedContentChanged;
        StoreAssetQueryService.PublishedContentChanged += OnPublishedContentChanged;

        await LoadPlayerAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        AppEvents.CurrentUserChanged -= OnCurrentUserChanged;
        AppEvents.StoreProgressChanged -= OnStoreProgressChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        StoreAssetQueryService.PublishedContentChanged -= OnPublishedContentChanged;
    }

    async void OnCurrentUserChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(LoadPlayerAsync);
    }

    async void OnStoreProgressChanged(string playerId)
    {
        if (!SameId(playerId, _playerId))
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await LoadPlayerAsync();
            if (AvatarPickerOverlay.IsVisible)
            {
                await LoadAvailableAvatarsAsync();
                BuildAvatarCategories();
                LoadAvatars(selectedCategory);
            }
        });
    }

    async void OnTeamAssetsChanged(string teamId)
    {
        if (currentPlayer == null)
            return;

        var team =
            await TeamProfileService.GetTeamByPlayerIdAsync(
                currentPlayer.PlayerId);
        if (!SameId(team?.TeamId, teamId))
            return;

        await MainThread.InvokeOnMainThreadAsync(LoadPlayerAsync);
    }

    async void OnPublishedContentChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await LoadPlayerAsync();
            if (AvatarPickerOverlay.IsVisible)
            {
                await LoadAvailableAvatarsAsync();
                BuildAvatarCategories();
                LoadAvatars(selectedCategory);
            }
        });
    }

    async Task LoadPlayerAsync()
    {
        await PlayerTeamSyncService.SyncPlayersFromTeamsAsync();
        currentPlayer =
            await PlayerProfileService.GetPlayerByIdAsync(_playerId);

        if (currentPlayer == null)
        {
            await DisplayAlert("خطأ", "لم يتم العثور على ملف اللاعب.", "حسناً");
            await Navigation.PopAsync();
            return;
        }

        PlayerEngine.Normalize(currentPlayer);
        var visualIdentity =
            await PlayerVisualIdentityResolver.ResolveAsync(
                currentPlayer.PlayerId);
        await ApplyEquippedProfileBackgroundAsync(
            currentPlayer.PlayerId,
            visualIdentity);

        var rank =
            PlayerRankService.Calculate(currentPlayer.PlayerXP);

        PlayerAvatarImage.Source =
            PlayerProfileService.GetPlayerImageSource(currentPlayer);

        AvatarPreviewImage.Source =
            PlayerProfileService.GetPlayerImageSource(currentPlayer);
        PlayerNameLabel.Text = currentPlayer.PlayerName;
        PlayerNamePlate.OwnerId = currentPlayer.PlayerId;
        PlayerNamePlate.DisplayText = currentPlayer.PlayerName;
        PlayerNamePlate.IsVisible = true;
        PlayerNameLabel.IsVisible = false;

        string identityRole =
            await ResolveIdentityRoleAsync(currentPlayer);
        PlayerStatusLabel.Text = visualIdentity.Title == null
            ? identityRole
            : $"{identityRole} • {visualIdentity.Title.DisplayName}";
        ApplyAvatarIdentityVisuals(visualIdentity);

        PlayerRankLabel.Text =
            rank.DisplayName;

        RankProgressBar.Progress =
            rank.Progress;

        RankProgressLabel.Text =
            $"متبقي للترقية: {rank.RemainingXP} XP";

        LevelLabel.Text =
            currentPlayer.PlayerLevel.ToString();

        XpLabel.Text =
            currentPlayer.PlayerXP.ToString();

        LegacyLabel.Text =
            currentPlayer.LegacyScore.ToString();

        TotalMatchesLabel.Text =
            currentPlayer.TotalMatches.ToString();

        WinsLabel.Text =
            currentPlayer.Wins.ToString();

        LossesLabel.Text =
            currentPlayer.Losses.ToString();

        WinRateLabel.Text =
            $"{currentPlayer.WinRate:0.##}%";

        HighestScoreLabel.Text =
            currentPlayer.HighestScore.ToString();

        HallOfFameLabel.Text =
            currentPlayer.HallOfFameCount.ToString();

        BuildHonors(currentPlayer, identityRole);
        BuildAchievements(currentPlayer);
        await PlayerProfileService.UpdatePlayerProfileAsync(currentPlayer);
        await BuildIdentityHistoryAsync(currentPlayer);
        await BuildTimeline(currentPlayer);
    }

    async Task ApplyEquippedProfileBackgroundAsync(
        string playerId,
        PlayerVisualIdentity? identity = null)
    {
        identity ??=
            await PlayerVisualIdentityResolver.ResolveAsync(playerId);
        var background = identity.ProfileBackground;
        if (background == null)
        {
            HeroProfileBackgroundImage.Source = null;
            HeroProfileBackgroundImage.IsVisible = false;
            return;
        }

        var imagePath = background.PreviewImage;
        HeroProfileBackgroundImage.Source =
            ToImageSource(imagePath);
        HeroProfileBackgroundImage.IsVisible =
            !string.IsNullOrWhiteSpace(imagePath);
    }

    void ApplyAvatarIdentityVisuals(PlayerVisualIdentity identity)
    {
        var frameSource =
            ToImageSource(identity.Frame?.PreviewImage);
        AvatarFrameOverlay.Source = frameSource;
        AvatarFrameOverlay.IsVisible = frameSource != null;
        PlayerEffectEngine.Apply(AvatarEffectOverlay, identity.Effect, 1.18);
        AvatarFrame.Stroke = identity.Frame == null
            ? Color.FromArgb("#D4AF37")
            : Colors.Transparent;
        AvatarFrame.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(
                identity.Effect == null
                    ? Color.FromArgb("#D4AF37")
                    : Color.FromArgb("#F2C14E")),
            Radius = identity.Effect == null ? 18 : 24,
            Opacity = identity.Effect == null ? 0.45f : 0.65f
        };
    }

    static ImageSource? ResolveAvatarSource(
        PlayerVisualIdentity identity)
    {
        var path = identity.Avatar?.PreviewImage;
        return ToImageSource(path);
    }

    static ImageSource? ToImageSource(string? path) =>
        InventoryDisplayResolver.ResolveOptionalImageSource(path);

    // Build timeline based on player properties
    async Task BuildTimeline(PlayerProfileModel player)
    {
        timelineVisibleCount = TimelinePageSize;
        timelineItems =
            await PlayerTimelineService.BuildTimelineAsync(player);
        RenderTimeline();
    }

    void RenderTimeline()
    {
        TimelineContainer.Children.Clear();
        DeleteAllTimelineButton.IsVisible =
            timelineItems.Any(item => item.IsIdentityEvent);

        if (timelineItems.Count == 0)
        {
            TimelineContainer.Children.Add(
                new Label
                {
                    Text = "لا توجد أحداث في السجل حالياً",
                    TextColor = Color.FromArgb("#AAAAAA"),
                    FontSize = 13,
                    HorizontalTextAlignment = TextAlignment.Center
                });

            return;
        }

        foreach (var item in timelineItems.Take(timelineVisibleCount))
        {
            TimelineContainer.Children.Add(
                CreateTimelineCard(item));
        }

        int remaining = Math.Max(
            0,
            timelineItems.Count - timelineVisibleCount);
        if (remaining <= 0)
            return;

        var showMore = new Button
        {
            Text = $"عرض المزيد ({remaining})",
            BackgroundColor = Color.FromArgb("#1A1A1A"),
            TextColor = Color.FromArgb("#D4AF37"),
            BorderColor = Color.FromArgb("#8A5B27"),
            BorderWidth = 1,
            CornerRadius = 14,
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.Fill
        };
        showMore.Clicked += (_, _) =>
        {
            timelineVisibleCount += TimelinePageSize;
            RenderTimeline();
        };
        TimelineContainer.Children.Add(showMore);
    }

    View CreateTimelineCard(PlayerTimelineItemModel item)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 42 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };
        grid.Add(
            new Label
            {
                Text = item.Icon,
                FontSize = 22,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            },
            0,
            0);
        grid.Add(CreateTimelineTextBlock(item), 1, 0);
        if (item.IsIdentityEvent)
        {
            var deleteButton = new Button
            {
                Text = "✕",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#FF7777"),
                FontSize = 13,
                Padding = new Thickness(6, 2),
                MinimumWidthRequest = 30,
                MinimumHeightRequest = 30,
                VerticalOptions = LayoutOptions.Center
            };
            deleteButton.Clicked += async (_, _) =>
                await DeleteTimelineEventAsync(item);
            grid.Add(deleteButton, 2, 0);
        }

        return new Border
        {
            BackgroundColor = Color.FromArgb("#151515"),
            Stroke = Color.FromArgb(item.ColorHex),
            StrokeThickness = 1.1,
            Padding = 10,
            StrokeShape =
                new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 18
                },

            Content = grid
        };
    }

    View CreateTimelineTextBlock(PlayerTimelineItemModel item)
    {
        VerticalStackLayout layout =
            new()
            {
                Spacing = 3,
                VerticalOptions = LayoutOptions.Center
            };

        layout.Children.Add(
            new Label
            {
                Text = item.Title,
                TextColor = Color.FromArgb(item.ColorHex),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.End
            });

        layout.Children.Add(
            new Label
            {
                Text = item.Details,
                TextColor = Colors.White,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.End,
                LineBreakMode = LineBreakMode.WordWrap
            });

        layout.Children.Add(
            new Label
            {
                Text = item.Date.ToString("yyyy/MM/dd HH:mm"),
                TextColor = Color.FromArgb("#888888"),
                FontSize = 10,
                HorizontalTextAlignment = TextAlignment.End
            });

        return layout;
    }

    async Task DeleteTimelineEventAsync(PlayerTimelineItemModel item)
    {
        if (currentPlayer == null || !item.IsIdentityEvent)
            return;

        bool confirmed = await DisplayAlertAsync(
            "حذف حدث الهوية",
            "هل تريد حذف هذا الحدث من سجل الهوية؟",
            "حذف",
            "إلغاء");
        if (!confirmed ||
            !PlayerTimelineService.DeleteIdentityEvent(
                currentPlayer,
                item.EventId))
        {
            return;
        }

        await PlayerProfileService.UpdatePlayerProfileAsync(currentPlayer);
        await BuildTimeline(currentPlayer);
    }

    async void OnDeleteAllTimelineClicked(
        object? sender,
        EventArgs e)
    {
        if (currentPlayer == null)
            return;

        bool confirmed = await DisplayAlertAsync(
            "حذف سجل الهوية",
            "هل تريد حذف جميع أحداث الهوية؟ لا يؤثر هذا على بيانات اللاعب أو سجل المباريات.",
            "حذف السجل",
            "إلغاء");
        if (!confirmed ||
            !PlayerTimelineService.DeleteAllIdentityEvents(currentPlayer))
        {
            return;
        }

        await PlayerProfileService.UpdatePlayerProfileAsync(currentPlayer);
        await BuildTimeline(currentPlayer);
    }
    // Build honors based on player properties
    void BuildHonors(
        PlayerProfileModel player,
        string identityRole)
    {
        HonorsContainer.Children.Clear();

        HonorsContainer.Children.Add(
            CreateHonorChip(identityRole));

        if (player.IsEarlyAdopter)
            HonorsContainer.Children.Add(CreateHonorChip("Early"));

        if (player.IsSeasonVeteran)
            HonorsContainer.Children.Add(CreateHonorChip("Veteran"));

    }

    static async Task<string> ResolveIdentityRoleAsync(
        PlayerProfileModel player)
    {
        try
        {
            var user =
                await ApplicationUserService.GetCurrentUserAsync();
            if (user.Role != ApplicationUserRole.Ghost &&
                SameId(user.PlayerId, player.PlayerId))
            {
                return user.Role switch
                {
                    ApplicationUserRole.Developer => "Developer",
                    ApplicationUserRole.Founder => "Founder",
                    ApplicationUserRole.Honor => "Honor",
                    ApplicationUserRole.Member => "Member",
                    _ => "Member"
                };
            }
        }
        catch
        {
            // Fall back to the existing player profile role.
        }

        return PlayerEngine.GetStatusDisplayName(
            player.ProfileStatus);
    }

    async void OnAccountHubTapped(
        object? sender,
        TappedEventArgs e)
    {
        if (Navigation.NavigationStack.Count >= 2 &&
            Navigation.NavigationStack[^2] is PlayerProfilesPage)
        {
            await Navigation.PopAsync();
            return;
        }

        await Navigation.PushAsync(new PlayerProfilesPage());
    }

    static bool SameId(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(
            left.Trim(),
            right.Trim(),
            StringComparison.OrdinalIgnoreCase);

    View CreateHonorChip(string text)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#151515"),
            Stroke = Color.FromArgb("#D4AF37"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 6),
            StrokeShape =
                new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 16
                },
            Content =
                new Label
                {
                    Text = text,
                    TextColor = Color.FromArgb("#D4AF37"),
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold
                }
        };
    }

    async void OnOpenAvatarPicker(object sender, EventArgs e)
    {
        selectedAvatar = null;
        selectedCategory = "All";

        await LoadAvailableAvatarsAsync();
        BuildAvatarCategories();
        LoadAvatars("All");

        AvatarPreviewImage.Source =
            currentPlayer == null
                ? "player_card.png"
                : PlayerProfileService.GetPlayerImageSource(currentPlayer);

        AvatarPickerOverlay.IsVisible = true;
    }

    void BuildAvatarCategories()
    {
        AvatarCategoryContainer.Children.Clear();

        var categories = AvatarService.GetCategories();
        if (ownedStoreAvatarIds.Count > 0)
        {
            categories.Add(
                OwnedAssetCategoryCatalog.Get("Avatar").DisplayName);
        }

        foreach (string category in categories)
        {
            Button button =
                new()
                {
                    Text = category,
                    BackgroundColor =
                        category == selectedCategory
                            ? Color.FromArgb("#D4AF37")
                            : Color.FromArgb("#151515"),
                    TextColor =
                        category == selectedCategory
                            ? Colors.Black
                            : Color.FromArgb("#D4AF37"),
                    BorderColor = Color.FromArgb("#D4AF37"),
                    BorderWidth = 1,
                    CornerRadius = 15,
                    FontAttributes = FontAttributes.Bold,
                    Padding = new Thickness(14, 6)
                };

            button.Clicked +=
                (s, e) =>
                {
                    selectedCategory = category;
                    selectedAvatar = null;
                    BuildAvatarCategories();
                    LoadAvatars(category);
                };

            AvatarCategoryContainer.Children.Add(button);
        }
    }

    void LoadAvatars(string category)
    {
        AvatarCollection.SelectedItem = null;

        var filtered =
            string.IsNullOrWhiteSpace(category) || category == "All"
                ? availableAvatars
                : availableAvatars
                    .Where(item => item.Category == category)
                    .ToList();

        AvatarCollection.ItemsSource = filtered;

        var equipped = filtered.FirstOrDefault(item =>
            string.Equals(
                item.Id,
                equippedStoreAvatarId,
                StringComparison.OrdinalIgnoreCase));
        if (equipped != null)
        {
            AvatarCollection.SelectedItem = equipped;
            selectedAvatar = equipped;
        }
    }

    async Task LoadAvailableAvatarsAsync()
    {
        var ownedCategory =
            OwnedAssetCategoryCatalog.Get("Avatar");
        var byId = AvatarService.GetAll()
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .ToDictionary(
                item => item.Id,
                item => item,
                StringComparer.OrdinalIgnoreCase);

        ownedStoreAvatarIds.Clear();
        equippedStoreAvatarId = "";

        if (currentPlayer != null &&
            !string.IsNullOrWhiteSpace(currentPlayer.PlayerId))
        {
            var owned =
                (await PlayerInventoryService.LoadOwnedAsync(
                    currentPlayer.PlayerId))
                .Where(item =>
                    string.Equals(
                        StoreAssetCatalogService.CanonicalTypeId(
                            item.StoreTypeId),
                        ownedCategory.AssetType,
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(item.AssetId))
                .GroupBy(
                    item => item.AssetId,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(item => item.IsEquipped)
                    .ThenByDescending(item => item.PurchasedAt)
                    .First())
                .ToList();

            var catalog = await StoreAssetQueryService.LoadAvatarsAsync();
            var catalogById = catalog
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(
                    item => item.Id,
                    item => item,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var inventoryItem in owned)
            {
                if (!catalogById.TryGetValue(
                        inventoryItem.AssetId,
                        out var asset))
                {
                    byId[inventoryItem.AssetId] = new AvatarItemModel
                    {
                        Id = inventoryItem.AssetId,
                        Category = ownedCategory.DisplayName,
                        DisplayName =
                            StoreAssetCatalogService.IncompleteDisplayName,
                        Image = string.Empty,
                        IsUnlocked = true
                    };
                    continue;
                }

                ownedStoreAvatarIds.Add(asset.Id);
                if (inventoryItem.IsEquipped)
                    equippedStoreAvatarId = asset.Id;

                byId[asset.Id] = new AvatarItemModel
                {
                    Id = asset.Id,
                    Category = ownedCategory.DisplayName,
                    DisplayName =
                        string.IsNullOrWhiteSpace(asset.NameAr)
                            ? asset.NameEn
                            : asset.NameAr,
                    Image =
                        string.IsNullOrWhiteSpace(asset.ThumbnailPath)
                            ? asset.ImagePath
                            : asset.ThumbnailPath,
                    IsUnlocked = true
                };
            }
        }

        availableAvatars = byId.Values
            .OrderBy(item =>
                item.Category == ownedCategory.DisplayName ? 0 : 1)
            .ThenBy(item => item.Category)
            .ThenBy(item => item.DisplayName)
            .ToList();
    }

    void OnAvatarSelected(
        object sender,
        SelectionChangedEventArgs e)
    {
        selectedAvatar =
            e.CurrentSelection
            .FirstOrDefault()
            as AvatarItemModel;

        if (selectedAvatar != null)
        {
            AvatarPreviewImage.Source =
                selectedAvatar.ImageSource;
        }
    }

    async void OnSaveAvatarClicked(
        object sender,
        EventArgs e)
    {
        if (currentPlayer == null)
            return;

        if (selectedAvatar == null)
        {
            await DisplayAlert(
                "تنبيه",
                "يرجى اختيار Avatar أولاً.",
                "حسناً");

            return;
        }

        var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            if (ownedStoreAvatarIds.Contains(selectedAvatar.Id))
            {
                var equipTask = StoreEquipService.EquipAsync(
                    currentPlayer.PlayerId,
                    selectedAvatar.Id);
                var completed = await Task.WhenAny(equipTask, Task.Delay(-1, cts.Token));
                if (completed != equipTask)
                    throw new OperationCanceledException("Equip timed out.");
                await equipTask;
            }
            else
            {
                var setTask = PlayerProfileService.SetBuiltInAvatarAsync(
                    currentPlayer.PlayerId,
                    selectedAvatar.Image);
                var completed = await Task.WhenAny(setTask, Task.Delay(-1, cts.Token));
                if (completed != setTask)
                    throw new OperationCanceledException("Set avatar timed out.");
                await setTask;
            }
        }
        catch (OperationCanceledException)
        {
            await DisplayAlert("تنبيه", "انتهت مهلة تجهيز الصورة الرمزية. حاول مرة أخرى بعد لحظات.", "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", $"فشل حفظ الصورة الرمزية:\n{ex.Message}", "حسناً");
        }
        finally
        {
            // Trigger profile/data refresh only after persistence completed to avoid loops.
            AppEvents.RaisePlayerProfileChanged();
            AppEvents.RaiseDataChanged();

            AvatarPickerOverlay.IsVisible = false;

            await LoadPlayerAsync();
        }
    }

    async void OnPickFromDeviceClicked(
        object sender,
        EventArgs e)
    {
        if (currentPlayer == null)
            return;

        try
        {
            FileResult? result =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle = "اختر صورة اللاعب من الجهاز",
                        FileTypes = FilePickerFileType.Images
                    });

            if (result == null)
                return;

            await PlayerProfileService.SetProfileImageFromDeviceAsync(
                currentPlayer.PlayerId,
                result);

            AppEvents.RaisePlayerProfileChanged();
            AppEvents.RaiseDataChanged();

            AvatarPickerOverlay.IsVisible = false;

            await LoadPlayerAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشل اختيار صورة اللاعب:\n{ex.Message}",
                "حسناً");
        }
    }

    void OnCloseAvatarPicker(object sender, EventArgs e)
    {
        AvatarPickerOverlay.IsVisible = false;
    }

    async void OnBackTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    async Task BuildIdentityHistoryAsync(PlayerProfileModel player)
    {
        IdentityLastUpdateLabel.Text =
            $"آخر تحديث للهوية: {player.LastUpdatedAt:yyyy/MM/dd HH:mm}";

        CurrentTeamsLabel.Text =
            await GetCurrentTeamNamesAsync(player.CurrentTeamIds);

        LastRankHistoryLabel.Text =
            GetLastHistoryValue(player.RankHistory);

        LastXPHistoryLabel.Text =
            GetLastHistoryValue(player.XPHistory);

        LastAchievementHistoryLabel.Text =
            GetLastHistoryValue(player.AchievementHistory);
    }

    async Task<string> GetCurrentTeamNamesAsync(string teamIds)
    {
        if (string.IsNullOrWhiteSpace(teamIds))
            return "لا توجد فرق حالية";

        var teams =
            await TeamProfileService.LoadTeamsAsync();

        var ids =
            teamIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        var names =
            teams
            .Where(x => ids.Contains(x.TeamId))
            .Select(x => x.TeamName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        return names.Count == 0
            ? "لا توجد فرق حالية"
            : string.Join("، ", names);
    }

    string GetLastHistoryValue(string history)
    {
        if (string.IsNullOrWhiteSpace(history))
            return "—";

        string last =
            history
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault() ?? "";

        string[] parts =
            last.Split('|');

        return parts.Length >= 2
            ? parts[1]
            : last;
    }

    string GetLastHistory(string history)
    {
        if (string.IsNullOrWhiteSpace(history))
            return "—";

        return history
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault() ?? "—";
    }
}



