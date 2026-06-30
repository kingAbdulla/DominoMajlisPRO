using System.Collections.ObjectModel;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;
using DominoMajlisPRO.GalleryEngine.Services;
using Microsoft.Maui.Controls.Shapes;

namespace DominoMajlisPRO.Pages;

public partial class HistoryPage : ContentPage
{
    const int PageSize = 10;

    List<SavedMatch> allMatches = new();
    List<SavedMatch> filteredMatches = new();
    ObservableCollection<SavedMatch> displayedMatches = new();

    string currentFilter = "ALL";
    int loadedCount = 0;
    bool isExpanded = false;

    CancellationTokenSource? searchDelay;

    public HistoryPage()
    {
        InitializeComponent();

        HistoryCollection.ItemsSource =
            displayedMatches;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AppEvents.DataChanged -= OnHistoryDataChanged;
        AppEvents.MatchesChanged -= OnHistoryDataChanged;
        AppEvents.TeamsChanged -= OnHistoryDataChanged;
        AppEvents.PlayerProfileChanged -= OnHistoryDataChanged;
        AppEvents.TeamAssetsChanged -= OnHistoryTeamAssetsChanged;

        AppEvents.DataChanged += OnHistoryDataChanged;
        AppEvents.MatchesChanged += OnHistoryDataChanged;
        AppEvents.TeamsChanged += OnHistoryDataChanged;
        AppEvents.PlayerProfileChanged += OnHistoryDataChanged;
        AppEvents.TeamAssetsChanged += OnHistoryTeamAssetsChanged;

        await LoadHistoryAsync();
    }
    // Subscribe to data change events to refresh history when matches, teams, or player profiles change
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        AppEvents.DataChanged -= OnHistoryDataChanged;
        AppEvents.MatchesChanged -= OnHistoryDataChanged;
        AppEvents.TeamsChanged -= OnHistoryDataChanged;
        AppEvents.PlayerProfileChanged -= OnHistoryDataChanged;
        AppEvents.TeamAssetsChanged -= OnHistoryTeamAssetsChanged;
    }

    async void OnHistoryDataChanged()
    {
        await MainThread.InvokeOnMainThreadAsync(
            async () =>
            {
                await LoadHistoryAsync();
            });
    }
    // Unsubscribe from events when leaving the page to prevent memory leaks

    async Task LoadHistoryAsync()
    {
        allMatches =
            await GameService.LoadMatchesAsync();

        await NormalizeMatchesAsync(allMatches);
        UpdateDashboard();
        ApplyFilters(reset: true);
    }

    async Task NormalizeMatchesAsync(List<SavedMatch> matches)
    {
        var identities = await TeamIdentityResolver.ResolveManyAsync(
            matches.SelectMany(match =>
                new[] { match.Team1Id, match.Team2Id }));
        foreach (var match in matches)
        {
            match.Team1Name ??= "الفريق الأول";
            match.Team2Name ??= "الفريق الثاني";
            match.WinnerTeamName ??= "بدون فائز";
            match.Team1Players ??= "";
            match.Team2Players ??= "";
            identities.TryGetValue(match.Team1Id, out var team1);
            identities.TryGetValue(match.Team2Id, out var team2);
            match.Team1Emblem =
                team1?.EmblemImagePath ??
                (string.IsNullOrWhiteSpace(match.Team1Emblem)
                    ? "shield_3d.png"
                    : match.Team1Emblem);
            match.Team2Emblem =
                team2?.EmblemImagePath ??
                (string.IsNullOrWhiteSpace(match.Team2Emblem)
                    ? "shield_3d.png"
                    : match.Team2Emblem);
            match.Team1ColorHex =
                team1?.TeamColorHex ?? match.Team1ColorHex;
            match.Team2ColorHex =
                team2?.TeamColorHex ?? match.Team2ColorHex;
        }
    }

    void OnHistoryTeamAssetsChanged(string teamId) => OnHistoryDataChanged();

    void UpdateDashboard()
    {
        TotalMatchesLabel.Text =
            allMatches.Count.ToString();

        TotalMelesLabel.Text =
            allMatches.Count(x => x.HasMeles).ToString();

        UnfinishedMatchesLabel.Text =
            allMatches.Count(x => !x.IsFinished).ToString();

        CompletedMatchesLabel.Text =
            allMatches.Count(x => x.IsFinished).ToString();
    }

    async void OnBackClicked(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }

    async void OnDeleteAllClicked(object sender, EventArgs e)
    {
        bool confirm =
            await DisplayAlert(
                "حذف جميع المباريات",
                "سيتم حذف جميع المباريات نهائياً",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        await GameService.DeleteAllMatches();

        await RankingService.RebuildAllRankingsAsync();
        AppEvents.RaiseDataChanged();

        await LoadHistoryAsync();
    }

    async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        searchDelay?.Cancel();
        searchDelay = new CancellationTokenSource();

        try
        {
            await Task.Delay(250, searchDelay.Token);
            ApplyFilters(reset: true);
        }
        catch
        {
        }
    }

    void OnAllFilterTapped(object sender, TappedEventArgs e)
    {
        currentFilter = "ALL";
        UpdateFilterUI();
        ApplyFilters(reset: true);
    }

    void OnFinishedFilterTapped(object sender, TappedEventArgs e)
    {
        currentFilter = "FINISHED";
        UpdateFilterUI();
        ApplyFilters(reset: true);
    }

    void OnUnfinishedFilterTapped(object sender, TappedEventArgs e)
    {
        currentFilter = "UNFINISHED";
        UpdateFilterUI();
        ApplyFilters(reset: true);
    }

    void OnMelesFilterTapped(object sender, TappedEventArgs e)
    {
        currentFilter = "MELES";
        UpdateFilterUI();
        ApplyFilters(reset: true);
    }

    void ApplyFilters(bool reset)
    {
        IEnumerable<SavedMatch> query =
            allMatches;

        string search =
            SearchEntry.Text?.Trim().ToLower() ?? "";

        if (!string.IsNullOrWhiteSpace(search))
        {
            query =
                query.Where(x =>
                    (x.Team1Name ?? "").ToLower().Contains(search) ||
                    (x.Team2Name ?? "").ToLower().Contains(search) ||
                    (x.WinnerTeamName ?? "").ToLower().Contains(search));
        }

        query =
            currentFilter switch
            {
                "FINISHED" => query.Where(x => x.IsFinished),
                "UNFINISHED" => query.Where(x => !x.IsFinished),
                "MELES" => query.Where(x => x.HasMeles),
                _ => query
            };

        filteredMatches =
            query
            .OrderByDescending(x => x.MatchDate)
            .ToList();

        if (reset)
        {
            displayedMatches.Clear();
            loadedCount = 0;
            isExpanded = false;
        }

        LoadNextBatch();
    }
    // Load the next batch of matches for pagination
    void LoadNextBatch()
    {
        var next =
            filteredMatches
            .Skip(loadedCount)
            .Take(PageSize)
            .ToList();

        foreach (var match in next)
            displayedMatches.Add(match);

        loadedCount += next.Count;

        if (loadedCount >= filteredMatches.Count)
        {
            LoadMoreButton.Text = "عرض أقل";
            isExpanded = true;
            LoadMoreButton.IsVisible =
                filteredMatches.Count > PageSize;
        }
        else
        {
            LoadMoreButton.Text = "عرض المزيد";
            isExpanded = false;
            LoadMoreButton.IsVisible =
                filteredMatches.Count > PageSize;
        }
    }
    // Handle Load More / Show Less button click
    void OnLoadMoreClicked(object sender, EventArgs e)
    {
        if (isExpanded)
        {
            displayedMatches.Clear();

            loadedCount = 0;
            isExpanded = false;

            LoadNextBatch();

            LoadMoreButton.Text = "عرض المزيد";
            return;
        }

        LoadNextBatch();
    }

    async void OnDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.BindingContext is SavedMatch match)
        {
            await Navigation.PushAsync(
                new MatchDetailsPage(match));
        }
    }

    async void OnResumeClicked(object sender, EventArgs e)
    {
        if (sender is Button button &&
            button.BindingContext is SavedMatch match &&
            !match.IsFinished)
        {
            await Navigation.PushAsync(
                new GamePage(match));
        }
    }

    async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.BindingContext is not SavedMatch match)
            return;

        bool confirm =
            await DisplayAlert(
                "حذف المباراة",
                "هل تريد حذف المباراة؟",
                "حذف",
                "إلغاء");

        if (!confirm)
            return;

        await GameService.DeleteMatchAsync(match);

        await RankingService.RebuildAllRankingsAsync();
        AppEvents.RaiseDataChanged();

        await LoadHistoryAsync();
    }

    void UpdateFilterUI()
    {
        ResetFilter(AllFilter);
        ResetFilter(FinishedFilter);
        ResetFilter(UnfinishedFilter);
        ResetFilter(MelesFilter);

        switch (currentFilter)
        {
            case "ALL":
                HighlightFilter(AllFilter);
                break;
            case "FINISHED":
                HighlightFilter(FinishedFilter);
                break;
            case "UNFINISHED":
                HighlightFilter(UnfinishedFilter);
                break;
            case "MELES":
                HighlightFilter(MelesFilter);
                break;
        }
    }

    void HighlightFilter(Border border)
    {
        border.BackgroundColor =
            Color.FromArgb("#FFD700");

        border.StrokeThickness = 0;
    }

    void ResetFilter(Border border)
    {
        border.BackgroundColor =
            Color.FromArgb("#111111");

        border.Stroke =
            Color.FromArgb("#333333");

        border.StrokeThickness = 1;
    }
}

