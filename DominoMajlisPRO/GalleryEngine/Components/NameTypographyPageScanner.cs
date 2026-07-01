using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Components;

public static class NameTypographyPageScanner
{
    public static async Task ApplyAsync(Element root)
    {
        try
        {
            var teamsTask = TeamProfileService.LoadTeamsAsync();
            var playersTask = PlayerProfileService.LoadPlayersAsync();
            await Task.WhenAll(teamsTask, playersTask);

            var teams = teamsTask.Result
                .Where(team => !string.IsNullOrWhiteSpace(team.TeamName) && !string.IsNullOrWhiteSpace(team.TeamId))
                .ToList();
            var players = playersTask.Result
                .Where(player => !string.IsNullOrWhiteSpace(player.PlayerName) && !string.IsNullOrWhiteSpace(player.PlayerId))
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                foreach (var label in EnumerateLabels(root))
                {
                    var text = label.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    var team = teams.FirstOrDefault(item => SameVisibleName(text, item.TeamName));
                    if (team != null)
                    {
                        await NameTypographyRuntime.ApplyTeamAsync(label, team.TeamId);
                        continue;
                    }

                    var player = players.FirstOrDefault(item => SameVisibleName(text, item.PlayerName));
                    if (player != null)
                        await NameTypographyRuntime.ApplyPlayerAsync(label, player.PlayerId);
                }
            });
        }
        catch
        {
            // Name typography is a visual layer only and must never crash a page.
        }
    }

    public static async Task ApplyDelayedAsync(Element root, int delayMs = 120)
    {
        await Task.Delay(delayMs);
        await ApplyAsync(root);
    }

    private static IEnumerable<Label> EnumerateLabels(Element root)
    {
        if (root is Label label)
            yield return label;

        foreach (var child in root.LogicalChildren)
        {
            foreach (var labelChild in EnumerateLabels(child))
                yield return labelChild;
        }
    }

    private static bool SameVisibleName(string visible, string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        var normalizedVisible = visible.Trim();
        var normalizedSource = source.Trim();
        if (string.Equals(normalizedVisible, normalizedSource, StringComparison.OrdinalIgnoreCase))
            return true;

        if (normalizedSource.Length > 14)
        {
            var shortName = normalizedSource[..14] + "...";
            if (string.Equals(normalizedVisible, shortName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
