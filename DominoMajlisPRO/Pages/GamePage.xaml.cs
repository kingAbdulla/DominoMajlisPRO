using DominoMajlisPRO.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
namespace DominoMajlisPRO.Pages;

public partial class GamePage : ContentPage
{

 
    // =========================
    // VARIABLES
    // =========================
    int team1Score = 0;
    int team2Score = 0;
    int roundNumber = 1;
    bool gameFinished = false;
    bool matchSaved = false;
    readonly int targetScore = 151;
    bool isLocalRules = false;
    int selectedTeam = 1;
    bool showAllRounds = false;

    string keypadValue = "";
    string team1Id = "";
    string team2Id = "";
    SavedMatch currentMatch =
        new SavedMatch();
    List<RoundModel> roundsHistory =
        new();
    TeamProfileModel? team1Profile;
    TeamProfileModel? team2Profile;
    TeamIdentityModel? team1Identity;
    TeamIdentityModel? team2Identity;
    TaskCompletionSource<bool>? dialogCompletion;
    bool timerStarted;

    sealed record MatchVictoryEvaluation(
        bool IsCompleted,
        int WinnerTeamNumber,
        string WinnerName,
        string WinnerPlayers,
        bool HasMeles);
    // =========================
    // NEW MATCH
    // =========================
    public GamePage(
     string team1Name,
     string team2Name,
     string team1Players,
     string team2Players,

     string team1Id,
     string team2Id,

     string team1Player1Id,
     string team1Player2Id,
     string team2Player1Id,
     string team2Player2Id,

     string rules)
    {
        InitializeComponent();
        this.team1Id = team1Id;
        this.team2Id = team2Id;
        // TEAM DATA
        Team1Name.Text =
            team1Name;

        Team1Players.Text =
            team1Players;

        Team2Name.Text =
            team2Name;

        Team2Players.Text =
            team2Players;

        // RULES
        RulesLabel.Text =
            $"القوانين: {rules}";

        isLocalRules =
            rules.Contains("محلي");

        // MATCH DATA
        currentMatch.Team1Name =
            team1Name;

        currentMatch.Team2Name =
            team2Name;

        currentMatch.Team1Players =
            team1Players;

        currentMatch.Team2Players =
            team2Players;

        currentMatch.Team1Id =
            team1Id;

        currentMatch.Team2Id =
            team2Id;

        currentMatch.Team1Player1Id =
            team1Player1Id;

        currentMatch.Team1Player2Id =
            team1Player2Id;

        currentMatch.Team2Player1Id =
            team2Player1Id;

        currentMatch.Team2Player2Id =
            team2Player2Id;

        ApplyPlayerNamePlates();

        currentMatch.IsLocalRules =
            isLocalRules;

        currentMatch.MatchDate =
            DateTime.Now;

        currentMatch.LastPlayedTime =
            DateTime.Now;

        currentMatch.DisplayTitle =
            $"{team1Name} ({team1Players}) VS " +
            $"{team2Name} ({team2Players})";

        SetupGestures();
        SetupScoreCardGestures();

        SelectTeam(1);

        UpdateLeaderUI();

        _ = LoadTeamProfiles();

        UpdateWinRate();

        StartMatchTimer();
    }


    // =========================
    // RESUME MATCH
    // =========================
    public GamePage(
        SavedMatch match)
    {
        
        InitializeComponent();

        team1Id =match.Team1Id;
        team2Id =match.Team2Id;


        currentMatch = match;
        team1Score =
            match.Team1Score;
        team2Score =
            match.Team2Score;
        roundNumber =
            match.RoundNumber;
        roundsHistory =
            match.RoundsHistory ?? new();
        isLocalRules =
            match.IsLocalRules;
        Team1Name.Text =
            match.Team1Name;
        Team1Players.Text =
            match.Team1Players;
        Team2Name.Text =
            match.Team2Name;
        Team2Players.Text =
            match.Team2Players;
        ApplyPlayerNamePlates();
        Team1Score.Text =
            team1Score.ToString();
        Team2Score.Text =
            team2Score.ToString();
       

        RulesLabel.Text =
            match.IsLocalRules
            ? "القوانين: محلي"
            : "القوانين: عالمي";
        SetupGestures();
        SetupScoreCardGestures();
        SelectTeam(1);
        UpdateLeaderUI();
        _ = RefreshLiveTeamVisualsAsync();
        StartMatchTimer();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;

        await RefreshLiveTeamVisualsAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
    }
    // =========================
    // GESTURES
    // =========================
    void SetupGestures()
    {
      

        var team1Tap = new TapGestureRecognizer();

        team1Tap.Tapped += async (s, e) =>
        {
            SelectTeam(1);

            if (int.TryParse(keypadValue, out int points))
            {
                keypadValue = "";
                KeypadDisplay.Text = "0";

                await AddPoints(1, points);
            }
        };

        var team2Tap = new TapGestureRecognizer();

        team2Tap.Tapped += async (s, e) =>
        {
            SelectTeam(2);

            if (int.TryParse(keypadValue, out int points))
            {
                keypadValue = "";
                KeypadDisplay.Text = "0";

                await AddPoints(2, points);
            }
        };

        SelectTeam(1);
    }

    void SetupScoreCardGestures()
    {
        Team1Card.GestureRecognizers.Clear();
        Team2Card.GestureRecognizers.Clear();

        var team1Tap = new TapGestureRecognizer();

        team1Tap.Tapped += async (s, e) =>
        {
            if (selectedTeam == 1)
            {
                if (int.TryParse(keypadValue, out int points))
                {
                    keypadValue = "";
                    KeypadDisplay.Text = "0";

                    await AddPoints(1, points);
                }
            }
            else
            {
                SelectTeam(1);
            }
        };

        Team1Card.GestureRecognizers.Add(team1Tap);

        var team2Tap = new TapGestureRecognizer();

        team2Tap.Tapped += async (s, e) =>
        {
            if (selectedTeam == 2)
            {
                if (int.TryParse(keypadValue, out int points))
                {
                    keypadValue = "";
                    KeypadDisplay.Text = "0";

                    await AddPoints(2, points);
                }
            }
            else
            {
                SelectTeam(2);
            }
        };

        Team2Card.GestureRecognizers.Add(team2Tap);
    }

    async Task LoadTeamProfiles()
    {
        var teams =
            await TeamProfileService.LoadTeamsAsync();

        var players =
            await PlayerProfileService.LoadPlayersAsync();

        var rankingTeams =
            await RankingService.LoadTeamsAsync();

        team1Profile =
            ResolveRankedTeamProfile(
                teams,
                rankingTeams,
                team1Id);

        team2Profile =
            ResolveRankedTeamProfile(
                teams,
                rankingTeams,
                team2Id);

        if (team1Profile != null)
        {
            currentMatch.Team1Emblem =
                team1Profile.Emblem;

            currentMatch.Team1ColorHex =
                team1Profile.ColorHex;

            ApplySpecialHonorIcon(
                team1Profile,
                players,
                Team1SpecialHonorIcon);
        }

        if (team2Profile != null)
        {
            currentMatch.Team2Emblem =
                team2Profile.Emblem;

            currentMatch.Team2ColorHex =
                team2Profile.ColorHex;

            ApplySpecialHonorIcon(
                team2Profile,
                players,
                Team2SpecialHonorIcon);
        }

        await RefreshLiveTeamVisualsAsync();
    }

    static TeamProfileModel? ResolveRankedTeamProfile(
        IReadOnlyList<TeamProfileModel> sourceTeams,
        IReadOnlyList<TeamProfileModel> rankingTeams,
        string teamId)
    {
        var source =
            sourceTeams.FirstOrDefault(x => x.TeamId == teamId);

        var ranked =
            rankingTeams.FirstOrDefault(x => x.TeamId == teamId);

        if (ranked == null)
            return source;

        if (source == null)
            return ranked;

        ranked.TeamName = FirstPayload(source.TeamName, ranked.TeamName, string.Empty);
        ranked.Player1 = FirstPayload(source.Player1, ranked.Player1, string.Empty);
        ranked.Player2 = FirstPayload(source.Player2, ranked.Player2, string.Empty);
        ranked.Player1Id = FirstPayload(source.Player1Id, ranked.Player1Id, string.Empty);
        ranked.Player2Id = FirstPayload(source.Player2Id, ranked.Player2Id, string.Empty);
        ranked.Emblem = FirstPayload(source.Emblem, ranked.Emblem, "shield_3d.png");
        ranked.ColorHex = FirstPayload(source.ColorHex, ranked.ColorHex, "#FFD700");
        ranked.EmblemBackground = FirstPayload(source.EmblemBackground, ranked.EmblemBackground, "Transparent");
        ranked.EmblemBackgroundAssetId = FirstPayload(source.EmblemBackgroundAssetId, ranked.EmblemBackgroundAssetId, string.Empty);
        ranked.EquippedTeamEffectAssetId = FirstPayload(source.EquippedTeamEffectAssetId, ranked.EquippedTeamEffectAssetId, string.Empty);
        ranked.TeamColorAssetId = FirstPayload(source.TeamColorAssetId, ranked.TeamColorAssetId, string.Empty);

        if (ranked.TrustScore <= 0)
            ranked.TrustScore = source.TrustScore;

        return ranked;
    }

    async void OnTeamAssetsChanged(string changedTeamId)
    {
        if (!string.Equals(
                changedTeamId,
                team1Id,
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                changedTeamId,
                team2Id,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(RefreshLiveTeamVisualsAsync);
    }

    async Task RefreshLiveTeamVisualsAsync()
    {
        team1Identity = await ResolveLiveIdentityAsync(
            team1Id,
            team1Profile,
            currentMatch.Team1Emblem,
            currentMatch.Team1ColorHex);
        team2Identity = await ResolveLiveIdentityAsync(
            team2Id,
            team2Profile,
            currentMatch.Team2Emblem,
            currentMatch.Team2ColorHex);

        Team1Emblem.Source =
            InventoryDisplayResolver.ResolveImageSource(
                team1Identity.EmblemImagePath,
                "shield_3d.png");
        Team2Emblem.Source =
            InventoryDisplayResolver.ResolveImageSource(
                team2Identity.EmblemImagePath,
                "shield_3d.png");
        await TeamEffectEngine.ApplyAroundAsync(Team1Emblem, team1Id, 1.18);
        await TeamEffectEngine.ApplyAroundAsync(Team2Emblem, team2Id, 1.18);
        ApplyTeamIdentityVisual(Team1Card, Team1BackgroundImage, team1Identity);
        ApplyTeamIdentityVisual(Team2Card, Team2BackgroundImage, team2Identity);
        ApplyTeamProgressVisuals();
        await ApplyMatchFooterVisualsAsync();
    }

    static void ApplyTeamIdentityVisual(
        Border card,
        Image background,
        TeamIdentityModel identity)
    {
        var color = ParseTeamColor(identity.TeamColorHex);
        card.Stroke = color;
        card.BackgroundColor = ResolveIdentityBackground(identity);

        if (!string.IsNullOrWhiteSpace(identity.EmblemBackgroundSource) &&
            !identity.EmblemBackgroundSource.StartsWith('#') &&
            !identity.EmblemBackgroundSource.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
        {
            background.Source = InventoryDisplayResolver.ResolveImageSource(
                identity.EmblemBackgroundSource,
                string.Empty);
            background.IsVisible = true;
        }
        else
        {
            background.Source = null;
            background.IsVisible = false;
        }
    }

    static Color ResolveIdentityBackground(TeamIdentityModel identity)
    {
        var source = identity.EmblemBackgroundSource;
        if (!string.IsNullOrWhiteSpace(source) &&
            source.StartsWith('#'))
        {
            try { return Color.FromArgb(source); }
            catch { }
        }

        try
        {
            var color = Color.FromArgb(identity.TeamColorHex);
            return new Color(color.Red, color.Green, color.Blue, 0.12f);
        }
        catch
        {
            return Colors.Transparent;
        }
    }

    static Color ParseTeamColor(string? colorHex)
    {
        try
        {
            return string.IsNullOrWhiteSpace(colorHex)
                ? Color.FromArgb("#D4AF37")
                : Color.FromArgb(colorHex);
        }
        catch
        {
            return Color.FromArgb("#D4AF37");
        }
    }

    static async Task<TeamIdentityModel> ResolveLiveIdentityAsync(
        string teamId,
        TeamProfileModel? legacyProfile,
        string? snapshotEmblem,
        string? snapshotColor)
    {
        try
        {
            return await TeamIdentityResolver.ResolveAsync(teamId);
        }
        catch
        {
            return new TeamIdentityModel
            {
                TeamId = teamId,
                TeamName = legacyProfile?.TeamName ?? string.Empty,
                EmblemImagePath =
                    FirstPayload(
                        legacyProfile?.Emblem,
                        snapshotEmblem,
                        "shield_3d.png"),
                EmblemBackgroundSource = "Transparent",
                TeamColorHex =
                    FirstPayload(
                        legacyProfile?.ColorHex,
                        snapshotColor,
                        "#FFD700"),
                ResolvedAt = DateTime.UtcNow
            };
        }
    }

    static string FirstPayload(
        string? primary,
        string? secondary,
        string fallback) =>
        !string.IsNullOrWhiteSpace(primary)
            ? primary
            : !string.IsNullOrWhiteSpace(secondary)
                ? secondary
                : fallback;

    void ApplyTeamProgressVisuals()
    {
        ApplyTeamProgressVisual(
            team1Profile,
            ParseTeamColor(team1Identity?.TeamColorHex ?? team1Profile?.ColorHex),
            Team1RankNameLabel,
            Team1XpLabel,
            Team1RankPercentLabel,
            Team1RankProgressTrack,
            Team1RankProgressFill,
            Team1CurrentRankIcon,
            Team1NextRankIcon,
            Team1CurrentRankLabel,
            Team1NextRankLabel);

        ApplyTeamProgressVisual(
            team2Profile,
            ParseTeamColor(team2Identity?.TeamColorHex ?? team2Profile?.ColorHex),
            Team2RankNameLabel,
            Team2XpLabel,
            Team2RankPercentLabel,
            Team2RankProgressTrack,
            Team2RankProgressFill,
            Team2CurrentRankIcon,
            Team2NextRankIcon,
            Team2CurrentRankLabel,
            Team2NextRankLabel);
    }

    static void ApplyTeamProgressVisual(
        TeamProfileModel? team,
        Color teamColor,
        Label rankLabel,
        Label xpLabel,
        Label percentLabel,
        Border progressTrack,
        BoxView progressFill,
        Image currentIcon,
        Image nextIcon,
        Label currentTier,
        Label nextTier)
    {
        int xp = Math.Max(0, team?.XP ?? team?.SeasonXP ?? team?.LifetimeXP ?? 0);
        var rank = PlayerRankService.Calculate(xp);
        var next = PlayerRankService.Calculate(rank.NextRankXP);
        double progress = Math.Clamp(rank.Progress, 0, 1);

        rankLabel.Text = string.IsNullOrWhiteSpace(rank.DisplayName)
            ? "Unranked"
            : rank.DisplayName;
        xpLabel.Text = rank.NextRankXP > rank.CurrentRankMinXP
            ? $"{xp:N0} / {rank.NextRankXP:N0} XP"
            : $"{xp:N0} XP";
        percentLabel.Text = $"{progress * 100:0}%";
        progressTrack.Stroke = teamColor.WithAlpha(0.65f);
        progressFill.Color = teamColor;
        progressFill.ScaleX = progress;
        currentIcon.Source = ResolveGameRankIcon(rank);
        nextIcon.Source = ResolveGameRankIcon(next);
        currentTier.Text = ToRoman(rank.Tier);
        nextTier.Text = ToRoman(next.Tier);
    }

    static string ResolveGameRankIcon(PlayerRankResult rank) =>
        rank.RankBase switch
        {
            "Bronze" => "bronze.png",
            "Silver" => "silver.png",
            "Gold" => "gold.png",
            "Platinum" => "platinum.png",
            "Diamond" => "diamond.png",
            "Majlis Master" => "majlis_master.png",
            "Majlis Legend" => "majlis_legend.png",
            _ => "unranked.png"
        };

    async Task ApplyMatchFooterVisualsAsync()
    {
        int trust = 100;
        if (team1Profile != null || team2Profile != null)
        {
            var values = new[]
            {
                team1Profile?.TrustScore ?? 100,
                team2Profile?.TrustScore ?? 100
            };
            trust = Math.Clamp((int)Math.Round(values.Average()), 0, 100);
        }

        TeamTrustLabel.Text = $"{trust} %";
        try
        {
            var wallet = await PlayerStoreIdentityService.GetDeviceWalletAsync();
            MatchCoinsLabel.Text = (wallet?.Coins ?? 0).ToString("N0");
        }
        catch
        {
            MatchCoinsLabel.Text = "0";
        }
    }

    static string ToRoman(int tier) => tier switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        _ => ""
    };

    // =========================
    //Animation +
    async Task ShowFloatingScore(
    int team,
    int points)
    {
        Label targetLabel =
            team == 1
            ? Team1FloatingScore
            : Team2FloatingScore;

        var targetCard =
            team == 1
            ? Team1Card
            : Team2Card;

        var targetScore =
            team == 1
            ? Team1Score
            : Team2Score;

        targetLabel.Text =
            $"+{points}";

        targetLabel.TranslationY = 20;
        targetLabel.Opacity = 1;

        var floatingTask =
            Task.WhenAll(
                targetLabel.TranslateToAsync(
                    0,
                    -25,
                    900,
                    Easing.CubicOut),

                targetLabel.FadeToAsync(
                    0,
                    900));

        async Task PulseCardAsync()
        {
            try
            {
                await targetCard.ScaleToAsync(
                    1.12,
                    120,
                    Easing.CubicOut);

                await targetCard.ScaleToAsync(
                    1.08,
                    120,
                    Easing.CubicIn);
            }
            catch { }
        }

        async Task PulseScoreAsync()
        {
            try
            {
                await targetScore.ScaleToAsync(
                    1.20,
                    120,
                    Easing.CubicOut);

                await targetScore.ScaleToAsync(
                    1.00,
                    120,
                    Easing.CubicIn);
            }
            catch { }
        }

        var cardPulse = PulseCardAsync();
        var scorePulse = PulseScoreAsync();

        await Task.WhenAll(
            floatingTask,
            cardPulse,
            scorePulse);
    }
    // Timer
    // =========================
    void StartMatchTimer()
    {
        if (timerStarted)
            return;

        timerStarted = true;

        Dispatcher.StartTimer(
            TimeSpan.FromSeconds(1),
            () =>
            {
                MatchTimeLabel.Text =
                    FormatMatchDuration();

                return true;
            });
    }


    // =========================
    //Show Tabele
    void OnToggleRoundsClicked(
    object sender,
    EventArgs e)
    {
        showAllRounds =
            !showAllRounds;

        ToggleRoundsButton.Text =
            showAllRounds
            ? "إخفاء ▲"
            : "عرض المزيد ▼";

        RefreshRoundsHistory();
    }

    void RefreshRoundsHistory()
    {
        RoundsContainer.Children.Clear();

        ToggleRoundsButton.IsVisible =
            roundsHistory.Count > 5;

        ToggleRoundsButton.Text =
            showAllRounds
                ? "إخفاء ▲"
                : "إظهار المزيد ⌄";

        var roundsToShow =
            showAllRounds
            ? roundsHistory
            : roundsHistory.TakeLast(5);

        foreach (var round in roundsToShow)
        {
            var rowColor = round.WinnerTeamId == 1
                ? ParseTeamColor(team1Identity?.TeamColorHex ?? team1Profile?.ColorHex)
                : ParseTeamColor(team2Identity?.TeamColorHex ?? team2Profile?.ColorHex);
            var rowTextColor = GetContrastingTextColor(rowColor);

            Border row = new()
            {
                BackgroundColor = round == roundsHistory.Last()
                    ? Color.FromArgb("#141006")
                    : Color.FromArgb("#090C0B"),
                Stroke = round == roundsHistory.Last()
                    ? Color.FromArgb("#FFD447")
                    : Color.FromArgb("#5D4517"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Padding = new Thickness(6, 7),
                Margin = new Thickness(0, 0, 0, 1)
            };

            Grid grid = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(1.1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(1.1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(1.7, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(1.7, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(118))
                },
                ColumnSpacing = 4
            };

            grid.Add(CreateHistoryCell(round.RoundNumber.ToString(), Colors.Gold, true), 0, 0);
            grid.Add(CreateHistoryCell(FormatRoundElapsed(round.RoundTime), Color.FromArgb("#FFD447"), true), 1, 0);
            grid.Add(CreateHistoryBadge(round.WinnerTeam, rowColor, rowTextColor), 2, 0);
            grid.Add(CreateHistoryCell($"{round.Team1NewScore} - {round.Team2NewScore}", Colors.White, true), 3, 0);

            var actions = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(new GridLength(36))
                },
                ColumnSpacing = 5,
                HorizontalOptions = LayoutOptions.Center
            };

            actions.Add(CreateRoundActionButton("✎", Color.FromArgb("#2E90FF"), async () => await EditRoundAsync(round)), 0, 0);
            actions.Add(CreateRoundActionButton("⇄", Color.FromArgb("#AA4CFF"), async () => await TransferRoundAsync(round)), 1, 0);
            actions.Add(CreateRoundActionButton("⌫", Color.FromArgb("#FF5A45"), async () => await DeleteRoundAsync(round)), 2, 0);
            grid.Add(actions, 4, 0);

            row.Content = grid;

            RoundsContainer.Children.Add(row);
        }
    }

    static Label CreateHistoryCell(string text, Color color, bool bold = false) =>
        new()
        {
            Text = text,
            TextColor = color,
            FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 12 : 15,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };

    static Border CreateHistoryBadge(string text, Color background, Color foreground) =>
        new()
        {
            BackgroundColor = background,
            Stroke = foreground.WithAlpha(0.34f),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 7 },
            Padding = new Thickness(4, 2),
            Content = CreateHistoryCell(text, foreground, true)
        };

    static Color GetContrastingTextColor(Color background)
    {
        var luminance = (0.2126 * background.Red) +
                        (0.7152 * background.Green) +
                        (0.0722 * background.Blue);
        return luminance > 0.52 ? Colors.Black : Colors.White;
    }

    void ApplyPlayerNamePlates()
    {
        BindPlayers(
            Team1Players.Text,
            currentMatch.Team1Player1Id,
            currentMatch.Team1Player2Id,
            Team1Player1NamePlate,
            Team1Player2NamePlate);
        BindPlayers(
            Team2Players.Text,
            currentMatch.Team2Player1Id,
            currentMatch.Team2Player2Id,
            Team2Player1NamePlate,
            Team2Player2NamePlate);

        static void BindPlayers(
            string? displayNames,
            string? firstId,
            string? secondId,
            GalleryEngine.Components.RuntimeNamePlateView first,
            GalleryEngine.Components.RuntimeNamePlateView second)
        {
            var names = (displayNames ?? string.Empty)
                .Split(new[] { "+", "•", "&" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            first.OwnerId = firstId ?? string.Empty;
            first.DisplayText = names.ElementAtOrDefault(0) ?? displayNames ?? string.Empty;
            first.IsVisible = !string.IsNullOrWhiteSpace(first.DisplayText);
            second.OwnerId = secondId ?? string.Empty;
            second.DisplayText = names.ElementAtOrDefault(1) ?? string.Empty;
            second.IsVisible = !string.IsNullOrWhiteSpace(second.DisplayText);
        }
    }

    string FormatRoundElapsed(DateTime roundTime)
    {
        var elapsed = roundTime - currentMatch.MatchDate;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        return elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    Button CreateRoundActionButton(string text, Color color, Func<Task> action)
    {
        var button = new Button
        {
            Text = text,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = color,
            BackgroundColor = Color.FromArgb("#101010"),
            BorderColor = color,
            BorderWidth = 1,
            CornerRadius = 7,
            Padding = 0,
            HeightRequest = 36,
            WidthRequest = 36,
            MinimumWidthRequest = 36,
            MinimumHeightRequest = 36
        };

        button.Clicked += async (_, _) => await action();
        return button;
    }

    async Task EnsureRoundEditableAsync()
    {
        if (!currentMatch.IsLocked)
            return;

        await ShowInfoDialogAsync(
            "المباراة مقفلة",
            "لا يمكن تعديل أو حذف أو نقل الجولات بعد اعتماد المباراة.");

        throw new InvalidOperationException("Match is locked.");
    }

    async Task TransferRoundAsync(RoundModel round)
    {
        try { await EnsureRoundEditableAsync(); }
        catch { return; }

        round.WinnerTeamId = round.WinnerTeamId == 1 ? 2 : 1;
        round.WinnerTeam =
            round.WinnerTeamId == 1
                ? $"{Team1Name.Text} ({Team1Players.Text})"
                : $"{Team2Name.Text} ({Team2Players.Text})";

        await ApplyScoreMutationAsync();
    }

    async Task DeleteRoundAsync(RoundModel round)
    {
        try { await EnsureRoundEditableAsync(); }
        catch { return; }

        bool confirm = await ShowConfirmDialogAsync(
            "حذف الجولة",
            "هل تريد حذف هذه الجولة؟",
            "نعم",
            "لا");

        if (!confirm)
            return;

        roundsHistory.Remove(round);
        int counter = 1;
        foreach (var r in roundsHistory)
            r.RoundNumber = counter++;

        roundNumber = counter;
        await ApplyScoreMutationAsync();
    }

    async Task EditRoundAsync(RoundModel round)
    {
        try { await EnsureRoundEditableAsync(); }
        catch { return; }

        string result = await DisplayPromptAsync(
            "تعديل الجولة",
            "أدخل النقاط الجديدة",
            "حفظ",
            "إلغاء",
            keyboard: Keyboard.Numeric);

        if (!int.TryParse(result, out int newPoints))
            return;

        round.Points = newPoints;
        await ApplyScoreMutationAsync();
    }

    void RecalculateScores()
    {
        team1Score = 0;
        team2Score = 0;

        foreach (var round in roundsHistory
            .OrderBy(r => r.RoundNumber))
        {
            if (round.WinnerTeamId == 1)
            {
                team1Score += round.Points;
            }
            else
            {
                team2Score += round.Points;
            }

            round.Team1NewScore = team1Score;
            round.Team2NewScore = team2Score;
        }

        Team1Score.Text =
            team1Score.ToString();

        Team2Score.Text =
            team2Score.ToString();

       

        UpdateLeaderUI();
        UpdateWinRate();
    }

    MatchVictoryEvaluation EvaluateMatchVictory()
    {
        if (isLocalRules)
        {
            if (team1Score >= 101 && team2Score < 25)
            {
                return new MatchVictoryEvaluation(
                    true,
                    1,
                    Team1Name.Text,
                    Team1Players.Text,
                    true);
            }

            if (team2Score >= 101 && team1Score < 25)
            {
                return new MatchVictoryEvaluation(
                    true,
                    2,
                    Team2Name.Text,
                    Team2Players.Text,
                    true);
            }
        }

        if (team1Score >= targetScore && team1Score > team2Score)
        {
            return new MatchVictoryEvaluation(
                true,
                1,
                Team1Name.Text,
                Team1Players.Text,
                false);
        }

        if (team2Score >= targetScore && team2Score > team1Score)
        {
            return new MatchVictoryEvaluation(
                true,
                2,
                Team2Name.Text,
                Team2Players.Text,
                false);
        }

        return new MatchVictoryEvaluation(
            false,
            0,
            string.Empty,
            string.Empty,
            false);
    }

    async Task ApplyScoreMutationAsync()
    {
        RecalculateScores();

        var victory =
            EvaluateMatchVictory();

        foreach (var round in roundsHistory)
            round.IsMeles = false;

        if (victory.IsCompleted)
        {
            var lastRound =
                roundsHistory.LastOrDefault();

            if (lastRound != null)
                lastRound.IsMeles = victory.HasMeles;

            RefreshRoundsHistory();
            UpdateLastRoundCard();
            UpdateLeaderUI();

            await FinishMatch(
                victory.WinnerName,
                victory.WinnerPlayers,
                victory.HasMeles);
            return;
        }

        gameFinished = false;
        matchSaved = false;
        SynchronizeActiveMatchSnapshot();

        await GameService.SaveMatchAsync(
            currentMatch);

        RefreshRoundsHistory();
        UpdateLastRoundCard();
        UpdateLeaderUI();
    }

    void SynchronizeActiveMatchSnapshot()
    {
        currentMatch.Team1Score =
            team1Score;

        currentMatch.Team2Score =
            team2Score;

        currentMatch.RoundNumber =
            roundNumber;

        currentMatch.RoundsHistory =
            roundsHistory;

        currentMatch.IsFinished =
            false;

        currentMatch.IsLocked =
            false;

        currentMatch.WinnerTeam =
            string.Empty;

        currentMatch.WinnerTeamName =
            string.Empty;

        currentMatch.WinnerTeamId =
            string.Empty;

        currentMatch.HasMeles =
            false;

        currentMatch.LastPlayedTime =
            DateTime.Now;
    }
    // =========================
    // ADD POINTS
    // =========================

    void OnClearKeypadClicked(
    object sender,
    EventArgs e)
    {
        keypadValue = "";

        KeypadDisplay.Text = "0";
    }
    async Task AddPoints(
     int team,
     int points)
    {
        await ShowFloatingScore(team, points);

        bool meles = false;
        int oldTeam1 =
            team1Score;
        int oldTeam2 =
            team2Score;
        // TEAM 1
        if (team == 1)
        {
            team1Score += points;
            Team1Score.Text =
                team1Score.ToString();
            // LOCAL MELES
            if (isLocalRules &&
                team1Score >= 101 &&
                team2Score < 25)
            {
                meles = true;
            }
        }
        // TEAM 2
        else
        {
            team2Score += points;
            Team2Score.Text =
                team2Score.ToString();
            // LOCAL MELES
            if (isLocalRules &&
                team2Score >= 101 &&
                team1Score < 25)
            {
                meles = true;
            }
        }
        // SAVE ROUND
        roundsHistory.Add(
          new RoundModel
          {
              RoundNumber = roundNumber,

              WinnerTeam =
                  team == 1
                  ? Team1Name.Text
                  : Team2Name.Text,

              LoserTeam =
                  team == 1
                  ? Team2Name.Text
                  : Team1Name.Text,


              WinnerProfileId =
    team == 1
    ? currentMatch.Team1Id
    : currentMatch.Team2Id,

              LoserProfileId =
    team == 1
    ? currentMatch.Team2Id
    : currentMatch.Team1Id,

              Points = points,

              IsMeles = meles,

              Team1OldScore = oldTeam1,
              Team2OldScore = oldTeam2,

              Team1NewScore = team1Score,
              Team2NewScore = team2Score,

              RoundTime = DateTime.Now,

              WinnerTeamId = team,

              WinnerTeamColor =
                  team == 1
                  ? team1Profile?.ColorHex ?? "#1E3A5F"
                  : team2Profile?.ColorHex ?? "#1E3A5F",

              LoserTeamColor =
                  team == 1
                  ? team2Profile?.ColorHex ?? "#5A1E1E"
                  : team1Profile?.ColorHex ?? "#5A1E1E",

              WinnerTeamEmblem =
                  team == 1
                  ? team1Profile?.Emblem ?? ""
                  : team2Profile?.Emblem ?? "",

              LoserTeamEmblem =
                  team == 1
                  ? team2Profile?.Emblem ?? ""
                  : team1Profile?.Emblem ?? ""
          });
        roundNumber++;
        await ApplyScoreMutationAsync();

    }

    void UpdateLastRoundCard()
    {
        if (!roundsHistory.Any())
        {
            LastRoundTeamLabel.Text =
                "لا توجد جولات";

            LastRoundPointsLabel.Text =
                "";

            LastRoundTimeLabel.Text =
                "";

            return;
        }

        var last =
            roundsHistory.Last();

        LastRoundTeamLabel.Text =
    $"⚡ {last.WinnerTeam}";

        LastRoundPointsLabel.Text =
       $"+{last.Points}";
        LastRoundPointsLabel.TextColor =
    last.WinnerTeamId == 1
    ? Color.FromArgb("#76FF03")
    : Color.FromArgb("#3D5AFE");
        TimeSpan elapsed =
            DateTime.Now - last.RoundTime;

        if (elapsed.TotalSeconds < 60)
        {
            LastRoundTimeLabel.Text =
                $"منذ {(int)elapsed.TotalSeconds} ثانية";
        }
        else if (elapsed.TotalMinutes < 60)
        {
            LastRoundTimeLabel.Text =
                $"منذ {(int)elapsed.TotalMinutes} دقيقة";
        }
        else
        {
            LastRoundTimeLabel.Text =
                $"منذ {(int)elapsed.TotalHours} ساعة";
        }
    }
    // =========================
    // FINISH MATCH
    // =========================
    async Task FinishMatch(
        string winner,
        string winnerPlayers,
        bool meles)
    {
        if (matchSaved)
            return;
        matchSaved = true;
        gameFinished = true;
        // MATCH FINISHED
        currentMatch.IsFinished = true;
        currentMatch.IsLocked=true;
        // WINNER
        currentMatch.WinnerTeam =
     $"{winner} ({winnerPlayers})";

        currentMatch.WinnerTeamName =
            winner;
        // MELES
        currentMatch.HasMeles =
            meles;
        // SCORES
        currentMatch.Team1Score =
            team1Score;
        currentMatch.Team2Score =
            team2Score;
        // ROUNDS
        currentMatch.RoundNumber =
            roundNumber;
        currentMatch.RoundsHistory =
            roundsHistory;
        // DATES
        currentMatch.MatchEndDate =
            DateTime.Now;
        currentMatch.LastPlayedTime =
            DateTime.Now;
        currentMatch.MatchDurationMinutes =
            Math.Max(
                1,
                (int)(
                    currentMatch.MatchEndDate -
                    currentMatch.MatchDate)
                .TotalMinutes);
        // DISPLAY TITLE
        currentMatch.DisplayTitle =
            $"{Team1Name.Text} ({Team1Players.Text}) VS " +
            $"{Team2Name.Text} ({Team2Players.Text})";

        currentMatch.WinnerTeamId =
            team1Score > team2Score
            ? currentMatch.Team1Id
            : currentMatch.Team2Id;

        // SAVE MATCH
        await GameService.SaveMatchAsync(
            currentMatch);
        // UPDATE RANKINGS
        await RankingService.UpdateRankingsAsync(
            currentMatch);
        AppEvents.RaiseDataChanged();
        AppEvents.RaiseMatchesChanged();
        AppEvents.RaiseRankingsChanged();
        AppEvents.RaiseTeamsChanged();
        if (!string.IsNullOrWhiteSpace(currentMatch.Team1Id))
            AppEvents.RaiseTeamAssetsChanged(currentMatch.Team1Id);
        if (!string.IsNullOrWhiteSpace(currentMatch.Team2Id))
            AppEvents.RaiseTeamAssetsChanged(currentMatch.Team2Id);


        int coinsAwarded = await AwardWinnerCoinsAsync(currentMatch.WinnerTeamId);
        await LoadTeamProfiles();

        bool continuePlaying = await ShowMatchDialogAsync(
            meles ? "ملص ملكي" : "انتهت المباراة",
            $"{winner}\n{winnerPlayers}\n\nالنتيجة {team1Score} - {team2Score}",
            "استمرار اللعب",
            "الخروج",
            coinsAwarded,
            true);

        if (continuePlaying)
            StartNewMatchSameTeams();
        else
            await Navigation.PopAsync();

    }
    async Task<int> AwardWinnerCoinsAsync(string winnerTeamId)
    {
        const int baseReward = 25;

        try
        {
            string? playerId = await PlayerStoreIdentityService.GetDeviceIdentityPlayerIdAsync();
            if (string.IsNullOrWhiteSpace(playerId))
                return 0;

            int reward = currentMatch.HasMeles ? baseReward * 2 : baseReward;
            await PlayerWalletService.CreditAsync(playerId, coins: reward);
            await ApplyMatchFooterVisualsAsync();
            return reward;
        }
        catch
        {
            return 0;
        }
    }

    async Task<bool> ShowMatchDialogAsync(
        string title,
        string message,
        string primaryText,
        string secondaryText,
        int coinsAwarded = 0,
        bool showReward = false)
    {
        dialogCompletion = new TaskCompletionSource<bool>();

        MatchDialogTitle.Text = title;
        MatchDialogMessage.Text = message;
        MatchDialogPrimaryButton.Text = primaryText;
        MatchDialogSecondaryButton.Text = secondaryText;

        bool hasSecondary = !string.IsNullOrWhiteSpace(secondaryText);
        MatchDialogSecondaryButton.IsVisible = hasSecondary;
        Grid.SetColumn(MatchDialogPrimaryButton, hasSecondary ? 1 : 0);
        Grid.SetColumnSpan(MatchDialogPrimaryButton, hasSecondary ? 1 : 2);
        MatchDialogRewardCard.IsVisible = showReward;
        MatchDialogRewardLabel.Text = $"+{coinsAwarded:N0} coins";

        MatchDialogCard.Scale = 0.92;
        MatchDialogCard.Opacity = 0;
        MatchDialogOverlay.IsVisible = true;

        await Task.WhenAll(
            MatchDialogCard.ScaleToAsync(1, 180, Easing.CubicOut),
            MatchDialogCard.FadeToAsync(1, 160, Easing.CubicOut));

        if (showReward)
            _ = PlayVictoryCelebrationAsync();

        return await dialogCompletion.Task;
    }

    async Task PlayVictoryCelebrationAsync()
    {
        MatchDialogIcon.Rotation = -8;
        MatchDialogIcon.Scale = 0.88;
        MatchDialogRewardCard.Scale = 0.96;

        await Task.WhenAll(
            MatchDialogIcon.ScaleToAsync(1.18, 180, Easing.CubicOut),
            MatchDialogIcon.RotateToAsync(8, 180, Easing.CubicOut),
            MatchDialogRewardCard.ScaleToAsync(1.04, 180, Easing.CubicOut));

        await Task.WhenAll(
            MatchDialogIcon.ScaleToAsync(1.0, 160, Easing.CubicInOut),
            MatchDialogIcon.RotateToAsync(0, 160, Easing.CubicInOut),
            MatchDialogRewardCard.ScaleToAsync(1.0, 160, Easing.CubicInOut));

        await MatchDialogRewardLabel.ScaleToAsync(1.16, 120, Easing.CubicOut);
        await MatchDialogRewardLabel.ScaleToAsync(1.0, 120, Easing.CubicIn);
    }

    Task<bool> ShowConfirmDialogAsync(string title, string message, string primaryText, string secondaryText) =>
        ShowMatchDialogAsync(title, message, primaryText, secondaryText);

    async Task ShowInfoDialogAsync(string title, string message, string buttonText = "حسناً")
    {
        await ShowMatchDialogAsync(title, message, buttonText, string.Empty);
    }

    async void OnDialogPrimaryClicked(object sender, EventArgs e)
    {
        await HideMatchDialogAsync(true);
    }

    async void OnDialogSecondaryClicked(object sender, EventArgs e)
    {
        await HideMatchDialogAsync(false);
    }

    async Task HideMatchDialogAsync(bool result)
    {
        if (dialogCompletion == null)
            return;

        await Task.WhenAll(
            MatchDialogCard.ScaleToAsync(0.96, 120, Easing.CubicIn),
            MatchDialogCard.FadeToAsync(0, 100, Easing.CubicIn));

        MatchDialogOverlay.IsVisible = false;
        dialogCompletion.TrySetResult(result);
        dialogCompletion = null;
    }

    // =========================
    // UPDATE UI
    // =========================
    void UpdateLeaderUI()
    {
        if (team1Score > team2Score)
        {
            Team1Score.TextColor =

     Color.FromArgb("#76FF03");
            Team2Score.TextColor = Colors.Gold;
            Team1Card.BackgroundColor =
    Color.FromArgb("#142000");

            Team2Card.BackgroundColor =
                Colors.Transparent;

        
        

            LeaderLabel.Text = Team1Name.Text;

           
        }
        else if (team2Score > team1Score)
        {
            Team2Score.TextColor =
     Color.FromArgb("#76FF03");
            Team1Score.TextColor = Colors.Gold;
            Team2Card.BackgroundColor =
    Color.FromArgb("#142000");

            Team1Card.BackgroundColor =
                Colors.Transparent;

          

            LeaderLabel.Text = Team2Name.Text;

          
        }
        else
        {
            Team1Score.TextColor = Colors.Gold;
            Team2Score.TextColor = Colors.Gold;
            Team1Card.BackgroundColor =
    Colors.Transparent;

            Team2Card.BackgroundColor =
                Colors.Transparent;
            

            LeaderLabel.Text = "تعادل";

        }

        RoundsCountLabel.Text =
            $"{roundsHistory.Count} / {Math.Max(roundNumber, 1)}";

        MatchTimeLabel.Text =
            FormatMatchDuration();

        Team1TargetLabel.Text = $"الهدف: {targetScore}";
        Team2TargetLabel.Text = $"الهدف: {targetScore}";
        MatchStatusLabel.Text = gameFinished ? "منتهية" : "جارية";
        ApplyTeamProgressVisuals();

        UpdateWinRate();
        RestoreTeamCardVisuals();
    }

    string FormatMatchDuration()
    {
        var elapsed = DateTime.Now - currentMatch.MatchDate;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        return elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }
    void UpdateWinRate()
    {
        int total =
            team1Score + team2Score;

        if (total == 0)
        {
            WinRateBar.Progress = 0.5;

            WinRateLabel.Text =
                "50% - 50%";

            return;
        }

        double team1Rate =
            (double)team1Score / total;

        double team2Rate =
            (double)team2Score / total;

        WinRateBar.Progress =
            team1Rate;

        WinRateLabel.Text =
            $"{Team1Name.Text} {team1Rate * 100:0}%  |  {Team2Name.Text} {team2Rate * 100:0}%";
    }

    void SelectTeam(int team)
    {
        selectedTeam = team;

        if (team == 1)
        {
            _ = Team1Card.ScaleToAsync(1.05, 180);
            _ = Team2Card.ScaleToAsync(1.00, 180);

            Team1Card.BackgroundColor =
                Color.FromArgb("#1A1A00");

            Team2Card.BackgroundColor =
                Colors.Transparent;

            Team1Name.TextColor =
                Colors.Gold;

            Team2Name.TextColor =
                Colors.White;

            Team1Score.TextColor =
                Colors.Gold;

            Team2Score.TextColor =
                Colors.White;

            Team1Emblem.Scale = 1.15;
            Team2Emblem.Scale = 1.00;
        }
        else
        {
            _ = Team2Card.ScaleToAsync(1.05, 180);
            _ = Team1Card.ScaleToAsync(1.00, 180);

            Team2Card.BackgroundColor =
                Color.FromArgb("#1A1A00");

            Team1Card.BackgroundColor =
                Colors.Transparent;

            Team2Name.TextColor =
                Colors.Gold;

            Team1Name.TextColor =
                Colors.White;

            Team2Score.TextColor =
                Colors.Gold;

            Team1Score.TextColor =
                Colors.White;

            Team2Emblem.Scale = 1.15;
            Team1Emblem.Scale = 1.00;
        }

        RestoreTeamCardVisuals();
    }

    void RestoreTeamCardVisuals()
    {
        if (team1Identity != null)
            ApplyTeamIdentityVisual(Team1Card, Team1BackgroundImage, team1Identity);

        if (team2Identity != null)
            ApplyTeamIdentityVisual(Team2Card, Team2BackgroundImage, team2Identity);
    }
    void OnKeypadClicked(
    object sender,
    EventArgs e)
    {
        Button button =
            (Button)sender;

        keypadValue +=
            button.Text;

        KeypadDisplay.Text =
            keypadValue;
    }

    void OnDeleteDigitClicked(
    object sender,
    EventArgs e)
    {
        if (string.IsNullOrEmpty(keypadValue))
            return;

        keypadValue =
            keypadValue[..^1];

        KeypadDisplay.Text =
            string.IsNullOrEmpty(keypadValue)
            ? "0"
            : keypadValue;
    }

    async void OnAddScoreClicked(
    object sender,
    EventArgs e)
    {
        if (!int.TryParse(
            keypadValue,
            out int points))
            return;

        keypadValue = "";

        KeypadDisplay.Text = "0";

        await AddPoints(
            selectedTeam,
            points);
    }
    // =========================
    // RESET
    // =========================
    async void OnResetClicked(
        object sender,
        EventArgs e)
    {
        bool confirm = await ShowConfirmDialogAsync(
            "إعادة تعيين",
            "هل تريد إعادة المباراة من الصفر؟",
            "نعم",
            "إلغاء");

        if (!confirm)
            return;

        StartNewMatchSameTeams();
    }
    // =========================
    // UNDO
    // =========================
    async void OnUndoClicked(
        object sender,
        EventArgs e)
    {
        if (roundsHistory.Count == 0)
            return;

        RoundModel lastRound = roundsHistory.Last();
        roundsHistory.Remove(lastRound);
        roundNumber = Math.Max(1, roundsHistory.Count + 1);

        await ApplyScoreMutationAsync();
        await ShowInfoDialogAsync("تم", "تم التراجع عن الجولة الأخيرة.");
    }
    // =========================
    // FINISH BUTTON
    // =========================
    // =========================
    // FINISH BUTTON
    // =========================

    async void OnFinishClicked(
        object sender,
        EventArgs e)
    {
        if (roundsHistory.Count == 0)
        {
            await ShowInfoDialogAsync(
                "لا يمكن إنهاء المباراة",
                "لم يتم لعب أي جولة بعد.");
            return;
        }

        if (team1Score == team2Score)
        {
            await ShowInfoDialogAsync(
                "تعادل",
                "لا يمكن إنهاء المباراة والنتيجة متعادلة.");
            return;
        }

        string winner = team1Score > team2Score ? Team1Name.Text : Team2Name.Text;
        string winnerPlayers = team1Score > team2Score ? Team1Players.Text : Team2Players.Text;

        bool confirm = await ShowConfirmDialogAsync(
            "إنهاء المباراة",
            $"هل تريد إنهاء المباراة؟\n\nالفائز: {winner}",
            "إنهاء",
            "إلغاء");

        if (!confirm)
            return;

        await FinishMatch(winner, winnerPlayers, false);
    }
    // =========================
    // BACK
    // =========================
    async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        if (gameFinished || roundsHistory.Count == 0)
        {
            await Navigation.PopAsync();
            return;
        }

        bool save = await ShowConfirmDialogAsync(
            "حفظ المباراة",
            "هل تريد حفظ المباراة واستكمالها لاحقاً؟",
            "نعم",
            "لا");

        if (!save)
        {
            await Navigation.PopAsync();
            return;
        }

        currentMatch.Team1Score = team1Score;
        currentMatch.Team2Score = team2Score;
        currentMatch.RoundNumber = roundNumber;
        currentMatch.RoundsHistory = roundsHistory;
        currentMatch.LastPlayedTime = DateTime.Now;
        currentMatch.IsFinished = false;
        currentMatch.IsLocked = false;

        await GameService.SaveMatchAsync(currentMatch);
        AppEvents.RaiseDataChanged();
        AppEvents.RaiseMatchesChanged();
        await Navigation.PopAsync();
    }

    async void OnAddTeam1Clicked(
    object sender,
    EventArgs e)
    {
        if (!int.TryParse(
            keypadValue,
            out int points))
            return;

        keypadValue = "";
        KeypadDisplay.Text = "0";

        await AddPoints(1, points);
    }

    async void OnAddTeam2Clicked(
        object sender,
        EventArgs e)
    {
        if (!int.TryParse(
            keypadValue,
            out int points))
            return;

        keypadValue = "";
        KeypadDisplay.Text = "0";

        await AddPoints(2, points);
    }

    //=========================
    // تصفير الجدول
    void StartNewMatchSameTeams()
    {
        string oldTeam1Id = currentMatch.Team1Id;
        string oldTeam2Id = currentMatch.Team2Id;
        string oldTeam1Emblem = currentMatch.Team1Emblem;
        string oldTeam2Emblem = currentMatch.Team2Emblem;
        string oldTeam1Color = currentMatch.Team1ColorHex;
        string oldTeam2Color = currentMatch.Team2ColorHex;
        string oldTeam1Player1Id = currentMatch.Team1Player1Id;
        string oldTeam1Player2Id = currentMatch.Team1Player2Id;
        string oldTeam2Player1Id = currentMatch.Team2Player1Id;
        string oldTeam2Player2Id = currentMatch.Team2Player2Id;

        team1Score = 0;
        team2Score = 0;
        roundNumber = 1;
        keypadValue = string.Empty;
        roundsHistory.Clear();

        Team1Score.Text = "0";
        Team2Score.Text = "0";
        KeypadDisplay.Text = "0";
        LeaderLabel.Text = "تعادل";
        LastRoundTeamLabel.Text = "لا توجد جولات";
        LastRoundPointsLabel.Text = string.Empty;
        LastRoundTimeLabel.Text = string.Empty;
        RoundsContainer.Children.Clear();

        gameFinished = false;
        matchSaved = false;

        currentMatch = new SavedMatch
        {
            Team1Id = oldTeam1Id,
            Team2Id = oldTeam2Id,
            Team1Emblem = oldTeam1Emblem,
            Team2Emblem = oldTeam2Emblem,
            Team1ColorHex = oldTeam1Color,
            Team2ColorHex = oldTeam2Color,
            Team1Name = Team1Name.Text,
            Team2Name = Team2Name.Text,
            Team1Players = Team1Players.Text,
            Team2Players = Team2Players.Text,
            Team1Player1Id = oldTeam1Player1Id,
            Team1Player2Id = oldTeam1Player2Id,
            Team2Player1Id = oldTeam2Player1Id,
            Team2Player2Id = oldTeam2Player2Id,
            IsLocalRules = isLocalRules,
            MatchDate = DateTime.Now,
            LastPlayedTime = DateTime.Now,
            IsFinished = false,
            IsLocked = false,
            RoundNumber = 1,
            RoundsHistory = new List<RoundModel>()
        };

        RefreshRoundsHistory();
        UpdateLeaderUI();
        UpdateWinRate();
        _ = LoadTeamProfiles();
    }

    async void OnMenuClicked(object sender, EventArgs e)
    {
        SideMenuOverlay.IsVisible = true;

        await SideMenuPanel.TranslateToAsync(
            0,
            0,
            250,
            Easing.CubicOut);
    }

    async void CloseMenuClicked(
      object sender,
      TappedEventArgs e)
    {
        await SideMenuPanel.TranslateToAsync(
            -280,
            0,
            250,
            Easing.CubicIn);

        SideMenuOverlay.IsVisible = false;
    }

    // =========================
    // SPECIAL HONORS
    //  =========================

    void ApplySpecialHonorIcon(
     TeamProfileModel team,
     List<PlayerProfileModel> players,
     Image targetIcon)
    {
        targetIcon.Source = null;
        targetIcon.IsVisible = false;
        targetIcon.Opacity = 0;
    }
}


