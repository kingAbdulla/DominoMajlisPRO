using DominoMajlisPRO.GalleryEngine.Services;
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
        0);

    IReadOnlyList<RankingTeamCard> visibleTeams = Array.Empty<RankingTeamCard>();
    string sortMode = "XP";
    string searchText = "";
    bool showAllRows;
    int seasonSlideIndex;
    bool pageActive;

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
        base.OnDisappearing();
        pageActive = false;
        Unsubscribe();
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

    void BuildPage()
    {
        PageContent.Children.Clear();
        PageContent.Children.Add(BuildHeader());
        PageContent.Children.Add(BuildHero());
        PageContent.Children.Add(BuildSeasonInfo());

        if (snapshot.Champion != null)
            PageContent.Children.Add(BuildChampion(snapshot.Champion));

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
                    FontSize = 22,
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

    View BuildHero()
    {
        var slide = CurrentSlide();
        var root = Card("#E60A0D10", "#7E5A21", 0, 14);
        root.Padding = 0;
        root.HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 162 : 220;
        root.Content = new Grid
        {
            Children =
            {
                new Image
                {
                    Source = ResolveSource(slide.ImagePath, "season_reward_gold.png"),
                    Aspect = Aspect.AspectFill,
                    Opacity = 0.62
                },
                new BoxView { Color = Color.FromArgb("#BB000000") },
                new Grid
                {
                    Padding = 14,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Children =
                    {
                        new VerticalStackLayout
                        {
                            Spacing = 7,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                Label("موسم التصنيفات", 12, "#F2C46D", true),
                                Label(slide.Title, DeviceInfo.Idiom == DeviceIdiom.Phone ? 22 : 30, "#FFFFFF", true),
                                Label(slide.Subtitle, 12, "#DEC894", false),
                                BuildMiniProgress(snapshot.SeasonProgress, "#F2C46D", "#39270D", 8)
                            }
                        },
                        SetColumn(new VerticalStackLayout
                        {
                            Spacing = 4,
                            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 86 : 116,
                            HorizontalOptions = LayoutOptions.End,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Image { Source = "season_reward_gold.png", WidthRequest = 62, HeightRequest = 62, HorizontalOptions = LayoutOptions.Center },
                                Label($"{snapshot.SeasonDaysLeft} يوم", 13, "#F2C46D", true, TextAlignment.Center),
                                Label("نهاية الموسم", 10, "#E7D8B0", false, TextAlignment.Center)
                            }
                        }, 1)
                    }
                }
            }
        };
        return root;
    }

    View BuildSeasonInfo()
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

        grid.Children.Add(InfoPill("إجمالي XP", snapshot.TotalXp.ToString("N0"), "xp_gold.png", 0));
        grid.Children.Add(InfoPill("الثقة", $"{snapshot.AverageTrust}%", "trust_gold.png", 1));
        grid.Children.Add(InfoPill("المباريات", snapshot.TotalMatches.ToString("N0"), "calendar_gold.png", 2));
        return grid;
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

        var emblem = TeamEmblem(card, DeviceInfo.Idiom == DeviceIdiom.Phone ? 116 : 154);
        Grid.SetColumn(emblem, 0);
        grid.Children.Add(emblem);

        var text = new VerticalStackLayout
        {
            Spacing = 7,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label("البطل الحالي", 12, "#F2C46D", true),
                Label(card.Team.TeamName, 24, "#F8D47B", true),
                Label(card.PlayersText, 12, "#FFFFFF", false),
                RankBadge(card.Rank, 48),
                BuildRankProgress(card),
                Label($"{card.Team.XP:N0} XP", 13, "#FFFFFF", true)
            }
        };
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
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
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 180 : 230
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

        var height = place == 1 ? 154 : 132;
        var item = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            VerticalOptions = LayoutOptions.End
        };
        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.End,
            Children =
            {
                RankBadge(card.Rank, place == 1 ? 48 : 38),
                Label(card.Team.TeamName, place == 1 ? 13 : 11, "#FFFFFF", true, TextAlignment.Center),
                Label($"{card.Team.XP:N0} XP", 10, "#F2C46D", true, TextAlignment.Center)
            }
        };
        item.Children.Add(stack);

        var baseGrid = new Grid
        {
            HeightRequest = place == 1 ? 54 : 42,
            Children =
            {
                new Image { Source = "pos.png", Aspect = Aspect.Fill, Opacity = place == 1 ? 1 : 0.82 },
                Label(place.ToString(), 13, "#0A0A0A", true, TextAlignment.Center)
            }
        };
        Grid.SetRow(baseGrid, 1);
        item.Children.Add(baseGrid);
        item.HeightRequest = height;
        Grid.SetColumn(item, column);
        grid.Children.Add(item);
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

        var tableTeams = visibleTeams.Skip(3).ToList();
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
                new ColumnDefinition { Width = 32 },
                new ColumnDefinition { Width = 54 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 58 },
                new ColumnDefinition { Width = 46 }
            },
            ColumnSpacing = 8,
            HeightRequest = 78
        };

        grid.Children.Add(Label(card.Position.ToString(), 18, card.Position <= 3 ? "#F2C46D" : "#FFFFFF", true, TextAlignment.Center));

        var emblem = TeamEmblem(card, 48);
        Grid.SetColumn(emblem, 1);
        grid.Children.Add(emblem);

        var info = new VerticalStackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label(card.Team.TeamName, 14, "#F6D17D", true),
                Label(card.PlayersText, 10, "#D8D0C2", false),
                BuildRankProgress(card)
            }
        };
        Grid.SetColumn(info, 2);
        grid.Children.Add(info);

        var xp = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label(card.Team.XP.ToString("N0"), 12, "#FFFFFF", true, TextAlignment.Center),
                TrustRing(card.Team.TrustScore, 34)
            }
        };
        Grid.SetColumn(xp, 3);
        grid.Children.Add(xp);

        var trend = new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                Label(card.IsTrendUp ? "↑" : card.IsTrendDown ? "↓" : "-", 18, ToHex(card.TrendColor), true, TextAlignment.Center),
                Label(card.TrendText, 11, ToHex(card.TrendColor), true, TextAlignment.Center),
                Label($"🔥 {card.Team.ConsecutiveWins}", 10, "#F2C46D", true, TextAlignment.Center)
            }
        };
        Grid.SetColumn(trend, 4);
        grid.Children.Add(trend);
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

    View BuildRankProgress(RankingTeamCard card)
    {
        var root = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 22 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 22 }
            },
            ColumnSpacing = 5,
            HeightRequest = 24
        };
        root.Children.Add(new Image { Source = card.Rank.Icon, WidthRequest = 22, HeightRequest = 22 });
        var bar = new Grid { HeightRequest = 10, VerticalOptions = LayoutOptions.Center };
        bar.Children.Add(new Border { BackgroundColor = Color.FromArgb("#2B2519"), StrokeShape = new RoundRectangle { CornerRadius = 6 } });
        bar.Children.Add(new ProgressBar
        {
            Progress = card.Rank.Progress,
            ProgressColor = Color.FromArgb("#F2C46D"),
            BackgroundColor = Colors.Transparent,
            HeightRequest = 10,
            VerticalOptions = LayoutOptions.Center
        });
        bar.Children.Add(Label($"{card.Rank.Progress:P0}", 8, "#FFFFFF", true, TextAlignment.Center));
        root.Children.Add(bar);
        Grid.SetColumn(bar, 1);
        var nextIcon = new Image { Source = RankingsPageEngine.ResolveRankIcon(RankingService.GetNextRankName(card.Team.XP)), WidthRequest = 22, HeightRequest = 22 };
        Grid.SetColumn(nextIcon, 2);
        root.Children.Add(nextIcon);
        return root;
    }

    View RankBadge(RankingRankInfo rank, double iconSize)
    {
        var grid = new Grid { WidthRequest = iconSize + 22, HeightRequest = iconSize + 20 };
        grid.Children.Add(new Image { Source = rank.Icon, WidthRequest = iconSize, HeightRequest = iconSize, HorizontalOptions = LayoutOptions.Center });
        if (!string.IsNullOrWhiteSpace(rank.Tier))
        {
            grid.Children.Add(new Border
            {
                WidthRequest = 24,
                HeightRequest = 24,
                BackgroundColor = Color.FromArgb("#11100B"),
                Stroke = Color.FromArgb("#F2C46D"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Content = Label(rank.Tier, 9, "#F2C46D", true, TextAlignment.Center)
            });
        }
        return grid;
    }

    View TeamEmblem(RankingTeamCard card, double size)
    {
        var grid = new Grid { WidthRequest = size, HeightRequest = size };
        grid.Children.Add(new Border
        {
            BackgroundColor = SafeColor(card.Identity.EmblemBackgroundSource, "#090909"),
            StrokeShape = new RoundRectangle { CornerRadius = size / 5 },
            StrokeThickness = 0
        });
        if (!string.IsNullOrWhiteSpace(card.Identity.EmblemBackgroundSource) &&
            !card.Identity.EmblemBackgroundSource.StartsWith('#') &&
            !string.Equals(card.Identity.EmblemBackgroundSource, "Transparent", StringComparison.OrdinalIgnoreCase))
        {
            grid.Children.Add(new Image
            {
                Source = ResolveSource(card.Identity.EmblemBackgroundSource, "ss.png"),
                Aspect = Aspect.AspectFill,
                Opacity = 0.72
            });
        }
        var effectSlot = new Image { Aspect = Aspect.AspectFit };
        grid.Children.Add(effectSlot);
        var emblem = new Image
        {
            Source = ResolveSource(card.Identity.EmblemImagePath, "shield_3d.png"),
            Aspect = Aspect.AspectFit,
            Margin = new Thickness(size * 0.09)
        };
        grid.Children.Add(emblem);
        _ = TeamEffectEngine.ApplyAroundAsync(emblem, card.Team.TeamId, 1.04, lightweight: true);
        return grid;
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

    void StartSeasonSlider()
    {
        Dispatcher.StartTimer(TimeSpan.FromSeconds(5), () =>
        {
            if (!pageActive || snapshot.SeasonSlides.Count <= 1)
                return pageActive;
            seasonSlideIndex = (seasonSlideIndex + 1) % snapshot.SeasonSlides.Count;
            BuildPage();
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
        await NavigateAsync(new PlayerStatisticsPage());

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
