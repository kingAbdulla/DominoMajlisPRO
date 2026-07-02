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
            var lines = SplitTimeline(player.TimelineHistory);

            foreach (var line in lines)
            {
                var parts = line.Split('|');

                if (parts.Length < 5)
                    continue;

                DateTime.TryParse(parts[0], out DateTime date);

                items.Add(new PlayerTimelineItemModel
                {
                    EventId = line,
                    IsIdentityEvent = true,
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

        title = title?.Trim() ?? string.Empty;
        details = details?.Trim() ?? string.Empty;
        icon = icon?.Trim() ?? string.Empty;
        colorHex = colorHex?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(details))
        {
            return;
        }

        bool duplicateExists = SplitTimeline(player.TimelineHistory)
            .Select(line => line.Split('|'))
            .Any(parts =>
                parts.Length >= 5 &&
                Same(parts[1], title) &&
                Same(parts[2], details) &&
                Same(parts[3], icon) &&
                Same(parts[4], colorHex));
        if (duplicateExists)
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

    public static bool DeleteIdentityEvent(
        PlayerProfileModel player,
        string eventId)
    {
        if (player == null || string.IsNullOrWhiteSpace(eventId))
            return false;

        var lines = SplitTimeline(player.TimelineHistory).ToList();
        int index = lines.FindIndex(line =>
            string.Equals(line, eventId, StringComparison.Ordinal));
        if (index < 0)
            return false;

        lines.RemoveAt(index);
        player.TimelineHistory = string.Join(Environment.NewLine, lines);
        player.LastUpdatedAt = DateTime.Now;
        return true;
    }

    public static bool DeleteAllIdentityEvents(PlayerProfileModel player)
    {
        if (player == null ||
            string.IsNullOrWhiteSpace(player.TimelineHistory))
        {
            return false;
        }

        player.TimelineHistory = string.Empty;
        player.LastUpdatedAt = DateTime.Now;
        return true;
    }

    private static IEnumerable<string> SplitTimeline(string? history) =>
        (history ?? string.Empty).Split(
            new[] { "\r\n", "\n", "\r" },
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

    private static bool Same(string? left, string? right) =>
        string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
