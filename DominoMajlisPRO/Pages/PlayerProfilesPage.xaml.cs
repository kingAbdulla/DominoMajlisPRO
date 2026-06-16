using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class PlayerProfilesPage : ContentPage
{
    List<PlayerProfileModel> allPlayers = new();
    bool canManagePlayers = false;

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

        await LoadPlayersAsync();
    }

    async void OnPlayerProfileChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await LoadPlayersAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        AppEvents.PlayerProfileChanged -= OnPlayerProfileChanged;
        AppEvents.DataChanged -= OnPlayerProfileChanged;
    }

    async Task LoadPlayersAsync()
    {
        canManagePlayers =
            await PlayerManagementService.CanManagePlayersAsync();

        await PlayerTeamSyncService.SyncPlayersFromTeamsAsync();

        allPlayers =
            await PlayerProfileService.LoadPlayersAsync();

        RenderPlayers(allPlayers);
    }

    void RenderPlayers(List<PlayerProfileModel> players)
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

        int rank = 1;

        foreach (var player in players.OrderByDescending(x => x.Wins))
        {
            PlayersContainer.Children.Add(
                CreatePlayerCard(rank, player));

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

    View CreatePlayerCard(int rank, PlayerProfileModel player)
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

        var avatarSection = CreateAvatarSection(player);
        Grid.SetColumn(avatarSection, 0);
        root.Children.Add(avatarSection);

        var infoSection = CreatePlayerInfoSection(rank, player, rankResult);
        Grid.SetColumn(infoSection, 1);
        root.Children.Add(infoSection);

        card.Content = root;

        return card;
    }

    View CreateAvatarSection(PlayerProfileModel player)
    {
        string statusColor =
            PlayerEngine.GetStatusColor(player.ProfileStatus);

        return new Border
        {
            WidthRequest = 92,
            HeightRequest = 92,
            BackgroundColor = Color.FromArgb("#151515"),
            Stroke = Color.FromArgb(statusColor),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Content =
                new Image
                {
                    Source = PlayerProfileService.GetPlayerImageSource(player),
                    Aspect = Aspect.AspectFill,
                    WidthRequest = 92,
                    HeightRequest = 92
                }
        };
    }

    View CreatePlayerInfoSection(
        int rank,
        PlayerProfileModel player,
        PlayerRankResult rankResult)
    {
        VerticalStackLayout info =
            new()
            {
                Spacing = 7
            };

        info.Children.Add(CreatePlayerHeader(rank, player));
        info.Children.Add(CreateStatusRankLabel(player, rankResult));
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
        PlayerRankResult rankResult)
    {
        string statusText =
            PlayerEngine.GetStatusDisplayName(player.ProfileStatus);

        string statusColor =
            PlayerEngine.GetStatusColor(player.ProfileStatus);

        return new Label
        {
            Text = $"{statusText}  •  {rankResult.DisplayName}",
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

    void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        string text =
            e.NewTextValue?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            RenderPlayers(allPlayers);
            return;
        }

        RenderPlayers(
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