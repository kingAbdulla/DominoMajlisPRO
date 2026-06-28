using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Motion;

namespace DominoMajlisPRO.LivingVisualPlatform.Behavior;

public enum LivingBehaviorState
{
    Idle,
    Breathing,
    Blinking,
    Looking,
    MouthMotion,
    Attention,
    Cooldown,
    Paused
}

public sealed class LivingBehaviorBrain
{
    private const string DragonHeadBone = "Bone";
    private const string DragonJawBone = "Jaw";
    private readonly Random random = new();
    private TimeSpan elapsed;
    private double nextBreathAt;
    private double nextBlinkAt;
    private double nextLookAt;
    private double nextMouthAt;

    public LivingBehaviorBrain()
    {
        ResetSchedule(0);
    }

    public LivingBehaviorState State { get; private set; } = LivingBehaviorState.Idle;

    public IReadOnlyList<LivingMotionCommand> Tick(LivingVisualAssetManifest? manifest, TimeSpan delta)
    {
        if (State == LivingBehaviorState.Paused)
            return Array.Empty<LivingMotionCommand>();

        elapsed += delta < TimeSpan.Zero ? TimeSpan.Zero : delta;

        if (manifest == null || !manifest.Capabilities.HasFlag(LivingVisualCapability.BehaviorBrain))
        {
            State = LivingBehaviorState.Idle;
            return Array.Empty<LivingMotionCommand>();
        }

        var now = elapsed.TotalSeconds;

        if (now >= nextBlinkAt)
        {
            nextBlinkAt = now + Range(5, 11);
            State = LivingBehaviorState.Blinking;
            return Single(LivingMotionCommandType.SetMorphWeight, "Blink", 1, 0.18);
        }

        if (now >= nextLookAt)
        {
            nextLookAt = now + Range(8, 16);
            State = LivingBehaviorState.Looking;
            return new[]
            {
                Command(LivingMotionCommandType.SetBoneRotation, DragonHeadBone, Range(-16, 16), 1.6, "Z"),
                Command(LivingMotionCommandType.SetBoneRotation, DragonHeadBone, Range(-8, 8), 1.6, "X")
            };
        }

        if (now >= nextMouthAt)
        {
            nextMouthAt = now + Range(10, 22);
            State = LivingBehaviorState.MouthMotion;
            return new[]
            {
                Command(LivingMotionCommandType.SetBoneRotation, DragonJawBone, -18, 0.8, "X"),
                Command(LivingMotionCommandType.SetMorphWeight, "MouthOpen", 0.35, 0.8)
            };
        }

        if (now >= nextBreathAt)
        {
            nextBreathAt = now + 3.8;
            State = LivingBehaviorState.Breathing;
            return new[]
            {
                Command(LivingMotionCommandType.SetBoneRotation, DragonHeadBone, Range(-3.5, 3.5), 3.8, "X"),
                Command(LivingMotionCommandType.SetRootFloat, "breathing", 0.85, 3.8)
            };
        }

        State = LivingBehaviorState.Idle;
        return Single(LivingMotionCommandType.SetRootFloat, "idle", 0.5, Math.Max(0.016, delta.TotalSeconds));
    }

    public void Pause() => State = LivingBehaviorState.Paused;

    public void Resume()
    {
        if (State == LivingBehaviorState.Paused)
            State = LivingBehaviorState.Idle;
    }

    public void Reset()
    {
        elapsed = TimeSpan.Zero;
        ResetSchedule(0);
    }

    private void ResetSchedule(double now)
    {
        nextBreathAt = now + 3.8;
        nextBlinkAt = now + Range(5, 11);
        nextLookAt = now + Range(8, 16);
        nextMouthAt = now + Range(10, 22);
        State = LivingBehaviorState.Idle;
    }

    private IReadOnlyList<LivingMotionCommand> Single(LivingMotionCommandType type, string target, double value, double durationSeconds, string? axis = null) =>
        new[] { Command(type, target, value, durationSeconds, axis) };

    private static LivingMotionCommand Command(LivingMotionCommandType type, string target, double value, double durationSeconds, string? axis = null)
    {
        var parameters = string.IsNullOrWhiteSpace(axis)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["axis"] = axis };

        return new LivingMotionCommand
        {
            Type = type,
            Target = target,
            Value = value,
            DurationSeconds = durationSeconds,
            Parameters = parameters
        };
    }

    private double Range(double min, double max) => max <= min ? min : min + (random.NextDouble() * (max - min));
}
