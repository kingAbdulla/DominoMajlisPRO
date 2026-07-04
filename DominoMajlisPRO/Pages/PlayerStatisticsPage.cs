using DominoMajlisPRO.Controls;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace DominoMajlisPRO.Pages;

public sealed class PlayerStatisticsPage : ContentPage
{
    PlayerStatisticsSnapshot snapshot = new(Array.Empty<PlayerStatisticsProfile>(), PlayerStatisticsProfile.Empty);
    PlayerStatisticsProfile selected = PlayerStatisticsProfile.Empty;
    VerticalStackLayout content = new();
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
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            }
        };
        content = new VerticalStackLayout { Padding = new Thickness(12, 10, 12, 18), Spacing = 10 };
        root.Add(new ScrollView { Content = content });

        var bottom = new HallBottomNavigationView();
        Grid.SetRow(bottom, 1);
        root.Add(bottom);
        Content = root;
    }

    async Task LoadAsync(bool force = false)
    {
        snapshot = await HallStatisticsDashboardService.LoadPlayerSnapshotAsync(force);
        if (string.IsNullOrWhiteSpace(selected.PlayerId) ||
            !snapshot.Players.Any(player => string.Equals(player.PlayerId, selected.PlayerId, StringComparison.OrdinalIgnoreCase)))
        {
            selected = snapshot.Selected;
        }
        else
        {
            selected = snapshot.Players.First(player => string.Equals(player.PlayerId, selected.PlayerId, StringComparison.OrdinalIgnoreCase));
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
        var row = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.SpaceBetween };
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
        PlayerVisualIdentity identity = await PlayerVisualIdentityResolver.ResolveAsync(selected.PlayerId);
        ImageSource avatar = InventoryDisplayResolver.ResolveOptionalImageSource(identity.Avatar?.PreviewImage) ??
            PlayerProfileService.GetPlayerImageSource(selected.SourcePlayer);

        var avatarHost = new Grid
        {
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 160,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 118 : 160,
            HorizontalOptions = LayoutOptions.Center
        };

        var background = InventoryDisplayResolver.ResolveOptionalImageSource(identity.ProfileBackground?.PreviewImage);
        if (background != null)
            avatarHost.Add(new Image { Source = background, Aspect = Aspect.AspectFill, Opacity = 0.36 });

        avatarHost.Add(new Image { Source = avatar, Aspect = Aspect.AspectFill });
        AddOverlay(avatarHost, identity.Frame?.PreviewImage);
        AddEffect(avatarHost, identity.Effect);

        var info = new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center };
        info.Children.Add(StatisticsDashboardUi.Label(selected.PlayerName, 25, Color.FromArgb(StatisticsDashboardUi.Gold), true, TextAlignment.End));
        info.Children.Add(StatisticsDashboardUi.Label(identity.Title?.DisplayName ?? "Hall Of Legends Member", 14, Color.FromArgb(StatisticsDashboardUi.Gold), false, TextAlignment.End));
        info.Children.Add(ProgressLine($"Level {selected.Level}", selected.RankProgress));

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
        status.Children.Add(StatisticsDashboardUi.Label("حالة اللاعب", 12, Color.FromArgb(StatisticsDashboardUi.Muted), false));
        status.Children.Add(StatisticsDashboardUi.StatusBadge(selected.Status));
        status.Children.Add(new GraphicsView
        {
            HeightRequest = 92,
            WidthRequest = 92,
            Drawable = new TrendChartDrawable { Values = new[] { selected.Status.Progress, 1 - selected.Status.Progress }, Kind = ChartKind.Donut, PrimaryColor = Color.FromArgb(selected.Status.ColorHex) }
        });

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
        layout.Add(avatarHost);
        Grid.SetColumn(info, 1);
        layout.Add(info);
        Grid.SetColumn(status, 2);
        layout.Add(status);
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

    View CreateTabs()
    {
        var grid = new Grid { ColumnSpacing = 0 };
        string[] tabs = { "نظرة عامة", "الأداء", "الإنجازات", "المواسم", "المباريات" };
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
            StatisticsDashboardUi.ChartCard("توزيع النتائج", new[] { (double)selected.Wins, selected.Losses, Math.Max(0, selected.TotalMatches - selected.Wins - selected.Losses) }, ChartKind.Donut, Color.FromArgb("#69D84F")),
            StatisticsDashboardUi.ChartCard("تطور الـ XP", selected.XpTrend, ChartKind.Area, Color.FromArgb("#A259FF")),
            StatisticsDashboardUi.ChartCard("Level Progress", selected.LevelTrend, ChartKind.Line, Color.FromArgb("#D4AE62")),
            StatisticsDashboardUi.ChartCard("Coins Earned", selected.CoinsTrend, ChartKind.Area, Color.FromArgb("#F4B942"))
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
                new ColumnDefinition { Width = 74 },
                new ColumnDefinition { Width = 58 },
                new ColumnDefinition { Width = 70 },
                new ColumnDefinition { Width = 88 },
                new ColumnDefinition { Width = 38 }
            },
            ColumnSpacing = 5
        };
        grid.Add(StatisticsDashboardUi.Label(row.OpponentOrTeam, 12, Colors.White, false, TextAlignment.End));
        AddCell(grid, row.Result, 1, row.Result.Contains("خس") ? Color.FromArgb("#FF3B30") : row.Result.Contains("تع") ? Color.FromArgb("#BFC3C7") : Color.FromArgb("#69D84F"));
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

    View ProgressLine(string title, double progress)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = 74 }, new ColumnDefinition() }, ColumnSpacing = 8 };
        grid.Add(StatisticsDashboardUi.Label(title, 12, Colors.White, true, TextAlignment.End));
        var bar = new ProgressBar { Progress = progress, ProgressColor = Color.FromArgb(StatisticsDashboardUi.Gold), BackgroundColor = Color.FromArgb("#252525"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(bar, 1);
        grid.Add(bar);
        return grid;
    }

    async Task OnSearchClicked()
    {
        string result = await DisplayPromptAsync("بحث", "اكتب اسم اللاعب", "بحث", "إلغاء") ?? "";
        if (string.IsNullOrWhiteSpace(result))
            return;
        var found = snapshot.Players.FirstOrDefault(player => player.PlayerName.Contains(result, StringComparison.OrdinalIgnoreCase));
        if (found != null)
        {
            selected = found;
            await RenderAsync();
        }
    }

    async Task OnFilterClicked()
    {
        string action = await DisplayActionSheet("فلتر", "إلغاء", null, "الكل", "Hall", "Rank", "Date range", "الفريق") ?? "";
        IEnumerable<PlayerStatisticsProfile> query = snapshot.Players;
        if (action == "Hall")
            query = query.Where(player => player.HallEntries > 0);
        if (action == "Rank")
            query = query.Where(player => !string.Equals(player.Rank, "Unranked", StringComparison.OrdinalIgnoreCase));
        var first = query.FirstOrDefault();
        if (first != null)
        {
            selected = first;
            await RenderAsync();
        }
    }

    async Task OnSortClicked()
    {
        string action = await DisplayActionSheet("ترتيب", "إلغاء", null, "Legacy", "XP", "Win Rate", "MVP", "Coins") ?? "";
        selected = action switch
        {
            "XP" => snapshot.Players.OrderByDescending(player => player.XP).FirstOrDefault() ?? selected,
            "Win Rate" => snapshot.Players.OrderByDescending(player => player.WinRate).FirstOrDefault() ?? selected,
            "MVP" => snapshot.Players.OrderByDescending(player => player.MVP).FirstOrDefault() ?? selected,
            "Coins" => snapshot.Players.OrderByDescending(player => player.Coins).FirstOrDefault() ?? selected,
            _ => snapshot.Players.OrderByDescending(player => player.Legacy).FirstOrDefault() ?? selected
        };
        await RenderAsync();
    }

    async Task OnExportClicked() =>
        await DisplayAlert("تصدير", "تم تجهيز بيانات إحصائيات اللاعبين للتصدير المستقبلي.", "حسناً");

    async Task ShowStatusLegendAsync() =>
        await DisplayAlert(
            "دلالات حالة اللاعب",
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
