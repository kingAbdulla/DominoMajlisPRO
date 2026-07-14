using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class TeamEligibleAssetService
{
    private static readonly HashSet<string> TeamTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Emblem",
            "TeamColor",
            "EmblemBackground",
            "TeamEffect",
            "TeamNameEffect",
            "TeamNameFrame",
            "PlayerNameEffect",
            "PlayerNameFrame",
            "Frame",
            "ProfileBackground",
            "Effect"
        };

    public static Task<IReadOnlyList<TeamOwnedAssetItem>> GetEligibleAsync(
        TeamProfileModel team) =>
        GetEligibleAsync(team.TeamId, team.Player1Id,
            team.IsSinglePlayer ? null : team.Player2Id);

    public static async Task<IReadOnlyList<TeamOwnedAssetItem>> GetEligibleAsync(
        string? teamId,
        string? player1Id,
        string? player2Id)
    {
        var result = CreateDefaults(teamId).ToList();
        var playerIds = new[] { player1Id, player2Id }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var playerId in playerIds)
        {
            var owned = await PlayerInventoryService.LoadOwnedAsync(playerId);
            var session = await DominoMajlisPRO.Services.ApplicationUserService.EnsureCurrentSessionAsync();
            var appUserId = session.ApplicationUserId ?? string.Empty;
            result.AddRange(owned
                .Where(item => IsTeamType(item.StoreTypeId) &&
                               !RemovedStoreAssetPolicy.IsRemoved(item.AssetId))
                .Select(item => new TeamOwnedAssetItem
                {
                    TeamInventoryItemId = item.InventoryItemId,
                    ApplicationUserId = appUserId,
                    TeamId = teamId?.Trim() ?? string.Empty,
                    TeamAssetId = item.AssetId,
                    TeamAssetTypeId =
                        StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId),
                    IsOwned = true,
                    AcquiredAt = item.AcquiredAt,
                    Source = item.Source,
                    SeasonId = item.SeasonId,
                    CollectionId = item.CollectionId
                }));
        }


        return result
            .Where(item => !RemovedStoreAssetPolicy.IsRemoved(item.TeamAssetId))
            .GroupBy(
                item => $"{StoreAssetCatalogService.CanonicalTypeId(item.TeamAssetTypeId)}\u001F{item.TeamAssetId}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item =>
                    TeamAssetPayloadCatalog.IsDefaultTeamAsset(item.TeamAssetId))
                .ThenByDescending(item => item.AcquiredAt)
                .First())
            .ToList();
    }

    private static IEnumerable<TeamOwnedAssetItem> CreateDefaults(
        string? teamId)
    {
        foreach (var payload in TeamAssetPayloadCatalog.GetDefaultTeamPayloads())
        {
            yield return new TeamOwnedAssetItem
            {
                TeamInventoryItemId =
                    $"DEFAULT-{payload.TeamAssetTypeId}-{payload.TeamAssetId}",
                TeamId = teamId?.Trim() ?? string.Empty,
                TeamAssetId = payload.TeamAssetId,
                TeamAssetTypeId = payload.TeamAssetTypeId,
                IsOwned = false,
                AcquiredAt = new DateTime(
                    2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Source = "Default"
            };
        }
    }

    private static bool IsTeamType(string? assetType) =>
        TeamTypes.Contains(
            StoreAssetCatalogService.CanonicalTypeId(assetType));
}
