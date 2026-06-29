namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed record LivingSkeletonDiscoveredBone(string Name, LivingSkeletonBoneRole Role);

public sealed class LivingSkeletonBoneMapping
{
    private readonly Dictionary<LivingSkeletonBoneRole, string> _primaryBones = new();
    private readonly List<LivingSkeletonDiscoveredBone> _mappedBones = new();
    private readonly List<string> _unknownBones = new();

    public IReadOnlyDictionary<LivingSkeletonBoneRole, string> PrimaryBones => _primaryBones;

    public IReadOnlyList<LivingSkeletonDiscoveredBone> MappedBones => _mappedBones;

    public IReadOnlyList<string> UnknownBones => _unknownBones;

    public bool SkeletonDetected => _mappedBones.Count > 0;

    public int BoneCount => _mappedBones.Count + _unknownBones.Count;

    public void Clear()
    {
        _primaryBones.Clear();
        _mappedBones.Clear();
        _unknownBones.Clear();
    }

    public void AddBone(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var role = LivingSkeletonBoneAliasMap.ResolveRole(name);
        if (role == LivingSkeletonBoneRole.Unknown)
        {
            _unknownBones.Add(name);
            return;
        }

        _mappedBones.Add(new LivingSkeletonDiscoveredBone(name, role));
        _primaryBones.TryAdd(role, name);
    }

    public bool TryGetBone(LivingSkeletonBoneRole role, out string name) =>
        _primaryBones.TryGetValue(role, out name!);

    public string FormatMappedBones() =>
        _mappedBones.Count == 0
            ? "none"
            : string.Join(", ", _mappedBones.Select(item => $"{item.Role}:{item.Name}"));

    public string FormatMissingBones()
    {
        var expected = new[]
        {
            LivingSkeletonBoneRole.Head,
            LivingSkeletonBoneRole.Neck,
            LivingSkeletonBoneRole.Spine,
            LivingSkeletonBoneRole.Chest,
            LivingSkeletonBoneRole.Jaw,
            LivingSkeletonBoneRole.Eye,
            LivingSkeletonBoneRole.Tail,
            LivingSkeletonBoneRole.Wing,
            LivingSkeletonBoneRole.Arm,
            LivingSkeletonBoneRole.ForeArm,
            LivingSkeletonBoneRole.Hand
        };

        var missing = expected.Where(role => !_primaryBones.ContainsKey(role)).Select(role => role.ToString());
        return string.Join(", ", missing);
    }

    public string FormatUnknownBones(int max = 32)
    {
        if (_unknownBones.Count == 0)
            return "none";

        var shown = _unknownBones.Take(max).ToList();
        if (_unknownBones.Count > shown.Count)
            shown.Add("...");
        return string.Join(", ", shown);
    }
}
