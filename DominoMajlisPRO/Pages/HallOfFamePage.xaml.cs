using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.GalleryEngine.Pages;

namespace DominoMajlisPRO.Pages;

public partial class HallOfFamePage : ContentPage
{
    HallOfFameSnapshot snapshot = new();
    IReadOnlyDictionary<string, TeamIdentityModel> teamIdentities =
        new Dictionary<string, TeamIdentityModel>(
            StringComparer.OrdinalIgnoreCase);
    IReadOnlyDictionary<string, NameTypographyIdentity> teamNameTypographies =
        new Dictionary<string, NameTypographyIdentity>(
            StringComparer.OrdinalIgnoreCase);

    const string CardGlass = "#151515";
    const string BronzeStroke = "#5B3B18";
    const string MutedText = "#C8B58A";

    public HallOfFamePage()
    {
        InitializeComponent();

        SideMenu.NavigationRequested -= OnSideMenuNavigation;
        SideMenu.NavigationRequested += OnSideMenuNavigation;
        BottomNavigation.NavigationRequested -= OnBottomNavigation;
        BottomNavigation.NavigationRequested += OnBottomNavigation;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.DataChanged -= OnHallDataChanged;
        AppEvents.PlayerProfileChanged -= OnHallDataChanged;
        AppEvents.MatchesChanged -= OnHallDataChanged;
        AppEvents.TeamsChanged -= OnHallDataChanged;
        AppEvents.RankingsChanged -= OnHallDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged -= OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged -= OnStoreIdentityChanged;
        AppEvents.StoreProgressChanged -= OnStoreIdentityChanged;

        AppEvents.DataChanged += OnHallDataChanged;
        AppEvents.PlayerProfileChanged += OnHallDataChanged;
        AppEvents.MatchesChanged += OnHallDataChanged;
        AppEvents.TeamsChanged += OnHallDataChanged;
        AppEvents.RankingsChanged += OnHallDataChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged += OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged += OnStoreIdentityChanged;
        AppEvents.StoreProgressChanged += OnStoreIdentityChanged;

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
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged -= OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged -= OnStoreIdentityChanged;
        AppEvents.StoreProgressChanged -= OnStoreIdentityChanged;
    }

    async void OnHallDataChanged()
    {
        HallOfFameService.InvalidateCache();
        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                await LoadHallOfFameAsync(forceRefresh: true);
            });
    }

    void OnTeamAssetsChanged(string teamId) => OnHallDataChanged();
    void OnStoreIdentityChanged(string playerId) => OnHallDataChanged();

    async Task LoadHallOfFameAsync(bool forceRefresh = false)
    {
        snapshot = await HallOfFameService.LoadAsync(forceRefresh);
        teamIdentities = await TeamIdentityResolver.ResolveManyAsync(
            snapshot.Teams.Select(team => team.TeamId));
        teamNameTypographies = await ResolveTeamNameTypographiesAsync(
            snapshot.Teams.Select(team => team.TeamId));

        HallContainer.Opacity = 0;

        ClearDynamicSections();

        if (snapshot.Matches.Count == 0)
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
            CreateEmptyCard("لا توجد بيانات للاعبين بعد"));

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
        var champion = snapshot.Hero;

        SeasonNumberLabel.Text = snapshot.SeasonText;

        if (champion == null)
        {
            HeroTeamNameLabel.Text = "لا توجد أسطورة";
            HeroSubtitleLabel.Text = "بانتظار فريق يحقق شروط الدستور";
            HeroWinsLabel.Text = "0";
            HeroWinRateLabel.Text = "0%";
            HeroLegacyLabel.Text = "0";
            return;
        }

        HeroTeamNameLabel.Text = champion.TeamName;
        HeroSubtitleLabel.Text = "دخل قاعة الأساطير وفق الدستور";
        HeroWinsLabel.Text = champion.Wins.ToString();
        HeroWinRateLabel.Text = $"{champion.WinRate:0}%";
        HeroLegacyLabel.Text = champion.LegacyScore.ToString();
    }

    void LoadTopTeams()
    {
        var topTeams =
            snapshot.TeamHall
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
                    team));

            rank++;
        }

        TopTeamsContainer.HorizontalOptions =
            topTeams.Count <= 2
                ? LayoutOptions.Center
                : LayoutOptions.End;
    }

    async Task LoadTopPlayersAsync()
    {
        var topPlayers =
            snapshot.PlayerHall
            .Take(6)
            .ToList();

        if (topPlayers.Count == 0)
        {
            LoadFallbackPlayersFromTeams();
            return;
        }

        var identities =
            await PlayerVisualIdentityResolver.ResolveManyAsync(
                topPlayers.Select(player => player.PlayerId));
        int rank = 1;

        foreach (var player in topPlayers)
        {
            identities.TryGetValue(
                player.PlayerId,
                out var identity);
            TopPlayersContainer.Children.Add(
                CreatePlayerLegendCard(rank, player, identity));

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
            snapshot.Teams
            .SelectMany(x => new[] { x.Player1, x.Player2 })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(6)
            .ToList();

        if (playerNames.Count == 0)
        {
            TopPlayersContainer.Children.Add(
                CreateEmptyCard("لا توجد بيانات للاعبين بعد"));

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
            snapshot.Candidates
            .ToList();

        CandidatesSection.IsVisible = candidates.Count > 0;

        foreach (var candidate in candidates)
        {
            CandidatesContainer.Children.Add(
                CreateCandidateRow(candidate));
        }
    }

    void LoadLegendaryRecords()
    {
        RenderLegendaryRecordsFromSnapshot();
    }

    void LoadStatistics()
    {
        RenderStatisticsFromSnapshot();
    }

    void RenderLegendaryRecordsFromSnapshot()
    {
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

        var records = snapshot.Records.Take(4).ToList();
        for (int index = 0; index < records.Count; index++)
        {
            AddRecordCard(
                recordsGrid,
                records[index].Icon,
                records[index].Title,
                records[index].Subtitle,
                records[index].Value,
                index % 2,
                index / 2);
        }

        RecordsContainer.Children.Add(recordsGrid);
    }

    void RenderStatisticsFromSnapshot()
    {
        for (int index = 0; index < snapshot.Statistics.Count; index++)
        {
            AddStat(
                snapshot.Statistics[index].Icon,
                snapshot.Statistics[index].Title,
                snapshot.Statistics[index].Value,
                index % 3,
                index / 3);
        }
    }

    View CreateTeamLegendCard(
        int rank,
        TeamLegendResult team)
    {
        string borderColor =
            rank == 1 ? "#D4AE62" :
            rank == 2 ? "#BFC3C7" :
            rank == 3 ? "#B87333" :
            "#765021";

        string shield = GetTeamRankIcon(team);

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

        teamIdentities.TryGetValue(team.Key, out var identity);

        var emblemHost = new Grid
        {
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 72 : 98
        };

        var backgroundSource =
            InventoryDisplayResolver.ResolveOptionalImageSource(
                identity?.EmblemBackgroundSource);
        if (backgroundSource != null)
        {
            emblemHost.Add(
                new Image
                {
                    Source = backgroundSource,
                    Aspect = Aspect.AspectFill,
                    Opacity = 0.48,
                    InputTransparent = true
                });
        }

        var teamEmblem = new Image
        {
            Source = shield,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 62 : 88,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center
        };
        TeamEffectBehavior.SetTeamId(teamEmblem, team.Key);
        emblemHost.Add(teamEmblem);
        layout.Children.Add(emblemHost);

        layout.Children.Add(
            CreateTeamNameView(
                team,
                DeviceInfo.Idiom == DeviceIdiom.Phone ? 15 : 19,
                260));

        layout.Children.Add(
            new Label
            {
                Text = $"{team.Wins} انتصار",
                TextColor = Color.FromArgb("#C8B58A"),
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.Center
            });

        layout.Children.Add(
            new Label
            {
                Text = $"Legacy {team.LegacyScore}",
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

    string GetTeamRankIcon(TeamLegendResult result)
    {
        if (teamIdentities.TryGetValue(result.Key, out var identity) &&
            !string.IsNullOrWhiteSpace(identity.EmblemImagePath))
        {
            return identity.EmblemImagePath;
        }

        var team = snapshot.Teams.FirstOrDefault(item =>
            string.Equals(item.TeamId, result.Key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.TeamName, result.DisplayName, StringComparison.OrdinalIgnoreCase));
        var rank = team?.Rank ?? "Unranked";
        if (rank.StartsWith("Bronze", StringComparison.OrdinalIgnoreCase)) return "bronze.png";
        if (rank.StartsWith("Silver", StringComparison.OrdinalIgnoreCase)) return "silver.png";
        if (rank.StartsWith("Gold", StringComparison.OrdinalIgnoreCase)) return "gold.png";
        if (rank.StartsWith("Platinum", StringComparison.OrdinalIgnoreCase)) return "platinum.png";
        if (rank.StartsWith("Diamond", StringComparison.OrdinalIgnoreCase)) return "diamond.png";
        if (string.Equals(rank, "Majlis Master", StringComparison.OrdinalIgnoreCase)) return "majlis_master.png";
        if (string.Equals(rank, "Majlis Legend", StringComparison.OrdinalIgnoreCase)) return "majlis_legend.png";
        return "unranked.png";
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
        PlayerHallResult hallPlayer,
        PlayerVisualIdentity? identity)
    {
        if (hallPlayer.Player == null)
        {
            return CreatePlayerLegendCard(
                rank,
                hallPlayer.PlayerName,
                hallPlayer.FinalHallScore,
                hallPlayer.Category);
        }

        return CreatePlayerLegendCard(
            rank,
            hallPlayer.Player,
            identity,
            hallPlayer.Category,
            hallPlayer.FinalHallScore);
    }

    View CreatePlayerLegendCard(
        int rank,
        PlayerProfileModel player,
        PlayerVisualIdentity? identity,
        string? hallCategory = null,
        int hallScore = 0)
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
                ToOptionalImageSource(
                    identity?.Avatar?.PreviewImage) ??
                PlayerProfileService.GetPlayerImageSource(player),
                player.PlayerName,
                identity?.Title == null
                    ? string.IsNullOrWhiteSpace(hallCategory)
                        ? rankResult.DisplayName
                        : $"{rankResult.DisplayName} â€¢ {hallCategory}"
                    : $"{rankResult.DisplayName} • {identity.Title.DisplayName}",
                (hallScore > 0 ? hallScore : player.LegacyScore).ToString(),
                identity);

        var surface = new Grid();
        var background =
            CreatePlayerBackgroundImage(
                identity?.ProfileBackground?.PreviewImage);
        if (background != null)
            surface.Add(background);
        surface.Add(layout);
        card.Content = surface;

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

    static Image? CreatePlayerBackgroundImage(string? imagePath)
    {
        var source = ToOptionalImageSource(imagePath);
        return source == null
            ? null
            : new Image
            {
                Source = source,
                Aspect = Aspect.AspectFill,
                Opacity = 0.34,
                InputTransparent = true
            };
    }

    static ImageSource? ToOptionalImageSource(string? imagePath) =>
        InventoryDisplayResolver.ResolveOptionalImageSource(
            imagePath);

    static void AddPlayerOverlay(Grid container, string? imagePath)
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

    View CreateTeamNameView(
        TeamLegendResult result,
        double fontSize,
        double maxWidth)
    {
        var team = snapshot.Teams.FirstOrDefault(item =>
            string.Equals(item.TeamId, result.Key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.TeamName, result.DisplayName, StringComparison.OrdinalIgnoreCase));
        NameTypographyIdentity? typography = null;
        if (!string.IsNullOrWhiteSpace(team?.TeamId))
        {
            teamNameTypographies.TryGetValue(
                team.TeamId,
                out typography);
        }

        return CreateNamePlate(
            result.DisplayName,
            typography,
            fontSize,
            Colors.White,
            maxWidth);
    }

    static void AddPlayerEffect(
        Grid container,
        CatalogAssetDisplay? effect,
        double baseScale = 1.16)
    {
        if (effect == null)
            return;

        var overlay = new Image
        {
            Aspect = Aspect.AspectFit,
            InputTransparent = true
        };
        PlayerEffectEngine.Apply(overlay, effect, baseScale);
        container.Add(overlay);
    }

    static async Task<IReadOnlyDictionary<string, NameTypographyIdentity>> ResolveTeamNameTypographiesAsync(
        IEnumerable<string> teamIds)
    {
        Dictionary<string, NameTypographyIdentity> resolved =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (string teamId in teamIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var identity = await NameTypographyResolver.ResolveTeamAsync(teamId);
            if (identity != null)
                resolved[teamId] = identity;
        }

        return resolved;
    }

    static View CreateNamePlate(
        string text,
        NameTypographyIdentity? typography,
        double fontSize,
        Color textColor,
        double maxWidth)
    {
        Grid plate = new()
        {
            WidthRequest = maxWidth,
            HorizontalOptions = LayoutOptions.Center
        };

        AddPlayerOverlay(plate, typography?.NameFrame?.PreviewImage);
        AddPlayerEffect(plate, typography?.NameEffect, 1.04);

        plate.Add(
            new Label
            {
                Text = text,
                TextColor = textColor,
                FontSize = fontSize,
                FontFamily = "timesbi",
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        return plate;
    }

    VerticalStackLayout CreatePlayerCardLayout(
        int rank,
        string borderColor,
        ImageSource avatarSource,
        string playerName,
        string rankText,
        string score,
        PlayerVisualIdentity? identity = null)
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

        var avatar = new Grid();
        avatar.Add(
            new Image
            {
                Source = avatarSource,
                Aspect = Aspect.AspectFill
            });
        AddPlayerOverlay(avatar, identity?.Frame?.PreviewImage);
        AddPlayerEffect(avatar, identity?.Effect);

        layout.Children.Add(
            new Border
            {
                WidthRequest =
                    DeviceInfo.Idiom == DeviceIdiom.Phone ? 70 : 96,
                HeightRequest =
                    DeviceInfo.Idiom == DeviceIdiom.Phone ? 70 : 96,
                BackgroundColor = Color.FromArgb("#151515"),
                Stroke = identity?.Frame == null
                    ? Color.FromArgb(borderColor)
                    : Colors.Transparent,
                StrokeThickness = 1.2,
                StrokeShape =
                    new RoundRectangle { CornerRadius = 999 },
                Shadow = new Shadow
                    {
                        Brush = new SolidColorBrush(
                            Color.FromArgb("#F2C14E")),
                        Radius = 16,
                        Opacity = 0.5f
                    },
                Content = avatar
            });

        layout.Children.Add(
            CreateNamePlate(
                playerName,
                new NameTypographyIdentity(
                    string.Empty,
                    identity?.PlayerNameEffect,
                    identity?.PlayerNameFrame),
                DeviceInfo.Idiom == DeviceIdiom.Phone ? 13 : 17,
                Colors.White,
                240));

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

    View CreateCandidateRow(HallCandidateResult candidate)
    {
        var team = candidate.Team;

        double progress =
            Math.Min(
                1.0,
                new[]
                {
                    candidate.TrustProgress,
                    candidate.LegacyProgress,
                    candidate.MatchesProgress,
                    candidate.WinRateProgress,
                    candidate.AchievementProgress,
                    candidate.IntegrityProgress
                }.Average());

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
                Text = $"{candidate.RejectionReason} | {candidate.BlockingArticle}",
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

        middle.Children.Add(CreateCandidateProgressRow("Trust", candidate.TrustProgress));
        middle.Children.Add(CreateCandidateProgressRow("Legacy", candidate.LegacyProgress));
        middle.Children.Add(CreateCandidateProgressRow("Matches", candidate.MatchesProgress));
        middle.Children.Add(CreateCandidateProgressRow("WinRate", candidate.WinRateProgress));
        middle.Children.Add(CreateCandidateProgressRow("Achievement", candidate.AchievementProgress));
        middle.Children.Add(CreateCandidateProgressRow("Integrity", candidate.IntegrityProgress));

        middle.Children.Add(
            new Label
            {
                Text = $"Missing: {string.Join(", ", candidate.MissingRequirements)}",
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#9E8A66"),
                FontSize = 9,
                HorizontalTextAlignment = TextAlignment.End,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.WordWrap
            });

        middle.Children.Add(
            new Label
            {
                Text = $"Remaining: {candidate.EstimatedRemaining}",
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#D4AE62"),
                FontSize = 9,
                HorizontalTextAlignment = TextAlignment.End,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.WordWrap
            });

        middle.Children.Add(
            new Label
            {
                Text = $"Audit: {team.AuditData}",
                FontFamily = "timesbi",
                TextColor = Color.FromArgb("#8F6730"),
                FontSize = 8,
                HorizontalTextAlignment = TextAlignment.End,
                MaxLines = 2,
                LineBreakMode = LineBreakMode.WordWrap
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

    View CreateCandidateProgressRow(
        string title,
        double progress)
    {
        Grid row =
            new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 82 },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 42 }
                },
                ColumnSpacing = 6
            };

        row.Add(
            new Label
            {
                Text = title,
                TextColor = Color.FromArgb("#C8B58A"),
                FontFamily = "timesbi",
                FontSize = 9,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

        var bar =
            new ProgressBar
            {
                Progress = progress,
                ProgressColor = Color.FromArgb("#D4AE62"),
                BackgroundColor = Color.FromArgb("#252525"),
                HeightRequest = 5,
                VerticalOptions = LayoutOptions.Center
            };
        Grid.SetColumn(bar, 1);
        row.Add(bar);

        var value =
            new Label
            {
                Text = $"{progress * 100:0}%",
                TextColor = Color.FromArgb("#888888"),
                FontFamily = "timesbi",
                FontSize = 9,
                HorizontalTextAlignment = TextAlignment.End
            };
        Grid.SetColumn(value, 2);
        row.Add(value);

        return row;
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

    async Task AnimatePageAsync()
    {
        HallContainer.TranslationY = 18;
        HallContainer.Opacity = 0;

        await Task.WhenAll(
            HallContainer.FadeTo(
                1,
                320,
                Easing.CubicOut),

            HallContainer.TranslateToAsync(
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
            snapshot.TeamHall;

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

    async void OnBottomNavigation(
        string destination)
    {
        switch (destination)
        {
            case "SETTINGS":
                await Navigation.PushAsync(new MainPage());
                break;

            case "PLAYERS":
                await Navigation.PushAsync(new PlayerProfilesPage());
                break;

            case "GAME":
                await Navigation.PushAsync(new CreateTeamPage());
                break;

            case "STORE":
                await Navigation.PushAsync(new GalleryPage());
                break;
        }
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

    class LegacyTeamLegendResult
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



