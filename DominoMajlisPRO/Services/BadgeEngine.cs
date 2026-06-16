using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class BadgeEngine
{
    public static void UpdateAllBadges(
        TeamProfileModel team,
        List<TeamProfileModel> rankings)
    {
        if (team == null)
            return;

        rankings ??= new List<TeamProfileModel>();

        UpdateActivityBadge(team);
        UpdateVerifiedBadge(team);
        UpdateTrustBadge(team);
        UpdateRivalryBadge(team);
        UpdateSeasonRewardBadge(team);
        UpdateMVPBadge(team);
        UpdateChampionBadge(team, rankings);
        UpdateHallOfFameBadge(team);
    }

    public static void UpdateAllTeamsBadges(
        List<TeamProfileModel> rankings)
    {
        if (rankings == null || rankings.Count == 0)
            return;

        foreach (var team in rankings)
        {
            UpdateAllBadges(team, rankings);
        }
    }

    private static void UpdateActivityBadge(TeamProfileModel team)
    {
        if (team.ActivityScore >= 7)
        {
            team.HasActivityBadge = true;

            if (team.ActivityBadgeEarnedDate == default)
                team.ActivityBadgeEarnedDate = DateTime.Now;

            team.ActivityBadgeExpireDate =
                team.ActivityBadgeEarnedDate.AddDays(7);
        }
        else
        {
            team.HasActivityBadge = false;
        }

        if (team.ActivityBadgeExpireDate != default &&
            DateTime.Now > team.ActivityBadgeExpireDate)
        {
            team.HasActivityBadge = false;
        }
    }

    private static void UpdateVerifiedBadge(TeamProfileModel team)
    {
        bool hasIdentity =
            !string.IsNullOrWhiteSpace(team.TeamName) &&
            !string.IsNullOrWhiteSpace(team.Player1);

        if (!team.IsSinglePlayer)
        {
            hasIdentity =
                hasIdentity &&
                !string.IsNullOrWhiteSpace(team.Player2);
        }

        team.HasVerifiedBadge =
            team.TotalMatches >= 5 &&
            team.TrustScore >= 95 &&
            team.WinRate >= 40 &&
            hasIdentity;

        team.IsVerified = team.HasVerifiedBadge;
        team.VerifiedTeam = team.HasVerifiedBadge;
    }

    private static void UpdateTrustBadge(TeamProfileModel team)
    {
        team.HasTrustBadge =
            team.TrustScore >= 95 &&
            team.TotalMatches >= 15 &&
            team.WinRate >= 50;
    }

    private static void UpdateRivalryBadge(TeamProfileModel team)
    {
        team.HasRivalryBadge =
            team.HasRivalry &&
            !string.IsNullOrWhiteSpace(team.RivalTeamName);
    }

    private static void UpdateSeasonRewardBadge(TeamProfileModel team)
    {
        team.HasSeasonRewardBadge =
            team.HasSeasonReward;
    }

    private static void UpdateMVPBadge(TeamProfileModel team)
    {
        team.HasMVPBadge =
            team.IsMVP || team.MVPPoints > 0;
    }

    private static void UpdateChampionBadge(
        TeamProfileModel team,
        List<TeamProfileModel> rankings)
    {
        var champion = rankings
            .OrderByDescending(x => x.XP)
            .ThenByDescending(x => x.Wins)
            .ThenByDescending(x => x.WinRate)
            .FirstOrDefault();

        bool isChampion =
            champion != null &&
            !string.IsNullOrWhiteSpace(team.TeamId) &&
            champion.TeamId == team.TeamId;

        team.HasChampionBadge = isChampion;

        if (isChampion)
        {
            team.ChampionBadgeExpireDate = DateTime.Now.AddDays(30);
        }
    }

    private static void UpdateHallOfFameBadge(TeamProfileModel team)
    {
        team.HasHallOfFameBadge =
            team.HallOfFameMember;

        if (team.HasHallOfFameBadge &&
            team.HallOfFameDate == default)
        {
            team.HallOfFameDate = DateTime.Now;
        }
    }



    // =========================
    // SPECIAL HONORS
    // =========================
    public static void UpdateSpecialHonors(
       TeamProfileModel team,
       List<PlayerProfileModel> players)
    {
        if (team == null || players == null)
            return;

        var player1 =
            players.FirstOrDefault(x =>
                x.PlayerId == team.Player1Id);

        var player2 =
            players.FirstOrDefault(x =>
                x.PlayerId == team.Player2Id);

        team.IsDeveloper =
            (player1?.IsDeveloper ?? false) ||
            (player2?.IsDeveloper ?? false);

        team.IsFounder =
            (player1?.IsFounder ?? false) ||
            (player2?.IsFounder ?? false);
    }



}