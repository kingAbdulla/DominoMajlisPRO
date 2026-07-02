using System;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Runtime Profile for Living Visual Identity Engine.
    /// Connects runtime rendering decisions to PerformanceManager and DeviceProfiler.
    /// Manages per-context performance settings.
    /// Lazy initialization - no manual Initialize() required.
    /// Event-driven updates - no polling through Update().
    /// 
    /// Part of Phase 2.2 Stage A implementation.
    /// Phase 2.2 Status: Stage A - Runtime Bridge
    /// </summary>
    public static class RuntimeProfile
    {
        private static PerformanceMode _currentMode;
        private static DeviceProfile _currentDeviceProfile;
        private static LODLevel _currentLOD;
        private static VisualRenderContext _currentContext;
        private static bool _isInitialized;
        private static IDisposable _performanceModeSubscription;
        
        static RuntimeProfile()
        {
            _currentMode = PerformanceMode.Medium;
            _currentDeviceProfile = DeviceProfile.Medium;
            _currentLOD = LODLevel.Medium;
            _currentContext = VisualRenderContext.Store;
            _isInitialized = false;
        }
        
        /// <summary>
        /// Gets the current performance mode.
        /// Lazy initialization on first access.
        /// </summary>
        public static PerformanceMode CurrentMode
        {
            get
            {
                EnsureInitialized();
                return _currentMode;
            }
        }
        
        /// <summary>
        /// Gets the current device profile.
        /// Lazy initialization on first access.
        /// </summary>
        public static DeviceProfile CurrentDeviceProfile
        {
            get
            {
                EnsureInitialized();
                return _currentDeviceProfile;
            }
        }
        
        /// <summary>
        /// Gets the current LOD level.
        /// Lazy initialization on first access.
        /// </summary>
        public static LODLevel CurrentLOD
        {
            get
            {
                EnsureInitialized();
                return _currentLOD;
            }
        }
        
        /// <summary>
        /// Gets the current render context.
        /// Lazy initialization on first access.
        /// </summary>
        public static VisualRenderContext CurrentContext
        {
            get
            {
                EnsureInitialized();
                return _currentContext;
            }
        }
        
        /// <summary>
        /// Gets whether the runtime profile is initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Ensures the runtime profile is initialized.
        /// Lazy initialization - called automatically on first property access.
        /// Can be called explicitly from VisualIdentityEngine for early initialization.
        /// Idempotent - safe to call multiple times.
        /// Subscribes to PerformanceModeChanged events for automatic updates.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_isInitialized)
                return;
            
            // Sync with Phase 1 systems
            _currentMode = PerformanceManager.CurrentMode;
            _currentDeviceProfile = DeviceProfiler.CurrentProfile;
            _currentLOD = LODManager.CurrentLOD;
            _currentContext = VisualRenderContext.Store;
            
            // Subscribe to PerformanceModeChanged events for automatic updates
            _performanceModeSubscription = VisualEventBus.Subscribe(
                EventCategory.Performance,
                OnPerformanceModeChanged);
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Handles PerformanceModeChanged events from VisualEventBus.
        /// Automatically updates runtime state without polling.
        /// </summary>
        /// <param name="eventEntry">The event entry.</param>
        private static void OnPerformanceModeChanged(EventEntry eventEntry)
        {
            if (eventEntry.EventName == "PerformanceModeChanged" && eventEntry.EventData != null)
            {
                if (eventEntry.EventData.TryGetValue("CurrentMode", out var currentModeObj) && 
                    currentModeObj is PerformanceMode currentMode)
                {
                    _currentMode = currentMode;
                }
                
                if (eventEntry.EventData.TryGetValue("CurrentDeviceProfile", out var deviceProfileObj) && 
                    deviceProfileObj is DeviceProfile deviceProfile)
                {
                    _currentDeviceProfile = deviceProfile;
                }
            }
        }
        
        /// <summary>
        /// Sets the current render context.
        /// </summary>
        /// <param name="context">The render context.</param>
        public static void SetContext(VisualRenderContext context)
        {
            EnsureInitialized();
            _currentContext = context;
        }
        
        /// <summary>
        /// Gets the performance budget for the current mode.
        /// </summary>
        /// <returns>Performance budget for current mode.</returns>
        public static PerformanceBudget GetCurrentBudget()
        {
            EnsureInitialized();
            return PerformanceBudgetCatalog.GetBudget(_currentMode);
        }
        
        /// <summary>
        /// Gets the performance budget for a specific mode.
        /// </summary>
        /// <param name="mode">The performance mode.</param>
        /// <returns>Performance budget for specified mode.</returns>
        public static PerformanceBudget GetBudget(PerformanceMode mode)
        {
            EnsureInitialized();
            return PerformanceBudgetCatalog.GetBudget(mode);
        }
        
        /// <summary>
        /// Validates whether particles can be spawned based on current budget.
        /// Note: Active effect count budget is not yet represented in PerformanceBudget.
        /// Only particle budget validation is available in Stage A.
        /// </summary>
        /// <param name="currentParticleCount">Current active particle count.</param>
        /// <returns>True if spawning is allowed, false otherwise.</returns>
        public static bool CanSpawnParticles(int currentParticleCount)
        {
            EnsureInitialized();
            var budget = GetCurrentBudget();
            return currentParticleCount < budget.ParticleBudget;
        }
        
        /// <summary>
        /// Validates whether an effect can be rendered based on current budget.
        /// Note: Active effect count budget is not yet represented in PerformanceBudget.
        /// Only particle budget validation is available in Stage A.
        /// </summary>
        /// <param name="currentEffectCount">Current active effect count (not validated in Stage A).</param>
        /// <param name="currentParticleCount">Current active particle count.</param>
        /// <returns>True if rendering is allowed based on particle budget, false otherwise.</returns>
        public static bool CanRenderEffect(int currentEffectCount, int currentParticleCount)
        {
            EnsureInitialized();
            var budget = GetCurrentBudget();
            // Note: currentEffectCount is not validated as PerformanceBudget lacks effect count property
            // Only particle budget validation is available in Stage A
            return currentParticleCount < budget.ParticleBudget;
        }
        
        /// <summary>
        /// Gets runtime profile diagnostics.
        /// </summary>
        /// <returns>Runtime profile diagnostics snapshot.</returns>
        public static RuntimeProfileDiagnostics GetDiagnostics()
        {
            EnsureInitialized();
            return new RuntimeProfileDiagnostics(
                _currentMode,
                _currentDeviceProfile,
                _currentLOD,
                _currentContext,
                GetCurrentBudget(),
                _isInitialized
            );
        }
        
        /// <summary>
        /// Cleanup method to dispose event subscriptions.
        /// Should be called during application shutdown.
        /// </summary>
        public static void Cleanup()
        {
            _performanceModeSubscription?.Dispose();
            _performanceModeSubscription = null;
            _isInitialized = false;
        }
    }
    
    /// <summary>
    /// Performance Budget Catalog - isolated configuration source for performance budgets.
    /// Can be replaced with a Performance Catalog in future phases without changing RuntimeProfile.
    /// Uses the existing PerformanceBudget class from PerformanceManager.
    /// </summary>
    internal static class PerformanceBudgetCatalog
    {
        private static readonly PerformanceBudget _ultraBudget;
        private static readonly PerformanceBudget _highBudget;
        private static readonly PerformanceBudget _mediumBudget;
        private static readonly PerformanceBudget _lowBudget;
        private static readonly PerformanceBudget _emergencyBudget;
        
        static PerformanceBudgetCatalog()
        {
            // Initialize performance budgets using existing PerformanceBudget class
            _ultraBudget = new PerformanceBudget(200, GlowQuality.Maximum, 60, 200, 20, 15, 10, 10, 10, 15, isImmutable: true);
            _highBudget = new PerformanceBudget(150, GlowQuality.High, 60, 150, 15, 12, 8, 8, 8, 12, isImmutable: true);
            _mediumBudget = new PerformanceBudget(100, GlowQuality.Medium, 30, 100, 10, 8, 5, 5, 5, 8, isImmutable: true);
            _lowBudget = new PerformanceBudget(50, GlowQuality.Low, 15, 50, 5, 4, 3, 3, 3, 4, isImmutable: true);
            _emergencyBudget = new PerformanceBudget(20, GlowQuality.VeryLow, 10, 20, 2, 2, 1, 1, 1, 2, isImmutable: true);
        }
        
        public static PerformanceBudget GetBudget(PerformanceMode mode)
        {
            return mode switch
            {
                PerformanceMode.Ultra => _ultraBudget,
                PerformanceMode.High => _highBudget,
                PerformanceMode.Medium => _mediumBudget,
                PerformanceMode.Lite => _lowBudget,
                PerformanceMode.VeryLite => _lowBudget,
                PerformanceMode.Emergency => _emergencyBudget,
                _ => _mediumBudget
            };
        }
    }
    
    /// <summary>
    /// Runtime profile diagnostics snapshot.
    /// Immutable readonly struct for allocation-safe diagnostics.
    /// </summary>
    public readonly struct RuntimeProfileDiagnostics
    {
        public readonly PerformanceMode CurrentMode;
        public readonly DeviceProfile CurrentDeviceProfile;
        public readonly LODLevel CurrentLOD;
        public readonly VisualRenderContext CurrentContext;
        public readonly PerformanceBudget CurrentBudget;
        public readonly bool IsInitialized;
        
        public RuntimeProfileDiagnostics(
            PerformanceMode currentMode,
            DeviceProfile currentDeviceProfile,
            LODLevel currentLOD,
            VisualRenderContext currentContext,
            PerformanceBudget currentBudget,
            bool isInitialized)
        {
            CurrentMode = currentMode;
            CurrentDeviceProfile = currentDeviceProfile;
            CurrentLOD = currentLOD;
            CurrentContext = currentContext;
            CurrentBudget = currentBudget;
            IsInitialized = isInitialized;
        }
    }
}
