using System.Text.Json;
using DominoMajlisPRO.Backend.Authentication;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.Backend.Profiles;

public static class SupabaseAccountLinkService
{
    sealed class LinkState
    {
        public List<SupabaseAccountLink> Links { get; set; } = new();
    }

    sealed class SupabaseAccountLink
    {
        public string SupabaseUserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public string ApplicationUserId { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string Nickname { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    }

    const string FileName = "supabase_account_links.json";

    static readonly SemaphoreSlim Gate = new(1, 1);

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    public static async Task<string?> ResolveEmailByUsernameAsync(string username)
    {
        username = username.Trim();

        if (string.IsNullOrWhiteSpace(username))
            return null;

        var state = await LoadAsync();
        var link = state.Links.FirstOrDefault(item =>
            Same(item.Username, username));

        return string.IsNullOrWhiteSpace(link?.Email)
            ? null
            : link.Email.Trim();
    }

    public static async Task<ApplicationUserModel> EnsureLinkedApplicationUserAsync(
        SupabaseAuthenticationSession session,
        string? preferredNickname = null)
    {
        if (string.IsNullOrWhiteSpace(session.SupabaseUserId))
            throw new InvalidOperationException("Supabase session is missing user ID.");

        await Gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            var link = state.Links.FirstOrDefault(item =>
                Same(item.SupabaseUserId, session.SupabaseUserId));

            if (link != null && !string.IsNullOrWhiteSpace(link.ApplicationUserId))
            {
                link.Email = session.Email.Trim();
                link.Username = session.Username.Trim();
                link.Nickname = BuildNickname(session.Email, preferredNickname);
                link.LastLoginAt = DateTime.UtcNow;
                await SaveAsync(state);

                await ApplicationUserService.SwitchUserAsync(link.ApplicationUserId);
                return await ApplicationUserService.GetCurrentUserAsync();
            }

            string nickname = BuildNickname(session.Email, preferredNickname);
            var user = await ApplicationUserService.RegisterMemberAsync(nickname);

            state.Links.Add(new SupabaseAccountLink
            {
                SupabaseUserId = session.SupabaseUserId.Trim(),
                Email = session.Email.Trim(),
                Username = session.Username.Trim(),
                ApplicationUserId = user.ApplicationUserId,
                PlayerId = user.PlayerId,
                Nickname = user.DisplayName,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            });

            await SaveAsync(state);
            return user;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task RegisterPendingLinkAsync(
        string username,
        string email,
        string nickname)
    {
        username = username.Trim();
        email = email.Trim();
        nickname = nickname.Trim();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        await Gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            var existing = state.Links.FirstOrDefault(item =>
                Same(item.Username, username) || Same(item.Email, email));

            if (existing == null)
            {
                state.Links.Add(new SupabaseAccountLink
                {
                    Email = email,
                    Username = username,
                    Nickname = nickname,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Email = email;
                existing.Username = username;
                existing.Nickname = nickname;
                existing.LastLoginAt = DateTime.UtcNow;
            }

            await SaveAsync(state);
        }
        finally
        {
            Gate.Release();
        }
    }

    static string BuildNickname(string email, string? preferredNickname)
    {
        string nickname = preferredNickname?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(nickname))
            return nickname;

        string localPart = email.Split('@').FirstOrDefault()?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(localPart)
            ? "Player"
            : localPart;
    }

    static async Task<LinkState> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new LinkState();

        try
        {
            string json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<LinkState>(json, JsonOptions) ?? new LinkState();
        }
        catch
        {
            return new LinkState();
        }
    }

    static async Task SaveAsync(LinkState state)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        string json = JsonSerializer.Serialize(state, JsonOptions);
        string tempPath = FilePath + ".tmp." + Guid.NewGuid().ToString("N");

        await File.WriteAllTextAsync(tempPath, json);

        if (File.Exists(FilePath))
            File.Delete(FilePath);

        File.Move(tempPath, FilePath);
    }

    static bool Same(string? left, string? right) =>
        string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
