using System.Text.Json;
using DominoMajlisPRO.Cloud;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class TeamProfileService
{
    static string filePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "teams.json");

    public static async Task<List<TeamProfileModel>>
      LoadTeamsAsync()
    {
        if (!File.Exists(filePath))
            return new();

        string json =
            await File.ReadAllTextAsync(filePath);

        var teams =
            JsonSerializer.Deserialize<
                List<TeamProfileModel>>(json)
            ?? new();

        bool modified = false;

        int counter = 1;

        foreach (var team in teams)
        {
            if (string.IsNullOrWhiteSpace(
                team.TeamId))
            {
                team.TeamId =
                    $"T{counter:0000}";

                modified = true;
            }

            counter++;
        }

        if (modified)
        {
            await SaveTeamsAsync(teams);
        }

        return teams;
    }

    public static async Task SaveTeamsAsync(
        List<TeamProfileModel> teams)
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

        await CloudSyncRuntime.TryUpsertManyAsync(
            CloudResources.Teams,
            teams,
            team => team.TeamId);
    }

    public static async Task<TeamProfileModel?> GetTeamAsync(
    string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
            return null;

        var teams =
            await LoadTeamsAsync();

        var byId = teams.FirstOrDefault(x => string.Equals(x.TeamId, teamName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (byId != null)
            return byId;

        return teams.FirstOrDefault(
            x => string.Equals(x.TeamName, teamName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<TeamProfileModel?> GetTeamByIdAsync(
    string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return null;

        var teams =
            await LoadTeamsAsync();

        return teams.FirstOrDefault(
            x => x.TeamId == teamId);
    }

    public static async Task<TeamProfileModel?> GetTeamByPlayerIdAsync(
    string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return null;

        var teams =
            await LoadTeamsAsync();

        return teams.FirstOrDefault(
            x =>
            x.Player1Id == playerId
            ||
            x.Player2Id == playerId);
    }
}
