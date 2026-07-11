using DominoMajlisPRO.GalleryEngine.Models;

namespace DominoMajlisPRO.GalleryEngine.Services;

public static class TeamAssetPayloadCatalog
{
    public const string TeamColorTypeId = "TeamColor";

    private static readonly IReadOnlyDictionary<string, TeamAssetPayloadModel> Payloads =
        BuildPayloads();

    public static TeamAssetPayloadModel? Resolve(
        string? teamAssetId,
        string? expectedTypeId = null)
    {
        var normalizedId = Normalize(teamAssetId);

        if (normalizedId.Length == 0 ||
            !Payloads.TryGetValue(normalizedId, out var payload))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(expectedTypeId) ||
               SameId(payload.TeamAssetTypeId, expectedTypeId)
            ? payload
            : null;
    }

    public static bool TryResolve(
        string? teamAssetId,
        string? expectedTypeId,
        out TeamAssetPayloadModel? payload)
    {
        payload = Resolve(teamAssetId, expectedTypeId);
        return payload != null;
    }

    public static IReadOnlyList<TeamAssetPayloadModel> GetAllPayloads() =>
        Payloads.Values
            .DistinctBy(
                payload => $"{payload.TeamAssetTypeId}\u001F{payload.TeamAssetId}",
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(payload => payload.TeamAssetTypeId)
            .ThenBy(payload => payload.TeamAssetId)
            .ToList();

    public static IReadOnlyList<TeamAssetPayloadModel> GetDefaultTeamPayloads() =>
        GetAllPayloads()
            .Where(payload =>
                IsDefaultEmblem(payload.TeamAssetId) ||
                IsDefaultTeamColor(payload.TeamAssetId) ||
                IsDefaultEmblemBackground(payload.TeamAssetId))
            .ToList();

    public static bool IsDefaultTeamAsset(string? teamAssetId) =>
        IsDefaultEmblem(teamAssetId) ||
        IsDefaultTeamColor(teamAssetId) ||
        IsDefaultEmblemBackground(teamAssetId);

    public static bool IsDefaultEmblem(string? teamAssetId)
    {
        var id = Normalize(teamAssetId);

        return SameId(id, "team-emblem-shield-3d") ||
               SameId(id, "team-emblem-dragon-3d") ||
               SameId(id, "team-emblem-lion-3d") ||
               SameId(id, "team-emblem-eagle-3d") ||
               SameId(id, "team-emblem-wolf-3d") ||
               SameId(id, "team-emblem-crown-3d");
    }

    public static bool IsDefaultTeamColor(string? teamAssetId)
    {
        var id = Normalize(teamAssetId);

        return SameId(id, "team-color-gold") ||
               SameId(id, "team-color-red") ||
               SameId(id, "team-color-blue") ||
               SameId(id, "team-color-green") ||
               SameId(id, "team-color-purple") ||
               SameId(id, "team-color-black");
    }

    public static bool IsDefaultEmblemBackground(string? teamAssetId)
    {
        var id = Normalize(teamAssetId);

        return SameId(id, "emblem-background-transparent");
    }

    private static IReadOnlyDictionary<string, TeamAssetPayloadModel> BuildPayloads()
    {
        var payloads = new Dictionary<string, TeamAssetPayloadModel>(
            StringComparer.OrdinalIgnoreCase);

        AddEmblem(
            payloads,
            "team-emblem-shield-3d",
            "شعار الدرع ثلاثي الأبعاد",
            "3D Shield Emblem",
            "shield_3d.png",
            "emblem-shield-3d",
            "shield-3d",
            "shield_3d",
            "shield_3d.png");

        AddEmblem(
            payloads,
            "team-emblem-dragon-3d",
            "شعار التنين ثلاثي الأبعاد",
            "3D Dragon Emblem",
            "dragon_3d.png",
            "emblem-dragon-3d",
            "dragon-3d",
            "dragon_3d",
            "dragon_3d.png");

        AddEmblem(
            payloads,
            "team-emblem-lion-3d",
            "شعار الأسد ثلاثي الأبعاد",
            "3D Lion Emblem",
            "lion_3d.png",
            "emblem-lion-3d",
            "lion-3d",
            "lion_3d",
            "lion_3d.png");

        AddEmblem(
            payloads,
            "team-emblem-eagle-3d",
            "شعار النسر ثلاثي الأبعاد",
            "3D Eagle Emblem",
            "eagle_3d.png",
            "emblem-eagle-3d",
            "eagle-3d",
            "eagle_3d",
            "eagle_3d.png");

        AddEmblem(
            payloads,
            "team-emblem-wolf-3d",
            "شعار الذئب ثلاثي الأبعاد",
            "3D Wolf Emblem",
            "wolf_3d.png",
            "emblem-wolf-3d",
            "wolf-3d",
            "wolf_3d",
            "wolf_3d.png");

        AddEmblem(
            payloads,
            "team-emblem-crown-3d",
            "شعار التاج ثلاثي الأبعاد",
            "3D Crown Emblem",
            "crown_3d.png",
            "emblem-crown-3d",
            "crown-3d",
            "crown_3d",
            "crown_3d.png");

        AddColor(
            payloads,
            "team-color-gold",
            "#FFD700",
            "gold_color.png",
            "team-color-gold",
            "gold",
            "gold-color",
            "gold_color",
            "gold_color.png");

        AddColor(
            payloads,
            "team-color-red",
            "#E53935",
            "red_color.png",
            "team-color-red",
            "red",
            "red-color",
            "red_color",
            "red_color.png");

        AddColor(
            payloads,
            "team-color-blue",
            "#1E88E5",
            "blue_color.png",
            "team-color-blue",
            "blue",
            "blue-color",
            "blue_color",
            "blue_color.png");

        AddColor(
            payloads,
            "team-color-green",
            "#43A047",
            "green_color.png",
            "team-color-green",
            "green",
            "green-color",
            "green_color",
            "green_color.png");

        AddColor(
            payloads,
            "team-color-purple",
            "#8E24AA",
            "purple_color.png",
            "team-color-purple",
            "purple",
            "purple-color",
            "purple_color",
            "purple_color.png");

        AddColor(
            payloads,
            "team-color-black",
            "#111111",
            "black_color.png",
            "team-color-black",
            "black",
            "black-color",
            "black_color",
            "black_color.png");

        AddAliases(
            payloads,
            new TeamAssetPayloadModel
            {
                TeamAssetId = "emblem-background-transparent",
                TeamAssetTypeId = TeamAssetTypes.EmblemBackground.TeamAssetTypeId,
                ArabicDisplayName = "خلفية شعار شفافة",
                EnglishDisplayName = "Transparent Emblem Background",
                BackgroundColorHex = "Transparent"
            },
            "emblem-background-transparent",
            "team-emblem-background-transparent",
            "transparent");

        AddAliases(
            payloads,
            new TeamAssetPayloadModel
            {
                TeamAssetId = "emblem-background-gold",
                TeamAssetTypeId = TeamAssetTypes.EmblemBackground.TeamAssetTypeId,
                ArabicDisplayName = "خلفية شعار ذهبية",
                EnglishDisplayName = "Gold Emblem Background",
                BackgroundColorHex = "#33FFD700"
            },
            "emblem-background-gold",
            "team-emblem-background-gold");

        AddAliases(
            payloads,
            new TeamAssetPayloadModel
            {
                TeamAssetId = "emblem-background-dark",
                TeamAssetTypeId = TeamAssetTypes.EmblemBackground.TeamAssetTypeId,
                ArabicDisplayName = "خلفية شعار داكنة",
                EnglishDisplayName = "Dark Emblem Background",
                BackgroundColorHex = "#CC080808"
            },
            "emblem-background-dark",
            "team-emblem-background-dark");

        return payloads;
    }

    private static void AddEmblem(
        IDictionary<string, TeamAssetPayloadModel> payloads,
        string teamAssetId,
        string arabicDisplayName,
        string englishDisplayName,
        string imagePath,
        params string[] aliases)
    {
        AddAliases(
            payloads,
            new TeamAssetPayloadModel
            {
                TeamAssetId = teamAssetId,
                TeamAssetTypeId = TeamAssetTypes.Emblem.TeamAssetTypeId,
                ArabicDisplayName = arabicDisplayName,
                EnglishDisplayName = englishDisplayName,
                ImagePath = imagePath
            },
            aliases);
    }

    private static void AddColor(
        IDictionary<string, TeamAssetPayloadModel> payloads,
        string teamAssetId,
        string colorHex,
        string imagePath,
        params string[] aliases)
    {
        AddAliases(
            payloads,
            new TeamAssetPayloadModel
            {
                TeamAssetId = teamAssetId,
                TeamAssetTypeId = TeamColorTypeId,
                ArabicDisplayName = ArabicColorName(teamAssetId),
                EnglishDisplayName = EnglishColorName(teamAssetId),
                ColorHex = colorHex,
                ImagePath = imagePath
            },
            aliases);
    }

    private static void AddAliases(
        IDictionary<string, TeamAssetPayloadModel> payloads,
        TeamAssetPayloadModel payload,
        params string[] aliases)
    {
        payloads[Normalize(payload.TeamAssetId)] = payload;

        foreach (var alias in aliases)
        {
            var normalizedAlias = Normalize(alias);

            if (normalizedAlias.Length > 0)
                payloads[normalizedAlias] = payload;
        }
    }

    private static string Normalize(string? value) =>
        value?.Trim() ?? string.Empty;

    private static string ArabicColorName(string assetId) => assetId switch
    {
        "team-color-gold" => "لون الفريق الذهبي",
        "team-color-red" => "لون الفريق الأحمر",
        "team-color-blue" => "لون الفريق الأزرق",
        "team-color-green" => "لون الفريق الأخضر",
        "team-color-purple" => "لون الفريق البنفسجي",
        "team-color-black" => "لون الفريق الأسود",
        _ => "لون الفريق"
    };

    private static string EnglishColorName(string assetId) => assetId switch
    {
        "team-color-gold" => "Gold Team Color",
        "team-color-red" => "Red Team Color",
        "team-color-blue" => "Blue Team Color",
        "team-color-green" => "Green Team Color",
        "team-color-purple" => "Purple Team Color",
        "team-color-black" => "Black Team Color",
        _ => "Team Color"
    };

    private static bool SameId(string? left, string? right) =>
        string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
