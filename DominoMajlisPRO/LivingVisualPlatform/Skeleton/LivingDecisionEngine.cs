namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingDecisionEngine
{
    private readonly Random _random = new();
    private double _nextDecisionSeconds;

    public string CurrentDecision { get; private set; } = "I breathe.";

    public void Reset()
    {
        _nextDecisionSeconds = 0;
        CurrentDecision = "I breathe.";
    }

    public string Decide(double seconds, LivingMindState mind, LivingEmotion emotion, LivingMemory memory)
    {
        if (mind.TouchReactionStrength > 0.02)
        {
            CurrentDecision = memory.LastTouchZone switch
            {
                "Upper" => "I look at touch source.",
                "Center" => "I recoil slightly.",
                "Lower" => "I stabilize my stance.",
                "Left" => "I shift weight right softly.",
                "Right" => "I shift weight left softly.",
                _ => "I was touched."
            };
            _nextDecisionSeconds = seconds + Range(0.55, 1.2);
            return CurrentDecision;
        }

        if (seconds < _nextDecisionSeconds)
            return CurrentDecision;

        var roll = _random.NextDouble();
        if (emotion.Startled > 0.12)
            CurrentDecision = "I return carefully.";
        else if (mind.Boredom > 0.62 && roll < 0.45)
            CurrentDecision = "I look down.";
        else if (mind.Curiosity > 0.58 && roll < 0.62)
            CurrentDecision = "I noticed something.";
        else if (emotion.Focused > 0.48 && roll < 0.58)
            CurrentDecision = "I adjust posture.";
        else if (emotion.Relaxed > 0.66 && roll < 0.50)
            CurrentDecision = "I relax shoulders.";
        else
            CurrentDecision = roll < 0.5 ? "I breathe." : "I settle.";

        _nextDecisionSeconds = seconds + Range(0.75, 2.4);
        return CurrentDecision;
    }

    private double Range(double min, double max) =>
        min + (_random.NextDouble() * (max - min));
}
