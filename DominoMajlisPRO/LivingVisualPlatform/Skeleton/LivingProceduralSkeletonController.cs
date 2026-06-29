namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingProceduralSkeletonController
{
    private readonly Random _random = new();
    private LivingSkeletonActionState _state = LivingSkeletonActionState.Idle;
    private double _stateStartSeconds;
    private double _stateDurationSeconds = 2.0;
    private double _nextActionSeconds;
    private double _startYaw;
    private double _startPitch;
    private double _targetYaw;
    private double _targetPitch;
    private double _currentYaw;
    private double _currentPitch;
    private bool _initialized;

    public LivingSkeletonActionState State => _state;

    public void Reset()
    {
        _state = LivingSkeletonActionState.Idle;
        _stateStartSeconds = 0;
        _stateDurationSeconds = 2.0;
        _nextActionSeconds = 0;
        _startYaw = 0;
        _startPitch = 0;
        _targetYaw = 0;
        _targetPitch = 0;
        _currentYaw = 0;
        _currentPitch = 0;
        _initialized = false;
    }

    public LivingSkeletonPose Update(double seconds, LivingSkeletonBoneMapping mapping)
    {
        if (!_initialized)
        {
            _initialized = true;
            _nextActionSeconds = seconds + Range(1.5, 4.0);
        }

        if (_state == LivingSkeletonActionState.Idle && seconds >= _nextActionSeconds)
            BeginLook(seconds);
        else if (_state != LivingSkeletonActionState.Idle && seconds >= _stateStartSeconds + _stateDurationSeconds)
            BeginReturn(seconds);

        UpdateHeadState(seconds);

        var pose = new LivingSkeletonPose { StateName = _state.ToString() };
        var breath = Math.Sin(seconds * 1.35) * 1.4;
        var breathLift = Math.Cos(seconds * 1.35) * 0.55;

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Chest, out _) ||
            mapping.TryGetBone(LivingSkeletonBoneRole.Spine, out _))
        {
            pose.SetRotation(
                mapping.TryGetBone(LivingSkeletonBoneRole.Chest, out _)
                    ? LivingSkeletonBoneRole.Chest
                    : LivingSkeletonBoneRole.Spine,
                breath,
                0,
                breathLift);
        }

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Head, out _))
            pose.SetRotation(LivingSkeletonBoneRole.Head, _currentPitch, _currentYaw, 0);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Neck, out _))
            pose.SetRotation(LivingSkeletonBoneRole.Neck, _currentPitch * 0.45, _currentYaw * 0.45, 0);

        return pose;
    }

    private void BeginLook(double seconds)
    {
        _state = _random.NextDouble() < 0.5
            ? LivingSkeletonActionState.LookLeft
            : LivingSkeletonActionState.LookRight;
        _stateStartSeconds = seconds;
        _stateDurationSeconds = Range(0.9, 1.6);
        _startYaw = _currentYaw;
        _startPitch = _currentPitch;
        _targetYaw = _state == LivingSkeletonActionState.LookLeft ? -Range(4.0, 8.0) : Range(4.0, 8.0);
        _targetPitch = Range(-2.0, 2.0);
    }

    private void BeginReturn(double seconds)
    {
        if (_state == LivingSkeletonActionState.ReturnToNeutral)
        {
            _state = LivingSkeletonActionState.Idle;
            _nextActionSeconds = seconds + Range(1.5, 4.0);
            _currentYaw = 0;
            _currentPitch = 0;
            return;
        }

        _state = LivingSkeletonActionState.ReturnToNeutral;
        _stateStartSeconds = seconds;
        _stateDurationSeconds = Range(0.8, 1.4);
        _startYaw = _currentYaw;
        _startPitch = _currentPitch;
        _targetYaw = 0;
        _targetPitch = 0;
    }

    private void UpdateHeadState(double seconds)
    {
        if (_state == LivingSkeletonActionState.Idle)
            return;

        var progress = Ease(Saturate((seconds - _stateStartSeconds) / _stateDurationSeconds));
        _currentYaw = Lerp(_startYaw, _targetYaw, progress);
        _currentPitch = Lerp(_startPitch, _targetPitch, progress);
    }

    private double Range(double min, double max) =>
        min + (_random.NextDouble() * (max - min));

    private static double Lerp(double start, double end, double amount) =>
        start + ((end - start) * amount);

    private static double Saturate(double value) =>
        Math.Clamp(value, 0, 1);

    private static double Ease(double value) =>
        value * value * (3 - (2 * value));
}

public enum LivingSkeletonActionState
{
    Idle,
    LookLeft,
    LookRight,
    ReturnToNeutral
}
