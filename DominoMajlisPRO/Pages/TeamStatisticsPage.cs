using DominoMajlisPRO.Controls;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace DominoMajlisPRO.Pages;

public sealed class TeamStatisticsPage : ContentPage
{
    TeamStatisticsSnapshot snapshot = new(Array.Empty<TeamStatisticsProfile>(), TeamStatisticsProfile.Empty);
    TeamStatisticsProfile selected = TeamStatisticsProfile.Empty;
    VerticalStackLayout content = new();
    bool showAllMatches;

    public TeamStatisticsPage()
    {
        Title = "إحصائيات الفرق";
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
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        content = new VerticalStackLayout
        {
            Padding = new Thickness(12, 10, 12, 18),
            Spacing = 10
        };

        var scroll = new ScrollView { Content = content };
        root.Add(scroll);

        var bottom = new HallBottomNavigationView();
        Grid.SetRow(bottom, 1);
        root.Add(bottom);

        Content = root;
    }

    async Task LoadAsync(bool force = false)
    {
        snapshot = await HallStatisticsDashboardService.LoadTeamSnapshotAsync(force);
        if (string.IsNullOrWhiteSpace(selected.TeamId) ||
            !snapshot.Teams.Any(team => string.Equals(team.TeamId, selected.TeamId, StringComparison.OrdinalIgnoreCase)))
        {
            selected = snapshot.Selected;
        }
        else
        {
            selected = snapshot.Teams.First(team => string.Equals(team.TeamId, selected.TeamId, StringComparison.OrdinalIgnoreCase));
        }

        await RenderAsync();
    }

    async Task RenderAsync()
    {
        content.Children.Clear();
        content.Children.Add(CreateHeader());
        content.Children.Add(CreateToolbar());
        content.Children.Add(await CreateHeroAsync());
        content.Children.Add(CreateTabs());
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
            }
        };

        var help = StatisticsDashboardUi.CommandButton("?");
        help.WidthRequest = 38;
        help.Clicked += async (s, e) => await ShowStatusLegendAsync();
        grid.Add(help);

        var title = StatisticsDashboardUi.Label("إحصائيات الفرق", 24, Color.FromArgb(StatisticsDashboardUi.Gold), true);
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

        row.Children.Add(ToolButton("بحث", OnSearchClicked));
        row.Children.Add(ToolButton("فلتر", OnFilterClicked));
        row.Children.Add(ToolButton("ترتيب", OnSortClicked));
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

    async Task<View> CreateHeroAsync()
    {
        var identity = await TeamIdentityResolver.ResolveAsync(selected.TeamId);
        var emblem = InventoryDisplayResolver.ResolveImageSource(identity.EmblemImagePath, "shield_3d.png");
        var background = InventoryDisplayResolver.ResolveOptionalImageSource(identity.EmblemBackgroundSource);

        var imageHost = new Grid
        {
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 170,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 170,
            HorizontalOptions = LayoutOptions.Center
        };

        if (background != null)
        {
            imageHost.Add(new Image { Source = background, Aspect = Aspect.AspectFill, Opacity = 0.38 });
        }

        imageHost.Add(new Image { Source = emblem, Aspect = Aspect.AspectFit });

        var info = new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center };
        info.Children.Add(StatisticsDashboardUi.Label(selected.TeamName, 25, Color.FromArgb(StatisticsDashboardUi.Gold), true, TextAlignment.End));
        info.Children.Add(StatisticsDashboardUi.Label(selected.LevelTitle, 14, Color.FromArgb(StatisticsDashboardUi.Gold), false, TextAlignment.End));
        info.Children.Add(ProgressLine($"Level {selected.Level}", selected.LevelProgress));

        var mini = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition(), new ColumnDefinition() }, ColumnSpacing = 8 };
        mini.Add(StatisticsDashboardUi.Metric("rankings_gold_icon.png", selected.Rank, "الرتبة الحالية"));
        var legacy = StatisticsDashboardUi.Metric("trophy_3d.png", selected.Legacy.ToString("N0"), "Legacy");
        Grid.SetColumn(legacy, 1);
        mini.Add(legacy);
        var trust = StatisticsDashboardUi.Metric("trust_gold.png", selected.Trust.ToString(), "Trust Score");
        Grid.SetColumn(trust, 2);
        mini.Add(trust);
        info.Children.Add(mini);

        var status = new VerticalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        status.Children.Add(StatisticsDashboardUi.Label("حالة الفريق", 12, Color.FromArgb(StatisticsDashboardUi.Muted), false));
        status.Children.Add(StatisticsDashboardUi.StatusBadge(selected.Status));
        status.Children.Add(StatusRing(selected.Status));

        var layout = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = DeviceInfo.Idiom == DeviceIdiom.Phone ? 110 : 160 }
            },
            ColumnSpacing = 12
        };

        layout.Add(imageHost);
        Grid.SetColumn(info, 1);
        layout.Add(info);
        Grid.SetColumn(status, 2);
        layout.Add(status);

        return StatisticsDashboardUi.Frame(layout, 20, "#8A5B27", "#080808", 10);
    }

    View CreateTabs()
    {
        var grid = new Grid { ColumnSpacing = 0 };
        string[] tabs = { "نظرة عامة", "الأداء", "المواسم", "الإنجازات" };
        for (int i = 0; i < tabs.Length; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        for (int i = 0; i < tabs.Length; i++)
        {
            var tab = StatisticsDashboardUi.Frame(
                StatisticsDashboardUi.Label(tabs[i], 12, i == 0 ? Colors.White : Color.FromArgb(StatisticsDashboardUi.Muted), i == 0),
                12,
                "#5B3B18",
                i == 0 ? "#8F6730" : "#080808",
                8);
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
            StatisticsDashboardUi.Metric("xp_gold.png", selected.XP.ToString("N0"), "إجمالي XP"),
            StatisticsDashboardUi.Metric("coin_gold.png", selected.Coins.ToString("N0"), "إجمالي Coins"),
            StatisticsDashboardUi.Metric("trophy_3d.png", selected.MVP.ToString("N0"), "MVP"),
            StatisticsDashboardUi.Metric("champion_gold.png", selected.Championships.ToString("N0"), "البطولات"),
            StatisticsDashboardUi.Metric("halloffame_gold.png", selected.HallEntries.ToString("N0"), "Hall Entries"),
            StatisticsDashboardUi.Metric("crown_3d.png", selected.HighestWinStreak.ToString("N0"), "أعلى سلسلة فوز")
        });

    View CreateCharts()
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        int columns = DeviceInfo.Idiom == DeviceIdiom.Phone ? 1 : 2;
        for (int i = 0; i < columns; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        var charts = new[]
        {
            StatisticsDashboardUi.ChartCard("نسبة الفوز في آخر 20 مباراة", selected.WinRateTrend, ChartKind.Line, Color.FromArgb("#69D84F")),
            StatisticsDashboardUi.ChartCard("تطور Legacy", selected.LegacyTrend, ChartKind.Area, Color.FromArgb("#D4AE62")),
            StatisticsDashboardUi.ChartCard("XP Progress", selected.XpTrend, ChartKind.Area, Color.FromArgb("#A259FF")),
            StatisticsDashboardUi.ChartCard("Season Progress", selected.SeasonTrend, ChartKind.Line, Color.FromArgb("#F4B942"))
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
        stack.Children.Add(StatisticsDashboardUi.Label("آخر 5 مباريات", 15, Color.FromArgb(StatisticsDashboardUi.Gold), true));
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
                new ColumnDefinition { Width = 70 },
                new ColumnDefinition { Width = 70 },
                new ColumnDefinition { Width = 96 },
                new ColumnDefinition { Width = 46 }
            },
            ColumnSpacing = 6
        };

        grid.Add(StatisticsDashboardUi.Label(row.OpponentOrTeam, 12, Colors.White, false, TextAlignment.End));
        AddCell(grid, row.Score, 1, Colors.White);
        AddCell(grid, row.Result, 2, row.Result.Contains("خس") ? Color.FromArgb("#FF3B30") : row.Result.Contains("تع") ? Color.FromArgb("#BFC3C7") : Color.FromArgb("#69D84F"));
        AddCell(grid, row.Date.ToString("dd/MM/yyyy"), 3, Color.FromArgb(StatisticsDashboardUi.Muted));
        AddCell(grid, "›", 4, Color.FromArgb(StatisticsDashboardUi.Gold));
        return StatisticsDashboardUi.Frame(grid, 10, "#2D2415", "#101010", 8);
    }

    static void AddCell(Grid grid, string text, int column, Color color)
    {
        var label = StatisticsDashboardUi.Label(text, 12, color, false);
        Grid.SetColumn(label, column);
        grid.Add(label);
    }

    View ProgressLine(string title, double progress)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 74 }, new ColumnDefinition() }, ColumnSpacing = 8 };
        grid.Add(StatisticsDashboardUi.Label(title, 12, Colors.White, true, TextAlignment.End));
        var bar = new ProgressBar { Progress = progress, ProgressColor = Color.FromArgb(StatisticsDashboardUi.Gold), BackgroundColor = Color.FromArgb("#252525"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(bar, 1);
        grid.Add(bar);
        return grid;
    }

    View StatusRing(SafeStatusResult status)
    {
        var ring = new GraphicsView
        {
            HeightRequest = 92,
            WidthRequest = 92,
            Drawable = new TrendChartDrawable { Values = new[] { status.Progress, 1 - status.Progress }, Kind = ChartKind.Donut, PrimaryColor = Color.FromArgb(status.ColorHex) }
        };
        return ring;
    }

    async Task OnSearchClicked()
    {
        string result = await DisplayPromptAsync("بحث", "اكتب اسم الفريق", "بحث", "إلغاء") ?? "";
        if (string.IsNullOrWhiteSpace(result))
            return;
        var found = snapshot.Teams.FirstOrDefault(team => team.TeamName.Contains(result, StringComparison.OrdinalIgnoreCase));
        if (found != null)
        {
            selected = found;
            await RenderAsync();
        }
    }

    async Task OnFilterClicked()
    {
        string action = await DisplayActionSheet("فلتر", "إلغاء", null, "الكل", "Hall only", "آخر 30 يوم", "قريب من الشروط") ?? "";
        IEnumerable<TeamStatisticsProfile> query = snapshot.Teams;
        if (action == "Hall only")
            query = query.Where(team => team.HallEntries > 0);
        if (action == "قريب من الشروط")
            query = query.Where(team => team.Status.ColorHex == "#69D84F");
        var first = query.FirstOrDefault();
        if (first != null)
        {
            selected = first;
            await RenderAsync();
        }
    }

    async Task OnSortClicked()
    {
        string action = await DisplayActionSheet("ترتيب", "إلغاء", null, "Legacy", "Wins", "Win Rate", "Trust", "Rank") ?? "";
        selected = action switch
        {
            "Wins" => snapshot.Teams.OrderByDescending(team => team.Wins).FirstOrDefault() ?? selected,
            "Win Rate" => snapshot.Teams.OrderByDescending(team => team.WinRate).FirstOrDefault() ?? selected,
            "Trust" => snapshot.Teams.OrderByDescending(team => team.Trust).FirstOrDefault() ?? selected,
            "Rank" => snapshot.Teams.OrderByDescending(team => team.XP).FirstOrDefault() ?? selected,
            _ => snapshot.Teams.OrderByDescending(team => team.Legacy).FirstOrDefault() ?? selected
        };
        await RenderAsync();
    }

    async Task OnExportClicked() =>
        await DisplayAlert("تصدير", "تم تجهيز بيانات إحصائيات الفرق للتصدير المستقبلي.", "حسناً");

    async Task ShowStatusLegendAsync() =>
        await DisplayAlert(
            "دلالات حالة الفريق",
            "أخضر: قريب من تحقيق الشروط\nأصفر: تحت المراقبة\nأحمر: مشبوه أو أدلة مؤكدة\nرمادي: غير مؤهل بعد",
            "حسناً");

    async void OnBottomNavigation(string destination)
    {
        switch (destination)
        {
            case "SETTINGS":
                await Navigation.PushAsync(new MainPage());
                break;
            case "PLAYERS":
                await Navigation.PushAsync(new PlayerStatisticsPage());
                break;
            case "GAME":
                await Navigation.PushAsync(new CreateTeamPage());
                break;
            case "STORE":
                await Navigation.PushAsync(new GalleryEngine.Pages.GalleryPage());
                break;
        }
    }
}
