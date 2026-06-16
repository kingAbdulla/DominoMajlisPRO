using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class SecurityLogService
{
    static string filePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "security_logs.json");

    public static async Task<List<SecurityLogModel>> LoadAsync()
    {
        try
        {
            if (!File.Exists(filePath))
                return new();

            string json =
                await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return new();

            var logs =
                JsonSerializer.Deserialize<List<SecurityLogModel>>(json)
                ?? new();

            logs =
                CleanupExpiredTemporaryLogs(logs);

            await SaveAsync(logs);

            return logs;
        }
        catch
        {
            return new();
        }
    }

    public static async Task SaveAsync(
        List<SecurityLogModel> logs)
    {
        string json =
            JsonSerializer.Serialize(
                logs,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            filePath,
            json);
    }

    public static async Task AddAsync(
        string category,
        string action,
        string details,
        string severity = "Info",
        bool isPermanent = false)
    {
        var developer =
            await LoadDeveloperContextAsync();

        string enrichedDetails =
            details;

        if (!string.IsNullOrWhiteSpace(developer.DeveloperId))
        {
            enrichedDetails =
                $"{details}\n\n" +
                $"Developer ID: {developer.DeveloperId}\n" +
                $"Developer Username: {developer.Username}\n" +
                $"Device: {developer.DeviceFingerprint}";
        }

        var logs =
            await LoadAsync();

        logs.Insert(
            0,
            new SecurityLogModel
            {
                Date = DateTime.Now,
                Category = category,
                Action = action,
                Details = enrichedDetails,
                Severity = severity,
                IsPermanent = isPermanent,
                DeveloperId = developer.DeveloperId,
                DeveloperUsername = developer.Username,
                DeviceFingerprint = developer.DeviceFingerprint
            });

        await SaveAsync(logs);
    }

    public static async Task ClearTemporaryAsync()
    {
        var logs =
            await LoadAsync();

        logs =
            logs
                .Where(x => x.IsPermanent)
                .ToList();

        await SaveAsync(logs);
    }

    public static async Task ClearAllAsync()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await Task.CompletedTask;
    }

    static List<SecurityLogModel> CleanupExpiredTemporaryLogs(
        List<SecurityLogModel> logs)
    {
        DateTime limit =
            DateTime.Now.AddDays(-5);

        return logs
            .Where(x =>
                x.IsPermanent ||
                x.Date >= limit)
            .ToList();
    }

    static async Task<(string DeveloperId, string Username, string DeviceFingerprint)>
        LoadDeveloperContextAsync()
    {
        try
        {
            var developer =
                await DeveloperLockService.LoadAsync();

            if (string.IsNullOrWhiteSpace(developer.DeveloperId))
                return ("", "", "");

            return
                (
                    developer.DeveloperId,
                    developer.Username,
                    developer.DeviceFingerprint
                );
        }
        catch
        {
            return ("", "", "");
        }
    }
}