using System.Text.Json;
using DominoMajlisPRO.Backend.Authentication;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class PlayerDisplayNameService
{
    sealed class NameChangeState
    {
        public List<NameChangeRecord> Records { get; set; } = new();
    }

    sealed class NameChangeRecord
    {
        public string ApplicationUserId { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string OldName { get; set; } = "";
        public string NewName { get; set; } = "";
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public bool WasFreeChange { get; set; }
    }

    const string UsersFileName = "application_users.json";
    const string SessionFileName = "current_user_session.json";
    const string ChangeHistoryFileName = "player_display_name_history.json";

    static readonly SemaphoreSlim Gate = new(1, 1);

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    static string UsersFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, UsersFileName);

    static string SessionFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, SessionFileName);

    static string ChangeHistoryFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, ChangeHistoryFileName);

    public static async Task UpdateCurrentDisplayNameAsync(string newName)
    {
        newName = Normalize(newName);
        ValidateName(newName);

        await Gate.WaitAsync();
        try
        {
            var users = await LoadUsersAsync();
            var session = await LoadSessionAsync();

            string currentUserId =
                session?.ApplicationUserId?.Trim() ??
                session?.CurrentAccountId?.Trim() ??
                "";

            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new InvalidOperationException("لا توجد جلسة مستخدم نشطة.");

            var currentUser = users.FirstOrDefault(user =>
                Same(user.ApplicationUserId, currentUserId));

            if (currentUser == null || currentUser.Role == ApplicationUserRole.Ghost)
                throw new InvalidOperationException("يجب تسجيل الدخول بحساب عضو لتغيير الاسم.");

            if (users.Any(user =>
                    !Same(user.ApplicationUserId, currentUser.ApplicationUserId) &&
                    Same(user.DisplayName, newName)))
            {
                throw new InvalidOperationException("هذا الاسم مستخدم من حساب آخر.");
            }

            string oldName = currentUser.DisplayName?.Trim() ?? "";

            if (Same(oldName, newName))
                return;

            var players = await PlayerProfileService.LoadPlayersAsync();
            var linkedPlayer = players.FirstOrDefault(player =>
                Same(player.PlayerId, currentUser.PlayerId));

            if (players.Any(player =>
                    !Same(player.PlayerId, currentUser.PlayerId) &&
                    Same(player.PlayerName, newName)))
            {
                throw new InvalidOperationException("هذا الاسم مستخدم من لاعب آخر.");
            }

            var tokenSession = await SupabaseTokenStore.LoadAsync();
            if (tokenSession != null &&
                !string.IsNullOrWhiteSpace(tokenSession.AccessToken))
            {
                var auth = new SupabaseAuthenticationService();
                var freshResult = await auth.EnsureFreshSessionAsync(tokenSession);

                if (!freshResult.IsSuccess || freshResult.Session == null)
                {
                    SupabaseTokenStore.Clear();
                    throw new InvalidOperationException("انتهت الجلسة. يرجى تسجيل الدخول من جديد.");
                }

                var freshSession = freshResult.Session;
                var result = await auth.UpdateNicknameAsync(
                    freshSession.AccessToken,
                    newName);

                if (!result.IsSuccess)
                    throw new InvalidOperationException(result.Message);

                await SupabaseTokenStore.SaveAsync(new SupabaseAuthenticationSession
                {
                    SupabaseUserId = freshSession.SupabaseUserId,
                    Email = freshSession.Email,
                    Username = freshSession.Username,
                    Nickname = newName,
                    EmailConfirmed = freshSession.EmailConfirmed,
                    AccessToken = freshSession.AccessToken,
                    RefreshToken = freshSession.RefreshToken,
                    ExpiresAtUtc = freshSession.ExpiresAtUtc
                });
            }

            currentUser.DisplayName = newName;
            currentUser.UpdatedAt = DateTime.UtcNow;

            if (linkedPlayer != null)
            {
                linkedPlayer.PlayerName = newName;
                linkedPlayer.LastUpdatedAt = DateTime.Now;
                await PlayerProfileService.SavePlayersAsync(players);
            }

            if (session != null && Same(session.ApplicationUserId, currentUser.ApplicationUserId))
            {
                session.CurrentAccountId = currentUser.ApplicationUserId;
                session.ApplicationUserId = currentUser.ApplicationUserId;
                session.PlayerId = currentUser.PlayerId;
                session.CurrentPlayerId = currentUser.PlayerId;
                session.Role = currentUser.Role;
                session.IsLoggedOut = false;
                session.LastActiveAt = DateTime.UtcNow;
                await WriteJsonSafelyAsync(SessionFilePath, session);
            }

            await SaveUsersAsync(users);
            await AddHistoryAsync(new NameChangeRecord
            {
                ApplicationUserId = currentUser.ApplicationUserId,
                PlayerId = currentUser.PlayerId,
                OldName = oldName,
                NewName = newName,
                ChangedAt = DateTime.UtcNow,
                WasFreeChange = true
            });

            AppEvents.RaiseCurrentUserChanged();
            AppEvents.RaisePlayerProfileChanged();
            AppEvents.RaiseDataChanged();
        }
        finally
        {
            Gate.Release();
        }
    }

    static void ValidateName(string name)
    {
        if (name.Length < 3 || name.Length > 40)
            throw new InvalidOperationException("الاسم يجب أن يكون بين 3 و40 حرفاً.");

        if (BlockedWords.Any(word =>
                name.Contains(word, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("الاسم يحتوي على كلمة غير مسموحة.");
        }

        bool valid = name.All(ch =>
            char.IsLetterOrDigit(ch) ||
            char.IsWhiteSpace(ch) ||
            ch == '_' || ch == '-' || ch == '|' || ch == '.');

        if (!valid)
            throw new InvalidOperationException("الاسم يحتوي على رموز غير مسموحة.");
    }

    static string Normalize(string value) =>
        value?.Trim() ?? "";

    static string[] BlockedWords => new[]
    {
        "admin",
        "developer",
        "moderator",
        "system",
        "support",
        "null"
    };

    static async Task<List<ApplicationUserModel>> LoadUsersAsync()
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

    static async Task SaveUsersAsync(List<ApplicationUserModel> users)
    {
        await WriteJsonSafelyAsync(UsersFilePath, users);
    }

    static async Task<CurrentUserSessionModel?> LoadSessionAsync()
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

    static async Task AddHistoryAsync(NameChangeRecord record)
    {
        NameChangeState state;

        if (File.Exists(ChangeHistoryFilePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(ChangeHistoryFilePath);
                state = JsonSerializer.Deserialize<NameChangeState>(json, JsonOptions) ?? new NameChangeState();
            }
            catch
            {
                state = new NameChangeState();
            }
        }
        else
        {
            state = new NameChangeState();
        }

        state.Records.Add(record);
        await WriteJsonSafelyAsync(ChangeHistoryFilePath, state);
    }

    static async Task WriteJsonSafelyAsync<T>(string path, T value)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);
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
