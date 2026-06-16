using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class HonorIdentityService
{
    static readonly string FilePath =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "honor_identity.json");

    public static async Task<HonorIdentityModel> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new HonorIdentityModel();

            string json =
                await File.ReadAllTextAsync(FilePath);

            if (string.IsNullOrWhiteSpace(json))
                return new HonorIdentityModel();

            return JsonSerializer
                .Deserialize<HonorIdentityModel>(json)
                ?? new HonorIdentityModel();
        }
        catch
        {
            return new HonorIdentityModel();
        }
    }

    public static async Task SaveAsync(
        HonorIdentityModel identity)
    {
        string json =
            JsonSerializer.Serialize(
                identity,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        await File.WriteAllTextAsync(
            FilePath,
            json);
    }

    public static async Task<bool> HasActiveRoleAsync()
    {
        var identity =
            await LoadAsync();

        return
            identity.IsActivated
            &&
            identity.Role != HonorRoleType.None;
    }

    public static async Task<HonorRoleType> GetCurrentRoleAsync()
    {
        var identity =
            await LoadAsync();

        if (!identity.IsActivated)
            return HonorRoleType.None;

        return identity.Role;
    }

    public static async Task<bool> IsDeveloperAsync()
    {
        return await GetCurrentRoleAsync()
            == HonorRoleType.Developer;
    }

    public static async Task<bool> IsFounderAsync()
    {
        return await GetCurrentRoleAsync()
            == HonorRoleType.Founder;
    }

    public static async Task<bool> IsHonorAsync()
    {
        return await GetCurrentRoleAsync()
            == HonorRoleType.Honor;
    }

    public static async Task<bool> ActivateAsync(
        string displayName,
        string activationKey)
    {
        var current =
            await LoadAsync();

        if (current.IsActivated)
            return false;

        HonorRoleType role =
            HonorActivationService
            .GetRoleFromKey(activationKey);

        if (role == HonorRoleType.None)
            return false;

        var identity =
            new HonorIdentityModel
            {
                Role = role,
                IsActivated = true,
                ActivationKey = activationKey.Trim(),
                FounderNumber =
                    role == HonorRoleType.Founder
                    ? HonorActivationService.GetFounderNumber(activationKey)
                    : 0,
                ActivationDate = DateTime.Now,
                DeviceId = GetDeviceId(),
                DisplayName = displayName.Trim()
            };

        await SaveAsync(identity);

        await SecurityLogService.AddAsync(
            "HONOR",
            $"تم تفعيل صلاحية {role}",
            $"Activated Role: {role}",
            "High",
            true);

        return true;
    }

    public static async Task ClearAsync()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }

        await SecurityLogService.AddAsync(
            "HONOR",
            "تم حذف هوية الشرف من الجهاز",
            "Honor identity cleared from device",
            "High",
            true);
    }

    static string GetDeviceId()
    {
        try
        {
            return $"{DeviceInfo.Manufacturer}-{DeviceInfo.Model}-{DeviceInfo.Platform}";
        }
        catch
        {
            return "UnknownDevice";
        }
    }
}