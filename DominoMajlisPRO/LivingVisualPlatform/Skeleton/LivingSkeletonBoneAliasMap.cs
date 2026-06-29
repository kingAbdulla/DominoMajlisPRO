namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public static class LivingSkeletonBoneAliasMap
{
    private static readonly Dictionary<LivingSkeletonBoneRole, string[]> Aliases = new()
    {
        [LivingSkeletonBoneRole.Root] = new[] { "root", "hips", "pelvis", "mixamorig:hips", "bip001 pelvis", "bip001" },
        [LivingSkeletonBoneRole.Head] = new[] { "head", "mixamorig:head", "bip001 head", "headbone", "bone" },
        [LivingSkeletonBoneRole.Neck] = new[] { "neck", "mixamorig:neck", "bip001 neck", "neckbone" },
        [LivingSkeletonBoneRole.Spine] = new[] { "spine", "mixamorig:spine", "mixamorig:spine1", "bip001 spine", "spinebone" },
        [LivingSkeletonBoneRole.Chest] = new[] { "chest", "mixamorig:spine2", "mixamorig:chest", "bip001 spine1", "bip001 chest", "upperchest" },
        [LivingSkeletonBoneRole.Jaw] = new[] { "jaw", "mixamorig:jaw", "bip001 jaw", "jawbone" },
        [LivingSkeletonBoneRole.Eye] = new[] { "eye", "eyes", "lefteye", "righteye", "mixamorig:lefteye", "mixamorig:righteye" },
        [LivingSkeletonBoneRole.Tail] = new[] { "tail", "tailbone", "mixamorig:tail" },
        [LivingSkeletonBoneRole.Wing] = new[] { "wing", "leftwing", "rightwing", "wingbone" },
        [LivingSkeletonBoneRole.Arm] = new[] { "arm", "leftarm", "rightarm", "mixamorig:leftarm", "mixamorig:rightarm", "bip001 l upperarm", "bip001 r upperarm" },
        [LivingSkeletonBoneRole.ForeArm] = new[] { "forearm", "leftforearm", "rightforearm", "mixamorig:leftforearm", "mixamorig:rightforearm", "bip001 l forearm", "bip001 r forearm" },
        [LivingSkeletonBoneRole.Hand] = new[] { "hand", "lefthand", "righthand", "mixamorig:lefthand", "mixamorig:righthand", "bip001 l hand", "bip001 r hand" }
    };

    public static LivingSkeletonBoneRole ResolveRole(string? boneName)
    {
        var normalized = Normalize(boneName);
        if (string.IsNullOrWhiteSpace(normalized))
            return LivingSkeletonBoneRole.Unknown;

        foreach (var pair in Aliases)
        {
            if (pair.Value.Any(alias => normalized.Equals(Normalize(alias), StringComparison.OrdinalIgnoreCase)))
                return pair.Key;
        }

        foreach (var pair in Aliases)
        {
            if (pair.Value.Any(alias => normalized.EndsWith(Normalize(alias), StringComparison.OrdinalIgnoreCase)))
                return pair.Key;
        }

        return LivingSkeletonBoneRole.Unknown;
    }

    public static IReadOnlyList<string> GetAliases(LivingSkeletonBoneRole role) =>
        Aliases.TryGetValue(role, out var aliases) ? aliases : Array.Empty<string>();

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = value.Trim()
            .Replace('_', ' ')
            .Replace('-', ' ')
            .ToLowerInvariant()
            .Where(ch => !char.IsWhiteSpace(ch))
            .ToArray();

        return new string(chars);
    }
}
