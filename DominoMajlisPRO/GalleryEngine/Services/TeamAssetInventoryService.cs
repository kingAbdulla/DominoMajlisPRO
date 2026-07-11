using DominoMajlisPRO.GalleryEngine.Admin.Core;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class TeamAssetInventoryService
{
    private const string FileName = "team_owned_assets.json";
    private const string UnknownType = "Unknown";
    private const string DefaultSource = "Default";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<IReadOnlyList<TeamOwnedAssetItem>> GetInventoryForTeamAsync(string teamId)
    {
        ValidateTeamId(teamId);
        var appUserId = (await ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;

        var purchasedItems = (await LoadAsync())
            .Where(item =>
                SameId(item.ApplicationUserId, appUserId) &&
                SameId(item.TeamId, teamId))
            .ToList();

        var defaultItems = CreateDefaultAssets(teamId).ToList();

        return defaultItems
            .Concat(purchasedItems)
            .GroupBy(OwnershipKey, StringComparer.OrdinalIgnoreCase)
            .Select(MergeOwnership)
            .OrderBy(item => IsDefaultAsset(item) ? 0 : 1)
            .ThenBy(item => item.TeamAssetTypeId)
            .ThenBy(item => item.TeamAssetId)
            .ToList();
    }

    public static async Task<bool> IsOwnedAsync(string teamId, string teamAssetId)
    {
        ValidateIdentity(teamId, teamAssetId);
        var appUserId = (await ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;

        if (TeamAssetPayloadCatalog.IsDefaultTeamAsset(teamAssetId))
            return true;

        return (await LoadAsync()).Any(item =>
            SameId(item.ApplicationUserId, appUserId) &&
            SameLegacyOwnership(item, teamId, teamAssetId) && item.IsOwned);
    }

    public static async Task<bool> IsOwnedAsync(
        string teamId,
        string teamAssetId,
        string teamAssetTypeId)
    {
        ValidateIdentity(teamId, teamAssetId, teamAssetTypeId);
        var appUserId = (await ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;

        if (TeamAssetPayloadCatalog.IsDefaultTeamAsset(teamAssetId))
        {
            var payload = TeamAssetPayloadCatalog.Resolve(teamAssetId);

            return payload != null &&
                   SameId(payload.TeamAssetTypeId, teamAssetTypeId);
        }

        return (await LoadAsync()).Any(item =>
            SameId(item.ApplicationUserId, appUserId) &&
            SameOwnership(item, teamId, teamAssetId, teamAssetTypeId) &&
            item.IsOwned);
    }

    public static async Task<bool> AddOwnedAssetAsync(
        string teamId,
        string teamAssetId,
        string teamAssetTypeId,
        string source = "TeamAsset",
        string? seasonId = null,
        string? collectionId = null)
    {
        ValidateIdentity(teamId, teamAssetId, teamAssetTypeId);

        if (TeamAssetPayloadCatalog.IsDefaultTeamAsset(teamAssetId))
        {
            AppEvents.RaiseTeamAssetsChanged(teamId);
            return false;
        }

        var added = false;
        var migratedLegacyType = false;

        // Resolve current application user id outside the gate to avoid cross-locks.
        var appUserId = (await ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;

        await Gate.WaitAsync();

        try
        {
            var records = await LoadAsync();

            if (records.Any(item =>
                    string.Equals(item.ApplicationUserId ?? string.Empty, appUserId, StringComparison.OrdinalIgnoreCase) &&
                    SameOwnership(
                        item,
                        teamId,
                        teamAssetId,
                        teamAssetTypeId)))
            {
                return false;
            }

            var legacyRecord = records.FirstOrDefault(item =>
                SameLegacyOwnership(item, teamId, teamAssetId) &&
                SameId(item.TeamAssetTypeId, UnknownType));

            if (legacyRecord != null)
            {
                legacyRecord.TeamAssetTypeId = teamAssetTypeId.Trim();
                legacyRecord.ApplicationUserId = string.IsNullOrWhiteSpace(legacyRecord.ApplicationUserId)
                    ? appUserId
                    : legacyRecord.ApplicationUserId.Trim();
                await SaveAsync(records);
                migratedLegacyType = true;
            }
            else
            {
                records.Add(new TeamOwnedAssetItem
                {
                    TeamInventoryItemId = Guid.NewGuid().ToString(),
                    ApplicationUserId = appUserId,
                    TeamId = teamId.Trim(),
                    TeamAssetId = teamAssetId.Trim(),
                    TeamAssetTypeId = teamAssetTypeId.Trim(),
                    IsOwned = true,
                    IsEquipped = false,
                    AcquiredAt = DateTime.UtcNow,
                    Source = string.IsNullOrWhiteSpace(source)
                        ? "TeamAsset"
                        : source.Trim(),
                    SeasonId = NormalizeOptionalId(seasonId),
                    CollectionId = NormalizeOptionalId(collectionId)
                });

                await SaveAsync(records);
                added = true;
            }
        }
        finally
        {
            Gate.Release();
        }

        if (added || migratedLegacyType)
            AppEvents.RaiseTeamAssetsChanged(teamId);

        return added;
    }

    public static async Task<bool> EquipAsync(string teamId, string teamAssetId)
    {
        ValidateIdentity(teamId, teamAssetId);

        var inventory = await GetInventoryForTeamAsync(teamId);

        var target = inventory.FirstOrDefault(item =>
            SameLegacyOwnership(item, teamId, teamAssetId) &&
            item.IsOwned);

        return target != null &&
               await EquipAsync(
                   teamId,
                   teamAssetId,
                   target.TeamAssetTypeId);
    }

    public static async Task<bool> EquipAsync(
        string teamId,
        string teamAssetId,
        string teamAssetTypeId)
    {
        ValidateIdentity(teamId, teamAssetId, teamAssetTypeId);
        var appUserId = (await ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;

        if (!await IsOwnedAsync(teamId, teamAssetId, teamAssetTypeId))
            return false;

        var changed = false;

        await Gate.WaitAsync();

        try
        {
            var records = await LoadAsync();

            foreach (var item in records.Where(item =>
                         SameId(item.ApplicationUserId, appUserId) &&
                         SameId(item.TeamId, teamId) &&
                         SameId(item.TeamAssetTypeId, teamAssetTypeId)))
            {
                if (item.IsEquipped)
                {
                    item.IsEquipped = false;
                    changed = true;
                }
            }

            var target = records.FirstOrDefault(item =>
                SameId(item.ApplicationUserId, appUserId) &&
                SameOwnership(
                    item,
                    teamId,
                    teamAssetId,
                    teamAssetTypeId));

            if (target == null)
            {
                target = new TeamOwnedAssetItem
                {
                    TeamInventoryItemId = Guid.NewGuid().ToString(),
                    ApplicationUserId = appUserId,
                    TeamId = teamId.Trim(),
                    TeamAssetId = teamAssetId.Trim(),
                    TeamAssetTypeId = teamAssetTypeId.Trim(),
                    IsOwned = true,
                    IsEquipped = true,
                    AcquiredAt = DateTime.UtcNow,
                    Source = TeamAssetPayloadCatalog.IsDefaultTeamAsset(teamAssetId)
                        ? DefaultSource
                        : "TeamAsset"
                };

                records.Add(target);
                changed = true;
            }
            else
            {
                if (!target.IsOwned)
                {
                    target.IsOwned = true;
                    changed = true;
                }

                if (!target.IsEquipped)
                {
                    target.IsEquipped = true;
                    changed = true;
                }
            }

            if (changed)
                await SaveAsync(records);
        }
        finally
        {
            Gate.Release();
        }

        if (changed)
            AppEvents.RaiseTeamAssetsChanged(teamId);

        return true;
    }

    public static async Task<bool> UnequipAsync(string teamId, string teamAssetId)
    {
        ValidateIdentity(teamId, teamAssetId);
        var appUserId = (await ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;

        var changed = false;

        await Gate.WaitAsync();

        try
        {
            var records = await LoadAsync();

            var target = records.FirstOrDefault(item =>
                SameId(item.ApplicationUserId, appUserId) &&
                SameLegacyOwnership(item, teamId, teamAssetId));

            if (target == null || !target.IsEquipped)
                return false;

            target.IsEquipped = false;

            if (TeamAssetPayloadCatalog.IsDefaultTeamAsset(target.TeamAssetId) &&
                SameId(target.Source, DefaultSource))
            {
                target.IsOwned = true;
            }

            await SaveAsync(records);
            changed = true;
        }
        finally
        {
            Gate.Release();
        }

        if (changed)
            AppEvents.RaiseTeamAssetsChanged(teamId);

        return true;
    }

    public static async Task<TeamOwnedAssetItem?> GetEquippedAsync(
        string teamId,
        string teamAssetTypeId)
    {
        ValidateTeamId(teamId);
        ValidateTypeId(teamAssetTypeId);

        return (await GetInventoryForTeamAsync(teamId))
            .FirstOrDefault(item =>
                SameId(item.TeamId, teamId) &&
                SameId(item.TeamAssetTypeId, teamAssetTypeId) &&
                item.IsOwned &&
                item.IsEquipped);
    }

    private static IEnumerable<TeamOwnedAssetItem> CreateDefaultAssets(string teamId)
    {
        var acquiredAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (var payload in TeamAssetPayloadCatalog.GetDefaultTeamPayloads())
        {
            yield return new TeamOwnedAssetItem
            {
                TeamInventoryItemId =
                    $"DEFAULT-{teamId.Trim()}-{payload.TeamAssetTypeId}-{payload.TeamAssetId}",
                ApplicationUserId = string.Empty, // default assets have no owner application user id
                TeamId = teamId.Trim(),
                TeamAssetId = payload.TeamAssetId.Trim(),
                TeamAssetTypeId = payload.TeamAssetTypeId.Trim(),
                IsOwned = false,
                IsEquipped = false,
                AcquiredAt = acquiredAt,
                Source = DefaultSource,
                SeasonId = null,
                CollectionId = null
            };
        }
    }

    private static async Task<List<TeamOwnedAssetItem>> LoadAsync()
    {
        var records =
            await StoreCmsJsonRepository.LoadListAsync<TeamOwnedAssetItem>(StoragePath);

        var changed = false;

        foreach (var item in records)
            changed |= Normalize(item);

        var validRecords = records.Where(HasIdentity).ToList();

        var legacyRecordsWithoutIdentity =
            records.Where(item => !HasIdentity(item)).ToList();

        // Deduplicate ownership by application user + team + asset id + type id.
        var deduplicated = validRecords
            .GroupBy(OwnershipKey, StringComparer.OrdinalIgnoreCase)
            .Select(MergeOwnership)
            .ToList();

        changed |= deduplicated.Count != validRecords.Count;

        foreach (var equippedGroup in deduplicated
                     .Where(item => item.IsOwned && item.IsEquipped)
                     .GroupBy(EquippedTypeKey, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var duplicate in equippedGroup
                         .OrderByDescending(item => item.AcquiredAt)
                         .Skip(1))
            {
                duplicate.IsEquipped = false;
                changed = true;
            }
        }

        deduplicated.AddRange(legacyRecordsWithoutIdentity);

        if (changed)
            await SaveAsync(deduplicated);

        return deduplicated;
    }

    private static TeamOwnedAssetItem MergeOwnership(
        IGrouping<string, TeamOwnedAssetItem> group)
    {
        var records = group
            .OrderByDescending(item => item.IsEquipped)
            .ThenByDescending(item => !SameId(item.Source, DefaultSource))
            .ThenByDescending(item => item.AcquiredAt)
            .ToList();

        var merged = records[0];

        merged.ApplicationUserId = records.Select(r => r.ApplicationUserId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        merged.IsOwned = records.Any(item => item.IsOwned);
        merged.IsEquipped = merged.IsOwned && records.Any(item => item.IsEquipped);

        merged.TeamAssetTypeId = records
            .Select(item => item.TeamAssetTypeId)
            .FirstOrDefault(typeId =>
                !string.IsNullOrWhiteSpace(typeId) &&
                !SameId(typeId, UnknownType))
            ?? UnknownType;

        merged.Source = records
            .Select(item => item.Source)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? "TeamAsset";

        merged.SeasonId ??= records
            .Select(item => item.SeasonId)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        merged.CollectionId ??= records
            .Select(item => item.CollectionId)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return merged;
    }

    private static bool Normalize(TeamOwnedAssetItem item)
    {
        var before =
            $"{item.TeamInventoryItemId}|{item.ApplicationUserId}|{item.TeamId}|{item.TeamAssetId}|{item.TeamAssetTypeId}|{item.AcquiredAt:O}|{item.Source}|{item.SeasonId}|{item.CollectionId}";

        item.TeamInventoryItemId =
            string.IsNullOrWhiteSpace(item.TeamInventoryItemId)
                ? Guid.NewGuid().ToString()
                : item.TeamInventoryItemId.Trim();

        item.ApplicationUserId = item.ApplicationUserId?.Trim() ?? string.Empty;
        item.TeamId = item.TeamId?.Trim() ?? string.Empty;
        item.TeamAssetId = item.TeamAssetId?.Trim() ?? string.Empty;

        item.TeamAssetTypeId =
            string.IsNullOrWhiteSpace(item.TeamAssetTypeId)
                ? UnknownType
                : item.TeamAssetTypeId.Trim();

        item.AcquiredAt =
            item.AcquiredAt == default
                ? DateTime.UtcNow
                : item.AcquiredAt;

        item.Source =
            string.IsNullOrWhiteSpace(item.Source)
                ? "TeamAsset"
                : item.Source.Trim();

        item.SeasonId = NormalizeOptionalId(item.SeasonId);
        item.CollectionId = NormalizeOptionalId(item.CollectionId);

        if (!item.IsOwned)
            item.IsEquipped = false;

        if (TeamAssetPayloadCatalog.IsDefaultTeamAsset(item.TeamAssetId))
            item.IsOwned = true;

        var after =
            $"{item.TeamInventoryItemId}|{item.ApplicationUserId}|{item.TeamId}|{item.TeamAssetId}|{item.TeamAssetTypeId}|{item.AcquiredAt:O}|{item.Source}|{item.SeasonId}|{item.CollectionId}";

        return !string.Equals(before, after, StringComparison.Ordinal);
    }

    private static bool SameOwnership(
        TeamOwnedAssetItem item,
        string teamId,
        string teamAssetId,
        string teamAssetTypeId) =>
        SameLegacyOwnership(item, teamId, teamAssetId) &&
        SameId(item.TeamAssetTypeId, teamAssetTypeId);

    private static bool SameLegacyOwnership(
        TeamOwnedAssetItem item,
        string teamId,
        string teamAssetId) =>
        SameId(item.TeamId, teamId) &&
        SameId(item.TeamAssetId, teamAssetId);

    private static bool HasIdentity(TeamOwnedAssetItem item) =>
        !string.IsNullOrWhiteSpace(item.TeamId) &&
        !string.IsNullOrWhiteSpace(item.TeamAssetId);

    private static bool IsDefaultAsset(TeamOwnedAssetItem item) =>
        TeamAssetPayloadCatalog.IsDefaultTeamAsset(item.TeamAssetId) ||
        SameId(item.Source, DefaultSource);

    private static string OwnershipKey(TeamOwnedAssetItem item) =>
        $"{item.ApplicationUserId}\u001F{item.TeamId}\u001F{item.TeamAssetId}\u001F{item.TeamAssetTypeId}";

    private static string EquippedTypeKey(TeamOwnedAssetItem item) =>
        $"{item.ApplicationUserId}\u001F{item.TeamId}\u001F{item.TeamAssetTypeId}";

    private static bool SameId(string? left, string? right) =>
        string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeOptionalId(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateIdentity(
        string teamId,
        string teamAssetId,
        string? teamAssetTypeId = null)
    {
        ValidateTeamId(teamId);

        if (string.IsNullOrWhiteSpace(teamAssetId))
            throw new ArgumentException("TeamAssetId is required.", nameof(teamAssetId));

        if (teamAssetTypeId != null)
            ValidateTypeId(teamAssetTypeId);
    }

    private static void ValidateTeamId(string teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            throw new ArgumentException("TeamId is required.", nameof(teamId));
    }

    private static void ValidateTypeId(string teamAssetTypeId)
    {
        if (string.IsNullOrWhiteSpace(teamAssetTypeId))
        {
            throw new ArgumentException(
                "TeamAssetTypeId is required.",
                nameof(teamAssetTypeId));
        }
    }

    private static string StoragePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    private static Task SaveAsync(IReadOnlyList<TeamOwnedAssetItem> records) =>
        StoreCmsJsonRepository.SaveListAsync(StoragePath, records);
}
