namespace DominoMajlisPRO.Services.LivingVisual.Runtime;

public sealed class LivingEmblemBehaviorController
{
    private LivingEmblemBehaviourProfile profile;

    public LivingEmblemState CurrentState { get; private set; } = LivingEmblemState.Idle;

    public LivingEmblemBehaviorController(LivingEmblemBehaviourProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public void SetProfile(LivingEmblemBehaviourProfile profile)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        CurrentState = LivingEmblemState.Idle;
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
        CurrentState = LivingEmblemState.Idle;

        return CreateDecision(
            LivingEmblemState.Idle,
            "idle",
            null,
            null,
            profile.DefaultIntensity * 0.6,
            safeDelta,
            false);
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
