using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Pages;

public partial class StatisticsPage : ContentPage
{
    public StatisticsPage()
    {
        InitializeComponent();

        LoadStatistics();
    }

    async void LoadStatistics()
    {
        List<SavedMatch> matches =
            await GameService.LoadMatchesAsync();

        List<TeamProfileModel> teams =
            await RankingService.LoadTeamsAsync();

        // =========================
        // TOTAL MATCHES
        // =========================

        int totalMatches =
            matches.Count;

        TotalMatchesLabel.Text =
            totalMatches.ToString();

        // =========================
        // MELES COUNT
        // =========================

        int melesCount =
            matches.Count(x => x.HasMeles);

        MelesMatchesLabel.Text =
            melesCount.ToString();

        // =========================
        // TOTAL POINTS
        // =========================

        int totalPoints =
            matches.Sum(x =>
                x.Team1Score +
                x.Team2Score);

        TotalPointsLabel.Text =
            totalPoints.ToString("N0");

        // =========================
        // AVERAGE DURATION
        // =========================

        int averageDuration =
            matches.Count == 0
            ? 0
            : (int)matches
                .Average(x =>
                    x.MatchDurationMinutes);

        AverageDurationLabel.Text =
            $"{averageDuration} دقيقة";

        // =========================
        // BEST PLAYER
        // =========================

        TeamProfileModel? bestPlayer =
            teams
            .OrderByDescending(x => x.WinRate)
            .ThenByDescending(x => x.Wins)
            .FirstOrDefault();

        if (bestPlayer != null)
        {
            BestPlayerLabel.Text =
                bestPlayer.TeamName;

            BestPlayerWinRateLabel.Text =
                $"{bestPlayer.WinRate}% نسبة فوز";

            BestPlayerProgress.Progress =
                bestPlayer.WinRate / 100.0;
        }
        else
        {
            BestPlayerLabel.Text =
                "لا يوجد";

            BestPlayerWinRateLabel.Text =
                "0%";

            BestPlayerProgress.Progress =
                0;
        }

        // =========================
        // MELES %
        // =========================

        double melesPercentage =
            totalMatches == 0
            ? 0
            : ((double)melesCount /
               totalMatches) * 100;

        MelesPercentageLabel.Text =
            $"{Math.Round(melesPercentage)}%";

        MelesProgress.Progress =
            melesPercentage / 100.0;

        // =========================
        // XP LEADER
        // =========================

        TeamProfileModel? xpLeader =
            teams
            .OrderByDescending(x => x.XP)
            .FirstOrDefault();

        if (xpLeader != null)
        {
            XpLeaderLabel.Text =
                xpLeader.TeamName;

            XpLeaderXpLabel.Text =
                $"{xpLeader.XP} XP";
        }
        else
        {
            XpLeaderLabel.Text =
                "لا يوجد";

            XpLeaderXpLabel.Text =
                "0 XP";
        }

        // =========================
        // HIGHEST SCORE
        // =========================

        int highestScore =
            matches.Count == 0
            ? 0
            : matches.Max(x =>
                Math.Max(
                    x.Team1Score,
                    x.Team2Score));

        HighestScoreLabel.Text =
            highestScore.ToString();

        // =========================
        // LONGEST MATCH
        // =========================

        int longestMatch =
            matches.Count == 0
            ? 0
            : matches.Max(x =>
                x.MatchDurationMinutes);

        LongestMatchLabel.Text =
            $"{longestMatch} دقيقة";

        // =========================
        // DASHBOARD SUMMARY
        // =========================

        string bestName =
            bestPlayer?.TeamName ??
            "لا يوجد";

        DashboardSummaryLabel.Text =
            $"📜 المباريات: {totalMatches}\n" +
            $"🏆 الأفضل: {bestName}\n" +
            $"🎯 النقاط: {totalPoints:N0}\n" +
            $"🔥 الملص: {melesCount}";
    }

    async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}