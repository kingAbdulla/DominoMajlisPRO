using System.Text.Json;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

public static class ApplicationUserService
{
    public sealed record StoreOwnerContext(
        string ApplicationUserId,
        string PlayerId,
        ApplicationUserRole Role)
    {
        public bool IsGhost => Role == ApplicationUserRole.Ghost;
        public bool HasPlayerProfile =>
            !string.IsNullOrWhiteSpace(PlayerId);
    }

    const string UsersFileName = "application_users.json";
    const string SessionFileName = "current_user_session.json";
    const string DeveloperLockFileName = "developer_lock.json";

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

    static string DeveloperLockFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, DeveloperLockFileName);

    public static async Task<ApplicationUserModel> GetCurrentUserAsync()
    {
        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            bool changed = await MigrateLegacyIdentityAsync(state);

            var currentUser = FindCurrentUser(state);

            if (currentUser == null)
            {
                currentUser = state.Users.FirstOrDefault(user =>
                    user.Role == ApplicationUserRole.Ghost &&
                    user.IsTemporary);

                if (currentUser == null)
                {
                    currentUser = CreateGhostUser();
                    state.Users.Add(currentUser);
                }

                SetSession(state, currentUser);
                changed = true;
            }

            changed |= SynchronizeSessionWithUser(state, currentUser);

            if (changed)
            {
                await SaveStateAsync(state);
                AppEvents.RaiseCurrentUserChanged();
            }

            return currentUser;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<ApplicationUserModel> EnsureGhostUserAsync()
    {
        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            bool changed = await MigrateLegacyIdentityAsync(state);

            var currentUser = FindCurrentUser(state);

            if (currentUser != null)
            {
                if (changed)
                {
                    await SaveStateAsync(state);
                    AppEvents.RaiseCurrentUserChanged();
                }

                return currentUser;
            }

            var ghostUser = state.Users.FirstOrDefault(user =>
                user.Role == ApplicationUserRole.Ghost &&
                user.IsTemporary);

            if (ghostUser == null)
            {
                ghostUser = CreateGhostUser();
                state.Users.Add(ghostUser);
            }

            SetSession(state, ghostUser);
            await SaveStateAsync(state);
            AppEvents.RaiseCurrentUserChanged();

            return ghostUser;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<ApplicationUserModel> RegisterMemberAsync(
        string playerName)
    {
        string displayName = playerName?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Player name is required.", nameof(playerName));

        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            await MigrateLegacyIdentityAsync(state);

            string playerId = CreateId("PLY");
            var member = new ApplicationUserModel
            {
                ApplicationUserId = CreateId("USR"),
                PlayerId = playerId,
                DisplayName = displayName,
                Role = ApplicationUserRole.Member,
                IsTemporary = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            state.Users.Add(member);

            SetSession(state, member);
            await SaveStateAsync(state);
            AppEvents.RaiseCurrentUserChanged();

            return member;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task SetCurrentUserAsync(string userId)
    {
        await SwitchUserAsync(userId);
    }

    public static async Task LogoutAsync()
    {
        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            // capture previous player/account before clearing session so UI can refresh ownership views
            string previousPlayerId = state.Session?.CurrentPlayerId?.Trim() ?? "";
            string previousAccountId = state.Session?.CurrentAccountId?.Trim() ?? "";

            state.Session = new CurrentUserSessionModel
            {
                IsLoggedOut = true,
                StartedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow
            };

            await WriteJsonSafelyAsync(SessionFilePath, state.Session);
            AppEvents.RaiseCurrentUserChanged();

            // trigger store/profile refresh for the previous player so UI transitions correctly to logged-out state
            if (!string.IsNullOrWhiteSpace(previousPlayerId))
                AppEvents.RaiseStoreEconomyChanged(previousPlayerId);

            // also refresh player profile visuals generally
            AppEvents.RaisePlayerProfileChanged();
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<List<ApplicationUserModel>> GetAllUsersAsync()
    {
        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            bool changed = await MigrateLegacyIdentityAsync(state);

            if (changed)
                await SaveStateAsync(state);

            return state.Users
                .OrderBy(user => user.Role == ApplicationUserRole.Ghost)
                .ThenBy(user => user.DisplayName)
                .ThenBy(user => user.CreatedAt)
                .ToList();
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task SwitchUserAsync(string applicationUserId)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId))
        {
            throw new ArgumentException(
                "Application user ID is required.",
                nameof(applicationUserId));
        }

        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            await MigrateLegacyIdentityAsync(state);

            var user = state.Users.FirstOrDefault(item =>
                Same(item.ApplicationUserId, applicationUserId));

            if (user == null)
                throw new InvalidOperationException("Application user was not found.");

            // capture previous session ids to force refreshes after switch
            string previousPlayerId = state.Session?.CurrentPlayerId?.Trim() ?? "";
            string previousAccountId = state.Session?.CurrentAccountId?.Trim() ?? "";

            SetSession(state, user);
            await SaveStateAsync(state);
            AppEvents.RaiseCurrentUserChanged();

            // If player/account changed, trigger refreshes for inventory and store views
            if (!string.Equals(previousPlayerId, state.Session?.CurrentPlayerId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(previousPlayerId))
                AppEvents.RaiseStoreEconomyChanged(previousPlayerId);

            if (!string.Equals(previousAccountId, state.Session?.CurrentAccountId, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(previousAccountId))
                AppEvents.RaisePlayerProfileChanged();
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<ApplicationUserModel> UpgradeGhostToMemberAsync(
        string playerName)
    {
        string displayName = playerName?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Player name is required.", nameof(playerName));

        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            await MigrateLegacyIdentityAsync(state);

            var currentUser = FindCurrentUser(state);

            if (currentUser == null)
            {
                currentUser = CreateGhostUser();
                state.Users.Add(currentUser);
            }

            if (currentUser.Role != ApplicationUserRole.Ghost)
            {
                throw new InvalidOperationException(
                    "Only a Ghost user can be upgraded to Member.");
            }

            var players = await PlayerProfileService.LoadPlayersAsync();
            string playerId =
                !string.IsNullOrWhiteSpace(currentUser.PlayerId)
                    ? currentUser.PlayerId.Trim()
                    : CreateId("PLY");

            var linkedPlayer = players.FirstOrDefault(player =>
                Same(player.PlayerId, playerId));

            if (linkedPlayer == null)
            {
                players.Add(new PlayerProfileModel
                {
                    PlayerId = playerId,
                    PlayerName = displayName,
                    ProfileStatus = PlayerProfileStatus.Normal,
                    CreatedAt = DateTime.Now,
                    LastActiveAt = DateTime.Now,
                    LastActivityAt = DateTime.Now,
                    LastUpdatedAt = DateTime.Now
                });

                await PlayerProfileService.SavePlayersAsync(players);
            }

            currentUser.PlayerId = playerId;
            currentUser.DisplayName = displayName;
            currentUser.Role = ApplicationUserRole.Member;
            currentUser.IsTemporary = false;
            currentUser.UpdatedAt = DateTime.UtcNow;

            SetSession(state, currentUser);
            await SaveStateAsync(state);
            AppEvents.RaiseCurrentUserChanged();

            return currentUser;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<bool> HasActiveSessionAsync()
    {
        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();

            return !state.Session.IsLoggedOut &&
                   FindCurrentUser(state) != null;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<bool> RequiresIdentityChoiceAsync()
    {
        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            bool changed = await MigrateLegacyIdentityAsync(state);

            if (changed)
                await SaveStateAsync(state);

            return state.Session.IsLoggedOut ||
                   FindCurrentUser(state) == null;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<ApplicationUserModel> EnsureCurrentSessionAsync()
    {
        return await GetCurrentUserAsync();
    }

    public static async Task<string?> GetCurrentUserPlayerIdAsync()
    {
        var profile = await EnsureCurrentUserPlayerProfileAsync();
        return string.IsNullOrWhiteSpace(profile?.PlayerId)
            ? null
            : profile.PlayerId;
    }

    public static async Task<StoreOwnerContext>
        GetCurrentStoreOwnerAsync()
    {
        var user = await GetCurrentUserAsync();

        if (user.Role == ApplicationUserRole.Ghost)
        {
            return new StoreOwnerContext(
                user.ApplicationUserId,
                "",
                user.Role);
        }

        var profile = await EnsureCurrentUserPlayerProfileAsync();
        string playerId =
            profile?.PlayerId?.Trim() ??
            user.PlayerId?.Trim() ??
            "";

        return new StoreOwnerContext(
            user.ApplicationUserId,
            playerId,
            user.Role);
    }

    public static async Task<PlayerProfileModel?>
        EnsureCurrentUserPlayerProfileAsync()
    {
        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            bool usersChanged = await MigrateLegacyIdentityAsync(state);
            var user = FindCurrentUser(state);

            if (user == null)
            {
                user = CreateGhostUser();
                state.Users.Add(user);
                SetSession(state, user);
                usersChanged = true;
            }

            if (user.Role == ApplicationUserRole.Ghost &&
                string.IsNullOrWhiteSpace(user.PlayerId))
            {
                if (usersChanged)
                    await SaveStateAsync(state);

                return null;
            }

            var players = await PlayerProfileService.LoadPlayersAsync();
            var profile = ResolveExistingPlayerProfile(user, players);

            if (profile == null)
            {
                if (user.Role == ApplicationUserRole.Ghost)
                    return null;

                profile = CreatePlayerProfile(user);
                players.Add(profile);
                await PlayerProfileService.SavePlayersAsync(players);
            }

            if (!Same(user.PlayerId, profile.PlayerId))
            {
                user.PlayerId = profile.PlayerId;
                user.UpdatedAt = DateTime.UtcNow;
                SetSession(state, user);
                usersChanged = true;
            }

            if (usersChanged)
            {
                await SaveStateAsync(state);
                AppEvents.RaiseCurrentUserChanged();
            }

            return profile;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<PlayerProfileModel>
        LinkUserToExistingPlayerProfileAsync(
            string applicationUserId,
            string playerId)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId))
        {
            throw new ArgumentException(
                "Application user ID is required.",
                nameof(applicationUserId));
        }

        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException("PlayerId is required.", nameof(playerId));

        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            var user = state.Users.FirstOrDefault(item =>
                Same(item.ApplicationUserId, applicationUserId));

            if (user == null)
                throw new InvalidOperationException("Application user was not found.");

            var profile =
                await PlayerProfileService.GetPlayerByIdAsync(playerId);

            if (profile == null)
                throw new InvalidOperationException("Player profile was not found.");

            user.PlayerId = profile.PlayerId;
            user.UpdatedAt = DateTime.UtcNow;

            if (Same(
                    state.Session.ApplicationUserId,
                    user.ApplicationUserId))
            {
                SetSession(state, user);
            }

            await SaveStateAsync(state);
            AppEvents.RaiseCurrentUserChanged();

            return profile;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<PlayerProfileModel>
        CreatePlayerProfileForUserAsync(string applicationUserId)
    {
        if (string.IsNullOrWhiteSpace(applicationUserId))
        {
            throw new ArgumentException(
                "Application user ID is required.",
                nameof(applicationUserId));
        }

        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            var user = state.Users.FirstOrDefault(item =>
                Same(item.ApplicationUserId, applicationUserId));

            if (user == null)
                throw new InvalidOperationException("Application user was not found.");

            if (user.Role == ApplicationUserRole.Ghost)
            {
                throw new InvalidOperationException(
                    "Ghost must be upgraded before creating a player profile.");
            }

            var players = await PlayerProfileService.LoadPlayersAsync();
            var profile = ResolveExistingPlayerProfile(user, players);

            if (profile == null)
            {
                profile = CreatePlayerProfile(user);
                players.Add(profile);
                await PlayerProfileService.SavePlayersAsync(players);
            }

            user.PlayerId = profile.PlayerId;
            user.UpdatedAt = DateTime.UtcNow;

            if (Same(
                    state.Session.ApplicationUserId,
                    user.ApplicationUserId))
            {
                SetSession(state, user);
            }

            await SaveStateAsync(state);
            AppEvents.RaiseCurrentUserChanged();

            return profile;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<ApplicationUserRole> GetCurrentRoleAsync()
    {
        var user = await GetCurrentUserAsync();
        return user.Role;
    }

    public static async Task<ApplicationUserModel> SetCurrentRoleAsync(
        ApplicationUserRole role,
        string? displayName = null,
        string? legacyDeveloperId = null)
    {
        if (role == ApplicationUserRole.Ghost)
            throw new ArgumentException("Ghost is not an activatable role.", nameof(role));

        await Gate.WaitAsync();

        try
        {
            var state = await LoadStateAsync();
            await MigrateLegacyIdentityAsync(state);
            var user = FindCurrentUser(state);

            // Prefer attaching role to an existing user when possible.
            // First try to find a user by legacyDeveloperId to avoid creating duplicates.
            if (user == null && !string.IsNullOrWhiteSpace(legacyDeveloperId))
            {
                string legacyIdTrim = legacyDeveloperId.Trim();
                user = state.Users.FirstOrDefault(u =>
                    (!string.IsNullOrWhiteSpace(u.LegacyDeveloperId) && Same(u.LegacyDeveloperId, legacyIdTrim)) ||
                    (!string.IsNullOrWhiteSpace(u.LegacyIdentityId) && Same(u.LegacyIdentityId, legacyIdTrim)));
            }

            // Next, if a displayName was provided, try to bind to an existing user with that name
            // (legacy fallback to avoid creating a duplicate player with same display name)
            if (user == null && !string.IsNullOrWhiteSpace(displayName))
            {
                string nameTrim = displayName.Trim();
                user = state.Users.FirstOrDefault(u =>
                    !string.IsNullOrWhiteSpace(u.DisplayName) &&
                    string.Equals(u.DisplayName.Trim(), nameTrim, StringComparison.OrdinalIgnoreCase));
            }

            if (user == null)
            {
                user = CreateGhostUser();
                state.Users.Add(user);
            }

            if (string.IsNullOrWhiteSpace(user.PlayerId))
                user.PlayerId = CreateId("PLY");

            if (!string.IsNullOrWhiteSpace(displayName))
                user.DisplayName = displayName.Trim();

            user.Role = role;
            user.IsTemporary = false;
            if (!string.IsNullOrWhiteSpace(legacyDeveloperId))
                user.LegacyDeveloperId = legacyDeveloperId.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var players = await PlayerProfileService.LoadPlayersAsync();
            var profile = players.FirstOrDefault(player =>
                Same(player.PlayerId, user.PlayerId));

            if (profile == null)
            {
                profile = CreatePlayerProfile(user);
                players.Add(profile);
            }
            else
            {
                profile.PlayerName = FirstNonEmpty(user.DisplayName, profile.PlayerName);
                profile.ProfileStatus = MapProfileStatus(role);
                profile.IsDeveloper = role == ApplicationUserRole.Developer;
                profile.IsFounder = role == ApplicationUserRole.Founder;
                profile.LastUpdatedAt = DateTime.Now;
            }

            await PlayerProfileService.SavePlayersAsync(players);
            SetSession(state, user);
            await SaveStateAsync(state);
            AppEvents.RaiseCurrentUserChanged();
            if (!string.IsNullOrWhiteSpace(user.PlayerId))
                AppEvents.RaiseStoreEconomyChanged(user.PlayerId);
            return user;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task<bool> IsDeveloperAsync() =>
        await GetCurrentRoleAsync() == ApplicationUserRole.Developer;

    public static async Task<bool> IsFounderAsync() =>
        await GetCurrentRoleAsync() == ApplicationUserRole.Founder;

    public static async Task<bool> IsHonorAsync() =>
        await GetCurrentRoleAsync() == ApplicationUserRole.Honor;

    public static async Task<bool> IsMemberAsync() =>
        await GetCurrentRoleAsync() == ApplicationUserRole.Member;

    public static async Task<bool> IsGhostAsync() =>
        await GetCurrentRoleAsync() == ApplicationUserRole.Ghost;

    static async Task<ApplicationUserState> LoadStateAsync()
    {
        var users =
            await ReadJsonAsync<List<ApplicationUserModel>>(UsersFilePath)
            ?? new List<ApplicationUserModel>();

        NormalizeUsers(users);

        var session =
            await ReadJsonAsync<CurrentUserSessionModel>(SessionFilePath)
            ?? new CurrentUserSessionModel();

        return new ApplicationUserState(users, session);
    }

    static async Task SaveStateAsync(ApplicationUserState state)
    {
        await WriteJsonSafelyAsync(UsersFilePath, state.Users);
        await WriteJsonSafelyAsync(SessionFilePath, state.Session);
    }

    static async Task<bool> MigrateLegacyIdentityAsync(
        ApplicationUserState state)
    {
        bool hadUsers = state.Users.Count > 0;
        var legacy = await LoadLegacyIdentityAsync();

        if (legacy == null)
            return false;

        bool changed = false;

        var user = state.Users.FirstOrDefault(item =>
            (!string.IsNullOrWhiteSpace(legacy.LegacyIdentityId) &&
             Same(item.LegacyIdentityId, legacy.LegacyIdentityId)) ||
            (!string.IsNullOrWhiteSpace(legacy.DeveloperId) &&
             Same(item.LegacyDeveloperId, legacy.DeveloperId)) ||
            (!string.IsNullOrWhiteSpace(legacy.HonorOwnerId) &&
             Same(item.LegacyHonorOwnerId, legacy.HonorOwnerId)) ||
            (!string.IsNullOrWhiteSpace(legacy.PlayerId) &&
             Same(item.PlayerId, legacy.PlayerId)));

        var canonicalCurrent = FindCurrentUser(state);
        bool hasCanonicalIdentity =
            canonicalCurrent != null &&
            !string.IsNullOrWhiteSpace(state.Session?.CurrentAccountId) &&
            !string.IsNullOrWhiteSpace(state.Session?.CurrentPlayerId) &&
            Same(
                canonicalCurrent.ApplicationUserId,
                state.Session?.CurrentAccountId) &&
            Same(
                canonicalCurrent.PlayerId,
                state.Session?.CurrentPlayerId);

        if (user == null && hasCanonicalIdentity)
            user = canonicalCurrent;

        if (user == null)
        {
            user = new ApplicationUserModel
            {
                ApplicationUserId = CreateId("USR"),
                PlayerId = legacy.PlayerId,
                DisplayName = legacy.DisplayName,
                Role = legacy.Role,
                IsTemporary = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LegacyIdentityId = legacy.LegacyIdentityId,
                LegacyDeveloperId = legacy.DeveloperId,
                LegacyHonorOwnerId = legacy.HonorOwnerId,
                MigrationSource = legacy.Source
            };

            state.Users.Add(user);
            changed = true;
        }
        else
        {
            changed |= SetIfEmpty(
                value => user.PlayerId = value,
                user.PlayerId,
                legacy.PlayerId);
            changed |= SetIfEmpty(
                value => user.DisplayName = value,
                user.DisplayName,
                legacy.DisplayName);
            changed |= SetIfEmpty(
                value => user.LegacyIdentityId = value,
                user.LegacyIdentityId,
                legacy.LegacyIdentityId);
            changed |= SetIfEmpty(
                value => user.LegacyDeveloperId = value,
                user.LegacyDeveloperId,
                legacy.DeveloperId);
            changed |= SetIfEmpty(
                value => user.LegacyHonorOwnerId = value,
                user.LegacyHonorOwnerId,
                legacy.HonorOwnerId);
            changed |= SetIfEmpty(
                value => user.MigrationSource = value,
                user.MigrationSource,
                legacy.Source);

            if (user.Role != legacy.Role)
            {
                user.Role = legacy.Role;
                changed = true;
            }

            if (user.IsTemporary)
            {
                user.IsTemporary = false;
                changed = true;
            }

            if (changed)
                user.UpdatedAt = DateTime.UtcNow;
        }

        var current = FindCurrentUser(state);

        if (!hadUsers &&
            !state.Session.IsLoggedOut &&
            (current == null || current.Role == ApplicationUserRole.Ghost) &&
            !Same(current?.ApplicationUserId, user.ApplicationUserId))
        {
            SetSession(state, user);
            changed = true;
        }

        return changed;
    }

    static async Task<LegacyApplicationIdentity?> LoadLegacyIdentityAsync()
    {
        var honorIdentity = await HonorIdentityService.LoadAsync();

        if (honorIdentity.IsActivated &&
            TryMapRole(honorIdentity.Role, out var honorRole))
        {
            var developerLock = await LoadDeveloperLockIfPresentAsync();

            string developerId =
                honorRole == ApplicationUserRole.Developer
                    ? developerLock?.DeveloperId?.Trim() ?? ""
                    : "";

            string displayName =
                FirstNonEmpty(
                    honorIdentity.DisplayName,
                    developerLock?.Username,
                    honorRole.ToString());

            string legacyId =
                FirstNonEmpty(
                    developerId,
                    honorIdentity.HonorOwnerId,
                    honorIdentity.PlayerId,
                    $"{honorRole}:{displayName}:{honorIdentity.ActivationDate:O}");

            return new LegacyApplicationIdentity(
                honorRole,
                displayName,
                honorIdentity.PlayerId?.Trim() ?? "",
                developerId,
                honorIdentity.HonorOwnerId?.Trim() ?? "",
                legacyId,
                "HonorIdentityService");
        }

        var developer = await LoadDeveloperLockIfPresentAsync();

        if (developer is not
            {
                IsEnabled: true,
                IsSetupCompleted: true
            } ||
            string.IsNullOrWhiteSpace(developer.DeveloperId))
        {
            return null;
        }

        return new LegacyApplicationIdentity(
            ApplicationUserRole.Developer,
            FirstNonEmpty(developer.Username, "Developer"),
            "",
            developer.DeveloperId.Trim(),
            "",
            developer.DeveloperId.Trim(),
            "DeveloperLockService");
    }

    static async Task<DeveloperLockModel?> LoadDeveloperLockIfPresentAsync()
    {
        if (!File.Exists(DeveloperLockFilePath))
            return null;

        return await ReadJsonAsync<DeveloperLockModel>(DeveloperLockFilePath);
    }

    static bool TryMapRole(
        HonorRoleType legacyRole,
        out ApplicationUserRole role)
    {
        role = legacyRole switch
        {
            HonorRoleType.Developer => ApplicationUserRole.Developer,
            HonorRoleType.Founder => ApplicationUserRole.Founder,
            HonorRoleType.Honor => ApplicationUserRole.Honor,
            _ => ApplicationUserRole.Ghost
        };

        return legacyRole != HonorRoleType.None;
    }

    static ApplicationUserModel? FindCurrentUser(
        ApplicationUserState state)
    {
        string accountId = FirstNonEmpty(
            state.Session?.CurrentAccountId,
            state.Session.ApplicationUserId);

        if (string.IsNullOrWhiteSpace(accountId))
            return null;

        return state.Users.FirstOrDefault(user =>
            Same(
                user.ApplicationUserId,
                accountId));
    }

    static ApplicationUserModel CreateGhostUser() =>
        new()
        {
            ApplicationUserId = CreateId("USR"),
            DisplayName = "Guest",
            Role = ApplicationUserRole.Ghost,
            IsTemporary = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    static PlayerProfileModel? ResolveExistingPlayerProfile(
        ApplicationUserModel user,
        IReadOnlyList<PlayerProfileModel> players)
    {
        if (!string.IsNullOrWhiteSpace(user.PlayerId))
        {
            var linked = players.FirstOrDefault(player =>
                Same(player.PlayerId, user.PlayerId));

            if (linked != null)
                return linked;
        }

        return null;
    }

    static bool MatchesRole(
        PlayerProfileModel player,
        ApplicationUserRole role) =>
        role switch
        {
            ApplicationUserRole.Developer =>
                player.IsDeveloper ||
                player.ProfileStatus == PlayerProfileStatus.Developer,
            ApplicationUserRole.Founder =>
                player.IsFounder ||
                player.ProfileStatus == PlayerProfileStatus.Founder,
            ApplicationUserRole.Honor =>
                player.ProfileStatus == PlayerProfileStatus.Honor ||
                !string.IsNullOrWhiteSpace(player.HonorOwnerId),
            ApplicationUserRole.Member =>
                player.ProfileStatus == PlayerProfileStatus.Normal,
            _ => false
        };

    static PlayerProfileModel CreatePlayerProfile(
        ApplicationUserModel user)
    {
        string playerId = string.IsNullOrWhiteSpace(user.PlayerId)
            ? CreateId("PLY")
            : user.PlayerId.Trim();

        string displayName = FirstNonEmpty(
            user.DisplayName,
            user.Role.ToString());

        return new PlayerProfileModel
        {
            PlayerId = playerId,
            PlayerName = displayName,
            ProfileStatus = MapProfileStatus(user.Role),
            IsDeveloper = user.Role == ApplicationUserRole.Developer,
            IsFounder = user.Role == ApplicationUserRole.Founder,
            HonorOwnerId = user.Role == ApplicationUserRole.Honor
                ? user.LegacyHonorOwnerId
                : "",
            CreatedAt = DateTime.Now,
            LastActiveAt = DateTime.Now,
            LastActivityAt = DateTime.Now,
            LastUpdatedAt = DateTime.Now
        };
    }

    static PlayerProfileStatus MapProfileStatus(
        ApplicationUserRole role) =>
        role switch
        {
            ApplicationUserRole.Developer =>
                PlayerProfileStatus.Developer,
            ApplicationUserRole.Founder =>
                PlayerProfileStatus.Founder,
            ApplicationUserRole.Honor =>
                PlayerProfileStatus.Honor,
            ApplicationUserRole.Member =>
                PlayerProfileStatus.Normal,
            _ => PlayerProfileStatus.Ghost
        };

    static void SetSession(
        ApplicationUserState state,
        ApplicationUserModel user)
    {
        bool sameUser =
            Same(
                state.Session.ApplicationUserId,
                user.ApplicationUserId);

        state.Session = new CurrentUserSessionModel
        {
            ApplicationUserId = user.ApplicationUserId,
            PlayerId = user.PlayerId,
            CurrentAccountId = user.ApplicationUserId,
            CurrentPlayerId = user.PlayerId,
            Role = user.Role,
            TeamId = sameUser ? state.Session.TeamId : "",
            IsLoggedOut = false,
            StartedAt =
                sameUser && state.Session.StartedAt != default
                    ? state.Session.StartedAt
                    : DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };
    }

    static bool SynchronizeSessionWithUser(
        ApplicationUserState state,
        ApplicationUserModel user)
    {
        bool changed = false;

        if (!Same(state.Session.ApplicationUserId, user.ApplicationUserId))
        {
            state.Session.ApplicationUserId = user.ApplicationUserId;
            changed = true;
        }

        if (!Same(state.Session?.CurrentAccountId, user.ApplicationUserId))
        {
            state.Session?.CurrentAccountId = user.ApplicationUserId;
            changed = true;
        }

        if (!string.Equals(
                state.Session.PlayerId?.Trim(),
                user.PlayerId?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            state.Session.PlayerId = user.PlayerId?.Trim() ?? "";
            changed = true;
        }

        if (!string.Equals(
                state.Session?.CurrentPlayerId?.Trim(),
                user.PlayerId?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            state.Session?.CurrentPlayerId = user.PlayerId?.Trim() ?? "";
            changed = true;
        }

        if (state.Session.Role != user.Role)
        {
            state.Session.Role = user.Role;
            changed = true;
        }

        if (state.Session.IsLoggedOut)
        {
            state.Session.IsLoggedOut = false;
            changed = true;
        }

        if (changed)
            state.Session.LastActiveAt = DateTime.UtcNow;

        return changed;
    }

    static void NormalizeUsers(List<ApplicationUserModel> users)
    {
        foreach (var user in users)
        {
            user.ApplicationUserId = user.ApplicationUserId?.Trim() ?? "";
            user.PlayerId = user.PlayerId?.Trim() ?? "";
            user.DisplayName = user.DisplayName?.Trim() ?? "";
            user.LegacyIdentityId = user.LegacyIdentityId?.Trim() ?? "";
            user.LegacyDeveloperId = user.LegacyDeveloperId?.Trim() ?? "";
            user.LegacyHonorOwnerId = user.LegacyHonorOwnerId?.Trim() ?? "";
            user.MigrationSource = user.MigrationSource?.Trim() ?? "";

            if (user.CreatedAt == default)
                user.CreatedAt = DateTime.UtcNow;

            if (user.UpdatedAt == default)
                user.UpdatedAt = user.CreatedAt;
        }

        users.RemoveAll(user =>
            string.IsNullOrWhiteSpace(user.ApplicationUserId));
    }

    static async Task<T?> ReadJsonAsync<T>(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return default;

            string json = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    static async Task WriteJsonSafelyAsync<T>(
        string filePath,
        T value)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(filePath)
            ?? FileSystem.AppDataDirectory);

        string temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        string backupPath = $"{filePath}.bak";

        try
        {
            string json = JsonSerializer.Serialize(value, JsonOptions);
            await File.WriteAllTextAsync(temporaryPath, json);

            if (File.Exists(filePath))
                File.Copy(filePath, backupPath, true);

            File.Move(temporaryPath, filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    static bool SetIfEmpty(
        Action<string> setter,
        string current,
        string candidate)
    {
        if (!string.IsNullOrWhiteSpace(current) ||
            string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        setter(candidate.Trim());
        return true;
    }

    static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim()
        ?? "";

    static string CreateId(string prefix) =>
        $"{prefix}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";

    static bool Same(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(
            left.Trim(),
            right.Trim(),
            StringComparison.OrdinalIgnoreCase);

    sealed class ApplicationUserState(
        List<ApplicationUserModel> users,
        CurrentUserSessionModel session)
    {
        public List<ApplicationUserModel> Users { get; } = users;

        public CurrentUserSessionModel Session { get; set; } = session;
    }

    sealed record LegacyApplicationIdentity(
        ApplicationUserRole Role,
        string DisplayName,
        string PlayerId,
        string DeveloperId,
        string HonorOwnerId,
        string LegacyIdentityId,
        string Source);
}


