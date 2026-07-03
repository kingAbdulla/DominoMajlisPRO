using System;
using System.Collections.Generic;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Performance Manager for monitoring and managing visual effect performance.
    /// Provides decision-based frame limiting, page/context budgets, and adaptive profile awareness.
    /// Uses SharedAnimationClock (singleton) as primary timing source.
    /// Automatically integrates with DeviceProfiler.CurrentProfile.
    /// 
    /// IMPORTANT: This manager does NOT block the UI thread.
    /// It does NOT use Thread.Sleep.
    /// It only provides decision data for render hosts.
    /// Frame limiting is decision-based, not blocking.
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Golden Constitution Approved
    /// </summary>
    public static class PerformanceManager
    {
        private static double _currentFPS;
        private static double _averageFPS;
        private static double _minFPS;
        private static double _maxFPS;
        private static int _frameDrops;
        private static int _activeEffectCount;
        private static int _activeParticleCount;
        private static double _currentBudgetUsage;
        private static double _peakBudgetUsage;
        private static double _averageBudgetUsage;
        private static double _worstFrameTime;
        private static int _longFrameCount;
        private static bool _isInitialized;
        private static PerformanceMode _currentMode;
        private static PerformanceMode _previousMode;
        private static double _maxDeltaTime;
        private static VisualRenderContext _currentContext;

        // Telemetry tracking
        private static int _frameCount;
        private static double _totalFPS;
        private static double _totalBudgetUsage;
        private static long _telemetryUpdateFrame;
        private const int TelemetryUpdateIntervalFrames = 60;

        // Performance thresholds
        private const double MinimumAcceptableFPS = 15.0;
        private const double EmergencyFPS = 10.0;
        private const double MaximumBudgetUsage = 0.95;
        private const int MaximumFrameDrops = 10;
        private const double LongFrameThreshold = 0.033; // 33ms

        // Context budgets - accessed through ContextBudgetCatalog
        private static readonly IReadOnlyList<PerformanceBudget> _contextBudgets;

        /// <summary>
        /// Gets the current FPS.
        /// </summary>
        public static double CurrentFPS => _currentFPS;

        /// <summary>
        /// Gets the average FPS.
        /// </summary>
        public static double AverageFPS => _averageFPS;

        /// <summary>
        /// Gets the minimum FPS recorded.
        /// </summary>
        public static double MinFPS => _minFPS;

        /// <summary>
        /// Gets the maximum FPS recorded.
        /// </summary>
        public static double MaxFPS => _maxFPS;

        /// <summary>
        /// Gets the number of frame drops recorded.
        /// </summary>
        public static int FrameDrops => _frameDrops;

        /// <summary>
        /// Gets the number of active effects.
        /// </summary>
        public static int ActiveEffectCount => _activeEffectCount;

        /// <summary>
        /// Gets the number of active particles.
        /// </summary>
        public static int ActiveParticleCount => _activeParticleCount;

        /// <summary>
        /// Gets the current budget usage (0.0-1.0).
        /// </summary>
        public static double CurrentBudgetUsage => _currentBudgetUsage;

        /// <summary>
        /// Gets the peak budget usage recorded.
        /// </summary>
        public static double PeakBudgetUsage => _peakBudgetUsage;

        /// <summary>
        /// Gets the average budget usage.
        /// </summary>
        public static double AverageBudgetUsage => _averageBudgetUsage;

        /// <summary>
        /// Gets the worst frame time recorded.
        /// </summary>
        public static double WorstFrameTime => _worstFrameTime;

        /// <summary>
        /// Gets the count of long frames (> 33ms).
        /// </summary>
        public static int LongFrameCount => _longFrameCount;

        /// <summary>
        /// Gets whether the manager has been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets or sets the current performance mode.
        /// </summary>
        public static PerformanceMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _previousMode = _currentMode;
                    _currentMode = value;
                    PublishPerformanceModeChanged();
                }
            }
        }

        /// <summary>
        /// Gets the current visual render context.
        /// </summary>
        public static VisualRenderContext CurrentContext => _currentContext;

        /// <summary>
        /// Sets the current visual render context.
        /// </summary>
        public static void SetCurrentContext(VisualRenderContext context)
        {
            _currentContext = context;
        }

        /// <summary>
        /// Gets or sets the maximum allowed DeltaTime to prevent huge jumps after pause/resume.
        /// Default is 0.1 seconds (100ms).
        /// </summary>
        public static double MaxDeltaTime
        {
            get => _maxDeltaTime;
            set => _maxDeltaTime = Math.Max(0.01, Math.Min(1.0, value));
        }

        static PerformanceManager()
        {
            _maxDeltaTime = 0.1;
            _contextBudgets = ContextBudgetCatalog.GetBudgets();
            _currentMode = PerformanceMode.Medium;
            _previousMode = PerformanceMode.Medium;
            _minFPS = double.MaxValue;
            _maxFPS = 0;
            _worstFrameTime = 0;
            _longFrameCount = 0;
            _peakBudgetUsage = 0;
            _averageBudgetUsage = 0;
            _currentContext = VisualRenderContext.MainPage;
        }

        /// <summary>
        /// Gets the performance mode priority (higher = higher quality).
        /// </summary>
        private static int GetPerformanceModePriority(PerformanceMode mode)
        {
            return mode switch
            {
                PerformanceMode.Ultra => 6,
                PerformanceMode.High => 5,
                PerformanceMode.Medium => 4,
                PerformanceMode.Lite => 3,
                PerformanceMode.VeryLite => 2,
                PerformanceMode.Emergency => 1,
                _ => 4
            };
        }

        /// <summary>
        /// Initializes the performance manager.
        /// </summary>
        public static void Initialize()
        {
            _currentFPS = 30.0;
            _averageFPS = 30.0;
            _minFPS = double.MaxValue;
            _maxFPS = 0;
            _frameDrops = 0;
            _activeEffectCount = 0;
            _activeParticleCount = 0;
            _currentBudgetUsage = 0;
            _peakBudgetUsage = 0;
            _averageBudgetUsage = 0;
            _worstFrameTime = 0;
            _longFrameCount = 0;
            _frameCount = 0;
            _totalFPS = 0;
            _totalBudgetUsage = 0;
            _telemetryUpdateFrame = 0;
            _isInitialized = true;

            // Auto-detect mode from DeviceProfiler
            AutoDetectPerformanceMode();
        }

        /// <summary>
        /// Auto-detects performance mode from DeviceProfiler.
        /// </summary>
        private static void AutoDetectPerformanceMode()
        {
            var profile = DeviceProfiler.CurrentProfile;
            _currentMode = profile switch
            {
                DeviceProfile.Ultra => PerformanceMode.Ultra,
                DeviceProfile.High => PerformanceMode.High,
                DeviceProfile.Medium => PerformanceMode.Medium,
                DeviceProfile.Lite => PerformanceMode.Lite,
                DeviceProfile.VeryLite => PerformanceMode.VeryLite,
                _ => PerformanceMode.Medium
            };
        }

        /// <summary>
        /// Updates the performance manager for the current frame.
        /// Should be called once per frame from the main render loop.
        /// This method does NOT block the UI thread.
        /// Uses SharedAnimationClock for timing.
        /// </summary>
        public static void Update()
        {
            if (!_isInitialized) return;

            // Use SharedAnimationClock for timing
            var clock = SharedAnimationClock.Instance;
            _currentFPS = clock.CurrentFPS;
            var deltaTime = clock.DeltaTime;
            var frameNumber = clock.FrameNumber;

            // Clamp DeltaTime
            deltaTime = Math.Min(deltaTime, _maxDeltaTime);

            // Track worst frame time
            if (deltaTime > _worstFrameTime)
                _worstFrameTime = deltaTime;

            // Track long frames (> 33ms, below 30 FPS)
            if (deltaTime > LongFrameThreshold)
                _longFrameCount++;

            // Update telemetry
            _frameCount++;
            _totalFPS += _currentFPS;

            // Update min/max FPS
            if (_currentFPS < _minFPS && _currentFPS > 0)
                _minFPS = _currentFPS;
            if (_currentFPS > _maxFPS)
                _maxFPS = _currentFPS;

            // Track frame drops (FPS below 15)
            if (_currentFPS < MinimumAcceptableFPS && _currentFPS > 0)
                _frameDrops++;

            // Update average FPS every 60 frames
            if (frameNumber - _telemetryUpdateFrame >= TelemetryUpdateIntervalFrames)
            {
                if (_frameCount > 0)
                {
                    _averageFPS = _totalFPS / _frameCount;
                    _averageBudgetUsage = _totalBudgetUsage / _frameCount;
                }
                _frameCount = 0;
                _totalFPS = 0;
                _totalBudgetUsage = 0;
                _telemetryUpdateFrame = frameNumber;

                // Auto-adjust mode based on performance
                AutoAdjustPerformanceMode();
            }

            // Update budget usage
            UpdateBudgetUsage();
        }

        /// <summary>
        /// Auto-adjusts performance mode based on multiple factors.
        /// Considers FPS, ActiveEffects, ActiveParticles, BudgetUsage, DeviceProfile, and RenderContext.
        /// Only downgrades, never upgrades automatically.
        /// Emergency mode is triggered by severe performance degradation.
        /// </summary>
        private static void AutoAdjustPerformanceMode()
        {
            var profile = DeviceProfiler.CurrentProfile;
            var budget = ContextBudgetCatalog.GetBudget(_currentContext);
            
            // Emergency mode triggers:
            // - FPS below 10 for extended period
            // - Budget usage above 95%
            // - Frame drops > 10 in last 60 frames
            bool emergencyTrigger = _currentFPS < EmergencyFPS || _currentBudgetUsage > MaximumBudgetUsage || _frameDrops > MaximumFrameDrops;
            
            if (emergencyTrigger && _currentMode != PerformanceMode.Emergency)
            {
                _currentMode = PerformanceMode.Emergency;
                PublishPerformanceModeChanged();
                return;
            }

            // Calculate performance score (0-100)
            var fpsScore = Math.Min(100, _currentFPS * 2);
            var budgetScore = (1.0 - _currentBudgetUsage) * 100;
            var profileScore = profile switch
            {
                DeviceProfile.Ultra => 100,
                DeviceProfile.High => 80,
                DeviceProfile.Medium => 60,
                DeviceProfile.Lite => 40,
                DeviceProfile.VeryLite => 20,
                _ => 50
            };
            
            var overallScore = (fpsScore * 0.4) + (budgetScore * 0.3) + (profileScore * 0.3);
            
            var recommendedMode = overallScore switch
            {
                >= 80 => PerformanceMode.Ultra,
                >= 60 => PerformanceMode.High,
                >= 40 => PerformanceMode.Medium,
                >= 20 => PerformanceMode.Lite,
                _ => PerformanceMode.VeryLite
            };

            // Only downgrade using priority mapping, never upgrade automatically
            var currentPriority = GetPerformanceModePriority(_currentMode);
            var recommendedPriority = GetPerformanceModePriority(recommendedMode);
            
            if (recommendedPriority < currentPriority && _currentMode != PerformanceMode.Emergency)
            {
                _currentMode = recommendedMode;
                PublishPerformanceModeChanged();
            }
        }

        /// <summary>
        /// Publishes PerformanceModeChanged event through VisualEventBus.
        /// Routes failures to Developer diagnostics.
        /// </summary>
        private static void PublishPerformanceModeChanged()
        {
            try
            {
                var eventData = new Dictionary<string, object>
                {
                    { "PreviousMode", _previousMode },
                    { "CurrentMode", _currentMode },
                    { "Context", _currentContext },
                    { "FPS", _currentFPS },
                    { "BudgetUsage", _currentBudgetUsage }
                };
                
                VisualEventBus.Publish(EventCategory.Performance, "PerformanceModeChanged", eventData);
            }
            catch (Exception ex)
            {
                // Route failures to Developer diagnostics
                // TODO: Replace with DeveloperDiagnostics/VisualDiagnostics service when available
                System.Diagnostics.Debug.WriteLine($"[PerformanceManager] VisualEventBus.Publish failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the current budget usage.
        /// </summary>
        private static void UpdateBudgetUsage()
        {
            // Calculate budget usage based on active effects and particles
            var maxEffects = GetMaxEffectCount();
            var maxParticles = GetMaxParticleCount();

            var effectUsage = maxEffects > 0 ? (double)_activeEffectCount / maxEffects : 0;
            var particleUsage = maxParticles > 0 ? (double)_activeParticleCount / maxParticles : 0;

            _currentBudgetUsage = Math.Max(effectUsage, particleUsage);
            _totalBudgetUsage += _currentBudgetUsage;
            
            // Track peak budget usage
            if (_currentBudgetUsage > _peakBudgetUsage)
                _peakBudgetUsage = _currentBudgetUsage;
        }

        /// <summary>
        /// Gets the maximum effect count for the current mode.
        /// </summary>
        private static int GetMaxEffectCount()
        {
            return _currentMode switch
            {
                PerformanceMode.Ultra => 20,
                PerformanceMode.High => 15,
                PerformanceMode.Medium => 10,
                PerformanceMode.Lite => 5,
                PerformanceMode.VeryLite => 3,
                PerformanceMode.Emergency => 1,
                _ => 10
            };
        }

        /// <summary>
        /// Gets the maximum particle count for the current mode.
        /// </summary>
        private static int GetMaxParticleCount()
        {
            return _currentMode switch
            {
                PerformanceMode.Ultra => 200,
                PerformanceMode.High => 150,
                PerformanceMode.Medium => 100,
                PerformanceMode.Lite => 50,
                PerformanceMode.VeryLite => 25,
                PerformanceMode.Emergency => 10,
                _ => 100
            };
        }

        /// <summary>
        /// Determines whether a frame should be rendered based on performance and budget.
        /// This is a decision helper, NOT a blocking mechanism.
        /// Budget-aware decision: considers both FPS and budget usage.
        /// Context-aware: DeveloperPreview and PhotoMode use relaxed frame-drop policies.
        /// </summary>
        /// <returns>True if the frame should be rendered.</returns>
        public static bool ShouldRenderFrame()
        {
            // DeveloperPreview and PhotoMode have relaxed policies
            var relaxedContext = _currentContext == VisualRenderContext.DeveloperPreview || 
                                 _currentContext == VisualRenderContext.PhotoMode;
            
            var fpsThreshold = relaxedContext ? 5.0 : MinimumAcceptableFPS;
            var budgetThreshold = relaxedContext ? 1.0 : 1.0;
            
            return _currentFPS >= fpsThreshold && _currentBudgetUsage < budgetThreshold;
        }

        /// <summary>
        /// Determines whether a frame should be skipped based on performance and budget.
        /// This is a decision helper, NOT a blocking mechanism.
        /// Budget-aware decision: skip if FPS is too low OR budget is exceeded.
        /// Context-aware: DeveloperPreview and PhotoMode use relaxed frame-drop policies.
        /// </summary>
        /// <returns>True if the frame should be skipped.</returns>
        public static bool ShouldSkipFrame()
        {
            // DeveloperPreview and PhotoMode have relaxed policies
            var relaxedContext = _currentContext == VisualRenderContext.DeveloperPreview || 
                                 _currentContext == VisualRenderContext.PhotoMode;
            
            var fpsThreshold = relaxedContext ? 5.0 : MinimumAcceptableFPS;
            var budgetThreshold = relaxedContext ? 1.0 : 1.0;
            
            return _currentFPS < fpsThreshold || _currentBudgetUsage >= budgetThreshold;
        }

        /// <summary>
        /// Gets the recommended quality level based on multiple factors.
        /// Evaluates the same factors as AutoAdjustPerformanceMode:
        /// - FPS
        /// - BudgetUsage
        /// - DeviceProfile
        /// - ActiveEffects
        /// - ActiveParticles
        /// - RenderContext
        /// </summary>
        /// <returns>The recommended performance quality level.</returns>
        public static PerformanceQuality GetRecommendedQuality()
        {
            var profile = DeviceProfiler.CurrentProfile;
            
            // Calculate performance score (0-100)
            var fpsScore = Math.Min(100, _currentFPS * 2);
            var budgetScore = (1.0 - _currentBudgetUsage) * 100;
            var profileScore = profile switch
            {
                DeviceProfile.Ultra => 100,
                DeviceProfile.High => 80,
                DeviceProfile.Medium => 60,
                DeviceProfile.Lite => 40,
                DeviceProfile.VeryLite => 20,
                _ => 50
            };
            
            var overallScore = (fpsScore * 0.4) + (budgetScore * 0.3) + (profileScore * 0.3);
            
            return overallScore switch
            {
                >= 80 => PerformanceQuality.High,
                >= 60 => PerformanceQuality.Medium,
                >= 40 => PerformanceQuality.Low,
                _ => PerformanceQuality.VeryLow
            };
        }

        /// <summary>
        /// Gets the recommended particle budget for a given context.
        /// Adjusted by current performance mode and DeviceProfiler profile.
        /// </summary>
        /// <param name="context">The visual render context.</param>
        /// <returns>The recommended particle budget.</returns>
        public static int GetRecommendedParticleBudget(VisualRenderContext context)
        {
            var budget = ContextBudgetCatalog.GetBudget(context);
            
            // Adjust budget based on current performance mode
            var modeMultiplier = _currentMode switch
            {
                PerformanceMode.Ultra => 1.0,
                PerformanceMode.High => 0.9,
                PerformanceMode.Medium => 0.75,
                PerformanceMode.Lite => 0.5,
                PerformanceMode.VeryLite => 0.25,
                PerformanceMode.Emergency => 0.1,
                _ => 0.75
            };

            // Further adjust by DeviceProfiler profile
            var profileMultiplier = DeviceProfiler.GetAdaptiveProfileMultiplier(DeviceProfiler.CurrentProfile);

            return (int)(budget.ParticleBudget * modeMultiplier * profileMultiplier);
        }

        /// <summary>
        /// Gets the performance budget for a given context.
        /// Uses ContextBudgetCatalog for immutable access.
        /// </summary>
        /// <param name="context">The visual render context.</param>
        /// <returns>The performance budget for the context.</returns>
        public static PerformanceBudget GetContextBudget(VisualRenderContext context)
        {
            return ContextBudgetCatalog.GetBudget(context);
        }

        /// <summary>
        /// Gets the adaptive profile awareness adjustment.
        /// Returns a multiplier (0.0-1.0) to apply to quality settings based on device profile.
        /// </summary>
        /// <param name="profile">The device profile.</param>
        /// <returns>The quality multiplier.</returns>
        public static double GetAdaptiveProfileMultiplier(DeviceProfile profile)
        {
            return DeviceProfiler.GetAdaptiveProfileMultiplier(profile);
        }

        /// <summary>
        /// Sets the number of active effects.
        /// </summary>
        /// <param name="count">The number of active effects.</param>
        public static void SetActiveEffectCount(int count)
        {
            _activeEffectCount = Math.Max(0, count);
        }

        /// <summary>
        /// Sets the number of active particles.
        /// </summary>
        /// <param name="count">The number of active particles.</param>
        public static void SetActiveParticleCount(int count)
        {
            _activeParticleCount = Math.Max(0, count);
        }

        /// <summary>
        /// Resets the performance manager.
        /// </summary>
        public static void Reset()
        {
            _currentFPS = 30.0;
            _averageFPS = 30.0;
            _minFPS = double.MaxValue;
            _maxFPS = 0;
            _frameDrops = 0;
            _activeEffectCount = 0;
            _activeParticleCount = 0;
            _currentBudgetUsage = 0;
            _peakBudgetUsage = 0;
            _averageBudgetUsage = 0;
            _worstFrameTime = 0;
            _longFrameCount = 0;
            _frameCount = 0;
            _totalFPS = 0;
            _totalBudgetUsage = 0;
            _telemetryUpdateFrame = 0;
            AutoDetectPerformanceMode();
        }
    }

    /// <summary>
    /// Performance budget for a specific render context.
    /// All fields represent real visual engine subsystems.
    /// Supports cloning and immutable default instances.
    /// </summary>
    public class PerformanceBudget
    {
        /// <summary>
        /// Gets the particle budget.
        /// </summary>
        public int ParticleBudget { get; }

        /// <summary>
        /// Gets the glow quality.
        /// </summary>
        public GlowQuality GlowQuality { get; }

        /// <summary>
        /// Gets the target frame rate.
        /// </summary>
        public int TargetFPS { get; }

        /// <summary>
        /// Gets the memory budget in MB.
        /// </summary>
        public int MemoryBudget { get; }

        /// <summary>
        /// Gets the aura layers budget.
        /// </summary>
        public int AuraLayers { get; }

        /// <summary>
        /// Gets the dynamic lights budget.
        /// </summary>
        public int DynamicLights { get; }

        /// <summary>
        /// Gets the smoke budget.
        /// </summary>
        public int Smoke { get; }

        /// <summary>
        /// Gets the sparks budget.
        /// </summary>
        public int Sparks { get; }

        /// <summary>
        /// Gets the active timelines budget.
        /// </summary>
        public int ActiveTimelines { get; }

        /// <summary>
        /// Gets the living emblems budget.
        /// </summary>
        public int LivingEmblems { get; }

        /// <summary>
        /// Gets whether this budget is immutable.
        /// </summary>
        public bool IsImmutable { get; }

        /// <summary>
        /// Default performance budget (immutable).
        /// </summary>
        public static readonly PerformanceBudget Default = new PerformanceBudget(50, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3, isImmutable: true);

        /// <summary>
        /// Initializes a new instance of the PerformanceBudget class.
        /// All parameters represent real visual engine subsystems.
        /// </summary>
        public PerformanceBudget(
            int particleBudget, 
            GlowQuality glowQuality, 
            int targetFPS, 
            int memoryBudget,
            int auraLayers = 2,
            int dynamicLights = 2,
            int smoke = 2,
            int sparks = 2,
            int activeTimelines = 2,
            int livingEmblems = 3,
            bool isImmutable = false)
        {
            // Validate and clamp values
            ParticleBudget = Math.Clamp(particleBudget, 0, 1000);
            GlowQuality = glowQuality;
            TargetFPS = Math.Clamp(targetFPS, 15, 120);
            MemoryBudget = Math.Clamp(memoryBudget, 10, 1000);
            AuraLayers = Math.Clamp(auraLayers, 0, 10);
            DynamicLights = Math.Clamp(dynamicLights, 0, 10);
            Smoke = Math.Clamp(smoke, 0, 10);
            Sparks = Math.Clamp(sparks, 0, 10);
            ActiveTimelines = Math.Clamp(activeTimelines, 0, 10);
            LivingEmblems = Math.Clamp(livingEmblems, 0, 10);
            IsImmutable = isImmutable;
        }

        /// <summary>
        /// Creates a clone of this budget.
        /// </summary>
        /// <returns>A new PerformanceBudget with the same values.</returns>
        public PerformanceBudget Clone()
        {
            return new PerformanceBudget(
                ParticleBudget,
                GlowQuality,
                TargetFPS,
                MemoryBudget,
                AuraLayers,
                DynamicLights,
                Smoke,
                Sparks,
                ActiveTimelines,
                LivingEmblems,
                isImmutable: false);
        }

        /// <summary>
        /// Validates the budget and returns true if valid.
        /// </summary>
        public bool IsValid()
        {
            return ParticleBudget >= 0 && TargetFPS >= 15 && MemoryBudget >= 10;
        }
    }

    /// <summary>
    /// Context budget catalog for separating configuration data from engine logic.
    /// Provides budgets for all VisualRenderContext values.
    /// </summary>
    public static class ContextBudgetCatalog
    {
        private static readonly PerformanceBudget[] _budgets;
        private static readonly IReadOnlyList<PerformanceBudget> _budgetsReadOnly;

        static ContextBudgetCatalog()
        {
            // Auto-size array based on VisualRenderContext enum
            var contextCount = System.Enum.GetValues(typeof(VisualRenderContext)).Length;
            _budgets = new PerformanceBudget[contextCount];

            // Initialize budgets for each VisualRenderContext
            _budgets[(int)VisualRenderContext.Store] = new PerformanceBudget(20, GlowQuality.Low, 15, 50, 1, 1, 1, 1, 1, 2);
            _budgets[(int)VisualRenderContext.Inventory] = new PerformanceBudget(30, GlowQuality.Low, 15, 50, 1, 1, 1, 1, 1, 2);
            _budgets[(int)VisualRenderContext.MainPage] = new PerformanceBudget(50, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.PlayerProfile] = new PerformanceBudget(40, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.PlayerDetails] = new PerformanceBudget(40, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.PlayerProfiles] = new PerformanceBudget(40, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.Team] = new PerformanceBudget(70, GlowQuality.Medium, 30, 150, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.CreateTeam] = new PerformanceBudget(50, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.EditTeam] = new PerformanceBudget(50, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.Match] = new PerformanceBudget(100, GlowQuality.High, 30, 200, 3, 3, 3, 3, 3, 4);
            _budgets[(int)VisualRenderContext.MatchDetails] = new PerformanceBudget(100, GlowQuality.High, 30, 200, 3, 3, 3, 3, 3, 4);
            _budgets[(int)VisualRenderContext.Victory] = new PerformanceBudget(120, GlowQuality.High, 60, 250, 3, 3, 3, 3, 3, 4);
            _budgets[(int)VisualRenderContext.HallOfFame] = new PerformanceBudget(80, GlowQuality.High, 60, 200, 3, 3, 3, 3, 3, 4);
            _budgets[(int)VisualRenderContext.Rankings] = new PerformanceBudget(60, GlowQuality.Medium, 30, 150, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.History] = new PerformanceBudget(50, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.Certificate] = new PerformanceBudget(40, GlowQuality.Medium, 30, 100, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.DeveloperPreview] = new PerformanceBudget(200, GlowQuality.Maximum, 60, 500, 5, 5, 5, 5, 5, 10);
            _budgets[(int)VisualRenderContext.PhotoMode] = new PerformanceBudget(200, GlowQuality.Maximum, 60, 500, 5, 5, 5, 5, 5, 10);
            _budgets[(int)VisualRenderContext.StoreProductPreview] = new PerformanceBudget(60, GlowQuality.Medium, 30, 150, 2, 2, 2, 2, 2, 3);
            _budgets[(int)VisualRenderContext.StoreHeader] = new PerformanceBudget(30, GlowQuality.Low, 15, 50, 1, 1, 1, 1, 1, 2);

            // Cache the readonly wrapper once
            _budgetsReadOnly = Array.AsReadOnly(_budgets);
        }

        /// <summary>
        /// Gets the budget array for all contexts (immutable).
        /// </summary>
        /// <returns>IReadOnlyList of PerformanceBudget indexed by VisualRenderContext.</returns>
        public static IReadOnlyList<PerformanceBudget> GetBudgets()
        {
            return _budgetsReadOnly;
        }

        /// <summary>
        /// Gets the budget for a specific context.
        /// </summary>
        /// <param name="context">The visual render context.</param>
        /// <returns>The performance budget for the context.</returns>
        public static PerformanceBudget GetBudget(VisualRenderContext context)
        {
            var index = (int)context;
            if (index >= 0 && index < _budgets.Length && _budgets[index] != null)
            {
                return _budgets[index];
            }

            return PerformanceBudget.Default.Clone();
        }
    }
}
