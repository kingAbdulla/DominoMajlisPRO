using System.Text.Json;
using DominoMajlisPRO.Cloud;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PlayerProfileService
{
    static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, "players.json");

    static string ImagesFolder =>
        Path.Combine(FileSystem.AppDataDirectory, "player_images");

    public static async Task<List<PlayerProfileModel>> LoadPlayersAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new();

            string json = await File.ReadAllTextAsync(FilePath);

            var players =
                JsonSerializer.Deserialize<List<PlayerProfileModel>>(json)
                ?? new();

            foreach (var player in players)
                PlayerEngine.Normalize(player);

            return PlayerEngine.SortForDisplay(players);
        }
        catch
        {
            return new();
        }
    }

    public static async Task SavePlayersAsync(List<PlayerProfileModel> players)
    {
        foreach (var player in players)
            PlayerEngine.Normalize(player);

        string json =
            JsonSerializer.Serialize(
                players,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(FilePath, json);

        AppEvents.RaiseDataChanged();

        await CloudSyncRuntime.TryUpsertManyAsync(
            CloudResources.Players,
            players,
            player => player.PlayerId);
    }

    public static async Task<PlayerProfileModel?> GetPlayerByIdAsync(string playerId)
    {
        var players = await LoadPlayersAsync();

        return players.FirstOrDefault(x => x.PlayerId == playerId);
    }

    public static async Task<PlayerProfileModel?> GetPlayerByNameAsync(string playerName)
    {
        var players = await LoadPlayersAsync();

        if (string.IsNullOrWhiteSpace(playerName))
            return null;

        string trimmed = playerName.Trim();

        if (trimmed.StartsWith("P", StringComparison.OrdinalIgnoreCase))
        {
            var byId = players.FirstOrDefault(x => string.Equals(x.PlayerId, trimmed, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
                return byId;
        }

        string normalizedName =
            PlayerIdentityService.NormalizePlayerName(playerName);

        return players.FirstOrDefault(x =>
            PlayerIdentityService.NormalizePlayerName(x.PlayerName) == normalizedName);
    }

    public static async Task UpdatePlayerStatsAsync(
        string playerName,
        bool wonMatch)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        var players = await LoadPlayersAsync();

        string trimmed = playerName.Trim();

        PlayerProfileModel? player = null;

        if (trimmed.StartsWith("P", StringComparison.OrdinalIgnoreCase))
        {
            player = players.FirstOrDefault(x => string.Equals(x.PlayerId, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        if (player == null)
        {
            string normalizedName =
                PlayerIdentityService.NormalizePlayerName(playerName);

            player = players.FirstOrDefault(x =>
                PlayerIdentityService.NormalizePlayerName(x.PlayerName) == normalizedName);
        }

        if (player == null)
            return;

        PlayerEngine.ApplyMatchResult(player, wonMatch);

        await SavePlayersAsync(players);
    }

    public static async Task UpdatePlayerProfileAsync(PlayerProfileModel updatedPlayer)
    {
        var players = await LoadPlayersAsync();

        int index =
            players.FindIndex(x => x.PlayerId == updatedPlayer.PlayerId);

        if (index < 0)
            throw new Exception("لم يتم العثور على اللاعب.");

        PlayerEngine.Normalize(updatedPlayer);

        players[index] = updatedPlayer;

        await SavePlayersAsync(players);
    }

    public static async Task SetBuiltInAvatarAsync(
        string playerId,
        string avatarImage)
    {
        var players = await LoadPlayersAsync();

        var player =
            players.FirstOrDefault(x => x.PlayerId == playerId);

        if (player == null)
            throw new Exception("لم يتم العثور على اللاعب.");

        player.AvatarImage =
            string.IsNullOrWhiteSpace(avatarImage)
                ? "player_card.png"
                : avatarImage;

        player.ProfileImagePath = "";
        player.BuiltInAvatar = player.AvatarImage;
        player.AvatarPath = "";
        player.UseCustomAvatar = false;
        player.LastUpdatedAt = DateTime.Now;

        PlayerEngine.Normalize(player);
        PlayerTimelineService.AddEvent(
            player,
            "تغيير الصورة الشخصية",
            "تم اعتماد Avatar جديد",
            "🖼",
            "#D4AF37");

        await SavePlayersAsync(players);
    }

    public static async Task SetProfileImageFromDeviceAsync(
        string playerId,
        FileResult file)
    {
        if (file == null)
            throw new Exception("لم يتم اختيار صورة.");

        var players = await LoadPlayersAsync();

        var player =
            players.FirstOrDefault(x => x.PlayerId == playerId);

        if (player == null)
            throw new Exception("لم يتم العثور على اللاعب.");

        Directory.CreateDirectory(ImagesFolder);

        string extension =
            Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        string targetPath =
            Path.Combine(
                ImagesFolder,
                $"{playerId}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");

        await using Stream sourceStream =
            await file.OpenReadAsync();

        await using FileStream targetStream =
            File.Create(targetPath);

        await sourceStream.CopyToAsync(targetStream);

        player.ProfileImagePath = targetPath;
        player.AvatarPath = targetPath;
        player.UseCustomAvatar = true;
        player.AvatarImage = "";
        player.LastUpdatedAt = DateTime.Now;
        PlayerEngine.Normalize(player);

        PlayerTimelineService.AddEvent(
            player,
            "تغيير الصورة الشخصية",
            "تم اختيار صورة من الجهاز",
            "🖼",
            "#D4AF37");

        await SavePlayersAsync(players);
    }

    public static async Task RemoveProfileImageAsync(string playerId)
    {
        var players = await LoadPlayersAsync();

        var player =
            players.FirstOrDefault(x => x.PlayerId == playerId);

        if (player == null)
            throw new Exception("لم يتم العثور على اللاعب.");

        player.ProfileImagePath = "";
        player.AvatarImage = "player_card.png";
        player.AvatarPath = "";
        player.BuiltInAvatar = "player_card.png";
        player.UseCustomAvatar = false;
        player.LastUpdatedAt = DateTime.Now;

        PlayerEngine.Normalize(player);

        await SavePlayersAsync(players);
    }

    public static ImageSource GetPlayerImageSource(PlayerProfileModel player)
    {
        return PlayerEngine.GetImageSource(player);
    }
}
