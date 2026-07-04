using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace DominoMajlisPRO.Pages;

public sealed class TeamStatisticsPage : ContentPage
{
    TeamStatisticsSnapshot snapshot = new(Array.Empty<TeamStatisticsProfile>(), TeamStatisticsProfile.Empty);
    IReadOnlyList<TeamStatisticsProfile> visibleTeams = Array.Empty<TeamStatisticsProfile>();
    TeamStatisticsProfile selected = TeamStatisticsProfile.Empty;
    VerticalStackLayout content = new();
    string filterMode = "ط§ظ„ظƒظ„";
    string sortMode = "Legacy";
    string activeTab = "overview";
    bool showAllMatches;

    public TeamStatisticsPage()
    {
        Title = "ط¥ط­طµط§ط¦ظٹط§طھ ط§ظ„ظپط±ظ‚";
        BackgroundColor = Color.FromArgb(StatisticsDashboardUi.PageBackground);
        FlowDirection = FlowDirection.RightToLeft;
        BuildShell();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Subscribe();
        await LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Unsubscribe();
    }

    void Subscribe()
    {
        AppEvents.MatchesChanged -= OnDataChanged;
        AppEvents.TeamsChanged -= OnDataChanged;
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamIdentityChanged;
        AppEvents.TeamEffectChanged -= OnTeamIdentityChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;

        AppEvents.MatchesChanged += OnDataChanged;
        AppEvents.TeamsChanged += OnDataChanged;
        AppEvents.RankingsChanged += OnDataChanged;
        AppEvents.TeamAssetsChanged += OnTeamIdentityChanged;
        AppEvents.TeamEffectChanged += OnTeamIdentityChanged;
        AppEvents.StoreEconomyChanged += OnStoreChanged;
        AppEvents.StoreProgressChanged += OnStoreChanged;
    }

    void Unsubscribe()
    {
        AppEvents.MatchesChanged -= OnDataChanged;
        AppEvents.TeamsChanged -= OnDataChanged;
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamIdentityChanged;
        AppEvents.TeamEffectChanged -= OnTeamIdentityChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;
    }

    async void OnDataChanged()
    {
        HallStatisticsDashboardService.Invalidate();
        await MainThread.InvokeOnMainThreadAsync(async () => await LoadAsync(true));
    }

    void OnTeamIdentityChanged(string teamId) => OnDataChanged();
    void OnStoreChanged(string playerId) => OnDataChanged();

    void BuildShell()
    {
        content = new VerticalStackLayout
        {
            Padding = new Thickness(14, 10, 14, 16),
            Spacing = 10
        };

        Content = new ScrollView
        {
            Content = content
        };
    }

    async Task LoadAsync(bool force = false)
    {
        snapshot = await HallStatisticsDashboardService.LoadTeamSnapshotAsync(force);
        ApplyFilterAndSort(keepSelection: true);
        await RenderAsync();
    }

    void ApplyFilterAndSort(bool keepSelection)
    {
        IEnumerable<TeamStatisticsProfile> query = snapshot.Teams;

        query = filterMode switch
        {
            "Hall" => query.Where(team => team.HallEntries > 0),
            "ظ‚ط±ظٹط¨ ظ…ظ† ط§ظ„ط´ط±ظˆط·" => query.Where(team => team.Status.ColorHex == "#69D84F"),
            "طھط­طھ ط§ظ„ظ…ط±ط§ظ‚ط¨ط©" => query.Where(team => team.Status.ColorHex == "#F4B942"),
            "ط§ظ„ط£ط¹ظ„ظ‰ ط«ظ‚ط©" => query.Where(team => team.Trust >= 85),
            "ط¢ط®ط± 30 ظٹظˆظ…" => query.Where(team => team.Matches.Any(match => ResolveDate(match) >= DateTime.Now.AddDays(-30))),
            _ => query
        };

        query = sortMode switch
        {
            "Wins" => query.OrderByDescending(team => team.Wins),
            "Win Rate" => query.OrderByDescending(team => team.WinRate),
            "Trust" => query.OrderByDescending(team => team.Trust),
            "XP" => query.OrderByDescending(team => team.XP),
            "Matches" => query.OrderByDescending(team => team.TotalMatches),
            _ => query.OrderByDescending(team => team.Legacy)
        };

        visibleTeams = query.ToList();

        if (keepSelection && !string.IsNullOrWhiteSpace(selected.TeamId))
        {
            var same = visibleTeams.FirstOrDefault(team => string.Equals(team.TeamId, selected.TeamId, StringComparison.OrdinalIgnoreCase));
            if (same != null)
            {
                selected = same;
                return;
            }
        }

        selected = visibleTeams.FirstOrDefault() ?? snapshot.Selected;
    }

    async Task RenderAsync()
    {
        content.Children.Clear();
        content.Children.Add(CreateHeader());
        content.Children.Add(CreateToolbar());
        content.Children.Add(CreateTeamSelector());
        content.Children.Add(await CreateHeroAsync());
        content.Children.Add(CreateTabs());
        AddActiveTabContent();
    }

    void AddActiveTabContent()
    {
        if (activeTab == "performance")
        {
            content.Children.Add(CreateMetrics());
            content.Children.Add(CreateCharts());
            return;
        }

        if (activeTab == "seasons")
        {
            content.Children.Add(StatisticsDashboardUi.ChartCard("تقدم الموسم", selected.SeasonTrend, ChartKind.Line, Color.FromArgb("#F4B942"), $"{selected.SeasonTrend.LastOrDefault():0}"));
            content.Children.Add(StatisticsDashboardUi.ChartCard("تطور XP", selected.XpTrend, ChartKind.Area, Color.FromArgb("#A259FF"), selected.XP.ToString("N0")));
            return;
        }

        if (activeTab == "achievements")
        {
            content.Children.Add(StatisticsDashboardUi.MetricGrid(new[]
            {
                StatisticsDashboardUi.Metric("champion_gold.png", selected.Championships.ToString("N0"), "البطولات"),
                StatisticsDashboardUi.Metric("halloffame_gold.png", selected.HallEntries.ToString("N0"), "Hall Entries"),
                StatisticsDashboardUi.Metric("crown_3d.png", selected.HighestWinStreak.ToString("N0"), "أعلى سلسلة"),
                StatisticsDashboardUi.Metric("trust_gold.png", selected.Trust.ToString("N0"), "Trust Score")
            }));
            return;
        }

        if (activeTab == "matches")
        {
            content.Children.Add(CreateMatches());
            return;
        }

        content.Children.Add(CreateMetrics());
        content.Children.Add(CreateCharts());
        content.Children.Add(CreateMatches());
    }

    View CreateHeader()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 42 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 42 }
            },
            ColumnSpacing = 8
        };

        var help = StatisticsDashboardUi.CommandButton("?");
        help.WidthRequest = 38;
        help.Clicked += async (s, e) => await ShowStatusLegendAsync();
        grid.Add(help);

        var title = StatisticsDashboardUi.Label("ط¥ط­طµط§ط¦ظٹط§طھ ط§ظ„ظپط±ظ‚", 24, Color.FromArgb(StatisticsDashboardUi.Gold), true);
        Grid.SetColumn(title, 1);
        grid.Add(title);

        var back = StatisticsDashboardUi.CommandButton("â€¹");
        back.WidthRequest = 38;
        back.Clicked += async (s, e) => await Navigation.PopAsync();
        Grid.SetColumn(back, 2);
        grid.Add(back);

        return grid;
    }

    View CreateToolbar()
    {
        var row = new FlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            JustifyContent = FlexJustify.SpaceBetween,
            AlignItems = FlexAlignItems.Center
        };

        row.Children.Add(ToolButton("ط§ط®طھظٹط§ط± ظپط±ظٹظ‚", OnChooseTeamClicked));
        row.Children.Add(ToolButton("ط¨ط­ط«", OnSearchClicked));
        row.Children.Add(ToolButton($"ظپظ„طھط±: {filterMode}", OnFilterClicked));
        row.Children.Add(ToolButton($"طھط±طھظٹط¨: {sortMode}", OnSortClicked));
        row.Children.Add(ToolButton("طھط­ط¯ظٹط«", async () => await LoadAsync(true)));
        row.Children.Add(ToolButton("طھطµط¯ظٹط±", OnExportClicked));
        return row;
    }

    Button ToolButton(string text, Func<Task> action)
    {
        var button = StatisticsDashboardUi.CommandButton(text);
        button.Margin = new Thickness(2);
        button.Clicked += async (s, e) => await action();
        return button;
    }

    View CreateTeamSelector()
    {
        var strip = new HorizontalStackLayout
        {
            Spacing = 8,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Thickness(2, 0)
        };

        foreach (var team in visibleTeams.Take(24))
            strip.Children.Add(CreateTeamChip(team));

        return new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = strip
        };
    }

    View CreateTeamChip(TeamStatisticsProfile team)
    {
        bool active = string.Equals(team.TeamId, selected.TeamId, StringComparison.OrdinalIgnoreCase);
        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 116 : 150
        };
        stack.Children.Add(StatisticsDashboardUi.Label(team.TeamName, 12, active ? Colors.White : Color.FromArgb(StatisticsDashboardUi.Muted), true, TextAlignment.Center, 1));
        stack.Children.Add(StatisticsDashboardUi.Label($"{team.WinRate:0.#}% | {team.Legacy:N0}", 10, Color.FromArgb(StatisticsDashboardUi.Gold), false, TextAlignment.Center, 1));

        var card = StatisticsDashboardUi.Frame(stack, 14, active ? "#D4AE62" : "#5B3B18", active ? "#8F6730" : "#080808", 8);
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                selected = team;
                showAllMatches = false;
                await RenderAsync();
            })
        });
        return card;
    }

    async Task<View> CreateHeroAsync()
    {
        var identity = await TeamIdentityResolver.ResolveAsync(selected.TeamId);
        var emblem = InventoryDisplayResolver.ResolveImageSource(identity.EmblemImagePath, "shield_3d.png");
        var background = InventoryDisplayResolver.ResolveOptionalImageSource(identity.EmblemBackgroundSource);

        var imageHost = new Grid
        {
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 168,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 168,
            HorizontalOptions = LayoutOptions.Center,
            Clip = new RoundRectangleGeometry { CornerRadius = 26, Rect = new Rect(0, 0, DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 168, DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 168) }
        };

        if (background != null)
            imageHost.Add(new Image { Source = background, Aspect = Aspect.AspectFill, Opacity = 0.46 });

        imageHost.Add(new Image { Source = emblem, Aspect = Aspect.AspectFit });

        var info = new VerticalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        info.Children.Add(StatisticsDashboardUi.Label(selected.TeamName, DeviceInfo.Idiom == DeviceIdiom.Phone ? 23 : 30, Color.FromArgb(StatisticsDashboardUi.Gold), true, TextAlignment.End));
        info.Children.Add(StatisticsDashboardUi.Label(selected.LevelTitle, 14, Color.FromArgb(StatisticsDashboardUi.Gold), false, TextAlignment.End));
        info.Children.Add(ProgressLine($"Level {selected.Level}", selected.LevelProgress));
        info.Children.Add(CreateMiniStats());

        var status = new VerticalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        status.Children.Add(StatisticsDashboardUi.Label("ط­ط§ظ„ط© ط§ظ„ظپط±ظٹظ‚", 12, Color.FromArgb(StatisticsDashboardUi.Muted), false));
        status.Children.Add(StatisticsDashboardUi.StatusBadge(selected.Status));
        status.Children.Add(StatusRing(selected.Status));

        View layout;
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            layout = new VerticalStackLayout
            {
                Spacing = 10,
                Children = { imageHost, info, status }
            };
        }
        else
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 170 }
                },
                ColumnSpacing = 14
            };
            grid.Add(imageHost);
            Grid.SetColumn(info, 1);
            grid.Add(info);
            Grid.SetColumn(status, 2);
            grid.Add(status);
            layout = grid;
        }

        return StatisticsDashboardUi.Frame(layout, 20, "#8A5B27", "#080808", 10);
    }

    View CreateMiniStats()
    {
        var mini = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition(), new ColumnDefinition() }, ColumnSpacing = 8 };
        mini.Add(StatisticsDashboardUi.Metric("rankings_gold_icon.png", selected.Rank, "ط§ظ„ط±طھط¨ط© ط§ظ„ط­ط§ظ„ظٹط©"));
        var legacy = StatisticsDashboardUi.Metric("trophy_3d.png", selected.Legacy.ToString("N0"), "Legacy");
        Grid.SetColumn(legacy, 1);
        mini.Add(legacy);
        var trust = StatisticsDashboardUi.Metric("trust_gold.png", selected.Trust.ToString(), "Trust Score");
        Grid.SetColumn(trust, 2);
        mini.Add(trust);
        return mini;
    }

    View CreateTabs()
    {
        var grid = new Grid { ColumnSpacing = 0 };
        (string Key, string Title)[] tabs =
        {
            ("overview", "نظرة عامة"),
            ("performance", "الأداء"),
            ("achievements", "الإنجازات"),
            ("seasons", "المواسم"),
            ("matches", "المباريات")
        };

        for (int i = 0; i < tabs.Length; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = activeTab == tabs[i].Key;
            var tab = StatisticsDashboardUi.Frame(
                StatisticsDashboardUi.Label(tabs[i].Title, 12, isActive ? Colors.White : Color.FromArgb(StatisticsDashboardUi.Muted), isActive),
                12,
                "#5B3B18",
                isActive ? "#8F6730" : "#080808",
                8);
            string key = tabs[i].Key;
            tab.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    activeTab = key;
                    await RenderAsync();
                })
            });
            Grid.SetColumn(tab, i);
            grid.Add(tab);
        }
        return grid;
    }
    View CreateMetrics() =>
        StatisticsDashboardUi.MetricGrid(new[]
        {
            StatisticsDashboardUi.Metric("joystick_gold.png", selected.TotalMatches.ToString("N0"), "ط§ظ„ظ…ط¨ط§ط±ظٹط§طھ"),
            StatisticsDashboardUi.Metric("win_gold.png", selected.Wins.ToString("N0"), "ط§ظ„ظپظˆط²"),
            StatisticsDashboardUi.Metric("loss_gold.png", selected.Losses.ToString("N0"), "ط§ظ„ط®ط³ط§ط±ط©"),
            StatisticsDashboardUi.Metric("target_3d.png", $"{selected.WinRate:0.0}%", "ظ…ط¹ط¯ظ„ ط§ظ„ظپظˆط²"),
            StatisticsDashboardUi.Metric("crown_3d.png", selected.HighestWinStreak.ToString("N0"), "ط£ط¹ظ„ظ‰ ط³ظ„ط³ظ„ط© ظپظˆط²"),
            StatisticsDashboardUi.Metric("trophy_3d.png", selected.Legacy.ToString("N0"), "Legacy"),
            StatisticsDashboardUi.Metric("champion_gold.png", selected.Championships.ToString("N0"), "ط§ظ„ط¨ط·ظˆظ„ط§طھ"),
            StatisticsDashboardUi.Metric("halloffame_gold.png", selected.HallEntries.ToString("N0"), "Hall Entries"),
            StatisticsDashboardUi.Metric("xp_gold.png", selected.XP.ToString("N0"), "ط¥ط¬ظ…ط§ظ„ظٹ XP"),
            StatisticsDashboardUi.Metric("coin_gold.png", selected.Coins.ToString("N0"), "ط¥ط¬ظ…ط§ظ„ظٹ Coins")
        });

    View CreateCharts()
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        int columns = DeviceInfo.Idiom == DeviceIdiom.Phone ? 1 : 2;
        for (int i = 0; i < columns; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        var charts = new[]
        {
            StatisticsDashboardUi.ChartCard("نسبة الفوز في آخر 20 مباراة", selected.WinRateTrend, ChartKind.Line, Color.FromArgb("#69D84F"), $"{selected.WinRate:0.#}%"),
            StatisticsDashboardUi.ChartCard("تطور Legacy", selected.LegacyTrend, ChartKind.Area, Color.FromArgb("#D4AE62"), selected.Legacy.ToString("N0")),
            StatisticsDashboardUi.ChartCard("تطور XP", selected.XpTrend, ChartKind.Area, Color.FromArgb("#A259FF"), selected.XP.ToString("N0")),
            StatisticsDashboardUi.ChartCard("تقدم الموسم", selected.SeasonTrend, ChartKind.Line, Color.FromArgb("#F4B942"), $"{selected.SeasonTrend.LastOrDefault():0}")
        };

        for (int i = 0; i < charts.Length; i++)
        {
            int row = i / columns;
            while (grid.RowDefinitions.Count <= row)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(charts[i], i % columns);
            Grid.SetRow(charts[i], row);
            grid.Add(charts[i]);
        }

        return grid;
    }

    View CreateMatches()
    {
        var rows = (showAllMatches ? selected.RecentMatches : selected.RecentMatches.Take(5)).ToList();
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(StatisticsDashboardUi.Label(showAllMatches ? "ط§ظ„ظ…ط¨ط§ط±ظٹط§طھ" : "ط¢ط®ط± 5 ظ…ط¨ط§ط±ظٹط§طھ", 15, Color.FromArgb(StatisticsDashboardUi.Gold), true));
        foreach (var row in rows)
            stack.Children.Add(MatchRow(row));

        if (selected.RecentMatches.Count > 5)
        {
            var more = StatisticsDashboardUi.CommandButton(showAllMatches ? "ط¹ط±ط¶ ط£ظ‚ظ„" : "ط¹ط±ط¶ ط§ظ„ظ…ط²ظٹط¯");
            more.Clicked += async (s, e) =>
            {
                showAllMatches = !showAllMatches;
                await RenderAsync();
            };
            stack.Children.Add(more);
        }

        return StatisticsDashboardUi.Frame(stack, 16, "#5B3B18", "#090909", 8);
    }

    View MatchRow(StatisticsMatchRow row)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 70 },
                new ColumnDefinition { Width = 70 },
                new ColumnDefinition { Width = 96 },
                new ColumnDefinition { Width = 38 }
            },
            ColumnSpacing = 6
        };

        grid.Add(StatisticsDashboardUi.Label(row.OpponentOrTeam, 12, Colors.White, false, TextAlignment.End));
        AddCell(grid, row.Score, 1, Colors.White);
        AddCell(grid, row.Result, 2, ResultColor(row.Result));
        AddCell(grid, row.Date.ToString("dd/MM/yyyy"), 3, Color.FromArgb(StatisticsDashboardUi.Muted));
        AddCell(grid, "â€؛", 4, Color.FromArgb(StatisticsDashboardUi.Gold));
        return StatisticsDashboardUi.Frame(grid, 10, "#2D2415", "#101010", 8);
    }

    static void AddCell(Grid grid, string text, int column, Color color)
    {
        var label = StatisticsDashboardUi.Label(text, 12, color, false);
        Grid.SetColumn(label, column);
        grid.Add(label);
    }

    static Color ResultColor(string result)
    {
        if (result.Contains("ط®ط³ط§ط±ط©", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#FF3B30");
        if (result.Contains("طھط¹ط§ط¯ظ„", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#BFC3C7");
        return Color.FromArgb("#69D84F");
    }

    View ProgressLine(string title, double progress)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 86 }, new ColumnDefinition() }, ColumnSpacing = 8 };
        grid.Add(StatisticsDashboardUi.Label(title, 12, Colors.White, true, TextAlignment.End));
        var bar = new ProgressBar { Progress = Math.Clamp(progress, 0, 1), ProgressColor = Color.FromArgb(StatisticsDashboardUi.Gold), BackgroundColor = Color.FromArgb("#252525"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(bar, 1);
        grid.Add(bar);
        return grid;
    }

    View StatusRing(SafeStatusResult status) =>
        new GraphicsView
        {
            HeightRequest = 96,
            WidthRequest = 96,
            Drawable = new TrendChartDrawable { Values = new[] { status.Progress, 1 - status.Progress }, Kind = ChartKind.Donut, PrimaryColor = Color.FromArgb(status.ColorHex), CenterText = $"{status.Progress * 100:0}%" }
        };

    async Task OnChooseTeamClicked()
    {
        var names = visibleTeams.Select(team => team.TeamName).Take(80).ToArray();
        if (names.Length == 0)
            return;

        string choice = await DisplayActionSheet("ط§ط®طھظٹط§ط± ظپط±ظٹظ‚", "ط¥ظ„ط؛ط§ط،", null, names) ?? "";
        var team = visibleTeams.FirstOrDefault(item => item.TeamName == choice);
        if (team == null)
            return;

        selected = team;
        showAllMatches = false;
        await RenderAsync();
    }

    async Task OnSearchClicked()
    {
        string result = await DisplayPromptAsync("ط¨ط­ط«", "ط§ظƒطھط¨ ط§ط³ظ… ط§ظ„ظپط±ظٹظ‚", "ط¨ط­ط«", "ط¥ظ„ط؛ط§ط،") ?? "";
        if (string.IsNullOrWhiteSpace(result))
            return;

        var found = snapshot.Teams
            .Where(team => team.TeamName.Contains(result, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(team => team.Legacy)
            .FirstOrDefault();

        if (found != null)
        {
            selected = found;
            filterMode = "ط§ظ„ظƒظ„";
            ApplyFilterAndSort(keepSelection: true);
            await RenderAsync();
        }
    }

    async Task OnFilterClicked()
    {
        string action = await DisplayActionSheet("ظپظ„طھط±", "ط¥ظ„ط؛ط§ط،", null, "ط§ظ„ظƒظ„", "Hall", "ظ‚ط±ظٹط¨ ظ…ظ† ط§ظ„ط´ط±ظˆط·", "طھط­طھ ط§ظ„ظ…ط±ط§ظ‚ط¨ط©", "ط§ظ„ط£ط¹ظ„ظ‰ ط«ظ‚ط©", "ط¢ط®ط± 30 ظٹظˆظ…") ?? "";
        if (string.IsNullOrWhiteSpace(action) || action == "ط¥ظ„ط؛ط§ط،")
            return;

        filterMode = action;
        ApplyFilterAndSort(keepSelection: false);
        await RenderAsync();
    }

    async Task OnSortClicked()
    {
        string action = await DisplayActionSheet("طھط±طھظٹط¨", "ط¥ظ„ط؛ط§ط،", null, "Legacy", "Wins", "Win Rate", "Trust", "XP", "Matches") ?? "";
        if (string.IsNullOrWhiteSpace(action) || action == "ط¥ظ„ط؛ط§ط،")
            return;

        sortMode = action;
        ApplyFilterAndSort(keepSelection: true);
        await RenderAsync();
    }

    async Task OnExportClicked() =>
        await DisplayAlert("طھطµط¯ظٹط±", $"طھظ… طھط¬ظ‡ظٹط² ظ…ظ„ظپ ط¥ط­طµط§ط¦ظٹط§طھ {selected.TeamName} ظ„ظ„ظ…ط±ط­ظ„ط© ط§ظ„طھط§ظ„ظٹط©.", "ط­ط³ظ†ط§ظ‹");

    async Task ShowStatusLegendAsync() =>
        await DisplayAlert(
            "ط¯ظ„ط§ظ„ط§طھ ط­ط§ظ„ط© ط§ظ„ظپط±ظٹظ‚",
            "ط£ط®ط¶ط±: ظ‚ط±ظٹط¨ ظ…ظ† طھط­ظ‚ظٹظ‚ ط§ظ„ط´ط±ظˆط·\nط£طµظپط±: طھط­طھ ط§ظ„ظ…ط±ط§ظ‚ط¨ط©\nط£ط­ظ…ط±: ظ…ط´طھط¨ظ‡ ط£ظˆ طھظˆط¬ط¯ ط£ط¯ظ„ط© ظ…ط¤ظƒط¯ط©\nط±ظ…ط§ط¯ظٹ: ط؛ظٹط± ظ…ط¤ظ‡ظ„ ط¨ط¹ط¯\n\nطھط¸ظ‡ط± ط§ظ„ط­ط§ظ„ط© ظپظٹ ط¨ط·ط§ظ‚ط© ط§ظ„ظپط±ظٹظ‚طŒ ط§ظ„ظ‚ط§ط¦ظ…ط©طŒ طھظپط§طµظٹظ„ ط§ظ„ظپط±ظٹظ‚طŒ طµظپط­ط© ط§ظ„ظ…ط¨ط§ط±ظٹط§طھطŒ ط§ظ„طھط±طھظٹط¨طŒ ظˆظ‚ط§ط¦ظ…ط© ط§ظ„ظ…ط±ط´ط­ظٹظ†.",
            "ط­ط³ظ†ط§ظ‹");

    static DateTime ResolveDate(Models.SavedMatch match) =>
        match.MatchEndDate != default ? match.MatchEndDate :
        match.MatchDate != default ? match.MatchDate :
        match.LastPlayedTime;
}
