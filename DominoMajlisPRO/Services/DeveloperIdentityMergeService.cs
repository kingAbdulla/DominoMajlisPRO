using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class DeveloperIdentityMergeService
{
    sealed class UserState
    {
        public List<ApplicationUserModel> Users { get; set; } = new();
        public CurrentUserSessionModel? Session { get; set; }
    }

    const string UsersFileName = "application_users.json";
    const string SessionFileName = "current_user_session.json";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    static string UsersFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, UsersFileName);

    static string SessionFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, SessionFileName);

    public static async Task<ApplicationUserModel?> MergeCurrentUserWithVerifiedHonorRoleAsync(
        HonorRoleType verifiedRole,
        string verifiedDisplayName)
    {
        if (verifiedRole == HonorRoleType.None)
            return null;

        var state = await LoadStateAsync();
        var user = FindCurrentUser(state);

        if (user == null)
            return null;

        user.Role = MapRole(verifiedRole);
        user.IsTemporary = false;
        user.DisplayName = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName.Trim()
            : verifiedDisplayName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        state.Session ??= new CurrentUserSessionModel();
        state.Session.ApplicationUserId = user.ApplicationUserId;
        state.Session.CurrentAccountId = user.ApplicationUserId;
        state.Session.PlayerId = user.PlayerId;
        state.Session.CurrentPlayerId = user.PlayerId;
        state.Session.Role = user.Role;
        state.Session.IsLoggedOut = false;
        state.Session.LastActiveAt = DateTime.UtcNow;

        await SaveStateAsync(state);
        AppEvents.RaiseCurrentUserChanged();
        AppEvents.RaisePlayerProfileChanged();

        if (!string.IsNullOrWhiteSpace(user.PlayerId))
            AppEvents.RaiseStoreEconomyChanged(user.PlayerId);

        return user;
    }

    static ApplicationUserRole MapRole(HonorRoleType role) =>
        role switch
        {
            HonorRoleType.Developer => ApplicationUserRole.Developer,
            HonorRoleType.Founder => ApplicationUserRole.Founder,
            HonorRoleType.Honor => ApplicationUserRole.Honor,
            _ => ApplicationUserRole.Member
        };

    static ApplicationUserModel? FindCurrentUser(UserState state)
    {
        string currentId =
            state.Session?.ApplicationUserId?.Trim() ??
            state.Session?.CurrentAccountId?.Trim() ??
            "";

        if (string.IsNullOrWhiteSpace(currentId))
            return null;

        return state.Users.FirstOrDefault(user =>
            Same(user.ApplicationUserId, currentId));
    }

    static async Task<UserState> LoadStateAsync()
    {
        var state = new UserState();

        if (File.Exists(UsersFilePath))
        {
            try
            {
                string usersJson = await File.ReadAllTextAsync(UsersFilePath);
                state.Users = JsonSerializer.Deserialize<List<ApplicationUserModel>>(
                    usersJson,
                    JsonOptions) ?? new List<ApplicationUserModel>();
            }
            catch
            {
                state.Users = new List<ApplicationUserModel>();
            }
        }

        if (File.Exists(SessionFilePath))
        {
            try
            {
                string sessionJson = await File.ReadAllTextAsync(SessionFilePath);
                state.Session = JsonSerializer.Deserialize<CurrentUserSessionModel>(
                    sessionJson,
                    JsonOptions);
            }
            catch
            {
                state.Session = null;
            }
        }

        return state;
    }

    static async Task SaveStateAsync(UserState state)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);

        await WriteJsonSafelyAsync(UsersFilePath, state.Users);

        if (state.Session != null)
            await WriteJsonSafelyAsync(SessionFilePath, state.Session);
    }

    static async Task WriteJsonSafelyAsync<T>(string path, T value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        string tempPath = path + ".tmp." + Guid.NewGuid().ToString("N");

        await File.WriteAllTextAsync(tempPath, json);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tempPath, path);
    }

    static bool Same(string? left, string? right) =>
        string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
