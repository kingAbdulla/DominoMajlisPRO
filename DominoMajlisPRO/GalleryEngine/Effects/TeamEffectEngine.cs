using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class TeamEffectEngine
{
    public static async Task ApplyAsync(
        Image overlaySlot,
        string? teamId,
        double baseScale = 1.18,
        bool lightweight = false)
    {
        var team = string.IsNullOrWhiteSpace(teamId)
            ? null
            : await TeamProfileService.GetTeamByIdAsync(teamId);
        await ApplyAsync(overlaySlot, team, baseScale, lightweight);
    }

    public static async Task ApplyAsync(
        Image overlaySlot,
        TeamProfileModel? team,
        double baseScale = 1.18,
        bool lightweight = false)
    {
        if (team == null || string.IsNullOrWhiteSpace(team.EquippedTeamEffectAssetId))
        {
            IdentityEffectRenderer.Clear(overlaySlot);
            return;
        }

        var effect = await StoreAssetCatalogService.ResolveAsync(
            team.EquippedTeamEffectAssetId,
            StoreProductAssetType.TeamEffect.ToString());

        if (effect == null) { IdentityEffectRenderer.Clear(overlaySlot); return; }

        IdentityEffectRenderer.Apply(overlaySlot, effect, baseScale, lightweight);
    }

    public static async Task<bool> EquipAsync(string playerId, string teamId, string? assetId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(teamId))
            return false;

        var team = await TeamProfileService.GetTeamByIdAsync(teamId);
        if (team == null || !IsManagedBy(team, playerId))
            return false;

        if (!string.IsNullOrWhiteSpace(assetId))
        {
            var inventory = await PlayerInventoryService.LoadOwnedAsync(playerId);
            if (!inventory.Any(item =>
                    string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(StoreAssetCatalogService.CanonicalTypeId(item.StoreTypeId),
                        StoreProductAssetType.TeamEffect.ToString(),
                        StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        var teams = await TeamProfileService.LoadTeamsAsync();
        var stored = teams.FirstOrDefault(item =>
            string.Equals(item.TeamId, teamId, StringComparison.OrdinalIgnoreCase));
        if (stored == null)
            return false;

        stored.EquippedTeamEffectAssetId = assetId?.Trim() ?? string.Empty;
        stored.EquippedTeamEffectOwnerPlayerId =
            string.IsNullOrWhiteSpace(assetId) ? string.Empty : playerId.Trim();
        await TeamProfileService.SaveTeamsAsync(teams);
        AppEvents.RaiseTeamEffectChanged(teamId);
        AppEvents.RaiseTeamAssetsChanged(teamId);
        return true;
    }

    public static bool IsManagedBy(TeamProfileModel team, string playerId) =>
        string.Equals(team.Player1Id, playerId, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(team.Player2Id, playerId, StringComparison.OrdinalIgnoreCase);

    public static async Task ApplyAroundAsync(
        Image emblem,
        string? teamId,
        double baseScale = 1.18,
        bool lightweight = false)
    {
        var team = string.IsNullOrWhiteSpace(teamId)
            ? null
            : await TeamProfileService.GetTeamByIdAsync(teamId);
        CatalogAssetDisplay? effect = null;
        if (!string.IsNullOrWhiteSpace(team?.EquippedTeamEffectAssetId))
        {
            effect = await StoreAssetCatalogService.ResolveAsync(
                team.EquippedTeamEffectAssetId,
                StoreProductAssetType.TeamEffect.ToString());

            
        }

        IdentityEffectRenderer.ApplyAround(emblem, effect, baseScale, lightweight);
    }
}

public static class TeamEffectBehavior
{
    public static readonly BindableProperty TeamIdProperty = BindableProperty.CreateAttached(
        "TeamId", typeof(string), typeof(TeamEffectBehavior), string.Empty,
        propertyChanged: OnTeamIdChanged);
    public static readonly BindableProperty LightweightProperty = BindableProperty.CreateAttached(
        "Lightweight", typeof(bool), typeof(TeamEffectBehavior), false);
    private static readonly BindableProperty IsHookedProperty = BindableProperty.CreateAttached(
        "IsHooked", typeof(bool), typeof(TeamEffectBehavior), false);

    public static string GetTeamId(BindableObject view) => (string)view.GetValue(TeamIdProperty);
    public static void SetTeamId(BindableObject view, string value) => view.SetValue(TeamIdProperty, value);
    public static bool GetLightweight(BindableObject view) => (bool)view.GetValue(LightweightProperty);
    public static void SetLightweight(BindableObject view, bool value) => view.SetValue(LightweightProperty, value);

    private static void OnTeamIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Image image)
            return;
        if (image.IsLoaded)
        {
            _ = TeamEffectEngine.ApplyAroundAsync(
                image, newValue?.ToString(), 1.18, GetLightweight(image));
            return;
        }
        if ((bool)image.GetValue(IsHookedProperty))
            return;
        image.SetValue(IsHookedProperty, true);
        image.Loaded += OnImageLoaded;
    }

    private static void OnImageLoaded(object? sender, EventArgs e)
    {
        if (sender is Image image)
            _ = TeamEffectEngine.ApplyAroundAsync(
                image, GetTeamId(image), 1.18, GetLightweight(image));
    }
}


