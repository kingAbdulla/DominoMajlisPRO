using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class HallOfFamePage : ContentPage
{
    const string Gold = "#D4AE62";
    const string Bronze = "#5B3B18";
    const string Muted = "#C8B58A";

    HallOfFameSnapshot snapshot = new(
        "الحالي",
        null,
        Array.Empty<HallTeamEvaluation>(),
        Array.Empty<HallPlayerEvaluation>(),
        Array.Empty<HallTeamEvaluation>(),
        Array.Empty<HallPlayerEvaluation>(),
        Array.Empty<HallRecord>(),
        Array.Empty<HallStatistic>(),
        Array.Empty<HallAuditEntry>(),
        new HallVerificationResult(Array.Empty<HallVerificationCheck>(), false));

    public HallOfFamePage()
    {
        InitializeComponent();
        SideMenu.NavigationRequested -= OnSideMenuNavigation;
        SideMenu.NavigationRequested += OnSideMenuNavigation;
        BottomNavigation.NavigationRequested -= OnBottomNavigation;
        BottomNavigation.NavigationRequested += OnBottomNavigation;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Subscribe();
        await LoadHallOfFameAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Unsubscribe();
    }

    void Subscribe()
    {
        AppEvents.DataChanged -= OnHallDataChanged;
        AppEvents.PlayerProfileChanged -= OnHallDataChanged;
        AppEvents.MatchesChanged -= OnHallDataChanged;
        AppEvents.TeamsChanged -= OnHallDataChanged;
        AppEvents.RankingsChanged -= OnHallDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged -= OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;

        AppEvents.DataChanged += OnHallDataChanged;
        AppEvents.PlayerProfileChanged += OnHallDataChanged;
        AppEvents.MatchesChanged += OnHallDataChanged;
        AppEvents.TeamsChanged += OnHallDataChanged;
        AppEvents.RankingsChanged += OnHallDataChanged;
        AppEvents.TeamAssetsChanged += OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged += OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged += OnStoreChanged;
        AppEvents.StoreProgressChanged += OnStoreChanged;
    }

    void Unsubscribe()
    {
        AppEvents.DataChanged -= OnHallDataChanged;
        AppEvents.PlayerProfileChanged -= OnHallDataChanged;
        AppEvents.MatchesChanged -= OnHallDataChanged;
        AppEvents.TeamsChanged -= OnHallDataChanged;
        AppEvents.RankingsChanged -= OnHallDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamAssetsChanged;
        AppEvents.TeamEffectChanged -= OnTeamAssetsChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;
    }

    async void OnHallDataChanged()
    {
        HallOfFameService.InvalidateCache();
        HallStatisticsDashboardService.Invalidate();
        await MainThread.InvokeOnMainThreadAsync(async () => await LoadHallOfFameAsync(true));
    }

    void OnTeamAssetsChanged(string teamId) => OnHallDataChanged();
    void OnStoreChanged(string playerId) => OnHallDataChanged();

    async Task LoadHallOfFameAsync(bool forceRefresh = false)
    {
        snapshot = await HallOfFameService.LoadAsync(forceRefresh);
        HallContainer.Opacity = 0;
        ClearDynamicSections();
        RenderHero();
        await RenderTeamsAsync();
        RenderCandidateCenterButton();
        await RenderPlayersAsync();
        RenderRecords();
        RenderStatistics();
        await AnimatePageAsync();
    }

    void ClearDynamicSections()
    {
        TopTeamsContainer.Children.Clear();
        TopPlayersContainer.Children.Clear();
        CandidatesContainer.Children.Clear();
        RecordsContainer.Children.Clear();
        StatsContainer.Children.Clear();
    }

    void RenderHero()
    {
        SeasonNumberLabel.Text = snapshot.SeasonText;
        var hero = snapshot.HeroTeam;
        if (hero == null)
        {
            HeroTeamNameLabel.Text = "لا توجد أسطورة";
            HeroSubtitleLabel.Text = "بانتظار عضو مؤكد وفق دستور قاعة الأساطير";
            HeroWinsLabel.Text = "0";
            HeroWinRateLabel.Text = "0%";
            HeroLegacyLabel.Text = "0";
            return;
        }

        HeroTeamNameLabel.Text = hero.DisplayName;
        HeroSubtitleLabel.Text = "عضو مؤكد في قاعة الأساطير";
        HeroWinsLabel.Text = hero.Wins.ToString("N0");
        HeroWinRateLabel.Text = $"{hero.WinRate:0.#}%";
        HeroLegacyLabel.Text = hero.Legacy.ToString("N0");
    }

    async Task RenderTeamsAsync()
    {
        var teams = snapshot.TeamMembers.Take(6).ToList();
        if (teams.Count == 0)
        {
            TopTeamsContainer.Children.Add(CreateEmptyCard("لا توجد فرق دخلت قاعة الأساطير وفق الشروط بعد"));
            return;
        }

        var identities = await TeamIdentityResolver.ResolveManyAsync(teams.Select(team => team.TeamId));
        for (int i = 0; i < teams.Count; i++)
        {
            identities.TryGetValue(teams[i].TeamId, out var identity);
            identity ??= await TeamIdentityResolver.ResolveAsync(teams[i].TeamId);
            TopTeamsContainer.Children.Add(CreateTeamCard(i + 1, teams[i], identity));
        }

        TopTeamsContainer.HorizontalOptions = teams.Count <= 2 ? LayoutOptions.Center : LayoutOptions.End;
    }

    async Task RenderPlayersAsync()
    {
        var players = snapshot.PlayerMembers.Take(6).ToList();
        if (players.Count == 0)
        {
            TopPlayersContainer.Children.Add(CreateEmptyCard("لا يوجد لاعب دخل قاعة الأساطير وفق الشروط بعد"));
            return;
        }

        var identities = await PlayerVisualIdentityResolver.ResolveManyAsync(players.Select(player => player.PlayerId));
        for (int i = 0; i < players.Count; i++)
        {
            identities.TryGetValue(players[i].PlayerId, out var identity);
            identity ??= await PlayerVisualIdentityResolver.ResolveAsync(players[i].PlayerId);
            TopPlayersContainer.Children.Add(CreatePlayerCard(i + 1, players[i], identity));
        }

        TopPlayersContainer.HorizontalOptions = players.Count <= 3 ? LayoutOptions.Center : LayoutOptions.End;
    }

    void RenderCandidateCenterButton()
    {
        CandidatesSection.IsVisible = true;
        CandidatesContainer.Children.Add(Frame(
            new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Label("مركز المرشحين", 18, Color.FromArgb(Gold), true),
                    Label("التقييم والتدقيق منفصلان عن قائمة أعضاء قاعة الأساطير.", 12, Color.FromArgb(Muted), false, TextAlignment.Center, 2),
                    CommandButton("فتح مركز المرشحين", async () => await Navigation.PushAsync(new HallCandidateCenterPage()))
                }
            },
            22,
            Bronze,
            "#070707",
            12));
    }

    void RenderRecords()
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        for (int i = 0; i < snapshot.Records.Count; i++)
        {
            var record = snapshot.Records[i];
            int row = i / 2;
            while (grid.RowDefinitions.Count <= row)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var card = CreateRecordCard(record);
            Grid.SetColumn(card, i % 2);
            Grid.SetRow(card, row);
            grid.Add(card);
        }

        RecordsContainer.Children.Add(grid);
    }

    void RenderStatistics()
    {
        for (int i = 0; i < snapshot.Statistics.Count; i++)
        {
            var stat = snapshot.Statistics[i];
            AddStat(stat.Icon, stat.Title, stat.Value, i % 3, i / 3);
        }
    }

    View CreateTeamCard(int rank, HallTeamEvaluation team, TeamIdentityModel? identity)
    {
        string border = !string.IsNullOrWhiteSpace(identity?.TeamColorHex) ? identity.TeamColorHex : RankBorder(rank);
        var emblem = new Image
        {
            Source = ToImageSource(identity?.EmblemImagePath) ?? "shield_3d.png",
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 62 : 88,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center
        };
        TeamEffectBehavior.SetTeamId(emblem, team.TeamId);
        var emblemHost = new Grid { HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 74 : 98 };
        AddTeamBackground(emblemHost, identity);
        emblemHost.Children.Add(emblem);

        var layout = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center };
        layout.Children.Add(Label(rank <= 3 ? $"#{rank}" : rank.ToString(), 13, Color.FromArgb(Gold), true));
        layout.Children.Add(emblemHost);
        layout.Children.Add(Label(team.DisplayName, DeviceInfo.Idiom == DeviceIdiom.Phone ? 15 : 19, Colors.White, true));
        layout.Children.Add(Label(team.LevelTitle, 10, Color.FromArgb(Muted)));
        layout.Children.Add(Label($"Level {team.Level}", 10, Color.FromArgb(Gold), true));
        layout.Children.Add(Label($"{team.Wins:N0} انتصار", 11, Color.FromArgb(Muted)));
        layout.Children.Add(Label($"Legacy {team.Legacy:N0}", 12, Color.FromArgb(Gold), true));
        layout.Children.Add(Label($"دخل القاعة {team.HallEnteredAt:dd/MM/yyyy}", 10, Color.FromArgb(Muted)));
        layout.Children.Add(Label($"{team.FinalScore:0.#}%", 11, Color.FromArgb(Gold), true));

        return Frame(layout, 22, border, rank == 1 ? "#1B1005" : "#0C0C0C", 8, DeviceInfo.Idiom == DeviceIdiom.Phone ? 132 : 176, DeviceInfo.Idiom == DeviceIdiom.Phone ? 218 : 270);
    }

    View CreatePlayerCard(int rank, HallPlayerEvaluation player, PlayerVisualIdentity? identity)
    {
        string border = RankBorder(rank);
        var avatar = new Grid { HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 70 : 92, WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 70 : 92 };
        AddOptionalBackground(avatar, identity?.ProfileBackground?.PreviewImage);
        avatar.Children.Add(new Image
        {
            Source = ToImageSource(identity?.Avatar?.PreviewImage) ?? ToImageSource(player.AvatarImagePath) ?? "player_card.png",
            Aspect = Aspect.AspectFill,
            HeightRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 66 : 88,
            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 66 : 88,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        });
        AddPlayerEffect(avatar, identity?.Effect);
        AddPlayerFrame(avatar, identity?.Frame?.PreviewImage);

        var layout = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center };
        layout.Children.Add(Label(rank <= 3 ? $"#{rank}" : rank.ToString(), 13, Color.FromArgb(Gold), true));
        layout.Children.Add(avatar);
        layout.Children.Add(Label(player.DisplayName, DeviceInfo.Idiom == DeviceIdiom.Phone ? 14 : 18, Colors.White, true));
        layout.Children.Add(Label(identity?.Title?.DisplayName ?? player.Category, 10, Color.FromArgb(Muted)));
        layout.Children.Add(Label($"Legacy {player.Legacy:N0}", 12, Color.FromArgb(Gold), true));
        layout.Children.Add(Label($"دخل القاعة {player.HallEnteredAt:dd/MM/yyyy}", 10, Color.FromArgb(Muted)));
        layout.Children.Add(Label($"{player.FinalScore:0.#}%", 11, Color.FromArgb(Gold), true));

        return Frame(layout, 22, border, rank == 1 ? "#1B1005" : "#0C0C0C", 8, DeviceInfo.Idiom == DeviceIdiom.Phone ? 132 : 166, DeviceInfo.Idiom == DeviceIdiom.Phone ? 214 : 260);
    }

    View CreateRecordCard(HallRecord record)
    {
        return Frame(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                Label(record.Title, 12, Color.FromArgb(Muted), false),
                Label(record.Holder, 13, Colors.White, true),
                Label(record.Value, 15, Color.FromArgb(Gold), true)
            }
        }, 14, Bronze, "#101010", 8);
    }

    View CreateEmptyCard(string text) =>
        Frame(Label(text, 13, Color.FromArgb(Muted), false, TextAlignment.Center, 2), 18, Bronze, "#0C0C0C", 14);

    void AddStat(string icon, string title, string value, int column, int row)
    {
        while (StatsContainer.RowDefinitions.Count <= row)
            StatsContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var card = Frame(new HorizontalStackLayout
        {
            FlowDirection = FlowDirection.RightToLeft,
            Spacing = 8,
            Children =
            {
                new Image { Source = icon, WidthRequest = 30, HeightRequest = 30, Aspect = Aspect.AspectFit },
                new VerticalStackLayout
                {
                    Spacing = 0,
                    Children =
                    {
                        Label(value, 16, Colors.White, true, TextAlignment.End),
                        Label(title, 10, Color.FromArgb(Muted), false, TextAlignment.End)
                    }
                }
            }
        }, 14, Bronze, "#101010", 8);
        Grid.SetColumn(card, column);
        Grid.SetRow(card, row);
        StatsContainer.Add(card);
    }

    static Border Frame(View content, double radius, string stroke, string background, double padding, double width = -1, double height = -1)
    {
        var frame = new Border
        {
            BackgroundColor = Color.FromArgb(background),
            Stroke = Color.FromArgb(stroke),
            StrokeThickness = 0.85,
            Padding = padding,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Content = content
        };
        if (width > 0) frame.WidthRequest = width;
        if (height > 0) frame.HeightRequest = height;
        return frame;
    }

    static Label Label(string text, double size, Color color, bool bold = false, TextAlignment align = TextAlignment.Center, int maxLines = 1) =>
        new()
        {
            Text = text,
            FontFamily = "Tajawal-Regular",
            FontSize = size,
            TextColor = color,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = align,
            MaxLines = maxLines,
            LineBreakMode = maxLines == 1 ? LineBreakMode.TailTruncation : LineBreakMode.WordWrap
        };

    static Button CommandButton(string text, Func<Task> action)
    {
        var button = new Button
        {
            Text = text,
            FontFamily = "Tajawal-Regular",
            TextColor = Color.FromArgb(Gold),
            BackgroundColor = Color.FromArgb("#15100A"),
            BorderColor = Color.FromArgb("#8A5B27"),
            BorderWidth = 1,
            CornerRadius = 16,
            HeightRequest = 42
        };
        button.Clicked += async (s, e) => await action();
        return button;
    }

    static ImageSource? ToImageSource(string? imagePath) =>
        string.IsNullOrWhiteSpace(imagePath) ? null : InventoryDisplayResolver.ResolveImageSource(imagePath, "");

    static void AddOptionalBackground(Grid host, string? imagePath)
    {
        var source = ToImageSource(imagePath);
        if (source == null)
            return;
        host.Children.Add(new Image { Source = source, Aspect = Aspect.AspectFill, Opacity = 0.45 });
    }

    static void AddTeamBackground(Grid host, TeamIdentityModel? identity)
    {
        string source = identity?.EmblemBackgroundSource ?? "";
        if (string.IsNullOrWhiteSpace(source) || string.Equals(source, "Transparent", StringComparison.OrdinalIgnoreCase))
            return;

        if (source.StartsWith("#", StringComparison.Ordinal))
        {
            host.BackgroundColor = Color.FromArgb(source);
            return;
        }

        host.Children.Add(new Image
        {
            Source = InventoryDisplayResolver.ResolveImageSource(source, ""),
            Aspect = Aspect.AspectFill,
            Opacity = 0.46
        });
    }

    static void AddPlayerEffect(Grid host, CatalogAssetDisplay? effect)
    {
        var overlay = new Image { InputTransparent = true, Aspect = Aspect.AspectFit };
        PlayerEffectEngine.Apply(overlay, effect, 1.08);
        host.Children.Add(overlay);
    }

    static void AddPlayerFrame(Grid host, string? imagePath)
    {
        var source = ToImageSource(imagePath);
        if (source == null)
            return;

        host.Children.Add(new Image
        {
            Source = source,
            Aspect = Aspect.AspectFit,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        });
    }

    static string RankBorder(int rank) => rank switch
    {
        1 => "#D4AE62",
        2 => "#BFC3C7",
        3 => "#B87333",
        _ => "#765021"
    };

    async Task AnimatePageAsync()
    {
        HallContainer.TranslationY = 12;
        await Task.WhenAll(
            HallContainer.FadeTo(1, 240, Easing.CubicOut),
            HallContainer.TranslateTo(0, 0, 240, Easing.CubicOut));
    }

    async void OnBackTapped(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
        else
            await Navigation.PushAsync(new MainPage());
    }

    async void OnShowAllTeamsClicked(object sender, EventArgs e) =>
        await DisplayAlert("أعضاء قاعة الأساطير", snapshot.TeamMembers.Count == 0 ? "لا توجد فرق مؤكدة حالياً." : string.Join("\n", snapshot.TeamMembers.Select((team, index) => $"#{index + 1} {team.DisplayName} | {team.FinalScore:0.#}%")), "حسناً");

    async void OnShowAllPlayersClicked(object sender, EventArgs e) =>
        await DisplayAlert("لاعبو قاعة الأساطير", snapshot.PlayerMembers.Count == 0 ? "لا يوجد لاعب مؤكد حالياً." : string.Join("\n", snapshot.PlayerMembers.Select((player, index) => $"#{index + 1} {player.DisplayName} | {player.Category} | {player.FinalScore:0.#}%")), "حسناً");

    async void OnTeamStatisticsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new TeamStatisticsPage());
    async void OnPlayerStatisticsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new PlayerStatisticsPage());

    async void OnBottomNavigation(string destination)
    {
        switch (destination)
        {
            case "HOME":
                await Navigation.PushAsync(new MainPage());
                break;
            case "PLAYERS":
                await Navigation.PushAsync(new PlayerProfilesPage());
                break;
            case "GAME":
                await OpenGameAsync();
                break;
            case "STORE":
                await Navigation.PushAsync(new GalleryEngine.Pages.GalleryPage());
                break;
        }
    }

    async Task OpenGameAsync()
    {
        var teams = (await TeamProfileService.LoadTeamsAsync())
            .Where(team => !string.IsNullOrWhiteSpace(team.TeamName))
            .Take(2)
            .ToList();
        if (teams.Count < 2)
        {
            await Navigation.PushAsync(new CreateTeamPage());
            return;
        }

        var team1 = teams[0];
        var team2 = teams[1];
        await Navigation.PushAsync(new GamePage(
            team1.TeamName,
            team2.TeamName,
            $"{team1.Player1} + {team1.Player2}",
            $"{team2.Player1} + {team2.Player2}",
            team1.TeamId,
            team2.TeamId,
            team1.Player1Id,
            team1.Player2Id,
            team2.Player1Id,
            team2.Player2Id,
            "محلي"));
    }

    async void OnSideMenuNavigation(string section)
    {
        switch (section)
        {
            case "HOME":
                await HallScrollView.ScrollToAsync(0, 0, true);
                break;
            case "TEAMS":
                await HallScrollView.ScrollToAsync(TeamsSection, ScrollToPosition.Start, true);
                break;
            case "PLAYERS":
                await HallScrollView.ScrollToAsync(PlayersSection, ScrollToPosition.Start, true);
                break;
            case "ACHIEVEMENTS":
                await Navigation.PushAsync(new HallCandidateCenterPage());
                break;
            case "HISTORY":
                await HallScrollView.ScrollToAsync(RecordsSection, ScrollToPosition.Start, true);
                break;
            case "STATS":
                await HallScrollView.ScrollToAsync(StatsSection, ScrollToPosition.Start, true);
                break;
        }
    }
}
