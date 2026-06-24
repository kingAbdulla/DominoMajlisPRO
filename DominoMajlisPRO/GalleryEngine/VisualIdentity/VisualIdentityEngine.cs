using System;
using DominoMajlisPRO.GalleryEngine.VisualIdentity.Particles;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Visual identity engine for Living Visual Identity Engine.
    /// Central runtime engine that integrates Phase 1 foundation services.
    /// Render-agnostic architecture.
    /// External application/game loop owns timing via Update(deltaTime).
    /// 
    /// Responsibilities:
    /// - Initialize Phase 1 systems
    /// - Start/stop runtime safely
    /// - Update with external deltaTime
    /// - Call PerformanceManager
    /// - Call LODManager
    /// - Update active timelines/state machines/particles
    /// - Provide diagnostics
    /// 
    /// Part of Phase 2.1 Runtime Integration implementation.
    /// Phase 2.1 Status: Foundation Lock Approved
    /// Do not modify unless a compiler error or real runtime bug is discovered.
    /// </summary>
    public static class VisualIdentityEngine
    {
        private static VisualIdentityEngineState _engineState;
        private static bool _isInitialized;
        private static double _engineUptime;
        private static long _droppedEvents;
        private static double _averageDispatchTime;
        
        // Reserved telemetry fields - populated from VisualEventBus diagnostics in future Phase
        // Currently unused and reserved for runtime wiring
        private static VisualRenderContext _currentContext;
        
        // Cleanup timer accumulator
        private static double _cleanupAccumulator;
        
        /// <summary>
        /// Gets the engine state.
        /// </summary>
        public static VisualIdentityEngineState EngineState => _engineState;
        
        /// <summary>
        /// Gets whether the engine is initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Gets the engine uptime in seconds.
        /// </summary>
        public static double EngineUptime => _engineUptime;
        
        /// <summary>
        /// Gets the number of dropped events.
        /// </summary>
        public static long DroppedEvents => _droppedEvents;
        
        /// <summary>
        /// Gets the average event dispatch time in milliseconds.
        /// </summary>
        public static double AverageDispatchTime => _averageDispatchTime;
        
        /// <summary>
        /// Gets the current visual render context.
        /// </summary>
        public static VisualRenderContext CurrentContext => _currentContext;
        
        /// <summary>
        /// Initializes the visual identity engine.
        /// </summary>
        /// <returns>True if initialized successfully.</returns>
        public static bool Initialize()
        {
            if (_engineState == VisualIdentityEngineState.Running || _engineState == VisualIdentityEngineState.Initializing)
                return false;
            
            _engineState = VisualIdentityEngineState.Initializing;
            
            // Phase 1 systems are already initialized via static constructors
            // PerformanceManager, DeviceProfiler, LODManager
            
            _engineUptime = 0.0;
            _droppedEvents = 0;
            _averageDispatchTime = 0.0;
            _currentContext = VisualRenderContext.Store;
            _cleanupAccumulator = 0.0;
            
            _engineState = VisualIdentityEngineState.Stopped;
            _isInitialized = true;
            return true;
        }
        
        /// <summary>
        /// Starts the visual identity engine.
        /// </summary>
        /// <returns>True if started successfully.</returns>
        public static bool Start()
        {
            if (_engineState != VisualIdentityEngineState.Stopped || !_isInitialized)
                return false;
            
            _engineState = VisualIdentityEngineState.Running;
            return true;
        }
        
        /// <summary>
        /// Stops the visual identity engine.
        /// </summary>
        /// <returns>True if stopped successfully.</returns>
        public static bool Stop()
        {
            if (_engineState != VisualIdentityEngineState.Running && _engineState != VisualIdentityEngineState.Paused)
                return false;
            
            _engineState = VisualIdentityEngineState.Stopped;
            
            // Cleanup active instances
            VisualEffectRuntimeRegistry.ClearAll();
            
            return true;
        }
        
        /// <summary>
        /// Pauses the visual identity engine.
        /// </summary>
        /// <returns>True if paused successfully.</returns>
        public static bool Pause()
        {
            if (_engineState != VisualIdentityEngineState.Running)
                return false;
            
            _engineState = VisualIdentityEngineState.Paused;
            return true;
        }
        
        /// <summary>
        /// Resumes the visual identity engine.
        /// </summary>
        /// <returns>True if resumed successfully.</returns>
        public static bool Resume()
        {
            if (_engineState != VisualIdentityEngineState.Paused)
                return false;
            
            _engineState = VisualIdentityEngineState.Running;
            return true;
        }
        
        /// <summary>
        /// Updates the visual identity engine.
        /// External application/game loop owns timing.
        /// Zero allocations in hot path.
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        public static void Update(double deltaTime)
        {
            if (_engineState != VisualIdentityEngineState.Running)
                return;
            
            // Validate deltaTime
            if (double.IsNaN(deltaTime) || deltaTime < 0 || deltaTime > 1.0)
                deltaTime = 0.016; // Cap at ~60fps equivalent
            
            _engineUptime += deltaTime;
            
            // Update Phase 1 systems
            PerformanceManager.Update();
            LODManager.Update();
            
            // Update active effect instances
            VisualEffectRuntimeRegistry.UpdateAll(deltaTime);
            
            // Cleanup expired instances periodically using accumulator
            _cleanupAccumulator += deltaTime;
            if (_cleanupAccumulator >= 10.0)
            {
                VisualEffectRuntimeRegistry.CleanupExpired();
                _cleanupAccumulator = 0.0;
            }
        }
        
        /// <summary>
        /// Gets diagnostics for the engine.
        /// Allocation-safe: returns a struct.
        /// </summary>
        /// <returns>Visual identity diagnostics.</returns>
        public static VisualIdentityDiagnostics GetDiagnostics()
        {
            return new VisualIdentityDiagnostics(
                VisualEffectRuntimeRegistry.ActiveInstanceCount,
                ParticlePool.ActiveCount,
                LODManager.CurrentLOD,
                PerformanceManager.CurrentMode,
                PerformanceManager.CurrentFPS,
                _droppedEvents,
                _averageDispatchTime,
                DeviceProfiler.CurrentProfile,
                _engineState,
                _engineUptime,
                _currentContext
            );
        }
        
        /// <summary>
        /// Shuts down the visual identity engine.
        /// </summary>
        /// <returns>True if shut down successfully.</returns>
        public static bool Shutdown()
        {
            if (_engineState == VisualIdentityEngineState.ShuttingDown || _engineState == VisualIdentityEngineState.Stopped)
                return false;
            
            _engineState = VisualIdentityEngineState.ShuttingDown;
            
            // Cleanup registry directly (Stop() requires Running/Paused state)
            VisualEffectRuntimeRegistry.ClearAll();
            
            _engineState = VisualIdentityEngineState.Stopped;
            _isInitialized = false;
            return true;
        }
        
        /// <summary>
        /// Validates the engine state.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool Validate()
        {
            // Validate engine state is defined
            if (!Enum.IsDefined(typeof(VisualIdentityEngineState), _engineState))
                return false;
            
            // Validate uptime
            if (_engineUptime < 0 || double.IsNaN(_engineUptime))
                return false;
            
            // Validate counters
            if (_droppedEvents < 0)
                return false;
            
            // Validate durations
            if (_averageDispatchTime < 0 || double.IsNaN(_averageDispatchTime))
                return false;
            
            return true;
        }
    }
}
