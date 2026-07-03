namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingMindRuntime
{
    private readonly Random _random = new();
    private readonly double _noiseA;
    private readonly double _noiseB;
    private readonly double _noiseC;
    private readonly double _noiseD;
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
        _noiseD = Range(0, 200);
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
        State.TouchReactionStrength = Math.Clamp(State.TouchReactionStrength + (_lastTouch.Intensity * 0.38), 0, 0.62);
        State.LastTouchZone = _lastTouch.Zone;
        State.Decision = _lastTouch.Zone switch
        {
            "Upper" => "StartledAttention",
            "Center" => "BodyRecoil",
            "Lower" => "BalanceRecover",
            _ => "TouchInvestigate"
        };
        State.DecisionCooldown = Math.Max(State.DecisionCooldown, 0.55);
        Sensors.Notice(timestamp, (0.5 - _lastTouch.X) * 2.0, (0.5 - _lastTouch.Y) * 2.0);
    }

    public LivingMindOutput Tick(double seconds)
    {
        if (!_initialized)
        {
            _initialized = true;
            _lastSeconds = seconds;
            State.DecisionCooldown = Range(0.18, 0.55);
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
        var thinkingNoise = Wave(seconds, 0.11, _noiseB) * 0.5 + 0.5;
        var bodyNoise = Wave(seconds, 0.29, _noiseD) * 0.5 + 0.5;
        State.Awareness = Clamp01(Lerp(State.Awareness, Personality.AwarenessBaseline + (slowNoise * 0.26), delta * 1.05));
        State.Curiosity = Clamp01(State.Curiosity + (Personality.CuriosityRisePerSecond * delta * (1.15 + thinkingNoise)));
        State.Boredom = Clamp01(State.Boredom + (Personality.BoredomRisePerSecond * delta * (1.38 - State.Attention)));
        State.Calm = Clamp01(State.Calm + (Personality.CalmRecoveryPerSecond * delta) - (State.Attention * 0.010 * delta) - (State.TouchReactionStrength * 0.04 * delta));
        State.Attention = Clamp01(Lerp(State.Attention, Math.Max(Math.Abs(State.TargetFocusX), Math.Abs(State.TargetFocusY)), delta * 1.25));
        State.BreathingEnergy = Clamp01(Lerp(State.BreathingEnergy, 0.66 + ((1.0 - State.Calm) * 0.28) + (State.Awareness * 0.16), delta * 1.10));
        State.MicroMovement = Clamp01(Lerp(State.MicroMovement, 0.42 + (State.Curiosity * 0.34) + ((1.0 - State.Calm) * 0.24) + (bodyNoise * 0.12), delta * 1.15));
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
        var damping = age > 0.20 ? 2.65 : 1.05;
        State.TouchReactionStrength = Math.Max(0, State.TouchReactionStrength - (delta * damping));
    }

    private void MaybeChooseAttention(double seconds)
    {
        if (State.DecisionCooldown > 0)
            return;

        var pressure = Math.Max(
            State.Curiosity - (Personality.CuriosityDecisionThreshold * 0.72),
            State.Boredom - (Personality.BoredomDecisionThreshold * 0.72));
        var shouldDecide = pressure > 0 || _random.NextDouble() < 0.22;
        if (!shouldDecide)
        {
            State.DecisionCooldown = Range(0.25, 0.65);
            State.Decision = "ThinkingPause";
            return;
        }

        var decision = ChooseDecision(seconds);
        State.TargetFocusX = decision.TargetFocusX;
        State.TargetFocusY = decision.TargetFocusY;
        State.DecisionCooldown = decision.CooldownSeconds;
        State.Decision = decision.Name;
        State.Curiosity = Clamp01(State.Curiosity * Range(0.38, 0.62));
        State.Boredom = Clamp01(State.Boredom * Range(0.30, 0.56));
        State.Calm = Clamp01(State.Calm - Range(0.035, 0.095));
        Sensors.Notice(seconds, decision.TargetFocusX, decision.TargetFocusY);
    }

    private LivingMindDecision ChooseDecision(double seconds)
    {
        var roll = _random.NextDouble();
        if (roll < 0.14)
            return new LivingMindDecision("ThinkingPause", Range(-0.10, 0.10), Range(-0.22, 0.16), Range(0.75, 1.55));
        if (roll < 0.34)
            return new LivingMindDecision("PostureAdjust", Range(-0.36, 0.36), Range(-0.10, 0.18), Range(0.65, 1.40));
        if (roll < 0.56)
            return new LivingMindDecision("CuriousScan", Range(-Personality.MaxFocusX, Personality.MaxFocusX), Range(-Personality.MaxFocusY, Personality.MaxFocusY), Range(0.70, 1.60));
        if (roll < 0.76)
            return new LivingMindDecision("SettleBalance", Range(-0.22, 0.22), Range(-0.08, 0.12), Range(0.55, 1.25));

        var focusBias = Wave(seconds, 0.13, _noiseC) * 0.42;
        var targetX = Math.Clamp(Range(-Personality.MaxFocusX, Personality.MaxFocusX) + focusBias, -Personality.MaxFocusX, Personality.MaxFocusX);
        var targetY = Range(-Personality.MaxFocusY, Personality.MaxFocusY);
        return new LivingMindDecision("ObserveWorld", targetX, targetY, Range(0.65, 1.55));
    }

    private void UpdateFocus(double delta)
    {
        var returnHome = Personality.FocusReturnPerSecond * delta * (0.38 + State.Calm * 0.55);
        State.TargetFocusX = Lerp(State.TargetFocusX, 0, returnHome * 0.12);
        State.TargetFocusY = Lerp(State.TargetFocusY, 0, returnHome * 0.16);
        State.FocusX = Lerp(State.FocusX, State.TargetFocusX, Math.Clamp(delta * (2.15 - State.Calm), 0.04, 0.28));
        State.FocusY = Lerp(State.FocusY, State.TargetFocusY, Math.Clamp(delta * (1.95 - State.Calm), 0.04, 0.24));
    }

    private LivingMindOutput CreateOutput(double seconds)
    {
        var calmDamping = 0.68 + ((1.0 - State.Calm) * 0.44);
        var breath = Math.Sin(seconds * (1.22 + (State.BreathingEnergy * 0.24))) * State.BreathingEnergy;
        var microA = Wave(seconds, 0.78, _noiseA) * State.MicroMovement;
        var microB = Wave(seconds, 0.53, _noiseB) * State.MicroMovement;
        var microC = Wave(seconds, 0.41, _noiseC) * State.MicroMovement;
        var microD = Wave(seconds, 0.31, _noiseD) * State.MicroMovement;
        var touchStrength = State.TouchReactionStrength;
        var touchAwayX = (0.5 - State.TouchX) * 2.0 * touchStrength;
        var touchAwayY = (0.5 - State.TouchY) * 2.0 * touchStrength;
        var upperTouch = State.TouchY < 0.32 ? touchStrength : 0;
        var bodyTouch = State.TouchY >= 0.32 && State.TouchY <= 0.72 ? touchStrength : 0;
        var lowerTouch = State.TouchY > 0.72 ? touchStrength : 0;
        var reaction = Math.Max(0, 1.0 - Sensors.SecondsSinceStimulus(seconds) / 1.05) * 0.30;
        var decisionEnergy = State.Decision switch
        {
            "PostureAdjust" => 0.24,
            "CuriousScan" => 0.22,
            "SettleBalance" => 0.20,
            "TouchInvestigate" => 0.28,
            "BodyRecoil" => 0.26,
            "BalanceRecover" => 0.24,
            _ => 0.12
        };
        var intensity = Clamp01(0.42 + (State.Awareness * 0.18) + (State.Attention * 0.26) + (State.MicroMovement * 0.26) + reaction + decisionEnergy);
        var weightShift = Clamp((microA * 0.46) + (State.FocusX * 0.22) + (touchAwayX * 0.40) + (microD * 0.18), -0.78, 0.78);
        var shoulderRelax = Clamp01(0.66 + (State.Calm * 0.10) - (bodyTouch * 0.20) + (microD * 0.06));
        var armRelax = Clamp01(0.70 + (State.Calm * 0.08) - (upperTouch * 0.12) + (microA * 0.04));
        var elbowBend = Clamp01(0.48 + (State.MicroMovement * 0.24) + (bodyTouch * 0.18) + (decisionEnergy * 0.32));
        var handMicro = Clamp((microC * 0.78) + (microB * 0.24), -0.88, 0.88);
        var kneeSoftness = Clamp01(0.42 + (Math.Abs(weightShift) * 0.28) + (lowerTouch * 0.30) + (decisionEnergy * 0.22));

        var headYaw = ((State.FocusX * 13.0) + (microA * 3.0) + (touchAwayX * (upperTouch > 0 ? 4.0 : 1.5))) * calmDamping;
        var headPitch = ((State.FocusY * -6.4) + (breath * 1.35) + (microB * 1.9) + (touchAwayY * (upperTouch > 0 ? 2.2 : 0.6))) * calmDamping;
        var headRoll = ((State.FocusX * -2.0) + (microC * 1.45) + (touchAwayX * 0.75)) * calmDamping;
        var chestPitch = ((breath * 4.4) + (microB * 1.15) - (bodyTouch * 1.0) + (decisionEnergy * 1.4)) * calmDamping;
        var chestRoll = ((microA * 1.35) + (State.FocusX * 0.92) + (touchAwayX * 1.05)) * calmDamping;

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
            Clamp(headYaw, -16.0, 16.0),
            Clamp(headPitch, -7.0, 7.5),
            Clamp(headRoll, -3.2, 3.2),
            Clamp(headYaw * 0.46, -7.0, 7.0),
            Clamp(headPitch * 0.50, -3.6, 3.8),
            Clamp(chestPitch, -5.2, 5.2),
            Clamp(chestRoll, -3.0, 3.0),
            Clamp(chestPitch * 0.42, -2.2, 2.2),
            Clamp(chestRoll * 0.55, -2.0, 2.0),
            Clamp(microB * 1.45 + decisionEnergy * 0.7, -1.4, 1.4),
            Clamp(microC * 1.35 + microD * 0.4, -1.2, 1.2),
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
