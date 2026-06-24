using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;
namespace DominoMajlisPRO.Pages;

public partial class RankingsPage : ContentPage
{
    List<TeamProfileModel> allTeams =
        new();

    List<TeamProfileModel> filteredTeams =
        new();

    IReadOnlyDictionary<string, TeamIdentityModel> teamIdentities =
        new Dictionary<string, TeamIdentityModel>(
            StringComparer.OrdinalIgnoreCase);
    
    // VisualEventBus subscription tokens
    IDisposable? teamEmblemChangedSubscription;
    IDisposable? teamColorChangedSubscription;
    IDisposable? teamEffectChangedSubscription;
    IDisposable? teamEmblemBackgroundChangedSubscription;
    IDisposable? playerAvatarChangedSubscription;
    IDisposable? playerProfileBackgroundChangedSubscription;
    IDisposable? playerFrameChangedSubscription;
    IDisposable? playerEffectChangedSubscription;

    public RankingsPage()
    {
        InitializeComponent();

        DeleteAllButton.Clicked +=
            OnDeleteAllClicked;

        SetFilterActive(AllFilter);

        LoadRankings();

    }

    // =========================
    // LOAD
    // =========================

    async void LoadRankings()
    {
        allTeams =
            await RankingService.LoadTeamsAsync();
        teamIdentities = await TeamIdentityResolver.ResolveManyAsync(
            allTeams.Select(team => team.TeamId));
        SeasonManager.EnsureSeason(allTeams);
        var players =
    await PlayerProfileService.LoadPlayersAsync();

        foreach (var team in allTeams)
        {
            BadgeEngine.UpdateSpecialHonors(
                team,
                players);
        }
        await RankingService.SaveTeamsAsync(allTeams);
        UpdateSeasonHeader();
        foreach (var team in allTeams)
        {
            if (string.IsNullOrWhiteSpace(team.Rank))
            {
                team.Rank =
                    RankingService.GetRankFromXP(team.XP);
            }
        }

        BadgeEngine.UpdateAllTeamsBadges(allTeams);
        await RankingService.SaveTeamsAsync(allTeams);
        filteredTeams =
            allTeams
            .OrderByDescending(x => x.XP)
            .ToList();

        BuildPage(filteredTeams);
    }

    // =========================
    // BUILD PAGE
    // =========================

    void BuildPage(
        List<TeamProfileModel> teams)
    {
        ChampionContainer.Children.Clear();

        PodiumContainer.Children.Clear();

        LeaderboardTableContainer
            .Children
            .Clear();

        if (!teams.Any())
        {
            ShowEmptyState();
            return;
        }

        BuildChampionCard(
            teams.First());

        BuildTop3Podium(
            teams.Take(3).ToList());

        BuildLeaderboardTable(
            teams.Skip(3).ToList());
    }

    // =========================
    // EMPTY
    // =========================

    void ShowEmptyState()
    {
        ChampionContainer.Children.Add(

            new Label
            {
                Text =
                    "لا توجد بيانات حالياً",

                FontSize = 22,

                TextColor =
                    Colors.Gray,

                HorizontalOptions =
                    LayoutOptions.Center
            });
    }

    // =========================
    // SEARCH
    // =========================

    void OnSearchTextChanged(
      object sender,
      TextChangedEventArgs e)
    {
        string search =

            e.NewTextValue?
            .Trim()
            .ToLower()

            ?? "";

        if (string.IsNullOrWhiteSpace(
            search))
        {
            filteredTeams =
                allTeams
                .OrderByDescending(
                    x => x.XP)
                .ToList();

            BuildPage(
                filteredTeams);

            return;
        }

        filteredTeams =
            allTeams
            .Where(x =>

                 (x.TeamName ?? "").ToLower().Contains(search)
                   ||
                 (x.Player1 ?? "").ToLower().Contains(search)
                   ||
                 (x.Player2 ?? "").ToLower().Contains(search))

            .OrderByDescending(
                x => x.XP)

            .ToList();

        BuildPage(
            filteredTeams);
    }

    // =========================
    // FILTERS
    // =========================

    async void OnAllFilterClicked(
        object sender,
        TappedEventArgs e)
    {
        await AnimateFilter(AllFilter);

        ResetFilterStyles();

        SetFilterActive(AllFilter);

        filteredTeams =
            allTeams
            .OrderByDescending(x => x.XP)
            .ToList();

        BuildPage(filteredTeams);
    }


    async void OnTopXPFilterClicked(
        object sender,
        TappedEventArgs e)
    {
        await AnimateFilter(XPFilter);

        ResetFilterStyles();

        SetFilterActive(XPFilter);

        filteredTeams =
            allTeams
            .OrderByDescending(x => x.XP)
            .ToList();

        BuildPage(filteredTeams);
    }


    async void OnWinsFilterClicked(
        object sender,
        TappedEventArgs e)
    {
        await AnimateFilter(WinsFilter);

        ResetFilterStyles();

        SetFilterActive(WinsFilter);

        filteredTeams =
            allTeams
            .OrderByDescending(x => x.Wins)
            .ToList();

        BuildPage(filteredTeams);
    }


    async void OnMelesFilterClicked(
        object sender,
        TappedEventArgs e)
    {
        await AnimateFilter(MelesFilter);

        ResetFilterStyles();

        SetFilterActive(MelesFilter);

        filteredTeams =
            allTeams
            .OrderByDescending(x => x.MelesCount)
            .ToList();

        BuildPage(filteredTeams);
    }


    // =========================
    // BACK
    // =========================

    async void OnBackClicked(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    // =========================
    // CHAMPION CARD
    // =========================

    void BuildChampionCard(TeamProfileModel team)
    {
        if (string.IsNullOrWhiteSpace(team.Rank))
            team.Rank =
                RankingService.GetRankFromXP(team.XP);

        var theme =
            RankThemeService.GetTheme(team.Rank);

        int nextXP =
            RankingService.GetNextRankXP(team.XP);

        int remainingXP =
            RankingService.GetXPRemaining(team.XP);

        double progress =
            RankingService.GetProgressPercentage(team.XP);

        Border card =
            new Border
            {
                BackgroundColor =
                    Color.FromArgb("#081224"),

                Stroke =
                    Color.FromArgb("#223B6B"),

                StrokeThickness = 1.5,

                Padding = new Thickness(10),

                Margin = new Thickness(0, 0, 0, 12),

                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius = 24
                    }
            };

        Grid root =
            new Grid
            {
                ColumnSpacing = 10,

                ColumnDefinitions =
{
    new ColumnDefinition
    {
        Width = new GridLength(2.2, GridUnitType.Star)
    },

    new ColumnDefinition
    {
        Width = 1
    },

    new ColumnDefinition
    {
        Width = new GridLength(4.6, GridUnitType.Star)
    },

    new ColumnDefinition
    {
        Width = 1
    },

    new ColumnDefinition
    {
        Width = new GridLength(2.2, GridUnitType.Star)
    }
}

            };

        // =========================
        // TRUST
        // =========================

        VerticalStackLayout trustSection =
            new VerticalStackLayout
            {
                Spacing = 8,

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center
            };

        trustSection.Children.Add(
            new Label
            {
                Text = "مستوى الثقة",

                FontSize = 13,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Colors.White,

                HorizontalTextAlignment =
                    TextAlignment.Center
            });

        trustSection.Children.Add(
            CreateTrustRing(
                team.TrustScore,
                theme));

        Grid.SetColumn(trustSection, 0);

        root.Children.Add(trustSection);

        // =========================
        // LEFT DIVIDER
        // =========================

        BoxView leftDivider =
            new BoxView
            {
                WidthRequest = 1,

                Color =
                    Color.FromArgb("#233A63"),

                VerticalOptions =
                    LayoutOptions.Fill
            };

        Grid.SetColumn(leftDivider, 1);

        root.Children.Add(leftDivider);

        // =========================
        // CENTER
        // =========================

        VerticalStackLayout center =
            new VerticalStackLayout
            {
                Spacing = 8,

                VerticalOptions =
                    LayoutOptions.Center
            };

        center.Children.Add(
            new Label
            {
                Text = "ترتيبي الحالي",

                FontSize = 13,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb("#C9D5FF")
            });

        center.Children.Add(
            new Label
            {
                Text = team.Rank,

                FontSize = 22,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    theme.ProgressColor
            });

        center.Children.Add(
            new Label
            {
                Text =
                    $"{team.XP:N0} / {nextXP:N0} XP",

                FontSize = 16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Colors.White
            });

        Grid segmentedBar =
            new Grid
            {
                ColumnSpacing = 4,

                HeightRequest = 12
            };

        for (int i = 0; i < 10; i++)
        {
            segmentedBar.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Star
                });

            Border segment =
                new Border
                {
                    StrokeThickness = 0,

                    BackgroundColor =
                        i < (int)Math.Round(progress * 10)
                        ? theme.ProgressColor
                        : Color.FromArgb("#26324D"),

                    StrokeShape =
                        new RoundRectangle
                        {
                            CornerRadius = 4
                        }
                };

            Grid.SetColumn(segment, i);

            segmentedBar.Children.Add(segment);
        }

        center.Children.Add(segmentedBar);

        center.Children.Add(
            new Label
            {
                Text =
                    $"{remainingXP:N0} XP للوصول إلى {RankingService.GetNextRankName(team.XP)}",

                FontSize = 13,

                TextColor =
                    Color.FromArgb("#63B7FF")
            });

        Grid.SetColumn(center, 2);

        root.Children.Add(center);

        // =========================
        // RIGHT DIVIDER
        // =========================

        BoxView rightDivider =
            new BoxView
            {
                WidthRequest = 1,

                Color =
                    Color.FromArgb("#233A63"),

                VerticalOptions =
                    LayoutOptions.Fill
            };

        Grid.SetColumn(rightDivider, 3);

        root.Children.Add(rightDivider);

        // =========================
        // RANK ICON
        // =========================

        VerticalStackLayout rankSection =
            new VerticalStackLayout
            {
                Spacing = 6,

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center
            };

        rankSection.Children.Add(
            new Image
            {
                Source =
                    GetRankIcon(team.Rank),

                HeightRequest = 80,

                WidthRequest = 80,

                Aspect =
                    Aspect.AspectFit
            });

        rankSection.Children.Add(
            new Label
            {
                Text = team.TeamName,

                FontSize = 15,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Colors.White,

                MaxLines = 1,

                HorizontalTextAlignment =
                    TextAlignment.Center
            });

        Grid.SetColumn(rankSection, 4);

        root.Children.Add(rankSection);

        card.Content = root;

        ChampionContainer.Children.Add(card);
    }

    // Trust Ring
    View CreateTrustRing(
    int trustScore,
    RankTheme theme)
    {
        Color ringColor =
            trustScore >= 80
            ? Color.FromArgb("#32D74B")
            : trustScore >= 60
            ? Color.FromArgb("#FF9F0A")
            : Color.FromArgb("#FF453A");

        Grid root =
            new Grid
            {
                WidthRequest = 75,
                HeightRequest = 75
            };

        root.Children.Add(
            new GraphicsView
            {
                Drawable =
                    new TrustRingDrawable
                    {
                        Percentage = trustScore,
                        RingColor = ringColor
                    }
            });

        root.Children.Add(
            new VerticalStackLayout
            {
                Spacing = 2,

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center,

                Children =
                {
                new Label
                {
                    Text = $"{trustScore}%",

                    FontSize = 14,

                    FontAttributes =
                        FontAttributes.Bold,

                    TextColor =
                        Colors.White,

                    HorizontalTextAlignment =
                        TextAlignment.Center
                },

                new Label
                {
                    Text = "موثوق",

                    FontSize = 12,

                    FontAttributes =
                        FontAttributes.Bold,

                    TextColor =
                        ringColor,

                    HorizontalTextAlignment =
                        TextAlignment.Center
                }
                }
            });

        return root;
    }


    // =========================
    // TOP 3 PODIUM
    // =========================

    void BuildTop3Podium(
        List<TeamProfileModel> teams)
    {
        PodiumContainer.Children.Clear();

        if (teams.Count == 0)
            return;

        Grid podiumGrid =
            new Grid
            {
                ColumnDefinitions =
{
    new ColumnDefinition
    {
        Width =
            new GridLength(
                0.95,
                GridUnitType.Star)
    },

    new ColumnDefinition
    {
        Width =
            new GridLength(
                1.15,
                GridUnitType.Star)
    },

    new ColumnDefinition
    {
        Width =
            new GridLength(
                0.95,
                GridUnitType.Star)
    }
},

                Margin =
                    new Thickness(
                        0,
                        5,
                        0,
                        15)
            };

        if (teams.Count > 1)
        {
            var second =
                CreatePodiumCard(
                    teams[1],
                    2);

            Grid.SetColumn(
                second,
                2);

            podiumGrid.Children.Add(
                second);
        }

        if (teams.Count > 0)
        {
            var first =
                CreatePodiumCard(
                    teams[0],
                    1);

            Grid.SetColumn(
                first,
                1);

            podiumGrid.Children.Add(
                first);
        }

        if (teams.Count > 2)
        {
            var third =
                CreatePodiumCard(
                    teams[2],
                    3);

            Grid.SetColumn(
                third,
                0);

            podiumGrid.Children.Add(
                third);
        }

        PodiumContainer.Children.Add(
            podiumGrid);
    }

    // Podium Card
    Border CreatePodiumCard(
       TeamProfileModel team,
       int position)
    {
        bool isChampion =
            position == 1;

        Color borderColor =
            position switch
            {
                1 => Color.FromArgb("#D9A441"),
                2 => Color.FromArgb("#6AA5FF"),
                _ => Color.FromArgb("#D47B3C")
            };

        Border card =
            new Border
            {
                BackgroundColor =
                    Color.FromArgb(
                        isChampion
                        ? "#2A1B0F"
                        : "#14141A"),

                Stroke =
                    borderColor,

                StrokeThickness =
                    isChampion ? 2.5 : 1.5,

                Padding =
                    new Thickness(12),

                HeightRequest =
    position switch
    {
        1 => 300,
        2 => 250,
        _ => 240
    },


                Margin =
    position == 1
    ? new Thickness(2, 0, 2, 0)
    : new Thickness(6, 40, 6, 0),
                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius = 24
                    }
            };


        VerticalStackLayout layout =
            new VerticalStackLayout
            {
                Spacing = 4,

                VerticalOptions =
                    LayoutOptions.Center
            };

        layout.Children.Add(
    new Label
    {
        Text =
            position == 1 ? "🥇"
            : position == 2 ? "🥈"
            : "🥉",

        FontSize =
            position == 1 ? 34 : 28,

        HorizontalTextAlignment =
            TextAlignment.Center
    });


        layout.Children.Add(

            new Image
            {
                Source =
                    GetRankIcon(team.Rank),

                WidthRequest =
    position switch
    {
        1 => 90,
        2 => 70,
        _ => 65
    },

                HeightRequest =
    position switch
    {
        1 => 90,
        2 => 70,
        _ => 65
    },
                HorizontalOptions =
                    LayoutOptions.Center
            });

        layout.Children.Add(

            new Label
            {
                Text =
                    team.TeamName,

                FontSize =
                    isChampion ? 18 : 15,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Colors.White,

                HorizontalTextAlignment =
                    TextAlignment.Center,

                MaxLines = 1,

                LineBreakMode =
                    LineBreakMode.TailTruncation



            });

        layout.Children.Add(
       CreateBadgeIconsRow(team, position == 1 ? 30 : 24));

        layout.Children.Add(

    new Label
    {
        Text =
            $"{team.Player1} + {team.Player2}",

        FontSize = 11,

        TextColor =
            Colors.LightGray,

        HorizontalTextAlignment =
            TextAlignment.Center,

        MaxLines = 1,

        LineBreakMode =
            LineBreakMode.TailTruncation
    });
        layout.Children.Add(

           new Label
           {
               Text = team.Rank,

               LineBreakMode =
        LineBreakMode.NoWrap,

               MaxLines = 1,

               FontSize = 12,

               TextColor =
                    borderColor,

               HorizontalTextAlignment =
                    TextAlignment.Center
           });

        layout.Children.Add(

            new Label
            {
                Text =
                    $"{team.XP} XP",

                FontSize = 20,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Colors.White,

                HorizontalTextAlignment =
                    TextAlignment.Center
            });
        layout.Children.Add(
    new ProgressBar
    {
        Progress =
            RankingService
            .GetProgressPercentage(
                team.XP),

        HeightRequest = 12,
        WidthRequest = 170,

        ProgressColor =
            borderColor,

        BackgroundColor =
            Color.FromArgb("#2B3142")
    });

        card.Content = layout;


        var tap =
    new TapGestureRecognizer();

        tap.Tapped += async (s, e) =>
        {
            await card.ScaleTo(0.96, 80);

            await card.ScaleTo(1.00, 80);

            ShowTeamDetails(team);
        };

        card.GestureRecognizers.Add(tap);

        return card;
    }
    // =========================
    // TABLE HEADER
    // =========================

    void BuildLeaderboardHeader()
    {
        Border header =
            new Border
            {
                BackgroundColor =
                    Color.FromArgb("#10182F"),

                Stroke =
                    Color.FromArgb("#1E294A"),

                StrokeThickness = 1,

                Padding =
                    new Thickness(16, 12),

                Margin =
                    new Thickness(0, 0, 0, 6),

                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius = 14
                    }
            };

        Grid grid =
            new Grid
            {
                ColumnDefinitions =
                {
                new ColumnDefinition{Width=new GridLength(0.8,GridUnitType.Star)},
                new ColumnDefinition{Width=new GridLength(4.8,GridUnitType.Star)},
                new ColumnDefinition{Width=new GridLength(2.0,GridUnitType.Star)},
                new ColumnDefinition{Width=new GridLength(1.2,GridUnitType.Star)},
                new ColumnDefinition{Width=new GridLength(1.3,GridUnitType.Star)}
                }
            };

        AddHeader(grid, "#", 0);
        AddHeader(grid, "الفريق", 1);
        AddHeader(grid, "الرتبة", 2);
        AddHeader(grid, "XP", 3);
        AddHeader(grid, "الثقة", 4);

        header.Content = grid;

        LeaderboardTableContainer.Children.Add(header);
    }


    void AddHeader(
    Grid grid,
    string text,
    int column)
    {
        Label label =
            new Label
            {
                Text = text,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#8CA0D7"),
                HorizontalTextAlignment = TextAlignment.Center
            };

        Grid.SetColumn(label, column);

        grid.Children.Add(label);
    }



    Label CreateHeaderLabel(
    string text,
    int column)
    {
        Label label =
            new Label
            {
                Text = text,

                FontSize = 13,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb("#9F9FA9"),

                HorizontalTextAlignment =
                    TextAlignment.Center
            };

        Grid.SetColumn(
            label,
            column);

        return label;
    }




    // Delete
    async void OnDeleteAllClicked(
        object sender,
        EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "حذف جميع التصنيفات",
                "سيتم حذف جميع بيانات التصنيفات فقط.\n\nلن يتم حذف الفرق أو سجل المباريات.\n\nيمكنك إعادة بناء التصنيفات لاحقاً من الإعدادات.",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        try
        {
            await RankingService.SaveTeamsAsync(
                new List<TeamProfileModel>());

            allTeams.Clear();
            filteredTeams.Clear();

            ChampionContainer.Children.Clear();
            PodiumContainer.Children.Clear();
            LeaderboardTableContainer.Children.Clear();

            UpdateSeasonHeader();

            BuildPage(filteredTeams);

            AppEvents.RaiseDataChanged();

            await DisplayAlert(
                "تم",
                "تم حذف جميع التصنيفات بنجاح",
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
    
    // =========================
    // LEADERBOARD TABLE
    // =========================

    void BuildLeaderboardTable(
      List<TeamProfileModel> teams)
    {
        BuildLeaderboardHeader();

        int position = 4;

        foreach (var team in teams)
        {
            LeaderboardTableContainer.Children.Add(
                CreateProfessionalLeaderboardRow(
                    team,
                    position));

            position++;
        }
    }




    Border CreateProfessionalLeaderboardRow(
       TeamProfileModel team,
       int position)
    {
        Border row =
            new Border
            {
                BackgroundColor =
                    Color.FromArgb("#0F172A"),

                Stroke =
                    Color.FromArgb("#1E293B"),

                StrokeThickness = 1,

                Padding =
                    new Thickness(12, 18),

                Margin =
                    new Thickness(0, 0, 0, 6),

                StrokeShape =
                    new RoundRectangle
                    {
                        CornerRadius = 14
                    }
            };

        Grid grid =
            new Grid
            {
                ColumnDefinitions =
                {
                new ColumnDefinition{ Width = new GridLength(0.8, GridUnitType.Star) },
                new ColumnDefinition{ Width = 1 },

                new ColumnDefinition{ Width = new GridLength(3.8, GridUnitType.Star) },
                new ColumnDefinition{ Width = 1 },

                new ColumnDefinition{ Width = new GridLength(1.1, GridUnitType.Star) },
                new ColumnDefinition{ Width = 1 },

                new ColumnDefinition{ Width = new GridLength(1.4, GridUnitType.Star) },
                new ColumnDefinition{ Width = 1 },

                new ColumnDefinition{ Width = new GridLength(1.2, GridUnitType.Star) }
                }
            };

        Color dividerColor =
            Color.FromArgb("#243244");

        // Position

        Label positionLabel =
            new Label
            {
                Text = $"#{position}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

        Grid.SetColumn(positionLabel, 0);

        // Divider 1

        BoxView divider1 =
            new BoxView
            {
                WidthRequest = 1,
                Color = dividerColor,
                Opacity = 0.45
            };

        Grid.SetColumn(divider1, 1);

        // Team

        VerticalStackLayout teamInfo =
     new VerticalStackLayout
     {
         Spacing = 4,
         VerticalOptions = LayoutOptions.Center
     };

        teamInfo.Children.Add(
            new Label
            {
                Text = team.TeamName,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation,
                VerticalTextAlignment = TextAlignment.Center
            });

        teamInfo.Children.Add(
            CreateBadgeIconsRow(team, 30));

        Grid.SetColumn(teamInfo, 2);


        // Divider 2

        BoxView divider2 =
            new BoxView
            {
                WidthRequest = 1,
                Color = dividerColor,
                Opacity = 0.45
            };

        Grid.SetColumn(divider2, 3);

        // Rank Icon

        Image rankIcon =
            new Image
            {
                Source = GetRankIcon(team.Rank),
                WidthRequest = 28,
                HeightRequest = 28,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

        Grid.SetColumn(rankIcon, 4);

        // Divider 3

        BoxView divider3 =
            new BoxView
            {
                WidthRequest = 1,
                Color = dividerColor,
                Opacity = 0.45
            };

        Grid.SetColumn(divider3, 5);

        // XP

        Label xpLabel =
            new Label
            {
                Text = team.XP.ToString("N0"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#FFD700"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

        Grid.SetColumn(xpLabel, 6);

        // Divider 4

        BoxView divider4 =
            new BoxView
            {
                WidthRequest = 1,
                Color = dividerColor,
                Opacity = 0.45
            };

        Grid.SetColumn(divider4, 7);

        // Trust

        Label trustLabel =
            new Label
            {
                Text = $"{team.TrustScore}%",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor =
                    team.TrustScore >= 80
                    ? Color.FromArgb("#22C55E")
                    : team.TrustScore >= 60
                    ? Color.FromArgb("#F59E0B")
                    : Color.FromArgb("#EF4444"),

                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };

        Grid.SetColumn(trustLabel, 8);

        grid.Children.Add(positionLabel);
        grid.Children.Add(divider1);

        grid.Children.Add(teamInfo);
        grid.Children.Add(divider2);

        grid.Children.Add(rankIcon);
        grid.Children.Add(divider3);

        grid.Children.Add(xpLabel);
        grid.Children.Add(divider4);

        grid.Children.Add(trustLabel);

        row.Content = grid;

        var tap =
            new TapGestureRecognizer();

        tap.Tapped += (s, e) =>
        {
            ShowTeamDetails(team);
        };

        row.GestureRecognizers.Add(tap);

        return row;
    }

    // =========================
    // TEAM DETAILS SHEET
    // =========================

    void ShowTeamDetails(
        TeamProfileModel team)
    {
        if (string.IsNullOrWhiteSpace(team.Rank))
        {
            team.Rank =
                RankingService.GetRankFromXP(team.XP);
        }
        BadgeEngine.UpdateAllBadges(team, allTeams);
        SheetRankIcon.Source =
            GetRankIcon(team.Rank);

        SheetRankIcon.Source =
            GetRankIcon(team.Rank);

        SheetTeamName.Text =
            team.TeamName;

        SheetPlayers.Text =
            $"{team.Player1} • {team.Player2}";

        // Identity

        SheetTeamIdentity.Text =
            $"{team.TeamId} • {team.Player1Id} • {team.Player2Id}";

        // Quick Stats

        SheetXP.Text =
            team.XP.ToString("N0");

        SheetTrust.Text =
            $"{team.TrustScore}%";

        SheetMatches.Text =
            team.TotalMatches.ToString();

        // Rank Section

        string currentRank =
            string.IsNullOrWhiteSpace(team.Rank)
            ? RankingService.GetRankFromXP(team.XP)
            : team.Rank;

        SheetCurrentRank.Text =
            currentRank;

        SheetHighestRank.Text =
            string.IsNullOrWhiteSpace(team.HighestRank)
            ? currentRank
            : team.HighestRank;


        SheetCurrentRankIcon.Source =
    GetRankIcon(currentRank);

        SheetHighestRankIcon.Source =
            GetRankIcon(
                string.IsNullOrWhiteSpace(team.HighestRank)
                ? currentRank
                : team.HighestRank);

        // Win Rate Section

        SheetCurrentWR.Text =
            $"{team.WinRate}%";

        SheetPeakWR.Text =
            $"{team.PeakWinRate}%";




        int nextXP =
    RankingService.GetNextRankXP(
        team.XP);

        int remainingXP =
            RankingService.GetXPRemaining(
                team.XP);

        double progress =
            RankingService.GetProgressPercentage(
                team.XP);

        SheetRankProgressName.Text =
            team.Rank;

        SheetXPProgress.Text =
            $"{team.XP:N0} / {nextXP:N0} XP";

        SheetRankProgressBar.Progress =
            progress;

        SheetNextRankText.Text =
            $"متبقي {remainingXP:N0} XP للترقية إلى {RankingService.GetNextRankName(team.XP)}";
        // W / L / M
        SheetWins.Text =
            team.Wins.ToString();

        SheetLosses.Text =
            team.Losses.ToString();

        SheetMeles.Text =
            team.MelesCount.ToString();
        // Badges



        VerifiedIcon.Opacity =
            team.HasVerifiedBadge ? 1.0 : 0.25;

        RivalryIcon.Opacity =
            team.HasRivalryBadge ? 1.0 : 0.25;

        TrustIcon.Opacity =
            team.HasTrustBadge ? 1.0 : 0.25;

        ActivityIcon.Opacity =
            team.HasActivityBadge ? 1.0 : 0.25;

        SeasonIcon.Opacity =
            team.HasSeasonRewardBadge ? 1.0 : 0.25;

        MVPIcon.Opacity =
            team.HasMVPBadge ? 1.0 : 0.25;

        HallOfFameIcon.Opacity =
            team.HasHallOfFameBadge ? 1.0 : 0.25;

        BottomSheetOverlay.IsVisible =
            true;
    }

    // Close Sheet
    void OnCloseBottomSheetClicked(
    object sender,
    EventArgs e)
    {
        BottomSheetOverlay.IsVisible =
            false;
    }



    // =========================
    // BADGE ROW
    // =========================
    HorizontalStackLayout CreateBadgeIconsRow(TeamProfileModel team, double size = 38)
    {
        HorizontalStackLayout row =
            new HorizontalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center
            };

        void AddBadge(bool active, string icon)
        {
            if (!active)
                return;

            row.Children.Add(
                new Image
                {
                    Source = icon,
                    WidthRequest = size,
                    HeightRequest = size,
                    Aspect = Aspect.AspectFit
                });
        }
        AddBadge(team.IsDeveloper, "developer_gold.png");
        AddBadge(team.IsFounder, "founder_gold.png");
        AddBadge(team.HasChampionBadge, "champion_gold.png");
        AddBadge(team.HasVerifiedBadge, "verified_team_gold.png");
        AddBadge(team.HasTrustBadge, "trust_gold.png");
        AddBadge(team.HasActivityBadge, "activity_gold.png");
        AddBadge(team.HasRivalryBadge, "rivalry_gold.png");
        AddBadge(team.HasSeasonRewardBadge, "season_reward_gold.png");
        AddBadge(team.HasMVPBadge, "mvp_gold.png");
        AddBadge(team.HasHallOfFameBadge, "hall_of_fame_gold.png");

        return row;
    }


    // =========================
    // SEASON HEADER

    void UpdateSeasonHeader()
    {
        if (allTeams == null || allTeams.Count == 0)
        {
            SeasonTitleLabel.Text = "الموسم الحالي";
            SeasonCountdownLabel.Text = "لا توجد بيانات موسم حالياً";
            SeasonProgressPercentLabel.Text = "0%";
            SeasonProgressBar.Progress = 0;
            return;
        }

        var seasonTeam =
            allTeams
            .FirstOrDefault(x => x.CurrentSeasonId > 0)
            ?? allTeams.First();

        int seasonNumber =
            SeasonManager.GetCurrentSeasonNumber(allTeams);

        int daysRemaining =
            SeasonManager.GetDaysRemaining(seasonTeam);

        double progress =
            SeasonManager.GetSeasonProgress(seasonTeam);

        SeasonTitleLabel.Text =
            $"الموسم {seasonNumber}";

        SeasonCountdownLabel.Text =
            $"متبقي {daysRemaining} يوم على نهاية الموسم";

        SeasonProgressPercentLabel.Text =
            $"{progress * 100:0}%";

        SeasonProgressBar.Progress =
            progress;
    }
    // =========================
    // RANK ICON
    // =========================

    string GetRankIcon(string rank)
    {
        if (string.IsNullOrWhiteSpace(rank))
            return "unranked.png";

        if (rank.StartsWith("Bronze"))
            return "bronze.png";

        if (rank.StartsWith("Silver"))
            return "silver.png";

        if (rank.StartsWith("Gold"))
            return "gold.png";

        if (rank.StartsWith("Platinum"))
            return "platinum.png";

        if (rank.StartsWith("Diamond"))
            return "diamond.png";

        if (rank == "Majlis Master")
            return "majlis_master.png";

        if (rank == "Majlis Legend")
            return "majlis_legend.png";

        return "unranked.png";
    }

    // =========================
    //Disappearing
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Dispose VisualEventBus subscriptions
        teamEmblemChangedSubscription?.Dispose();
        teamColorChangedSubscription?.Dispose();
        teamEffectChangedSubscription?.Dispose();
        teamEmblemBackgroundChangedSubscription?.Dispose();
        playerAvatarChangedSubscription?.Dispose();
        playerProfileBackgroundChangedSubscription?.Dispose();
        playerFrameChangedSubscription?.Dispose();
        playerEffectChangedSubscription?.Dispose();

        AppEvents.RankingsChanged -= LoadRankings;
        AppEvents.PlayerProfileChanged -= LoadRankings;
        AppEvents.DataChanged -= LoadRankings;
        AppEvents.TeamsChanged -= LoadRankings;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.MatchesChanged -= LoadRankings;
    }
    // Appearing
    protected override void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.RankingsChanged -= LoadRankings;
        AppEvents.PlayerProfileChanged -= LoadRankings;
        AppEvents.DataChanged -= LoadRankings;
        AppEvents.TeamsChanged -= LoadRankings;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.MatchesChanged -= LoadRankings;

        AppEvents.RankingsChanged += LoadRankings;
        AppEvents.PlayerProfileChanged += LoadRankings;
        AppEvents.DataChanged += LoadRankings;
        AppEvents.TeamsChanged += LoadRankings;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        AppEvents.MatchesChanged += LoadRankings;
        
        // Subscribe to VisualEventBus identity events
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
        playerAvatarChangedSubscription = VisualEventBus.Subscribe(
            EventCategory.Player,
            OnPlayerAvatarChanged);
        playerProfileBackgroundChangedSubscription = VisualEventBus.Subscribe(
            EventCategory.Player,
            OnPlayerProfileBackgroundChanged);
        playerFrameChangedSubscription = VisualEventBus.Subscribe(
            EventCategory.Player,
            OnPlayerFrameChanged);
        playerEffectChangedSubscription = VisualEventBus.Subscribe(
            EventCategory.Player,
            OnPlayerEffectChanged);

        LoadRankings();
    }

    void OnTeamAssetsChanged(string teamId) => LoadRankings();

    // VisualEventBus identity event handler - reuse existing refresh path
    // Filtering is deferred by architecture to avoid false negatives
    void HandleVisualIdentityEvent(EventEntry eventEntry)
    {
        if (eventEntry.EventData == null)
            return;
        
        // Validate payload contains either TeamId or PlayerId
        bool hasTeamId = eventEntry.EventData.ContainsKey(VisualIdentityPayloadKeys.TeamId);
        bool hasPlayerId = eventEntry.EventData.ContainsKey(VisualIdentityPayloadKeys.PlayerId);
        
        if (!hasTeamId && !hasPlayerId)
            return;
        
        // Conservative behavior: always refresh on valid identity events
        LoadRankings();
    }

    // Individual event handlers
    void OnTeamEmblemChanged(EventEntry eventEntry) => HandleVisualIdentityEvent(eventEntry);
    void OnTeamColorChanged(EventEntry eventEntry) => HandleVisualIdentityEvent(eventEntry);
    void OnTeamEffectChanged(EventEntry eventEntry) => HandleVisualIdentityEvent(eventEntry);
    void OnTeamEmblemBackgroundChanged(EventEntry eventEntry) => HandleVisualIdentityEvent(eventEntry);

    void OnPlayerAvatarChanged(EventEntry eventEntry) => HandleVisualIdentityEvent(eventEntry);
    void OnPlayerProfileBackgroundChanged(EventEntry eventEntry) => HandleVisualIdentityEvent(eventEntry);
    void OnPlayerFrameChanged(EventEntry eventEntry) => HandleVisualIdentityEvent(eventEntry);
    void OnPlayerEffectChanged(EventEntry eventEntry) => HandleVisualIdentityEvent(eventEntry);
    // Reset filter styles
    void ResetFilterStyles()
    {
        SetFilterInactive(AllFilter);
        SetFilterInactive(XPFilter);
        SetFilterInactive(WinsFilter);
        SetFilterInactive(MelesFilter);
    }

    void SetFilterActive(Border filter)
    {
        filter.BackgroundColor =
            Color.FromArgb("#2A1B0F");

        filter.Stroke =
            Color.FromArgb("#D9A441");

        filter.StrokeThickness = 2;
    }

    void SetFilterInactive(Border filter)
    {
        filter.BackgroundColor =
            Color.FromArgb("#17171F");

        filter.Stroke =
            Color.FromArgb("#292938");

        filter.StrokeThickness = 1;
    }



    // Animation for filter taps
    async Task AnimateFilter(Border filter)
    {
        await filter.ScaleTo(0.94, 70);

        await filter.ScaleTo(1.00, 70);
    }


}
