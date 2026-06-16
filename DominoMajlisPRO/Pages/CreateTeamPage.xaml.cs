using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class CreateTeamPage : ContentPage
{
    List<PlayerProfileModel> allPlayers = new();
    private bool IsEditMode = false;
   
    string selectedEmblem =
    "shield_3d.png";
    bool isTeamMode = true;

    string selectedColor = "#FFD700";
  

    private TeamProfileModel? CurrentTeam = null;
    private List<TeamProfileModel> LoadedTeams = new();
    public CreateTeamPage()
    {
        InitializeComponent();
      
         
        OnGoldColorClicked(this, EventArgs.Empty);
        ResetEmblemSelection();

        ShieldButton.Scale = 1.15;
        ShieldButton.Opacity = 1;
        OnTeamClicked(
           this,
           EventArgs.Empty);
        TeamNameEntry.TextChanged +=
  TeamNameChanged;

        Player1Entry.TextChanged +=
            TeamPlayersChanged;

        Player2Entry.TextChanged +=
            TeamPlayersChanged;
       

    }




    void OnSingleClicked(
    object sender,
    EventArgs e)
    {
        PreviewMode.Text =
     "فردي";
        isTeamMode = false;

        Player2Layout.IsVisible = false;
        PreviewPlayer2.IsVisible = false;
        string player1 =
    string.IsNullOrWhiteSpace(
        Player1Entry.Text)
    ? "اللاعب الأول"
    : Player1Entry.Text;


        PreviewPlayer1.Text = player1;
        PreviewPlayer2.Text = "";
        SingleCard.Stroke =
            Color.FromArgb("#FFD700");

        SingleCard.BackgroundColor =
            Color.FromArgb("#1A1A00");

        TeamCard.Stroke =
            Color.FromArgb("#404040");

        TeamCard.BackgroundColor =
            Color.FromArgb("#151515");

        SingleCard.ScaleTo(1.05, 150);
        TeamCard.ScaleTo(1.00, 150);
    }

    void OnTeamClicked(
        object sender,
        EventArgs e)
    {
        PreviewMode.Text =
    "فريق";
        isTeamMode = true;

        Player2Layout.IsVisible = true;
        TeamPlayersChanged(
    this,
    null);
        PreviewPlayer2.IsVisible = true;
        TeamCard.Stroke =
            Color.FromArgb("#FFD700");

        TeamCard.BackgroundColor =
            Color.FromArgb("#1A1A00");

        SingleCard.Stroke =
            Color.FromArgb("#404040");

        SingleCard.BackgroundColor =
            Color.FromArgb("#151515");

        TeamCard.ScaleTo(1.05, 150);
        SingleCard.ScaleTo(1.00, 150);
    }
    void OnEagleClicked(
     object sender,
     EventArgs e)
    {
        ResetEmblemSelection();
       
        EagleButton.Scale = 1.15;
        EagleButton.Opacity = 1;
        EagleSelected.IsVisible = true;
        selectedEmblem =
            "eagle_3d.png";

        
        PreviewEmblem.Source =
    selectedEmblem;
    }

    void OnLionClicked(
  object sender,
  EventArgs e)
    {
        ResetEmblemSelection();
       
       LionButton.Scale = 1.15;
        LionButton.Opacity = 1;
        LionSelected.IsVisible = true;
        selectedEmblem =
            "lion_3d.png";

    
        PreviewEmblem.Source =
    selectedEmblem;
    }

    void OnWolfClicked(
     object sender,
     EventArgs e)
    {
        ResetEmblemSelection();
       
        WolfButton.Scale = 1.15;
        WolfButton.Opacity = 1;
        WolfSelected.IsVisible = true;
        selectedEmblem =
            "wolf_3d.png";

        PreviewEmblem.Source =
    selectedEmblem;
    }

    void OnDragonClicked(
      object sender,
      EventArgs e)
    {
        ResetEmblemSelection();
      
       DragonButton.Scale = 1.15;
        DragonButton.Opacity = 1;
        DragonSelected.IsVisible = true;
        selectedEmblem =
            "dragon_3d.png";

        PreviewEmblem.Source =
    selectedEmblem;
    }

    void OnCrownClicked(
      object sender,
      EventArgs e)
    {
        ResetEmblemSelection();
       
      CrownButton.Scale = 1.15;
        CrownButton.Opacity = 1;
        CrownSelected.IsVisible = true;
        selectedEmblem =
            "crown_3d.png";

        PreviewEmblem.Source =
    selectedEmblem;
    }

    void OnShieldClicked(
     object sender,
     EventArgs e)
    {
        ResetEmblemSelection();
       
        ShieldButton.Scale = 1.15;
        ShieldButton.Opacity = 1;
        ShieldSelected.IsVisible = true;
        selectedEmblem =
            "shield_3d.png";

        PreviewEmblem.Source =
    selectedEmblem;
    }

    // Color Selection

    void OnGoldColorClicked(object sender, EventArgs e)
    {
        ResetColorSelection();

        GoldSelected.IsVisible = true;
      

        selectedColor = "#FFD700";

        PreviewColorDot.BackgroundColor =
            Color.FromArgb(selectedColor);
    }

    void OnBlueColorClicked(
object sender,
EventArgs e)
    {
        ResetColorSelection();

        BlueSelected.IsVisible = true;
      

        selectedColor = "#0066FF";

        PreviewColorDot.BackgroundColor =
            Color.FromArgb(selectedColor);
    }

    void OnGreenColorClicked(
    object sender,
    EventArgs e)
    {
        ResetColorSelection();

        GreenSelected.IsVisible = true;
        

        selectedColor = "#00C853";

        PreviewColorDot.BackgroundColor =
            Color.FromArgb(selectedColor);
    }

    void OnRedColorClicked(
    object sender,
    EventArgs e)
    {
        ResetColorSelection();

        RedSelected.IsVisible = true;
       

        selectedColor = "#FF1744";

        PreviewColorDot.BackgroundColor =
            Color.FromArgb(selectedColor);
    }

    void OnPurpleColorClicked(
    object sender,
    EventArgs e)
    {
        ResetColorSelection();

        PurpleSelected.IsVisible = true;
      

        selectedColor = "#AA00FF";

        PreviewColorDot.BackgroundColor =
            Color.FromArgb(selectedColor);
    }

    void OnBlackColorClicked(
    object sender,
    EventArgs e)
    {
        ResetColorSelection();

        BlackSelected.IsVisible = true;
     

        selectedColor = "#111111";

        PreviewColorDot.BackgroundColor =
            Color.FromArgb(selectedColor);
    }
    // Condition 
    private string BuildTeamSignature(
       string player1Id,
       string player2Id)
    {
        var players =
            new List<string>();

        if (!string.IsNullOrWhiteSpace(player1Id))
            players.Add(player1Id);

        if (!string.IsNullOrWhiteSpace(player2Id))
            players.Add(player2Id);

        players.Sort();

        return string.Join("|", players);
    }

    private bool TeamCompositionExists(
      List<TeamProfileModel> teams,
      string player1Id,
      string player2Id)
    {
        string newSignature =
            BuildTeamSignature(
                player1Id,
                player2Id);

        foreach (var team in teams)
        {
            if (CurrentTeam != null &&
                team.TeamId ==
                CurrentTeam.TeamId)
                continue;

            string existingSignature =
                BuildTeamSignature(
                    team.Player1Id,
                    team.Player2Id);

            if (existingSignature ==
                newSignature)
            {
                return true;
            }
        }

        return false;
    }
    async void OnSaveClicked(
       object sender,
       EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(
            TeamNameEntry.Text))
        {
            await DisplayAlert(
                "تنبيه",
                "أدخل اسم الفريق",
                "حسناً");

            return;
        }

        var teams =
            await TeamProfileService.LoadTeamsAsync();
        string player1 =
    Player1Entry.Text?.Trim() ?? "";

        string player2 =
            Player2Entry.Text?.Trim() ?? "";
        if (!isTeamMode)
        {
            player2 = "";
            Player2Entry.Text = "";
        }


        string normalizedPlayer1 =
   PlayerIdentityService.NormalizePlayerName(player1);

        string normalizedPlayer2 =
          PlayerIdentityService.NormalizePlayerName(player2);

        if (!string.IsNullOrWhiteSpace(player2)
            &&
            normalizedPlayer1 ==
            normalizedPlayer2)
        {
            await DisplayAlert(
                "لا يمكن إنشاء الفريق",
                "لا يمكن إضافة نفس اللاعب مرتين داخل الفريق",
                "حسناً");

            return;
        }
        var duplicateName =
    teams.Any(x =>
        x.TeamName.Trim()
        .Equals(
            TeamNameEntry.Text.Trim(),
            StringComparison.OrdinalIgnoreCase)
        &&
        (CurrentTeam == null ||
         x.TeamId != CurrentTeam.TeamId)); ;

        if (duplicateName)
        {
            await DisplayAlert(
                "تنبيه",
                "اسم الفريق مستخدم مسبقاً",
                "حسناً");

            return;
        }


        string player1Id =
        await GetOrCreatePlayerIdAsync(
            player1);

        string player2Id =
            await GetOrCreatePlayerIdAsync(
                player2);

        if (TeamCompositionExists(
                teams,
                player1Id,
                player2Id))
        {
            await DisplayAlert(
                "تنبيه",
                "هذه التشكيلة تمتلك فريقاً مسبقاً",
                "حسناً");

            return;
        }

        // إنشاء جديد
       
        if (!IsEditMode)
        {
            string nextTeamId =
           GenerateNextTeamId(
               teams);

            TeamProfileModel team =
                new()
                {
                    TeamId = nextTeamId,

                    TeamName = TeamNameEntry.Text,

                    Player1 = player1,
                    Player2 = player2,

                    Player1Id = player1Id,
                    Player2Id = player2Id,

                    IsSinglePlayer = !isTeamMode,

                    Emblem = selectedEmblem,
                    ColorHex = selectedColor
                };

            teams.Add(team);

            await TeamProfileService
                .SaveTeamsAsync(teams);
            AppEvents.RaiseDataChanged();

            await DisplayAlert(
                "تم",
                "تم إنشاء الفريق",
                "ممتاز");

            ResetForm();

            return;
        }

        // تحديث فريق موجود
        var existing =
            teams.FirstOrDefault(
                x => x.TeamId ==
                CurrentTeam?.TeamId);

        if (existing == null)
            return;

        existing.TeamName =
            TeamNameEntry.Text;

        existing.Player1 =
            Player1Entry.Text ?? "";

        existing.Player2 =
            Player2Entry.Text ?? "";

        existing.Player1Id =
            player1Id;

        existing.Player2Id =
            player2Id;

        existing.IsSinglePlayer =
    !isTeamMode;

        existing.Emblem =
            selectedEmblem;

        existing.ColorHex =
            selectedColor;

        var rankings =
    await RankingService.LoadTeamsAsync();

        var rankingTeam =
            rankings.FirstOrDefault(
                x => x.TeamId ==
                existing.TeamId);

        if (rankingTeam != null)
        {
            rankingTeam.TeamName =
                existing.TeamName;

            rankingTeam.Player1 =
                existing.Player1;

            rankingTeam.Player2 =
                existing.Player2;

            rankingTeam.Player1Id =
                existing.Player1Id;

            rankingTeam.Player2Id =
                existing.Player2Id;

            rankingTeam.IsSinglePlayer =
    existing.IsSinglePlayer;


            rankingTeam.Emblem =
                existing.Emblem;

            rankingTeam.ColorHex =
                existing.ColorHex;

            string json =
                System.Text.Json.JsonSerializer.Serialize(
                    rankings,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            string rankingFile =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    "rankings.json");

            await File.WriteAllTextAsync(
                rankingFile,
                json);
        }


        await TeamProfileService
            .SaveTeamsAsync(teams);

        AppEvents.RaiseDataChanged();
        await DisplayAlert(
            "تم",
            "تم تحديث الفريق",
            "ممتاز");

        ResetForm();

        LoadedTeams =
            await TeamProfileService.LoadTeamsAsync();

        TeamsCollection.ItemsSource = null;
        TeamsCollection.ItemsSource = LoadedTeams;
    }
    async void OnBackClicked(
    object sender,
    EventArgs e)
    {
        await Navigation.PopAsync();
    }
    void ResetEmblemSelection()
    {
        WolfButton.Scale = 1;
        EagleButton.Scale = 1;
        LionButton.Scale = 1;
        DragonButton.Scale = 1;
        CrownButton.Scale = 1;
        ShieldButton.Scale = 1;

        WolfButton.Opacity = 0.75;
        EagleButton.Opacity = 0.75;
        LionButton.Opacity = 0.75;
        DragonButton.Opacity = 0.75;
        CrownButton.Opacity = 0.75;
        ShieldButton.Opacity = 0.75;

        EagleSelected.IsVisible = false;
        WolfSelected.IsVisible = false;
        LionSelected.IsVisible = false;
        DragonSelected.IsVisible = false;
        CrownSelected.IsVisible = false;
        ShieldSelected.IsVisible = false;


    }

    //Color
    void ResetColorSelection()
    {
        GoldSelected.IsVisible = false;
        BlueSelected.IsVisible = false;
        GreenSelected.IsVisible = false;
        RedSelected.IsVisible = false;
        PurpleSelected.IsVisible = false;
        BlackSelected.IsVisible = false;

      
    }

    void TeamNameChanged(
    object sender,
    TextChangedEventArgs e)
    {
        PreviewTeamName.Text =
            string.IsNullOrWhiteSpace(
                TeamNameEntry.Text)
            ? "اسم الفريق"
            : TeamNameEntry.Text;
    }

    void TeamPlayersChanged(
        object sender,
        TextChangedEventArgs e)
    {
        string player1 =
            string.IsNullOrWhiteSpace(
                Player1Entry.Text)
            ? "اللاعب الأول"
            : Player1Entry.Text;

        string player2 =
            string.IsNullOrWhiteSpace(
                Player2Entry.Text)
            ? "اللاعب الثاني"
            : Player2Entry.Text;

        PreviewPlayer1.Text = player1;

        PreviewPlayer2.Text =
            isTeamMode
            ? player2
            : "";
    }


    //Delet 
    async void OnDeleteTeamClicked(
      object sender,
      TappedEventArgs e)
    {
        var image = sender as Image;

        var team =
            image?.BindingContext
            as TeamProfileModel;

        if (team == null)
            return;

        bool confirm =
            await DisplayAlert(
                "حذف الفريق",
                $"هل تريد حذف {team.TeamName} ؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        var teams =
            await TeamProfileService.LoadTeamsAsync();


        teams.RemoveAll(
            x => x.TeamId == team.TeamId);

        await TeamProfileService.SaveTeamsAsync(
            teams);
        AppEvents.RaiseDataChanged();

        var rankings =
    await RankingService.LoadTeamsAsync();

        rankings.RemoveAll(
            x => x.TeamId ==
            team.TeamId);

        string rankingsJson =
            System.Text.Json.JsonSerializer.Serialize(
                rankings,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

        string rankingsFile =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "rankings.json");

        await File.WriteAllTextAsync(
            rankingsFile,
            rankingsJson);
        AppEvents.RaiseDataChanged();

        LoadedTeams =
            await TeamProfileService.LoadTeamsAsync();

        TeamsCollection.ItemsSource = null;
        TeamsCollection.ItemsSource = LoadedTeams;
    }

    

    // Delete All

    async void OnDeleteAllTeamsClicked(
    object sender,
    EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "حذف جميع الفرق",
                "سيتم حذف جميع الفرق المحفوظة نهائياً، هل أنت متأكد؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        await TeamProfileService.SaveTeamsAsync(
            new List<TeamProfileModel>());
        AppEvents.RaiseDataChanged();

        string rankingsFile =
    Path.Combine(
        FileSystem.AppDataDirectory,
        "rankings.json");

        if (File.Exists(rankingsFile))
        {
            File.Delete(rankingsFile);
        }
        AppEvents.RaiseDataChanged();
        LoadedTeams.Clear();

        TeamsCollection.ItemsSource = null;

        TeamsCollection.ItemsSource =
            LoadedTeams;
        CurrentTeam = null;

        IsEditMode = false;

        TeamNameEntry.Text = "";
        Player1Entry.Text = "";
        Player2Entry.Text = "";

        
        await DisplayAlert(
            "تم",
            "تم حذف جميع الفرق",
            "ممتاز");
    }



    // Select Team To Edit 
    async void OnSelectTeamClicked(
       object sender,
       EventArgs e)
    {
        LoadedTeams =
            await TeamProfileService.LoadTeamsAsync();

        TeamsCollection.ItemsSource = null;

        TeamsCollection.ItemsSource =
            LoadedTeams;

        TeamsOverlay.IsVisible = true;

        await Task.Delay(50);

        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                await TeamsOverlay.FadeTo(
                    1,
                    180);
            });
     
    }

    // Delete All
    async Task OnDeleteAllTeamsDirect()
    {
        bool confirm =
            await DisplayAlert(
                "حذف جميع الفرق",
                "سيتم حذف جميع الفرق المحفوظة نهائياً، هل أنت متأكد؟",
                "نعم",
                "إلغاء");

        if (!confirm)
            return;

        await TeamProfileService.SaveTeamsAsync(
            new List<TeamProfileModel>());
        AppEvents.RaiseDataChanged();

        CurrentTeam = null;

        IsEditMode = false;

        TeamNameEntry.Text = "";
        Player1Entry.Text = "";
        Player2Entry.Text = "";

       
       

        await DisplayAlert(
            "تم",
            "تم حذف جميع الفرق",
            "ممتاز");
    }

    void LoadTeam(
    TeamProfileModel team)
    {

        SaveButtonIcon.Source = "edit_card.png";
        CurrentTeam = team;

        IsEditMode = true;

        if (team.IsSinglePlayer)
        {
            OnSingleClicked(this, EventArgs.Empty);
        }
        else
        {
            OnTeamClicked(this, EventArgs.Empty);
        }


        TeamNameEntry.Text =
            team.TeamName;

        Player1Entry.Text =
            team.Player1;

        Player2Entry.Text =
            team.Player2;

        selectedEmblem =
            team.Emblem;

        selectedColor =
            team.ColorHex;

        PreviewEmblem.Source =
            selectedEmblem;

        PreviewColorDot.BackgroundColor =
            Color.FromArgb(selectedColor);


       
        SaveButtonText.Text =
    "تحديث الفريق";
        ApplyLoadedEmblem();

        ApplyLoadedColor();
    }

    void ApplyLoadedEmblem()
    {
        ResetEmblemSelection();

        switch (selectedEmblem)
        {
            case "shield_3d.png":
                ShieldSelected.IsVisible = true;
                ShieldButton.Scale = 1.15;
                break;

            case "crown_3d.png":
                CrownSelected.IsVisible = true;
                CrownButton.Scale = 1.15;
                break;

            case "dragon_3d.png":
                DragonSelected.IsVisible = true;
                DragonButton.Scale = 1.15;
                break;

            case "lion_3d.png":
                LionSelected.IsVisible = true;
                LionButton.Scale = 1.15;
                break;

            case "wolf_3d.png":
                WolfSelected.IsVisible = true;
                WolfButton.Scale = 1.15;
                break;

            case "eagle_3d.png":
                EagleSelected.IsVisible = true;
                EagleButton.Scale = 1.15;
                break;
        }

    }

    void ApplyLoadedColor()
    {
        ResetColorSelection();

        switch (selectedColor)
        {
            case "#FFD700":
                GoldSelected.IsVisible = true;
                break;

            case "#0066FF":
                BlueSelected.IsVisible = true;
                break;

            case "#00C853":
                GreenSelected.IsVisible = true;
                break;

            case "#FF1744":
                RedSelected.IsVisible = true;
                break;

            case "#AA00FF":
                PurpleSelected.IsVisible = true;
                break;

            case "#111111":
                BlackSelected.IsVisible = true;
                break;
        }
    }

    // Reset Form For New Team Creation
    void ResetForm()
    {
        SaveButtonIcon.Source = "save_card.png";
        CurrentTeam = null;

        IsEditMode = false;

        TeamNameEntry.Text = "";

        Player1Entry.Text = "";

        Player2Entry.Text = "";

        selectedEmblem = "shield_3d.png";

        selectedColor = "#FFD700";

        PreviewEmblem.Source =
            selectedEmblem;

        PreviewColorDot.BackgroundColor =
            Color.FromArgb(selectedColor);

        SaveButtonText.Text =
            "إنشاء الفريق";

       
          

        ResetEmblemSelection();

        ShieldSelected.IsVisible = true;

        ShieldButton.Scale = 1.15;

        ResetColorSelection();

        GoldSelected.IsVisible = true;
    }


    void OnTeamSelected(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
            return;

        var team =
            e.CurrentSelection[0]
            as TeamProfileModel;

        if (team == null)
            return;

        LoadTeam(team);
        TeamSearchEntry.Text = "";

        NoResultsLabel.IsVisible = false;
        TeamsOverlay.IsVisible = false;

       
    }
    async void OnEditTeamClicked(
    object sender,
    TappedEventArgs e)
    {
        var image = sender as Image;

        var team =
            image?.BindingContext
            as TeamProfileModel;

        if (team == null)
            return;

        LoadTeam(team);

        TeamsOverlay.IsVisible = false;
        await Task.Delay(100);
        await MainScroll.ScrollToAsync(0, 0, true);
        TeamNameEntry.Focus();

       
    }


    //Search 
    void OnTeamSearchTextChanged(
    object sender,
    TextChangedEventArgs e)
    {
        if (LoadedTeams == null)
            return;

        string searchText =
            e.NewTextValue?
            .Trim()
            .ToLower() ?? "";

        // عرض الكل عند عدم وجود نص
        if (string.IsNullOrWhiteSpace(searchText))
        {
            TeamsCollection.ItemsSource = LoadedTeams;

            NoResultsLabel.IsVisible = false;

            return;
        }

        var filtered =
            LoadedTeams
            .Where(x =>
                (!string.IsNullOrWhiteSpace(x.TeamName)
                    && x.TeamName.ToLower()
                    .Contains(searchText))

                ||

                (!string.IsNullOrWhiteSpace(x.Player1)
                    && x.Player1.ToLower()
                    .Contains(searchText))

                ||

                (!string.IsNullOrWhiteSpace(x.Player2)
                    && x.Player2.ToLower()
                    .Contains(searchText))
            )
            .ToList();

        TeamsCollection.ItemsSource = filtered;

        NoResultsLabel.IsVisible =
            filtered.Count == 0;
    }
    // Search Bar Cleen
    void OnCloseTeamsOverlay(
     object sender,
     EventArgs e)
    {
        TeamSearchEntry.Text = "";

        TeamsCollection.ItemsSource =
            LoadedTeams;

        NoResultsLabel.IsVisible = false;

        TeamsOverlay.IsVisible = false;
    }





    async Task<string> GetOrCreatePlayerIdAsync(
        string playerName)
    {

        if (string.IsNullOrWhiteSpace(playerName))
            return "";

        var players =
            await PlayerProfileService
            .LoadPlayersAsync();

        string normalized =
            PlayerIdentityService
            .NormalizePlayerName(playerName);

        var existing =
            players.FirstOrDefault(x =>
                PlayerIdentityService
                .NormalizePlayerName(
                    x.PlayerName) == normalized);

        if (existing != null)
            return existing.PlayerId;

        var similarPlayer =
            players.FirstOrDefault(x =>
                IsVerySimilarName(
                    x.PlayerName,
                    playerName));

        if (similarPlayer != null)
        {
            bool useExisting =
                await DisplayAlert(
                    "لاعب مشابه",
                    $"تم العثور على لاعب مشابه:\n\n{similarPlayer.PlayerName}\n({similarPlayer.PlayerId})\n\nهل تقصد هذا اللاعب؟",
                    "نعم",
                    "لا");

            if (useExisting)
            {
                return similarPlayer.PlayerId;
            }
        }

        string nextId =
            $"P{(players.Count + 1):0000}";

        players.Add(
            new PlayerProfileModel
            {
                PlayerId = nextId,
                PlayerName = playerName.Trim(),
                CreatedAt = DateTime.Now
            });

        await PlayerProfileService
            .SavePlayersAsync(players);

        return nextId;
    }
    string GenerateNextTeamId(
    List<TeamProfileModel> teams)
    {
        if (teams.Count == 0)
            return "T0001";

        int maxId =
            teams
            .Where(x =>
                !string.IsNullOrWhiteSpace(
                    x.TeamId))
            .Select(x =>
            {
                string numericPart =
                    x.TeamId.Replace("T", "");

                return int.TryParse(
                    numericPart,
                    out int id)
                    ? id
                    : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"T{(maxId + 1):0000}";
    }

    bool IsVerySimilarName(
    string name1,
    string name2)
    {
        name1 =
            PlayerIdentityService
            .NormalizePlayerName(name1);

        name2 =
            PlayerIdentityService
            .NormalizePlayerName(name2);

        if (name1 == name2)
            return true;

        if (Math.Abs(
                name1.Length -
                name2.Length) > 1)
            return false;

        int differences = 0;

        int minLength =
            Math.Min(
                name1.Length,
                name2.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (name1[i] != name2[i])
                differences++;
        }

        differences +=
            Math.Abs(
                name1.Length -
                name2.Length);

        return differences <= 1;

    }

    // Normalize Player Name For Comparison

    // Register Player If Not Exists
    async Task RegisterPlayerAsync(
    string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        var players =
            await PlayerProfileService
            .LoadPlayersAsync();

        string normalized =
           PlayerIdentityService.NormalizePlayerName(playerName);

        bool exists =
     players.Any(x =>
         PlayerIdentityService
             .NormalizePlayerName(
                 x.PlayerName) == normalized);

        if (exists)
            return;

        string nextId =
            $"P{(players.Count + 1):0000}";

        players.Add(
            new PlayerProfileModel
            {
                PlayerId = nextId,
                PlayerName = playerName.Trim(),
                CreatedAt = DateTime.Now
            });

        await PlayerProfileService
            .SavePlayersAsync(players);
    }
}