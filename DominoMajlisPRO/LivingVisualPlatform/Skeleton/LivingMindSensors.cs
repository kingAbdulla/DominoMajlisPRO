namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingMindSensors
{
    public double LastStimulusTime { get; private set; }
    public double StimulusX { get; private set; }
    public double StimulusY { get; private set; }

    public double SecondsSinceStimulus(double seconds) =>
        Math.Max(0, seconds - LastStimulusTime);

    public void Reset(double seconds = 0)
    {
        LastStimulusTime = seconds;
        StimulusX = 0;
        StimulusY = 0;
    }

    public void Notice(double seconds, double x, double y)
    {
        LastStimulusTime = seconds;
        StimulusX = Math.Clamp(x, -1, 1);
        StimulusY = Math.Clamp(y, -1, 1);
    }
}
