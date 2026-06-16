using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using System.Reflection;

namespace DominoMajlisPRO.Pages;

public partial class HallOfFamePage : ContentPage
{
    List<SavedMatch> matches = new();
    List<TeamProfileModel> teams = new();
    List<PlayerProfileModel> players = new();

    const string CardGlass = "#151515";
    const string BronzeStroke = "#5B3B18";
    const string MutedText = "#C8B58A";

    public HallOfFamePage()
    {
        InitializeComponent();

        SideMenu.NavigationRequested -= OnSideMenuNavigation;
        SideMenu.NavigationRequested += OnSideMenuNavigation;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.DataChanged -= OnHallDataChanged;
        AppEvents.PlayerProfileChanged -= OnHallDataChanged;
        AppEvents.MatchesChanged -= OnHallDataChanged;
        AppEvents.TeamsChanged -= OnHallDataChanged;
        AppEvents.RankingsChanged -= OnHallDataChanged;

        AppEvents.DataChanged += OnHallDataChanged;
        AppEvents.PlayerProfileChanged += OnHallDataChanged;
        AppEvents.MatchesChanged += OnHallDataChanged;
        AppEvents.TeamsChanged += OnHallDataChanged;
        AppEvents.RankingsChanged += OnHallDataChanged;

        await LoadHallOfFameAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        AppEvents.DataChanged -= OnHallDataChanged;
        AppEvents.PlayerProfileChanged -= OnHallDataChanged;
        AppEvents.MatchesChanged -= OnHallDataChanged;
        AppEvents.TeamsChanged -= OnHallDataChanged;
        AppEvents.RankingsChanged -= OnHallDataChanged;
    }

    async void OnHallDataChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                await LoadHallOfFameAsync();
            });
    }

    async Task LoadHallOfFameAsync()
    {
        matches = await GameService.LoadMatchesAsync();
        teams = await TeamProfileService.LoadTeamsAsync();
        players = await PlayerProfileService.LoadPlayersAsync();

        HallContainer.Opacity = 0;

        ClearDynamicSections();

        if (matches.Count == 0)
        {
            LoadEmptyState();
            await AnimatePageAsync();
            return;
        }

        LoadHeroChampion();
        LoadTopTeams();
        LoadCandidates();
        await LoadTopPlayersAsync();
        LoadLegendaryRecords();
        LoadStatistics();

        await AnimatePageAsync();
    }

    void ClearDynamicSections()
    {
        TopTeamsContainer.Children.Clear();
        TopPlayersContainer.Children.Clear();
        RecordsContainer.Children.Clear();
        StatsContainer.Children.Clear();
        CandidatesContainer.Children.Clear();
    }

    void LoadEmptyState()
    {
        SeasonNumberLabel.Text = "—";

        HeroTeamNameLabel.Text = "لا توجد أسطورة";
        HeroSubtitleLabel.Text = "ابدأ أول مباراة ليظهر أبطال القاعة";
        HeroWinsLabel.Text = "0";
        HeroWinRateLabel.Text = "0%";
        HeroLegacyLabel.Text = "0";

        TopTeamsContainer.Children.Add(
            CreateEmptyCard("لا توجد فرق مؤهلة حالياً"));

        TopPlayersContainer.Children.Add(
            CreateEmptyCard("لا توجد بيانات لاعبين بعد"));

        RecordsContainer.Children.Add(CreateRecordEmptyGrid());

        AddStat("all_gold.png", "المرشحون", "0", 0, 0);
        AddStat("trophy_3d.png", "المؤهلون", "0", 1, 0);
        AddStat("joystick_gold.png", "المباريات", "0", 2, 0);
        AddStat("target_3d.png", "أعلى نتيجة", "0", 0, 1);
        AddStat("xp_gold.png", "Legacy", "0", 1, 1);
        AddStat("halloffame_gold.png", "الدستور", "Active", 2, 1);
    }

    void LoadHeroChampion()
    {
        var champion =
            GetEligibleHallTeams()
            .FirstOrDefault();

        SeasonNumberLabel.Text = GetCurrentSeasonText();

        if (champion == null)
        {
            HeroTeamNameLabel.Text = "لا توجد أسطورة";
            HeroSubtitleLabel.Text = "بانتظار فريق يحقق شروط الدستور";
            HeroWinsLabel.Text = "0";
            HeroWinRateLabel.Text = "0%";
            HeroLegacyLabel.Text = "0";
            return;
        }

        HeroTeamNameLabel.Text = champion.DisplayName;
        HeroSubtitleLabel.Text = "دخل قاعة الأساطير وفق الدستور";
        HeroWinsLabel.Text = champion.Wins.ToString();
        HeroWinRateLabel.Text = $"{champion.WinRate:0}%";
        HeroLegacyLabel.Text = champion.LegacyScore.ToString();
    }

    void LoadTopTeams()
    {
        var topTeams =
            GetEligibleHallTeams()
            .Take(6)
            .ToList();

        if (topTeams.Count == 0)
        {
            TopTeamsContainer.Children.Add(
                CreateEmptyCard("لا توجد فرق مؤهلة حالياً"));

            return;
        }

        int rank = 1;

        foreach (var team in topTeams)
        {
            TopTeamsContainer.Children.Add(
                CreateTeamLegendCard(
                    rank,
                    team.DisplayName,
                    team.Wins,
                    team.LegacyScore));

            rank++;
        }

        TopTeamsContainer.HorizontalOptions =
            topTeams.Count <= 2
                ? LayoutOptions.Center
                : LayoutOptions.End;
    }

    async Task LoadTopPlayersAsync()
    {
        players = await PlayerProfileService.LoadPlayersAsync();

        var topPlayers =
            players
            .Where(x => !string.IsNullOrWhiteSpace(x.PlayerName))
            .OrderByDescending(x => x.LegacyScore)
            .ThenByDescending(x => x.PlayerXP)
            .ThenByDescending(x => x.Wins)
            .Take(6)
            .ToList();

        if (topPlayers.Count == 0)
        {
            LoadFallbackPlayersFromTeams();
            return;
        }

        int rank = 1;

        foreach (var player in topPlayers)
        {
            TopPlayersContainer.Children.Add(
                CreatePlayerLegendCard(rank, player));

            rank++;
        }

        TopPlayersContainer.HorizontalOptions =
            topPlayers.Count <= 3
                ? LayoutOptions.Center
                : LayoutOptions.End;
    }

    void LoadFallbackPlayersFromTeams()
    {
        var playerNames =
            teams
            .SelectMany(x => new[] { x.Player1, x.Player2 })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(6)
            .ToList();

        if (playerNames.Count == 0)
        {
            TopPlayersContainer.Children.Add(
                CreateEmptyCard("لا توجد بيانات لاعبين بعد"));

            return;
        }

        int rank = 1;

        foreach (string name in playerNames)
        {
            TopPlayersContainer.Children.Add(
                CreatePlayerLegendCard(rank, name, 0, "Player"));

            rank++;
        }

        TopPlayersContainer.HorizontalOptions =
            playerNames.Count <= 3
                ? LayoutOptions.Center
                : LayoutOptions.End;
    }

    void LoadCandidates()
    {
        var candidates =
            GetTeamResults()
            .Where(x => !IsHallEligible(x))
            .OrderByDescending(x => x.LegacyScore)
            .Take(4)
            .ToList();

        foreach (var candidate in candidates)
        {
            CandidatesContainer.Children.Add(
                CreateCandidateRow(candidate));
        }
    }

    void LoadLegendaryRecords()
    {
        var teamResults =
            GetTeamResults()
            .OrderByDescending(x => x.Wins)
            .FirstOrDefault();

        var fastest =
            matches
            .Where(x => x.MatchDurationMinutes > 0)
            .OrderBy(x => x.MatchDurationMinutes)
            .FirstOrDefault();

        int highestScore =
            matches.Count == 0
                ? 0
                : matches.Max(x => Math.Max(x.Team1Score, x.Team2Score));

        var melesKing =
            matches
            .Where(x => x.HasMeles)
            .GroupBy(x => GetWinnerDisplayName(x))
            .OrderByDescending(x => x.Count())
            .FirstOrDefault();

        Grid recordsGrid =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnSpacing = 8,
                RowSpacing = 8
            };

        AddRecordCard(
            recordsGrid,
            "wins_gold.png",
            "أكثر فريق فاز",
            teamResults?.DisplayName ?? "—",
            teamResults?.Wins.ToString() ?? "0",
            0,
            0);

        AddRecordCard(
            recordsGrid,
            "meles_badge_gold.png",
            "ملك الملص",
            melesKing?.Key ?? "—",
            melesKing?.Count().ToString() ?? "0",
            1,
            0);

        AddRecordCard(
            recordsGrid,
            "fast_round_gold.png",
            "أسرع مباراة",
            "الزمن",
            fastest == null ? "—" : $"{fastest.MatchDurationMinutes} د",
            0,
            1);

        AddRecordCard(
            recordsGrid,
            "highest_score_gold.png",
            "أعلى نتيجة",
            "Score",
            highestScore.ToString(),
            1,
            1);

        RecordsContainer.Children.Add(recordsGrid);
    }

    void LoadStatistics()
    {
        var allResults = GetTeamResults();
        var eligible = GetEligibleHallTeams();

        int totalMatches = matches.Count;

        int highestScore =
            matches.Count == 0
                ? 0
                : matches.Max(x => Math.Max(x.Team1Score, x.Team2Score));

        AddStat("all_gold.png", "المرشحون", allResults.Count.ToString(), 0, 0);
        AddStat("trophy_3d.png", "المؤهلون", eligible.Count.ToString(), 1, 0);
        AddStat("joystick_gold.png", "المباريات", totalMatches.ToString(), 2, 0);
        AddStat("target_3d.png", "أعلى نتيجة", highestScore.ToString(), 0, 1);
        AddStat("xp_gold.png", "Legacy", eligible.Sum(x => x.LegacyScore).ToString(), 1, 1);
        AddStat("halloffame_gold.png", "الدستور", "Active", 2, 1);
    }

    View CreateTeamLegendCard(
        int rank,
        string teamName,
        int wins,
        int legacy)
    {
        string borderColor =
            rank == 1 ? "#D4AE62" :
            rank == 2 ? "#BFC3C7" :
            rank == 3 ? "#B87333" :
            "#765021";

        string shield =
            rank switch
            {
                1 => "crown_3d.png",
                2 => "wolf_3d.png",
                3 => "eagle_3d.png",
                4 => "lion_3d.png",
                5 => "horse_3d.png",
                _ => "shield_3d.png"
            };

        Border card =
            new()
            {
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 120 : 165,
                HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 180 : 235,
                BackgroundColor = Color.FromArgb(rank == 1 ? "#1B1005" : "#0C0C0C"),
                Stroke = Color.FromArgb(borderColor),
                StrokeThickness = rank == 1 ? 1.2 : 0.75,
                Padding = 8,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                Shadow =
                    new Shadow
                    {
                        Brush = new SolidColorBrush(Color.FromArgb(rank == 1 ? "#B8873C" : "#5B3B18")),
                        Radius = rank == 1 ? 8 : 4,
                        Opacity = rank == 1 ? 0.20f : 0.10f,
                        Offset = new Point(0, 2)
                    }
            };

        VerticalStackLayout layout =
            new()
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

        layout.Children.Add(
            new Label
            {
                Text = rank <= 3 ? $"♛ {rank}" : rank.ToString(),
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            });

        layout.Children.Add(
            new Image
            {
                Source = shield,
                HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 62 : 88,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center
            });

        layout.Children.Add(
            new Label
            {
                Text = teamName,
                TextColor = Colors.White,
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 15 : 19,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        layout.Children.Add(
            new Label
            {
                Text = $"{wins} انتصار",
                TextColor = Color.FromArgb("#C8B58A"),
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.Center
            });

        layout.Children.Add(
            new Label
            {
                Text = $"Legacy {legacy}",
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            });

        layout.Children.Add(
            new Label
            {
                Text = "★★★★★",
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.Center
            });

        card.Content = layout;

        return card;
    }

    View CreatePlayerLegendCard(
        int rank,
        string playerName,
        int score,
        string rankText)
    {
        string borderColor =
            rank == 1 ? "#D4AE62" :
            rank == 2 ? "#BFC3C7" :
            rank == 3 ? "#B87333" :
            "#765021";

        Border card =
            CreatePlayerBaseCard(rank, borderColor);

        VerticalStackLayout layout =
            CreatePlayerCardLayout(
                rank,
                borderColor,
                "player_card.png",
                playerName,
                rankText,
                score > 0 ? score.ToString() : "—");

        card.Content = layout;

        return card;
    }

    View CreatePlayerLegendCard(
        int rank,
        PlayerProfileModel player)
    {
        PlayerEngine.Normalize(player);

        var rankResult =
            PlayerRankService.Calculate(player.PlayerXP);

        string borderColor =
            rank == 1 ? "#D4AE62" :
            rank == 2 ? "#BFC3C7" :
            rank == 3 ? "#B87333" :
            "#765021";

        Border card =
            CreatePlayerBaseCard(rank, borderColor);

        VerticalStackLayout layout =
            CreatePlayerCardLayout(
                rank,
                borderColor,
                PlayerProfileService.GetPlayerImageSource(player),
                player.PlayerName,
                rankResult.DisplayName,
                player.LegacyScore.ToString());

        card.Content = layout;

        var tap = new TapGestureRecognizer();

        tap.Tapped += async (s, e) =>
        {
            await Navigation.PushAsync(
                new PlayerDetailsPage(player.PlayerId));
        };

        card.GestureRecognizers.Add(tap);

        return card;
    }

    Border CreatePlayerBaseCard(
        int rank,
        string borderColor)
    {
        return new Border
        {
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 128 : 175,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 205 : 270,
            BackgroundColor = Color.FromArgb(rank == 1 ? "#1B1005" : "#0C0C0C"),
            Stroke = Color.FromArgb(borderColor),
            StrokeThickness = rank == 1 ? 1.2 : 0.75,
            Padding = 7,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Shadow =
                new Shadow
                {
                    Brush = new SolidColorBrush(Color.FromArgb(rank == 1 ? "#B8873C" : "#5B3B18")),
                    Radius = rank == 1 ? 8 : 4,
                    Opacity = rank == 1 ? 0.20f : 0.10f,
                    Offset = new Point(0, 2)
                }
        };
    }

    VerticalStackLayout CreatePlayerCardLayout(
        int rank,
        string borderColor,
        ImageSource avatarSource,
        string playerName,
        string rankText,
        string score)
    {
        VerticalStackLayout layout =
            new()
            {
                Spacing = 3,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

        layout.Children.Add(
            new Label
            {
                Text = rank <= 3 ? $"#{rank} 🏅" : $"#{rank}",
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = 12,
                FontFamily= "timesbi",
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            });

        layout.Children.Add(
            new Border
            {
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 70 : 96,
                HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 70 : 96,
                BackgroundColor = Color.FromArgb("#151515"),
                Stroke = Color.FromArgb(borderColor),
                StrokeThickness = 1.2,
                StrokeShape = new RoundRectangle { CornerRadius = 999 },
                Content =
                    new Image
                    {
                        Source = avatarSource,
                        Aspect = Aspect.AspectFill
                    }
            });

        layout.Children.Add(
            new Label
            {
                Text = playerName,
                TextColor = Colors.White,
                FontFamily = "timesbi",
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 13 : 17,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        layout.Children.Add(
            new Label
            {
                Text = string.IsNullOrWhiteSpace(rankText) ? "Legend" : rankText,
                TextColor = Color.FromArgb("#C8B58A"),
                FontSize = 12,
                FontFamily = "timesbi",
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        layout.Children.Add(
            new Label
            {
                Text = score,
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 21 : 28,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            });

        layout.Children.Add(
            new Label
            {
                Text = "LEGACY SCORE",
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#B8873C"),
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12 : 13,
                HorizontalTextAlignment = TextAlignment.Center
            });

        return layout;
    }

    View CreateCandidateRow(TeamLegendResult team)
    {
        string reason =
            GetCandidateRejectReason(team);

        double progress =
            Math.Min(1.0, team.LegacyScore / 300.0);

        Border card =
            new()
            {
                BackgroundColor = Color.FromArgb("#101010"),
                Stroke = Color.FromArgb("#5B3B18"),
                StrokeThickness = 0.75,
                Padding = 10,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 18
                }
            };

        Grid root =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 46 },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 68 }
                },
                ColumnSpacing = 8,
                FlowDirection = FlowDirection.RightToLeft
            };

        Border iconFrame =
            new()
            {
                WidthRequest = 42,
                HeightRequest = 42,
                BackgroundColor = Color.FromArgb("#18120A"),
                Stroke = Color.FromArgb("#765021"),
                StrokeThickness = 0.75,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 14
                },
                Content =
                    new Image
                    {
                        Source = "shield_3d.png",
                        WidthRequest = 32,
                        HeightRequest = 32,
                        Aspect = Aspect.AspectFit,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
            };

        Grid.SetColumn(iconFrame, 0);
        root.Children.Add(iconFrame);

        VerticalStackLayout middle =
            new()
            {
                Spacing = 4,
                VerticalOptions = LayoutOptions.Center
            };

        middle.Children.Add(
            new Label
            {
                Text = team.DisplayName,
                TextColor = Colors.White,
                FontFamily = "timesbi",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.End,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        middle.Children.Add(
            new Label
            {
                Text = reason,
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#C8B58A"),
                FontSize = 10,
                HorizontalTextAlignment = TextAlignment.End,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.WordWrap
            });

        middle.Children.Add(
            new ProgressBar
            {
                Progress = progress,
                ProgressColor = Color.FromArgb("#D4AE62"),
                BackgroundColor = Color.FromArgb("#252525"),
                HeightRequest = 6
            });

        Grid.SetColumn(middle, 1);
        root.Children.Add(middle);

        VerticalStackLayout status =
            new()
            {
                Spacing = 0,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

        status.Children.Add(
            new Label
            {
                Text = team.LegacyScore.ToString(),
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            });

        status.Children.Add(
            new Label
            {
                Text = "Legacy",
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#888888"),
                FontSize = 10,
                HorizontalTextAlignment = TextAlignment.Center
            });

        status.Children.Add(
            new Label
            {
                Text = $"{team.WinRate:0}%",
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#C8B58A"),
                FontSize = 9,
                HorizontalTextAlignment = TextAlignment.Center
            });

        Grid.SetColumn(status, 2);
        root.Children.Add(status);

        card.Content = root;

        return card;
    }

    View CreateRecordEmptyGrid()
    {
        Grid grid =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Star
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Star
                    }
                },
                RowDefinitions =
                {
                    new RowDefinition
                    {
                        Height = GridLength.Auto
                    },
                    new RowDefinition
                    {
                        Height = GridLength.Auto
                    }
                },
                ColumnSpacing = 8,
                RowSpacing = 8
            };

        AddRecordCard(
            grid,
            "wins_gold.png",
            "أكثر فريق فاز",
            "—",
            "0",
            0,
            0);

        AddRecordCard(
            grid,
            "meles_badge_gold.png",
            "ملك الملص",
            "—",
            "0",
            1,
            0);

        AddRecordCard(
            grid,
            "fast_round_gold.png",
            "أسرع مباراة",
            "الزمن",
            "—",
            0,
            1);

        AddRecordCard(
            grid,
            "highest_score_gold.png",
            "أعلى نتيجة",
            "Score",
            "0",
            1,
            1);

        return grid;
    }

    void AddRecordCard(
        Grid grid,
        string icon,
        string title,
        string subtitle,
        string value,
        int column,
        int row)
    {
        Border card =
            new()
            {
                BackgroundColor = Color.FromArgb("#101010"),
                Stroke = Color.FromArgb("#5B3B18"),
                StrokeThickness = 0.75,
                Padding = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 10
                    : 13,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 18
                }
            };

        VerticalStackLayout layout =
            new()
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

        layout.Children.Add(
            new Image
            {
                Source = icon,
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 48
                    : 60,
                HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 48
                    : 60,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center
            });

        layout.Children.Add(
            new Label
            {
                Text = value,
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 24
                    : 32,
                FontFamily = "timesbi",
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        layout.Children.Add(
            new Label
            {
                Text = title,
                TextColor = Colors.White,
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 14
                    : 17,
                FontFamily = "timesbi",
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        layout.Children.Add(
            new Label
            {
                Text = subtitle,
                TextColor = Color.FromArgb("#C8B58A"),
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 10
                    : 12,
                FontFamily = "timesbi",
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        card.Content = layout;

        Grid.SetColumn(card, column);
        Grid.SetRow(card, row);

        grid.Children.Add(card);
    }

    void AddStat(
        string iconSource,
        string title,
        string value,
        int column,
        int row)
    {
        Border stat =
            new()
            {
                BackgroundColor = Color.FromArgb("#101010"),
                Stroke = Color.FromArgb("#5B3B18"),
                StrokeThickness = 0.75,
                Padding = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 10
                    : 13,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 18
                }
            };

        VerticalStackLayout layout =
            new()
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

        layout.Children.Add(
            new Image
            {
                Source = iconSource,
                WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 46
                    : 58,
                HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 46
                    : 58,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center
            });

        layout.Children.Add(
            new Label
            {
                Text = value,
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 22
                    : 30,
                FontFamily = "timesbi",
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        layout.Children.Add(
            new Label
            {
                Text = title,
                TextColor = Color.FromArgb("#C8B58A"),
                FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 11
                    : 14,
                FontFamily = "timesbi",
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        stat.Content = layout;

        Grid.SetColumn(stat, column);
        Grid.SetRow(stat, row);

        StatsContainer.Children.Add(stat);
    }

    View CreateRecordRow(
        string iconSource,
        string title,
        string value)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#101010"),
            Stroke = Color.FromArgb("#5B3B18"),
            StrokeThickness = 0.75,
            Padding = 10,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 18
            },
            Content =
                new Label
                {
                    Text = $"{title}: {value}",
                    TextColor = Color.FromArgb("#D4AE62"),
                    FontSize = 14,
                    FontFamily = "timesbi",
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                }
        };
    }

    Border CreatePremiumCard(
        string strokeColor)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb(CardGlass),
            Stroke = Color.FromArgb(strokeColor),
            StrokeThickness = 0.75,
            Padding = 10,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 20
            }
        };
    }

    View CreateEmptyCard(
        string text)
    {
        Border card =
            CreatePremiumCard(BronzeStroke);

        card.WidthRequest = 220;
        card.Padding = 12;

        card.Content =
            new Label
            {
                Text = text,
                FontSize = 13,
                FontFamily = "timesbi",
                TextColor = Color.FromArgb(MutedText),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

        return card;
    }

    List<TeamLegendResult> GetTeamResults()
    {
        var allKeys =
            matches
            .SelectMany(x =>
                new[]
                {
                    GetTeam1Key(x),
                    GetTeam2Key(x)
                })
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        List<TeamLegendResult> results =
            new();

        foreach (string key in allKeys)
        {
            int total =
                matches.Count(x =>
                    GetTeam1Key(x) == key ||
                    GetTeam2Key(x) == key);

            int wins =
                matches.Count(x =>
                    GetWinnerKey(x) == key);

            if (total == 0)
                continue;

            double winRate =
                (double)wins / total * 100;

            int meles =
                matches.Count(x =>
                    GetWinnerKey(x) == key &&
                    x.HasMeles);

            int legacy =
                wins * 100 +
                meles * 50 +
                (int)winRate;

            results.Add(
                new TeamLegendResult
                {
                    Key = key,
                    DisplayName = GetTeamDisplayName(key),
                    Wins = wins,
                    TotalMatches = total,
                    WinRate = winRate,
                    MelesCount = meles,
                    LegacyScore = legacy
                });
        }

        return results;
    }

    List<TeamLegendResult> GetEligibleHallTeams()
    {
        return GetTeamResults()
            .Where(IsHallEligible)
            .OrderByDescending(x => x.LegacyScore)
            .ThenByDescending(x => x.WinRate)
            .ToList();
    }

    bool IsHallEligible(
        TeamLegendResult result)
    {
        var team =
            teams.FirstOrDefault(x =>
                x.TeamId == result.Key ||
                x.TeamName == result.DisplayName);

        int trust =
            team?.TrustScore ?? 100;

        bool suspicious =
            team?.IsSuspicious ?? false;

        if (result.LegacyScore < 300)
            return false;

        if (result.TotalMatches < 20)
            return false;

        if (trust < 95)
            return false;

        if (result.WinRate < 60)
            return false;

        if (suspicious)
            return false;

        return true;
    }

    string GetCandidateRejectReason(
        TeamLegendResult result)
    {
        var team =
            teams.FirstOrDefault(x =>
                x.TeamId == result.Key ||
                x.TeamName == result.DisplayName);

        int trust =
            team?.TrustScore ?? 100;

        bool suspicious =
            team?.IsSuspicious ?? false;

        if (result.LegacyScore < 300)
            return "يحتاج Legacy أعلى";

        if (result.TotalMatches < 20)
            return $"يحتاج مباريات أكثر ({result.TotalMatches}/20)";

        if (trust < 95)
            return $"Trust Score غير كاف ({trust}/95)";

        if (result.WinRate < 60)
            return $"Win Rate أقل من المطلوب ({result.WinRate:0}%)";

        if (suspicious)
            return "الفريق تحت المراجعة";

        return "قريب من التأهل";
    }

    string GetTeam1Key(
        SavedMatch match)
    {
        string id =
            GetTextProperty(
                match,
                "Team1Id",
                "Team1ID");

        return string.IsNullOrWhiteSpace(id)
            ? match.Team1Name
            : id;
    }

    string GetTeam2Key(
        SavedMatch match)
    {
        string id =
            GetTextProperty(
                match,
                "Team2Id",
                "Team2ID");

        return string.IsNullOrWhiteSpace(id)
            ? match.Team2Name
            : id;
    }

    string GetWinnerKey(
        SavedMatch match)
    {
        string id =
            GetTextProperty(
                match,
                "WinnerTeamId",
                "WinnerTeamID");

        return string.IsNullOrWhiteSpace(id)
            ? match.WinnerTeam
            : id;
    }

    string GetWinnerDisplayName(
        SavedMatch match)
    {
        return GetTeamDisplayName(
            GetWinnerKey(match));
    }

    string GetTeamDisplayName(
        string key)
    {
        var team =
            teams.FirstOrDefault(x =>
                x.TeamId == key);

        if (team != null &&
            !string.IsNullOrWhiteSpace(team.TeamName))
        {
            return team.TeamName;
        }

        return key;
    }

    string GetCurrentSeasonText()
    {
        try
        {
            int season =
                SeasonManager
                    .GetCurrentSeasonNumber(teams);

            return season <= 0
                ? "—"
                : season.ToString();
        }
        catch
        {
            return "—";
        }
    }

    string GetTextProperty(
        object source,
        params string[] names)
    {
        foreach (string name in names)
        {
            PropertyInfo? prop =
                source
                .GetType()
                .GetProperty(name);

            if (prop == null)
                continue;

            object? value =
                prop.GetValue(source);

            if (value == null)
                continue;

            string text =
                value.ToString() ?? "";

            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return "";
    }

    async Task AnimatePageAsync()
    {
        HallContainer.TranslationY = 18;
        HallContainer.Opacity = 0;

        await Task.WhenAll(
            HallContainer.FadeTo(
                1,
                320,
                Easing.CubicOut),

            HallContainer.TranslateTo(
                0,
                0,
                320,
                Easing.CubicOut));
    }

    async void OnBackTapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    async void OnShowAllTeamsClicked(
        object sender,
        EventArgs e)
    {
        var eligible =
            GetEligibleHallTeams();

        if (eligible.Count == 0)
        {
            await DisplayAlert(
                "أعضاء قاعة الأساطير",
                "لا توجد فرق مؤهلة حالياً.",
                "حسناً");

            return;
        }

        string text =
            string.Join(
                "\n",
                eligible.Select(
                    (x, index) =>
                        $"#{index + 1} {x.DisplayName} | Wins {x.Wins} | Legacy {x.LegacyScore} | WR {x.WinRate:0}%"));

        await DisplayAlert(
            "أعضاء قاعة الأساطير",
            text,
            "حسناً");
    }

    async void OnShowAllPlayersClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new PlayerProfilesPage());
    }

    async void OnSideMenuNavigation(
        string section)
    {
        switch (section)
        {
            case "HOME":
                await HallScrollView
                    .ScrollToAsync(
                        0,
                        0,
                        true);
                break;

            case "TEAMS":
                await HallScrollView
                    .ScrollToAsync(
                        TeamsSection,
                        ScrollToPosition.Start,
                        true);
                break;

            case "PLAYERS":
                await HallScrollView
                    .ScrollToAsync(
                        PlayersSection,
                        ScrollToPosition.Start,
                        true);
                break;

            case "ACHIEVEMENTS":
                await HallScrollView
                    .ScrollToAsync(
                        CandidatesSection,
                        ScrollToPosition.Start,
                        true);
                break;

            case "HISTORY":
                await HallScrollView
                    .ScrollToAsync(
                        RecordsSection,
                        ScrollToPosition.Start,
                        true);
                break;

            case "STATS":
                await HallScrollView
                    .ScrollToAsync(
                        StatsSection,
                        ScrollToPosition.Start,
                        true);
                break;
        }
    }

    class TeamLegendResult
    {
        public string Key { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public int Wins { get; set; }

        public int TotalMatches { get; set; }

        public double WinRate { get; set; }

        public int MelesCount { get; set; }

        public int LegacyScore { get; set; }
    }
}
