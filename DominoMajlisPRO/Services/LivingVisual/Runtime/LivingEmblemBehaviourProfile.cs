namespace DominoMajlisPRO.Services.LivingVisual.Runtime;

public sealed class LivingEmblemBehaviourProfile
{
    public string ProfileId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public double BreathingIntervalSeconds { get; init; } = 3.5;

    public double BlinkMinIntervalSeconds { get; init; } = 4.0;

    public double BlinkMaxIntervalSeconds { get; init; } = 9.0;

    public double LookAroundMinIntervalSeconds { get; init; } = 6.0;

    public double LookAroundMaxIntervalSeconds { get; init; } = 14.0;

    public double MouthMotionMinIntervalSeconds { get; init; } = 8.0;

    public double MouthMotionMaxIntervalSeconds { get; init; } = 18.0;

    public double DefaultIntensity { get; init; } = 1.0;

    public static LivingEmblemBehaviourProfile DragonMaster => new()
    {
        ProfileId = "dragon_master",
        DisplayName = "Dragon Master",
        BreathingIntervalSeconds = 3.8,
        BlinkMinIntervalSeconds = 5.0,
        BlinkMaxIntervalSeconds = 11.0,
        LookAroundMinIntervalSeconds = 8.0,
        LookAroundMaxIntervalSeconds = 16.0,
        MouthMotionMinIntervalSeconds = 10.0,
        MouthMotionMaxIntervalSeconds = 22.0,
        DefaultIntensity = 0.85
    };
}
