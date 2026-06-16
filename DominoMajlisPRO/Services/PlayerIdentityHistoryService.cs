using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PlayerIdentityHistoryService
{
    public static void Touch(PlayerProfileModel player)
    {
        player.LastUpdatedAt = DateTime.Now;
    }

    public static void AddRankHistory(
        PlayerProfileModel player,
        string rank)
    {
        if (string.IsNullOrWhiteSpace(rank))
            return;

        string entry =
            $"{DateTime.Now:yyyy/MM/dd HH:mm}|{rank}";

        if (!player.RankHistory.Contains(rank))
        {
            player.RankHistory =
                Append(player.RankHistory, entry);
        }

        Touch(player);
    }

    public static void AddXPHistory(
        PlayerProfileModel player,
        int xp)
    {
        string entry =
            $"{DateTime.Now:yyyy/MM/dd HH:mm}|{xp}";

        player.XPHistory =
            Append(player.XPHistory, entry);

        Touch(player);
    }

    public static void AddAchievementHistory(
        PlayerProfileModel player,
        string achievement)
    {
        if (string.IsNullOrWhiteSpace(achievement))
            return;

        if (player.AchievementHistory.Contains(achievement))
            return;

        string entry =
            $"{DateTime.Now:yyyy/MM/dd HH:mm}|{achievement}";

        player.AchievementHistory =
            Append(player.AchievementHistory, entry);

        Touch(player);
    }

    public static void AddHonorHistory(
        PlayerProfileModel player,
        string honor)
    {
        if (string.IsNullOrWhiteSpace(honor))
            return;

        if (player.HonorHistory.Contains(honor))
            return;

        string entry =
            $"{DateTime.Now:yyyy/MM/dd HH:mm}|{honor}";

        player.HonorHistory =
            Append(player.HonorHistory, entry);

        Touch(player);
    }

    public static void SetCurrentTeams(
        PlayerProfileModel player,
        IEnumerable<string> teamIds)
    {
        player.CurrentTeamIds =
            string.Join(
                ",",
                teamIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct());

        Touch(player);
    }

    static string Append(string current, string entry)
    {
        if (string.IsNullOrWhiteSpace(current))
            return entry;

        return $"{current}\n{entry}";
    }
}