using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PlayerTimelineService
{
    public static async Task<List<PlayerTimelineItemModel>> BuildTimelineAsync(
        PlayerProfileModel player)
    {


        List<PlayerTimelineItemModel> items = new();

        AddHistory(items, player.RankHistory, "ترقية الرتبة", "تم الوصول إلى رتبة", "🏅", "#D4AF37");
        AddHistory(items, player.XPHistory, "تحديث XP", "وصل اللاعب إلى", "⚡", "#FFD700");
        AddHistory(items, player.AchievementHistory, "إنجاز جديد", "تم فتح إنجاز", "🏆", "#00C853");
        AddHistory(items, player.HonorHistory, "وسام جديد", "تم منح وسام", "👑", "#B887FF");
        if (!string.IsNullOrWhiteSpace(player.TimelineHistory))
{
    var lines =
        player.TimelineHistory.Split(
            Environment.NewLine,
            StringSplitOptions.RemoveEmptyEntries);

    foreach (var line in lines)
    {
        var parts = line.Split('|');

        if (parts.Length < 5)
            continue;

        DateTime.TryParse(parts[0], out DateTime date);

        items.Add(new PlayerTimelineItemModel
        {
            Date = date,
            Title = parts[1],
            Details = parts[2],
            Icon = parts[3],
            ColorHex = parts[4]
        });
    }
}
        string teamNames =
            await GetTeamNamesAsync(player.CurrentTeamIds);

        if (!string.IsNullOrWhiteSpace(teamNames))
        {
            items.Add(
                new PlayerTimelineItemModel
                {
                    Date = player.LastUpdatedAt,
                    Title = "تحديث الفريق الحالي",
                    Details = $"الفريق الحالي: {teamNames}",
                    Icon = "👥",
                    ColorHex = "#63B7FF"
                });
        }

        return items
            .OrderByDescending(x => x.Date)
            .Take(12)
            .ToList();
    }

    static void AddHistory(
        List<PlayerTimelineItemModel> items,
        string history,
        string title,
        string detailsPrefix,
        string icon,
        string colorHex)
    {
        if (string.IsNullOrWhiteSpace(history))
            return;

        var lines =
            history.Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string[] parts =
                line.Split('|');

            if (parts.Length < 2)
                continue;

            DateTime.TryParse(parts[0], out DateTime date);

            if (date == DateTime.MinValue)
                date = DateTime.Now;

            items.Add(
                new PlayerTimelineItemModel
                {
                    Date = date,
                    Title = title,
                    Details = $"{detailsPrefix}: {parts[1]}",
                    Icon = icon,
                    ColorHex = colorHex
                });
        }
    }
    // This method retrieves team names based on the provided team IDs
    static async Task<string> GetTeamNamesAsync(string teamIds)
    {
        if (string.IsNullOrWhiteSpace(teamIds))
            return "";

        var teams =
            await TeamProfileService.LoadTeamsAsync();

        var ids =
            teamIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        var names =
            teams
            .Where(x => ids.Contains(x.TeamId))
            .Select(x => x.TeamName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        return string.Join("، ", names);
    }

    // This method can be called whenever a significant event occurs for the player
    public static void AddEvent(
    PlayerProfileModel player,
    string title,
    string details,
    string icon = "⭐",
    string colorHex = "#D4AF37")
    {
        if (player == null)
            return;

        string line =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{title}|{details}|{icon}|{colorHex}";

        if (string.IsNullOrWhiteSpace(player.TimelineHistory))
        {
            player.TimelineHistory = line;
        }
        else
        {
            player.TimelineHistory =
                line + Environment.NewLine + player.TimelineHistory;
        }

        player.LastUpdatedAt = DateTime.Now;
    }
}