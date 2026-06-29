namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingMindRuntime
{
    private readonly Random _random = new();
    private readonly double _noiseA;
    private readonly double _noiseB;
    private readonly double _noiseC;
    private LivingTouchStimulus _lastTouch;
    private double _lastSeconds;
    private bool _initialized;

    public LivingMindRuntime()
        : this(LivingMindPersonality.TManDeveloperPreview)
    {
    }

    public LivingMindRuntime(LivingMindPersonality personality)
    {
        Personality = personality;
        Sensors = new LivingMindSensors();
        State = new LivingMindState();
        _noiseA = Range(0, 200);
        _noiseB = Range(0, 200);
        _noiseC = Range(0, 200);
    }

    public LivingMindPersonality Personality { get; }
    public LivingMindSensors Sensors { get; }
    public LivingMindState State { get; }

    public void Reset()
    {
        State.Reset();
        Sensors.Reset();
        _lastSeconds = 0;
        _initialized = false;
        _lastTouch = default;
    }

    public void RegisterTouchStimulus(double x, double y, double intensity, double timestamp)
    {
        _lastTouch = LivingTouchStimulus.Create(x, y, intensity, timestamp);
        State.TouchX = _lastTouch.X;
        State.TouchY = _lastTouch.Y;
        State.TouchReactionStrength = Math.Clamp(State.TouchReactionStrength + (_lastTouch.Intensity * 0.32), 0, 0.55);
        State.LastTouchZone = _lastTouch.Zone;
        State.Decision = _lastTouch.Zone switch
        {
            "Upper" => "Observing",
            "Center" => "Settling",
            "Lower" => "Settling",
            _ => "Curious"
        };
        State.DecisionCooldown = Math.Max(State.DecisionCooldown, 0.45);
        Sensors.Notice(timestamp, (0.5 - _lastTouch.X) * 2.0, (0.5 - _lastTouch.Y) * 2.0);
    }

    public LivingMindOutput Tick(double seconds)
    {
        if (!_initialized)
        {
            _initialized = true;
            _lastSeconds = seconds;
            State.DecisionCooldown = Range(0.45, 1.25);
            State.LastStimulusTime = seconds;
            Sensors.Reset(seconds);
        }

        var delta = Math.Clamp(seconds - _lastSeconds, 0.0, 0.1);
        _lastSeconds = seconds;

        EvolveInternalState(seconds, delta);
        UpdateTouchReaction(seconds, delta);
        MaybeChooseAttention(seconds);
        UpdateFocus(delta);

        return CreateOutput(seconds);
    }

    private void EvolveInternalState(double seconds, double delta)
    {
        var slowNoise = Wave(seconds, 0.17, _noiseA) * 0.5 + 0.5;
        var thinkingNoise = Wave(seconds, 0.09, _noiseB) * 0.5 + 0.5;
        State.Awareness = Clamp01(Lerp(State.Awareness, Personality.AwarenessBaseline + (slowNoise * 0.20), delta * 0.7));
        State.Curiosity = Clamp01(State.Curiosity + (Personality.CuriosityRisePerSecond * delta * (0.72 + thinkingNoise)));
        State.Boredom = Clamp01(State.Boredom + (Personality.BoredomRisePerSecond * delta * (1.18 - State.Attention)));
        State.Calm = Clamp01(State.Calm + (Personality.CalmRecoveryPerSecond * delta) - (State.Attention * 0.004 * delta));
        State.Attention = Clamp01(Lerp(State.Attention, Math.Max(Math.Abs(State.TargetFocusX), Math.Abs(State.TargetFocusY)), delta * 0.9));
        State.BreathingEnergy = Clamp01(Lerp(State.BreathingEnergy, 0.56 + ((1.0 - State.Calm) * 0.34) + (State.Awareness * 0.14), delta * 0.8));
        State.MicroMovement = Clamp01(Lerp(State.MicroMovement, 0.26 + (State.Curiosity * 0.30) + ((1.0 - State.Calm) * 0.20), delta * 0.9));
        State.DecisionCooldown = Math.Max(0, State.DecisionCooldown - delta);
        State.LastStimulusTime = Sensors.LastStimulusTime;
    }

    private void UpdateTouchReaction(double seconds, double delta)
    {
        if (State.TouchReactionStrength <= 0.0001)
        {
            State.TouchReactionStrength = 0;
            return;
        }

        var age = Math.Max(0, seconds - _lastTouch.TimestampSeconds);
        var damping = age > 0.18 ? 3.4 : 1.35;
        State.TouchReactionStrength = Math.Max(0, State.TouchReactionStrength - (delta * damping));
    }

    private void MaybeChooseAttention(double seconds)
    {
        if (State.DecisionCooldown > 0)
            return;

        var pressure = Math.Max(
            State.Curiosity - Personality.CuriosityDecisionThreshold,
            State.Boredom - Personality.BoredomDecisionThreshold);
        var shouldDecide = pressure > 0 || _random.NextDouble() < 0.08;
        if (!shouldDecide)
        {
            State.DecisionCooldown = Range(0.35, 0.9);
            State.Decision = "Thinking";
            return;
        }

        var decision = ChooseDecision(seconds);
        State.TargetFocusX = decision.TargetFocusX;
        State.TargetFocusY = decision.TargetFocusY;
        State.DecisionCooldown = decision.CooldownSeconds;
        State.Decision = decision.Name;
        State.Curiosity = Clamp01(State.Curiosity * Range(0.42, 0.68));
        State.Boredom = Clamp01(State.Boredom * Range(0.34, 0.62));
        State.Calm = Clamp01(State.Calm - Range(0.025, 0.075));
        Sensors.Notice(seconds, decision.TargetFocusX, decision.TargetFocusY);
    }

    private LivingMindDecision ChooseDecision(double seconds)
    {
        var roll = _random.NextDouble();
        if (roll < 0.18)
            return new LivingMindDecision("Thinking", 0, Range(-0.16, 0.10), Range(1.4, 3.2));

        var focusBias = Wave(seconds, 0.11, _noiseC) * 0.35;
        var targetX = Math.Clamp(Range(-Personality.MaxFocusX, Personality.MaxFocusX) + focusBias, -Personality.MaxFocusX, Personality.MaxFocusX);
        var targetY = Range(-Personality.MaxFocusY, Personality.MaxFocusY);
        var name = State.Boredom > State.Curiosity ? "Curious" : "Observing";
        return new LivingMindDecision(name, targetX, targetY, Range(Personality.MinDecisionCooldownSeconds, Personality.MaxDecisionCooldownSeconds));
    }

    private void UpdateFocus(double delta)
    {
        var returnHome = Personality.FocusReturnPerSecond * delta * (0.55 + State.Calm);
        State.TargetFocusX = Lerp(State.TargetFocusX, 0, returnHome * 0.22);
        State.TargetFocusY = Lerp(State.TargetFocusY, 0, returnHome * 0.26);
        State.FocusX = Lerp(State.FocusX, State.TargetFocusX, Math.Clamp(delta * (1.8 - State.Calm), 0.02, 0.22));
        State.FocusY = Lerp(State.FocusY, State.TargetFocusY, Math.Clamp(delta * (1.6 - State.Calm), 0.02, 0.20));
    }

    private LivingMindOutput CreateOutput(double seconds)
    {
        var calmDamping = 0.55 + ((1.0 - State.Calm) * 0.45);
        var breath = Math.Sin(seconds * (1.05 + (State.BreathingEnergy * 0.18))) * State.BreathingEnergy;
        var microA = Wave(seconds, 0.73, _noiseA) * State.MicroMovement;
        var microB = Wave(seconds, 0.49, _noiseB) * State.MicroMovement;
        var microC = Wave(seconds, 0.37, _noiseC) * State.MicroMovement;
        var touchStrength = State.TouchReactionStrength;
        var touchAwayX = (0.5 - State.TouchX) * 2.0 * touchStrength;
        var touchAwayY = (0.5 - State.TouchY) * 2.0 * touchStrength;
        var upperTouch = State.TouchY < 0.32 ? touchStrength : 0;
        var bodyTouch = State.TouchY >= 0.32 && State.TouchY <= 0.72 ? touchStrength : 0;
        var lowerTouch = State.TouchY > 0.72 ? touchStrength : 0;
        var reaction = Math.Max(0, 1.0 - Sensors.SecondsSinceStimulus(seconds) / 0.75) * 0.18;
        var intensity = Clamp01(0.25 + (State.Awareness * 0.18) + (State.Attention * 0.24) + (State.MicroMovement * 0.22) + reaction);
        var weightShift = Clamp((microA * 0.26) + (State.FocusX * 0.15) + (touchAwayX * 0.32), -0.55, 0.55);
        var shoulderRelax = Clamp01(0.72 + (State.Calm * 0.12) - (bodyTouch * 0.26));
        var armRelax = Clamp01(0.80 + (State.Calm * 0.08) - (upperTouch * 0.18));
        var elbowBend = Clamp01(0.55 + (State.MicroMovement * 0.15) + (bodyTouch * 0.20));
        var handMicro = Clamp(microC * 0.42, -0.45, 0.45);
        var kneeSoftness = Clamp01(0.34 + (Math.Abs(weightShift) * 0.20) + (lowerTouch * 0.36));

        var headYaw = ((State.FocusX * 10.5) + (microA * 2.4) + (touchAwayX * (upperTouch > 0 ? 3.1 : 1.1))) * calmDamping;
        var headPitch = ((State.FocusY * -5.2) + (breath * 1.05) + (microB * 1.5) + (touchAwayY * (upperTouch > 0 ? 1.7 : 0.45))) * calmDamping;
        var headRoll = ((State.FocusX * -1.5) + (microC * 1.1) + (touchAwayX * 0.55)) * calmDamping;
        var chestPitch = ((breath * 3.1) + (microB * 0.72) - (bodyTouch * 0.85)) * calmDamping;
        var chestRoll = ((microA * 1.05) + (State.FocusX * 0.72) + (touchAwayX * 0.95)) * calmDamping;

        return new LivingMindOutput(
            State.FocusX,
            State.FocusY,
            breath,
            weightShift,
            shoulderRelax,
            armRelax,
            elbowBend,
            handMicro,
            kneeSoftness,
            Clamp(headYaw, -13.5, 13.5),
            Clamp(headPitch, -5.8, 6.5),
            Clamp(headRoll, -2.4, 2.4),
            Clamp(headYaw * 0.42, -5.8, 5.8),
            Clamp(headPitch * 0.46, -2.8, 3.2),
            Clamp(chestPitch, -3.2, 3.2),
            Clamp(chestRoll, -2.1, 2.1),
            Clamp(chestPitch * 0.36, -1.25, 1.25),
            Clamp(chestRoll * 0.48, -1.2, 1.2),
            Clamp(microB * 0.75, -0.55, 0.55),
            Clamp(microC * 0.65, -0.45, 0.45),
            breath,
            intensity,
            touchStrength > 0.01,
            State.TouchX,
            State.TouchY,
            touchStrength,
            State.LastTouchZone,
            State.Decision);
    }

    private double Range(double min, double max) =>
        min + (_random.NextDouble() * (max - min));

    private static double Wave(double seconds, double speed, double phase) =>
        Math.Sin((seconds + phase) * speed);

    private static double Lerp(double start, double end, double amount) =>
        start + ((end - start) * Math.Clamp(amount, 0, 1));

    private static double Clamp01(double value) =>
        Math.Clamp(value, 0, 1);

    private static double Clamp(double value, double min, double max) =>
        Math.Clamp(value, min, max);
}
