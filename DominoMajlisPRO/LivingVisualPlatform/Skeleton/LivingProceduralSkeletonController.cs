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
    public const double ArmDropDegrees = 28.0;
    public const double ElbowBendDegrees = 10.0;
    public const double ShoulderRelaxDegrees = 5.0;
    public const double HandRelaxDegrees = 2.0;
    public const double SpineBreathDegrees = 2.2;
    public const double ChestBreathDegrees = 3.2;
    public const double HipWeightShiftDegrees = 2.8;
    public const double KneeSoftnessDegrees = 5.8;
    public const double WeightShiftDegrees = 2.8;
    public const bool SafeTouchReactionEnabled = true;
    public const double MaxHeadReactionDegrees = 8.0;
    public const double MaxNeckReactionDegrees = 5.0;
    public const double MaxChestReactionDegrees = 4.0;
    public const double MaxShoulderReactionDegrees = 6.0;
    public const double MaxUpperArmReactionDegrees = 8.0;
    public const double MaxForeArmReactionDegrees = 5.0;
    public const double MaxHandReactionDegrees = 3.0;
    public const double MaxHipReactionDegrees = 3.0;
    public const double MaxLegReactionDegrees = 3.0;
    private static readonly IReadOnlyDictionary<LivingSkeletonBoneRole, BoneAxisCalibration> AxisCalibration = CreateAxisCalibration();
    private readonly LivingCreatureRuntime _creatureRuntime = new();

    public string RuntimeState => "LivingMindRuntimeV1";

    public LivingMindState MindState => _creatureRuntime.MindState;
    public LivingEmotion Emotion => _creatureRuntime.Emotion;
    public LivingBodyIntent BodyIntent => _creatureRuntime.LastBodyIntent;
    public string CurrentDecision => _creatureRuntime.CurrentDecision;

    public LivingMindOutput LastMindOutput { get; private set; }

    public double MovementMultiplier { get; set; } = DefaultMovementMultiplier;
    public double LastMaxAppliedRotation { get; private set; }
    public int LastClampedBoneCount { get; private set; }
    public int LastDisabledUnsafeBoneCount { get; private set; }

    public void Reset()
    {
        _creatureRuntime.Reset();
        LastMindOutput = default;
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

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Chest, out _) ||
            mapping.TryGetBone(LivingSkeletonBoneRole.Spine, out _))
        {
            SetSafeRotation(
                pose,
                mapping.TryGetBone(LivingSkeletonBoneRole.Chest, out _)
                    ? LivingSkeletonBoneRole.Chest
                    : LivingSkeletonBoneRole.Spine,
                mind.ChestPitch * multiplier,
                0,
                mind.ChestRoll * multiplier);
        }

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Spine, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Spine, mind.SpinePitch * multiplier, 0, mind.SpineRoll * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Head, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Head, mind.HeadPitch * multiplier, mind.HeadYaw * multiplier, mind.HeadRoll * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Neck, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Neck, mind.NeckPitch * multiplier, mind.NeckYaw * multiplier, mind.HeadRoll * 0.35 * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Arm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Arm, mind.ArmMicroPitch * multiplier, 0, mind.ArmMicroRoll * multiplier);

        if (FullBodyPoseEnabled && TManFullBodyEnabled)
            ApplyFullBodyPose(pose, mapping, mind, multiplier);

        return pose;
    }

    private void ApplyFullBodyPose(
        LivingSkeletonPose pose,
        LivingSkeletonBoneMapping mapping,
        LivingMindOutput mind,
        double multiplier)
    {
        var intent = BodyIntent;
        var armDrop = ArmDropDegrees * intent.ArmRelax;
        var elbowBend = ElbowBendDegrees * intent.ElbowBend;
        var shoulderRelax = ShoulderRelaxDegrees * intent.ShoulderRelax;
        var weightShift = HipWeightShiftDegrees * intent.WeightShift;
        var breathLift = intent.Breath * SpineBreathDegrees;
        var handMotion = mind.HandMicroMotion * HandRelaxDegrees;
        var kneeSoftness = 2.4 + (intent.KneeSoftness * KneeSoftnessDegrees * 0.55);
        var postureCorrection = intent.PostureCorrection * 1.4;

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Hips, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Hips, postureCorrection * 0.18 * multiplier, weightShift * 0.22 * multiplier, weightShift * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftShoulder, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftShoulder, (-shoulderRelax + (intent.Breath * 0.95)) * multiplier, 0, -1.4 * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightShoulder, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightShoulder, (-shoulderRelax + (intent.Breath * 0.95)) * multiplier, 0, 1.4 * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftArm, out _))
            SetSafeRotation(
                pose,
                LivingSkeletonBoneRole.LeftArm,
                (mind.ArmMicroPitch - 1.1) * multiplier,
                -1.3 * multiplier,
                (-armDrop + (mind.WeightShift * 2.2) + mind.ArmMicroRoll) * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightArm, out _))
            SetSafeRotation(
                pose,
                LivingSkeletonBoneRole.RightArm,
                (mind.ArmMicroPitch - 1.1) * multiplier,
                1.3 * multiplier,
                (armDrop + (mind.WeightShift * 2.2) - mind.ArmMicroRoll) * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftForeArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftForeArm, elbowBend * multiplier, 0, -1.6 * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightForeArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightForeArm, elbowBend * multiplier, 0, 1.6 * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftHand, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftHand, handMotion * multiplier, 0, -1.0 * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightHand, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightHand, -handMotion * multiplier, 0, 1.0 * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftUpLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftUpLeg, (1.0 + breathLift * 0.08) * multiplier, 0, (-weightShift * 0.32) * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightUpLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightUpLeg, (1.0 + breathLift * 0.08) * multiplier, 0, (-weightShift * 0.32) * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftLeg, kneeSoftness * multiplier, 0, (weightShift * 0.18) * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightLeg, kneeSoftness * multiplier, 0, (weightShift * 0.18) * multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftFoot, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftFoot, -0.4 * multiplier, 0, 0);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightFoot, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightFoot, -0.4 * multiplier, 0, 0);
    }

    private void SetSafeRotation(
        LivingSkeletonPose pose,
        LivingSkeletonBoneRole role,
        double pitchDegrees,
        double yawDegrees,
        double rollDegrees)
    {
        if (!AxisCalibration.TryGetValue(role, out var calibration))
            calibration = BoneAxisCalibration.Default;

        if (!calibration.Enabled)
        {
            LastDisabledUnsafeBoneCount++;
            return;
        }

        var pitch = calibration.BasePitchDegrees + (pitchDegrees * calibration.PitchSign);
        var yaw = calibration.BaseYawDegrees + (yawDegrees * calibration.YawSign);
        var roll = calibration.BaseRollDegrees + (rollDegrees * calibration.RollSign);
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
        [LivingSkeletonBoneRole.Chest] = new(true, 1, 1, 1, 0, 0, 0, 7.0),
        [LivingSkeletonBoneRole.Spine] = new(true, 1, 1, 1, 0, 0, 0, 5.0),
        [LivingSkeletonBoneRole.Hips] = new(true, 1, 1, 1, 0, 0, 0, MaxHipReactionDegrees),
        [LivingSkeletonBoneRole.LeftShoulder] = new(true, 1, 1, 1, 0, 0, 0, MaxShoulderReactionDegrees),
        [LivingSkeletonBoneRole.RightShoulder] = new(true, 1, 1, 1, 0, 0, 0, MaxShoulderReactionDegrees),
        [LivingSkeletonBoneRole.LeftArm] = new(true, 1, 1, 1, 0, 0, 0, 64.0),
        [LivingSkeletonBoneRole.RightArm] = new(true, 1, 1, 1, 0, 0, 0, 64.0),
        [LivingSkeletonBoneRole.LeftForeArm] = new(true, 1, 1, 1, 0, 0, 0, 18.0),
        [LivingSkeletonBoneRole.RightForeArm] = new(true, 1, 1, 1, 0, 0, 0, 18.0),
        [LivingSkeletonBoneRole.LeftHand] = new(true, 1, 1, 1, 0, 0, 0, MaxHandReactionDegrees),
        [LivingSkeletonBoneRole.RightHand] = new(true, 1, 1, 1, 0, 0, 0, MaxHandReactionDegrees),
        [LivingSkeletonBoneRole.LeftUpLeg] = new(true, 1, 1, 1, 0, 0, 0, MaxLegReactionDegrees),
        [LivingSkeletonBoneRole.RightUpLeg] = new(true, 1, 1, 1, 0, 0, 0, MaxLegReactionDegrees),
        [LivingSkeletonBoneRole.LeftLeg] = new(true, 1, 1, 1, 0, 0, 0, 7.0),
        [LivingSkeletonBoneRole.RightLeg] = new(true, 1, 1, 1, 0, 0, 0, 7.0),
        [LivingSkeletonBoneRole.LeftFoot] = new(true, 1, 1, 1, 0, 0, 0, 1.2),
        [LivingSkeletonBoneRole.RightFoot] = new(true, 1, 1, 1, 0, 0, 0, 1.2)
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
        public static BoneAxisCalibration Default { get; } = new(true, 1, 1, 1, 0, 0, 0, 6.0);
    }
}
