namespace DominoMajlisPRO.Services.LivingVisual.Runtime;

public sealed class LivingEmblemBehaviorController
{
    private readonly Random random = new();

    private LivingEmblemBehaviourProfile profile;
    private double nextBreathAt;
    private double nextBlinkAt;
    private double nextLookAt;
    private double nextMouthAt;

    public LivingEmblemState CurrentState { get; private set; } = LivingEmblemState.Idle;

    public LivingEmblemBehaviorController(LivingEmblemBehaviourProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        ResetSchedule(0);
    }

    public void SetProfile(LivingEmblemBehaviourProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        ResetSchedule(0);
    }

    public LivingEmblemBehaviourDecision Tick(LivingEmblemRuntimeContext context, double deltaSeconds)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var safeDelta = Math.Max(0, deltaSeconds);

        if (!context.IsVisible)
        {
            CurrentState = LivingEmblemState.Rest;
            return CreateDecision(LivingEmblemState.Rest, null, null, null, 0, safeDelta, false);
        }

        context.ElapsedSeconds += safeDelta;
        var now = context.ElapsedSeconds;

        if (context.IsFocused)
        {
            CurrentState = LivingEmblemState.Attention;
            return CreateDecision(LivingEmblemState.Attention, "attention", "Head", null, profile.DefaultIntensity, 1.2, true);
        }

        if (now >= nextBlinkAt)
        {
            nextBlinkAt = now + Range(profile.BlinkMinIntervalSeconds, profile.BlinkMaxIntervalSeconds);
            CurrentState = LivingEmblemState.Blink;
            return CreateDecision(LivingEmblemState.Blink, null, null, "Blink", 1.0, 0.18, true);
        }

        if (now >= nextLookAt)
        {
            nextLookAt = now + Range(profile.LookAroundMinIntervalSeconds, profile.LookAroundMaxIntervalSeconds);
            CurrentState = LivingEmblemState.LookAround;
            return CreateDecision(LivingEmblemState.LookAround, null, "Head", null, profile.DefaultIntensity * 0.45, 1.6, true);
        }

        if (now >= nextMouthAt)
        {
            nextMouthAt = now + Range(profile.MouthMotionMinIntervalSeconds, profile.MouthMotionMaxIntervalSeconds);
            CurrentState = LivingEmblemState.MouthMotion;
            return CreateDecision(LivingEmblemState.MouthMotion, null, "Jaw", "MouthOpen", profile.DefaultIntensity * 0.35, 0.8, true);
        }

        if (now >= nextBreathAt)
        {
            nextBreathAt = now + profile.BreathingIntervalSeconds;
            CurrentState = LivingEmblemState.Breathing;
            return CreateDecision(LivingEmblemState.Breathing, "breathing", "Neck", null, profile.DefaultIntensity, profile.BreathingIntervalSeconds, false);
        }

        CurrentState = LivingEmblemState.Idle;
        return CreateDecision(LivingEmblemState.Idle, "idle", null, null, profile.DefaultIntensity * 0.6, safeDelta, false);
    }

    private void ResetSchedule(double now)
    {
        nextBreathAt = now + profile.BreathingIntervalSeconds;
        nextBlinkAt = now + Range(profile.BlinkMinIntervalSeconds, profile.BlinkMaxIntervalSeconds);
        nextLookAt = now + Range(profile.LookAroundMinIntervalSeconds, profile.LookAroundMaxIntervalSeconds);
        nextMouthAt = now + Range(profile.MouthMotionMinIntervalSeconds, profile.MouthMotionMaxIntervalSeconds);
        CurrentState = LivingEmblemState.Idle;
    }

    private double Range(double min, double max)
    {
        if (max <= min)
        {
            return min;
        }

        return min + (random.NextDouble() * (max - min));
    }

    private static LivingEmblemBehaviourDecision CreateDecision(
        LivingEmblemState state,
        string? animationName,
        string? boneName,
        string? morphName,
        double intensity,
        double durationSeconds,
        bool isTransient)
    {
        return new LivingEmblemBehaviourDecision
        {
            State = state,
            AnimationName = animationName,
            BoneName = boneName,
            MorphName = morphName,
            Intensity = intensity,
            DurationSeconds = durationSeconds,
            IsTransient = isTransient
        };
    }
}
