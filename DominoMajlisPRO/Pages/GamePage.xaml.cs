using DominoMajlisPRO.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Effects;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls;
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
    
    // VisualEventBus subscription tokens
    IDisposable? teamEmblemChangedSubscription;
    IDisposable? teamColorChangedSubscription;
    IDisposable? teamEffectChangedSubscription;
    IDisposable? teamEmblemBackgroundChangedSubscription;
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
        BindTeamNameSurfaces();

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
        BindTeamNameSurfaces();
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
        
        // Subscribe to VisualEventBus team identity events
        teamEmblemChangedSubscription = VisualEventBus.Subscribe(
            EventCategory.Team,
            OnTeamEmblemChanged);
        teamColorChangedSubscription = VisualEventBus.Subscribe(
            EventCategory.Team,
            OnTeamColorChanged);
        teamEffectChangedSubscription = VisualEventBus.Subscribe(
            EventCategory.Team,
            OnTeamEffectChanged);
        teamEmblemBackgroundChangedSubscription = VisualEventBus.Subscribe(
            EventCategory.Team,
            OnTeamEmblemBackgroundChanged);

        await RefreshLiveTeamVisualsAsync();
    }

    void BindTeamNameSurfaces()
    {
        GalleryEngine.Components.NameSurfaceBinder.BindTeam(
            Team1Name,
            team1Id,
            Team1Name.Text);
        GalleryEngine.Components.NameSurfaceBinder.BindTeam(
            Team2Name,
            team2Id,
            Team2Name.Text);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Dispose VisualEventBus subscriptions
        teamEmblemChangedSubscription?.Dispose();
        teamColorChangedSubscription?.Dispose();
        teamEffectChangedSubscription?.Dispose();
        teamEmblemBackgroundChangedSubscription?.Dispose();
        
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

        team1Profile =
            teams.FirstOrDefault(
                x => x.TeamId == team1Id);

        team2Profile =
            teams.FirstOrDefault(
                x => x.TeamId == team2Id);

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

    // VisualEventBus team identity event handler - reuse existing refresh path
    void HandleTeamIdentityEvent(EventEntry eventEntry)
    {
        if (eventEntry.EventData == null)
            return;
        
        if (!eventEntry.EventData.ContainsKey(VisualIdentityPayloadKeys.TeamId))
            return;
        
        eventEntry.EventData.TryGetValue(VisualIdentityPayloadKeys.TeamId, out var teamIdObject);
        
        if (teamIdObject is not string teamId || string.IsNullOrWhiteSpace(teamId))
            return;
        
        // Filter: Only refresh if teamId matches team1Id or team2Id
        if (!string.Equals(teamId, team1Id, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(teamId, team2Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        
        _ = MainThread.InvokeOnMainThreadAsync(RefreshLiveTeamVisualsAsync);
    }

    void OnTeamEmblemChanged(EventEntry eventEntry) => HandleTeamIdentityEvent(eventEntry);
    void OnTeamColorChanged(EventEntry eventEntry) => HandleTeamIdentityEvent(eventEntry);
    void OnTeamEffectChanged(EventEntry eventEntry) => HandleTeamIdentityEvent(eventEntry);
    void OnTeamEmblemBackgroundChanged(EventEntry eventEntry) => HandleTeamIdentityEvent(eventEntry);

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
        LivingEmblemBehavior.Attach(Team1Emblem, team1Id);
        LivingEmblemBehavior.Attach(Team2Emblem, team2Id);
        ApplyTeamIdentityVisual(Team1Card, team1Identity);
        ApplyTeamIdentityVisual(Team2Card, team2Identity);
    }

    static void ApplyTeamIdentityVisual(
        VisualElement card,
        TeamIdentityModel identity)
    {
        if (card is not Layout layout)
            return;
        layout.BackgroundColor = ResolveIdentityBackground(identity);
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

                targetLabel.FadeTo(
                    0,
                    900));

        var cardPulse =
            Task.Run(async () =>
            {
                try
                {
                    await targetCard.ScaleTo(
                        1.12,
                        120,
                        Easing.CubicOut);

                    await targetCard.ScaleTo(
                        1.08,
                        120,
                        Easing.CubicIn);
                }
                catch { }
            });

        var scorePulse =
            Task.Run(async () =>
            {
                try
                {
                    await targetScore.ScaleTo(
                        1.20,
                        120,
                        Easing.CubicOut);

                    await targetScore.ScaleTo(
                        1.00,
                        120,
                        Easing.CubicIn);
                }
                catch { }
            });

        await Task.WhenAll(
            floatingTask,
            cardPulse,
            scorePulse);
    }
    // Timer
    // =========================
    void StartMatchTimer()
    {
        Dispatcher.StartTimer(
            TimeSpan.FromSeconds(1),
            () =>
            {
                var elapsed =
                    DateTime.Now - currentMatch.MatchDate;

                MatchTimeLabel.Text =
                    $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

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

        var roundsToShow =
            showAllRounds
            ? roundsHistory
            : roundsHistory.TakeLast(5);

        foreach (var round in roundsToShow)
        {
            Frame row = new()
            {
                BackgroundColor =
         Color.FromArgb("#151515"),

                BorderColor =
         round.WinnerTeamId == 1
         ? Color.FromArgb("#76FF03")
         : Color.FromArgb("#3D5AFE"),

                CornerRadius = 20,

                Padding = 12,

                Margin = new Thickness(0, 6)
            };
            if (round == roundsHistory.Last())
            {
                row.BorderColor =
                    Color.FromArgb("#FFD700");

                row.BackgroundColor =
                    Color.FromArgb("#1A1A00");
            }
            Grid grid = new()
            {
                ColumnDefinitions =
    {
        new ColumnDefinition(GridLength.Star),
        new ColumnDefinition(60)
    },
                Padding = 10
            };
            VerticalStackLayout info =
    new()
    {
        Spacing = 2
    };

            info.Children.Add(
                new Label
                {
                    Text = $"🏆 {round.WinnerTeam}",
                    TextColor = Colors.White,
                    FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone
    ? 16
    : 18,
                    FontAttributes = FontAttributes.Bold
                });

            info.Children.Add(
                new Label
                {
                    Text = $"+{round.Points} نقطة",
                    TextColor =
                        round.WinnerTeamId == 1
                        ? Colors.Lime
                        : Colors.DeepSkyBlue,
                    FontSize = DeviceInfo.Idiom == DeviceIdiom.Phone
    ? 18
    : 20,
                    LineBreakMode = LineBreakMode.NoWrap,
                    FontAttributes = FontAttributes.Bold
                });

            info.Children.Add(
                new Label
                {
                    Text =
    $"النتيجة: {round.Team1NewScore} - {round.Team2NewScore}",
                    TextColor = Colors.Gold,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold
                });

            grid.Add(info);

            grid.Add(
                new Label
                {
                    Text = $"#{round.RoundNumber}",
                    TextColor = Colors.Gray,
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                },
                1,
                0);


            TapGestureRecognizer tap =
    new();

            tap.Tapped += async (s, e) =>
            {
                if (currentMatch.IsLocked)
{
    await DisplayAlert(
        "المباراة مقفلة",
        "لا يمكن تعديل أو حذف أو نقل الجولات بعد اعتماد المباراة.",
        "حسناً");

    return;
}
                string action =
    await DisplayActionSheet(
        $"الجولة {round.RoundNumber}\n{round.WinnerTeam}\n+{round.Points}",
        "إلغاء",
        null,
        "✏️ تعديل النقاط",
        "🔄 نقل للفريق الآخر",
        "🗑 حذف الجولة");

                if (action == "🔄 نقل للفريق الآخر")
                {
                    round.WinnerTeamId =
                        round.WinnerTeamId == 1 ? 2 : 1;

                    round.WinnerTeam =
                        round.WinnerTeamId == 1
                        ? $"{Team1Name.Text} ({Team1Players.Text})"
                        : $"{Team2Name.Text} ({Team2Players.Text})";

                    RecalculateScores();

                    RefreshRoundsHistory();

                    UpdateLastRoundCard();

                    UpdateLeaderUI();

                    return;
                }

                if (action == "🗑 حذف الجولة")
                {
                    bool confirm =
     await DisplayAlert(
         "حذف الجولة",
         "هل تريد حذف هذه الجولة؟",
         "نعم",
         "لا");

                    if (!confirm)
                        return;

                    roundsHistory.Remove(round);

                    int counter = 1;

                    foreach (var r in roundsHistory)
                    {
                        r.RoundNumber = counter++;
                    }

                    roundNumber = counter;
                 
                    RecalculateScores();
                    RefreshRoundsHistory();
                    UpdateLastRoundCard();
                }







                else if (action == "✏️ تعديل النقاط")
                {
                    string result =
                        await DisplayPromptAsync(
                            "تعديل الجولة",
                            "أدخل النقاط الجديدة",
                            "حفظ",
                            "إلغاء",
                            keyboard: Keyboard.Numeric);

                    if (!int.TryParse(
                        result,
                        out int newPoints))
                        return;

                    round.Points = newPoints;

                    RecalculateScores();
                    RefreshRoundsHistory();
                    UpdateLastRoundCard();
                }
            };

            row.GestureRecognizers.Add(tap);
            row.Content = grid;

            RoundsContainer.Children.Add(row);
        }
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
        string winner = "";
        string winnerPlayers = "";
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
            winner =
                Team1Name.Text;
            winnerPlayers =
                Team1Players.Text;
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
            winner =
                Team2Name.Text;
            winnerPlayers =
                Team2Players.Text;
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
        RefreshRoundsHistory();
        UpdateLastRoundCard();
        UpdateLeaderUI();
       
        // MELES
        if (meles)
        {
            await FinishMatch(
                winner,
                winnerPlayers,
                true);
            return;
        }
        // NORMAL WIN
        if (team1Score >= targetScore)
        {
            await FinishMatch(
                Team1Name.Text,
                Team1Players.Text,
                false);
            return;
        }
        if (team2Score >= targetScore)
        {
            await FinishMatch(
                Team2Name.Text,
                Team2Players.Text,
                false);
            return;
        }

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

        // Notify ranking views (teams + players) to refresh from fresh data.
        AppEvents.RaiseRankingsChanged();

        // ALERT
        if (meles)
        {
            await DisplayAlert(
                "🔥 ملص",
                $"الفائز:\n" +
                $"{winner}\n" +
                $"({winnerPlayers})\n\n" +
                $"تم تحقيق الملص",
                "ممتاز");
            StartNewMatchSameTeams();
        }
        else
        {
            await DisplayAlert(
     "🏆 انتهاء المباراة",
     $"الفائز:\n" +
     $"{winner}\n" +
     $"({winnerPlayers})",
     "ممتاز");

            StartNewMatchSameTeams();
        }
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
            roundsHistory.Count.ToString();

        MatchTimeLabel.Text =
            $"{(int)(DateTime.Now - currentMatch.MatchDate).TotalMinutes} دقيقة";

        UpdateWinRate();
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
            Team1Card.ScaleTo(1.05, 180);
            Team2Card.ScaleTo(1.00, 180);

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
            Team2Card.ScaleTo(1.05, 180);
            Team1Card.ScaleTo(1.00, 180);

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
        bool confirm =
            await DisplayAlert(
                "إعادة تعيين",
                "هل تريد إعادة المباراة؟",
                "نعم",
                "إلغاء");
        if (!confirm)
            return;
        team1Score = 0;
        team2Score = 0;
        roundNumber = 1;
        Team1Score.Text = "0";
        Team2Score.Text = "0";
        roundsHistory.Clear();
        gameFinished = false;
        matchSaved = false;
       
  currentMatch =
new SavedMatch
{
    Team1Id =
    currentMatch.Team1Id,

    Team2Id =
    currentMatch.Team2Id,

    Team1Emblem =
    currentMatch.Team1Emblem,

    Team2Emblem =
    currentMatch.Team2Emblem,

    Team1ColorHex =
    currentMatch.Team1ColorHex,

    Team2ColorHex =
    currentMatch.Team2ColorHex,

    Team1Name = Team1Name.Text,
    Team2Name = Team2Name.Text,

    Team1Players = Team1Players.Text,
    Team2Players = Team2Players.Text,

    Team1Player1Id =
        currentMatch.Team1Player1Id,

    Team1Player2Id =
        currentMatch.Team1Player2Id,

    Team2Player1Id =
        currentMatch.Team2Player1Id,

    Team2Player2Id =
        currentMatch.Team2Player2Id,

    MatchDate = DateTime.Now,
    LastPlayedTime = DateTime.Now,

    IsLocalRules = isLocalRules
};
        UpdateLeaderUI();
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
        RoundModel lastRound =
            roundsHistory.Last();
        team1Score =
            lastRound.Team1OldScore;
        team2Score =
            lastRound.Team2OldScore;
        Team1Score.Text =
            team1Score.ToString();
        Team2Score.Text =
            team2Score.ToString();
        roundsHistory.Remove(lastRound);
        roundNumber--;
        gameFinished = false;
        matchSaved = false;
        UpdateLeaderUI();
        await DisplayAlert(
            "تم",
            "تم التراجع عن الجولة الأخيرة",
            "OK");
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
        // =========================
        // NO ROUNDS PLAYED
        // =========================

        if (roundsHistory.Count == 0)
        {
            await DisplayAlert(
                "لا يمكن إنهاء المباراة",
                "لم يتم لعب أي جولة بعد",
                "OK");

            return;
        }

        // =========================
        // DRAW CHECK
        // =========================

        if (team1Score == team2Score)
        {
            await DisplayAlert(
                "تعادل",
                "لا يمكن إنهاء المباراة والنتيجة متعادلة",
                "OK");

            return;
        }

        // =========================
        // DETERMINE WINNER
        // =========================

        string winner =
            team1Score > team2Score
            ? Team1Name.Text
            : Team2Name.Text;

        string winnerPlayers =
            team1Score > team2Score
            ? Team1Players.Text
            : Team2Players.Text;

        // =========================
        // CONFIRM
        // =========================

        bool confirm =
            await DisplayAlert(
                "إنهاء المباراة",
                $"هل تريد إنهاء المباراة؟\n\nالفائز:\n{winner}",
                "إنهاء",
                "إلغاء");

        if (!confirm)
            return;

        // =========================
        // FINISH MATCH
        // =========================

        await FinishMatch(
            winner,
            winnerPlayers,
            false);
    }
    // =========================
    // BACK
    // =========================
    async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        // المباراة منتهية
        if (gameFinished)
        {
            await Navigation.PopAsync();
            return;
        }
        // لا توجد جولات
        if (roundsHistory.Count == 0)
        {
            await Navigation.PopAsync();
            return;
        }
        // SAVE QUESTION
        bool save =
            await DisplayAlert(
                "حفظ المباراة",
                "هل تريد حفظ المباراة واستكمالها لاحقاً؟",
                "نعم",
                "لا");
        if (!save)
        {
            await Navigation.PopAsync();
            return;
        }
        // SAVE DATA
        currentMatch.Team1Score =
            team1Score;
        currentMatch.Team2Score =
            team2Score;
        currentMatch.RoundNumber =
            roundNumber;
        currentMatch.RoundsHistory =
            roundsHistory;
        currentMatch.LastPlayedTime =
            DateTime.Now;
        // NOT FINISHED
        currentMatch.IsFinished =
            false;
        // SAVE
        await GameService.SaveMatchAsync(
            currentMatch);
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
        team1Score = 0;
        team2Score = 0;

        roundNumber = 1;

        roundsHistory.Clear();

        Team1Score.Text = "0";
        Team2Score.Text = "0";



        LeaderLabel.Text = "تعادل";

        LastRoundTeamLabel.Text =
            "لا توجد جولات";

        LastRoundPointsLabel.Text =
            "";

        LastRoundTimeLabel.Text =
            "";

        RoundsContainer.Children.Clear();

        gameFinished = false;
        matchSaved = false;

        currentMatch = new SavedMatch
        {
            Team1Id =
    currentMatch.Team1Id,

            Team2Id =
    currentMatch.Team2Id,

            Team1Emblem =
    currentMatch.Team1Emblem,

            Team2Emblem =
    currentMatch.Team2Emblem,

            Team1ColorHex =
    currentMatch.Team1ColorHex,

            Team2ColorHex =
    currentMatch.Team2ColorHex,


            Team1Name = Team1Name.Text,
            Team2Name = Team2Name.Text,

            Team1Players = Team1Players.Text,
            Team2Players = Team2Players.Text,

            Team1Player1Id =
         currentMatch.Team1Player1Id,

            Team1Player2Id =
         currentMatch.Team1Player2Id,

            Team2Player1Id =
         currentMatch.Team2Player1Id,

            Team2Player2Id =
         currentMatch.Team2Player2Id,

            IsLocalRules = isLocalRules,

            MatchDate = DateTime.Now,
            LastPlayedTime = DateTime.Now
        };

        RefreshRoundsHistory();

        UpdateLeaderUI();

        UpdateWinRate();
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
        var player1 =
            players.FirstOrDefault(x =>
                x.PlayerId == team.Player1Id);

        var player2 =
            players.FirstOrDefault(x =>
                x.PlayerId == team.Player2Id);

        bool hasFounder =
            (player1?.IsFounder ?? false) ||
            (player2?.IsFounder ?? false);

        bool hasDeveloper =
            (player1?.IsDeveloper ?? false) ||
            (player2?.IsDeveloper ?? false);

        if (hasFounder)
        {
            targetIcon.Source =
                "founder_gold.png";

            targetIcon.IsVisible =
                true;

            return;
        }

        if (hasDeveloper)
        {
            targetIcon.Source =
                "developer_gold.png";

            targetIcon.IsVisible =
                true;

            return;
        }

        targetIcon.IsVisible =
            false;
    }
}


