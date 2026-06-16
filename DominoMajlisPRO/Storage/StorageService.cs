using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Storage;

public class StorageService
{
    private readonly string matchFilePath;

    public StorageService()
    {
        matchFilePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "matches.json");
    }

    public async Task SaveMatchesAsync(List<MatchModel> matches)
    {
        var json = JsonSerializer.Serialize(matches);

        await File.WriteAllTextAsync(matchFilePath, json);
    }

    public async Task<List<MatchModel>> LoadMatchesAsync()
    {
        if (!File.Exists(matchFilePath))
            return new List<MatchModel>();

        var json = await File.ReadAllTextAsync(matchFilePath);

        return JsonSerializer.Deserialize<List<MatchModel>>(json)
               ?? new List<MatchModel>();
    }
}