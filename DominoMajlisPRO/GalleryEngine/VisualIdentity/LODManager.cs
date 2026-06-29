using System;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Level of Detail (LOD) Manager for dynamic quality adjustment.
    /// Adjusts rendering quality based on device capability and performance.
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public static class LODManager
    {
        private static LODLevel _currentLOD;
        private static LODReason _currentReason;
        private static bool _autoLODEnabled;
        private static bool _isInitialized;
        private static bool _isPhotoMode;

        // Cached static readonly LODSettings instances per LOD level (zero allocations)
        private static readonly LODSettings _veryLowSettings;
        private static readonly LODSettings _lowSettings;
        private static readonly LODSettings _mediumSettings;
        private static readonly LODSettings _highSettings;

        static LODManager()
        {
            // Initialize cached LOD settings
            _veryLowSettings = new LODSettings(
                particleMultiplier: 0.25,
                animationQualityMultiplier: 0.5,
                glowQualityMultiplier: 0.4,
                smokeMultiplier: 0.3,
                targetFrameRate: 30,
                shadowsEnabled: false,
                reflectionsEnabled: false,
                heavyEffectsEnabled: false,
                blurEnabled: false
            );

            _lowSettings = new LODSettings(
                particleMultiplier: 0.5,
                animationQualityMultiplier: 0.7,
                glowQualityMultiplier: 0.6,
                smokeMultiplier: 0.5,
                targetFrameRate: 45,
                shadowsEnabled: false,
                reflectionsEnabled: false,
                heavyEffectsEnabled: false,
                blurEnabled: false
            );

            _mediumSettings = new LODSettings(
                particleMultiplier: 0.75,
                animationQualityMultiplier: 0.85,
                glowQualityMultiplier: 0.8,
                smokeMultiplier: 0.7,
                targetFrameRate: 60,
                shadowsEnabled: true,
                reflectionsEnabled: false,
                heavyEffectsEnabled: false,
                blurEnabled: true
            );

            _highSettings = new LODSettings(
                particleMultiplier: 1.0,
                animationQualityMultiplier: 1.0,
                glowQualityMultiplier: 1.0,
                smokeMultiplier: 1.0,
                targetFrameRate: 60,
                shadowsEnabled: true,
                reflectionsEnabled: true,
                heavyEffectsEnabled: true,
                blurEnabled: true
            );

            _currentLOD = LODLevel.Medium;
            _currentReason = LODReason.Device;
            _autoLODEnabled = true;
            _isInitialized = false;
            _isPhotoMode = false;
        }

        /// <summary>
        /// Gets the current LOD level.
        /// </summary>
        public static LODLevel CurrentLOD => _currentLOD;

        /// <summary>
        /// Gets the reason for the current LOD level.
        /// </summary>
        public static LODReason CurrentReason => _currentReason;

        /// <summary>
        /// Gets whether automatic LOD adjustment is enabled.
        /// </summary>
        public static bool AutoLODEnabled => _autoLODEnabled;

        /// <summary>
        /// Gets whether LODManager is initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the LOD manager.
        /// Must be called before using LODManager.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            DetectLODFromDevice();
            _isInitialized = true;
        }

        /// <summary>
        /// Updates LOD based on current performance and context.
        /// Zero allocations per frame.
        /// </summary>
        public static void Update()
        {
            if (!_isInitialized || !_autoLODEnabled)
                return;

            // Check if device profile is forced by developer
            if (DeviceProfiler.IsForcedProfile)
            {
                // Respect developer forced profile, don't auto-adjust
                return;
            }

            // Consider performance manager recommendations
            var recommendedQuality = PerformanceManager.GetRecommendedQuality();
            var currentContext = PerformanceManager.CurrentContext;

            // Adjust LOD based on performance if not in photo mode
            if (!_isPhotoMode)
            {
                AdjustLODFromPerformance(recommendedQuality, currentContext);
            }
        }

        /// <summary>
        /// Sets a specific LOD level with a reason.
        /// </summary>
        /// <param name="lodLevel">The LOD level to set.</param>
        /// <param name="reason">The reason for the LOD change.</param>
        public static void SetLODLevel(LODLevel lodLevel, LODReason reason)
        {
            // Cap LOD by device capability
            var cappedLOD = CapLODByDevice(lodLevel);
            
            _currentLOD = cappedLOD;
            _currentReason = reason;

            // Disable auto LOD when manually set (unless it's a developer override)
            if (reason == LODReason.Manual)
            {
                _autoLODEnabled = false;
            }
        }

        /// <summary>
        /// Resets LOD to automatic detection based on device profile.
        /// </summary>
        public static void Reset()
        {
            _autoLODEnabled = true;
            DetectLODFromDevice();
        }

        /// <summary>
        /// Enables or disables photo mode.
        /// Photo mode requests higher quality but is capped by device capability.
        /// </summary>
        /// <param name="enabled">Whether photo mode is enabled.</param>
        public static void SetPhotoMode(bool enabled)
        {
            _isPhotoMode = enabled;

            if (enabled)
            {
                // Photo mode requests High quality, but capped by device capability
                var cappedLOD = CapLODByDevice(LODLevel.High);
                SetLODLevel(cappedLOD, LODReason.PhotoMode);
            }
            else
            {
                // Return to device-based LOD
                Reset();
            }
        }

        /// <summary>
        /// Gets the current LOD settings.
        /// Returns cached instance for zero allocations.
        /// </summary>
        /// <returns>The current LOD settings.</returns>
        public static LODSettings GetCurrentSettings()
        {
            return _currentLOD switch
            {
                LODLevel.VeryLow => _veryLowSettings,
                LODLevel.Low => _lowSettings,
                LODLevel.Medium => _mediumSettings,
                LODLevel.High => _highSettings,
                _ => _mediumSettings
            };
        }

        // Optional compatibility getters for existing code
        public static double GetParticleMultiplier() => GetCurrentSettings().ParticleMultiplier;
        public static double GetGlowMultiplier() => GetCurrentSettings().GlowQualityMultiplier;
        public static double GetSmokeMultiplier() => GetCurrentSettings().SmokeMultiplier;

        /// <summary>
        /// Detects LOD level from device profile.
        /// </summary>
        private static void DetectLODFromDevice()
        {
            _currentLOD = GetMaximumDeviceLOD();
            _currentReason = LODReason.Device;
        }

        /// <summary>
        /// Gets the maximum LOD level supported by the current device profile.
        /// </summary>
        /// <returns>The maximum LOD level for the device.</returns>
        private static LODLevel GetMaximumDeviceLOD()
        {
            var deviceProfile = DeviceProfiler.CurrentProfile;

            return deviceProfile switch
            {
                DeviceProfile.Ultra => LODLevel.High,
                DeviceProfile.High => LODLevel.High,
                DeviceProfile.Medium => LODLevel.Medium,
                DeviceProfile.Lite => LODLevel.Low,
                DeviceProfile.VeryLite => LODLevel.VeryLow,
                _ => LODLevel.Medium
            };
        }

        /// <summary>
        /// Adjusts LOD based on performance quality and context.
        /// Zero allocations.
        /// </summary>
        private static void AdjustLODFromPerformance(PerformanceQuality quality, VisualRenderContext context)
        {
            LODLevel targetLOD = quality switch
            {
                PerformanceQuality.High => LODLevel.High,
                PerformanceQuality.Medium => LODLevel.Medium,
                PerformanceQuality.Low => LODLevel.Low,
                PerformanceQuality.VeryLow => LODLevel.VeryLow,
                _ => LODLevel.Medium
            };

            // Cap by device capability
            targetLOD = CapLODByDevice(targetLOD);

            // Only update if different to avoid unnecessary changes
            if (targetLOD != _currentLOD)
            {
                _currentLOD = targetLOD;
                _currentReason = LODReason.Performance;
            }
        }

        /// <summary>
        /// Caps LOD level by device capability.
        /// PhotoMode and manual requests must not exceed device capability.
        /// </summary>
        private static LODLevel CapLODByDevice(LODLevel requestedLOD)
        {
            // Check if device profile is forced by developer
            if (DeviceProfiler.IsForcedProfile)
            {
                // Respect the forced profile's capability
                return GetMaximumDeviceLOD();
            }

            // Cap requested LOD by device capability
            var maxLOD = GetMaximumDeviceLOD();

            return requestedLOD <= maxLOD ? requestedLOD : maxLOD;
        }
    }

    /// <summary>
    /// LOD settings for a specific LOD level.
    /// Immutable struct for allocation-safe access.
    /// </summary>
    public readonly struct LODSettings
    {
        public readonly double ParticleMultiplier;
        public readonly double AnimationQualityMultiplier;
        public readonly double GlowQualityMultiplier;
        public readonly double SmokeMultiplier;
        public readonly int TargetFrameRate;
        public readonly bool ShadowsEnabled;
        public readonly bool ReflectionsEnabled;
        public readonly bool HeavyEffectsEnabled;
        public readonly bool BlurEnabled;

        public LODSettings(
            double particleMultiplier,
            double animationQualityMultiplier,
            double glowQualityMultiplier,
            double smokeMultiplier,
            int targetFrameRate,
            bool shadowsEnabled,
            bool reflectionsEnabled,
            bool heavyEffectsEnabled,
            bool blurEnabled)
        {
            ParticleMultiplier = particleMultiplier;
            AnimationQualityMultiplier = animationQualityMultiplier;
            GlowQualityMultiplier = glowQualityMultiplier;
            SmokeMultiplier = smokeMultiplier;
            TargetFrameRate = targetFrameRate;
            ShadowsEnabled = shadowsEnabled;
            ReflectionsEnabled = reflectionsEnabled;
            HeavyEffectsEnabled = heavyEffectsEnabled;
            BlurEnabled = blurEnabled;
        }
    }
}
