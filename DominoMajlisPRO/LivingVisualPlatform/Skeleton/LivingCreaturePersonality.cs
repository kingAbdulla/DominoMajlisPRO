namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingCreaturePersonality
{
    public double CalmBias { get; init; } = 0.78;
    public double Caution { get; init; } = 0.62;
    public double Curiosity { get; init; } = 0.58;
    public double Aggression { get; init; } = 0.08;
    public double HumanLike { get; init; } = 0.82;
    public double MotionClarity { get; init; } = 0.72;

    public static LivingCreaturePersonality TMan { get; } = new();
}
