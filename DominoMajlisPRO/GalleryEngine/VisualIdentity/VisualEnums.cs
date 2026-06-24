namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Shared enums for Visual Identity systems.
    /// Part of Phase 1 Foundation implementation.
    /// No duplicate enum definitions found in project - this is the single source of truth.
    /// </summary>

    /// <summary>
    /// Visual rendering contexts for different UI locations.
    /// Used by PerformanceManager, LODManager, and other systems to adapt behavior.
    /// </summary>
    public enum VisualRenderContext
    {
        Store,
        Inventory,
        MainPage,
        PlayerProfile,
        PlayerDetails,
        PlayerProfiles,
        Team,
        CreateTeam,
        EditTeam,
        Match,
        MatchDetails,
        Victory,
        HallOfFame,
        Rankings,
        History,
        Certificate,
        DeveloperPreview,
        PhotoMode,
        StoreProductPreview,
        StoreHeader
    }

    /// <summary>
    /// Visual blend modes for particle and effect composition.
    /// </summary>
    public enum VisualBlendMode
    {
        Normal,
        Additive,
        Multiply,
        Overlay,
        Screen,
        Subtract
    }

    /// <summary>
    /// Material types for visual assets.
    /// Used by MaterialProfile to define surface properties.
    /// </summary>
    public enum MaterialType
    {
        Default,
        GoldMetal,
        Scales,
        Frost,
        Bronze,
        PolishedGold,
        BrushedMetal,
        Feather,
        Steel,
        Crystal
    }

    /// <summary>
    /// Device profile levels for adaptive performance.
    /// Used by DeviceProfiler to classify device capability.
    /// </summary>
    public enum DeviceProfile
    {
        VeryLite,  // Low-end devices like Realme C33
        Lite,      // Low-end devices
        Medium,    // Mid-range devices
        High,      // High-end devices
        Ultra      // Flagship devices
    }

    /// <summary>
    /// LOD (Level of Detail) levels for dynamic quality adjustment.
    /// Used by LODManager to adjust rendering quality based on performance.
    /// </summary>
    public enum LODLevel
    {
        VeryLow,
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Performance quality levels.
    /// Used by PerformanceManager to report current performance status.
    /// </summary>
    public enum PerformanceQuality
    {
        VeryLow,
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Glow quality levels.
    /// Used by DeviceProfiler to determine glow rendering quality.
    /// </summary>
    public enum GlowQuality
    {
        VeryLow,
        Low,
        Medium,
        High,
        Maximum
    }

    /// <summary>
    /// Particle emitter types.
    /// Used by Particle system to define particle behavior.
    /// </summary>
    public enum ParticleEmitterType
    {
        None,
        Fire,
        Smoke,
        Dust,
        Spark,
        Magic,
        Lightning,
        Light,
        Ash,
        Snow,
        Leaf
    }

    /// <summary>
    /// Effect states for state machine.
    /// Used by EffectStateMachine to manage effect lifecycle.
    /// </summary>
    public enum EffectState
    {
        Idle,
        Active,
        Special,
        Cooldown
    }

    /// <summary>
    /// Timeline event types.
    /// Used by AnimationTimeline to define event actions.
    /// </summary>
    public enum TimelineEventType
    {
        AnimationPlay,
        AnimationStop,
        AnimationPause,
        AnimationResume,
        ParameterSet,
        ParameterAnimate,
        ParameterReset,
        ParticleSpawn,
        ParticleStop,
        ParticleChange,
        StateSet,
        StateTrigger,
        AudioPlay,
        AudioStop,
        AudioFade
    }

    /// <summary>
    /// Timeline states.
    /// Used by AnimationTimeline to manage playback state.
    /// </summary>
    public enum TimelineState
    {
        Stopped,
        Playing,
        Paused,
        Completed
    }

    /// <summary>
    /// Emblem types for visual DNA.
    /// Used by VisualDNA to define emblem identity.
    /// </summary>
    public enum EmblemType
    {
        None,
        Dragon,
        Lion,
        Eagle,
        Falcon,
        Wolf,
        Bull,
        Crown,
        Shield
    }

    /// <summary>
    /// Personality types for visual DNA.
    /// Used by VisualDNA to define asset personality.
    /// </summary>
    public enum PersonalityType
    {
        Calm,
        Royal,
        Aggressive,
        Mystic,
        Ancient,
        Shadow,
        Heavenly,
        Legendary,
        HallOfFame,
        Champion,
        Predator,
        Guardian
    }

    /// <summary>
    /// Idle styles for visual DNA.
    /// Used by VisualDNA to define idle behavior.
    /// </summary>
    public enum IdleStyle
    {
        Calm,
        Alert,
        Resting
    }

    /// <summary>
    /// Particle families for visual DNA.
    /// Used by VisualDNA to define particle type.
    /// </summary>
    public enum ParticleFamily
    {
        None,
        Fire,
        Smoke,
        Dust,
        Spark,
        Magic,
        Lightning,
        Light,
        Ash,
        Snow,
        Leaf
    }

    /// <summary>
    /// Visual priority levels.
    /// Used by VisualPriorityManager to resolve effect conflicts.
    /// </summary>
    public enum VisualPriority
    {
        Ambient = 40,
        Particles = 50,
        Glow = 60,
        Aura = 70,
        DragonSpecial = 80,
        LegendFrame = 85,
        Champion = 90,
        HallOfFame = 95,
        Victory = 100
    }

    /// <summary>
    /// Event categories for VisualEventBus.
    /// Used to categorize events for filtering and diagnostics.
    /// </summary>
    public enum EventCategory
    {
        Effect,
        State,
        Performance,
        Device,
        Timeline,
        Particle,
        Material,
        Audio,
        System,
        Match,
        Player,
        Team,
        Store,
        Developer,
        Camera,
        Lighting,
        Inventory,
        Ownership,
        Context,
        Season
    }

    /// <summary>
    /// Visual event priority levels for VisualEventBus.
    /// Used to determine dispatch order and overflow behavior.
    /// </summary>
    public enum VisualEventPriority
    {
        Critical,
        High,
        Normal,
        Low,
        Background
    }

    /// <summary>
    /// Visual target types for layer and rendering organization.
    /// </summary>
    public enum VisualTarget
    {
        None,
        PlayerAvatar,
        TeamEmblem,
        RankFrame,
        Victory,
        HallOfFame
    }

    /// <summary>
    /// Visual layer types for rendering order.
    /// </summary>
    public enum VisualLayerType
    {
        Shadow,
        BackgroundAura,
        AmbientSmoke,
        BaseImage,
        HeatDistortion,
        Fire,
        Lightning,
        Particles,
        Glow,
        UIOverlay
    }

    /// <summary>
    /// Performance modes for adaptive performance management.
    /// </summary>
    public enum PerformanceMode
    {
        Ultra,
        High,
        Medium,
        Lite,
        VeryLite,
        Emergency
    }

    /// <summary>
    /// Source of the device profile assignment.
    /// Used for diagnostics to report how the profile was determined.
    /// </summary>
    public enum DeviceProfileSource
    {
        /// <summary>
        /// Profile came from KnownDeviceProfiles registry.
        /// </summary>
        KnownRegistry,

        /// <summary>
        /// Profile was forced via manual override.
        /// </summary>
        ForcedOverride,

        /// <summary>
        /// Profile came from conservative fallback for unknown devices.
        /// </summary>
        ConservativeFallback,

        /// <summary>
        /// Profile came from developer override.
        /// </summary>
        DeveloperOverride,

        /// <summary>
        /// Profile came from future benchmark results.
        /// </summary>
        FutureBenchmark
    }

    /// <summary>
    /// LOD reason for tracking why LOD level changed.
    /// </summary>
    public enum LODReason
    {
        Device,
        Performance,
        Manual,
        Developer,
        PhotoMode
    }
}
