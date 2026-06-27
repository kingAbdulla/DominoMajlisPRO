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

    public IReadOnlyList<LivingMotionCommand> Tick(
        LivingVisualAssetManifest? manifest,
        TimeSpan delta)
    {
        if (State == LivingBehaviorState.Paused)
        {
            return Array.Empty<LivingMotionCommand>();
        }

        elapsed += delta < TimeSpan.Zero ? TimeSpan.Zero : delta;

        if (manifest == null ||
            !manifest.Capabilities.HasFlag(LivingVisualCapability.BehaviorBrain))
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
            return Single(LivingMotionCommandType.SetBoneRotation, "Head", 0.45, 1.6);
        }

        if (now >= nextMouthAt)
        {
            nextMouthAt = now + Range(10, 22);
            State = LivingBehaviorState.MouthMotion;
            return new[]
            {
                Command(LivingMotionCommandType.SetBoneRotation, "Jaw", 0.35, 0.8),
                Command(LivingMotionCommandType.SetMorphWeight, "MouthOpen", 0.35, 0.8)
            };
        }

        if (now >= nextBreathAt)
        {
            nextBreathAt = now + 3.8;
            State = LivingBehaviorState.Breathing;
            return new[]
            {
                Command(LivingMotionCommandType.SetBoneRotation, "Neck", 0.85, 3.8),
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
        {
            State = LivingBehaviorState.Idle;
        }
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

    private IReadOnlyList<LivingMotionCommand> Single(
        LivingMotionCommandType type,
        string target,
        double value,
        double durationSeconds)
    {
        return new[] { Command(type, target, value, durationSeconds) };
    }

    private static LivingMotionCommand Command(
        LivingMotionCommandType type,
        string target,
        double value,
        double durationSeconds)
    {
        return new LivingMotionCommand
        {
            Type = type,
            Target = target,
            Value = value,
            DurationSeconds = durationSeconds
        };
    }

    private double Range(double min, double max)
    {
        if (max <= min)
        {
            return min;
        }

        return min + (random.NextDouble() * (max - min));
    }
}
