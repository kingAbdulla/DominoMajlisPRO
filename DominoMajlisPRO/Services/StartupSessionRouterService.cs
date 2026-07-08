using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class StartupSessionRouterService
{
    const string SessionFileName = "current_user_session.json";
    const string UsersFileName = "application_users.json";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    static string SessionFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, SessionFileName);

    static string UsersFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, UsersFileName);

    public static async Task<bool> HasActiveRegisteredSessionAsync()
    {
        var session = await ReadSessionAsync();

        if (session == null || session.IsLoggedOut)
            return false;

        if (session.Role == ApplicationUserRole.Ghost)
            return false;

        string applicationUserId =
            session.ApplicationUserId?.Trim() ??
            session.CurrentAccountId?.Trim() ??
            "";

        if (string.IsNullOrWhiteSpace(applicationUserId))
            return false;

        var users = await ReadUsersAsync();
        return users.Any(user =>
            Same(user.ApplicationUserId, applicationUserId) &&
            user.Role != ApplicationUserRole.Ghost &&
            !user.IsTemporary);
    }

    static async Task<CurrentUserSessionModel?> ReadSessionAsync()
    {
        if (!File.Exists(SessionFilePath))
            return null;

        try
        {
            string json = await File.ReadAllTextAsync(SessionFilePath);
            return JsonSerializer.Deserialize<CurrentUserSessionModel>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    static async Task<List<ApplicationUserModel>> ReadUsersAsync()
    {
        if (!File.Exists(UsersFilePath))
            return new List<ApplicationUserModel>();

        try
        {
            string json = await File.ReadAllTextAsync(UsersFilePath);
            return JsonSerializer.Deserialize<List<ApplicationUserModel>>(json, JsonOptions) ??
                   new List<ApplicationUserModel>();
        }
        catch
        {
            return new List<ApplicationUserModel>();
        }
    }

    static bool Same(string? left, string? right) =>
        string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
