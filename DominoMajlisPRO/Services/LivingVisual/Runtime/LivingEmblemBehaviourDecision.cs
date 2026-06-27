namespace DominoMajlisPRO.Services.LivingVisual.Runtime;

public sealed class LivingEmblemBehaviourDecision
{
    public LivingEmblemState State { get; init; }

    public string? AnimationName { get; init; }

    public string? BoneName { get; init; }

    public string? MorphName { get; init; }

    public double Intensity { get; init; }

    public double DurationSeconds { get; init; }

    public bool IsTransient { get; init; }
}
