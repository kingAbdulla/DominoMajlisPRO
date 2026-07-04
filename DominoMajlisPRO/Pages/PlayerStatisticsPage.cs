using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace DominoMajlisPRO.Pages;

public sealed class PlayerStatisticsPage : ContentPage
{
    PlayerStatisticsSnapshot snapshot = new(Array.Empty<PlayerStatisticsProfile>(), PlayerStatisticsProfile.Empty);
    IReadOnlyList<PlayerStatisticsProfile> visiblePlayers = Array.Empty<PlayerStatisticsProfile>();
    PlayerStatisticsProfile selected = PlayerStatisticsProfile.Empty;
    VerticalStackLayout content = new();
    string filterMode = "الكل";
    string sortMode = "Legacy";
    string activeTab = "overview";
    bool showAllMatches;

    public PlayerStatisticsPage()
    {
        Title = "إحصائيات اللاعبين";
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
        AppEvents.PlayerProfileChanged -= OnDataChanged;
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;
        AppEvents.TeamAssetsChanged -= OnTeamChanged;

        AppEvents.MatchesChanged += OnDataChanged;
        AppEvents.PlayerProfileChanged += OnDataChanged;
        AppEvents.RankingsChanged += OnDataChanged;
        AppEvents.StoreEconomyChanged += OnStoreChanged;
        AppEvents.StoreProgressChanged += OnStoreChanged;
        AppEvents.TeamAssetsChanged += OnTeamChanged;
    }

    void Unsubscribe()
    {
        AppEvents.MatchesChanged -= OnDataChanged;
        AppEvents.PlayerProfileChanged -= OnDataChanged;
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;
        AppEvents.TeamAssetsChanged -= OnTeamChanged;
    }

    async void OnDataChanged()
    {
        HallStatisticsDashboardService.Invalidate();
        await MainThread.InvokeOnMainThreadAsync(async () => await LoadAsync(true));
    }

    void OnStoreChanged(string playerId) => OnDataChanged();
    void OnTeamChanged(string teamId) => OnDataChanged();

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
        snapshot = await HallStatisticsDashboardService.LoadPlayerSnapshotAsync(force);
        ApplyFilterAndSort(keepSelection: true);
        await RenderAsync();
    }

    void ApplyFilterAndSort(bool keepSelection)
    {
        IEnumerable<PlayerStatisticsProfile> query = snapshot.Players;

        query = filterMode switch
        {
            "Hall" => query.Where(player => player.HallEntries > 0),
            "الأبطال" => query.Where(player => player.Championships > 0),
            "MVP" => query.Where(player => player.MVP > 0),
            "الأعلى ثقة" => query.Where(player => player.Trust >= 85),
            "آخر 30 يوم" => query.Where(player => player.Matches.Any(match => ResolveDate(match) >= DateTime.Now.AddDays(-30))),
            _ => query
        };

        query = sortMode switch
        {
            "XP" => query.OrderByDescending(player => player.XP),
            "Win Rate" => query.OrderByDescending(player => player.WinRate),
            "MVP" => query.OrderByDescending(player => player.MVP),
            "Coins" => query.OrderByDescending(player => player.Coins),
            "Matches" => query.OrderByDescending(player => player.TotalMatches),
            _ => query.OrderByDescending(player => player.Legacy)
        };

        visiblePlayers = query.ToList();

        if (keepSelection && !string.IsNullOrWhiteSpace(selected.PlayerId))
        {
            var same = visiblePlayers.FirstOrDefault(player => string.Equals(player.PlayerId, selected.PlayerId, StringComparison.OrdinalIgnoreCase));
            if (same != null)
            {
                selected = same;
                return;
            }
        }

        selected = visiblePlayers.FirstOrDefault() ?? snapshot.Selected;
    }

    async Task RenderAsync()
    {
        content.Children.Clear();
        content.Children.Add(CreateHeader());
        content.Children.Add(CreateToolbar());
        content.Children.Add(CreatePlayerSelector());
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
            content.Children.Add(StatisticsDashboardUi.ChartCard("تطور XP", selected.XpTrend, ChartKind.Area, Color.FromArgb("#A259FF"), selected.XP.ToString("N0")));
            content.Children.Add(StatisticsDashboardUi.ChartCard("تقدم المستوى", selected.LevelTrend, ChartKind.Line, Color.FromArgb("#D4AE62"), $"Level {selected.Level}"));
            return;
        }

        if (activeTab == "achievements")
        {
            content.Children.Add(StatisticsDashboardUi.MetricGrid(new[]
            {
                StatisticsDashboardUi.Metric("champion_gold.png", selected.Championships.ToString("N0"), "البطولات"),
                StatisticsDashboardUi.Metric("halloffame_gold.png", selected.HallEntries.ToString("N0"), "Hall Entries"),
                StatisticsDashboardUi.Metric("trophy_3d.png", selected.MVP.ToString("N0"), "MVP"),
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

        var title = StatisticsDashboardUi.Label("إحصائيات اللاعبين", 24, Color.FromArgb(StatisticsDashboardUi.Gold), true);
        Grid.SetColumn(title, 1);
        grid.Add(title);

        var back = StatisticsDashboardUi.CommandButton("‹");
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
        row.Children.Add(ToolButton("اختيار لاعب", OnChoosePlayerClicked));
        row.Children.Add(ToolButton("بحث", OnSearchClicked));
        row.Children.Add(ToolButton($"فلتر: {filterMode}", OnFilterClicked));
        row.Children.Add(ToolButton($"ترتيب: {sortMode}", OnSortClicked));
        row.Children.Add(ToolButton("تحديث", async () => await LoadAsync(true)));
        row.Children.Add(ToolButton("تصدير", OnExportClicked));
        return row;
    }

    Button ToolButton(string text, Func<Task> action)
    {
        var button = StatisticsDashboardUi.CommandButton(text);
        button.Margin = new Thickness(2);
        button.Clicked += async (s, e) => await action();
        return button;
    }

    View CreatePlayerSelector()
    {
        var strip = new HorizontalStackLayout
        {
            Spacing = 8,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Thickness(2, 0)
        };

        foreach (var player in visiblePlayers.Take(24))
            strip.Children.Add(CreatePlayerChip(player));

        return new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = strip
        };
    }

    View CreatePlayerChip(PlayerStatisticsProfile player)
    {
        bool active = string.Equals(player.PlayerId, selected.PlayerId, StringComparison.OrdinalIgnoreCase);
        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 116 : 150
        };
        stack.Children.Add(StatisticsDashboardUi.Label(player.PlayerName, 12, active ? Colors.White : Color.FromArgb(StatisticsDashboardUi.Muted), true, TextAlignment.Center, 1));
        stack.Children.Add(StatisticsDashboardUi.Label($"L{player.Level} | {player.XP:N0} XP", 10, Color.FromArgb(StatisticsDashboardUi.Gold), false, TextAlignment.Center, 1));

        var card = StatisticsDashboardUi.Frame(stack, 14, active ? "#D4AE62" : "#5B3B18", active ? "#8F6730" : "#080808", 8);
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                selected = player;
                showAllMatches = false;
                await RenderAsync();
            })
        });
        return card;
    }

    async Task<View> CreateHeroAsync()
    {
        PlayerVisualIdentity identity = await PlayerVisualIdentityResolver.ResolveAsync(selected.PlayerId);
        ImageSource avatar = InventoryDisplayResolver.ResolveOptionalImageSource(identity.Avatar?.PreviewImage) ??
            PlayerProfileService.GetPlayerImageSource(selected.SourcePlayer);

        var avatarHost = new Grid
        {
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 160,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 160,
            HorizontalOptions = LayoutOptions.Center,
            Clip = new Microsoft.Maui.Controls.Shapes.EllipseGeometry { Center = new Point(DeviceInfo.Idiom == DeviceIdiom.Phone ? 59 : 80, DeviceInfo.Idiom == DeviceIdiom.Phone ? 59 : 80), RadiusX = DeviceInfo.Idiom == DeviceIdiom.Phone ? 59 : 80, RadiusY = DeviceInfo.Idiom == DeviceIdiom.Phone ? 59 : 80 }
        };

        var background = InventoryDisplayResolver.ResolveOptionalImageSource(identity.ProfileBackground?.PreviewImage);
        if (background != null)
            avatarHost.Add(new Image { Source = background, Aspect = Aspect.AspectFill, Opacity = 0.36 });

        avatarHost.Add(new Image { Source = avatar, Aspect = Aspect.AspectFill });
        AddOverlay(avatarHost, identity.Frame?.PreviewImage);
        AddEffect(avatarHost, identity.Effect);

        var info = new VerticalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        info.Children.Add(StatisticsDashboardUi.Label(selected.PlayerName, DeviceInfo.Idiom == DeviceIdiom.Phone ? 23 : 30, Color.FromArgb(StatisticsDashboardUi.Gold), true, TextAlignment.End));
        info.Children.Add(StatisticsDashboardUi.Label(identity.Title?.DisplayName ?? "Hall Of Legends Member", 14, Color.FromArgb(StatisticsDashboardUi.Gold), false, TextAlignment.End));
        info.Children.Add(ProgressLine($"Level {selected.Level}", selected.RankProgress));
        info.Children.Add(CreateMiniStats());

        var status = new VerticalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        status.Children.Add(StatisticsDashboardUi.Label("حالة اللاعب", 12, Color.FromArgb(StatisticsDashboardUi.Muted), false));
        status.Children.Add(StatisticsDashboardUi.StatusBadge(selected.Status));
        status.Children.Add(StatusRing(selected.Status));

        View layout;
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            layout = new VerticalStackLayout
            {
                Spacing = 10,
                Children = { avatarHost, info, status }
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
            grid.Add(avatarHost);
            Grid.SetColumn(info, 1);
            grid.Add(info);
            Grid.SetColumn(status, 2);
            grid.Add(status);
            layout = grid;
        }

        return StatisticsDashboardUi.Frame(layout, 20, "#8A5B27", "#080808", 10);
    }

    static void AddOverlay(Grid host, string? imagePath)
    {
        var source = InventoryDisplayResolver.ResolveOptionalImageSource(imagePath);
        if (source != null)
            host.Add(new Image { Source = source, Aspect = Aspect.AspectFit, InputTransparent = true });
    }

    static void AddEffect(Grid host, CatalogAssetDisplay? effect)
    {
        if (effect == null)
            return;
        var overlay = new Image { Aspect = Aspect.AspectFit, InputTransparent = true };
        PlayerEffectEngine.Apply(overlay, effect, 1.16);
        host.Add(overlay);
    }

    View CreateMiniStats()
    {
        var mini = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition(), new ColumnDefinition() }, ColumnSpacing = 8 };
        mini.Add(StatisticsDashboardUi.Metric("rankings_gold_icon.png", selected.Rank, "الرتبة الحالية"));
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
            StatisticsDashboardUi.Metric("joystick_gold.png", selected.TotalMatches.ToString("N0"), "المباريات"),
            StatisticsDashboardUi.Metric("win_gold.png", selected.Wins.ToString("N0"), "الفوز"),
            StatisticsDashboardUi.Metric("loss_gold.png", selected.Losses.ToString("N0"), "الخسارة"),
            StatisticsDashboardUi.Metric("target_3d.png", $"{selected.WinRate:0.0}%", "معدل الفوز"),
            StatisticsDashboardUi.Metric("trophy_3d.png", selected.MVP.ToString("N0"), "MVP"),
            StatisticsDashboardUi.Metric("xp_gold.png", selected.Legacy.ToString("N0"), "Legacy"),
            StatisticsDashboardUi.Metric("champion_gold.png", selected.Championships.ToString("N0"), "البطولات"),
            StatisticsDashboardUi.Metric("diamond.png", selected.XP.ToString("N0"), "إجمالي XP"),
            StatisticsDashboardUi.Metric("coin_gold.png", selected.Coins.ToString("N0"), "إجمالي Coins"),
            StatisticsDashboardUi.Metric("halloffame_gold.png", selected.HallEntries.ToString("N0"), "Hall Entries")
        });

    View CreateCharts()
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        int columns = DeviceInfo.Idiom == DeviceIdiom.Phone ? 1 : 2;
        for (int i = 0; i < columns; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());
        var charts = new[]
        {
            StatisticsDashboardUi.ChartCard("توزيع النتائج", new[] { (double)selected.Wins, selected.Losses, Math.Max(0, selected.TotalMatches - selected.Wins - selected.Losses) }, ChartKind.Donut, Color.FromArgb("#69D84F"), $"{selected.WinRate:0.#}%"),
            StatisticsDashboardUi.ChartCard("تطور XP", selected.XpTrend, ChartKind.Area, Color.FromArgb("#A259FF"), selected.XP.ToString("N0")),
            StatisticsDashboardUi.ChartCard("تقدم المستوى", selected.LevelTrend, ChartKind.Line, Color.FromArgb("#D4AE62"), $"Level {selected.Level}"),
            StatisticsDashboardUi.ChartCard("تطور العملات", selected.CoinsTrend, ChartKind.Area, Color.FromArgb("#F4B942"), selected.Coins.ToString("N0"))
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
        stack.Children.Add(StatisticsDashboardUi.Label(showAllMatches ? "المباريات" : "آخر 5 مباريات", 15, Color.FromArgb(StatisticsDashboardUi.Gold), true));
        foreach (var row in rows)
            stack.Children.Add(MatchRow(row));

        if (selected.RecentMatches.Count > 5)
        {
            var more = StatisticsDashboardUi.CommandButton(showAllMatches ? "عرض أقل" : "عرض المزيد");
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
                new ColumnDefinition { Width = 74 },
                new ColumnDefinition { Width = 58 },
                new ColumnDefinition { Width = 70 },
                new ColumnDefinition { Width = 88 },
                new ColumnDefinition { Width = 38 }
            },
            ColumnSpacing = 5
        };
        grid.Add(StatisticsDashboardUi.Label(row.OpponentOrTeam, 12, Colors.White, false, TextAlignment.End));
        AddCell(grid, row.Result, 1, ResultColor(row.Result));
        AddCell(grid, row.Score, 2, Colors.White);
        AddCell(grid, row.MvpOrPoints, 3, Color.FromArgb(StatisticsDashboardUi.Gold));
        AddCell(grid, row.Date.ToString("dd/MM/yyyy"), 4, Color.FromArgb(StatisticsDashboardUi.Muted));
        AddCell(grid, "›", 5, Color.FromArgb(StatisticsDashboardUi.Gold));
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
        if (result.Contains("خسارة", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#FF3B30");
        if (result.Contains("تعادل", StringComparison.OrdinalIgnoreCase))
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

    async Task OnChoosePlayerClicked()
    {
        var names = visiblePlayers.Select(player => player.PlayerName).Take(100).ToArray();
        if (names.Length == 0)
            return;

        string choice = await DisplayActionSheet("اختيار لاعب", "إلغاء", null, names) ?? "";
        var player = visiblePlayers.FirstOrDefault(item => item.PlayerName == choice);
        if (player == null)
            return;

        selected = player;
        showAllMatches = false;
        await RenderAsync();
    }

    async Task OnSearchClicked()
    {
        string result = await DisplayPromptAsync("بحث", "اكتب اسم اللاعب", "بحث", "إلغاء") ?? "";
        if (string.IsNullOrWhiteSpace(result))
            return;

        var found = snapshot.Players
            .Where(player => player.PlayerName.Contains(result, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(player => player.Legacy)
            .FirstOrDefault();

        if (found != null)
        {
            selected = found;
            filterMode = "الكل";
            ApplyFilterAndSort(keepSelection: true);
            await RenderAsync();
        }
    }

    async Task OnFilterClicked()
    {
        string action = await DisplayActionSheet("فلتر", "إلغاء", null, "الكل", "Hall", "الأبطال", "MVP", "الأعلى ثقة", "آخر 30 يوم") ?? "";
        if (string.IsNullOrWhiteSpace(action) || action == "إلغاء")
            return;

        filterMode = action;
        ApplyFilterAndSort(keepSelection: false);
        await RenderAsync();
    }

    async Task OnSortClicked()
    {
        string action = await DisplayActionSheet("ترتيب", "إلغاء", null, "Legacy", "XP", "Win Rate", "MVP", "Coins", "Matches") ?? "";
        if (string.IsNullOrWhiteSpace(action) || action == "إلغاء")
            return;

        sortMode = action;
        ApplyFilterAndSort(keepSelection: true);
        await RenderAsync();
    }

    async Task OnExportClicked() =>
        await DisplayAlert("تصدير", $"تم تجهيز ملف إحصائيات {selected.PlayerName} للمرحلة التالية.", "حسناً");

    async Task ShowStatusLegendAsync() =>
        await DisplayAlert(
            "دلالات حالة اللاعب",
            "أخضر: قريب من تحقيق الشروط\nأصفر: تحت المراقبة\nأحمر: مشتبه أو توجد أدلة مؤكدة\nرمادي: غير مؤهل بعد\n\nتظهر الحالة في بطاقة اللاعب، القائمة، تفاصيل اللاعب، صفحة المباريات، والترتيب.",
            "حسناً");

    static DateTime ResolveDate(Models.SavedMatch match) =>
        match.MatchEndDate != default ? match.MatchEndDate :
        match.MatchDate != default ? match.MatchDate :
        match.LastPlayedTime;
}
