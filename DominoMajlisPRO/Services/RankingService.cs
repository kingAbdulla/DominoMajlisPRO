using DominoMajlisPRO.Models;
using System.ComponentModel.Design;
using System.Text.Json;

namespace DominoMajlisPRO.Services;

public sealed record TrustEvaluationResult(
    int PreviousTrust,
    int NewTrust,
    int Delta,
    IReadOnlyList<string> EvidenceCodes,
    string Explanation,
    string MatchId,
    DateTime EvaluatedAt,
    bool IsManualReviewRequired);

public static class RankingService
{

    static void UpdateHallOfFame(
    TeamProfileModel team)
    {
        if (
            team.XP >= 3000 &&
            team.Wins >= 20 &&
            team.WinRate >= 60 &&
            !team.IsSuspicious)
        {
            team.HallOfFameMember = true;

            if (team.HallOfFameDate ==
                default)
            {
                team.HallOfFameDate =
                    DateTime.Now;
            }
        }
    }
    // =========================
    // FILE PATH
    // =========================


    // =========================
    // LOAD TEAMS
    // =========================

    static string filePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "rankings.json");

    static string rivalryFilePath =
    Path.Combine(
        FileSystem.AppDataDirectory,
        "rivalries.json");



    // =========================
    // SAVE TEAMS
    // =========================

    public static async Task SaveTeamsAsync(List<TeamProfileModel> teams)
    {
        string json =
            JsonSerializer.Serialize(
                teams,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            filePath,
            json);
    }

    // Rivalry Record

    public static async Task<List<TeamProfileModel>>
    LoadTeamsAsync()
    {
        try
        {
            if (!File.Exists(filePath))
                return new();

            string json =
                await File.ReadAllTextAsync(
                    filePath);

            return JsonSerializer.Deserialize<
                List<TeamProfileModel>>(json)
                ?? new();
        }
        catch
        {
            return new();
        }
    }

    static async Task<List<RivalryRecord>>
LoadRivalriesAsync()
    {
        try
        {
            if (!File.Exists(rivalryFilePath))
            {
                return new();
            }

            string json =
                await File.ReadAllTextAsync(
                    rivalryFilePath);

            return JsonSerializer.Deserialize
                <List<RivalryRecord>>(json)
                ?? new();
        }
        catch
        {
            return new();
        }
    }

    // Save Rivalries Async
    static async Task SaveRivalriesAsync(
        List<RivalryRecord> rivalries)
    {
        string json =
            JsonSerializer.Serialize(
                rivalries,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            rivalryFilePath,
            json);
    }
        

    // =========================
    // UPDATE RANKINGS
    // =========================

    public static async Task UpdateRankingsAsync(
        SavedMatch match)
    {
        List<TeamProfileModel> teams =
            await LoadTeamsAsync();

        List<RivalryRecord> rivalries= await LoadRivalriesAsync();
        string team1 =
            match.Team1Name
            .Trim();

        string team2 =
            match.Team2Name
            .Trim();

        string winner =
     string.IsNullOrWhiteSpace(
         match.WinnerTeamName)
     ? match.WinnerTeam.Trim()
     : match.WinnerTeamName.Trim();

        // TEAM 1


        TeamProfileModel? team1Profile =
    teams.FirstOrDefault(
        x => x.TeamId == match.Team1Id);

        if (team1Profile == null)
        {
            // Prefer resolving by TeamId for authoritative binding; fall back handled inside GetTeamAsync
            team1Profile =
                await TeamProfileService.GetTeamAsync(
                    match.Team1Id);

            if (team1Profile == null)
            {
                team1Profile =
                    new TeamProfileModel
                    {
                        TeamId = match.Team1Id,
                        TeamName = match.Team1Name,
                        Rank = "Unranked"
                    };
            }

            teams.Add(team1Profile);
        }


        // TEAM 2
        TeamProfileModel? team2Profile =
            teams.FirstOrDefault(
                x => x.TeamId == match.Team2Id);
        if (team2Profile == null)
        {
            // Prefer resolving by TeamId for authoritative binding; fall back handled inside GetTeamAsync
            team2Profile =
                await TeamProfileService.GetTeamAsync(
                    match.Team2Id);

            if (team2Profile == null)
            {
                team2Profile =
                    new TeamProfileModel
                    {
                        TeamId = match.Team2Id,
                        TeamName = match.Team2Name,
                        Rank = "Unranked"
                    };
            }

            teams.Add(team2Profile);
        }

        bool team1Won =
            match.WinnerTeamId ==
            match.Team1Id;

        bool team2Won =
            match.WinnerTeamId ==
            match.Team2Id;
        string rivalryKeyA =
    match.Team1Id.CompareTo(
        match.Team2Id) < 0
    ? match.Team1Id
    : match.Team2Id;

        string rivalryKeyB =
            match.Team1Id.CompareTo(
                match.Team2Id) < 0
            ? match.Team2Id
            : match.Team1Id;

        RivalryRecord? rivalry =
            rivalries.FirstOrDefault(
                x =>
                x.TeamAId == rivalryKeyA
                &&
                x.TeamBId == rivalryKeyB);

        if (rivalry == null)
        {
            rivalry =
       new RivalryRecord
       {
           TeamAId = rivalryKeyA,
           TeamBId = rivalryKeyB
       };


            rivalries.Add(
                rivalry);
        }

        rivalry.TotalMatches++;

        if (match.WinnerTeamId == rivalry.TeamAId)
        {
            rivalry.TeamAWins++;
        }
        else if (match.WinnerTeamId == rivalry.TeamBId)
        {
            rivalry.TeamBWins++;
        }

        if (team1Won)
        {
            int gainedXP =
                CalculateWinXP(
                    team1Profile,
                    team2Profile);

            int lostXP =
                CalculateLoseXP(
                    team1Profile,
                    team2Profile);

            team1Profile.XP += gainedXP;
            team1Profile.LifetimeXP += gainedXP;
            team2Profile.XP =
                Math.Max(
                    0,
                    team2Profile.XP - lostXP);
        }
        else if (team2Won)
        {
            int gainedXP =
                CalculateWinXP(
                    team2Profile,
                    team1Profile);

            int lostXP =
                CalculateLoseXP(
                    team2Profile,
                    team1Profile);

            team2Profile.XP += gainedXP;
            team2Profile.LifetimeXP += gainedXP;
            team1Profile.XP =
                Math.Max(
                    0,
                    team1Profile.XP - lostXP);
        }
        UpdateTeam(
            teams,
            match.Team1Id,
            match.Team1Name,
            team1Won,
            match.HasMeles && team1Won,
            match,
            match.Team1Score);

        UpdateTeam(
            teams,
            match.Team2Id,
            match.Team2Name,
            team2Won,
            match.HasMeles && team2Won,
            match,
            match.Team2Score);

        // REMOVE DUPLICATES



        teams =
         teams
         .GroupBy(x =>
             x.TeamId)
         .Select(g =>
         {
             TeamProfileModel merged =
                 g.OrderByDescending(x => x.XP)
                  .First();

             merged.TeamId =
                 g.First().TeamId;

             merged.XP =
                 g.Max(x => x.XP);

             merged.Wins =
                 g.Max(x => x.Wins);

             merged.Losses =
                 g.Max(x => x.Losses);

             merged.GamesPlayed =
                 g.Max(x => x.GamesPlayed);

             merged.TotalMatches =
                 g.Max(x => x.TotalMatches);

             merged.HighestScore =
                 g.Max(x => x.HighestScore);

             merged.MelesCount =
                 g.Max(x => x.MelesCount);

             merged.WinRate =
                 g.Max(x => x.WinRate);

             merged.SuspiciousScore =
                 g.Max(x => x.SuspiciousScore);

             merged.ConsecutiveWins =
                 g.Max(x => x.ConsecutiveWins);

             merged.ConsecutiveLosses =
                 g.Max(x => x.ConsecutiveLosses);

             merged.ShortMatchesCount =
                 g.Max(x => x.ShortMatchesCount);

             merged.SuspiciousMelesCount =
                 g.Max(x => x.SuspiciousMelesCount);

             merged.ActivityScore =
                 g.Max(x => x.ActivityScore);

             merged.SeasonXP =
                 g.Max(x => x.SeasonXP);

             merged.MVPPoints =
                 g.Max(x => x.MVPPoints);

             merged.RivalWins =
                 g.Max(x => x.RivalWins);

             merged.RankDecayDays =
                 g.Max(x => x.RankDecayDays);

             merged.TrustScore =
                 g.Max(x => x.TrustScore);

             merged.LastMatchDate =
                 g.Max(x => x.LastMatchDate);

             merged.HallOfFameMember =
                 g.Any(x => x.HallOfFameMember);

             merged.HallOfFameEligible =
                 g.Any(x => x.HallOfFameEligible);

             merged.IsVerified =
                 g.Any(x => x.IsVerified);

             merged.IsMVP =
                 g.Any(x => x.IsMVP);

             merged.HasRivalry =
                 g.Any(x => x.HasRivalry);

             merged.HasSeasonReward =
                 g.Any(x => x.HasSeasonReward);

             merged.IsSuspicious =
                 g.Any(x => x.IsSuspicious);

             merged.VerifiedTeam =
                 g.Any(x => x.VerifiedTeam);

             merged.HallOfFameDate =
                 g.Max(x => x.HallOfFameDate);

             return merged;
         })
         .ToList();

        foreach (var team in teams)
        {
            UpdateTrustScore(team);

            team.Rank =
                GetRankFromXP(
                    team.XP);

            if (GetRankLevel(team.Rank) >
    GetRankLevel(team.HighestRank))
            {
                team.HighestRank =
                    team.Rank;
            }


            UpdateHallOfFame(team);

            team.IsVerified =
                team.TrustScore >= 75
                && team.TotalMatches >= 5
                && team.WinRate >= 50
                && !team.IsSuspicious;
            team.IsMVP =
                team.WinRate >= 70
                && team.Wins >= 25
                && team.HighestScore >= 150;
            team.HasSeasonReward = team.SeasonXP >= 500;

            team.HasRivalry = false;
            team.RivalTeamName = "";

            foreach (var rivalryRecord in rivalries)
            {
                bool isTeamA =
                    rivalryRecord.TeamAId ==
                    team.TeamId;

                bool isTeamB =
                    rivalryRecord.TeamBId ==
                    team.TeamId;

                if (!isTeamA &&
                    !isTeamB)
                    continue;

                int wins =
                    isTeamA
                    ? rivalryRecord.TeamAWins
                    : rivalryRecord.TeamBWins;

                int losses =
                    isTeamA
                    ? rivalryRecord.TeamBWins
                    : rivalryRecord.TeamAWins;

                int difference =
                    Math.Abs(
                        wins - losses);

                if (
                    rivalryRecord.TotalMatches >= 5
                    &&
                    wins >= 3
                    &&
                    losses >= 2
                    &&
                    team.TrustScore >= 75
                )
                {
                    team.HasRivalry = true;


                    team.RivalTeamId =
                        isTeamA
                        ? rivalryRecord.TeamBId
                        : rivalryRecord.TeamAId;

                    var rivalTeam =
                        teams.FirstOrDefault(
                            x => x.TeamId ==
                                 team.RivalTeamId);
                    if (rivalTeam == null ||
    rivalTeam.TrustScore < 75)
                    {
                        continue;
                    }
                    team.RivalTeamName =
                        rivalTeam?.TeamName ?? "";


                    break;
                }
            }
        }
        SeasonManager.EnsureSeason(teams);

        BadgeEngine.UpdateAllTeamsBadges(teams);

        await SaveTeamsAsync(
            teams);
        await SaveRivalriesAsync(
            rivalries);
    }


    // =========================
    // XP SYSTEM
    // =========================

    static int CalculateWinXP(
        TeamProfileModel winner,
        TeamProfileModel loser)
    {
        int xp = 20;

        int gap =
            loser.XP - winner.XP;

        if (gap >= 500)
            xp += 15;

        if (gap >= 1000)
            xp += 25;

        if (gap >= 2000)
            xp += 40;

        if (gap >= 3500)
            xp += 60;

        if (gap >= 5000)
            xp += 90;

        return xp;
    }


    static int CalculateLoseXP(
        TeamProfileModel winner,
        TeamProfileModel loser)
    {
        int loss = 5;

        int gap =
            winner.XP - loser.XP;

        if (gap >= 1000)
            loss += 10;

        if (gap >= 2500)
            loss += 15;

        if (gap >= 4000)
            loss += 25;

        return loss;
    }

    // =========================
    // RANK SYSTEM
    // =========================

    public static string GetRankFromXP(
        int xp)
    {
        if (xp < 100)
            return "Unranked";

        if (xp < 300)
            return "Bronze I";

        if (xp < 500)
            return "Bronze II";

        if (xp < 700)
            return "Bronze III";

        if (xp < 1000)
            return "Silver I";

        if (xp < 1300)
            return "Silver II";

        if (xp < 1600)
            return "Silver III";

        if (xp < 2000)
            return "Gold I";

        if (xp < 2400)
            return "Gold II";

        if (xp < 2800)
            return "Gold III";

        if (xp < 3300)
            return "Platinum I";

        if (xp < 3800)
            return "Platinum II";

        if (xp < 4300)
            return "Platinum III";

        if (xp < 5000)
            return "Diamond I";

        if (xp < 6000)
            return "Diamond II";

        if (xp < 7000)
            return "Diamond III";

        if (xp < 9000)
            return "Majlis Master";

        return "Majlis Legend";
    }


    // Get Rank Level for Sorting
    static int GetRankLevel(string rank)
    {
        return rank switch
        {
            "Unranked" => 0,

            "Bronze I" => 1,
            "Bronze II" => 2,
            "Bronze III" => 3,

            "Silver I" => 4,
            "Silver II" => 5,
            "Silver III" => 6,

            "Gold I" => 7,
            "Gold II" => 8,
            "Gold III" => 9,

            "Platinum I" => 10,
            "Platinum II" => 11,
            "Platinum III" => 12,

            "Diamond I" => 13,
            "Diamond II" => 14,
            "Diamond III" => 15,

            "Majlis Master" => 16,
            "Majlis Legend" => 17,

            _ => 0
        };
    }

    public static int GetNextRankXP(
        int xp)
    {
        if (xp < 100)
            return 100;

        if (xp < 300)
            return 300;

        if (xp < 500)
            return 500;

        if (xp < 700)
            return 700;

        if (xp < 1000)
            return 1000;

        if (xp < 1300)
            return 1300;

        if (xp < 1600)
            return 1600;

        if (xp < 2000)
            return 2000;

        if (xp < 2400)
            return 2400;

        if (xp < 2800)
            return 2800;

        if (xp < 3300)
            return 3300;

        if (xp < 3800)
            return 3800;

        if (xp < 4300)
            return 4300;

        if (xp < 5000)
            return 5000;

        if (xp < 6000)
            return 6000;

        if (xp < 7000)
            return 7000;

        if (xp < 9000)
            return 9000;

        return 9000;
    }

    public static string GetNextRankName(int xp)
    {
        if (xp < 100)
            return "Bronze I";

        if (xp < 300)
            return "Bronze II";

        if (xp < 500)
            return "Silver I";

        if (xp < 700)
            return "Silver II";

        if (xp < 1000)
            return "Gold I";

        if (xp < 1300)
            return "Gold II";

        if (xp < 1600)
            return "Platinum I";

        if (xp < 2000)
            return "Platinum II";

        if (xp < 2400)
            return "Diamond I";

        if (xp < 2800)
            return "Diamond II";

        if (xp < 3300)
            return "Majlis Master";

        return "Majlis Legend";
    }

    public static int GetXPRemaining(
        int xp)
    {
        return Math.Max(
            0,
            GetNextRankXP(xp) - xp);
    }



    // =========================
    // RANK START XP
    // =========================

    public static int GetRankStartXP(
        int xp)
    {
        if (xp < 100)
            return 0;

        if (xp < 300)
            return 100;

        if (xp < 500)
            return 300;

        if (xp < 700)
            return 500;

        if (xp < 1000)
            return 700;

        if (xp < 1300)
            return 1000;

        if (xp < 1600)
            return 1300;

        if (xp < 2000)
            return 1600;

        if (xp < 2400)
            return 2000;

        if (xp < 2800)
            return 2400;

        if (xp < 3300)
            return 2800;

        if (xp < 3800)
            return 3300;

        if (xp < 4300)
            return 3800;

        if (xp < 5000)
            return 4300;

        if (xp < 6000)
            return 5000;

        if (xp < 7000)
            return 6000;

        if (xp < 9000)
            return 7000;

        return 9000;
    }

    // =========================
    // CURRENT RANK PROGRESS
    // =========================

    public static int GetCurrentRankXP(
        int xp)
    {
        return xp -
            GetRankStartXP(xp);
    }

    // =========================
    // TOTAL XP IN RANK
    // =========================

    public static int GetRankRangeXP(
        int xp)
    {
        return GetNextRankXP(xp)
            - GetRankStartXP(xp);
    }

    // =========================
    // PROGRESS PERCENTAGE
    // =========================

    public static double GetProgressPercentage(
        int xp)
    {
        int current =
            GetCurrentRankXP(xp);

        int total =
            GetRankRangeXP(xp);

        if (total <= 0)
            return 100;

        return
            (double)current
            / total;
    }

    // =========================
    // UPDATE TEAM
    // =========================

    static void UpdateTeam(
    List<TeamProfileModel> teams,
    string teamId,
    string teamName,
    bool won,
    bool meles,
    SavedMatch match,
    int score)
    {
        TeamProfileModel? team =
            teams.FirstOrDefault(
                x => x.TeamId == teamId);

        // CREATE

        if (team == null)
        {
            team =
                new TeamProfileModel
                {
                    TeamId = teamId,
                    TeamName = teamName,

                    XP = 0,
                    Wins = 0,
                    Losses = 0,
                    GamesPlayed = 0,
                    TotalMatches = 0,
                    HighestScore = 0,
                    MelesCount = 0,
                    WinRate = 0,

                    Rank = "🎯 مبتدئ",

                    IsSuspicious = false,
                    SuspiciousScore = 0,

                    ConsecutiveWins = 0,
                    ConsecutiveLosses = 0,

                    ShortMatchesCount = 0,
                    SuspiciousMelesCount = 0,

                    TrustLevel = "🟢 موثوق",

                    LastMatchDate = DateTime.Now
                };

            teams.Add(team);
        }

        // تحديث الاسم دائماً
        team.TeamName = teamName;
        if (match.Team1Id == teamId)
        {
            team.Player1 =
                match.Team1Players
                .Split('+')
                .FirstOrDefault() ?? "";

            team.Player2 =
                match.Team1Players
                .Split('+')
                .Skip(1)
                .FirstOrDefault() ?? "";
        }
        else if (match.Team2Id == teamId)
        {
            team.Player1 =
                match.Team2Players
                .Split('+')
                .FirstOrDefault() ?? "";

            team.Player2 =
                match.Team2Players
                .Split('+')
                .Skip(1)
                .FirstOrDefault() ?? "";
        }
        team.ActivityScore++;
        team.GamesPlayed++;
        team.TotalMatches++;

        team.HighestScore =
            Math.Max(
                team.HighestScore,
                score);

        team.LastMatchDate =
            DateTime.Now;

        if (won)
        {
            team.Wins++;
            team.LifetimeWins++;

            team.SeasonXP += 20;
        }
        else
        {
            team.Losses++;
            team.LifetimeLosses++;
        }

        if (meles)
        {
            team.MelesCount++;
            team.LifetimeMeles++;

            if (team.TrustScore >= 80)
                team.XP += 150;
            else if (team.TrustScore >= 60)
                team.XP += 100;
            else if (team.TrustScore >= 30)
                team.XP += 50;

            team.SeasonXP += 50;
        }


        team.WinRate =
            (int)Math.Round(
                ((double)team.Wins /
                Math.Max(1,
                team.GamesPlayed))
                * 100);

        UpdateAntiCheat(
            team,
            match,
            won);
        // =========================
        // XP PENALTY
        // =========================

        if (team.TrustScore < 30)
        {
            team.XP =
                Math.Max(
                    0,
                    team.XP - 50);
        }
        else if (team.TrustScore < 60)
        {
            team.XP =
                Math.Max(
                    0,
                    team.XP - 20);
        }

    }

    // =========================
    // DELETE PLAYER
    // =========================

    public static async Task DeletePlayerAsync(
        string playerName)
    {
        List<TeamProfileModel> teams =
            await LoadTeamsAsync();

        teams =
            teams
            .Where(x =>
                x.TeamName
                .Trim()
                .ToLower()
                !=
                playerName
                .Trim()
                .ToLower())
            .ToList();

        await SaveTeamsAsync(teams);
    }

    // =========================
    // DELETE ALL
    // =========================

    public static async Task
        DeleteAllRankingsAsync()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await Task.CompletedTask;
    }

    // =========================
    // TRUST SYSTEM
    // =========================

    static void UpdateTrustScore(
        TeamProfileModel team)
    {
        team.TrustScore =
            Math.Max(
                0,
                100 - team.SuspiciousScore);

        if (team.TrustScore >= 80)
        {
            team.TrustLevel =
                "🟢 موثوق";

            team.IsSuspicious =
                false;
        }
        else if (team.TrustScore >= 60)
        {
            team.TrustLevel =
                "🟡 مراقبة";

            team.IsSuspicious =
                false;
        }
        else if (team.TrustScore >= 30)
        {
            team.TrustLevel =
                "🟠 مشبوه";

            team.IsSuspicious =
                true;
        }
        else
        {
            team.TrustLevel =
                "🔴 غش محتمل";

            team.IsSuspicious =
                true;
        }
    }

    // Anti Cheat
    static void UpdateAntiCheat(
    TeamProfileModel team,
    SavedMatch match,
    bool won)
    {
        // =========================
        // WIN STREAK
        // =========================

        if (won)
        {
            team.ConsecutiveWins++;

            team.ConsecutiveLosses = 0;
        }
        else
        {
            team.ConsecutiveLosses++;

            team.ConsecutiveWins = 0;
        }

        if (match.MatchDurationMinutes <= 1)
            team.ShortMatchesCount++;

        // =========================
        // TOO MANY MELES
        // =========================

        if (match.HasMeles)
        {
            team.SuspiciousMelesCount++;
        }

        var trustEvaluation =
            EvaluateTrustEvidence(
                team,
                match,
                won);

        if (trustEvaluation.Delta < 0 &&
            trustEvaluation.EvidenceCodes.Count > 0)
        {
            team.SuspiciousScore =
                Math.Clamp(
                    100 - trustEvaluation.NewTrust,
                    0,
                    100);
        }

        UpdateTrustScore(
    team);

    }

    static TrustEvaluationResult EvaluateTrustEvidence(
        TeamProfileModel team,
        SavedMatch match,
        bool won)
    {
        List<string> evidenceCodes =
            new();

        if (match.MatchEndDate != default &&
            match.MatchDate != default &&
            match.MatchEndDate < match.MatchDate)
        {
            evidenceCodes.Add("ImpossibleRoundChronology");
        }

        if (match.IsFinished &&
            string.IsNullOrWhiteSpace(match.WinnerTeamId))
        {
            evidenceCodes.Add("InvalidWinnerIdentity");
        }

        if (!string.IsNullOrWhiteSpace(match.WinnerTeamId) &&
            !string.Equals(match.WinnerTeamId, match.Team1Id, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(match.WinnerTeamId, match.Team2Id, StringComparison.OrdinalIgnoreCase))
        {
            evidenceCodes.Add("InvalidParticipantIdentity");
        }

        int previousTrust =
            Math.Clamp(team.TrustScore <= 0 ? 100 : team.TrustScore, 0, 100);

        int penalty =
            evidenceCodes.Count == 0
                ? 0
                : Math.Min(40, evidenceCodes.Count * 15);

        int newTrust =
            Math.Clamp(previousTrust - penalty, 0, 100);

        return new TrustEvaluationResult(
            previousTrust,
            newTrust,
            newTrust - previousTrust,
            evidenceCodes,
            evidenceCodes.Count == 0
                ? "No trust penalty: competitive outcomes are not integrity evidence."
                : string.Join(", ", evidenceCodes),
            match.MatchId.ToString(),
            DateTime.UtcNow,
            evidenceCodes.Count > 0);
    }

    // =========================
    // REBUILD ALL RANKINGS
    // =========================

    public static async Task RebuildAllRankingsAsync()
    {
        // حذف ملفات التصنيف الحالية

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (File.Exists(rivalryFilePath))
        {
            File.Delete(rivalryFilePath);
        }

        // تحميل جميع المباريات

        var matches =
            await GameService.LoadMatchesAsync();

        // إعادة بناء التصنيف من الصفر

        foreach (var match in matches
            .Where(x => x.IsFinished)
            .OrderBy(x => x.MatchDate))
        {
            await UpdateRankingsAsync(match);
        }
    }




}
