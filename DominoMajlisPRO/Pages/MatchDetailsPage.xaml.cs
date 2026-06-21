using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.ApplicationModel.DataTransfer;
namespace DominoMajlisPRO.Pages;

public partial class MatchDetailsPage : ContentPage
{
    SavedMatch? match;
    bool roundsExpanded = false;

    const int InitialRoundsCount = 10;
    public MatchDetailsPage(
        SavedMatch? savedMatch = null)
    {
        InitializeComponent();

        match = savedMatch;

        _ = LoadMatchData();
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.DataChanged -= OnMatchDetailsDataChanged;
        AppEvents.MatchesChanged -= OnMatchDetailsDataChanged;
        AppEvents.TeamsChanged -= OnMatchDetailsDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.PlayerProfileChanged -= OnMatchDetailsDataChanged;

        AppEvents.DataChanged += OnMatchDetailsDataChanged;
        AppEvents.MatchesChanged += OnMatchDetailsDataChanged;
        AppEvents.TeamsChanged += OnMatchDetailsDataChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        AppEvents.PlayerProfileChanged += OnMatchDetailsDataChanged;

        await LoadMatchData();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        AppEvents.DataChanged -= OnMatchDetailsDataChanged;
        AppEvents.MatchesChanged -= OnMatchDetailsDataChanged;
        AppEvents.TeamsChanged -= OnMatchDetailsDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.PlayerProfileChanged -= OnMatchDetailsDataChanged;
    }

    async void OnMatchDetailsDataChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                await LoadMatchData();
            });
    }
    async Task LoadMatchData()
    {

        if (match == null)
            return;

        var team1 =
            await TeamProfileService
                .GetTeamByIdAsync(match.Team1Id);

        var team2 =
            await TeamProfileService
                .GetTeamByIdAsync(match.Team2Id);
        var identities = await TeamIdentityResolver.ResolveManyAsync(
            new[] { match.Team1Id, match.Team2Id });
        // Team 1

        Team1NameLabel.Text =
            match.Team1Name;

        if (team1 != null)
        {
            Team1IdLabel.Text =
                team1.TeamId;

            Team1Player1Label.Text =
      $"{team1.Player1} ({team1.Player1Id})";

            if (team1.IsSinglePlayer)
            {
                Team1Player2Label.IsVisible = false;
            }
            else
            {
                Team1Player2Label.IsVisible = true;

                Team1Player2Label.Text =
                    $"{team1.Player2} ({team1.Player2Id})";
            }

            identities.TryGetValue(
                match.Team1Id,
                out var team1Identity);
            Team1Emblem.Source =
                InventoryDisplayResolver.ResolveImageSource(
                    team1Identity?.EmblemImagePath ?? team1.Emblem,
                    "shield_3d.png");
            Team1Border.Stroke =
                SafeColor(
                    team1Identity?.TeamColorHex,
                    team1.ColorHex);
            Team1Border.BackgroundColor =
                SafeBackground(
                    team1Identity?.EmblemBackgroundSource);
        }

        // Team 2

        Team2NameLabel.Text =
            match.Team2Name;

        if (team2 != null)
        {
            Team2IdLabel.Text =
                team2.TeamId;

            Team2Player1Label.Text =
     $"{team2.Player1} ({team2.Player1Id})";

            if (team2.IsSinglePlayer)
            {
                Team2Player2Label.IsVisible = false;
            }
            else
            {
                Team2Player2Label.IsVisible = true;

                Team2Player2Label.Text =
                    $"{team2.Player2} ({team2.Player2Id})";
            }

            identities.TryGetValue(
                match.Team2Id,
                out var team2Identity);
            Team2Emblem.Source =
                InventoryDisplayResolver.ResolveImageSource(
                    team2Identity?.EmblemImagePath ?? team2.Emblem,
                    "shield_3d.png");
            Team2Border.Stroke =
                SafeColor(
                    team2Identity?.TeamColorHex,
                    team2.ColorHex);
            Team2Border.BackgroundColor =
                SafeBackground(
                    team2Identity?.EmblemBackgroundSource);
        }
        RoundsCountLabel.Text =
    match.RoundsHistory.Count.ToString();

        MelesLabel.Text =
            match.HasMeles
                ? "YES"
                : "NO";
        WinnerLabel.Text =
    match.WinnerTeam;

        ResultLabel.Text =
            $"{match.Team1Score} - {match.Team2Score}";
        if (match.RoundsHistory.Any())
        {
            var lastRound =
                match.RoundsHistory.Last();

        }
       
        MatchDateLabel.Text =
    match.MatchDate.ToString("yy/MM/dd");
        MatchStartTimeLabel.Text = match.MatchDate.ToString("HH:mm");
        MatchStartTimeLabel.Text =
     match.MatchDate.ToString("HH:mm");

        if (match.MatchEndDate > DateTime.MinValue)
        {
            MatchEndTimeLabel.Text =
                match.MatchEndDate.ToString("HH:mm");
        }
        else
        {
            MatchEndTimeLabel.Text = "--:--";
        }

        RulesTypeLabel.Text =
            match.IsLocalRules
                ? "محلي"
                : "دولي";
        BuildRoundsHistory();
        WinnerLabel.Text=String.IsNullOrWhiteSpace(match.WinnerTeamName)
            ?match.WinnerTeam
            : match.WinnerTeamName;

    }

    void OnTeamAssetsChanged(string teamId)
    {
        if (match == null ||
            (!string.Equals(teamId, match.Team1Id, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(teamId, match.Team2Id, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        OnMatchDetailsDataChanged();
    }

    static Color SafeColor(string? preferred, string fallback)
    {
        try
        {
            return Color.FromArgb(
                string.IsNullOrWhiteSpace(preferred)
                    ? fallback
                    : preferred);
        }
        catch
        {
            return Color.FromArgb("#FFD700");
        }
    }

    static Color SafeBackground(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Equals(
                "Transparent",
                StringComparison.OrdinalIgnoreCase) ||
            !value.StartsWith('#'))
        {
            return Color.FromArgb("#111111");
        }

        try { return Color.FromArgb(value); }
        catch { return Color.FromArgb("#111111"); }
    }

    // This method builds
    void BuildRoundsHistory()
    {
        if (match == null)
            return;

        RoundsContainer.Children.Clear();
        int roundsToShow =
            roundsExpanded
            ? match.RoundsHistory.Count
            : Math.Min(
                InitialRoundsCount,
                match.RoundsHistory.Count);

        for (int i = 0; i < roundsToShow; i++)
        {
            var round = match.RoundsHistory[i];

            string intervalText =
                i == 0
                ? "+00:00"
                : $"+{(round.RoundTime - match.RoundsHistory[i - 1].RoundTime):mm\\:ss}";

            var rowBorder = new Border
            {
                Stroke = Colors.Gold,
                StrokeThickness = 1.5,
                BackgroundColor = Color.FromArgb("#111111"),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 20
                },
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var row = new Grid
            {
                ColumnSpacing = 1,

                ColumnDefinitions =
            {
 new ColumnDefinition{ Width = 25 }, // الجولة
    new ColumnDefinition{ Width = 40 }, // الوقت

    new ColumnDefinition{ Width = 40 }, // الفائز

    new ColumnDefinition{ Width = 30 }, // نقاط الفائز

    new ColumnDefinition{ Width = 50 }, // النتيجة

    new ColumnDefinition{ Width = 40 }, // الخاسر

    new ColumnDefinition{ Width = 30 }, // نقاط الخاسر

    new ColumnDefinition{ Width = 20 }  // ملص
                                       }
            };

            // الجولة

            var roundLabel = new Label
            {
                Text = round.RoundNumber.ToString(),
                TextColor = Colors.Gold,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(roundLabel, 0);

            // الوقت

            var timeLabel = new Label
            {
                Text = intervalText,
                TextColor = Colors.White,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(timeLabel, 1);

            // شعار + اسم الفائز

            var winnerPanel = new VerticalStackLayout
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            winnerPanel.Children.Add(
                new Image
                {
                    Source =
                        InventoryDisplayResolver.ResolveImageSource(
                            round.WinnerTeamEmblem,
                            "shield_3d.png"),
                    WidthRequest = 22,
                    HeightRequest = 22
                });

            winnerPanel.Children.Add(
                new Label
                {
                    Text = round.WinnerTeam,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    MaxLines = 1,
                    LineBreakMode = LineBreakMode.TailTruncation
                });

            Grid.SetColumn(winnerPanel, 2);

            // نقاط الفائز

            var winnerPoints = new Label
            {
                Text = $"+{round.Points}",
                TextColor = Color.FromArgb("#4CAF50"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(winnerPoints, 3);

            // النتيجة

            var scoreLabel = new Label
            {
                Text = $"{round.Team1NewScore}-{round.Team2NewScore}",
                TextColor = Colors.Gold,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(scoreLabel, 4);

            // شعار + اسم الخاسر

            var loserPanel = new VerticalStackLayout
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            loserPanel.Children.Add(
                new Image
                {
                    Source =
                        InventoryDisplayResolver.ResolveImageSource(
                            round.LoserTeamEmblem,
                            "shield_3d.png"),
                    WidthRequest = 22,
                    HeightRequest = 22
                });

            loserPanel.Children.Add(
                new Label
                {
                    Text = round.LoserTeam,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    MaxLines = 1,
                    LineBreakMode = LineBreakMode.TailTruncation
                });

            Grid.SetColumn(loserPanel, 5);

            // نقاط الخاسر

            var loserPoints = new Label
            {
                Text =
                    round.WinnerTeam == match.Team1Name
                    ? round.Team2NewScore.ToString()
                    : round.Team1NewScore.ToString(),

                TextColor = Colors.White,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(loserPoints, 6);

            row.Children.Add(roundLabel);
            row.Children.Add(timeLabel);
            row.Children.Add(winnerPanel);
            row.Children.Add(winnerPoints);
            row.Children.Add(scoreLabel);
            row.Children.Add(loserPanel);
            row.Children.Add(loserPoints);

            if (round.IsMeles)
            {
                var meles = new Image
                {
                    Source = "meles_badge_gold.png",
                    WidthRequest = 20,
                    HeightRequest = 20
                };

                Grid.SetColumn(meles, 7);

                row.Children.Add(meles);
            }

            rowBorder.Content = row;

            RoundsContainer.Children.Add(rowBorder);
        }
        if (match.RoundsHistory.Count >
    InitialRoundsCount)
        {
            AddShowMoreButton(
                match.RoundsHistory.Count -
                InitialRoundsCount);
        }
    }
    // Add More Buttom

    void AddShowMoreButton(int hiddenRounds)
    {
        var buttonBorder = new Border
        {
            Stroke = Colors.Gold,
            StrokeThickness = 1.5,
            BackgroundColor = Color.FromArgb("#111111"),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 18
            },
            Padding = 12,
            Margin = new Thickness(0, 5)
        };

        var label = new Label
        {
            Text = roundsExpanded
                ? "عرض أقل"
                : $"عرض المزيد ({hiddenRounds})",

            TextColor = Colors.Gold,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };

        buttonBorder.Content = label;

        var tap = new TapGestureRecognizer();

        tap.Tapped += (s, e) =>
        {
            roundsExpanded = !roundsExpanded;

            BuildRoundsHistory();
        };

        buttonBorder.GestureRecognizers.Add(tap);

        RoundsContainer.Children.Add(buttonBorder);
    }
    // Back 
    async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    bool panelOpened = false;
    bool momentumOpened = false;

    async void OnMenuClicked(
        object sender,
        EventArgs e)
    {
        if (panelOpened)
            return;

        BottomSheetRoot.IsVisible = true;

        await BottomSheet.TranslateToAsync(
            0,
            0,
            250,
            Easing.CubicOut);

        panelOpened = true;
    }

    async void OnOverlayTapped(
        object sender,
        TappedEventArgs e)
    {
        if (!panelOpened)
            return;

        await BottomSheet.TranslateToAsync(
            0,
            600,
            250,
            Easing.CubicIn);

        BottomSheetRoot.IsVisible = false;

        panelOpened = false;
    }
    async void OnShareImageClicked(
        object sender,
        TappedEventArgs e)
    {
        await DisplayAlert(
            "قريباً",
            "ميزة مشاركة الصورة قيد التطوير",
            "حسناً");
    }

    async void OnSharePdfClicked(
        object sender,
        TappedEventArgs e)
    {
        await DisplayAlert(
            "قريباً",
            "ميزة PDF قيد التطوير",
            "حسناً");
    }

    // copy
    async void OnCopyResultClicked(
     object sender,
     TappedEventArgs e)
    {
        if (match == null)
            return;

        string resultText =
    $@"🏆 نتيجة مباراة Domino Majlis PRO

الفائز: {WinnerLabel.Text}

الخاسر:
{(match.WinnerTeam == match.Team1Name
    ? match.Team2Name
    : match.Team1Name)}
النتيجة: {match.Team1Score} - {match.Team2Score}

الجولات: {match.RoundsHistory.Count}

القوانين: {(match.IsLocalRules ? "محلي" : "دولي")}

ملص: {(match.HasMeles ? "نعم" : "لا")}";

        await Clipboard.Default.SetTextAsync(resultText);

        await DisplayAlert(
            "تم النسخ",
            "تم نسخ النتيجة إلى الحافظة",
            "حسناً");
    }


    // Momentum
    async void OnMomentumClicked(
      object sender,
      TappedEventArgs e)
    {
        if (match == null ||
            match.RoundsHistory.Count == 0)
            return;

        // أسرع جولة

        TimeSpan fastest = TimeSpan.MaxValue;

        // أبطأ جولة

        TimeSpan slowest = TimeSpan.Zero;

        // مجموع الأزمنة

        TimeSpan total = TimeSpan.Zero;
       

        for (int i = 1; i < match.RoundsHistory.Count; i++)
        {
            var diff =
                match.RoundsHistory[i].RoundTime -
                match.RoundsHistory[i - 1].RoundTime;

            if (diff < fastest)
                fastest = diff;

            if (diff > slowest)
                slowest = diff;

            total += diff;
        }

        if (match.RoundsHistory.Count == 1)
        {
            fastest = TimeSpan.Zero;
            slowest = TimeSpan.Zero;
        }

        var average =
            match.RoundsHistory.Count > 1
            ? TimeSpan.FromSeconds(
                total.TotalSeconds /
                (match.RoundsHistory.Count - 1))
            : TimeSpan.Zero;

        // أطول سلسلة انتصارات

        int longestStreak = 1;
        int currentStreak = 1;

        string currentWinner =
            match.RoundsHistory[0].WinnerTeam;

        for (int i = 1; i < match.RoundsHistory.Count; i++)
        {
            if (match.RoundsHistory[i].WinnerTeam ==
                currentWinner)
            {
                currentStreak++;
            }
            else
            {
                currentWinner =
                    match.RoundsHistory[i].WinnerTeam;

                currentStreak = 1;
            }

            if (currentStreak > longestStreak)
                longestStreak = currentStreak;
        }

        // الفريق المسيطر

        var dominantTeam =
            match.RoundsHistory
            .GroupBy(x => x.WinnerTeam)
            .OrderByDescending(x => x.Count())
            .First()
            
            .Key;

        // أعلى نقاط

        var highestPoints =
            match.RoundsHistory
            .Max(x => x.Points);

        // تعبئة الواجهة

        FastestRoundLabel.Text =
            fastest.ToString(@"mm\:ss");

        SlowestRoundLabel.Text =
            slowest.ToString(@"mm\:ss");

        AverageRoundLabel.Text =
            average.ToString(@"mm\:ss");

        StreakLabel.Text =
            $"{longestStreak} جولات";

        DominantTeamLabel.Text =
            dominantTeam;

        HighestRoundLabel.Text =
            $"{highestPoints} نقطة";

        MomentumSheetRoot.IsVisible = true;

        await MomentumSheet.TranslateToAsync(
            0,
            0,
            250,
            Easing.CubicOut);

        momentumOpened = true;
    }


    // إغلاق لوحة الزخم
    async void OnMomentumOverlayTapped(
    object sender,
    TappedEventArgs e)
    {
        if (!momentumOpened)
            return;

        await MomentumSheet.TranslateToAsync(
            0,
            700,
            250,
            Easing.CubicIn);

        MomentumSheetRoot.IsVisible = false;

        momentumOpened = false;
    }
    async void OnIntegrityClicked(
        object sender,
        TappedEventArgs e)
    {
        await DisplayAlert(
            "قريباً",
            "ميزة سلامة المباراة قيد التطوير",
            "حسناً");
    }

    async void OnCertificateClicked(
      object sender,
      TappedEventArgs e)
    {
        if (match == null)
            return;

        await Navigation.PushAsync(
            new CertificatePage(match));
    }

    async void OnQrCodeClicked(
        object sender,
        TappedEventArgs e)
    {
        await DisplayAlert(
            "قريباً",
            "ميزة QR Code قيد التطوير",
            "حسناً");
    }
}
