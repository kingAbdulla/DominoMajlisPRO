using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.Pages;

public partial class PlayerRankingsPage : ContentPage
{
    const int TableInitialCount = 10;

    PlayerRankingFilter _filter = PlayerRankingFilter.All;
    PlayerRankingSnapshot? _snapshot;
    bool _showAll;
    bool _isLoading;

    readonly Dictionary<string, PlayerVisualIdentity> _identities =
        new(StringComparer.OrdinalIgnoreCase);

    readonly List<(PlayerRankingFilter Filter, string Label, Border Chip)> _chips =
        new();

    bool Wide =>
        DeviceInfo.Idiom == DeviceIdiom.Tablet ||
        DeviceInfo.Idiom == DeviceIdiom.Desktop;

    public PlayerRankingsPage()
    {
        InitializeComponent();
        BuildFilterChips();
    }

    // =========================
    // LIFECYCLE / SYNC
    // =========================

    protected override void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.RankingsChanged -= OnDataShouldRefresh;
        AppEvents.RankingsChanged += OnDataShouldRefresh;

        AppEvents.PlayerProfileChanged -= OnDataShouldRefresh;
        AppEvents.PlayerProfileChanged += OnDataShouldRefresh;

        AppEvents.DataChanged -= OnDataShouldRefresh;
        AppEvents.DataChanged += OnDataShouldRefresh;

        AppEvents.CurrentUserChanged -= OnDataShouldRefresh;
        AppEvents.CurrentUserChanged += OnDataShouldRefresh;

        _ = ReloadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        AppEvents.RankingsChanged -= OnDataShouldRefresh;
        AppEvents.PlayerProfileChanged -= OnDataShouldRefresh;
        AppEvents.DataChanged -= OnDataShouldRefresh;
        AppEvents.CurrentUserChanged -= OnDataShouldRefresh;
    }

    void OnDataShouldRefresh()
    {
        // Always reload a fresh snapshot; never trust cached in-page state.
        MainThread.BeginInvokeOnMainThread(() => _ = ReloadAsync());
    }

    // =========================
    // LOAD
    // =========================

    async Task ReloadAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            _snapshot =
                await PlayerRankingService.GetSnapshotAsync(_filter);

            _showAll = false;

            await ResolveVisibleIdentitiesAsync();

            UpdateHeader();
            BuildContent();
        }
        catch
        {
            ShowEmptyState();
        }
        finally
        {
            _isLoading = false;
        }
    }

    async Task ResolveVisibleIdentitiesAsync()
    {
        if (_snapshot == null)
            return;

        var ids = VisibleEntries()
            .Select(e => e.PlayerId)
            .ToList();

        if (!string.IsNullOrWhiteSpace(_snapshot.CurrentPlayerId))
            ids.Add(_snapshot.CurrentPlayerId!);

        var missing = ids
            .Where(id => !string.IsNullOrWhiteSpace(id) &&
                         !_identities.ContainsKey(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missing.Count == 0)
            return;

        var resolved =
            await PlayerVisualIdentityResolver.ResolveManyAsync(missing);

        foreach (var pair in resolved)
            _identities[pair.Key] = pair.Value;
    }

    // =========================
    // VISIBLE ENTRY SELECTION
    // =========================

    List<PlayerRankingEntry> VisibleEntries()
    {
        if (_snapshot == null || !_snapshot.HasData)
            return new();

        var all = _snapshot.Entries;
        var visible = new List<PlayerRankingEntry>();

        // Hero + podium (first 3).
        visible.AddRange(all.Take(3));

        // Table (position 4+), capped unless expanded.
        var table = all.Skip(3);
        visible.AddRange(_showAll ? table : table.Take(TableInitialCount));

        return visible;
    }

    PlayerVisualIdentity? IdentityFor(string playerId) =>
        _identities.TryGetValue(playerId, out var identity)
            ? identity
            : null;

    // =========================
    // HEADER
    // =========================

    void UpdateHeader()
    {
        if (_snapshot == null)
            return;

        CoinsLabel.Text = _snapshot.Coins.ToString("N0");
        GemsLabel.Text = _snapshot.Gems.ToString("N0");

        var current = _snapshot.Entries
            .FirstOrDefault(e => e.IsCurrentUser);

        if (current != null)
        {
            var identity = IdentityFor(current.PlayerId);
            CurrentUserAvatar.Source =
                ToOptionalImageSource(identity?.Avatar?.PreviewImage) ??
                ResolveAvatarSource(current.Player);
        }
        else
        {
            CurrentUserAvatar.Source =
                InventoryDisplayResolver.ResolveImageSource(
                    "player_card.png",
                    "player_card.png");
        }

        LastUpdatedLabel.Text =
            $"آخر تحديث: {_snapshot.GeneratedAt:HH:mm}";
    }

    // =========================
    // CONTENT
    // =========================

    void BuildContent()
    {
        HeroContainer.Children.Clear();
        PodiumContainer.Children.Clear();
        TableContainer.Children.Clear();
        EmptyStateContainer.Children.Clear();

        if (_snapshot == null || !_snapshot.HasData)
        {
            ShowEmptyState();
            return;
        }

        EmptyStateContainer.IsVisible = false;
        TableCard.IsVisible = true;

        var entries = _snapshot.Entries;

        BuildHero(entries[0]);
        BuildPodium(entries.Take(3).ToList());
        BuildTable(entries.Skip(3).ToList());
    }

    // =========================
    // EMPTY STATE
    // =========================

    void ShowEmptyState()
    {
        HeroContainer.Children.Clear();
        PodiumContainer.Children.Clear();
        TableContainer.Children.Clear();
        TableCard.IsVisible = false;
        ShowMoreButton.IsVisible = false;
        EmptyStateContainer.Children.Clear();
        EmptyStateContainer.IsVisible = true;

        bool friends = _filter == PlayerRankingFilter.Friends;

        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#101016"),
            Stroke = Color.FromArgb("#D4AF37"),
            StrokeThickness = 1,
            Padding = new Thickness(24),
            StrokeShape = new RoundRectangle { CornerRadius = 22 }
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center
        };

        stack.Children.Add(new Image
        {
            Source = "rankings_gold.png",
            WidthRequest = 48,
            HeightRequest = 48,
            HorizontalOptions = LayoutOptions.Center
        });

        stack.Children.Add(new Label
        {
            Text = friends
                ? "لا يوجد أصدقاء بعد"
                : "لا توجد بيانات تصنيف بعد",
            TextColor = Colors.White,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

        stack.Children.Add(new Label
        {
            Text = friends
                ? "أضف أصدقاء ليظهروا في تصنيفك"
                : "ابدأ لعب المباريات ليظهر ترتيب اللاعبين",
            TextColor = Color.FromArgb("#9A9AA8"),
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center
        });

        card.Content = stack;
        EmptyStateContainer.Children.Add(card);
    }

    // =========================
    // HERO CARD
    // =========================

    void BuildHero(PlayerRankingEntry entry)
    {
        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#15110A"),
            Stroke = Color.FromArgb("#D9A441"),
            StrokeThickness = 1.5,
            Padding = new Thickness(14),
            StrokeShape = new RoundRectangle { CornerRadius = 24 }
        };

        var root = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        // Avatar + champion badge.
        var avatarStack = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        avatarStack.Children.Add(new Image
        {
            Source = "champion_gold.png",
            WidthRequest = 28,
            HeightRequest = 28,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center
        });
        avatarStack.Children.Add(BuildAvatar(entry, Wide ? 104 : 84));
        avatarStack.Children.Add(BuildPositionBadge(1));
        Grid.SetColumn(avatarStack, 0);
        root.Children.Add(avatarStack);

        // Center: name + rank + progress.
        var center = new VerticalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center
        };
        center.Children.Add(new Label
        {
            Text = entry.DisplayName,
            TextColor = Colors.White,
            FontSize = Wide ? 26 : 20,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        });
        center.Children.Add(BuildRankLine(entry));
        center.Children.Add(RankProgressVisualService.Build(entry.XP, 16, 24));
        Grid.SetColumn(center, 1);
        root.Children.Add(center);

        // Stats column (XP / matches / win rate / trust).
        var stats = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        stats.Children.Add(new Label
        {
            Text = "XP",
            TextColor = Color.FromArgb("#9A9AA8"),
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Center
        });
        stats.Children.Add(new Label
        {
            Text = entry.XP.ToString("N0"),
            TextColor = Color.FromArgb("#FFD700"),
            FontSize = Wide ? 24 : 20,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });
        stats.Children.Add(BuildMiniStat("المباريات", entry.Matches.ToString("N0")));
        stats.Children.Add(BuildMiniStat("الفوز", $"{entry.WinRate:0.#}%"));
        if (entry.TrustScore.HasValue)
            stats.Children.Add(BuildMiniStat("الثقة", $"{entry.TrustScore.Value}%"));
        Grid.SetColumn(stats, 2);
        root.Children.Add(stats);

        card.Content = root;
        HeroContainer.Children.Add(card);
    }

    View BuildMiniStat(string label, string value) =>
        new HorizontalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = label,
                    TextColor = Color.FromArgb("#9A9AA8"),
                    FontSize = 11,
                    VerticalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = value,
                    TextColor = Colors.White,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    VerticalTextAlignment = TextAlignment.Center
                }
            }
        };

    View BuildRankLine(PlayerRankingEntry entry)
    {
        var rank = PlayerRankService.Calculate(entry.XP);

        var stack = new HorizontalStackLayout
        {
            Spacing = 6
        };
        stack.Children.Add(new Image
        {
            Source = rank.RankIcon,
            WidthRequest = 22,
            HeightRequest = 22,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center
        });
        stack.Children.Add(new Label
        {
            Text = rank.DisplayName,
            TextColor = ParseColor(rank.RankColor, "#D4AF37"),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center
        });

        var badge = StatusBadge(entry);
        if (badge != null)
            stack.Children.Add(badge);

        return stack;
    }

    View? StatusBadge(PlayerRankingEntry entry)
    {
        string? text =
            entry.IsDeveloper ? "مطوّر" :
            entry.IsFounder ? "مؤسس" :
            entry.IsHonor ? "شرف" :
            null;

        if (text == null)
            return null;

        return new Border
        {
            BackgroundColor = Color.FromArgb("#1B1B24"),
            Stroke = Color.FromArgb(PlayerEngine.GetStatusColor(entry.Status)),
            StrokeThickness = 1,
            Padding = new Thickness(6, 1),
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new Label
            {
                Text = text,
                TextColor = Color.FromArgb(PlayerEngine.GetStatusColor(entry.Status)),
                FontSize = 10,
                FontAttributes = FontAttributes.Bold
            }
        };
    }

    View BuildPositionBadge(int position)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#D9A441"),
            StrokeThickness = 0,
            Padding = new Thickness(10, 2),
            HorizontalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new Label
            {
                Text = position.ToString(),
                TextColor = Colors.Black,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            }
        };
    }

    // =========================
    // TOP 3 PODIUM
    // =========================

    void BuildPodium(List<PlayerRankingEntry> top)
    {
        if (top.Count == 0)
            return;

        var grid = new Grid
        {
            ColumnSpacing = 8,
            Margin = new Thickness(0, 2, 0, 4),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        // RTL: column 0 = right. Put #2 on right, #1 center, #3 left.
        if (top.Count > 1)
        {
            var second = BuildPodiumCard(top[1], 2);
            Grid.SetColumn(second, 0);
            grid.Children.Add(second);
        }

        var first = BuildPodiumCard(top[0], 1);
        Grid.SetColumn(first, 1);
        grid.Children.Add(first);

        if (top.Count > 2)
        {
            var third = BuildPodiumCard(top[2], 3);
            Grid.SetColumn(third, 2);
            grid.Children.Add(third);
        }

        PodiumContainer.Children.Add(grid);
    }

    View BuildPodiumCard(PlayerRankingEntry entry, int position)
    {
        bool champion = position == 1;

        Color border = position switch
        {
            1 => Color.FromArgb("#D9A441"),
            2 => Color.FromArgb("#9AA7BF"),
            _ => Color.FromArgb("#C77B43")
        };

        var card = new Border
        {
            BackgroundColor = Color.FromArgb(champion ? "#1B1407" : "#101016"),
            Stroke = border,
            StrokeThickness = champion ? 2 : 1.2,
            Padding = new Thickness(10),
            Margin = champion
                ? new Thickness(0)
                : new Thickness(0, 22, 0, 0),
            StrokeShape = new RoundRectangle { CornerRadius = 20 }
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 5,
            HorizontalOptions = LayoutOptions.Center
        };

        if (champion)
            stack.Children.Add(new Image
            {
                Source = "champion_gold.png",
                WidthRequest = 24,
                HeightRequest = 24,
                HorizontalOptions = LayoutOptions.Center
            });

        stack.Children.Add(BuildAvatar(entry, champion ? 78 : 64));
        stack.Children.Add(BuildPositionBadge(position));

        stack.Children.Add(new Label
        {
            Text = entry.DisplayName,
            TextColor = Colors.White,
            FontSize = champion ? 15 : 13,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            HorizontalTextAlignment = TextAlignment.Center
        });

        var rank = PlayerRankService.Calculate(entry.XP);
        stack.Children.Add(new Label
        {
            Text = rank.DisplayName,
            TextColor = ParseColor(rank.RankColor, "#C9D5FF"),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            HorizontalTextAlignment = TextAlignment.Center
        });

        stack.Children.Add(new Label
        {
            Text = $"{entry.XP:N0} XP",
            TextColor = Color.FromArgb("#FFD700"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

        card.Content = stack;
        return card;
    }

    // =========================
    // TABLE
    // =========================

    void BuildTable(List<PlayerRankingEntry> rows)
    {
        TableContainer.Children.Clear();

        if (rows.Count == 0)
        {
            TableCard.IsVisible = false;
            ShowMoreButton.IsVisible = false;
            return;
        }

        TableCard.IsVisible = true;

        if (Wide)
            TableContainer.Children.Add(BuildTableHeaderRow());

        var visible = _showAll
            ? rows
            : rows.Take(TableInitialCount).ToList();

        bool alt = false;
        foreach (var entry in visible)
        {
            TableContainer.Children.Add(
                Wide ? BuildWideRow(entry, alt) : BuildCardRow(entry, alt));
            alt = !alt;
        }

        // Show more / hide control.
        if (rows.Count > TableInitialCount)
        {
            ShowMoreButton.IsVisible = true;
            ShowMoreLabel.Text = _showAll ? "إخفاء" : "عرض المزيد";
        }
        else
        {
            ShowMoreButton.IsVisible = false;
        }
    }

    View BuildTableHeaderRow()
    {
        var grid = WideRowGrid();
        AddCell(grid, 0, "#", header: true);
        AddCell(grid, 1, "اللاعب", header: true, start: true);
        AddCell(grid, 2, "الرتبة", header: true);
        AddCell(grid, 3, "التقدم", header: true);
        AddCell(grid, 4, "XP", header: true);
        AddCell(grid, 5, "الفوز", header: true);
        AddCell(grid, 6, "معدل الفوز", header: true);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#15151C"),
            StrokeThickness = 0,
            Padding = new Thickness(8, 8),
            Margin = new Thickness(0, 0, 0, 4),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = grid
        };
    }

    static Grid WideRowGrid() =>
        new()
        {
            ColumnSpacing = 6,
            VerticalOptions = LayoutOptions.Center,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(2.6, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

    static void AddCell(
        Grid grid,
        int column,
        string text,
        bool header = false,
        bool start = false)
    {
        var label = new Label
        {
            Text = text,
            TextColor = header
                ? Color.FromArgb("#9A9AA8")
                : Colors.White,
            FontSize = header ? 12 : 13,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = start
                ? TextAlignment.Start
                : TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        };
        Grid.SetColumn(label, column);
        grid.Children.Add(label);
    }

    View BuildWideRow(PlayerRankingEntry entry, bool alt)
    {
        var grid = WideRowGrid();

        AddCell(grid, 0, entry.Position.ToString());

        // Player (avatar + name).
        var playerCell = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center
        };
        playerCell.Children.Add(BuildAvatar(entry, 38));
        playerCell.Children.Add(PlayerNameSurface(entry.PlayerId, entry.DisplayName, 30));
        Grid.SetColumn(playerCell, 1);
        grid.Children.Add(playerCell);

        // Rank icon.
        var rank = PlayerRankService.Calculate(entry.XP);
        var rankIcon = new Image
        {
            Source = rank.RankIcon,
            WidthRequest = 26,
            HeightRequest = 26,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(rankIcon, 2);
        grid.Children.Add(rankIcon);

        // Progress (mini bar with %).
        var progress = BuildMiniProgress(entry.XP);
        Grid.SetColumn(progress, 3);
        grid.Children.Add(progress);

        AddCell(grid, 4, entry.XP.ToString("N0"));
        AddCell(grid, 5, entry.Wins.ToString("N0"));
        AddCell(grid, 6, $"{entry.WinRate:0.#}%");

        var background = entry.IsCurrentUser
            ? Color.FromArgb("#1E1808")
            : alt
                ? Color.FromArgb("#121218")
                : Colors.Transparent;

        return new Border
        {
            BackgroundColor = background,
            Stroke = entry.IsCurrentUser
                ? Color.FromArgb("#D4AF37")
                : Colors.Transparent,
            StrokeThickness = entry.IsCurrentUser ? 1 : 0,
            Padding = new Thickness(8, 6),
            Margin = new Thickness(0, 1),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = grid
        };
    }

    View BuildCardRow(PlayerRankingEntry entry, bool alt)
    {
        var grid = new Grid
        {
            ColumnSpacing = 10,
            VerticalOptions = LayoutOptions.Center,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        AddCell(grid, 0, entry.Position.ToString());

        var avatar = BuildAvatar(entry, 46);
        Grid.SetColumn(avatar, 1);
        grid.Children.Add(avatar);

        // Middle: name + rank + mini progress.
        var middle = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center
        };
        middle.Children.Add(PlayerNameSurface(entry.PlayerId, entry.DisplayName, 32));
        var rank = PlayerRankService.Calculate(entry.XP);
        middle.Children.Add(new Label
        {
            Text = rank.DisplayName,
            TextColor = ParseColor(rank.RankColor, "#C9D5FF"),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1
        });
        middle.Children.Add(BuildMiniProgress(entry.XP));
        Grid.SetColumn(middle, 2);
        grid.Children.Add(middle);

        // Right: XP + win rate compact.
        var compact = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };
        compact.Children.Add(new Label
        {
            Text = $"{entry.XP:N0}",
            TextColor = Color.FromArgb("#FFD700"),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.End
        });
        compact.Children.Add(new Label
        {
            Text = $"{entry.WinRate:0.#}%",
            TextColor = Color.FromArgb("#9A9AA8"),
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.End
        });
        Grid.SetColumn(compact, 3);
        grid.Children.Add(compact);

        var background = entry.IsCurrentUser
            ? Color.FromArgb("#1E1808")
            : alt
                ? Color.FromArgb("#121218")
                : Color.FromArgb("#0E0E14");

        return new Border
        {
            BackgroundColor = background,
            Stroke = entry.IsCurrentUser
                ? Color.FromArgb("#D4AF37")
                : Color.FromArgb("#1E1E28"),
            StrokeThickness = 1,
            Padding = new Thickness(10),
            Margin = new Thickness(0, 3),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = grid
        };
    }

    static View PlayerNameSurface(string playerId, string displayName, double height)
    {
        return new GalleryEngine.Components.RuntimeNamePlateView
        {
            OwnerId = playerId,
            OwnerKind = "Player",
            DisplayText = displayName,
            RenderingContext = GalleryEngine.Models.NameSurfaceRenderingContext.Rankings,
            HeightRequest = height,
            MinimumWidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 88 : 120,
            HorizontalOptions = LayoutOptions.Fill
        };
    }

    View BuildMiniProgress(int xp)
    {
        var visual = RankProgressVisualService.Resolve(xp);

        var container = new Grid { HeightRequest = 14 };

        container.Children.Add(new Border
        {
            BackgroundColor = Color.FromArgb("#26324D"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 7 }
        });

        double progress = Math.Clamp(visual.Progress, 0, 1);
        double filled = Math.Max(progress, 0.0001);
        double rest = Math.Max(1 - progress, 0.0001);

        var fillGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(filled, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(rest, GridUnitType.Star) }
            }
        };
        var fill = new Border
        {
            BackgroundColor = visual.CurrentColor,
            StrokeThickness = 0,
            IsVisible = progress > 0,
            StrokeShape = new RoundRectangle { CornerRadius = 7 }
        };
        Grid.SetColumn(fill, 0);
        fillGrid.Children.Add(fill);
        container.Children.Add(fillGrid);

        container.Children.Add(new Label
        {
            Text = $"{visual.Percent}%",
            TextColor = Colors.White,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        });

        return container;
    }

    // =========================
    // AVATAR (canonical identity path)
    // =========================

    View BuildAvatar(PlayerRankingEntry entry, double size)
    {
        var identity = IdentityFor(entry.PlayerId);

        var avatar = new Grid
        {
            WidthRequest = size,
            HeightRequest = size
        };

        avatar.Add(new Image
        {
            Source =
                ToOptionalImageSource(identity?.Avatar?.PreviewImage) ??
                ResolveAvatarSource(entry.Player),
            Aspect = Aspect.AspectFill,
            WidthRequest = size,
            HeightRequest = size
        });

        AddFrameOverlay(avatar, identity?.Frame?.PreviewImage);
        AddPlayerEffect(avatar, identity?.Effect);

        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            BackgroundColor = Color.FromArgb("#151515"),
            Stroke = identity?.Frame == null
                ? Color.FromArgb(PlayerEngine.GetStatusColor(entry.Status))
                : Colors.Transparent,
            StrokeThickness = 1.5,
            StrokeShape = new RoundRectangle { CornerRadius = 999 },
            Content = avatar
        };
    }

    static ImageSource ResolveAvatarSource(PlayerProfileModel player)
    {
        var candidate =
            player.UseCustomAvatar &&
            !string.IsNullOrWhiteSpace(player.AvatarPath)
                ? player.AvatarPath
                : !string.IsNullOrWhiteSpace(player.ProfileImagePath)
                    ? player.ProfileImagePath
                    : !string.IsNullOrWhiteSpace(player.AvatarImage)
                        ? player.AvatarImage
                        : player.BuiltInAvatar;

        return InventoryDisplayResolver.ResolveImageSource(
            candidate,
            "player_card.png");
    }

    static ImageSource? ToOptionalImageSource(string? path) =>
        InventoryDisplayResolver.ResolveOptionalImageSource(path);

    static void AddFrameOverlay(Grid container, string? imagePath)
    {
        var source = ToOptionalImageSource(imagePath);
        if (source == null)
            return;

        container.Add(new Image
        {
            Source = source,
            Aspect = Aspect.AspectFit,
            InputTransparent = true
        });
    }

    static void AddPlayerEffect(Grid container, CatalogAssetDisplay? effect)
    {
        if (effect == null)
            return;

        var overlay = new Image
        {
            Aspect = Aspect.AspectFit,
            InputTransparent = true
        };
        PlayerEffectEngine.Apply(overlay, effect, 1.14);
        container.Add(overlay);
    }

    // =========================
    // FILTER CHIPS
    // =========================

    void BuildFilterChips()
    {
        _chips.Clear();
        FilterContainer.Children.Clear();

        AddChip(PlayerRankingFilter.All, "الكل");
        AddChip(PlayerRankingFilter.Friends, "أصدقائي");
        AddChip(PlayerRankingFilter.Season, "هذا الموسم");
        AddChip(PlayerRankingFilter.TopXP, "الأعلى XP");
        AddChip(PlayerRankingFilter.TopWins, "الأعلى فوزاً");
        AddChip(PlayerRankingFilter.TopTrust, "الأعلى Trust");
        AddChip(PlayerRankingFilter.MostActive, "الأكثر نشاطاً");

        UpdateChipStyles();
    }

    void AddChip(PlayerRankingFilter filter, string label)
    {
        var chip = new Border
        {
            Padding = new Thickness(14, 8),
            BackgroundColor = Color.FromArgb("#17171F"),
            Stroke = Color.FromArgb("#292938"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new Label
            {
                Text = label,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 13
            }
        };

        chip.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnFilterSelected(filter))
        });

        _chips.Add((filter, label, chip));
        FilterContainer.Children.Add(chip);
    }

    void OnFilterSelected(PlayerRankingFilter filter)
    {
        if (_filter == filter)
            return;

        _filter = filter;
        UpdateChipStyles();
        _ = ReloadAsync();
    }

    void UpdateChipStyles()
    {
        foreach (var (filter, _, chip) in _chips)
        {
            bool active = filter == _filter;
            chip.BackgroundColor = active
                ? Color.FromArgb("#2A2008")
                : Color.FromArgb("#17171F");
            chip.Stroke = active
                ? Color.FromArgb("#D4AF37")
                : Color.FromArgb("#292938");

            if (chip.Content is Label label)
                label.TextColor = active
                    ? Color.FromArgb("#FFD700")
                    : Colors.White;
        }
    }

    // =========================
    // SHOW MORE / HIDE
    // =========================

    void OnShowMoreClicked(object sender, TappedEventArgs e)
    {
        _showAll = !_showAll;
        _ = ToggleTableAsync();
    }

    async Task ToggleTableAsync()
    {
        if (_snapshot == null)
            return;

        await ResolveVisibleIdentitiesAsync();
        BuildTable(_snapshot.Entries.Skip(3).ToList());
    }

    // =========================
    // SEGMENTED CONTROL
    // =========================

    async void OnTeamsTabClicked(object sender, TappedEventArgs e)
    {
        // Return to the team rankings page rather than stacking a new one.
        if (Navigation.NavigationStack.Count > 1)
        {
            var previous =
                Navigation.NavigationStack[Navigation.NavigationStack.Count - 2];

            if (previous is RankingsPage)
            {
                await Navigation.PopAsync();
                return;
            }
        }

        await Navigation.PushAsync(new RankingsPage());
    }

    void OnPlayersTabClicked(object sender, TappedEventArgs e)
    {
        // Already on the players tab; just refresh.
        _ = ReloadAsync();
    }

    // =========================
    // BACK
    // =========================

    async void OnBackClicked(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    static Color ParseColor(string value, string fallback)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value)
                ? Color.FromArgb(fallback)
                : Color.FromArgb(value);
        }
        catch
        {
            return Color.FromArgb(fallback);
        }
    }
}
