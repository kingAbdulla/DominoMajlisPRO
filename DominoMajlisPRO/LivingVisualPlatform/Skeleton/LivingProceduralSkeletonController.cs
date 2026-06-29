namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingProceduralSkeletonController
{
    private readonly Random _random = new();
    private double _nextAttentionShiftSeconds;
    private double _attentionStartSeconds;
    private double _attentionDurationSeconds = 2.2;
    private double _startYaw;
    private double _startPitch;
    private double _targetYaw;
    private double _targetPitch;
    private double _currentYaw;
    private double _currentPitch;
    private double _curiosity = 0.55;
    private double _calm = 0.72;
    private bool _initialized;

    public string RuntimeState => "LivingMind";

    public void Reset()
    {
        _nextAttentionShiftSeconds = 0;
        _attentionStartSeconds = 0;
        _attentionDurationSeconds = 2.2;
        _startYaw = 0;
        _startPitch = 0;
        _targetYaw = 0;
        _targetPitch = 0;
        _currentYaw = 0;
        _currentPitch = 0;
        _curiosity = 0.55;
        _calm = 0.72;
        _initialized = false;
    }

    public LivingSkeletonPose Update(double seconds, LivingSkeletonBoneMapping mapping)
    {
        if (!_initialized)
        {
            _initialized = true;
            _nextAttentionShiftSeconds = seconds + Range(0.65, 1.45);
            PickNewAttentionTarget(seconds);
        }

        if (seconds >= _nextAttentionShiftSeconds)
            PickNewAttentionTarget(seconds);

        UpdateAttention(seconds);

        var pose = new LivingSkeletonPose { StateName = RuntimeState };
        var breath = Math.Sin(seconds * 1.15) * 2.8;
        var breathLift = Math.Cos(seconds * 1.15) * 1.1;
        var microYaw = Math.Sin(seconds * 0.73) * 1.25;
        var microPitch = Math.Cos(seconds * 0.91) * 0.75;
        var livingYaw = _currentYaw + microYaw;
        var livingPitch = _currentPitch + microPitch;
        var livingRoll = Math.Sin(seconds * 0.62) * 1.25;
        var balanceSway = Math.Sin(seconds * 0.48) * 1.15;

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Chest, out _) ||
            mapping.TryGetBone(LivingSkeletonBoneRole.Spine, out _))
        {
            pose.SetRotation(
                mapping.TryGetBone(LivingSkeletonBoneRole.Chest, out _)
                    ? LivingSkeletonBoneRole.Chest
                    : LivingSkeletonBoneRole.Spine,
                breath,
                balanceSway * 0.45,
                breathLift);
        }

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Spine, out _))
            pose.SetRotation(LivingSkeletonBoneRole.Spine, breath * 0.38, balanceSway * 0.35, breathLift * 0.35);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Head, out _))
            pose.SetRotation(LivingSkeletonBoneRole.Head, livingPitch, livingYaw, livingRoll);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Neck, out _))
            pose.SetRotation(LivingSkeletonBoneRole.Neck, livingPitch * 0.42, livingYaw * 0.42, livingRoll * 0.35);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Arm, out _))
            pose.SetRotation(LivingSkeletonBoneRole.Arm, Math.Sin(seconds * 0.57) * 0.55, 0, Math.Cos(seconds * 0.44) * 0.45);

        return pose;
    }

    private void PickNewAttentionTarget(double seconds)
    {
        _attentionStartSeconds = seconds;
        _attentionDurationSeconds = Range(1.35, 3.25);
        _nextAttentionShiftSeconds = seconds + _attentionDurationSeconds + Range(0.35, 1.2);
        _startYaw = _currentYaw;
        _startPitch = _currentPitch;
        _curiosity = Clamp01(_curiosity + Range(-0.16, 0.18));
        _calm = Clamp01(_calm + Range(-0.10, 0.12));

        var attentionStrength = 0.45 + (_curiosity * 0.55);
        _targetYaw = Range(-15.0, 15.0) * attentionStrength;
        _targetPitch = Range(-5.0, 6.0) * attentionStrength;
    }

    private void UpdateAttention(double seconds)
    {
        var progress = Ease(Saturate((seconds - _attentionStartSeconds) / _attentionDurationSeconds));
        _currentYaw = Lerp(_startYaw, _targetYaw, progress);
        _currentPitch = Lerp(_startPitch, _targetPitch, progress);
    }

    private double Range(double min, double max) =>
        min + (_random.NextDouble() * (max - min));

    private static double Lerp(double start, double end, double amount) =>
        start + ((end - start) * amount);

    private static double Saturate(double value) =>
        Math.Clamp(value, 0, 1);

    private static double Clamp01(double value) =>
        Math.Clamp(value, 0, 1);

    private static double Ease(double value) =>
        value * value * (3 - (2 * value));
}
