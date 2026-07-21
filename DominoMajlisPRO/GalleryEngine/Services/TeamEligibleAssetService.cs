using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class TeamEligibleAssetService
{
    public static Task<IReadOnlyList<TeamOwnedAssetItem>> GetEligibleAsync(
        TeamProfileModel team) =>
        GetEligibleAsync(team.TeamId, team.Player1Id,
            team.IsSinglePlayer ? null : team.Player2Id);

    public static async Task<IReadOnlyList<TeamOwnedAssetItem>> GetEligibleAsync(
        string? teamId,
        string? player1Id,
        string? player2Id)
    {
        var result =
            CreateDefaults(teamId).ToList();

        var memberAssets =
            await TeamMemberOwnedVisualResolver.ResolveAsync(
                teamId,
                player1Id,
                player2Id);

        result.AddRange(
            memberAssets.Assets.Select(asset => asset.Ownership));


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
}
