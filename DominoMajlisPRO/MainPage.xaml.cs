using DominoMajlisPRO.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Pages;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Pages;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using System.Linq;

namespace DominoMajlisPRO;

public partial class MainPage : ContentPage
{
    const string DefaultHeaderAvatar = "normal_avatar_1.png";


    List<TeamProfileModel> allTeams = new();
    List<TeamProfileModel> recentTeams = new();
    const int MaxRecentTeams = 5;
    TeamProfileModel? selectedTeam1;
    TeamProfileModel? selectedTeam2;
    readonly Dictionary<string, TeamIdentityModel> liveTeamIdentities =
        new(StringComparer.OrdinalIgnoreCase);

    sealed class TeamPickerVisualItem
    {
        public required TeamProfileModel Team { get; init; }
        public ImageSource Emblem { get; init; } =
            InventoryDisplayResolver.ResolveImageSource(
                "shield_3d.png");
        public string TeamName => Team.TeamName;
        public string Player1 => Team.Player1;
        public string Player2 => Team.Player2;
    }

    bool isPickingTeam1 = true;
    bool isDataExpanded = false;
    bool isSystemExpanded = false;
    bool isHonorsExpanded = false;
    bool isAboutExpanded = false;
    bool isSupportExpanded = false;
    bool isSecurityExpanded = false;



    List<TeamProfileModel> filteredTeams = new();




    string selectedRules = "عالمي";


    // =========================
    // SECRET HONORS ACCESS - LONG PRESS LOGO
    // =========================
    CancellationTokenSource? logoPressToken;
    bool logoPressed = false;
    bool identityChoiceShowing;
    int headerRefreshVersion;


    public MainPage()
    {
        InitializeComponent();
        UpdateMainSeasonCard();

        TeamPickerCollection.ItemsSource =
    filteredTeams;
        LoadTeams();


    }

    // =========================
    // PAGE LIFECYCLE

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.DataChanged -= OnAppDataChanged;
        AppEvents.PlayerProfileChanged -= OnMainProfileChanged;
        AppEvents.TeamsChanged -= OnAppDataChanged;
        AppEvents.MatchesChanged -= OnAppDataChanged;
        AppEvents.RankingsChanged -= OnAppDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged -= OnStoreEconomyChanged;
        AppEvents.CurrentUserChanged -= OnCurrentUserChanged;

        AppEvents.DataChanged += OnAppDataChanged;
        AppEvents.PlayerProfileChanged += OnMainProfileChanged;
        AppEvents.TeamsChanged += OnAppDataChanged;
        AppEvents.MatchesChanged += OnAppDataChanged;
        AppEvents.RankingsChanged += OnAppDataChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged += OnStoreEconomyChanged;
        AppEvents.CurrentUserChanged += OnCurrentUserChanged;

        if (await ApplicationUserService.RequiresIdentityChoiceAsync())
            await ShowIdentityLoginRegisterFlowAsync();

        UpdateMainSeasonCard();

        allTeams =
            await TeamProfileService.LoadTeamsAsync();

        await RefreshSelectedTeamsFromIds();

        await RefreshLiveTeamVisualsAsync();
        UpdateMatchPreview();

        await RefreshProfileStatus();
        await RefreshHeaderPlayerAsync();
    }
    // =========================
    // PAGE LIFECYCLE - DISAPPEARING
    // =========================
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        AppEvents.DataChanged -= OnAppDataChanged;
        AppEvents.PlayerProfileChanged -= OnMainProfileChanged;
        AppEvents.TeamsChanged -= OnAppDataChanged;
        AppEvents.MatchesChanged -= OnAppDataChanged;
        AppEvents.RankingsChanged -= OnAppDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged -= OnStoreEconomyChanged;
        AppEvents.CurrentUserChanged -= OnCurrentUserChanged;
    }
    // =========================
    // PAGE LIFECYCLE - PROFILE STATUS
    // =========================

    private string FormatTeamName(string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
            return "";

        return teamName.Length > 14
            ? teamName.Substring(0, 14) + "..."
            : teamName;
    }
    async void LoadTeams()
    {
        allTeams =
            await TeamProfileService.LoadTeamsAsync();

        if (allTeams.Count >= 1)
        {
            selectedTeam1 = allTeams[0];
            await UpdateTeam1Card();
        }

        if (allTeams.Count >= 2)
        {
            selectedTeam2 = allTeams[1];
            await UpdateTeam2Card();
        }
    }
    //Team Card1
    async Task UpdateTeam1Card()
    {
        if (selectedTeam1 == null)
            return;
        PreviewTeam1NameLabel.Text =
        FormatTeamName(selectedTeam1.TeamName);
        PreviewTeam1PlayersLabel.Text =
            $"{selectedTeam1.Player1} - {selectedTeam1.Player2}";

        await RefreshLiveTeamIdentityAsync(selectedTeam1);
        PreviewTeam1Logo.Source =
            ResolveStoredImage(GetLiveEmblem(selectedTeam1));
        var color =
            GetTeamColor(GetLiveTeamColor(selectedTeam1));

        PreviewTeam1NameLabel.TextColor = Colors.Gold;
        PreviewTeam1PlayersLabel.TextColor = Colors.Gold;



        UpdateMatchPreview();
    }

    // List Card Team1


    async void OnTeam1CardTapped(
     object sender,
     TappedEventArgs e)
    {
        if (allTeams.Count == 0)
        {
            await DisplayAlert(
                "تنبيه",
                "لا توجد فرق متاحة حالياً",
                "حسناً");

            return;
        }

        isPickingTeam1 = true;

        PickerTitle.Text =
            "اختر الفريق الأول";
        filteredTeams =
            recentTeams

           .Where(x =>
              selectedTeam2 == null ||
                 x.TeamId != selectedTeam2.TeamId).Concat(
                  allTeams
                .Where(x =>
                (selectedTeam2 == null ||
                  x.TeamId != selectedTeam2.TeamId) &&
                  !recentTeams.Any(r =>
                        r.TeamId == x.TeamId)))
            .ToList();

        await SetTeamPickerItemsAsync(filteredTeams);

        TeamPickerSearchEntry.Text = "";
        NoSearchResultsLabel.IsVisible = false;
        TeamPickerOverlay.IsVisible = true;
    }
    async void OnTeam2CardTapped(
        object sender,
        TappedEventArgs e)
    {
        if (allTeams.Count == 0)
        {
            await DisplayAlert(
                "تنبيه",
                "لا توجد فرق متاحة حالياً",
                "حسناً");

            return;
        }

        isPickingTeam1 = false;

        PickerTitle.Text =
            "اختر الفريق الثاني";

        filteredTeams =
     recentTeams
            .Where(x =>
              selectedTeam1 == null ||
             x.TeamId != selectedTeam1.TeamId).Concat(
         allTeams
         .Where(x =>
             (selectedTeam1 == null ||
             x.TeamId != selectedTeam1.TeamId) &&
             !recentTeams.Any(r =>
                 r.TeamId == x.TeamId)))
     .ToList();

        await SetTeamPickerItemsAsync(filteredTeams);

        TeamPickerSearchEntry.Text = "";
        NoSearchResultsLabel.IsVisible = false;
        TeamPickerOverlay.IsVisible = true;
    }

    //Team Card2

    async Task UpdateTeam2Card()
    {
        if (selectedTeam2 == null)
            return;

        PreviewTeam2NameLabel.Text =
            FormatTeamName(selectedTeam2.TeamName);

        PreviewTeam2PlayersLabel.Text =
            $"{selectedTeam2.Player1} - {selectedTeam2.Player2}";

        await RefreshLiveTeamIdentityAsync(selectedTeam2);
        PreviewTeam2Logo.Source =
            ResolveStoredImage(GetLiveEmblem(selectedTeam2));

        UpdateMatchPreview();
    }

    // Match Preview
    void UpdateMatchPreview()
    {
        if (selectedTeam1 == null ||
            selectedTeam2 == null)
            return;

        PreviewTeamsLabel.Text =
            $"{selectedTeam1.TeamName} VS {selectedTeam2.TeamName}";

        PreviewTeam1Logo.Source =
            ResolveStoredImage(GetLiveEmblem(selectedTeam1));

        PreviewTeam2Logo.Source =
            ResolveStoredImage(GetLiveEmblem(selectedTeam2));

        PreviewTeam1NameLabel.Text =
            FormatTeamName(
                selectedTeam1.TeamName);

        PreviewTeam2NameLabel.Text =
            FormatTeamName(
                selectedTeam2.TeamName);

        string team1Players =
            string.IsNullOrWhiteSpace(
                selectedTeam1.Player2)
            ? selectedTeam1.Player1
            : $"{selectedTeam1.Player1} - {selectedTeam1.Player2}";

        string team2Players =
            string.IsNullOrWhiteSpace(
                selectedTeam2.Player2)
            ? selectedTeam2.Player1
            : $"{selectedTeam2.Player1} - {selectedTeam2.Player2}";

        PreviewTeam1PlayersLabel.Text =
            team1Players;

        PreviewTeam2PlayersLabel.Text =
            team2Players;

        PreviewRulesLabel.Text =
            selectedRules;
    }



    // =========================
    // HISTORY
    // =========================

    async void OnHistoryClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new HistoryPage());
    }

    // =========================
    // HALL OF FAME
    // =========================

    async void OnHallOfFameClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new HallOfFamePage());
    }

    // =========================
    // RANKINGS
    // =========================

    async void OnRankingsClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new RankingsPage());
    }





    // =========================
    // START GAME
    // =========================
    private async void OnStartGame(object sender, TappedEventArgs e)
    {
        OnStartGame(sender, EventArgs.Empty);
    }
    async void OnStartGame(
    object sender,
    EventArgs e)
    {
        if (selectedTeam1 == null ||
     selectedTeam2 == null)
        {
            await DisplayAlert(
                "تنبيه",
                "يجب اختيار فريقين",
                "حسناً");

            return;
        }

        if (selectedTeam1.TeamId == selectedTeam2.TeamId)
        {
            await DisplayAlert(
                "تنبيه",
                "لا يمكن اختيار نفس الفريق مرتين",
                "حسناً");

            return;
        }
        // =====================================
        // SELF MATCH PREVENTION ENGINE
        // =====================================
        var team1Ids = new List<string>
{
    selectedTeam1.Player1Id,
    selectedTeam1.Player2Id
}
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .ToList();

        var team2Ids = new List<string>
{
    selectedTeam2.Player1Id,
    selectedTeam2.Player2Id
}
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .ToList();

        var duplicatePlayerId =
            team1Ids
            .Intersect(team2Ids)
            .FirstOrDefault();
        string duplicatePlayerName = "";

        if (!string.IsNullOrWhiteSpace(duplicatePlayerId))
        {
            var players =
                await PlayerProfileService
                .LoadPlayersAsync();

            duplicatePlayerName =
                players
                .FirstOrDefault(x =>
                    x.PlayerId == duplicatePlayerId)
                ?.PlayerName ?? duplicatePlayerId;
            await DisplayAlert(
               "لا يمكن بدء المباراة",
               $"اللاعب ({duplicatePlayerName}) موجود في الفريقين.\n\nيجب أن يكون كل لاعب ضمن فريق واحد فقط.",
               "حسناً");

            return;
        }
        bool team1Single =
      string.IsNullOrWhiteSpace(selectedTeam1.Player2);

        bool team2Single =
            string.IsNullOrWhiteSpace(selectedTeam2.Player2);

        if (team1Single != team2Single)
        {
            await DisplayAlert(
                "تنبيه",
                "لا يمكن لعب فريق فردي ضد فريق زوجي",
                "يجب أن يكون الفريقان من نفس النوع",
                "حسناً");

            return;
        }



        await Navigation.PushAsync(
    new GamePage(
        selectedTeam1.TeamName,
        selectedTeam2.TeamName,

        $"{selectedTeam1.Player1} + {selectedTeam1.Player2}",
        $"{selectedTeam2.Player1} + {selectedTeam2.Player2}",

        selectedTeam1.TeamId,
        selectedTeam2.TeamId,

        selectedTeam1.Player1Id,
        selectedTeam1.Player2Id,

        selectedTeam2.Player1Id,
        selectedTeam2.Player2Id,

        selectedRules));

    }
    // =========================    
    // PLAYER PROFILES
    // =========================

    async void OnOpenPlayerProfilesClicked(
    object sender,
    EventArgs e)
    {
        PrivacyProfileOverlay.IsVisible = false;

        await Navigation.PushAsync(
            new PlayerProfilesPage());
    }
    // =========================
    // TEAM NAMES
    // =========================



    async void OnStatisticsClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new StatisticsPage());
    }
    //Select team

    async void OnCreateTeamClicked(
    object sender,
    EventArgs e)

    {
        await Navigation.PushAsync(
            new CreateTeamPage());
    }

    void OnCloseTeamPicker(
    object sender,
    TappedEventArgs e)
    {
        TeamPickerOverlay.IsVisible = false;
    }


    async void OnTeamPickerSearchChanged(
object sender,
TextChangedEventArgs e)
    {
        var search =
            e.NewTextValue?.Trim() ?? "";

        List<TeamProfileModel> results;

        if (string.IsNullOrWhiteSpace(search))
        {
            results = filteredTeams;
        }
        else
        {
            results =
                filteredTeams
                .Where(x =>
                    x.TeamName.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    x.Player1.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    (!string.IsNullOrWhiteSpace(x.Player2)
                     &&
                     x.Player2.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        await SetTeamPickerItemsAsync(results);

        NoSearchResultsLabel.IsVisible =
            results.Count == 0;
    }

    async void OnTeamPicked(
    object sender,
    TappedEventArgs e)
    {
        if (sender is not Border border)
            return;

        var team = border.BindingContext switch
        {
            TeamPickerVisualItem visualItem => visualItem.Team,
            TeamProfileModel profile => profile,
            _ => null
        };

        if (team == null)
            return;

        if (isPickingTeam1)
        {
            selectedTeam1 = team;
            AddToRecentTeams(team);
            await UpdateTeam1Card();

            if (selectedTeam2 != null &&
     selectedTeam2.TeamId == selectedTeam1.TeamId)
            {
                selectedTeam2 = null;

                PreviewTeam2NameLabel.Text =
                    "اختر الفريق الثاني";

                PreviewTeam2PlayersLabel.Text =
                    "";
            }
        }
        else
        {
            selectedTeam2 = team;
            AddToRecentTeams(selectedTeam2);
            await UpdateTeam2Card();
        }

        TeamPickerOverlay.IsVisible = false;

        UpdateMatchPreview();
    }
    //Chose Rules


    void OnRulesCardTapped(
       object sender,
       TappedEventArgs e)
    {
        RulesDropdownBorder.IsVisible =
            !RulesDropdownBorder.IsVisible;
    }

    void OnLocalRuleTapped(
    object sender,
    TappedEventArgs e)
    {
        selectedRules = "محلي";

        PreviewRulesLabel.Text =
            "محلي";

        RulesDropdownBorder.IsVisible = false;

        UpdateMatchPreview();
    }

    void OnInternationalRuleTapped(
    object sender,
    TappedEventArgs e)
    {
        selectedRules = "عالمي";

        PreviewRulesLabel.Text =
            "عالمي";

        RulesDropdownBorder.IsVisible = false;

        UpdateMatchPreview();
    }

    // Game Mode Changes





    // Icons
    string GetLogoFromEmblem(string emblem)
    {
        return emblem switch
        {
            "ًں¦…" => "eagle_3d.png",
            "ًںگ؛" => "wolf_3d.png",
            "ًں¦پ" => "lion_3d.png",
            "ًںگ‰" => "dragon_3d.png",
            "ًں‘‘" => "crown_3d.png",
            _ => "shield_3d.png"
        };
    }
    // Coror Changes
    Color GetTeamColor(string hex)
    {
        try
        {
            return Color.FromArgb(hex);
        }
        catch
        {
            return Colors.Gold;
        }
    }
    // Recent Teams Management
    void AddToRecentTeams(
    TeamProfileModel team)
    {
        if (team == null)
            return;

        recentTeams.RemoveAll(
            x => x.TeamId == team.TeamId);

        recentTeams.Insert(0, team);

        if (recentTeams.Count > MaxRecentTeams)
        {
            recentTeams =
                recentTeams
                .Take(MaxRecentTeams)
                .ToList();
        }
    }

    // Main Season Card
    async void UpdateMainSeasonCard()
    {
        var teams =
            await RankingService.LoadTeamsAsync();

        SeasonManager.EnsureSeason(teams);

        await RankingService.SaveTeamsAsync(teams);

        if (teams == null || teams.Count == 0)
        {
            MainSeasonTitleLabel.Text =
                "الموسم الحالي";

            MainSeasonCountdownLabel.Text =
                "ابدأ أول مباراة لتفعيل الموسم";

            MainSeasonProgressPercentLabel.Text =
                "0%";

            MainSeasonProgressBar.Progress =
                0;

            return;
        }

        var seasonTeam =
            teams
            .FirstOrDefault(x => x.CurrentSeasonId > 0)
            ?? teams.First();

        int seasonNumber =
            SeasonManager.GetCurrentSeasonNumber(teams);

        int daysRemaining =
            SeasonManager.GetDaysRemaining(seasonTeam);

        double progress =
            SeasonManager.GetSeasonProgress(seasonTeam);

        MainSeasonTitleLabel.Text =
            $"الموسم {seasonNumber}";

        MainSeasonCountdownLabel.Text =
            $"متبقي {daysRemaining} يوم على نهاية الموسم";

        MainSeasonProgressPercentLabel.Text =
            $"{progress * 100:0}%";

        MainSeasonProgressBar.Progress =
            progress;
    }

    // Refresh Selected Teams from IDs (in case they were updated in the team picker)
    async Task RefreshSelectedTeamsFromIds()
    {
        allTeams =
            await TeamProfileService.LoadTeamsAsync();

        // =========================
        // TEAM 1
        // =========================

        if (selectedTeam1 != null &&
            !string.IsNullOrWhiteSpace(selectedTeam1.TeamId))
        {
            var freshTeam1 =
                allTeams.FirstOrDefault(x =>
                    x.TeamId == selectedTeam1.TeamId);

            if (freshTeam1 == null)
            {
                selectedTeam1 = null;

                PreviewTeam1NameLabel.Text =
                    "اختر الفريق الأول";

                PreviewTeam1PlayersLabel.Text =
                    "";

                PreviewTeam1Logo.Source =
                    "shield_3d.png";
            }
            else
            {
                selectedTeam1 = freshTeam1;

                PreviewTeam1NameLabel.Text =
                    FormatTeamName(freshTeam1.TeamName);

                PreviewTeam1PlayersLabel.Text =
                    freshTeam1.IsSinglePlayer
                    ? freshTeam1.Player1
                    : $"{freshTeam1.Player1} - {freshTeam1.Player2}";

                await RefreshLiveTeamIdentityAsync(freshTeam1);
                PreviewTeam1Logo.Source =
                    ResolveStoredImage(GetLiveEmblem(freshTeam1));
            }
        }

        // =========================
        // TEAM 2
        // =========================

        if (selectedTeam2 != null &&
            !string.IsNullOrWhiteSpace(selectedTeam2.TeamId))
        {
            var freshTeam2 =
                allTeams.FirstOrDefault(x =>
                    x.TeamId == selectedTeam2.TeamId);

            if (freshTeam2 == null)
            {
                selectedTeam2 = null;

                PreviewTeam2NameLabel.Text =
                    "اختر الفريق الثاني";

                PreviewTeam2PlayersLabel.Text =
                    "";

                PreviewTeam2Logo.Source =
                    "shield_3d.png";
            }
            else
            {
                selectedTeam2 = freshTeam2;

                PreviewTeam2NameLabel.Text =
                    FormatTeamName(freshTeam2.TeamName);

                PreviewTeam2PlayersLabel.Text =
                    freshTeam2.IsSinglePlayer
                    ? freshTeam2.Player1
                    : $"{freshTeam2.Player1} - {freshTeam2.Player2}";

                await RefreshLiveTeamIdentityAsync(freshTeam2);
                PreviewTeam2Logo.Source =
                    ResolveStoredImage(GetLiveEmblem(freshTeam2));
            }
        }

        recentTeams =
            recentTeams
            .Where(r =>
                allTeams.Any(t =>
                    t.TeamId == r.TeamId))
            .ToList();

        if (selectedTeam1 != null &&
            selectedTeam2 != null)
        {
            UpdateMatchPreview();
        }
        else
        {
            PreviewTeamsLabel.Text =
                "اختر فريقين";
        }
    }


    // =========================
    // APP DATA CHANGED EVENT HANDLER
    // =========================
    async void OnAppDataChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                allTeams =
                    await TeamProfileService.LoadTeamsAsync();

                await RefreshSelectedTeamsFromIds();

                await SetTeamPickerItemsAsync(allTeams);

                UpdateMatchPreview();
                UpdateMainSeasonCard();

                await RefreshProfileStatus();
            });
    }
    // =========================
    // PLAYER PROFILE CHANGED EVENT HANDLER
    // =========================
    async void OnMainProfileChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                await RefreshProfileStatus();
                await RefreshHeaderPlayerAsync();
                UpdateMainSeasonCard();
            });
    }

    async void OnStoreEconomyChanged(string playerId)
    {
        var devicePlayerId =
            await PlayerStoreIdentityService.GetDeviceIdentityPlayerIdAsync();

        if (!string.Equals(
                playerId,
                devicePlayerId,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await RefreshHeaderPlayerAsync();
    }

    async void OnCurrentUserChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(
            RefreshHeaderPlayerAsync);
    }

    async void OnTeamAssetsChanged(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return;

        var selectedTeam = new[] { selectedTeam1, selectedTeam2 }
            .FirstOrDefault(team =>
                string.Equals(
                    team?.TeamId,
                    teamId,
                    StringComparison.OrdinalIgnoreCase));

        var pickerContainsTeam =
            TeamPickerOverlay.IsVisible &&
            filteredTeams.Any(team =>
                string.Equals(
                    team.TeamId,
                    teamId,
                    StringComparison.OrdinalIgnoreCase));

        if (selectedTeam == null && !pickerContainsTeam)
            return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            liveTeamIdentities.Remove(teamId);
            if (selectedTeam != null)
                await RefreshLiveTeamIdentityAsync(selectedTeam);
            if (pickerContainsTeam)
                await SetTeamPickerItemsAsync(filteredTeams);
            UpdateMatchPreview();
        });
    }

    async Task RefreshLiveTeamVisualsAsync()
    {
        var selectedTeams = new[] { selectedTeam1, selectedTeam2 }
            .Where(team => team != null)
            .Cast<TeamProfileModel>();

        foreach (var team in selectedTeams)
            await RefreshLiveTeamIdentityAsync(team);
    }

    async Task RefreshLiveTeamIdentityAsync(TeamProfileModel team)
    {
        if (string.IsNullOrWhiteSpace(team.TeamId))
            return;

        try
        {
            liveTeamIdentities[team.TeamId] =
                await TeamIdentityResolver.ResolveAsync(team.TeamId);
        }
        catch
        {
            liveTeamIdentities[team.TeamId] = LegacyIdentity(team);
        }
    }

    async Task SetTeamPickerItemsAsync(IEnumerable<TeamProfileModel> teams)
    {
        var teamList = teams.ToList();
        foreach (var team in teamList)
            await RefreshLiveTeamIdentityAsync(team);

        TeamPickerCollection.ItemsSource = teamList
            .Select(team => new TeamPickerVisualItem
            {
                Team = team,
                Emblem = ResolveStoredImage(GetLiveEmblem(team))
            })
            .ToList();
    }

    string GetLiveEmblem(TeamProfileModel team) =>
        liveTeamIdentities.TryGetValue(team.TeamId, out var identity) &&
        !string.IsNullOrWhiteSpace(identity.EmblemImagePath)
            ? identity.EmblemImagePath
            : LegacyIdentity(team).EmblemImagePath;

    static ImageSource ResolveStoredImage(
        string? imagePath,
        string fallback = "shield_3d.png") =>
        InventoryDisplayResolver.ResolveImageSource(
            imagePath,
            fallback);

    string GetLiveTeamColor(TeamProfileModel team) =>
        liveTeamIdentities.TryGetValue(team.TeamId, out var identity) &&
        !string.IsNullOrWhiteSpace(identity.TeamColorHex)
            ? identity.TeamColorHex
            : LegacyIdentity(team).TeamColorHex;

    static TeamIdentityModel LegacyIdentity(TeamProfileModel team) =>
        new()
        {
            TeamId = team.TeamId,
            TeamName = team.TeamName,
            EmblemImagePath = string.IsNullOrWhiteSpace(team.Emblem)
                ? "shield_3d.png"
                : team.Emblem,
            EmblemBackgroundSource = "Transparent",
            TeamColorHex = string.IsNullOrWhiteSpace(team.ColorHex)
                ? "#FFD700"
                : team.ColorHex,
            ResolvedAt = DateTime.UtcNow
        };

    // =========================
    // SECRET HONORS ACCESS - LONG PRESS LOGO
    // =========================
    async void OnLogoPressed(
     object sender,
     EventArgs e)
    {
        logoPressed = true;

        logoPressToken =
            new CancellationTokenSource();

        try
        {
            await Task.Delay(
                5000,
                logoPressToken.Token);

            if (logoPressed)
            {
                await Navigation.PushAsync(
                    new DeveloperLoginPage());
            }
        }
        catch
        {
            // طھظ… ط¥ظ„ط؛ط§ط، ط§ظ„ط¶ط؛ط·
        }
    }

    void OnLogoReleased(
        object sender,
        EventArgs e)
    {
        logoPressed = false;

        logoPressToken?.Cancel();
    }
    // =========================
    // SETTINGS ICON
    // =========================
    void OnSettingsImageTapped(
    object sender,
    TappedEventArgs e)
    {
        OnSettingsClicked(
            sender,
            EventArgs.Empty);
    }

    async void OnStoreTapped(
        object? sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new GalleryPage());
    }

    async Task RefreshHeaderPlayerAsync()
    {
        int refreshVersion =
            Interlocked.Increment(ref headerRefreshVersion);

        try
        {
            var currentUser =
                await ApplicationUserService.GetCurrentUserAsync();

            if (refreshVersion != headerRefreshVersion)
                return;

            string playerId = currentUser.PlayerId;

            if (string.IsNullOrWhiteSpace(playerId))
            {
                HeaderPlayerAvatar.Source = DefaultHeaderAvatar;
                HeaderAvatarFrameOverlay.IsVisible = false;
                HeaderAvatarEffectOverlay.IsVisible = false;
                HeaderProfileBackgroundImage.IsVisible = false;

                HeaderPlayerNameLabel.Text =
                    string.IsNullOrWhiteSpace(currentUser.DisplayName)
                        ? "اللاعب"
                        : currentUser.DisplayName;

                MemberLevelLabel.Text =
                    ResolveHeaderRoleLabel(currentUser.Role);

                return;
            }

            var profile =
                await PlayerProfileService.GetPlayerByIdAsync(playerId);

            var visualIdentity =
                await PlayerVisualIdentityResolver.ResolveAsync(playerId);

            if (refreshVersion != headerRefreshVersion)
                return;

            HeaderProfileBackgroundImage.Source = null;
            HeaderProfileBackgroundImage.IsVisible = false;

            ApplyHeaderOverlay(
                HeaderAvatarFrameOverlay,
                visualIdentity.Frame?.PreviewImage);

            PlayerEffectEngine.Apply(
                HeaderAvatarEffectOverlay,
                visualIdentity.Effect,
                1.08);

            HeaderPlayerAvatar.Source =
                profile == null
                    ? DefaultHeaderAvatar
                    : PlayerProfileService.GetPlayerImageSource(profile);

            HeaderPlayerNameLabel.Text =
                string.IsNullOrWhiteSpace(profile?.PlayerName)
                    ? string.IsNullOrWhiteSpace(currentUser.DisplayName)
                        ? "اللاعب"
                        : currentUser.DisplayName
                    : profile.PlayerName;

            MemberLevelLabel.Text =
                visualIdentity.Title != null
                    ? $"{ResolveHeaderRoleLabel(currentUser.Role)} • {visualIdentity.Title.DisplayName}"
                    : ResolveHeaderRoleLabel(currentUser.Role);
        }
        catch
        {
            HeaderPlayerAvatar.Source = DefaultHeaderAvatar;
            HeaderAvatarFrameOverlay.IsVisible = false;
            HeaderAvatarEffectOverlay.IsVisible = false;
            HeaderProfileBackgroundImage.IsVisible = false;
            HeaderPlayerNameLabel.Text = "اللاعب";
            MemberLevelLabel.Text = "Guest";
        }
    }

    static void ApplyHeaderOverlay(Image image, string? imagePath)
    {
        image.Source = ToHeaderImageSource(imagePath);
        image.IsVisible = !string.IsNullOrWhiteSpace(imagePath);
    }

    static ImageSource? ToHeaderImageSource(string? imagePath) =>
        InventoryDisplayResolver.ResolveOptionalImageSource(imagePath);

    static string ResolveHeaderRoleLabel(ApplicationUserRole role) =>
        role switch
        {
            ApplicationUserRole.Developer => "Developer",
            ApplicationUserRole.Founder => "Founder",
            ApplicationUserRole.Honor => "Honor",
            ApplicationUserRole.Member => "Member",
            _ => "Guest"
        };

    static string ResolveProfileRoleLabel(PlayerProfileModel? profile) =>
        profile?.ProfileStatus switch
        {
            PlayerProfileStatus.Developer => "Developer",
            PlayerProfileStatus.Founder => "Founder",
            PlayerProfileStatus.Honor => "Honor",
            PlayerProfileStatus.Normal => "Member",
            _ => "Guest"
        };


    // =========================
    // SETTINGS SHEET
    // =========================
    async void OnSettingsClicked(
    object sender,
    EventArgs e)
    {
        SettingsOverlay.IsVisible = true;

        await SettingsSheet.TranslateToAsync(
            0,
            0,
            250,
            Easing.CubicOut);
    }

    async void OnCloseSettings(
        object sender,
        TappedEventArgs e)
    {
        await SettingsSheet.TranslateToAsync(
            0,
            700,
            200,
            Easing.CubicIn);

        SettingsOverlay.IsVisible = false;
    }

    // Adjust settings sheet height on orientation change
    protected override void OnSizeAllocated(
       double width,
       double height)
    {
        base.OnSizeAllocated(width, height);

        if (height <= 0)
            return;

        if (SettingsSheet != null)
        {
            SettingsSheet.MaximumHeightRequest =
                height * 0.70;
        }

        if (DataManagerSheet != null)
        {
            DataManagerSheet.MaximumHeightRequest =
                height * 0.65;
        }

        if (VersionInfoSheet != null)
        {
            VersionInfoSheet.MaximumHeightRequest =
                height * 0.65;
        }
        if (UpdateLogSheet != null)
        {
            UpdateLogSheet.MaximumHeightRequest =
                height * 0.65;
        }
        if (InfoSheet != null)
        {
            InfoSheet.MaximumHeightRequest =
                height * 0.65;
        }
        if (UserGuideSheet != null)
        {
            UserGuideSheet.MaximumHeightRequest =
                height * 0.70;
        }
        if (PrivacyProfileSheet != null)
        {
            PrivacyProfileSheet.MaximumHeightRequest =
                height * 0.70;
        }
        if (SecurityLogSheet != null)
        {
            SecurityLogSheet.MaximumHeightRequest =
                height * 0.65;
        }
        if (DeveloperLockSheet != null)
        {
            DeveloperLockSheet.MaximumHeightRequest =
                height * 0.65;
        }
        if (HonorActivationSheet != null)
        {
            HonorActivationSheet.MaximumHeightRequest =
                height * 0.60;
        }
    }



    // Settings Sections Toggle
    void ToggleSection(
    VerticalStackLayout content,
    Label arrow,
    ref bool state)
    {
        state = !state;

        content.IsVisible = state;
        arrow.Text = state ? "â–²" : "â–¼";
    }

    void OnDataSettingsTapped(object sender, TappedEventArgs e)
    {
        ToggleSection(
            DataSettingsContent,
            DataArrow,
            ref isDataExpanded);
    }

    void OnSystemSettingsTapped(object sender, TappedEventArgs e)
    {
        ToggleSection(
            SystemSettingsContent,
            SystemArrow,
            ref isSystemExpanded);
    }

    void OnHonorsSettingsTapped(object sender, TappedEventArgs e)
    {
        ToggleSection(
            HonorsSettingsContent,
            HonorsArrow,
            ref isHonorsExpanded);
    }

    void OnAboutSettingsTapped(object sender, TappedEventArgs e)
    {
        ToggleSection(
            AboutSettingsContent,
            AboutArrow,
            ref isAboutExpanded);
    }

    void OnSupportSettingsTapped(object sender, TappedEventArgs e)
    {
        ToggleSection(
            SupportSettingsContent,
            SupportArrow,
            ref isSupportExpanded);
    }


    // Backup Data
    async void OnBackupTapped(
    object sender,
    TappedEventArgs e)
    {
        try
        {
            string backupPath =
                await BackupService.CreateBackupAsync();
            await DisplayAlert(
   "تم",
   "تم إنشاء النسخة الاحتياطية بنجاح. احفظ الملف في مكان آمن.",
   "حسناً");

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "نسخة احتياطية - Domino Majlis PRO",
                    File = new ShareFile(backupPath)
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
               "خطأ",
               $"فشل إنشاء النسخة الاحتياطية:\n{ex.Message}",
               "حسناً");
        }
    }


    // Restore Data

    async void OnRestoreTapped(
    object sender,
    TappedEventArgs e)
    {
        bool confirm =
           await DisplayAlert(
                "استعادة البيانات",
                "سيتم استبدال البيانات الحالية بالبيانات الموجودة داخل النسخة الاحتياطية.\n\nسيتم إنشاء نسخة طارئة قبل الاستعادة.\n\nهل تريد المتابعة؟",
                "استعادة",
                "إلغاء");

        if (!confirm)
            return;

        try
        {
            var file =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle =
                            "اختر ملف النسخة الاحتياطية"
                    });

            if (file == null)
                return;

            string emergencyBackup =
                await BackupService
                    .CreateEmergencyBackupAsync();

            await BackupService
                .RestoreBackupAsync(file);

            await DisplayAlert(
                "تمت الاستعادة",
                $"تمت استعادة البيانات بنجاح.\n\nتم إنشاء نسخة طارئة قبل الاستعادة:\n{System.IO.Path.GetFileName(emergencyBackup)}",
                "حسناً");
            AppEvents.RaiseDataChanged();
            allTeams =
                await TeamProfileService.LoadTeamsAsync();

            await RefreshSelectedTeamsFromIds();

            UpdateMatchPreview();
            UpdateMainSeasonCard();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشلت عملية الاستعادة:\n{ex.Message}",
                "حسناً");
        }
    }



    // =========================
    // REBUILD RANKINGS
    // =========================
    async void OnRebuildRankingsTapped(
        object sender,
        TappedEventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "إعادة بناء التصنيفات",
                "سيتم إعادة حساب التصنيفات من سجل المباريات الحالي.\n\nلن يتم حذف الفرق أو المباريات.\n\nهل تريد المتابعة؟",
                "إعادة البناء",
                "إلغاء");

        if (!confirm)
            return;

        try
        {
            await RankingService.RebuildAllRankingsAsync();

            AppEvents.RaiseDataChanged();

            await DisplayAlert(
                "تم",
                "تمت إعادة بناء التصنيفات بنجاح",
                "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشلت إعادة بناء التصنيفات:\n{ex.Message}",
                "حسناً");
        }
    }

    // =========================
    // DELETE RANKINGS
    //  =========================
    async void OnDeleteRankingsTapped(
    object sender,
    TappedEventArgs e)
    {
        bool confirm =
           await DisplayAlert(
                "حذف التصنيفات",
                "سيتم حذف التصنيفات فقط.\n\nلن يتم حذف الفرق أو سجل المباريات.\n\nيمكنك إعادة بناء التصنيفات لاحقاً.",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        try
        {
            await RankingService.SaveTeamsAsync(
                new List<TeamProfileModel>());

            AppEvents.RaiseDataChanged();

            await DisplayAlert(
                "تم",
                "تم حذف التصنيفات بنجاح",
                "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشل حذف التصنيفات:\n{ex.Message}",
                "حسناً");
        }
    }

    async void OnDeleteAllMatchesTapped(
    object sender,
    TappedEventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "حذف جميع المباريات",
                "سيتم حذف سجل جميع المباريات نهائياً.\n\nسيتم إنشاء نسخة طارئة قبل الحذف.\n\nبعد الحذف سيتم إعادة بناء التصنيفات.",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        try
        {
            string emergencyBackup =
                await BackupService
                    .CreateEmergencyBackupAsync();

            await GameService
                .DeleteAllMatches();

            await RankingService
                .RebuildAllRankingsAsync();

            AppEvents.RaiseDataChanged();

            await DisplayAlert(
                "تم",
                $"تم حذف جميع المباريات بنجاح.\n\nتم إنشاء نسخة طارئة:\n{System.IO.Path.GetFileName(emergencyBackup)}",
                "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشل حذف جميع المباريات:\n{ex.Message}",
                "حسناً");
        }
    }
    // =========================
    // CLEAN CORRUPTED DATA
    // =========================
    async void OnCleanCorruptedDataTapped(
    object sender,
    TappedEventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "اكتملت العملية",
                "سيتم فحص ملفات البيانات وإصلاح الملفات الفارغة أو التالفة.\n\nلن يتم حذف الفرق أو المباريات السليمة.",
                "اكتمل",
                "إلغاء");

        if (!confirm)
            return;

        try
        {
            string emergencyBackup =
                await BackupService
                    .CreateEmergencyBackupAsync();

            string result =
                await DataMaintenanceService
                    .CleanCorruptedDataAsync();

            AppEvents.RaiseDataChanged();

            await DisplayAlert(
                  "تم",
                  $"{result}\n\nتم إنشاء نسخة طارئة:\n{System.IO.Path.GetFileName(emergencyBackup)}",
                  "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشل تنظيف البيانات:\n{ex.Message}",
                "حسناً");
        }
    }


    // =========================
    // OPEN DATA MANAGER
    // =========================
    void OnOpenDataManager(
    object sender,
    TappedEventArgs e)
    {
        DataManagerOverlay.IsVisible = true;
    }

    void OnCloseDataManager(
        object sender,
        TappedEventArgs e)
    {
        DataManagerOverlay.IsVisible = false;
    }

    // =========================
    // SHOW DATA STATUS
    // =========================
    async void OnDataStatusTapped(
    object sender,
    TappedEventArgs e)
    {
        try
        {
            var status =
                await DataStatusService.GetStatusAsync();

            await DisplayAlert(
                "حالة البيانات",
                $"الفرق: {status.TeamsCount}\n" +
                $"اللاعبون: {status.PlayersCount}\n" +
                $"المباريات: {status.MatchesCount}\n" +
                $"التصنيفات: {status.RankingsCount}\n" +
                $"قاعة المشاهير: {status.HallOfFameCount}\n\n" +
                $"حجم البيانات: {status.DataSizeText}",
                "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشل عرض حالة البيانات:\n{ex.Message}",
                "حسناً");
        }
    }

    async void OnDiagnosticsTapped(
    object sender,
    TappedEventArgs e)
    {
        try
        {
            var result =
                await DiagnosticService.RunDiagnosticsAsync();

            string title =
                result.HasProblems
                ? "النسخة الحالية - لا يوجد تحديث"
                : "التشخيص - سليم";

            await DisplayAlert(
                title,
                string.Join("\n", result.Messages),
                "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشل تشغيل التشخيص:\n{ex.Message}",
                "حسناً");
        }
    }
    // ====================
    // VERSION INFO
    // ===================

    void OnVersionInfoTapped(
    object sender,
    TappedEventArgs e)
    {


        var info =
            AppVersionService.GetVersionInfo();

        VersionAppNameLabel.Text =
            info.AppName;

        VersionNumberLabel.Text =
            $"الإصدار الحالي: {info.Version}";

        VersionBuildLabel.Text =
            $"رقم البناء: {info.Build}";

        VersionReleaseTypeLabel.Text =
            $"نوع النسخة: {info.ReleaseType}";

        VersionDeveloperLabel.Text =
            $"المطور: {info.Developer}";

        VersionUpdateStatusLabel.Text =
            info.UpdateStatus;

        LatestUpdatesContainer.Children.Clear();

        foreach (string update in info.LatestUpdates)
        {
            LatestUpdatesContainer.Children.Add(
                new Label
                {

                    Text = $"✓ {update}",
                    TextColor = Colors.White,
                    FontSize = 14,
                    HorizontalTextAlignment = TextAlignment.End
                });
        }

        VersionInfoOverlay.IsVisible = true;
    }

    void OnCloseVersionInfo(
        object sender,
        TappedEventArgs e)
    {
        VersionInfoOverlay.IsVisible = false;

    }

    async void OnCopyVersionInfoTapped(
        object sender,
        TappedEventArgs e)
    {
        var info =
            AppVersionService.GetVersionInfo();

        string text =
            AppVersionService.BuildCopyText(info);

        await Clipboard.Default.SetTextAsync(text);

        await DisplayAlert(
            "تم",
            "تم نسخ معلومات الإصدار",
            "حسناً");
    }

    // =========================
    // UPDATE LOG
    // =========================
    void OnUpdateLogTapped(
    object sender,
    TappedEventArgs e)
    {
        UpdateLogContainer.Children.Clear();

        UpdateLogContainer.Children.Add(
            new Label
            {
                Text = "سجل التحديثات",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Gold,
                HorizontalTextAlignment = TextAlignment.Center
            });

        var logs =
            UpdateLogService.GetUpdateLogs();

        foreach (var log in logs)
        {
            Border card =
                new Border
                {
                    BackgroundColor = Color.FromArgb("#080808"),
                    Stroke = Color.FromArgb("#303030"),
                    StrokeThickness = 1,
                    Padding = 14,
                    StrokeShape =
                        new RoundRectangle
                        {
                            CornerRadius = 18
                        }
                };

            VerticalStackLayout layout =
                new VerticalStackLayout
                {
                    Spacing = 8
                };

            layout.Children.Add(
                new Label
                {
                    Text = $"الإصدار {log.Version}",
                    TextColor = Colors.Gold,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.End
                });

            layout.Children.Add(
                new Label
                {
                    Text = log.ReleaseDate,
                    TextColor = Color.FromArgb("#AAAAAA"),
                    FontSize = 13,
                    HorizontalTextAlignment = TextAlignment.End
                });

            foreach (string update in log.Updates)
            {
                layout.Children.Add(
                    new Label
                    {
                        Text = $"✓ {update}",
                        TextColor = Colors.White,
                        FontSize = 14,
                        HorizontalTextAlignment = TextAlignment.End
                    });
            }

            card.Content = layout;

            UpdateLogContainer.Children.Add(card);
        }

        UpdateLogOverlay.IsVisible = true;
    }

    void OnCloseUpdateLog(
        object sender,
        TappedEventArgs e)
    {
        UpdateLogOverlay.IsVisible = false;
    }


    // =========================
    // INFO SHEET
    // =========================
    void ShowInfoSheet(
    InfoSectionModel info)
    {
        InfoTitleLabel.Text =
            info.Title;

        InfoContentContainer.Children.Clear();

        foreach (string item in info.Items)
        {
            InfoContentContainer.Children.Add(
                new Label
                {
                    Text = $"✓ {item}",
                    TextColor = Colors.White,
                    FontSize = 14,
                    HorizontalTextAlignment = TextAlignment.End
                });
        }

        InfoOverlay.IsVisible = true;
    }

    void OnBadgeInfoTapped(
        object sender,
        TappedEventArgs e)
    {
        ShowInfoSheet(
            AchievementsInfoService.GetBadgesInfo());
    }

    void OnAchievementRulesTapped(
        object sender,
        TappedEventArgs e)
    {
        ShowInfoSheet(
            AchievementsInfoService.GetAchievementRules());
    }

    void OnHallOfFameRulesTapped(
        object sender,
        TappedEventArgs e)
    {
        ShowInfoSheet(
            AchievementsInfoService.GetHallOfFameRules());
    }

    void OnCloseInfoSheet(
        object sender,
        TappedEventArgs e)
    {
        InfoOverlay.IsVisible = false;
    }

    // =========================
    // SUPPORT REPORT
    // =========================
    async void OnSendSupportReportTapped(
       object sender,
       TappedEventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "إرسال تقرير تشخيص",
                "سيتم إنشاء تقرير يحتوي على معلومات الإصدار، حالة البيانات، ونتيجة التشخيص.\n\nلا يتم إرسال كلمات مرور أو مفاتيح خاصة.",
                "إنشاء التقرير",
                "إلغاء");

        if (!confirm)
            return;

        try
        {
            string reportPath =
                await SupportReportService
                    .CreateSupportReportAsync();

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "تقرير دعم Domino Majlis PRO",
                    File = new ShareFile(reportPath)
                });
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "خطأ",
                $"فشل إنشاء تقرير الدعم:\n{ex.Message}",
                "حسناً");
        }
    }    // =========================
    // OPEN INSTAGRAM
    // =========================

    async void OnInstagramTapped(
    object sender,
    TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(
                new Uri(
                    "https://www.instagram.com/dmp20.26?igsh=M2N4ajRqb2d6aDFn"));
        }
        catch
        {
            await DisplayAlert(
                "خطأ",
                "تعذر فتح إنستغرام",
                "حسناً");
        }
    }
    // =========================
    // OPEN EMAIL
    // =========================

    async void OnEmailTapped(
    object sender,
    TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(
                new Uri(
                    "mailto:domino.majlis@gmail.com\r\n"));
        }
        catch
        {
            await DisplayAlert(
                "خطأ",
                "تعذر فتح البريد الإلكتروني",
                "حسناً");
        }
    }
    // =========================
    // USER GUIDE
    // =========================
    void OnUserGuideTapped(
        object sender,
        TappedEventArgs e)
    {
        UserGuideContainer.Children.Clear();

        var sections =
            UserGuideService.GetUserGuideSections();

        foreach (var section in sections)
        {
            UserGuideContainer.Children.Add(
                new Label
                {
                    Text = section.Title,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.Gold,
                    HorizontalTextAlignment = TextAlignment.End
                });

            foreach (var item in section.Items)
            {
                bool isOpen = false;

                Label titleLabel =
     new Label
     {
         Text = $"{item.Title} ▼",

         FontSize = 16,

         FontAttributes =
             FontAttributes.Bold,

         TextColor = Colors.Gold,

         FlowDirection =
             FlowDirection.RightToLeft,

         HorizontalTextAlignment =
             TextAlignment.End
     };

                Label descriptionLabel =
     new Label
     {
         Text = item.Description,

         FontSize = 14,

         TextColor = Colors.White,

         FlowDirection =
             FlowDirection.RightToLeft,

         HorizontalTextAlignment =
             TextAlignment.End,

         LineBreakMode =
             LineBreakMode.WordWrap,

         IsVisible = false
     };
                Border card =
                    new Border
                    {
                        Stroke =
                            Color.FromArgb("#444444"),

                        StrokeThickness = 1,

                        BackgroundColor =
                            Color.FromArgb("#101010"),

                        Padding = 12,

                        Margin =
                            new Thickness(0, 0, 0, 8),

                        StrokeShape =
                            new RoundRectangle
                            {
                                CornerRadius = 16
                            },

                        Content =
                            new VerticalStackLayout
                            {
                                Spacing = 8,
                                Children =
                                {
                    titleLabel,
                    descriptionLabel
                                }
                            }
                    };
                TapGestureRecognizer tap =
                    new TapGestureRecognizer();

                tap.Tapped += (s, args) =>
                {
                    isOpen = !isOpen;

                    descriptionLabel.IsVisible =
                        isOpen;

                    titleLabel.Text =
                        isOpen
                        ? $"{item.Title} ▲"
                        : $"{item.Title} ▼";
                };

                card.GestureRecognizers.Add(tap);

                UserGuideContainer.Children.Add(card);
            }
        }

        UserGuideOverlay.IsVisible = true;
    }


    // =========================
    // CLOSE USER GUIDE
    // =========================
    void OnCloseUserGuide(
    object sender,
    TappedEventArgs e)
    {
        UserGuideOverlay.IsVisible = false;
    }

    async void OnProfileTapped(
    object sender,
    TappedEventArgs e)
    {
        var currentUser =
            await ApplicationUserService.EnsureCurrentSessionAsync();

        if (currentUser.Role == ApplicationUserRole.Ghost)
        {
            await Navigation.PushAsync(new PlayerProfilesPage());
            return;
        }

        var profile =
            await ApplicationUserService
                .EnsureCurrentUserPlayerProfileAsync();

        if (profile != null &&
            !string.IsNullOrWhiteSpace(profile.PlayerId))
        {
            await Navigation.PushAsync(
                new PlayerDetailsPage(profile.PlayerId));
            return;
        }

        await Navigation.PushAsync(new PlayerProfilesPage());
    }

    async Task ShowIdentityLoginRegisterFlowAsync()
    {
        if (identityChoiceShowing)
            return;

        identityChoiceShowing = true;

        try
        {
            string? choice = await DisplayActionSheetAsync(
                "الهوية المحلية",
                "الاستمرار كضيف",
                null,
                "تسجيل الدخول",
                "إنشاء حساب");

            switch (choice)
            {
                case "تسجيل الدخول":
                    await ShowLocalUserSwitcherAsync();
                    break;

                case "إنشاء حساب":
                    await ApplicationUserService.EnsureGhostUserAsync();
                    await UpgradeCurrentGhostAsync();
                    break;

                default:
                    await ApplicationUserService.EnsureGhostUserAsync();
                    break;
            }
        }
        finally
        {
            identityChoiceShowing = false;
        }
    }

    async Task UpgradeCurrentGhostAsync()
    {
        string? playerName = await DisplayPromptAsync(
             "إنشاء هوية لاعب",
            "أدخل اسم اللاعب الذي سيُربط بهذه الهوية المحلية:",
            "إنشاء",
            "إلغاء",
            maxLength: 40);

        if (string.IsNullOrWhiteSpace(playerName))
            return;

        try
        {
            await ApplicationUserService
                .UpgradeGhostToMemberAsync(playerName);

            await DisplayAlertAsync(
                 "تم",
                "تم إنشاء هوية اللاعب وربطها بالجلسة الحالية.",
                "حسناً");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "تعذر إنشاء الهوية",
                ex.Message,
                "حسناً");
        }
    }

    async Task ShowLocalUserSwitcherAsync()
    {
        var currentUser =
            await ApplicationUserService.EnsureCurrentSessionAsync();
        var users =
            await ApplicationUserService.GetAllUsersAsync();

        var choices = users
            .Where(user =>
                !string.Equals(
                    user.ApplicationUserId,
                    currentUser.ApplicationUserId,
                    StringComparison.OrdinalIgnoreCase))
            .Select(user => new
            {
                User = user,
                Label =
                    $"{user.DisplayName} • {ResolveHeaderRoleLabel(user.Role)} • {ShortUserId(user.ApplicationUserId)}"
            })
            .ToList();

        if (choices.Count == 0)
        {
            await DisplayAlertAsync(
                "تبديل الحساب",
                "لا توجد هويات محلية أخرى على هذا الجهاز.",
                "حسناً");
            return;
        }

        string? selected = await DisplayActionSheetAsync(
            "اختر الهوية المحلية",
            "إلغاء",
            null,
            choices.Select(item => item.Label).ToArray());

        var selection = choices.FirstOrDefault(item =>
            string.Equals(
                item.Label,
                selected,
                StringComparison.Ordinal));

        if (selection == null)
            return;

        await ApplicationUserService.SwitchUserAsync(
            selection.User.ApplicationUserId);
    }

    async Task LogoutCurrentUserAsync()
    {
        bool confirm = await DisplayAlertAsync(
            "تسجيل الخروج",
            "سيتم إنهاء الجلسة المحلية فقط. لن تُحذف الهوية أو بيانات اللاعب.",
            "تسجيل الخروج",
            "إلغاء");

        if (!confirm)
            return;

        await ApplicationUserService.LogoutAsync();
        await ShowIdentityLoginRegisterFlowAsync();
    }

    static string ShortUserId(string applicationUserId)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId))
            return "—";

        return applicationUserId.Length <= 10
            ? applicationUserId
            : applicationUserId[..10];
    }

    void OnClosePrivacyProfile(
        object sender,
        TappedEventArgs e)
    {
        PrivacyProfileOverlay.IsVisible = false;
    }

    async void OnSavePrivacyProfileClicked(
        object sender,
        EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
               "الموافقة على حفظ البيانات",
                "سيتم حفظ معلومات اختيارية فقط لتحسين تجربة التطبيق.\n\nلن يتم حفظ عنوان السكن أو رقم الهاتف أو الموقع الجغرافي.\n\nهل توافق؟",
                "أوافق",
                "إلغاء");

        if (!confirm)
            return;

        var profile =
            new UserPrivacyProfileModel
            {
                HasAcceptedPrivacyProfile = true,
                AgeGroup = AgeGroupPicker.SelectedItem?.ToString() ?? "",
                Gender = GenderPicker.SelectedItem?.ToString() ?? "",
                Governorate = GovernorateEntry.Text?.Trim() ?? ""
            };

        await UserPrivacyProfileService.SaveAsync(profile);
        AppEvents.RaiseDataChanged();

        PrivacyProfileOverlay.IsVisible = false;
        await RefreshProfileStatus();
        await DisplayAlert(
           "تم",
            "تم حفظ الملف الشخصي الاختياري بنجاح",
            "حسناً");

    }

    async void OnDeletePrivacyProfileClicked(
        object sender,
        EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "حذف البيانات",
                "هل تريد حذف بيانات الملف الشخصي الاختياري؟",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        await UserPrivacyProfileService.DeleteAsync();
        AppEvents.RaiseDataChanged();
        AgeGroupPicker.SelectedItem = null;
        GenderPicker.SelectedItem = null;
        GovernorateEntry.Text = "";

        PrivacyProfileOverlay.IsVisible = false;
        await RefreshProfileStatus();
        await DisplayAlert(
            "تم",
            "تم حذف بيانات الملف الشخصي",
            "حسناً");

    }

    // =========================
    // REFRESH PROFILE STATUS
    // =========================
    async Task RefreshProfileStatus()
    {
        var profile =
            await UserPrivacyProfileService.LoadAsync();

        bool completed =
            profile.HasAcceptedPrivacyProfile &&
            !string.IsNullOrWhiteSpace(profile.AgeGroup) &&
            !string.IsNullOrWhiteSpace(profile.Gender) &&
            !string.IsNullOrWhiteSpace(profile.Governorate);

        if (completed)
        {
            ProfileStatusBadge.BackgroundColor =
                Colors.Gold;
        }
        else
        {
            ProfileStatusBadge.BackgroundColor =
                Colors.Red;
        }
    }

    // =========================
    // SECURITY LOG
    // =========================
    async void OnSecurityLogTapped(
    object sender,
    TappedEventArgs e)
    {
        SecurityLogContainer.Children.Clear();

        var logs =
            await SecurityLogService.LoadAsync();

        int permanentCount =
    logs.Count(x => x.IsPermanent);

        int temporaryCount =
            logs.Count(x => !x.IsPermanent);

        SecurityLogCounterLabel.Text =
            $"الدائمة: {permanentCount} | المؤقتة: {temporaryCount}";

        var role =
            await HonorIdentityService.GetCurrentRoleAsync();

        ClearTemporaryLogsButton.IsVisible =
            role == HonorRoleType.Developer;

        if (logs.Count == 0)
        {
            SecurityLogContainer.Children.Add(
                new Label
                {
                    Text = "لا توجد أحداث أمنية مسجلة",
                    TextColor = Colors.White,
                    HorizontalTextAlignment =
                        TextAlignment.Center
                });
        }
        else
        {
            foreach (var log in logs)
            {
                string severityIcon =
                    log.Severity switch
                    {
                        "High" => "🔴",
                        "Medium" => "🟡",
                        _ => "🟢"
                    };

                string severityText =
                    log.Severity switch
                    {
                        "High" => "حساس",
                        "Medium" => "تحذير",
                        _ => "معلومات"
                    };

                SecurityLogContainer.Children.Add(
                    new Border
                    {
                        Stroke = Color.FromArgb("#333333"),
                        BackgroundColor = Color.FromArgb("#080808"),
                        Padding = 12,
                        StrokeShape =
                            new RoundRectangle
                            {
                                CornerRadius = 16
                            },

                        Content =
                            new VerticalStackLayout
                            {
                                Spacing = 6,

                                Children =
                                {
                        new Label
                        {
                            Text =
                                $"{severityIcon} {log.Action}",

                            TextColor =
                                Colors.Gold,

                            FontSize = 15,

                            FontAttributes =
                                FontAttributes.Bold,

                            HorizontalTextAlignment =
                                TextAlignment.End
                        },

                        new Label
                        {
                            Text =
                                $"نوع الحدث: {severityText}",

                            TextColor =
                                Color.FromArgb("#CCCCCC"),

                            FontSize = 13,

                            HorizontalTextAlignment =
                                TextAlignment.End
                        },

                        new Label
                        {
                            Text =
                                $"التاريخ: {log.Date:yyyy/MM/dd HH:mm}",

                            TextColor =
                                Color.FromArgb("#999999"),

                            FontSize = 12,

                            HorizontalTextAlignment =
                                TextAlignment.End
                        },

                        new Label
                        {
                            Text =
                                log.Details,

                            TextColor =
                                Color.FromArgb("#777777"),

                            FontSize = 12,

                            HorizontalTextAlignment =
                                TextAlignment.End,

                            IsVisible =
                                !string.IsNullOrWhiteSpace(
                                    log.Details)
                        }
                                }
                            }
                    });
            }
        }

        SecurityLogOverlay.IsVisible = true;
    }


    // =========================
    // CLOSE SECURITY LOG
    // =========================
    void OnCloseSecurityLog(
        object sender,
        TappedEventArgs e)
    {
        SecurityLogOverlay.IsVisible = false;
    }


    // =========================
    // DEVELOPER LOCK
    // =========================
    async void OnDeveloperLockTapped(
    object sender,
    TappedEventArgs e)
    {
        var lockData =
            await DeveloperLockService.LoadAsync();

        DeveloperLockStatusLabel.Text =
            lockData.IsEnabled
            ? "الحالة: مفعّل"
            : "الحالة: غير مفعّل";

        DeveloperLockLastChangeLabel.Text =
            lockData.LastPasswordChange == DateTime.MinValue
            ? "آخر تغيير لكلمة المرور: —"
            : $"آخر تغيير لكلمة المرور: {lockData.LastPasswordChange:yyyy/MM/dd HH:mm}";

        DeveloperPasswordEntry.Text = "";
        DeveloperConfirmPasswordEntry.Text = "";

        DeveloperLockOverlay.IsVisible = true;
    }

    void OnCloseDeveloperLock(
        object sender,
        TappedEventArgs e)
    {
        DeveloperLockOverlay.IsVisible = false;
    }


    // =========================
    // SAVE DEVELOPER PASSWORD
    // =========================

    async void OnSaveDeveloperPasswordClicked(
        object sender,
        EventArgs e)
    {
        string password =
            DeveloperPasswordEntry.Text?.Trim() ?? "";

        string confirm =
            DeveloperConfirmPasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert(
               "تنبيه",
                "أدخل كلمة مرور المطور",
                "حسناً");

            return;
        }

        if (password.Length < 8)
        {
            await DisplayAlert(
                "تنبيه",
                "كلمة المرور يجب أن تكون 8 أحرف على الأقل",
                "حسناً");

            return;
        }

        if (password != confirm)
        {
            await DisplayAlert(
               "تنبيه",
                "كلمة المرور وتأكيدها غير متطابقين",
                "حسناً");

            return;
        }

        await DeveloperLockService.SetPasswordAsync(password);

        await DisplayAlert(
            "تم",
              "تم",
            "تم حفظ كلمة مرور المطور",
            "حسناً");

        DeveloperLockOverlay.IsVisible = false;
    }
    // =========================
    // TEST DEVELOPER PASSWORD
    // =========================

    async void OnTestDeveloperPasswordClicked(
        object sender,
        EventArgs e)
    {
        string password =
            DeveloperPasswordEntry.Text?.Trim() ?? "";

        bool valid =
            await DeveloperLockService.VerifyPasswordAsync(password);

        if (valid)
        {
            await SecurityLogService.AddAsync(
                "SECURITY",
                 "تم التحقق من قفل المطور",
                "Developer password verified");

            await DisplayAlert(
                "تم",
                "كلمة المرور صحيحة",
                "حسناً");
        }
        else
        {
            await SecurityLogService.AddAsync(
                "SECURITY",
                "فشل التحقق من قفل المطور",
                "Developer password verification failed");

            await DisplayAlert(
                "خطأ",
                "كلمة المرور غير صحيحة",
                "حسناً");
        }
    }

    void OnSecuritySettingsTapped(
    object sender,
    TappedEventArgs e)
    {
        ToggleSection(
            SecuritySettingsContent,
            SecurityArrow,
            ref isSecurityExpanded);
    }
    // =========================
    // CLEAR TEMPORARY LOGS
    // =========================

    async void OnClearTemporaryLogsClicked(
    object sender,
    EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "حذف السجلات المؤقتة",
               "سيتم حذف السجلات المؤقتة فقط.\n\nالسجلات الدائمة ستبقى محفوظة.",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        await SecurityLogService.ClearTemporaryAsync();

        await SecurityLogService.AddAsync(
            "SECURITY",
            "تم حذف السجلات المؤقتة",
            "Temporary security logs cleared by Developer",
            "Medium",
            true);

        await DisplayAlert(
            "تم",
            "تم حذف السجلات المؤقتة بنجاح",
            "حسناً");

        OnSecurityLogTapped(
            sender,
            new TappedEventArgs(null));
    }

    // =========================
    // HALL OF FAME ACTIVATION
    // =========================
    async void OnHonorActivationTapped(
      object sender,
      TappedEventArgs e)
    {
        HonorDisplayNameEntry.Text = "";
        HonorActivationKeyEntry.Text = "";

        var identity =
            await HonorIdentityService.LoadAsync();

        if (!identity.IsActivated)
        {
            CurrentHonorRoleLabel.Text =
                "👤 الحالة الحالية: مستخدم عادي";
        }
        else
        {
            switch (identity.Role)
            {
                case HonorRoleType.Developer:

                    CurrentHonorRoleLabel.Text =
                        "⚙️ الحالة الحالية: Developer";
                    break;

                case HonorRoleType.Founder:

                    CurrentHonorRoleLabel.Text =
                        $"👑 الحالة الحالية: Founder #{identity.FounderNumber}";
                    break;

                case HonorRoleType.Honor:

                    CurrentHonorRoleLabel.Text =
                        "🏛️ الحالة الحالية: Honor Member";
                    break;

                default:

                    CurrentHonorRoleLabel.Text =
                        "👤 الحالة الحالية: مستخدم عادي";
                    break;
            }
        }

        HonorActivationOverlay.IsVisible = true;
    }



    void OnCloseHonorActivation(
        object sender,
        TappedEventArgs e)
    {
        HonorActivationOverlay.IsVisible = false;
    }

    async void OnActivateHonorClicked(
        object sender,
        EventArgs e)
    {
        string displayName =
            HonorDisplayNameEntry.Text?.Trim() ?? "";

        string activationKey =
            HonorActivationKeyEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(displayName))
        {
            await DisplayAlert(
                "تنبيه",
                "أدخل الاسم أو ID",
                "حسناً");

            return;
        }

        if (string.IsNullOrWhiteSpace(activationKey))
        {
            await DisplayAlert(
                "تنبيه",
                "أدخل مفتاح التفعيل",
                "حسناً");

            return;
        }


        if (activationKey == HonorActivationService.FirstDeveloperSetupKey)
        {
            var current =
                await HonorIdentityService.LoadAsync();

            if (!current.IsActivated)
            {
                var setupIdentity =
                    new HonorIdentityModel
                    {
                        Role = HonorRoleType.Developer,
                        IsActivated = true,
                        ActivationKey = activationKey,
                        ActivationDate = DateTime.Now,
                        DeviceId = $"{DeviceInfo.Manufacturer}-{DeviceInfo.Model}-{DeviceInfo.Platform}",
                        DisplayName = displayName
                    };

                await HonorIdentityService.SaveAsync(setupIdentity);

                HonorActivationOverlay.IsVisible = false;

                await DisplayAlert(
                    "تم",
                    "تم تأسيس أول Developer بنجاح. الآن صدّر Developer Vault فوراً.",
                    "حسناً");

                return;
            }
        }

        var currentIdentity =
            await HonorIdentityService.LoadAsync();

        if (currentIdentity.IsActivated)
        {
            await SecurityLogService.AddAsync(
                "HONOR",
                "محاولة تفعيل صلاحية إضافية",
                $"Current Role: {currentIdentity.Role}",
                "Medium",
                false);

            await DisplayAlert(
                "الصلاحية مفعّلة مسبقاً",
                $"هذا الجهاز مفعّل حالياً بصلاحية:\n{currentIdentity.Role}\n\nلا يمكن تفعيل صلاحية أخرى على نفس الجهاز.",
                "حسناً");

            return;
        }

        bool activated =
            await HonorIdentityService.ActivateAsync(
                displayName,
                activationKey);

        if (!activated)
        {
            await SecurityLogService.AddAsync(
                "HONOR",
                "فشل تفعيل قاعة الشرف",
                "Invalid honor activation key",
                "Medium",
                false);

            await DisplayAlert(
                "فشل التفعيل",
                "مفتاح التفعيل غير صحيح.",
                "حسناً");

            return;
        }

        var identity =
            await HonorIdentityService.LoadAsync();

        string message =
            identity.Role switch
            {
                HonorRoleType.Developer =>
                    "تم تفعيل صلاحية المطور بنجاح.",

                HonorRoleType.Founder =>
                    $"تم تفعيل صلاحية Founder بنجاح.\n\nFounder #{identity.FounderNumber}",

                HonorRoleType.Honor =>
                    "تم تفعيل صلاحية Honor بنجاح.",

                _ =>
                    "تم التفعيل بنجاح."
            };

        HonorActivationOverlay.IsVisible = false;

        await DisplayAlert(
            "تم التفعيل",
            message,
            "حسناً");
    }
    // =========================
    // DEBUG: CLEAR HONOR IDENTITY
    // =========================
    async void OnDebugClearHonorIdentityClicked(
    object sender,
    EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "تصفير هوية الشرف",
                "سيتم حذف صلاحية الجهاز الحالية فقط.\n\nلن يتم حذف الفرق أو المباريات أو التصنيفات.",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        await HonorIdentityService.ClearAsync();

        CurrentHonorRoleLabel.Text =
            "👤 الحالة الحالية: مستخدم عادي";

        HonorDisplayNameEntry.Text = "";
        HonorActivationKeyEntry.Text = "";

        await DisplayAlert(
         "تم",
            "تم حذف هوية الشرف من الجهاز. يمكنك الآن تفعيل Developer أو Founder أو Honor من جديد.",
            "حسناً");
    }
}





