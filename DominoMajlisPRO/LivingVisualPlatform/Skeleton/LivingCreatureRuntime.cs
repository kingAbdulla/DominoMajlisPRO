namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingCreatureRuntime
{
    private readonly LivingMindRuntime _mind = new();
    private readonly LivingDecisionEngine _decisionEngine = new();
    private double _previousCuriosity;
    private double _previousBoredom;

    public LivingCreatureRuntime()
        : this(LivingCreaturePersonality.TMan)
    {
    }

    public LivingCreatureRuntime(LivingCreaturePersonality personality)
    {
        Personality = personality;
        Emotion = new LivingEmotion();
        Memory = new LivingMemory();
    }

    public LivingCreaturePersonality Personality { get; }
    public LivingEmotion Emotion { get; }
    public LivingMemory Memory { get; }
    public LivingMindState MindState => _mind.State;
    public LivingBodyIntent LastBodyIntent { get; private set; }
    public string CurrentDecision => _decisionEngine.CurrentDecision;

    public void Reset()
    {
        _mind.Reset();
        Emotion.Reset();
        Memory.Reset();
        _decisionEngine.Reset();
        _previousCuriosity = 0;
        _previousBoredom = 0;
        LastBodyIntent = default;
    }

    public void RegisterTouchStimulus(double x, double y, double intensity, double timestamp)
    {
        _mind.RegisterTouchStimulus(x, y, intensity, timestamp);
        Memory.LastTouchZone = LivingTouchStimulus.Create(x, y, intensity, timestamp).Zone;
        Memory.LastStimulusSeconds = timestamp;
        Memory.UserInteractedRecently = true;
        Memory.LastReaction = "I was touched.";
    }

    public LivingMindOutput Tick(double seconds)
    {
        var output = _mind.Tick(seconds);
        UpdateEmotion(seconds);
        var decision = _decisionEngine.Decide(seconds, _mind.State, Emotion, Memory);
        Memory.LastReaction = decision;
        Memory.LastAttentionTarget = Math.Abs(_mind.State.FocusX) > 0.08 || Math.Abs(_mind.State.FocusY) > 0.08 ? "World" : "Neutral";
        Memory.UserInteractedRecently = Memory.TimeSinceLastStimulus(seconds) < 4.0;

        LastBodyIntent = new LivingBodyIntent(
            output.Breath,
            output.WeightShift,
            output.ShoulderRelax,
            output.ArmRelax,
            output.ElbowBend,
            Math.Clamp(0.60 + (Emotion.Relaxed * 0.22), 0, 1),
            output.KneeSoftness,
            Math.Clamp((Emotion.Focused * 0.22) + (_mind.State.MicroMovement * 0.18), 0, 1),
            Math.Clamp((Emotion.Interested + Emotion.Curious + Emotion.Alert) / 3.0, 0, 1),
            output.TouchReactionStrength);

        return output with
        {
            Decision = decision,
            LivingIntensity = Math.Clamp(output.LivingIntensity + (LastBodyIntent.EmotionIntensity * 0.10), 0, 1)
        };
    }

    private void UpdateEmotion(double seconds)
    {
        var mind = _mind.State;
        var touch = mind.TouchReactionStrength;
        Memory.CuriosityTrend = mind.Curiosity - _previousCuriosity;
        Memory.BoredomTrend = mind.Boredom - _previousBoredom;
        _previousCuriosity = mind.Curiosity;
        _previousBoredom = mind.Boredom;

        Emotion.Calm = Smooth(Emotion.Calm, Math.Clamp(Personality.CalmBias - (touch * 0.30) - (mind.Boredom * 0.08), 0, 1));
        Emotion.Interested = Smooth(Emotion.Interested, Math.Clamp(mind.Curiosity * 0.82 + mind.Attention * 0.20, 0, 1));
        Emotion.Alert = Smooth(Emotion.Alert, Math.Clamp((touch * 0.62) + ((1.0 - mind.Calm) * 0.22), 0, 1));
        Emotion.Relaxed = Smooth(Emotion.Relaxed, Math.Clamp(Emotion.Calm * 0.78 + (1.0 - touch) * 0.16, 0, 1));
        Emotion.Curious = Smooth(Emotion.Curious, Math.Clamp(mind.Curiosity * Personality.Curiosity, 0, 1));
        Emotion.Focused = Smooth(Emotion.Focused, Math.Clamp(mind.Attention * 0.74 + Math.Abs(mind.FocusX) * 0.18, 0, 1));
        Emotion.Startled = Smooth(Emotion.Startled, Math.Clamp(touch * Personality.Caution, 0, 1), 0.22);
        Emotion.Uncomfortable = Smooth(Emotion.Uncomfortable, Math.Clamp(touch * 0.22 + mind.Boredom * 0.08, 0, 1), 0.12);
        Memory.LastStimulusSeconds = Math.Max(Memory.LastStimulusSeconds, mind.LastStimulusTime);
    }

    private static double Smooth(double current, double target, double speed = 0.08) =>
        current + ((target - current) * Math.Clamp(speed, 0, 1));
}
