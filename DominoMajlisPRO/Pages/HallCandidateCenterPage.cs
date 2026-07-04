using DominoMajlisPRO.Services;
using DominoMajlisPRO.Models;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace DominoMajlisPRO.Pages;

public sealed class HallCandidateCenterPage : ContentPage
{
    readonly VerticalStackLayout content = new();
    readonly Entry searchEntry = new() { Placeholder = "بحث", FontFamily = "Tajawal-Regular", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#8F8063") };
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
    string mode = "teams";
    string filter = "الكل";
    string sort = "Score";
    int visibleCount = 10;

    public HallCandidateCenterPage()
    {
        Title = "مركز المرشحين";
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Color.FromArgb(StatisticsDashboardUi.PageBackground);
        BuildShell();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await IsDeveloperAsync())
        {
            Content = StatisticsDashboardUi.Frame(
                StatisticsDashboardUi.Label("غير مصرح", 22, Color.FromArgb(StatisticsDashboardUi.Gold), true),
                18,
                "#5B3B18",
                "#090909",
                18);
            await DisplayAlert("غير مصرح", "مركز المرشحين متاح للمطور فقط.", "حسناً");
            if (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync();
            return;
        }

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
        AppEvents.PlayerProfileChanged -= OnDataChanged;
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;

        AppEvents.MatchesChanged += OnDataChanged;
        AppEvents.TeamsChanged += OnDataChanged;
        AppEvents.PlayerProfileChanged += OnDataChanged;
        AppEvents.RankingsChanged += OnDataChanged;
        AppEvents.TeamAssetsChanged += OnTeamChanged;
        AppEvents.StoreProgressChanged += OnStoreChanged;
        AppEvents.StoreEconomyChanged += OnStoreChanged;
    }

    void Unsubscribe()
    {
        AppEvents.MatchesChanged -= OnDataChanged;
        AppEvents.TeamsChanged -= OnDataChanged;
        AppEvents.PlayerProfileChanged -= OnDataChanged;
        AppEvents.RankingsChanged -= OnDataChanged;
        AppEvents.TeamAssetsChanged -= OnTeamChanged;
        AppEvents.StoreProgressChanged -= OnStoreChanged;
        AppEvents.StoreEconomyChanged -= OnStoreChanged;
    }

    async void OnDataChanged()
    {
        HallOfFameService.InvalidateCache();
        HallStatisticsDashboardService.Invalidate();
        await MainThread.InvokeOnMainThreadAsync(async () => await LoadAsync(true));
    }

    void OnTeamChanged(string teamId) => OnDataChanged();
    void OnStoreChanged(string playerId) => OnDataChanged();

    void BuildShell()
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            Padding = DeviceInfo.Idiom == DeviceIdiom.Phone ? new Thickness(12, 10) : new Thickness(24, 18),
            RowSpacing = 10
        };

        var header = new VerticalStackLayout { Spacing = 8 };
        header.Children.Add(StatisticsDashboardUi.Label("مركز المرشحين", 24, Color.FromArgb(StatisticsDashboardUi.Gold), true));
        header.Children.Add(CreateToolbar());
        root.Add(header);

        content.Spacing = 10;
        var scroll = new ScrollView { Content = content };
        Grid.SetRow(scroll, 1);
        root.Add(scroll);
        Content = root;
    }

    View CreateToolbar()
    {
        searchEntry.TextChanged += async (s, e) => await RenderAsync();

        var controls = new FlexLayout
        {
            Wrap = FlexWrap.Wrap,
            Direction = FlexDirection.Row,
            JustifyContent = FlexJustify.End,
            AlignItems = FlexAlignItems.Center
        };
        searchEntry.WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 180 : 260;
        controls.Children.Add(searchEntry);
        controls.Children.Add(Button("الفرق / اللاعبون", async () =>
        {
            mode = mode == "teams" ? "players" : "teams";
            visibleCount = 10;
            await RenderAsync();
        }));
        controls.Children.Add(Button("فلتر", OnFilterAsync));
        controls.Children.Add(Button("ترتيب", OnSortAsync));
        controls.Children.Add(Button("تحديث", async () => await LoadAsync(true)));
        return controls;
    }

    async Task LoadAsync(bool force = false)
    {
        snapshot = await HallOfFameService.LoadAsync(force);
        await RenderAsync();
    }

    async Task RenderAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            content.Children.Clear();
            content.Children.Add(CreateVerificationCard());
            if (mode == "teams")
                RenderTeamCandidates();
            else
                RenderPlayerCandidates();
        });
    }

    void RenderTeamCandidates()
    {
        var query = ApplyCommon(snapshot.TeamCandidates.Cast<HallEvaluationBase>())
            .Cast<HallTeamEvaluation>()
            .ToList();
        content.Children.Add(StatisticsDashboardUi.Label($"مرشحو الفرق: {query.Count}", 16, Color.FromArgb(StatisticsDashboardUi.Gold), true));
        foreach (var item in query.Take(visibleCount))
            content.Children.Add(CreateCandidateCard(item.DisplayName, item.Category, item.PublicStatus, item.Requirements, item.MissingRequirements, item.BlockingArticle, item.EstimatedRemaining));
        AddMoreIfNeeded(query.Count);
    }

    void RenderPlayerCandidates()
    {
        var query = ApplyCommon(snapshot.PlayerCandidates.Cast<HallEvaluationBase>())
            .Cast<HallPlayerEvaluation>()
            .ToList();
        content.Children.Add(StatisticsDashboardUi.Label($"مرشحو اللاعبين: {query.Count}", 16, Color.FromArgb(StatisticsDashboardUi.Gold), true));
        foreach (var item in query.Take(visibleCount))
            content.Children.Add(CreateCandidateCard(item.DisplayName, item.Category, item.PublicStatus, item.Requirements, item.MissingRequirements, item.BlockingArticle, item.EstimatedRemaining));
        AddMoreIfNeeded(query.Count);
    }

    IEnumerable<HallEvaluationBase> ApplyCommon(IEnumerable<HallEvaluationBase> source)
    {
        string term = searchEntry.Text?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(term))
            source = source.Where(item => item.Audit.SubjectName.Contains(term, StringComparison.OrdinalIgnoreCase));
        source = filter switch
        {
            "قريب" => source.Where(item => item.Decision == HallDecision.Watch),
            "تحقيق" => source.Where(item => item.Decision == HallDecision.Investigation),
            "موقوف" => source.Where(item => item.Decision == HallDecision.BlockedByConfirmedEvidence),
            _ => source
        };
        return sort switch
        {
            "Trust" => source.OrderByDescending(item => item.Requirements.FirstOrDefault(r => r.Article is "T2" or "P6")?.CurrentValue ?? 0),
            "Missing" => source.OrderBy(item => item.MissingRequirements.Count),
            _ => source.OrderByDescending(item => item.FinalScore)
        };
    }

    View CreateVerificationCard()
    {
        var failed = snapshot.Verification.Checks.Where(item => !item.Passed).Select(item => item.Name).ToList();
        return StatisticsDashboardUi.Frame(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                StatisticsDashboardUi.Label(snapshot.Verification.Passed ? "التحقق الدستوري مكتمل" : "يوجد تحقق يحتاج مراجعة", 14, Color.FromArgb(snapshot.Verification.Passed ? "#69D84F" : "#F4B942"), true),
                StatisticsDashboardUi.Label(failed.Count == 0 ? "كل الفحوصات الأساسية ناجحة." : string.Join("، ", failed.Take(4)), 11, Color.FromArgb(StatisticsDashboardUi.Muted), false, TextAlignment.Center, 3)
            }
        }, 16, "#5B3B18", "#101010", 10);
    }

    View CreateCandidateCard(string name, string category, SafeStatusResult status, IReadOnlyList<HallRequirement> requirements, IReadOnlyList<string> missing, string blocking, string remaining)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(StatisticsDashboardUi.Label(name, 18, Colors.White, true));
        stack.Children.Add(StatisticsDashboardUi.Label(category, 12, Color.FromArgb(StatisticsDashboardUi.Gold), true));
        stack.Children.Add(StatisticsDashboardUi.StatusBadge(status));
        stack.Children.Add(ProgressGrid(requirements));
        stack.Children.Add(StatisticsDashboardUi.Label(missing.Count == 0 ? "لا توجد متطلبات ناقصة" : string.Join("\n", missing.Take(5)), 11, Color.FromArgb(StatisticsDashboardUi.Muted), false, TextAlignment.End, 5));
        stack.Children.Add(StatisticsDashboardUi.Label(string.IsNullOrWhiteSpace(blocking) ? $"المتبقي: {remaining}" : $"المادة المانعة: {blocking} | {remaining}", 11, Color.FromArgb(StatisticsDashboardUi.Gold), false, TextAlignment.End, 2));
        return StatisticsDashboardUi.Frame(stack, 18, "#5B3B18", "#090909", 12);
    }

    View ProgressGrid(IReadOnlyList<HallRequirement> requirements)
    {
        var mapped = new[]
        {
            ("Trust", requirements.FirstOrDefault(r => r.Article is "T2" or "P6")),
            ("Legacy", requirements.FirstOrDefault(r => r.Article is "T4" or "P5")),
            ("Matches", requirements.FirstOrDefault(r => r.Article is "T3" or "P2")),
            ("WinRate", requirements.FirstOrDefault(r => r.Article is "T5" or "P4")),
            ("Achievement", requirements.FirstOrDefault(r => r.Article is "T1" or "P7")),
            ("Integrity", requirements.FirstOrDefault(r => r.Article is "T7" or "P9"))
        };
        return StatisticsDashboardUi.MetricGrid(mapped.Select(item =>
        {
            double value = item.Item2 == null ? 0 : item.Item2.RequiredValue <= 0 ? 1 : Math.Clamp(item.Item2.CurrentValue / item.Item2.RequiredValue, 0, 1);
            return StatisticsDashboardUi.Metric("target_3d.png", $"{value * 100:0}%", item.Item1);
        }));
    }

    void AddMoreIfNeeded(int total)
    {
        if (total <= visibleCount)
            return;
        content.Children.Add(Button("عرض المزيد", async () =>
        {
            visibleCount += 10;
            await RenderAsync();
        }));
    }

    async Task OnFilterAsync()
    {
        string action = await DisplayActionSheet("فلتر", "إلغاء", null, "الكل", "قريب", "تحقيق", "موقوف") ?? "";
        if (string.IsNullOrWhiteSpace(action) || action == "إلغاء")
            return;
        filter = action;
        visibleCount = 10;
        await RenderAsync();
    }

    async Task OnSortAsync()
    {
        string action = await DisplayActionSheet("ترتيب", "إلغاء", null, "Score", "Trust", "Missing") ?? "";
        if (string.IsNullOrWhiteSpace(action) || action == "إلغاء")
            return;
        sort = action;
        await RenderAsync();
    }

    static Button Button(string text, Func<Task> action)
    {
        var button = StatisticsDashboardUi.CommandButton(text);
        button.Clicked += async (s, e) => await action();
        return button;
    }

    static async Task<bool> IsDeveloperAsync()
    {
        try
        {
            var user = await ApplicationUserService.GetCurrentUserAsync();
            return user.Role == ApplicationUserRole.Developer;
        }
        catch
        {
            return false;
        }
    }
}
