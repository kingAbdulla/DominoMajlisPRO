using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Internals;
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
        0);

    IReadOnlyList<RankingTeamCard> visibleTeams = Array.Empty<RankingTeamCard>();
    string sortMode = "XP";
    string searchText = string.Empty;
    bool showAllRows;
    int seasonSlideIndex;
    bool pageActive;
    bool seasonSliderRunning;

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
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.TeamsChanged -= OnDataChanged;
        AppEvents.MatchesChanged -= OnDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged -= OnTeamAssetsChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;

        AppEvents.RankingsChanged += OnDataChanged;
        AppEvents.TeamsChanged += OnDataChanged;
        AppEvents.MatchesChanged += OnDataChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged += OnTeamAssetsChanged;
        AppEvents.StoreProgressChanged += OnStoreChanged;
    }

    void Unsubscribe()
    {
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.TeamsChanged -= OnDataChanged;
        AppEvents.MatchesChanged -= OnDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged -= OnTeamAssetsChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;
    }

    async void OnDataChanged() => await LoadAsync();
    async void OnTeamAssetsChanged(string teamId) => await LoadAsync();
    async void OnStoreChanged(string playerId) => await LoadAsync();

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
        root.HeightRequest = isPhone ? 348 : 320;

        var layout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        var heroGrid = new Grid
        {
            Padding = new Thickness(14, 14, 14, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1.02, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.38, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };

        var seasonBackground = new Image
        {
            Source = ResolveSource(slide.ImagePath, "season_reward_gold.png"),
            Aspect = Aspect.AspectFill,
            Opacity = 0.52
        };
        heroGrid.Children.Add(SpanColumns(seasonBackground, 3));
        heroGrid.Children.Add(SpanColumns(new BoxView { Color = Color.FromArgb("#B9000000") }, 3));

        if (champion != null)
        {
            var emblemSize = isPhone ? 146 : 174;
            var emblem = TeamEmblem(champion, emblemSize, true);
            Grid.SetColumn(emblem, 0);
            heroGrid.Children.Add(emblem);

            var playerAvatars = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    PlayerAvatar(champion.Team.Player1Id, champion.Team.Player1, 34),
                    Label("+", 16, "#F2C46D", true, TextAlignment.Center),
                    PlayerAvatar(champion.Team.Player2Id, champion.Team.Player2, 34)
                }
            };

            var championInfo = new VerticalStackLayout
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    Label("بطل الموسم الحالي", 12, "#F2C46D", true, TextAlignment.Center),
                    Label(champion.Team.TeamName, isPhone ? 25 : 30, "#F8D47B", true, TextAlignment.Center),
                    playerAvatars,
                    Label(champion.PlayersText, 11, "#FFFFFF", false, TextAlignment.Center),
                    RankTitle(champion.Rank, 44),
                    BuildRankProgress(champion, true),
                    Label($"{champion.Team.XP:N0} / {champion.Rank.NextXp:N0} XP", 12, "#FFFFFF", true, TextAlignment.Center)
                }
            };
            Grid.SetColumn(championInfo, 1);
            heroGrid.Children.Add(championInfo);
        }
        else
        {
            var empty = new VerticalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    Label("موسم التصنيفات", 14, "#F2C46D", true, TextAlignment.Center),
                    Label(slide.Title, 24, "#FFFFFF", true, TextAlignment.Center),
                    Label(slide.Subtitle, 12, "#DEC894", false, TextAlignment.Center),
                    BuildMiniProgress(snapshot.SeasonProgress, "#F2C46D", "#39270D", 8)
                }
            };
            Grid.SetColumnSpan(empty, 2);
            heroGrid.Children.Add(empty);
        }

        var seasonBox = new Border
        {
            WidthRequest = isPhone ? 86 : 126,
            BackgroundColor = Color.FromArgb("#AA070809"),
            Stroke = Color.FromArgb("#5E421B"),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(8),
            VerticalOptions = LayoutOptions.Start,
            Content = new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    Label("الموسم", 12, "#F2C46D", true, TextAlignment.Center),
                    Label($"{snapshot.SeasonDaysLeft}", 28, "#FFFFFF", true, TextAlignment.Center),
                    Label("يوم", 12, "#F2C46D", true, TextAlignment.Center)
                }
            }
        };
        Grid.SetColumn(seasonBox, 2);
        heroGrid.Children.Add(seasonBox);
        layout.Children.Add(heroGrid);

        var rewards = new Grid
        {
            Padding = new Thickness(12, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            BackgroundColor = Color.FromArgb("#8008090A")
        };
        rewards.Children.Add(new HorizontalStackLayout
        {
            Spacing = 10,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label("المكافأة التالية", 12, "#F2C46D", true),
                Label("25,000", 14, "#FFFFFF", true),
                new Image { Source = "diamond.png", WidthRequest = 20, HeightRequest = 20 },
                Label("500", 14, "#FFFFFF", true)
            }
        });
        rewards.Children.Add(SetColumn(TextButton("عرض جميع الجوائز", "gift_gold.png", async (_, _) => await ShowSeasonPrizesAsync()), 1));
        layout.Children.Add(SetRow(rewards, 1));

        root.Content = layout;
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
                Label(card.Team.TeamName, 24, "#F8D47B", true),
                Label(card.PlayersText, 12, "#FFFFFF", false),
                RankTitle(card.Rank, 48),
                BuildRankProgress(card, true),
                Label($"{card.Team.XP:N0} XP", 13, "#FFFFFF", true)
            }
        }, 1));

        root.Content = grid;
        return root;
    }

    View BuildTopThree()
    {
        var root = Card("#E6090C0F", "#7E5A21", 0, 12);
        root.Content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label("أفضل 3 فرق", 17, "#F2C46D", true, TextAlignment.Center),
                BuildPodiumGrid()
            }
        };
        return root;
    }

    View BuildPodiumGrid()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 6,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 315 : 360
        };

        AddPodium(grid, snapshot.TopThree.ElementAtOrDefault(1), 0, 2);
        AddPodium(grid, snapshot.TopThree.ElementAtOrDefault(0), 1, 1);
        AddPodium(grid, snapshot.TopThree.ElementAtOrDefault(2), 2, 3);
        return grid;
    }

    void AddPodium(Grid grid, RankingTeamCard? card, int column, int place)
    {
        if (card == null)
            return;

        var isChampion = place == 1;

        var item = new Grid
        {
            VerticalOptions = LayoutOptions.End,
            HorizontalOptions = LayoutOptions.Fill,
            HeightRequest = isChampion ? 290 : 270
        };

        var background = new Image
        {
            Source = place switch
            {
                1 => "pos1.png",
                2 => "pos2.png",
                _ => "pos3.png"
            },
            Aspect = Aspect.Fill,
            ZIndex = 0
        };

        var glass = new Border
        {
            ZIndex = 1,
            BackgroundColor = Color.FromArgb("#99000000"), // حوالي 60% شفافية
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 26
            },
            Margin = new Thickness(18, 22, 18, 58),
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        };

        var stack = new VerticalStackLayout
        {
            ZIndex = 2,
            Margin = new Thickness(0, 26, 0, 0),
            Padding = new Thickness(10, 0),
            Spacing = 6,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Start,
            Children =
        {
            TeamEmblem(card, isChampion ? 68 : 58, false),

            Label(
                card.Team.TeamName,
                isChampion ? 14 : 12,
                "#FFFFFF",
                true,
                TextAlignment.Center),

            Label(
                card.PlayersText,
                10,
                "#E8D9B5",
                false,
                TextAlignment.Center),

            RankCompact(
                card.Rank,
                isChampion ? 26 : 22)
        }
        };

        item.Children.Add(background);
        item.Children.Add(glass);
        item.Children.Add(stack);

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

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 30 },
                new ColumnDefinition { Width = 54 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 48 },
                new ColumnDefinition { Width = 42 }
            },
            ColumnSpacing = 7,
            MinimumHeightRequest = 84
        };

        grid.Children.Add(Label(card.Position.ToString(), 18, card.Position <= 3 ? "#F2C46D" : "#FFFFFF", true, TextAlignment.Center));
        grid.Children.Add(SetColumn(TeamEmblem(card, 50, true), 1));

        var info = new VerticalStackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label(card.Team.TeamName, 14, "#F6D17D", true),
                Label(card.PlayersText, 10, "#D8D0C2", false),
                RankCompact(card.Rank, 22),
                BuildRankProgress(card, false)
            }
        };
        grid.Children.Add(SetColumn(info, 2));

        var xp = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label(card.Team.XP.ToString("N0"), 11, "#FFFFFF", true, TextAlignment.Center),
                TrustRing(card.Team.TrustScore, 34)
            }
        };
        grid.Children.Add(SetColumn(xp, 3));

        var trend = new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label(card.IsTrendUp ? "↑" : card.IsTrendDown ? "↓" : "-", 18, ToHex(card.TrendColor), true, TextAlignment.Center),
                Label(card.TrendText, 10, ToHex(card.TrendColor), true, TextAlignment.Center),
                Label($"🔥 {card.Team.ConsecutiveWins}", 10, "#F2C46D", true, TextAlignment.Center)
            }
        };
        grid.Children.Add(SetColumn(trend, 4));
        row.Content = grid;
        return row;
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
        DetailsContent.Children.Add(Label("جوائز الموسم", 20, "#F2C46D", true, TextAlignment.Center));
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
                InfoPill("ذهب", "25,000", "gold.png", 0),
                InfoPill("جواهر", "500", "diamond.png", 1)
            }
        });
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
                BuildPage();
            }

            return pageActive;
        });
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
        button.Padding = new Thickness(10, 8);
        button.Content = new HorizontalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Image { Source = image, WidthRequest = 18, HeightRequest = 18 },
                Label(text, 12, "#F2C46D", true)
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
