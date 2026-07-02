using System;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Visual identity runtime diagnostics for Living Visual Identity Engine.
    /// Provides runtime metrics and status information.
    /// Allocation-safe: returns a struct, no heap allocation.
    /// 
    /// Part of Phase 2.1 Runtime Integration implementation.
    /// Phase 2.1 Status: Foundation Lock Approved
    /// Do not modify unless a compiler error or real runtime bug is discovered.
    /// </summary>
    public readonly struct VisualIdentityDiagnostics
    {
        /// <summary>
        /// Gets the number of active effect instances.
        /// </summary>
        public readonly int ActiveEffectCount;
        
        /// <summary>
        /// Gets the number of active particles.
        /// </summary>
        public readonly int ActiveParticleCount;
        
        /// <summary>
        /// Gets the current LOD level.
        /// </summary>
        public readonly LODLevel CurrentLOD;
        
        /// <summary>
        /// Gets the current performance mode.
        /// </summary>
        public readonly PerformanceMode PerformanceMode;
        
        /// <summary>
        /// Gets the current FPS.
        /// </summary>
        public readonly double FPS;
        
        /// <summary>
        /// Gets the number of dropped events.
        /// </summary>
        public readonly long DroppedEvents;
        
        /// <summary>
        /// Gets the average event dispatch time in milliseconds.
        /// </summary>
        public readonly double AverageDispatchTime;
        
        /// <summary>
        /// Gets the device profile.
        /// </summary>
        public readonly DeviceProfile DeviceProfile;
        
        /// <summary>
        /// Gets the engine state.
        /// </summary>
        public readonly VisualIdentityEngineState EngineState;
        
        /// <summary>
        /// Gets the engine uptime in seconds.
        /// </summary>
        public readonly double EngineUptime;
        
        /// <summary>
        /// Gets the current visual render context.
        /// </summary>
        public readonly VisualRenderContext CurrentContext;
        
        /// <summary>
        /// Creates a new visual identity diagnostics instance.
        /// </summary>
        public VisualIdentityDiagnostics(
            int activeEffectCount,
            int activeParticleCount,
            LODLevel currentLOD,
            PerformanceMode performanceMode,
            double fps,
            long droppedEvents,
            double averageDispatchTime,
            DeviceProfile deviceProfile,
            VisualIdentityEngineState engineState,
            double engineUptime,
            VisualRenderContext currentContext)
        {
            ActiveEffectCount = activeEffectCount;
            ActiveParticleCount = activeParticleCount;
            CurrentLOD = currentLOD;
            PerformanceMode = performanceMode;
            FPS = fps;
            DroppedEvents = droppedEvents;
            AverageDispatchTime = averageDispatchTime;
            DeviceProfile = deviceProfile;
            EngineState = engineState;
            EngineUptime = engineUptime;
            CurrentContext = currentContext;
        }
        
        /// <summary>
        /// Creates a default diagnostics instance.
        /// </summary>
        public static VisualIdentityDiagnostics CreateDefault()
        {
            return new VisualIdentityDiagnostics(
                0,
                0,
                LODManager.CurrentLOD,
                PerformanceManager.CurrentMode,
                PerformanceManager.CurrentFPS,
                0,
                0.0,
                DeviceProfiler.CurrentProfile,
                VisualIdentityEngineState.Stopped,
                0.0,
                VisualRenderContext.Store
            );
        }
        
        /// <summary>
        /// Validates the diagnostics data.
        /// Not a hot path - used for consistency checks.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool Validate()
        {
            // Validate counts
            if (ActiveEffectCount < 0)
                return false;
            
            if (ActiveParticleCount < 0)
                return false;
            
            if (DroppedEvents < 0)
                return false;
            
            // Validate FPS
            if (FPS < 0 || double.IsNaN(FPS))
                return false;
            
            // Validate durations
            if (AverageDispatchTime < 0 || double.IsNaN(AverageDispatchTime))
                return false;
            
            if (EngineUptime < 0 || double.IsNaN(EngineUptime))
                return false;
            
            return true;
        }
    }
}
