using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.Models;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class TeamIdentityResolver
{
    private const string DefaultEmblemPath = "shield_3d.png";
    private const string DefaultTeamColorHex = "#FFD700";
    private const string DefaultEmblemBackground = "Transparent";

    public static async Task<TeamIdentityModel> ResolveAsync(string? teamId)
    {
        var normalizedTeamId = NormalizeId(teamId);
        if (normalizedTeamId.Length == 0)
            return await GetDefaultIdentityAsync(normalizedTeamId);

        TeamProfileModel? legacyProfile = null;
        try
        {
            legacyProfile =
                await TeamProfileService.GetTeamByIdAsync(normalizedTeamId);
        }
        catch
        {
            // Identity resolution must remain available when legacy JSON is unavailable.
        }

        var defaultIdentity = await GetDefaultIdentityAsync(normalizedTeamId);
        var catalog = await StoreAssetCatalogService.LoadAsync();
        var legacyEmblem = ValidImagePayload(legacyProfile?.Emblem);
        var legacyColor = ValidColorHex(legacyProfile?.ColorHex);
        var legacyBackground = ValidImagePayload(legacyProfile?.EmblemBackground)
            ?? ValidBackgroundColor(legacyProfile?.EmblemBackground);
        var emblemPayload = TeamAssetPayloadCatalog.Resolve(
            legacyProfile?.EmblemAssetId,
            TeamAssetTypes.Emblem.TeamAssetTypeId);
        var colorPayload = TeamAssetPayloadCatalog.Resolve(
            legacyProfile?.TeamColorAssetId,
            TeamAssetPayloadCatalog.TeamColorTypeId);
        var backgroundPayload = TeamAssetPayloadCatalog.Resolve(
            legacyProfile?.EmblemBackgroundAssetId,
            TeamAssetTypes.EmblemBackground.TeamAssetTypeId);
        var catalogEmblem = StoreAssetCatalogService.Resolve(
            catalog, legacyProfile?.EmblemAssetId, "Emblem");
        var catalogColor = StoreAssetCatalogService.Resolve(
            catalog, legacyProfile?.TeamColorAssetId, "TeamColor");
        var catalogBackground = StoreAssetCatalogService.Resolve(
            catalog, legacyProfile?.EmblemBackgroundAssetId, "EmblemBackground");
        var resolvedEmblem = ValidImagePayload(catalogEmblem?.PreviewImage)
            ?? ValidImagePayload(emblemPayload?.ImagePath)
            ?? legacyEmblem
            ?? defaultIdentity.EmblemImagePath;
        var resolvedColor = ValidColorHex(catalogColor?.ColorHex)
            ?? ValidColorHex(colorPayload?.ColorHex)
            ?? legacyColor
            ?? defaultIdentity.TeamColorHex;
        var resolvedBackground =
            ValidImagePayload(catalogBackground?.PreviewImage)
            ?? ValidBackgroundColor(catalogBackground?.ColorHex)
            ?? ValidImagePayload(backgroundPayload?.BackgroundImagePath)
            ?? ValidBackgroundColor(backgroundPayload?.BackgroundColorHex)
            ?? legacyBackground
            ?? defaultIdentity.EmblemBackgroundSource;

        return new TeamIdentityModel
        {
            TeamId = normalizedTeamId,
            TeamName = legacyProfile?.TeamName?.Trim() ?? string.Empty,
            EmblemAssetId = NormalizeOptionalId(legacyProfile?.EmblemAssetId),
            EmblemImagePath = resolvedEmblem,
            EmblemBackgroundAssetId =
                NormalizeOptionalId(legacyProfile?.EmblemBackgroundAssetId),
            EmblemBackgroundSource = resolvedBackground,
            TeamColorHex = resolvedColor,
            HasCustomEmblem =
                catalogEmblem != null || emblemPayload != null ||
                (legacyEmblem != null &&
                 !SameValue(legacyEmblem, defaultIdentity.EmblemImagePath)),
            HasCustomEmblemBackground =
                catalogBackground != null || backgroundPayload != null ||
                (legacyBackground != null &&
                 !SameValue(legacyBackground, defaultIdentity.EmblemBackgroundSource)),
            HasTeamColor =
                catalogColor != null || colorPayload != null ||
                (legacyColor != null &&
                 !SameValue(legacyColor, defaultIdentity.TeamColorHex)),
            ResolvedAt = DateTime.UtcNow
        };
    }

    public static async Task<IReadOnlyDictionary<string, TeamIdentityModel>> ResolveManyAsync(
        IEnumerable<string?>? teamIds)
    {
        var uniqueIds = (teamIds ?? Array.Empty<string?>())
            .Select(NormalizeId)
            .Where(teamId => teamId.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var identities = await Task.WhenAll(uniqueIds.Select(ResolveAsync));
        return identities.ToDictionary(
            identity => identity.TeamId,
            StringComparer.OrdinalIgnoreCase);
    }

    public static Task<TeamIdentityModel> GetDefaultIdentityAsync(string? teamId)
    {
        var identity = new TeamIdentityModel
        {
            TeamId = NormalizeId(teamId),
            EmblemImagePath = DefaultEmblemPath,
            EmblemBackgroundSource = DefaultEmblemBackground,
            TeamColorHex = DefaultTeamColorHex,
            ResolvedAt = DateTime.UtcNow
        };

        return Task.FromResult(identity);
    }

    public static async Task<string> ResolveEmblemAsync(string? teamId) =>
        (await ResolveAsync(teamId)).EmblemImagePath;

    public static async Task<string> ResolveTeamColorAsync(string? teamId) =>
        (await ResolveAsync(teamId)).TeamColorHex;

    public static async Task<string> ResolveEmblemBackgroundAsync(string? teamId) =>
        (await ResolveAsync(teamId)).EmblemBackgroundSource;

    private static string? ValidImagePayload(string? value)
    {
        var payload = value?.Trim();
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        if (Uri.TryCreate(payload, UriKind.Absolute, out var uri))
            return !uri.IsFile || File.Exists(uri.LocalPath) ? payload : null;

        return !Path.IsPathRooted(payload) || File.Exists(payload)
            ? payload
            : null;
    }

    private static string? ValidColorHex(string? value)
    {
        var colorHex = value?.Trim();
        if (string.IsNullOrWhiteSpace(colorHex) || colorHex[0] != '#')
            return null;

        var hexLength = colorHex.Length - 1;
        if (hexLength is not (3 or 4 or 6 or 8))
            return null;

        return colorHex.AsSpan(1).IndexOfAnyExcept(
            "0123456789abcdefABCDEF".AsSpan()) < 0
            ? colorHex
            : null;
    }

    private static string? ValidBackgroundColor(string? value)
    {
        var background = value?.Trim();
        if (string.Equals(
                background,
                "Transparent",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Transparent";
        }

        return ValidColorHex(background);
    }

    private static string NormalizeId(string? value) =>
        value?.Trim() ?? string.Empty;

    private static string? NormalizeOptionalId(string? value)
    {
        var normalized = NormalizeId(value);
        return normalized.Length == 0 ? null : normalized;
    }

    private static bool SameValue(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
