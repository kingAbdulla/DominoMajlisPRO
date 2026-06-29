namespace DominoMajlisPRO.LivingVisualPlatform.Skeleton;

public sealed class LivingProceduralSkeletonController
{
    public const double DefaultMovementMultiplier = 1.0;
    public const double DebugVisibleMovementMultiplier = 1.0;
    public const bool FullBodyPoseEnabled = true;
    public const bool TManFullBodyEnabled = true;
    public const bool TManLivingCreatureAIEnabled = true;
    public const double DevMovementMultiplier = DebugVisibleMovementMultiplier;
    public const double DevVisibleMovementMultiplier = DebugVisibleMovementMultiplier;
    public const double ArmDropDegrees = 14.0;
    public const double ElbowBendDegrees = 6.0;
    public const double ShoulderRelaxDegrees = 4.0;
    public const double HandRelaxDegrees = 2.0;
    public const double SpineBreathDegrees = 4.0;
    public const double ChestBreathDegrees = 5.2;
    public const double HipWeightShiftDegrees = 4.6;
    public const double KneeSoftnessDegrees = 4.8;
    public const double WeightShiftDegrees = 4.6;
    public const bool SafeTouchReactionEnabled = true;
    public const double MaxHeadReactionDegrees = 12.0;
    public const double MaxNeckReactionDegrees = 8.0;
    public const double MaxChestReactionDegrees = 8.0;
    public const double MaxShoulderReactionDegrees = 7.0;
    public const double MaxUpperArmReactionDegrees = 18.0;
    public const double MaxForeArmReactionDegrees = 10.0;
    public const double MaxHandReactionDegrees = 4.0;
    public const double MaxHipReactionDegrees = 6.0;
    public const double MaxLegReactionDegrees = 7.0;

    private static readonly IReadOnlyDictionary<LivingSkeletonBoneRole, BoneAxisCalibration> AxisCalibration = CreateAxisCalibration();
    private readonly LivingCreatureRuntime _creatureRuntime = new();

    public string RuntimeState => "LivingCreatureRuntimeV2";
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
        var m = Math.Clamp(MovementMultiplier, 0.1, DebugVisibleMovementMultiplier);
        ApplyBody(pose, mapping, mind, m);
        return pose;
    }

    private void ApplyBody(LivingSkeletonPose pose, LivingSkeletonBoneMapping mapping, LivingMindOutput mind, double multiplier)
    {
        var intent = BodyIntent;
        var breath = mind.BreathingAmount;
        var attentionX = mind.AttentionX;
        var attentionY = mind.AttentionY;
        var reaction = mind.TouchReactionStrength;
        var reactionX = mind.TouchActive ? (0.5 - mind.TouchX) * 2.0 * reaction : 0.0;
        var reactionY = mind.TouchActive ? (0.5 - mind.TouchY) * 2.0 * reaction : 0.0;
        var weight = (intent.WeightShift * HipWeightShiftDegrees) + (reactionX * 1.6);
        var bodyLife = 0.45 + (mind.LivingIntensity * 0.55);
        var muscle = Math.Clamp(0.25 + intent.ArmRelax * 0.35 + intent.ShoulderRelax * 0.20, 0, 1);
        var armLife = mind.ArmMicroRoll * 1.2 + breath * 0.5;
        var handLife = mind.HandMicroMotion * HandRelaxDegrees + breath * 0.45;
        var knee = KneeSoftnessDegrees * (0.32 + intent.KneeSoftness * 0.35);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.Hips, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Hips, intent.PostureCorrection + breath * 0.6, weight * 0.20, weight * 0.28, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.Spine, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Spine, mind.SpinePitch + breath * SpineBreathDegrees * 0.50, attentionX * 0.7, mind.SpineRoll + weight * 0.10, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.Chest, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Chest, mind.ChestPitch + breath * ChestBreathDegrees - reaction * 1.4, attentionX * 0.9, mind.ChestRoll + weight * 0.13, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.Neck, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Neck, mind.NeckPitch + reactionY * 1.4, mind.NeckYaw + reactionX * 2.0, mind.HeadRoll * 0.35, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.Head, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Head, mind.HeadPitch + reactionY * 2.3, mind.HeadYaw + reactionX * 3.2, mind.HeadRoll + reactionX, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.Eye, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.Eye, attentionY * -1.5, attentionX * 2.0, 0, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftShoulder, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftShoulder, -ShoulderRelaxDegrees * muscle + breath * 0.55, -0.6, -1.2 - weight * 0.06, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightShoulder, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightShoulder, -ShoulderRelaxDegrees * muscle + breath * 0.55, 0.6, 1.2 - weight * 0.06, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftArm, -1.2 + mind.ArmMicroPitch * bodyLife, -0.6 + reactionX * 0.5, ArmDropDegrees * muscle + armLife, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightArm, -1.2 + mind.ArmMicroPitch * bodyLife, 0.6 + reactionX * 0.5, -ArmDropDegrees * muscle - armLife, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftForeArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftForeArm, ElbowBendDegrees * intent.ElbowBend + breath * 0.22, 0, -0.9 + mind.HandMicroMotion * 0.45, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightForeArm, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightForeArm, ElbowBendDegrees * intent.ElbowBend + breath * 0.22, 0, 0.9 - mind.HandMicroMotion * 0.45, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftHand, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftHand, handLife * 0.35, -0.2, -0.5, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightHand, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightHand, -handLife * 0.35, 0.2, 0.5, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftFinger, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftFinger, handLife * 0.12, 0, -0.25, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightFinger, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightFinger, -handLife * 0.12, 0, 0.25, multiplier);

        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftUpLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftUpLeg, 0.7 + breath * 0.10, 0, -weight * 0.16, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightUpLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightUpLeg, 0.7 + breath * 0.10, 0, -weight * 0.16, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftLeg, knee + Math.Abs(weight) * 0.08, 0, weight * 0.05, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightLeg, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightLeg, knee + Math.Abs(weight) * 0.08, 0, weight * 0.05, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.LeftFoot, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.LeftFoot, -0.4, 0, -weight * 0.02, multiplier);
        if (mapping.TryGetBone(LivingSkeletonBoneRole.RightFoot, out _))
            SetSafeRotation(pose, LivingSkeletonBoneRole.RightFoot, -0.4, 0, -weight * 0.02, multiplier);
    }

    private void SetSafeRotation(LivingSkeletonPose pose, LivingSkeletonBoneRole role, double pitchDegrees, double yawDegrees, double rollDegrees, double multiplier)
    {
        if (!AxisCalibration.TryGetValue(role, out var c))
            c = BoneAxisCalibration.Default;
        if (!c.Enabled)
        {
            LastDisabledUnsafeBoneCount++;
            return;
        }
        var pitch = c.BasePitchDegrees + pitchDegrees * c.PitchSign * multiplier;
        var yaw = c.BaseYawDegrees + yawDegrees * c.YawSign * multiplier;
        var roll = c.BaseRollDegrees + rollDegrees * c.RollSign * multiplier;
        var cp = Math.Clamp(pitch, -c.MaxDegrees, c.MaxDegrees);
        var cy = Math.Clamp(yaw, -c.MaxDegrees, c.MaxDegrees);
        var cr = Math.Clamp(roll, -c.MaxDegrees, c.MaxDegrees);
        if (!NearlyEqual(pitch, cp) || !NearlyEqual(yaw, cy) || !NearlyEqual(roll, cr))
            LastClampedBoneCount++;
        LastMaxAppliedRotation = Math.Max(LastMaxAppliedRotation, Math.Max(Math.Abs(cp), Math.Max(Math.Abs(cy), Math.Abs(cr))));
        pose.SetRotation(role, cp, cy, cr);
    }

    private static IReadOnlyDictionary<LivingSkeletonBoneRole, BoneAxisCalibration> CreateAxisCalibration() => new Dictionary<LivingSkeletonBoneRole, BoneAxisCalibration>
    {
        [LivingSkeletonBoneRole.Head] = new(true, 1, 1, 1, 0, 0, 0, MaxHeadReactionDegrees),
        [LivingSkeletonBoneRole.Neck] = new(true, 1, 1, 1, 0, 0, 0, MaxNeckReactionDegrees),
        [LivingSkeletonBoneRole.Eye] = new(true, 1, 1, 1, 0, 0, 0, 4.0),
        [LivingSkeletonBoneRole.LeftEye] = new(true, 1, 1, 1, 0, 0, 0, 4.0),
        [LivingSkeletonBoneRole.RightEye] = new(true, 1, 1, 1, 0, 0, 0, 4.0),
        [LivingSkeletonBoneRole.Chest] = new(true, 1, 1, 1, 0, 0, 0, MaxChestReactionDegrees),
        [LivingSkeletonBoneRole.Spine] = new(true, 1, 1, 1, 0, 0, 0, 6.0),
        [LivingSkeletonBoneRole.Hips] = new(true, 1, 1, 1, 0, 0, 0, MaxHipReactionDegrees),
        [LivingSkeletonBoneRole.LeftShoulder] = new(true, 1, 1, 1, 0, 0, 0, MaxShoulderReactionDegrees),
        [LivingSkeletonBoneRole.RightShoulder] = new(true, 1, 1, 1, 0, 0, 0, MaxShoulderReactionDegrees),
        [LivingSkeletonBoneRole.LeftArm] = new(true, 1, 1, 1, 0, 0, 0, MaxUpperArmReactionDegrees),
        [LivingSkeletonBoneRole.RightArm] = new(true, 1, 1, 1, 0, 0, 0, MaxUpperArmReactionDegrees),
        [LivingSkeletonBoneRole.LeftForeArm] = new(true, 1, 1, 1, 0, 0, 0, MaxForeArmReactionDegrees),
        [LivingSkeletonBoneRole.RightForeArm] = new(true, 1, 1, 1, 0, 0, 0, MaxForeArmReactionDegrees),
        [LivingSkeletonBoneRole.LeftHand] = new(true, 1, 1, 1, 0, 0, 0, MaxHandReactionDegrees),
        [LivingSkeletonBoneRole.RightHand] = new(true, 1, 1, 1, 0, 0, 0, MaxHandReactionDegrees),
        [LivingSkeletonBoneRole.LeftFinger] = new(true, 1, 1, 1, 0, 0, 0, 2.0),
        [LivingSkeletonBoneRole.RightFinger] = new(true, 1, 1, 1, 0, 0, 0, 2.0),
        [LivingSkeletonBoneRole.LeftUpLeg] = new(true, 1, 1, 1, 0, 0, 0, MaxLegReactionDegrees),
        [LivingSkeletonBoneRole.RightUpLeg] = new(true, 1, 1, 1, 0, 0, 0, MaxLegReactionDegrees),
        [LivingSkeletonBoneRole.LeftLeg] = new(true, 1, 1, 1, 0, 0, 0, 6.0),
        [LivingSkeletonBoneRole.RightLeg] = new(true, 1, 1, 1, 0, 0, 0, 6.0),
        [LivingSkeletonBoneRole.LeftFoot] = new(true, 1, 1, 1, 0, 0, 0, 1.2),
        [LivingSkeletonBoneRole.RightFoot] = new(true, 1, 1, 1, 0, 0, 0, 1.2)
    };

    private static bool NearlyEqual(double left, double right) => Math.Abs(left - right) < 0.0001;

    private readonly record struct BoneAxisCalibration(bool Enabled, double PitchSign, double YawSign, double RollSign, double BasePitchDegrees, double BaseYawDegrees, double BaseRollDegrees, double MaxDegrees)
    {
        public static BoneAxisCalibration Default { get; } = new(true, 1, 1, 1, 0, 0, 0, 6.0);
    }
}
