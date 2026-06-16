using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class SeasonManager
{
    public const int SeasonDurationDays = 30;


    // This method ensures that the season information for each team is up to date. It checks if the current season has expired and starts a new season if necessary.
    public static void EnsureSeason(
      List<TeamProfileModel> teams)
    {
        if (teams == null || teams.Count == 0)
            return;

        DateTime now =
            DateTime.Now;

        int currentSeasonId =
            GetCurrentSeasonNumber(teams);

        bool seasonExpired =
            teams.Any(x =>
                x.SeasonEndDate != default &&
                now > x.SeasonEndDate);

        if (seasonExpired)
        {
            currentSeasonId++;
        }

        foreach (var team in teams)
        {
            if (team.CurrentSeasonId == 0 ||
                team.CurrentSeasonId < currentSeasonId ||
                team.SeasonEndDate == default ||
                now > team.SeasonEndDate)
            {
                StartNewSeason(
                    team,
                    currentSeasonId,
                    now);
            }
        }
    }

    static void StartNewSeason(
        TeamProfileModel team,
        int seasonId,
        DateTime now)
    {
        team.CurrentSeasonId =
            seasonId;

        team.SeasonStartDate =
            now.Date;

        team.SeasonEndDate =
            now.Date.AddDays(SeasonDurationDays);

        team.SeasonXP = 0;

        team.HasSeasonReward = false;
        team.HasSeasonRewardBadge = false;

        team.HasChampionBadge = false;
        team.ChampionBadgeExpireDate = default;

        team.HasActivityBadge = false;
        team.ActivityBadgeEarnedDate = default;
        team.ActivityBadgeExpireDate = default;
        team.ActivityRewardClaimedThisSeason = false;
    }
    // This method calculates the current season ID based on a fixed start date and the current date.
   

    public static int GetDaysRemaining(
    TeamProfileModel team)
    {
        if (team.SeasonEndDate == default)
            return SeasonDurationDays;

        int days =
            (team.SeasonEndDate.Date - DateTime.Now.Date).Days;

        return Math.Max(0, days);
    }

    public static string GetSeasonCountdownText(
        TeamProfileModel team)
    {
        int days =
            GetDaysRemaining(team);

        return $"متبقي {days} يوم على نهاية الموسم";
    }
    // This method calculates the progress of the current season as a value between 0 and 1.

 
    public static double GetSeasonProgress(
    TeamProfileModel team)
    {
        if (team.SeasonStartDate == default ||
            team.SeasonEndDate == default)
        {
            return 0;
        }

        double totalDays =
            (team.SeasonEndDate -
             team.SeasonStartDate).TotalDays;

        if (totalDays <= 0)
            return 0;

        double elapsedDays =
            (DateTime.Now -
             team.SeasonStartDate).TotalDays;

        double progress =
            elapsedDays / totalDays;

        return Math.Clamp(
            progress,
            0,
            1);
    }

    // This method determines the current season number based on the maximum season ID among the teams.
    public static int GetCurrentSeasonNumber(
        List<TeamProfileModel> teams)
    {
        if (teams == null || teams.Count == 0)
            return 1;

        int seasonId =
            teams
            .Where(x => x.CurrentSeasonId > 0)
            .Select(x => x.CurrentSeasonId)
            .DefaultIfEmpty(1)
            .Max();

        return seasonId;
    }
}