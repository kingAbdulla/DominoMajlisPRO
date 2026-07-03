namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public readonly record struct LivingSkeletonBoneRotation(
    LivingSkeletonBoneRole Role,
    double PitchDegrees,
    double YawDegrees,
    double RollDegrees);

public sealed class LivingSkeletonPose
{
    private readonly Dictionary<LivingSkeletonBoneRole, LivingSkeletonBoneRotation> _rotations = new();

    public string StateName { get; set; } = "Idle";

    public IReadOnlyCollection<LivingSkeletonBoneRotation> Rotations => _rotations.Values;

    public void SetRotation(LivingSkeletonBoneRole role, double pitchDegrees, double yawDegrees, double rollDegrees = 0)
    {
        _rotations[role] = new LivingSkeletonBoneRotation(role, pitchDegrees, yawDegrees, rollDegrees);
    }
}
