namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingMindState
{
    public double Awareness { get; set; } = 0.62;
    public double Curiosity { get; set; } = 0.48;
    public double Calm { get; set; } = 0.76;
    public double Boredom { get; set; } = 0.22;
    public double Attention { get; set; } = 0.45;
    public double FocusX { get; set; }
    public double FocusY { get; set; }
    public double TargetFocusX { get; set; }
    public double TargetFocusY { get; set; }
    public double BreathingEnergy { get; set; } = 0.55;
    public double MicroMovement { get; set; } = 0.28;
    public double DecisionCooldown { get; set; } = 0.8;
    public double LastStimulusTime { get; set; }
    public double TouchX { get; set; } = 0.5;
    public double TouchY { get; set; } = 0.5;
    public double TouchReactionStrength { get; set; }
    public string LastTouchZone { get; set; } = "None";
    public string Decision { get; set; } = "Settling";

    public void Reset()
    {
        Awareness = 0.62;
        Curiosity = 0.48;
        Calm = 0.76;
        Boredom = 0.22;
        Attention = 0.45;
        FocusX = 0;
        FocusY = 0;
        TargetFocusX = 0;
        TargetFocusY = 0;
        BreathingEnergy = 0.55;
        MicroMovement = 0.28;
        DecisionCooldown = 0.8;
        LastStimulusTime = 0;
        TouchX = 0.5;
        TouchY = 0.5;
        TouchReactionStrength = 0;
        LastTouchZone = "None";
        Decision = "Settling";
    }
}
