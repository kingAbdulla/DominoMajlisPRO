namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingProceduralSkeletonController
{
    public const double DefaultMovementMultiplier = 1.0;
    public const double DebugVisibleMovementMultiplier = 2.5;
    public const bool FullBodyPoseEnabled = true;
    public const bool TManFullBodyEnabled = true;
    public const bool TManLivingCreatureAIEnabled = true;
    public const double DevMovementMultiplier = DebugVisibleMovementMultiplier;
    public const double DevVisibleMovementMultiplier = DebugVisibleMovementMultiplier;
    public const double ArmDropDegrees = 72.0;
    public const double ElbowBendDegrees = 22.0;
    public const double ShoulderRelaxDegrees = 11.0;
    public const double HandRelaxDegrees = 8.0;
    public const double SpineBreathDegrees = 4.4;
    public const double ChestBreathDegrees = 6.2;
    public const double HipWeightShiftDegrees = 6.0;
    public const double KneeSoftnessDegrees = 8.5;
    public const double WeightShiftDegrees = 6.0;
    public const bool SafeTouchReactionEnabled = true;
    public const double MaxHeadReactionDegrees = 12.0;
    public const double MaxNeckReactionDegrees = 8.0;
    public const double MaxChestReactionDegrees = 10.0;
    public const double MaxShoulderReactionDegrees = 18.0;
    public const double MaxUpperArmReactionDegrees = 96.0;
    public const double MaxForeArmReactionDegrees = 36.0;
    public const double MaxHandReactionDegrees = 12.0;
    public const double MaxHipReactionDegrees = 8.0;
    public const double MaxLegReactionDegrees = 10.0;
    private static readonly IReadOnlyDictionary<LivingSkeletonBoneRole, BoneAxisCalibration> AxisCalibration = CreateAxisCalibration();
    private readonly LivingCreatureRuntime _creatureRuntime = new();

    public string RuntimeState => "LivingCreatureRuntimeV2";

    public LivingMindState MindState => _creatureRuntime.MindState;
    public LivingEmotion Emotion => _creatureRuntime.Emotion;
    public LivingBodyIntent BodyIntent => _creatureRuntime.LastBodyIntent;
    public string CurrentDecision => _creatureRuntime.CurrentDecision;

    public LivingMindOutput LastMindOutput { get; private set; }

    public double MovementMultiplier { get; set; } = DebugVisibleMovementMultiplier;
    public double LastMaxAppliedRotation { get; private set; }
    public int LastClampedBoneCount { get; private set; }
    public int LastDisabledUnsafeBoneCount { get; private set; }

    public void Reset()
    {
        _creatureRuntime.Reset();
        LastMindOutput = default;
        LastMaxAppliedRotation = 0;
        LastClampedBoneCount = 0;
        LastDisabledUnsafeBoneCount = 0;
    }

    public void RegisterTouchStimulus(double x, double y, double intensity, double timestamp) =>
        _creatureRuntime.RegisterTouchStimulus(x, y, intensity, timestamp);

    public LivingSkeletonPose Update(double seconds, LivingSkeletonBoneMapping mapping)
    {
        var mind = _creatureRuntime.Tick(seconds);
        LastMindOutput = mind;
        LastMaxAppliedRotation = 0;
        LastClampedBoneCount = 0;
        LastDisabledUnsafeBoneCount = 0;

        var pose = new LivingSkeletonPose { StateName = RuntimeState };
        var multiplier = Math.Clamp(MovementMultiplier, 0.1, DebugVisibleMovementMultiplier);

        ApplyCreaturePose(pose, mapping, mind, multiplier);
        return pose;
    }

    private void ApplyCreaturePose(
        LivingSkeletonPose pose,
        LivingSkeletonBoneMapping mapping,
        LivingMindOutput mind,
        double multiplier)
    {
        var intent = BodyIntent;
        var breath = mind.BreathingAmount;
        var life = 0.65 + (mind.LivingIntensity * 0.35);
        var attentionX = mind.AttentionX;
        var touch = mind.TouchReactionStrength;
        var touchSide = mind.TouchActive ? (0.5 - mind.TouchX) * 2.0 : 0.0;
        var touchVertical = mind.TouchActive ? (0.5 - mind.TouchY) * 2.0 : 0.0;
        var weightShift = (intent.WeightShift * HipWeightShiftDegrees) + (touchSide * 2.8);
        var chestRecoil = touch * (mind.LastTouchZone == "Center" ? -5.0 : -1.6);
        var headTouch = touch * (mind.LastTouchZone == "Upper" ? 6.0 : 1.5);
        var armDrop = ArmDropDegrees * (0.82 + (intent.ArmRelax * 0.18));
        var elbowBend = ElbowBendDegrees * (0.68 + (intent.ElbowBend * 0.32));
        var shoulderRelax = ShoulderRelaxDegrees * (0.72 + (intent.ShoulderRelax * 0.28));
        var handMotion = (mind.HandMicroMotion * HandRelaxDegrees) + (breath * 1.2);
        var kneeSoftness = KneeSoftnessDegrees * (0.58 + (intent.KneeSoftness * 0.42));
        var posture = intent.PostureCorrection * 2.0;

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Hips, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Hips, posture + (breath * 0.8), weightShift * 0.28, weightShift * 0.48, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Spine, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Spine, mind.SpinePitch + (breath * SpineBreathDegrees * 0.55), attentionX * 1.2, mind.SpineRoll + (weightShift * 0.18), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Chest, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Chest, mind.ChestPitch + (breath * ChestBreathDegrees) + chestRecoil, attentionX * 1.6, mind.ChestRoll + (weightShift * 0.22), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Neck, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Neck, mind.NeckPitch + (headTouch * touchVertical * 0.32), mind.NeckYaw + (touchSide * headTouch * 0.35), mind.HeadRoll * 0.45, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Head, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Head, mind.HeadPitch + (headTouch * touchVertical * 0.50), mind.HeadYaw + (touchSide * headTouch * 0.65), mind.HeadRoll + (touchSide * touch * 3.0), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Eye, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Eye, mind.AttentionY * -2.0, mind.AttentionX * 3.0, 0, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftShoulder, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftShoulder, -shoulderRelax + (breath * 1.2), -2.5, -7.0 - (weightShift * 0.18), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightShoulder, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightShoulder, -shoulderRelax + (breath * 1.2), 2.5, 7.0 - (weightShift * 0.18), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftArm, -8.0 + mind.ArmMicroPitch + (breath * 0.75), -5.0 + (touchSide * touch * 2.0), -armDrop + (mind.ArmMicroRoll * 4.0) + (weightShift * 0.35), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightArm, -8.0 + mind.ArmMicroPitch + (breath * 0.75), 5.0 + (touchSide * touch * 2.0), armDrop - (mind.ArmMicroRoll * 4.0) + (weightShift * 0.35), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftForeArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftForeArm, elbowBend + (breath * 0.6), 0, -6.0 + (mind.HandMicroMotion * 2.5), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightForeArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightForeArm, elbowBend + (breath * 0.6), 0, 6.0 - (mind.HandMicroMotion * 2.5), multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftHand, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftHand, handMotion, -1.5, -4.0, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightHand, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightHand, -handMotion, 1.5, 4.0, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftFinger, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftFinger, handMotion * 0.55, 0, -1.4 * life, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightFinger, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightFinger, -handMotion * 0.55, 0, 1.4 * life, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftUpLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftUpLeg, 1.6 + (breath * 0.25), 0, -weightShift * 0.30, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightUpLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightUpLeg, 1.6 + (breath * 0.25), 0, -weightShift * 0.30, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftLeg, kneeSoftness + (Math.Abs(weightShift) * 0.18), 0, weightShift * 0.12, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightLeg, kneeSoftness + (Math.Abs(weightShift) * 0.18), 0, weightShift * 0.12, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftFoot, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftFoot, -0.8, 0, -weightShift * 0.04, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightFoot, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightFoot, -0.8, 0, -weightShift * 0.04, multiplier);
    }

    private void SetSafeRotation(
        LivingSkeletonPose pose,
        LivingSkeletonBoneRole role,
        double pitchDegrees,
        double yawDegrees,
        double rollDegrees,
        double multiplier)
    {
        if (!AxisCalibration.TryGetValue(role, out var calibration))
            calibration = BoneAxisCalibration.Default;

        if (!calibration.Enabled)
        {
            LastDisabledUnsafeBoneCount++;
            return;
        }

        var pitch = calibration.BasePitchDegrees + (pitchDegrees * calibration.PitchSign * multiplier);
        var yaw = calibration.BaseYawDegrees + (yawDegrees * calibration.YawSign * multiplier);
        var roll = calibration.BaseRollDegrees + (rollDegrees * calibration.RollSign * multiplier);
        var clampedPitch = Math.Clamp(pitch, -calibration.MaxDegrees, calibration.MaxDegrees);
        var clampedYaw = Math.Clamp(yaw, -calibration.MaxDegrees, calibration.MaxDegrees);
        var clampedRoll = Math.Clamp(roll, -calibration.MaxDegrees, calibration.MaxDegrees);

        if (!NearlyEqual(pitch, clampedPitch) ||
            !NearlyEqual(yaw, clampedYaw) ||
            !NearlyEqual(roll, clampedRoll))
        {
            LastClampedBoneCount++;
        }

        LastMaxAppliedRotation = Math.Max(
            LastMaxAppliedRotation,
            Math.Max(Math.Abs(clampedPitch), Math.Max(Math.Abs(clampedYaw), Math.Abs(clampedRoll))));

        pose.SetRotation(role, clampedPitch, clampedYaw, clampedRoll);
    }

    private static IReadOnlyDictionary<LivingSkeletonBoneRole, BoneAxisCalibration> CreateAxisCalibration() => new Dictionary<LivingSkeletonBoneRole, BoneAxisCalibration>
    {
        [LivingSkeletonBoneRole.Head] = new(true, 1, 1, 1, 0, 0, 0, MaxHeadReactionDegrees),
        [LivingSkeletonBoneRole.Neck] = new(true, 1, 1, 1, 0, 0, 0, MaxNeckReactionDegrees),
        [LivingSkeletonBoneRole.Eye] = new(true, 1, 1, 1, 0, 0, 0, 4.0),
        [LivingSkeletonBoneRole.LeftEye] = new(true, 1, 1, 1, 0, 0, 0, 4.0),
        [LivingSkeletonBoneRole.RightEye] = new(true, 1, 1, 1, 0, 0, 0, 4.0),
        [LivingSkeletonBoneRole.Chest] = new(true, 1, 1, 1, 0, 0, 0, MaxChestReactionDegrees),
        [LivingSkeletonBoneRole.Spine] = new(true, 1, 1, 1, 0, 0, 0, 8.0),
        [LivingSkeletonBoneRole.Hips] = new(true, 1, 1, 1, 0, 0, 0, MaxHipReactionDegrees),
        [LivingSkeletonBoneRole.LeftShoulder] = new(true, 1, 1, 1, 0, 0, 0, MaxShoulderReactionDegrees),
        [LivingSkeletonBoneRole.RightShoulder] = new(true, 1, 1, 1, 0, 0, 0, MaxShoulderReactionDegrees),
        [LivingSkeletonBoneRole.LeftArm] = new(true, 1, 1, 1, 0, 0, 0, MaxUpperArmReactionDegrees),
        [LivingSkeletonBoneRole.RightArm] = new(true, 1, 1, 1, 0, 0, 0, MaxUpperArmReactionDegrees),
        [LivingSkeletonBoneRole.LeftForeArm] = new(true, 1, 1, 1, 0, 0, 0, MaxForeArmReactionDegrees),
        [LivingSkeletonBoneRole.RightForeArm] = new(true, 1, 1, 1, 0, 0, 0, MaxForeArmReactionDegrees),
        [LivingSkeletonBoneRole.LeftHand] = new(true, 1, 1, 1, 0, 0, 0, MaxHandReactionDegrees),
        [LivingSkeletonBoneRole.RightHand] = new(true, 1, 1, 1, 0, 0, 0, MaxHandReactionDegrees),
        [LivingSkeletonBoneRole.LeftFinger] = new(true, 1, 1, 1, 0, 0, 0, 4.0),
        [LivingSkeletonBoneRole.RightFinger] = new(true, 1, 1, 1, 0, 0, 0, 4.0),
        [LivingSkeletonBoneRole.LeftUpLeg] = new(true, 1, 1, 1, 0, 0, 0, MaxLegReactionDegrees),
        [LivingSkeletonBoneRole.RightUpLeg] = new(true, 1, 1, 1, 0, 0, 0, MaxLegReactionDegrees),
        [LivingSkeletonBoneRole.LeftLeg] = new(true, 1, 1, 1, 0, 0, 0, 10.0),
        [LivingSkeletonBoneRole.RightLeg] = new(true, 1, 1, 1, 0, 0, 0, 10.0),
        [LivingSkeletonBoneRole.LeftFoot] = new(true, 1, 1, 1, 0, 0, 0, 2.0),
        [LivingSkeletonBoneRole.RightFoot] = new(true, 1, 1, 1, 0, 0, 0, 2.0)
    };

    private static bool NearlyEqual(double left, double right) =>
        Math.Abs(left - right) < 0.0001;

    private readonly record struct BoneAxisCalibration(
        bool Enabled,
        double PitchSign,
        double YawSign,
        double RollSign,
        double BasePitchDegrees,
        double BaseYawDegrees,
        double BaseRollDegrees,
        double MaxDegrees)
    {
        public static BoneAxisCalibration Default { get; } = new(true, 1, 1, 1, 0, 0, 0, 12.0);
    }
}
