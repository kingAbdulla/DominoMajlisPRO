using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class UserPrivacyProfileService
{
    static string filePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "user_privacy_profile.json");

    public static async Task<UserPrivacyProfileModel> LoadAsync()
    {
        try
        {
            if (!File.Exists(filePath))
                return new UserPrivacyProfileModel();

            string json =
                await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return new UserPrivacyProfileModel();

            return JsonSerializer
                .Deserialize<UserPrivacyProfileModel>(json)
                ?? new UserPrivacyProfileModel();
        }
        catch
        {
            return new UserPrivacyProfileModel();
        }
    }

    public static async Task SaveAsync(
        UserPrivacyProfileModel profile)
    {
        profile.UpdatedAt =
            DateTime.Now;

        string json =
            JsonSerializer.Serialize(
                profile,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            filePath,
            json);
    }

    public static async Task DeleteAsync()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await Task.CompletedTask;
    }
}
