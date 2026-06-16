using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PlayerTeamSyncService
{
    public static async Task SyncPlayersFromTeamsAsync()
    {
        var players =
            await PlayerProfileService.LoadPlayersAsync();

        var teams =
            await RankingService.LoadTeamsAsync();

        bool changed = false;

        foreach (var player in players)
        {
            string playerName =
                PlayerIdentityService.NormalizePlayerName(
                    player.PlayerName);

            var playerTeams =
                teams
                .Where(t =>
                    t.Player1Id == player.PlayerId ||
                    t.Player2Id == player.PlayerId ||
                    PlayerIdentityService.NormalizePlayerName(t.Player1) == playerName ||
                    PlayerIdentityService.NormalizePlayerName(t.Player2) == playerName)
                .ToList();

            if (playerTeams.Count == 0)
                continue;

            int oldXP = player.PlayerXP;
            int oldWins = player.Wins;
            int oldLosses = player.Losses;
            int oldMatches = player.TotalMatches;
            double oldWinRate = player.WinRate;
            int oldLegacy = player.LegacyScore;
            int oldHighest = player.HighestScore;
            string oldTeams = player.CurrentTeamIds;
            string oldRank =
                PlayerRankService.Calculate(player.PlayerXP).DisplayName;

            player.PlayerXP =
                playerTeams.Sum(x => x.XP);

            player.Wins =
                playerTeams.Sum(x => x.Wins);

            player.Losses =
                playerTeams.Sum(x => x.Losses);

            player.TotalMatches =
                player.Wins + player.Losses;

            if (player.TotalMatches <= 0)
            {
                player.TotalMatches =
                    playerTeams.Sum(x => x.TotalMatches);
            }

            player.HighestScore =
                playerTeams.Max(x => x.HighestScore);

            player.LegacyScore =
                Math.Max(
                    100,
                    player.PlayerXP +
                    player.Wins * 80 +
                    player.TotalMatches * 10);

            var rankResult =
                PlayerRankService.Calculate(player.PlayerXP);

            PlayerEngine.Normalize(player);

            player.WinRate =
                player.TotalMatches > 0
                    ? Math.Round(
                        (double)player.Wins /
                        player.TotalMatches *
                        100,
                        2)
                    : 0;

            PlayerIdentityHistoryService.AddRankHistory(
                player,
                rankResult.DisplayName);

            PlayerIdentityHistoryService.AddXPHistory(
                player,
                player.PlayerXP);

            PlayerIdentityHistoryService.SetCurrentTeams(
                player,
                playerTeams.Select(x => x.TeamId));

            if (oldXP != player.PlayerXP)
            {
                PlayerTimelineService.AddEvent(
                    player,
                    "تحديث XP",
                    $"تغير XP من {oldXP} إلى {player.PlayerXP}",
                    "⚡",
                    "#FFD700");
            }

            if (oldRank != rankResult.DisplayName)
            {
                PlayerTimelineService.AddEvent(
                    player,
                    "ترقية الرتبة",
                    $"انتقل اللاعب من {oldRank} إلى {rankResult.DisplayName}",
                    "🏅",
                    "#D4AF37");
            }

            if (oldTeams != player.CurrentTeamIds)
            {
                PlayerTimelineService.AddEvent(
                    player,
                    "تحديث الفريق الحالي",
                    "تم تحديث ارتباط اللاعب بالفريق",
                    "👥",
                    "#63B7FF");
            }

            if (
                oldXP != player.PlayerXP ||
                oldWins != player.Wins ||
                oldLosses != player.Losses ||
                oldMatches != player.TotalMatches ||
                oldWinRate != player.WinRate ||
                oldLegacy != player.LegacyScore ||
                oldHighest != player.HighestScore ||
                oldTeams != player.CurrentTeamIds)
            {
                changed = true;
            }
        }

        if (changed)
        {
            await PlayerProfileService.SavePlayersAsync(players);
        }
    }
}