using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Admin.Services;
using DominoMajlisPRO.GalleryEngine.Components;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class RankingsPage : ContentPage
{
    RankingsPageSnapshot snapshot = new(
        Array.Empty<RankingTeamCard>(),
        Array.Empty<RankingTeamCard>(),
        null,
        Array.Empty<RankingSeasonSlide>(),
        0,
        0,
        0,
        0,
        0,
        1,
        null);

    IReadOnlyList<RankingTeamCard> visibleTeams = Array.Empty<RankingTeamCard>();
    string sortMode = "XP";
    string searchText = string.Empty;
    bool showAllRows;
    int seasonSlideIndex;
    bool pageActive;
    bool seasonSliderRunning;
    Image? heroSlideImage;

    public RankingsPage()
    {
        InitializeComponent();
        Title = "التصنيفات";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        pageActive = true;
        Subscribe();
        await LoadAsync();
        StartSeasonSlider();
    }

    protected override void OnDisappearing()
    {
        pageActive = false;
        Unsubscribe();
        base.OnDisappearing();
    }

    void Subscribe()
    {
        Unsubscribe();

        AppEvents.RankingsChanged += OnDataChanged;
        AppEvents.TeamsChanged += OnDataChanged;
        AppEvents.MatchesChanged += OnDataChanged;
        AppEvents.TeamStatsChanged += OnDataChanged;
        AppEvents.SeasonChanged += OnDataChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged += OnTeamAssetsChanged;
        AppEvents.StoreProgressChanged += OnStoreChanged;
        AppEvents.WalletChanged += OnStoreChanged;
        AppEvents.RankRewardGranted += OnTeamAssetsChanged;
        CurrentSeasonAdminService.PublishedChanged += OnSeasonPublishedChanged;
    }

    void Unsubscribe()
    {
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.TeamsChanged -= OnDataChanged;
        AppEvents.MatchesChanged -= OnDataChanged;
        AppEvents.TeamStatsChanged -= OnDataChanged;
        AppEvents.SeasonChanged -= OnDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged -= OnTeamAssetsChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;
        AppEvents.WalletChanged -= OnStoreChanged;
        AppEvents.RankRewardGranted -= OnTeamAssetsChanged;
        CurrentSeasonAdminService.PublishedChanged -= OnSeasonPublishedChanged;
    }

    async void OnDataChanged() => await LoadAsync();
    async void OnTeamAssetsChanged(string teamId) => await LoadAsync();
    async void OnStoreChanged(string playerId) => await LoadAsync();
    async void OnSeasonPublishedChanged(CurrentSeasonRecord? record) => await LoadAsync();

    async Task LoadAsync()
    {
        snapshot = await RankingsPageEngine.BuildAsync();
        ApplyFilterAndSort();
        BuildPage();
    }

    void ApplyFilterAndSort()
    {
        var query = snapshot.Teams.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(card =>
                Contains(card.Team.TeamName, term) ||
                Contains(card.Team.Player1, term) ||
                Contains(card.Team.Player2, term));
        }

        query = sortMode switch
        {
            "Wins" => query.OrderByDescending(card => card.Team.Wins).ThenBy(card => card.Position),
            "Trust" => query.OrderByDescending(card => card.Team.TrustScore).ThenBy(card => card.Position),
            "Meles" => query.OrderByDescending(card => card.Team.MelesCount).ThenBy(card => card.Position),
            _ => query.OrderBy(card => card.Position)
        };

        visibleTeams = query.ToList();
    }

    static bool Contains(string? source, string term) =>
        (source ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase);

    static T SetColumn<T>(T view, int column) where T : View
    {
        Grid.SetColumn(view, column);
        return view;
    }

    static T SetRow<T>(T view, int row) where T : View
    {
        Grid.SetRow(view, row);
        return view;
    }

    static T SpanColumns<T>(T view, int span) where T : View
    {
        Grid.SetColumnSpan(view, span);
        return view;
    }

    void BuildPage()
    {
        PageContent.Children.Clear();
        PageContent.Children.Add(BuildHeader());
        PageContent.Children.Add(BuildHeroShowcase());
        PageContent.Children.Add(BuildTopThree());
        PageContent.Children.Add(BuildSearchAndFilters());
        PageContent.Children.Add(BuildTable());
    }

    View BuildHeader()
    {
        var root = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };

        root.Children.Add(IconButton("back_gold.png", OnBackClicked));

        var title = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = "rankings_gold.png", WidthRequest = 30, HeightRequest = 30 },
                new Label
                {
                    Text = "التصنيفات",
                    TextColor = Color.FromArgb("#F2C46D"),
                    FontSize = 24,
                    FontAttributes = FontAttributes.Bold,
                    VerticalTextAlignment = TextAlignment.Center
                }
            }
        };
        Grid.SetColumn(title, 1);
        root.Children.Add(title);

        var playerRankings = TextButton("تصنيفات اللاعبين", "playerstats_gold.png", OnPlayerRankingsClicked);
        Grid.SetColumn(playerRankings, 2);
        root.Children.Add(playerRankings);
        return root;
    }

    View BuildHeroShowcase()
    {
        var slide = CurrentSlide();
        var champion = snapshot.Champion;
        var isPhone = DeviceInfo.Idiom == DeviceIdiom.Phone;

        var root = Card("#E60A0D10", "#B37A25", 0, 16);
        root.Padding = 0;
        root.HeightRequest = isPhone ? 344 : 300;

        var stack = new Grid();
        heroSlideImage = new Image
        {
            Source = ResolveSource(slide.ImagePath, "season_reward_gold.png"),
            Aspect = Aspect.AspectFill,
     
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        stack.Children.Add(heroSlideImage);
        stack.Children.Add(new BoxView
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb("#D8050608"), 0f),
                    new GradientStop(Color.FromArgb("#78050608"), 0.48f),
                    new GradientStop(Color.FromArgb("#C2050608"), 1f)
                }
            }
        });

        var content = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Thickness(12, 10, 12, 10),
            RowSpacing = 4,
            ColumnSpacing = 8,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var title = Label("بطل الموسم الحالي", isPhone ? 15 : 18, "#FFFFFF", false, TextAlignment.Center);
        title.Margin = new Thickness(0, 0, 0, 2);
        Grid.SetColumnSpan(title, 3);
        content.Children.Add(title);

        if (champion != null)
        {
            var emblemSize = isPhone ? 126 : 150;
            var emblemHost = new Grid
            {
                WidthRequest = emblemSize,
                HeightRequest = emblemSize + 34,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            emblemHost.Children.Add(TeamEmblem(champion, emblemSize, false));
            emblemHost.Children.Add(new Border
            {
                WidthRequest = 30,
                HeightRequest = 30,
                BackgroundColor = Color.FromArgb("#D90A0B0D"),
                Stroke = Color.FromArgb("#F2C46D"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                Content = Label("1", 12, "#FFD98A", true, TextAlignment.Center)
            });
            Grid.SetRow(emblemHost, 1);
            Grid.SetColumn(emblemHost, 0);
            content.Children.Add(emblemHost);

            var players = new HorizontalStackLayout
            {
                FlowDirection = FlowDirection.LeftToRight,
                Spacing = isPhone ? 10 : 14,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    PlayerAvatarName(champion.Team.Player1Id, champion.Team.Player1, isPhone ? 34 : 40),
                    PlayerAvatarName(champion.Team.Player2Id, champion.Team.Player2, isPhone ? 34 : 40)
                }
            };

            var center = new VerticalStackLayout
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    TeamNamePlate(champion.Team.TeamId, champion.Team.TeamName, isPhone ? 38 : 46),
                    players,
                    CenteredRankTitle(champion.Rank, isPhone ? 54 : 64)
                }
            };
            Grid.SetRow(center, 1);
            Grid.SetColumn(center, 1);
            content.Children.Add(center);

            var progress = BuildHeroWideProgress(champion);
            Grid.SetRow(progress, 2);
            Grid.SetColumn(progress, 0);
            Grid.SetColumnSpan(progress, 2);
            content.Children.Add(progress);
        }
        else
        {
            var empty = new VerticalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    Label("موسم التصنيفات", 14, "#F2C46D", true, TextAlignment.Center),
                    Label(slide.Title, 24, "#FFFFFF", true, TextAlignment.Center),
                    Label(slide.Subtitle, 12, "#DEC894", false, TextAlignment.Center)
                }
            };
            Grid.SetRow(empty, 1);
            Grid.SetColumn(empty, 1);
            content.Children.Add(empty);
        }

        var reward = snapshot.ChampionNextReward;
        var seasonColumn = new VerticalStackLayout
        {
            Spacing = isPhone ? 10 : 14,
            WidthRequest = isPhone ? 150 : 180,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Border
                {
                    WidthRequest = 90,
                    HeightRequest = 86,
                    BackgroundColor = Color.FromArgb("#B3070809"),
                    Stroke = Color.FromArgb("#5E421B"),
                    StrokeShape = new RoundRectangle { CornerRadius = 14 },
                    Padding = new Thickness(5, 4),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 1,
                        Children =
                        {
                            Label($"الموسم {snapshot.SeasonNumber}", 12, "#F2C46D", true, TextAlignment.Center),
                            Label("ينتهي بعد", 9, "#DEC894", false, TextAlignment.Center),
                            Label($"{snapshot.SeasonDaysLeft}", 24, "#FFFFFF", true, TextAlignment.Center),
                            Label("يوم", 11, "#F2C46D", true, TextAlignment.Center)
                        }
                    }
                },
                Label("المكافآت القادمة", 13, "#FFFFFF", false, TextAlignment.Center),
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Image { Source = "gems.png", WidthRequest = 22, HeightRequest = 22 },
                        Label((reward?.GemsReward ?? 0).ToString("N0"), 13, "#FFFFFF", true),
                        new Image { Source = "coins.png", WidthRequest = 22, HeightRequest = 22 },
                        Label((reward?.CoinsReward ?? 0).ToString("N0"), 13, "#FFFFFF", true)
                    }
                },
                TextButton("عرض جميع الجوائز", "gift_gems.png", async (_, _) => await ShowSeasonPrizesAsync())
            }
        };
        Grid.SetRow(seasonColumn, 1);
        Grid.SetColumn(seasonColumn, 2);
        Grid.SetRowSpan(seasonColumn, 2);
        content.Children.Add(seasonColumn);

        stack.Children.Add(content);
        root.Content = stack;
        return root;
    }
    View BuildChampion(RankingTeamCard card)
    {
        var root = Card("#F00B0E10", "#9C6D24", 0, 14);
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) }
            },
            ColumnSpacing = 10
        };

        grid.Children.Add(SetColumn(TeamEmblem(card, DeviceInfo.Idiom == DeviceIdiom.Phone ? 116 : 154, true), 0));
        grid.Children.Add(SetColumn(new VerticalStackLayout
        {
            Spacing = 7,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label("البطل الحالي", 12, "#F2C46D", true),
                TeamNamePlate(card.Team.TeamId, card.Team.TeamName, 46),
                Label(card.PlayersText, 12, "#FFFFFF", false),
                RankTitle(card.Rank, 48),
                BuildRankProgress(card, true),
                Label($"{card.Team.XP:N0} XP", 13, "#FFFFFF", true)
            }
        }, 1));

        root.Content = grid;
        return root;
    }

    View PlayerAvatarName(string? playerId, string? playerName, double size)
    {
        var name = string.IsNullOrWhiteSpace(playerName) ? "-" : playerName.Trim();
        var label = Label(name, 10, "#FFFFFF", false, TextAlignment.Center);
        label.MaxLines = 1;

        return new VerticalStackLayout
        {
            Spacing = 3,
            WidthRequest = Math.Max(54, size + 20),
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                PlayerAvatar(playerId, name, size),
                label
            }
        };
    }

    View CenteredRankTitle(RankingRankInfo rank, double iconSize)
    {
        var rankLabel = string.IsNullOrWhiteSpace(rank.Tier) ? rank.BaseName : $"{rank.BaseName} {rank.Tier}";
        return new VerticalStackLayout
        {
            Spacing = 3,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = rank.Icon, WidthRequest = iconSize, HeightRequest = iconSize, HorizontalOptions = LayoutOptions.Center },
                Label(rankLabel, 14, "#FFFFFF", false, TextAlignment.Center)
            }
        };
    }

    View BuildHeroWideProgress(RankingTeamCard card)
    {
        var rank = card.Rank;
        var progress = Math.Clamp(rank.Progress, 0, 1);
        var nextName = RankingService.GetNextRankName(card.Team.XP);
        var nextIcon = RankingsPageEngine.ResolveRankIcon(nextName);
        var currentName = string.IsNullOrWhiteSpace(rank.Tier) ? rank.BaseName : $"{rank.BaseName} {rank.Tier}";
        var remainingXp = Math.Max(0, rank.NextXp - card.Team.XP);

        var bar = new Grid
        {
            HeightRequest = 22,
            MinimumWidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 235 : 420,
            HorizontalOptions = LayoutOptions.Fill
        };
        bar.Children.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#55100B04"),
            Stroke = Color.FromArgb("#7A5A24"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 11 }
        });
        var fill = new Grid
        {
            Padding = new Thickness(2),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(progress, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1 - progress, GridUnitType.Star) }
            }
        };
        fill.Children.Add(SetColumn(new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = Color.FromArgb("#6EF018")
        }, 0));
        bar.Children.Add(fill);
        bar.Children.Add(Label($"XP {progress:P0}", 12, "#050805", true, TextAlignment.Center));

        var row = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 6,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 0,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Image { Source = rank.Icon, WidthRequest = 28, HeightRequest = 28, HorizontalOptions = LayoutOptions.Center },
                        Label(currentName, 9, "#FFFFFF", false, TextAlignment.Center)
                    }
                },
                SetColumn(bar, 1),
                SetColumn(new VerticalStackLayout
                {
                    Spacing = 0,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Image { Source = nextIcon, WidthRequest = 28, HeightRequest = 28, HorizontalOptions = LayoutOptions.Center },
                        Label(nextName, 9, "#FFFFFF", false, TextAlignment.Center)
                    }
                }, 2)
            }
        };

        return new VerticalStackLayout
        {
            Spacing = 1,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                row,
                Label($"متبقٍ {remainingXp:N0} XP", 10, "#D8C08A", false, TextAlignment.Center)
            }
        };
    }

    View BuildTopThree()
    {
        var root = Card("#E6090C0F", "#7E5A21", 0, 12);
        root.Padding = new Thickness(0, 10, 0, 0);
        root.Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Label("أفضل 3 فرق", 22, "#F2C46D", false, TextAlignment.Center),
                BuildPodiumGrid()
            }
        };
        return root;
    }

    View BuildPodiumGrid()
    {
        var grid = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 0,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 300 : 370
        };

        AddPodium(grid, snapshot.TopThree.ElementAtOrDefault(2), 0, 3);
        AddPodium(grid, snapshot.TopThree.ElementAtOrDefault(0), 1, 1);
        AddPodium(grid, snapshot.TopThree.ElementAtOrDefault(1), 2, 2);
        return grid;
    }

    void AddPodium(Grid grid, RankingTeamCard? card, int column, int place)
    {
        if (card == null)
            return;

        var isChampion = place == 1;

        var item = new Grid
        {
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
            Margin = new Thickness(0),
            HeightRequest = 300
        };

        var background = new Image
        {
            Source = place switch
            {
                1 => "pos3.png",
                2 => "pos2.png",
                _ => "pos1.png"
            },
            Aspect = Aspect.AspectFill,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            ZIndex = 0
        };

        var teamName = TeamNamePlate(card.Team.TeamId, card.Team.TeamName, isChampion ? 38 : 34);

        var players = Label(card.PlayersText, 10, "#FFFFFF", false, TextAlignment.Center);
        players.MaxLines = 1;
        players.LineBreakMode = LineBreakMode.TailTruncation;

        var rankName = string.IsNullOrWhiteSpace(card.Rank.Tier)
            ? card.Rank.BaseName
            : $"{card.Rank.BaseName} {card.Rank.Tier}";

        var rankLabel = Label(rankName, 9, "#FFFFFF", false, TextAlignment.Center);
        rankLabel.MaxLines = 1;
        rankLabel.LineBreakMode = LineBreakMode.TailTruncation;

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(8, 10, 8, 8),
            Spacing = 4,
            VerticalOptions = LayoutOptions.Start,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
        {
            TeamEmblem(card, isChampion ? 76 : 66, false),
            teamName,
            players,
            new Image
            {
                Source = card.Rank.Icon,
                WidthRequest = isChampion ? 28 : 26,
                HeightRequest = isChampion ? 28 : 26,
                HorizontalOptions = LayoutOptions.Center
            },
            rankLabel
        }
        };

        var glass = new Border
        {
            ZIndex = 1,
            BackgroundColor = Color.FromArgb("#78000000"),
            Stroke = Color.FromArgb("#24FFFFFF"),
            StrokeThickness = 0.5,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },

            // أهم تعديل: رفع البطاقة للأعلى وتقليل النزول
            Margin = new Thickness(14, 24, 14, 78),

            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
            Content = content
        };

        var badge = new Border
        {
            ZIndex = 2,
            WidthRequest = 34,
            HeightRequest = 34,
            Margin = new Thickness(20, 24, 20, 0),
            BackgroundColor = Color.FromArgb("#E6000000"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 17 },
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Content = Label(place.ToString(), 15, "#FFFFFF", false, TextAlignment.Center)
        };

        item.Children.Add(background);
        item.Children.Add(glass);
        item.Children.Add(badge);

        grid.Children.Add(SetColumn(item, column));
    }

    View BuildSearchAndFilters()
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            RowSpacing = 8
        };

        var search = new SearchBar
        {
            Placeholder = "ابحث عن فريق...",
            Text = searchText,
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#8E846F"),
            BackgroundColor = Color.FromArgb("#0C0E10"),
            HeightRequest = 44
        };
        search.TextChanged += (_, e) =>
        {
            searchText = e.NewTextValue ?? string.Empty;
            ApplyFilterAndSort();
            BuildPage();
        };
        root.Children.Add(CardWrap(search));

        var filters = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.End };
        Grid.SetRow(filters, 1);
        filters.Children.Add(FilterChip("XP", "xp_gold.png", "XP"));
        filters.Children.Add(FilterChip("الفوز", "wins_gold.png", "Wins"));
        filters.Children.Add(FilterChip("الثقة", "trust_gold.png", "Trust"));
        filters.Children.Add(FilterChip("الملص", "fire_gold.png", "Meles"));
        root.Children.Add(filters);
        return root;
    }

    View BuildTable()
    {
        var root = Card("#F0090C0F", "#7E5A21", 0, 12);
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(Label("قائمة التصنيفات", 18, "#F2C46D", true));

        var tableTeams = visibleTeams.ToList();
        var rows = showAllRows ? tableTeams : tableTeams.Take(5).ToList();
        foreach (var card in rows)
            stack.Children.Add(TeamRow(card));

        if (tableTeams.Count > 5)
            stack.Children.Add(MoreButton());

        root.Content = stack;
        return root;
    }

    View TeamRow(RankingTeamCard card)
    {
        var row = Card("#CC080A0C", "#2C2418", 0, 10);
        row.Padding = new Thickness(8);
        row.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await ShowDetailsAsync(card))
        });

        // Dynamic column layout (star columns + truncation) so it stays inside
        // the frame on any screen size and never breaks with long/short names.
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 28 },                          // 1: position + delta
                new ColumnDefinition { Width = 44 },                          // 2: team emblem
                new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) }, // 3: name + players
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },    // 4: rank + progress
                new ColumnDefinition { Width = 40 },                          // 5: trust ring
                new ColumnDefinition { Width = 28 },                          // 6: meles
                new ColumnDefinition { Width = 14 }                           // 7: details arrow
            },
            ColumnSpacing = 5,
            MinimumHeightRequest = 84
        };

        // Column 1 : ranking position + position delta.
        grid.Children.Add(new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                Label(card.Position.ToString(), 17, card.Position <= 3 ? "#F2C46D" : "#FFFFFF", true, TextAlignment.Center),
                Label(card.IsTrendUp ? "▲" : card.IsTrendDown ? "▼" : "—", 9, ToHex(card.TrendColor), true, TextAlignment.Center)
            }
        });

        // Column 2 : team emblem (PNG only).
        grid.Children.Add(SetColumn(new Grid
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children = { TeamEmblem(card, 42, false) }
        }, 1));

        // Column 3 : team name + player names (vertical, truncation-safe).
        var teamName = TeamNamePlate(card.Team.TeamId, card.Team.TeamName, 34);
        var players = Label(card.PlayersText, 10, "#D8D0C2", false);
        players.MaxLines = 1;
        grid.Children.Add(SetColumn(new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children = { teamName, players }
        }, 2));

        // Column 4 : current rank emblem, name + tier, progress bar.
        grid.Children.Add(SetColumn(BuildRowRankColumn(card), 3));

        // Column 5 : trust ring.
        grid.Children.Add(SetColumn(new Grid
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children = { TrustRing(card.Team.TrustScore, 34) }
        }, 4));

        // Column 6 : meles / fire count.
        grid.Children.Add(SetColumn(new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                Label("🔥", 13, "#F2C46D", true, TextAlignment.Center),
                Label(Math.Max(0, card.Team.MelesCount).ToString(), 11, "#FFFFFF", true, TextAlignment.Center)
            }
        }, 5));

        // Column 7 : details arrow (RTL -> points left).
        grid.Children.Add(SetColumn(Label("‹", 20, "#F2C46D", true, TextAlignment.Center), 6));

        row.Content = grid;
        return row;
    }

    static View TeamNamePlate(string teamId, string teamName, double height) =>
        new RuntimeNamePlateView
        {
            OwnerKind = "Team",
            OwnerId = teamId,
            DisplayText = teamName,
            RenderingContext = GalleryEngine.Models.NameSurfaceRenderingContext.Rankings,
            HeightRequest = height,
            HorizontalOptions = LayoutOptions.Fill
        };

    // Compact rank column for a leaderboard row: rank emblem + name, XP,
    // and a slim gold progress bar with the percentage inside.
    View BuildRowRankColumn(RankingTeamCard card)
    {
        var rank = card.Rank;
        var rankName = string.IsNullOrWhiteSpace(rank.Tier) ? rank.BaseName : $"{rank.BaseName} {rank.Tier}";
        var nameLabel = Label(rankName, 11, "#F2C46D", true, TextAlignment.Center);
        nameLabel.MaxLines = 1;

        var head = new HorizontalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = rank.Icon, WidthRequest = 16, HeightRequest = 16 },
                nameLabel
            }
        };

        var progress = Math.Clamp(rank.Progress, 0, 1);
        var bar = new Grid { HeightRequest = 14, VerticalOptions = LayoutOptions.Center };
        bar.Children.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#3A2F1C"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 7 }
        });
        var fill = new Grid
        {
            Padding = new Thickness(1),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(progress, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1 - progress, GridUnitType.Star) }
            }
        };
        fill.Children.Add(SetColumn(new Border
        {
            BackgroundColor = Color.FromArgb("#F2C46D"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 7 }
        }, 0));
        bar.Children.Add(fill);
        bar.Children.Add(Label($"{progress:P0}", 8, "#1A1206", true, TextAlignment.Center));

        return new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                head,
                Label($"{card.Team.XP:N0} XP", 10, "#FFFFFF", true, TextAlignment.Center),
                bar
            }
        };
    }

    View MoreButton()
    {
        var label = Label(showAllRows ? "إخفاء" : "عرض المزيد", 14, "#FFF4CF", true, TextAlignment.Center);
        var button = Card("#8C5A1B", "#D6A642", 0, 10);
        button.Padding = new Thickness(12, 10);
        button.Content = label;
        button.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                showAllRows = !showAllRows;
                BuildPage();
            })
        });
        return button;
    }

    View BuildRankProgress(RankingTeamCard card, bool large)
    {
        var iconSize = large ? 24 : 18;
        var height = large ? 26 : 20;
        var root = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = iconSize },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = iconSize }
            },
            ColumnSpacing = 5,
            HeightRequest = height
        };
        root.Children.Add(new Image { Source = card.Rank.Icon, WidthRequest = iconSize, HeightRequest = iconSize });

        var bar = new Grid { HeightRequest = large ? 12 : 8, VerticalOptions = LayoutOptions.Center };
        bar.Children.Add(new Border { BackgroundColor = Color.FromArgb("#2B2519"), StrokeShape = new RoundRectangle { CornerRadius = 6 } });
        bar.Children.Add(new ProgressBar
        {
            Progress = Math.Clamp(card.Rank.Progress, 0, 1),
            ProgressColor = Color.FromArgb("#F2C46D"),
            BackgroundColor = Colors.Transparent,
            HeightRequest = large ? 12 : 8,
            VerticalOptions = LayoutOptions.Center
        });
        bar.Children.Add(Label($"{card.Rank.Progress:P0}", large ? 8 : 7, "#FFFFFF", true, TextAlignment.Center));
        root.Children.Add(SetColumn(bar, 1));

        var nextIcon = new Image
        {
            Source = RankingsPageEngine.ResolveRankIcon(RankingService.GetNextRankName(card.Team.XP)),
            WidthRequest = iconSize,
            HeightRequest = iconSize
        };
        root.Children.Add(SetColumn(nextIcon, 2));
        return root;
    }

    View RankTitle(RankingRankInfo rank, double iconSize)
    {
        var root = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                RankBadge(rank, iconSize),
                new VerticalStackLayout
                {
                    Spacing = 0,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        Label(rank.BaseName, 13, "#F2C46D", true, TextAlignment.Center),
                        Label(string.IsNullOrWhiteSpace(rank.Tier) ? rank.Name : rank.Tier, 12, "#FFFFFF", true, TextAlignment.Center)
                    }
                }
            }
        };
        return root;
    }

    View RankCompact(RankingRankInfo rank, double iconSize)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = rank.Icon, WidthRequest = iconSize, HeightRequest = iconSize },
                Label(string.IsNullOrWhiteSpace(rank.Tier) ? rank.BaseName : $"{rank.BaseName} {rank.Tier}", 10, "#F2C46D", true)
            }
        };
    }

    View RankBadge(RankingRankInfo rank, double iconSize)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            WidthRequest = iconSize + 22,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = rank.Icon, WidthRequest = iconSize, HeightRequest = iconSize, HorizontalOptions = LayoutOptions.Center }
            }
        };

        if (!string.IsNullOrWhiteSpace(rank.Tier))
        {
            stack.Children.Add(new Border
            {
                WidthRequest = 30,
                HeightRequest = 22,
                BackgroundColor = Color.FromArgb("#11100B"),
                Stroke = Color.FromArgb("#F2C46D"),
                StrokeShape = new RoundRectangle { CornerRadius = 11 },
                HorizontalOptions = LayoutOptions.Center,
                Content = Label(rank.Tier, 9, "#F2C46D", true, TextAlignment.Center)
            });
        }

        return stack;
    }

    View TeamEmblem(RankingTeamCard card, double size, bool useTeamBackground)
    {
        var grid = new Grid
        {
            WidthRequest = size,
            HeightRequest = size,
            Clip = new RoundRectangleGeometry { CornerRadius = size / 5, Rect = new Rect(0, 0, size, size) }
        };

        var fallbackColor = string.IsNullOrWhiteSpace(card.Identity.TeamColorHex) ? "#090909" : card.Identity.TeamColorHex;
        var backgroundSource = useTeamBackground ? card.Identity.EmblemBackgroundSource : null;
        grid.Children.Add(new Border
        {
            BackgroundColor = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = size / 5 },
            StrokeThickness = 0
        });

        if (!string.IsNullOrWhiteSpace(backgroundSource) &&
            !backgroundSource.StartsWith('#') &&
            !string.Equals(backgroundSource, "Transparent", StringComparison.OrdinalIgnoreCase))
        {
            grid.Children.Add(new Image
            {
                Source = ResolveSource(backgroundSource, "ss.png"),
                Aspect = Aspect.AspectFill,
                Opacity = 0.78
            });
        }

      

        var emblem = new Image
        {
            Source = ResolveSource(card.Identity.EmblemImagePath, "shield_3d.png"),
            Aspect = Aspect.AspectFit,
            Margin = new Thickness(size * 0.08)
        };
        grid.Children.Add(emblem);
        _ = TeamEffectEngine.ApplyAroundAsync(emblem, card.Team.TeamId, 1.04, lightweight: true);
        return grid;
    }

    View PlayerAvatar(string? playerId, string? playerName, double size)
    {
        var image = new Image { Source = "player_card.png", Aspect = Aspect.AspectFill };
        var avatar = new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            BackgroundColor = Color.FromArgb("#15110B"),
            Stroke = Color.FromArgb("#F2C46D"),
            StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
            Padding = 1,
            Content = image
        };
        _ = LoadAvatarAsync(image, playerId, playerName);
        return avatar;
    }

    static async Task LoadAvatarAsync(Image image, string? playerId, string? playerName)
    {
        try
        {
            PlayerProfileModel? player = null;
            if (!string.IsNullOrWhiteSpace(playerId))
                player = await PlayerProfileService.GetPlayerByIdAsync(playerId);
            if (player == null && !string.IsNullOrWhiteSpace(playerName))
                player = await PlayerProfileService.GetPlayerByNameAsync(playerName);
            if (player == null)
                return;
            await MainThread.InvokeOnMainThreadAsync(() => image.Source = PlayerProfileService.GetPlayerImageSource(player));
        }
        catch
        {
        }
    }

    View TrustRing(int trust, double size)
    {
        var color = trust >= 80 ? "#72E449" : trust >= 60 ? "#F2C46D" : "#FF5F57";
        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            Stroke = Color.FromArgb(color),
            StrokeThickness = 2,
            BackgroundColor = Color.FromArgb("#101010"),
            StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
            Content = Label($"{trust}%", size < 40 ? 9 : 12, color, true, TextAlignment.Center)
        };
    }

    async Task ShowDetailsAsync(RankingTeamCard card)
    {
        DetailsContent.Children.Clear();
        DetailsContent.Children.Add(new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children =
            {
                Label("تفاصيل الفريق", 18, "#F2C46D", true),
                IconButton("back_gold.png", async (_, _) => await CloseDetailsAsync(), 1)
            }
        });
        DetailsContent.Children.Add(BuildChampion(card));
        DetailsContent.Children.Add(BuildStatsGrid(card));

        var openStats = TextButton("عرض ملف الفريق", "analytics_gold.png", async (_, _) =>
        {
            await CloseDetailsAsync();
            await NavigateAsync(new TeamStatisticsPage());
        });
        DetailsContent.Children.Add(openStats);

        DetailsOverlay.IsVisible = true;
        DetailsOverlay.Opacity = 0;
        DetailsSheet.TranslationY = 80;
        await Task.WhenAll(
            DetailsOverlay.FadeTo(1, 160, Easing.CubicOut),
            DetailsSheet.TranslateTo(0, 0, 220, Easing.CubicOut));
    }

    View BuildStatsGrid(RankingTeamCard card)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 8
        };
        grid.Children.Add(InfoPill("الفوز", card.Team.Wins.ToString("N0"), "wins_gold.png", 0));
        grid.Children.Add(InfoPill("الخسارة", card.Team.Losses.ToString("N0"), "history_gold.png", 1));
        grid.Children.Add(InfoPill("Win Rate", $"{card.Team.WinRate}%", "trust_gold.png", 2));
        return grid;
    }

    async Task CloseDetailsAsync()
    {
        await Task.WhenAll(
            DetailsOverlay.FadeTo(0, 120, Easing.CubicIn),
            DetailsSheet.TranslateTo(0, 80, 140, Easing.CubicIn));
        DetailsOverlay.IsVisible = false;
    }

    async Task ShowSeasonPrizesAsync()
    {
        DetailsContent.Children.Clear();
        DetailsContent.Children.Add(Label("جوائز الرتب", 20, "#F2C46D", true, TextAlignment.Center));

        var next = snapshot.ChampionNextReward;
        if (next != null)
        {
            DetailsContent.Children.Add(Label("المكافأة التالية", 13, "#DEC894", true, TextAlignment.Center));
            DetailsContent.Children.Add(new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 8,
                Children =
                {
                    InfoPill("ذهب", next.CoinsReward.ToString("N0"), "coins.png", 0),
                    InfoPill("جواهر", next.GemsReward.ToString("N0"), "gems.png", 1)
                }
            });
        }

        foreach (var definition in RankRewardCatalog.All)
        {
            DetailsContent.Children.Add(BuildRewardRow(definition));
        }

        DetailsContent.Children.Add(TextButton("إغلاق", "back_gold.png", async (_, _) => await CloseDetailsAsync()));

        DetailsOverlay.IsVisible = true;
        DetailsOverlay.Opacity = 0;
        DetailsSheet.TranslationY = 80;
        await Task.WhenAll(
            DetailsOverlay.FadeTo(1, 160, Easing.CubicOut),
            DetailsSheet.TranslateTo(0, 0, 220, Easing.CubicOut));
    }

    void StartSeasonSlider()
    {
        if (seasonSliderRunning)
            return;

        seasonSliderRunning = true;
        Dispatcher.StartTimer(TimeSpan.FromSeconds(5), () =>
        {
            if (!pageActive)
            {
                seasonSliderRunning = false;
                return false;
            }

            if (snapshot.SeasonSlides.Count > 1)
            {
                seasonSlideIndex = (seasonSlideIndex + 1) % snapshot.SeasonSlides.Count;
                _ = AnimateSeasonSlideAsync();
            }

            return pageActive;
        });
    }

    // Visual-only background swap. Does not rebuild the page or move any card.
    async Task AnimateSeasonSlideAsync()
    {
        var image = heroSlideImage;
        if (image == null)
            return;

        var next = ResolveSource(CurrentSlide().ImagePath, "season_reward_gold.png");
        try
        {
            await image.FadeTo(0, 260, Easing.CubicInOut);
            image.Source = next;
            await image.FadeTo(1, 260, Easing.CubicInOut);
        }
        catch
        {
            image.Source = next;
            image.Opacity = 1;
        }
    }

    RankingSeasonSlide CurrentSlide() =>
        snapshot.SeasonSlides.Count == 0
            ? new RankingSeasonSlide("موسم الدومينو", "", "season_reward_gold.png", null, null)
            : snapshot.SeasonSlides[seasonSlideIndex % snapshot.SeasonSlides.Count];

    View FilterChip(string text, string icon, string mode)
    {
        var active = sortMode == mode;
        var chip = TextButton(text, icon, (_, _) =>
        {
            sortMode = mode;
            ApplyFilterAndSort();
            BuildPage();
        });
        if (chip is Border border)
        {
            border.BackgroundColor = Color.FromArgb(active ? "#3B2B10" : "#101214");
            border.Stroke = Color.FromArgb(active ? "#F2C46D" : "#4A3515");
        }
        return chip;
    }

    View BuildRewardRow(Models.RankRewardDefinition definition)
    {
        var row = Card("#CC0A0D10", "#3A2B14", 0, 10);
        row.Padding = new Thickness(10, 8);
        var rankLabel = string.IsNullOrWhiteSpace(definition.Tier)
            ? definition.RankName
            : $"{definition.RankName} {definition.Tier}";
        row.Content = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 30 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = RankingsPageEngine.ResolveRankIcon(definition.RankId), WidthRequest = 26, HeightRequest = 26 },
                SetColumn(Label(rankLabel, 13, "#F6D17D", true), 1),
                SetColumn(new HorizontalStackLayout
                {
                    Spacing = 6,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Image { Source = "coins.png", WidthRequest = 16, HeightRequest = 16 },
                        Label(definition.CoinsReward.ToString("N0"), 12, "#FFFFFF", true),
                        new Image { Source = "gems.png", WidthRequest = 16, HeightRequest = 16 },
                        Label(definition.GemsReward.ToString("N0"), 12, "#FFFFFF", true)
                    }
                }, 2)
            }
        };
        return row;
    }

    Border InfoPill(string title, string value, string icon, int column)
    {
        var pill = Card("#E60A0D10", "#4A3515", 0, 10);
        Grid.SetColumn(pill, column);
        pill.Padding = new Thickness(8);
        pill.Content = new VerticalStackLayout
        {
            Spacing = 3,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = icon, WidthRequest = 24, HeightRequest = 24 },
                Label(value, 14, "#FFFFFF", true, TextAlignment.Center),
                Label(title, 10, "#D7B66F", false, TextAlignment.Center)
            }
        };
        return pill;
    }

    Border CardWrap(View content)
    {
        var card = Card("#E60A0D10", "#4A3515", 0, 10);
        card.Padding = 0;
        card.Content = content;
        return card;
    }

    Border Card(string background, string stroke, double margin, double radius) =>
        new()
        {
            BackgroundColor = Color.FromArgb(background),
            Stroke = Color.FromArgb(stroke),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Padding = 12,
            Margin = new Thickness(margin)
        };

    Label Label(string text, double size, string color, bool bold, TextAlignment align = TextAlignment.Start) =>
        new()
        {
            Text = text,
            FontSize = size,
            TextColor = Color.FromArgb(color),
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = align,
            VerticalTextAlignment = TextAlignment.Center,
            MaxLines = 2,
            LineBreakMode = LineBreakMode.TailTruncation
        };

    View BuildMiniProgress(double progress, string color, string background, double height)
    {
        var grid = new Grid { HeightRequest = height };
        grid.Children.Add(new Border { BackgroundColor = Color.FromArgb(background), StrokeShape = new RoundRectangle { CornerRadius = height / 2 } });
        grid.Children.Add(new ProgressBar
        {
            Progress = Math.Clamp(progress, 0, 1),
            ProgressColor = Color.FromArgb(color),
            BackgroundColor = Colors.Transparent,
            HeightRequest = height
        });
        return grid;
    }

    Border IconButton(string image, EventHandler<TappedEventArgs> tapped, int column = 0)
    {
        var button = Card("#111315", "#6E4A18", 0, 12);
        Grid.SetColumn(button, column);
        button.Padding = 8;
        button.WidthRequest = 44;
        button.HeightRequest = 44;
        button.Content = new Image { Source = image, WidthRequest = 24, HeightRequest = 24 };
        var tap = new TapGestureRecognizer();
        tap.Tapped += tapped;
        button.GestureRecognizers.Add(tap);
        return button;
    }

    Border TextButton(string text, string image, EventHandler<TappedEventArgs> tapped)
    {
        var button = Card("#15110B", "#6E4A18", 0, 12);
        button.Padding = new Thickness(6, 8);
        button.HorizontalOptions = LayoutOptions.Fill;
        button.Content = new HorizontalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,

            Children =
            {
                new Image { Source = image, WidthRequest = 16, HeightRequest = 16 },
                Label(text, 11, "#F2C46D", true,TextAlignment.Center)
            }
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += tapped;
        button.GestureRecognizers.Add(tap);
        return button;
    }

    ImageSource ResolveSource(string? value, string fallback) =>
        InventoryDisplayResolver.ResolveImageSource(value, fallback);

    static Color SafeColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "Transparent", StringComparison.OrdinalIgnoreCase) ||
            !value.StartsWith('#'))
            return Color.FromArgb(fallback);
        return Color.FromArgb(value);
    }

    static string ToHex(Color color)
    {
        var red = (int)Math.Round(Math.Clamp(color.Red, 0, 1) * 255);
        var green = (int)Math.Round(Math.Clamp(color.Green, 0, 1) * 255);
        var blue = (int)Math.Round(Math.Clamp(color.Blue, 0, 1) * 255);
        return $"#{red:X2}{green:X2}{blue:X2}";
    }

    async void OnBackClicked(object sender, TappedEventArgs e) =>
        await Navigation.PopAsync();

    async void OnPlayerRankingsClicked(object sender, TappedEventArgs e) =>
        await NavigateAsync(new PlayerRankingsPage());

    async void OnFooterHomeTapped(object sender, TappedEventArgs e) =>
        await NavigateAsync(new DominoMajlisPRO.MainPage());

    async void OnFooterCreateTapped(object sender, TappedEventArgs e) =>
        await NavigateAsync(new CreateTeamPage());

    async void OnFooterRankingsTapped(object sender, TappedEventArgs e) =>
        await MainScroll.ScrollToAsync(0, 0, true);

    async void OnFooterStoreTapped(object sender, TappedEventArgs e) =>
        await NavigateAsync(new DominoMajlisPRO.GalleryEngine.Pages.GalleryPage());

    async Task NavigateAsync(Page page)
    {
        await this.FadeTo(0.9, 90, Easing.CubicOut);
        await Navigation.PushAsync(page, true);
        Opacity = 1;
    }
}
