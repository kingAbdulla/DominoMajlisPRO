namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public static class LivingSkeletonBoneAliasMap
{
    private static readonly Dictionary<LivingSkeletonBoneRole, string[]> Aliases = new()
    {
        [LivingSkeletonBoneRole.Root] = new[] { "root", "bip001" },
        [LivingSkeletonBoneRole.Hips] = new[] { "hips", "pelvis", "mixamorig:hips", "bip001 pelvis" },
        [LivingSkeletonBoneRole.Head] = new[] { "head", "mixamorig:head", "bip001 head", "headbone", "bone" },
        [LivingSkeletonBoneRole.Neck] = new[] { "neck", "mixamorig:neck", "bip001 neck", "neckbone" },
        [LivingSkeletonBoneRole.Spine] = new[] { "spine", "mixamorig:spine", "mixamorig:spine1", "bip001 spine", "spinebone" },
        [LivingSkeletonBoneRole.Chest] = new[] { "chest", "mixamorig:spine2", "mixamorig:chest", "bip001 spine1", "bip001 chest", "upperchest" },
        [LivingSkeletonBoneRole.Jaw] = new[] { "jaw", "mixamorig:jaw", "bip001 jaw", "jawbone" },
        [LivingSkeletonBoneRole.Eye] = new[] { "eye", "eyes", "lefteye", "righteye", "mixamorig:lefteye", "mixamorig:righteye" },
        [LivingSkeletonBoneRole.Tail] = new[] { "tail", "tailbone", "mixamorig:tail" },
        [LivingSkeletonBoneRole.Wing] = new[] { "wing", "leftwing", "rightwing", "wingbone" },
        [LivingSkeletonBoneRole.Arm] = new[] { "arm", "upperarm" },
        [LivingSkeletonBoneRole.ForeArm] = new[] { "forearm" },
        [LivingSkeletonBoneRole.Hand] = new[] { "hand" },
        [LivingSkeletonBoneRole.LeftShoulder] = new[] { "leftshoulder", "mixamorig:leftshoulder", "bip001 l clavicle", "l clavicle" },
        [LivingSkeletonBoneRole.RightShoulder] = new[] { "rightshoulder", "mixamorig:rightshoulder", "bip001 r clavicle", "r clavicle" },
        [LivingSkeletonBoneRole.LeftArm] = new[] { "leftarm", "mixamorig:leftarm", "bip001 l upperarm", "l upperarm" },
        [LivingSkeletonBoneRole.RightArm] = new[] { "rightarm", "mixamorig:rightarm", "bip001 r upperarm", "r upperarm" },
        [LivingSkeletonBoneRole.LeftForeArm] = new[] { "leftforearm", "mixamorig:leftforearm", "bip001 l forearm", "l forearm" },
        [LivingSkeletonBoneRole.RightForeArm] = new[] { "rightforearm", "mixamorig:rightforearm", "bip001 r forearm", "r forearm" },
        [LivingSkeletonBoneRole.LeftHand] = new[] { "lefthand", "mixamorig:lefthand", "bip001 l hand", "l hand" },
        [LivingSkeletonBoneRole.RightHand] = new[] { "righthand", "mixamorig:righthand", "bip001 r hand", "r hand" },
        [LivingSkeletonBoneRole.LeftUpLeg] = new[] { "leftupleg", "mixamorig:leftupleg", "bip001 l thigh", "l thigh" },
        [LivingSkeletonBoneRole.RightUpLeg] = new[] { "rightupleg", "mixamorig:rightupleg", "bip001 r thigh", "r thigh" },
        [LivingSkeletonBoneRole.LeftLeg] = new[] { "leftleg", "mixamorig:leftleg", "bip001 l calf", "l calf" },
        [LivingSkeletonBoneRole.RightLeg] = new[] { "rightleg", "mixamorig:rightleg", "bip001 r calf", "r calf" },
        [LivingSkeletonBoneRole.LeftFoot] = new[] { "leftfoot", "mixamorig:leftfoot", "bip001 l foot", "l foot" },
        [LivingSkeletonBoneRole.RightFoot] = new[] { "rightfoot", "mixamorig:rightfoot", "bip001 r foot", "r foot" }
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
