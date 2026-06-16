using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class PlayerDetailsPage : ContentPage
{
    readonly string _playerId;

    PlayerProfileModel? currentPlayer;
    AvatarItemModel? selectedAvatar;
    string selectedCategory = "All";

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
        await LoadPlayerAsync();
    }

    async Task LoadPlayerAsync()
    {
        await PlayerTeamSyncService.SyncPlayersFromTeamsAsync();
        currentPlayer =
            await PlayerProfileService.GetPlayerByIdAsync(_playerId);

        if (currentPlayer == null)
        {
            await DisplayAlert("خطأ", "لم يتم العثور على اللاعب", "حسناً");
            await Navigation.PopAsync();
            return;
        }

        PlayerEngine.Normalize(currentPlayer);

        var rank =
            PlayerRankService.Calculate(currentPlayer.PlayerXP);

        PlayerAvatarImage.Source =
            PlayerProfileService.GetPlayerImageSource(currentPlayer);

        AvatarPreviewImage.Source =
            PlayerProfileService.GetPlayerImageSource(currentPlayer);

        PlayerNameLabel.Text = currentPlayer.PlayerName;

        PlayerStatusLabel.Text =
            PlayerEngine.GetStatusDisplayName(currentPlayer.ProfileStatus);

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

        BuildHonors(currentPlayer);
        BuildAchievements(currentPlayer);
        await PlayerProfileService.UpdatePlayerProfileAsync(currentPlayer);
        await BuildIdentityHistoryAsync(currentPlayer);
        await BuildTimeline(currentPlayer);
    }
    // Build timeline based on player properties
    async Task BuildTimeline(PlayerProfileModel player)
    {
        TimelineContainer.Children.Clear();

        var items =
            await PlayerTimelineService.BuildTimelineAsync(player);

        if (items.Count == 0)
        {
            TimelineContainer.Children.Add(
                new Label
                {
                    Text = "لا يوجد سجل نشاط بعد",
                    TextColor = Color.FromArgb("#AAAAAA"),
                    FontSize = 13,
                    HorizontalTextAlignment = TextAlignment.Center
                });

            return;
        }

        foreach (var item in items)
        {
            TimelineContainer.Children.Add(
                CreateTimelineCard(item));
        }
    }

    View CreateTimelineCard(PlayerTimelineItemModel item)
    {
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

            Content =
                new Grid
                {
                    ColumnDefinitions =
                    {
                    new ColumnDefinition { Width = 42 },
                    new ColumnDefinition { Width = GridLength.Star }
                    },
                    ColumnSpacing = 10,
                    Children =
                    {
                    new Label
                    {
                        Text = item.Icon,
                        FontSize = 22,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center
                    },

                    CreateTimelineTextBlock(item)
                    }
                }
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

        Grid.SetColumn(layout, 1);

        return layout;
    }
    // Build honors based on player properties
    void BuildHonors(PlayerProfileModel player)
    {
        HonorsContainer.Children.Clear();

        if (player.IsDeveloper)
            HonorsContainer.Children.Add(CreateHonorChip("Developer"));

        if (player.IsFounder)
            HonorsContainer.Children.Add(CreateHonorChip("Founder"));

        if (player.IsEarlyAdopter)
            HonorsContainer.Children.Add(CreateHonorChip("Early"));

        if (player.IsSeasonVeteran)
            HonorsContainer.Children.Add(CreateHonorChip("Veteran"));

        if (HonorsContainer.Children.Count == 0)
            HonorsContainer.Children.Add(CreateHonorChip("Normal"));
    }

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

    void OnOpenAvatarPicker(object sender, EventArgs e)
    {
        selectedAvatar = null;
        selectedCategory = "All";

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

        foreach (string category in AvatarService.GetCategories())
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

        AvatarCollection.ItemsSource =
            AvatarService.GetByCategory(category);
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
                selectedAvatar.Image;
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
                "اختر Avatar أولاً",
                "حسناً");

            return;
        }

        await PlayerProfileService.SetBuiltInAvatarAsync(
            currentPlayer.PlayerId,
            selectedAvatar.Image);

        AppEvents.RaisePlayerProfileChanged();
        AppEvents.RaiseDataChanged();

        AvatarPickerOverlay.IsVisible = false;

        await LoadPlayerAsync();
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
                        PickerTitle = "اختر صورة اللاعب",
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
                $"تعذر اختيار الصورة:\n{ex.Message}",
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
            $"آخر تحديث: {player.LastUpdatedAt:yyyy/MM/dd HH:mm}";

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
            return "لا يوجد";

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
            ? "لا يوجد"
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