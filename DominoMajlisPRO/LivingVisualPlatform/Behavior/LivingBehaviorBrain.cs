using DominoMajlisPRO.LivingVisualPlatform.Models;
using DominoMajlisPRO.LivingVisualPlatform.Motion;

namespace DominoMajlisPRO.LivingVisualPlatform.Behavior;

public enum LivingBehaviorState
{
    Idle,
    Breathing,
    Looking,
    Special,
    Cooldown,
    Paused
}

public sealed class LivingBehaviorBrain
{
    private TimeSpan _elapsed;

    public LivingBehaviorState State { get; private set; } = LivingBehaviorState.Idle;

    public IReadOnlyList<LivingMotionCommand> Tick(
        LivingVisualAssetManifest? manifest,
        TimeSpan delta)
    {
        if (State == LivingBehaviorState.Paused)
        {
            return Array.Empty<LivingMotionCommand>();
        }

        _elapsed += delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        State = ResolveState(_elapsed);

        if (manifest == null ||
            !manifest.Capabilities.HasFlag(LivingVisualCapability.BehaviorBrain))
        {
            return Array.Empty<LivingMotionCommand>();
        }

        return State switch
        {
            LivingBehaviorState.Breathing => new[]
            {
                new LivingMotionCommand
                {
                    Type = LivingMotionCommandType.SetRootFloat,
                    Target = "root",
                    Value = 0,
                    DurationSeconds = 1
                }
            },
            LivingBehaviorState.Looking => new[]
            {
                new LivingMotionCommand
                {
                    Type = LivingMotionCommandType.SetMorphWeight,
                    Target = "look-neutral",
                    Value = 0,
                    DurationSeconds = 0.5
                }
            },
            _ => Array.Empty<LivingMotionCommand>()
        };
    }

    public void Pause() => State = LivingBehaviorState.Paused;

    public void Resume()
    {
        if (State == LivingBehaviorState.Paused)
        {
            State = LivingBehaviorState.Idle;
        }
    }

    private static LivingBehaviorState ResolveState(TimeSpan elapsed)
    {
        var phase = elapsed.TotalSeconds % 12;
        if (phase < 2) return LivingBehaviorState.Idle;
        if (phase < 7) return LivingBehaviorState.Breathing;
        if (phase < 9) return LivingBehaviorState.Looking;
        if (phase < 10) return LivingBehaviorState.Special;
        return LivingBehaviorState.Cooldown;
    }
}
