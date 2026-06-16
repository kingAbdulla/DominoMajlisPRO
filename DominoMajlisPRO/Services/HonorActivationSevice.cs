using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class HonorActivationService
{

    public const string FirstDeveloperSetupKey =
    "DMP-FIRST-DEV-SETUP-2026";
    public static bool IsDeveloperKey(
        string key)
    {
        return key.StartsWith(
            "DMP-DEV-",
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFounderKey(
        string key)
    {
        return key.StartsWith(
            "DMP-FND-",
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsHonorKey(
        string key)
    {
        return key.StartsWith(
            "DMP-HNR-",
            StringComparison.OrdinalIgnoreCase);
    }

    public static HonorRoleType GetRoleFromKey(
        string key)
    {
        if (IsDeveloperKey(key))
            return HonorRoleType.Developer;

        if (IsFounderKey(key))
            return HonorRoleType.Founder;

        if (IsHonorKey(key))
            return HonorRoleType.Honor;

        return HonorRoleType.None;

    }

    public static int GetFounderNumber(
        string key)
    {
        try
        {
            if (!IsFounderKey(key))
                return 0;

            string[] parts =
                key.Split('-');

            if (parts.Length < 4)
                return 0;

            return int.Parse(parts[2]);
        }
        catch
        {
            return 0;
        }
    }
}