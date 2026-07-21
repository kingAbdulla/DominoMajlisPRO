using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public sealed record TeamMemberOwnedVisualAsset(
    TeamOwnedAssetItem Ownership,
    CatalogAssetDisplay? CatalogAsset,
    string OwnerPlayerId);

public sealed record TeamMemberOwnedVisualResolution(
    string TeamId,
    IReadOnlyList<string> MemberPlayerIds,
    IReadOnlyList<TeamMemberOwnedVisualAsset> Assets);

public static class TeamMemberOwnedVisualResolver
{
    private static readonly HashSet<string> TeamTypeIds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            StoreProductAssetType.Emblem.ToString(),
            StoreProductAssetType.TeamColor.ToString(),
            StoreProductAssetType.EmblemBackground.ToString(),
            StoreProductAssetType.TeamEffect.ToString(),
            StoreProductAssetType.TeamNameEffect.ToString(),
            StoreProductAssetType.TeamNameFrame.ToString()
        };

    public static Task<TeamMemberOwnedVisualResolution> ResolveAsync(
        TeamProfileModel team) =>
        ResolveAsync(
            team.TeamId,
            team.Player1Id,
            team.IsSinglePlayer ? null : team.Player2Id);

    public static async Task<TeamMemberOwnedVisualResolution> ResolveAsync(
        string? teamId,
        string? player1Id,
        string? player2Id)
    {
        var normalizedTeamId =
            teamId?.Trim() ?? string.Empty;

        var memberIds =
            new[] { player1Id, player2Id }
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        var catalog =
            await StoreAssetCatalogService.LoadAsync();

        var assets =
            new List<TeamMemberOwnedVisualAsset>();

        foreach (var memberId in memberIds)
        {
            var inventory =
                await PlayerInventoryService.LoadOwnedAsync(memberId);

            foreach (var owned in inventory.Where(item => item.IsOwned && !item.IsExpired))
            {
                var typeId =
                    NormalizeTeamTypeId(owned.StoreTypeId, owned.AssetId, catalog);

                if (!IsTeamType(typeId) ||
                    RemovedStoreAssetPolicy.IsRemoved(owned.AssetId))
                {
                    continue;
                }

                var catalogAsset =
                    ResolveTeamCatalogAsset(catalog, owned.AssetId, typeId);

                if (catalogAsset == null ||
                    RemovedStoreAssetPolicy.IsRemoved(catalogAsset.AssetId, catalogAsset.PreviewImage))
                {
                    continue;
                }

                assets.Add(new TeamMemberOwnedVisualAsset(
                    new TeamOwnedAssetItem
                    {
                        TeamInventoryItemId = owned.InventoryItemId,
                        ApplicationUserId = owned.ApplicationUserId?.Trim() ?? string.Empty,
                        TeamId = normalizedTeamId,
                        TeamAssetId = owned.AssetId,
                        TeamAssetTypeId = typeId,
                        IsOwned = true,
                        IsEquipped = owned.IsEquipped,
                        AcquiredAt = owned.AcquiredAt,
                        Source = string.IsNullOrWhiteSpace(owned.Source)
                            ? "PlayerInventory"
                            : owned.Source,
                        SeasonId = owned.SeasonId,
                        CollectionId = owned.CollectionId
                    },
                    catalogAsset,
                    memberId));
            }
        }

        return new TeamMemberOwnedVisualResolution(
            normalizedTeamId,
            memberIds,
            assets
                .GroupBy(
                    asset => $"{asset.Ownership.TeamAssetTypeId}\u001F{asset.Ownership.TeamAssetId}\u001F{asset.OwnerPlayerId}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(asset => asset.Ownership.AcquiredAt).First())
                .ToList());
    }

    public static bool IsTeamType(string? typeId) =>
        TeamTypeIds.Contains(StoreAssetCatalogService.CanonicalTypeId(typeId));

    private static string NormalizeTeamTypeId(
        string? typeId,
        string assetId,
        IReadOnlyList<CatalogAssetDisplay> catalog)
    {
        var canonical =
            StoreAssetCatalogService.CanonicalTypeId(typeId);

        if (IsTeamType(canonical))
            return canonical;

        var catalogAsset =
            catalog.FirstOrDefault(item =>
                string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase));

        if (catalogAsset != null && IsTeamType(catalogAsset.AssetType.ToString()))
            return catalogAsset.AssetType.ToString();

        if (catalogAsset?.AssetType == StoreProductAssetType.TeamEffect ||
            string.Equals(catalogAsset?.EquipTarget, "TeamEffect", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(catalogAsset?.EquipTarget, "Team", StringComparison.OrdinalIgnoreCase))
        {
            return StoreProductAssetType.TeamEffect.ToString();
        }

        return canonical;
    }

    private static CatalogAssetDisplay? ResolveTeamCatalogAsset(
        IReadOnlyList<CatalogAssetDisplay> catalog,
        string? assetId,
        string? typeId)
    {
        var strict =
            StoreAssetCatalogService.Resolve(catalog, assetId, typeId);
        if (strict != null)
            return strict;

        return catalog
            .Where(item =>
                string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase) &&
                IsTeamType(item.AssetType.ToString()))
            .OrderBy(item => item.AssetType.ToString(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
}
