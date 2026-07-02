namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingEmotion
{
    public double Calm { get; set; } = 0.76;
    public double Interested { get; set; } = 0.42;
    public double Alert { get; set; } = 0.18;
    public double Relaxed { get; set; } = 0.72;
    public double Curious { get; set; } = 0.48;
    public double Focused { get; set; } = 0.34;
    public double Startled { get; set; }
    public double Uncomfortable { get; set; }

    public void Reset()
    {
        Calm = 0.76;
        Interested = 0.42;
        Alert = 0.18;
        Relaxed = 0.72;
        Curious = 0.48;
        Focused = 0.34;
        Startled = 0;
        Uncomfortable = 0;
    }
}
