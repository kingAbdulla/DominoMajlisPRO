using DominoMajlisPRO.GalleryEngine.Admin.Models;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.VisualIdentity;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class TeamEffectEngine
{
    public const double DefaultTeamEffectScale = 1.08;

    public static async Task ApplyAsync(
        Image overlaySlot,
        string? teamId,
        double baseScale = DefaultTeamEffectScale,
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
        double baseScale = DefaultTeamEffectScale,
        bool lightweight = false)
    {
        var effect = await ResolveTeamEffectAsync(team);
        IdentityEffectRenderer.ApplyAround(overlaySlot, effect, baseScale, lightweight);
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
            var catalog = await StoreAssetCatalogService.LoadAsync();
            var ownsEligibleEffect = false;

            foreach (var item in inventory.Where(item =>
                         CanonicalAssetIdentityService.SameAssetId(item.AssetId, assetId)))
            {
                var effect = ResolveTeamEffectFromCatalog(catalog, item.AssetId, item.StoreTypeId);
                if (effect != null)
                {
                    ownsEligibleEffect = true;
                    break;
                }
            }

            if (!ownsEligibleEffect)
                return false;
        }

        var teams = await TeamProfileService.LoadTeamsAsync();
        var stored = teams.FirstOrDefault(item =>
            string.Equals(item.TeamId, teamId, StringComparison.OrdinalIgnoreCase));
        if (stored == null)
            return false;

        var previousAssetId = stored.EquippedTeamEffectAssetId;
        stored.EquippedTeamEffectAssetId = assetId?.Trim() ?? string.Empty;
        stored.EquippedTeamEffectOwnerPlayerId =
            string.IsNullOrWhiteSpace(assetId) ? string.Empty : playerId.Trim();
        await TeamProfileService.SaveTeamsAsync(teams);

        var payload = new Dictionary<string, object>
        {
            { "TeamId", teamId },
            { "EffectAssetId", assetId ?? string.Empty },
            { "TimestampUtc", DateTimeOffset.UtcNow }
        };

        if (!string.IsNullOrWhiteSpace(previousAssetId))
            payload["PreviousEffectAssetId"] = previousAssetId;

        VisualEventBus.Publish(
            EventCategory.Team,
            VisualIdentityEventNames.TeamEffectChanged,
            payload,
            isSticky: true,
            stickyExpirationMs: 0);

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
        double baseScale = DefaultTeamEffectScale,
        bool lightweight = false)
    {
        var team = string.IsNullOrWhiteSpace(teamId)
            ? null
            : await TeamProfileService.GetTeamByIdAsync(teamId);

        var effect = await ResolveTeamEffectAsync(team);
        IdentityEffectRenderer.ApplyAround(emblem, effect, baseScale, lightweight);
    }

    private static async Task<CatalogAssetDisplay?> ResolveTeamEffectAsync(TeamProfileModel? team)
    {
        if (team == null || string.IsNullOrWhiteSpace(team.EquippedTeamEffectAssetId))
            return null;

        var catalog = await StoreAssetCatalogService.LoadAsync();
        return ResolveTeamEffectFromCatalog(
            catalog,
            team.EquippedTeamEffectAssetId,
            StoreProductAssetType.TeamEffect.ToString());
    }

    private static CatalogAssetDisplay? ResolveTeamEffectFromCatalog(
        IReadOnlyList<CatalogAssetDisplay> catalog,
        string? assetId,
        string? storeTypeId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
            return null;

        var teamTyped = StoreAssetCatalogService.Resolve(
            catalog,
            assetId,
            StoreProductAssetType.TeamEffect.ToString());
        if (teamTyped != null)
            return teamTyped;

        var legacyEffect = StoreAssetCatalogService.Resolve(
            catalog,
            assetId,
            StoreProductAssetType.Effect.ToString());
        if (legacyEffect == null)
            return null;

        var canonicalTypeId = StoreAssetCatalogService.CanonicalTypeId(storeTypeId);
        return IsTeamEffectTarget(legacyEffect) ||
               string.Equals(canonicalTypeId, StoreProductAssetType.Effect.ToString(), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(canonicalTypeId, StoreProductAssetType.TeamEffect.ToString(), StringComparison.OrdinalIgnoreCase)
            ? legacyEffect
            : null;
    }

    private static bool IsTeamEffectTarget(CatalogAssetDisplay effect) =>
        effect.AssetType == StoreProductAssetType.TeamEffect ||
        string.Equals(effect.EquipTarget, "TeamEffect", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(effect.EquipTarget, "Team", StringComparison.OrdinalIgnoreCase);
}

public static class TeamEffectBehavior
{
    public static readonly BindableProperty TeamIdProperty = BindableProperty.CreateAttached(
        "TeamId", typeof(string), typeof(TeamEffectBehavior), string.Empty,
        propertyChanged: OnTeamIdChanged);

    public static readonly BindableProperty LightweightProperty = BindableProperty.CreateAttached(
        "Lightweight", typeof(bool), typeof(TeamEffectBehavior), false,
        propertyChanged: OnLightweightChanged);

    private static readonly BindableProperty IsHookedProperty = BindableProperty.CreateAttached(
        "IsHooked", typeof(bool), typeof(TeamEffectBehavior), false);

    private static readonly BindableProperty RefreshHandlerProperty = BindableProperty.CreateAttached(
        "RefreshHandler", typeof(Action<string>), typeof(TeamEffectBehavior), null);

    public static string GetTeamId(BindableObject view) => (string)view.GetValue(TeamIdProperty);
    public static void SetTeamId(BindableObject view, string value) => view.SetValue(TeamIdProperty, value);
    public static bool GetLightweight(BindableObject view) => (bool)view.GetValue(LightweightProperty);
    public static void SetLightweight(BindableObject view, bool value) => view.SetValue(LightweightProperty, value);

    private static Action<string>? GetRefreshHandler(BindableObject view) =>
        (Action<string>?)view.GetValue(RefreshHandlerProperty);

    private static void SetRefreshHandler(BindableObject view, Action<string>? value) =>
        view.SetValue(RefreshHandlerProperty, value);

    private static void OnTeamIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Image image)
            return;

        EnsureHooked(image);
        Refresh(image);
    }

    private static void OnLightweightChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is Image image)
            Refresh(image);
    }

    private static void EnsureHooked(Image image)
    {
        if ((bool)image.GetValue(IsHookedProperty))
            return;

        image.SetValue(IsHookedProperty, true);
        image.Loaded += OnImageLoaded;
        image.Unloaded += OnImageUnloaded;

        Action<string> handler = changedTeamId =>
        {
            var currentTeamId = GetTeamId(image);
            if (string.IsNullOrWhiteSpace(currentTeamId) ||
                !string.Equals(currentTeamId, changedTeamId, StringComparison.OrdinalIgnoreCase))
                return;

            MainThread.BeginInvokeOnMainThread(() => Refresh(image));
        };

        SetRefreshHandler(image, handler);
        AppEvents.TeamEffectChanged += handler;
    }

    private static void OnImageLoaded(object? sender, EventArgs e)
    {
        if (sender is Image image)
            Refresh(image);
    }

    private static void OnImageUnloaded(object? sender, EventArgs e)
    {
        if (sender is not Image image)
            return;

        image.Loaded -= OnImageLoaded;
        image.Unloaded -= OnImageUnloaded;

        var handler = GetRefreshHandler(image);
        if (handler != null)
            AppEvents.TeamEffectChanged -= handler;

        SetRefreshHandler(image, null);
        image.SetValue(IsHookedProperty, false);
    }

    private static void Refresh(Image image)
    {
        if (!image.IsLoaded)
            return;

        _ = TeamEffectEngine.ApplyAroundAsync(
            image,
            GetTeamId(image),
            TeamEffectEngine.DefaultTeamEffectScale,
            GetLightweight(image));
    }
}
