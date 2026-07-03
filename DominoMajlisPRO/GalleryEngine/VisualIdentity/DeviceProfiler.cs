using System;
using System.Collections.Generic;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Device Profiler for hardware capability detection and adaptive profile management.
    /// Capability decisions are based on:
    /// - Known device profiles
    /// - Manufacturer
    /// - Model
    /// - Hardware ID
    /// - Product ID
    /// - Conservative defaults
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public static class DeviceProfiler
    {
        private static DeviceProfile _currentProfile;
        private static DeviceProfileSource _profileSource;
        private static string _manufacturer;
        private static string _model;
        private static string _hardware;
        private static string _reason;
        private static bool _isForcedProfile;
        private static string _forcedProfileReason;

        /// <summary>
        /// Gets the current device profile.
        /// </summary>
        public static DeviceProfile CurrentProfile => _currentProfile;

        /// <summary>
        /// Gets the source of the current device profile.
        /// </summary>
        public static DeviceProfileSource ProfileSource => _profileSource;

        /// <summary>
        /// Gets the device manufacturer.
        /// </summary>
        public static string Manufacturer => _manufacturer;

        /// <summary>
        /// Gets the device model.
        /// </summary>
        public static string Model => _model;

        /// <summary>
        /// Gets the device hardware identifier.
        /// </summary>
        public static string Hardware => _hardware;

        /// <summary>
        /// Gets the reason for the current profile selection.
        /// </summary>
        public static string Reason => _reason;

        /// <summary>
        /// Gets whether the profile was forced (manual override).
        /// </summary>
        public static bool IsForcedProfile => _isForcedProfile;

        /// <summary>
        /// Gets the reason for forced profile (if applicable).
        /// </summary>
        public static string ForcedProfileReason => _forcedProfileReason;

        static DeviceProfiler()
        {
            DetectDeviceProfile();
        }

        /// <summary>
        /// Detects the device profile based on hardware characteristics.
        /// Uses KnownDeviceProfiles registry for recognized devices.
        /// Falls back to conservative defaults for unknown devices.
        /// Allocation-safe: no allocations after static constructor completes.
        /// </summary>
        private static void DetectDeviceProfile()
        {
#if ANDROID
            _manufacturer = Android.OS.Build.Manufacturer ?? "Unknown";
            _model = Android.OS.Build.Model ?? "Unknown";
            _hardware = Android.OS.Build.Hardware ?? "Unknown";
            
            // Check KnownDeviceProfiles registry
            if (KnownDeviceProfiles.TryGetProfile(_manufacturer, _model, _hardware, out var knownProfile, out var matchReason))
            {
                _currentProfile = knownProfile;
                _profileSource = DeviceProfileSource.KnownRegistry;
                _reason = matchReason;
                _isForcedProfile = false;
                _forcedProfileReason = null;
                return;
            }
            
            // Unknown Android device - use conservative defaults
            // Default to Lite for unknown Android devices
            _currentProfile = DeviceProfile.Lite;
            _profileSource = DeviceProfileSource.ConservativeFallback;
            _reason = "Unknown Android device - conservative default";
            _isForcedProfile = false;
            _forcedProfileReason = null;
#else
            // Non-Android platforms - use Medium as default
            _manufacturer = "Non-Android";
            _model = "Unknown";
            _hardware = "Unknown";
            _currentProfile = DeviceProfile.Medium;
            _profileSource = DeviceProfileSource.ConservativeFallback;
            _reason = "Non-Android platform - default profile";
            _isForcedProfile = false;
            _forcedProfileReason = null;
#endif
        }

        /// <summary>
        /// Forces a specific device profile (manual override).
        /// </summary>
        /// <param name="profile">The profile to force.</param>
        /// <param name="reason">The reason for the override (optional diagnostic).</param>
        public static void ForceProfile(DeviceProfile profile, string reason = null)
        {
            _currentProfile = profile;
            _profileSource = DeviceProfileSource.ForcedOverride;
            _isForcedProfile = true;
            _forcedProfileReason = reason ?? "Manual override";
            _reason = _forcedProfileReason;
        }

        /// <summary>
        /// Clears any forced profile and re-detects the device profile.
        /// </summary>
        public static void ClearForcedProfile()
        {
            _isForcedProfile = false;
            _forcedProfileReason = null;
            DetectDeviceProfile();
        }

        /// <summary>
        /// Gets the adaptive profile multiplier for a given device profile.
        /// </summary>
        /// <param name="profile">The device profile.</param>
        /// <returns>The quality multiplier (0.0-1.0).</returns>
        public static double GetAdaptiveProfileMultiplier(DeviceProfile profile)
        {
            return profile switch
            {
                DeviceProfile.Ultra => 1.0,
                DeviceProfile.High => 0.9,
                DeviceProfile.Medium => 0.75,
                DeviceProfile.Lite => 0.5,
                DeviceProfile.VeryLite => 0.25,
                _ => 0.75
            };
        }

        /// <summary>
        /// Gets diagnostics information about the current device profile.
        /// Allocation-safe: returns a struct, no heap allocation.
        /// </summary>
        /// <returns>Device profile diagnostics.</returns>
        public static DeviceProfileDiagnostics GetDiagnostics()
        {
            return new DeviceProfileDiagnostics
            {
                Manufacturer = _manufacturer,
                Model = _model,
                Hardware = _hardware,
                SelectedProfile = _currentProfile,
                ProfileSource = _profileSource,
                Reason = _reason,
                IsForced = _isForcedProfile,
                ForcedReason = _forcedProfileReason
            };
        }
    }

    /// <summary>
    /// Device profile diagnostics information.
    /// Struct for allocation-safe diagnostics reporting.
    /// </summary>
    public struct DeviceProfileDiagnostics
    {
        public string Manufacturer;
        public string Model;
        public string Hardware;
        public DeviceProfile SelectedProfile;
        public DeviceProfileSource ProfileSource;
        public string Reason;
        public bool IsForced;
        public string ForcedReason;
    }

    /// <summary>
    /// Known device profiles registry.
    /// Supports multiple identifier types: Model Names, Hardware IDs, Product IDs.
    /// Allows future device additions without modifying detection logic.
    /// Allocation-safe: all lookups use string comparison without allocations.
    /// 
    /// HardwareId and ProductId support is available but will be expanded incrementally
    /// as more device-specific identifiers are documented. Current registry entries use
    /// model-based matching primarily, with identifier support reserved for future expansion.
    /// </summary>
    public static class KnownDeviceProfiles
    {
        private static readonly Dictionary<string, DeviceProfileEntry> _deviceRegistry;

        static KnownDeviceProfiles()
        {
            _deviceRegistry = new Dictionary<string, DeviceProfileEntry>(StringComparer.OrdinalIgnoreCase)
            {
                // Realme devices
                { "realme_C33", new DeviceProfileEntry(DeviceProfile.VeryLite, "Realme C33") },
                { "realme_C35", new DeviceProfileEntry(DeviceProfile.VeryLite, "Realme C35") },
                { "realme_C55", new DeviceProfileEntry(DeviceProfile.Lite, "Realme C55") },
                { "realme_C67", new DeviceProfileEntry(DeviceProfile.Lite, "Realme C67") },
                { "realme_11 Pro", new DeviceProfileEntry(DeviceProfile.Medium, "Realme 11 Pro") },
                { "realme_11 Pro+", new DeviceProfileEntry(DeviceProfile.Medium, "Realme 11 Pro+") },
                { "realme_GT Neo 3", new DeviceProfileEntry(DeviceProfile.High, "Realme GT Neo 3") },
                { "realme_GT Neo 3T", new DeviceProfileEntry(DeviceProfile.High, "Realme GT Neo 3T") },
                { "realme_GT 2", new DeviceProfileEntry(DeviceProfile.High, "Realme GT 2") },
                { "realme_GT 2 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "Realme GT 2 Pro") },
                
                // Samsung devices
                { "samsung_Galaxy A03", new DeviceProfileEntry(DeviceProfile.VeryLite, "Samsung Galaxy A03") },
                { "samsung_Galaxy A04", new DeviceProfileEntry(DeviceProfile.VeryLite, "Samsung Galaxy A04") },
                { "samsung_Galaxy A05", new DeviceProfileEntry(DeviceProfile.VeryLite, "Samsung Galaxy A05") },
                { "samsung_Galaxy A13", new DeviceProfileEntry(DeviceProfile.Lite, "Samsung Galaxy A13") },
                { "samsung_Galaxy A14", new DeviceProfileEntry(DeviceProfile.Lite, "Samsung Galaxy A14") },
                { "samsung_Galaxy A23", new DeviceProfileEntry(DeviceProfile.Lite, "Samsung Galaxy A23") },
                { "samsung_Galaxy A24", new DeviceProfileEntry(DeviceProfile.Medium, "Samsung Galaxy A24") },
                { "samsung_Galaxy A33", new DeviceProfileEntry(DeviceProfile.Medium, "Samsung Galaxy A33") },
                { "samsung_Galaxy A34", new DeviceProfileEntry(DeviceProfile.Medium, "Samsung Galaxy A34") },
                { "samsung_Galaxy A53", new DeviceProfileEntry(DeviceProfile.High, "Samsung Galaxy A53") },
                { "samsung_Galaxy A54", new DeviceProfileEntry(DeviceProfile.High, "Samsung Galaxy A54") },
                { "samsung_Galaxy S21", new DeviceProfileEntry(DeviceProfile.High, "Samsung Galaxy S21") },
                { "samsung_Galaxy S22", new DeviceProfileEntry(DeviceProfile.High, "Samsung Galaxy S22") },
                { "samsung_Galaxy S23", new DeviceProfileEntry(DeviceProfile.Ultra, "Samsung Galaxy S23") },
                { "samsung_Galaxy S24", new DeviceProfileEntry(DeviceProfile.Ultra, "Samsung Galaxy S24") },
                
                // Xiaomi devices
                { "xiaomi_Redmi 9A", new DeviceProfileEntry(DeviceProfile.VeryLite, "Xiaomi Redmi 9A") },
                { "xiaomi_Redmi 9C", new DeviceProfileEntry(DeviceProfile.VeryLite, "Xiaomi Redmi 9C") },
                { "xiaomi_Redmi 10A", new DeviceProfileEntry(DeviceProfile.VeryLite, "Xiaomi Redmi 10A") },
                { "xiaomi_Redmi 10C", new DeviceProfileEntry(DeviceProfile.VeryLite, "Xiaomi Redmi 10C") },
                { "xiaomi_Redmi Note 10", new DeviceProfileEntry(DeviceProfile.Lite, "Xiaomi Redmi Note 10") },
                { "xiaomi_Redmi Note 11", new DeviceProfileEntry(DeviceProfile.Lite, "Xiaomi Redmi Note 11") },
                { "xiaomi_Redmi Note 12", new DeviceProfileEntry(DeviceProfile.Medium, "Xiaomi Redmi Note 12") },
                { "xiaomi_Redmi Note 13", new DeviceProfileEntry(DeviceProfile.Medium, "Xiaomi Redmi Note 13") },
                { "xiaomi_POCO F3", new DeviceProfileEntry(DeviceProfile.High, "Xiaomi POCO F3") },
                { "xiaomi_POCO F4", new DeviceProfileEntry(DeviceProfile.High, "Xiaomi POCO F4") },
                { "xiaomi_POCO F5", new DeviceProfileEntry(DeviceProfile.Ultra, "Xiaomi POCO F5") },
                { "xiaomi_POCO X3", new DeviceProfileEntry(DeviceProfile.Medium, "Xiaomi POCO X3") },
                { "xiaomi_POCO X4", new DeviceProfileEntry(DeviceProfile.High, "Xiaomi POCO X4") },
                { "xiaomi_POCO X5", new DeviceProfileEntry(DeviceProfile.High, "Xiaomi POCO X5") },
                { "xiaomi_Xiaomi 12", new DeviceProfileEntry(DeviceProfile.High, "Xiaomi 12") },
                { "xiaomi_Xiaomi 13", new DeviceProfileEntry(DeviceProfile.Ultra, "Xiaomi 13") },
                { "xiaomi_Xiaomi 14", new DeviceProfileEntry(DeviceProfile.Ultra, "Xiaomi 14") },
                
                // OnePlus devices
                { "oneplus_Nord CE 2", new DeviceProfileEntry(DeviceProfile.Lite, "OnePlus Nord CE 2") },
                { "oneplus_Nord CE 3", new DeviceProfileEntry(DeviceProfile.Medium, "OnePlus Nord CE 3") },
                { "oneplus_Nord 2", new DeviceProfileEntry(DeviceProfile.Medium, "OnePlus Nord 2") },
                { "oneplus_Nord 3", new DeviceProfileEntry(DeviceProfile.High, "OnePlus Nord 3") },
                { "oneplus_8", new DeviceProfileEntry(DeviceProfile.High, "OnePlus 8") },
                { "oneplus_8 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 8 Pro") },
                { "oneplus_9", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 9") },
                { "oneplus_9 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 9 Pro") },
                { "oneplus_10", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 10") },
                { "oneplus_10 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 10 Pro") },
                { "oneplus_11", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 11") },
                { "oneplus_11 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 11 Pro") },
                { "oneplus_12", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 12") },
                { "oneplus_12 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "OnePlus 12 Pro") },
                
                // Google devices
                { "google_Pixel 4a", new DeviceProfileEntry(DeviceProfile.Medium, "Google Pixel 4a") },
                { "google_Pixel 5", new DeviceProfileEntry(DeviceProfile.Medium, "Google Pixel 5") },
                { "google_Pixel 6", new DeviceProfileEntry(DeviceProfile.High, "Google Pixel 6") },
                { "google_Pixel 6a", new DeviceProfileEntry(DeviceProfile.Medium, "Google Pixel 6a") },
                { "google_Pixel 6 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "Google Pixel 6 Pro") },
                { "google_Pixel 7", new DeviceProfileEntry(DeviceProfile.High, "Google Pixel 7") },
                { "google_Pixel 7a", new DeviceProfileEntry(DeviceProfile.Medium, "Google Pixel 7a") },
                { "google_Pixel 7 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "Google Pixel 7 Pro") },
                { "google_Pixel 8", new DeviceProfileEntry(DeviceProfile.Ultra, "Google Pixel 8") },
                { "google_Pixel 8a", new DeviceProfileEntry(DeviceProfile.High, "Google Pixel 8a") },
                { "google_Pixel 8 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "Google Pixel 8 Pro") },
                { "google_Pixel Fold", new DeviceProfileEntry(DeviceProfile.Ultra, "Google Pixel Fold") },
                
                // Motorola devices
                { "motorola_Moto G13", new DeviceProfileEntry(DeviceProfile.VeryLite, "Motorola Moto G13") },
                { "motorola_Moto G23", new DeviceProfileEntry(DeviceProfile.VeryLite, "Motorola Moto G23") },
                { "motorola_Moto G53", new DeviceProfileEntry(DeviceProfile.Lite, "Motorola Moto G53") },
                { "motorola_Moto G73", new DeviceProfileEntry(DeviceProfile.Medium, "Motorola Moto G73") },
                { "motorola_Moto G82", new DeviceProfileEntry(DeviceProfile.Medium, "Motorola Moto G82") },
                { "motorola_Moto G84", new DeviceProfileEntry(DeviceProfile.High, "Motorola Moto G84") },
                { "motorola_Edge 30", new DeviceProfileEntry(DeviceProfile.High, "Motorola Edge 30") },
                { "motorola_Edge 40", new DeviceProfileEntry(DeviceProfile.Ultra, "Motorola Edge 40") },
                { "motorola_Edge 40 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "Motorola Edge 40 Pro") },
                
                // Vivo devices
                { "vivo_Y21", new DeviceProfileEntry(DeviceProfile.VeryLite, "Vivo Y21") },
                { "vivo_Y22", new DeviceProfileEntry(DeviceProfile.VeryLite, "Vivo Y22") },
                { "vivo_Y33", new DeviceProfileEntry(DeviceProfile.Lite, "Vivo Y33") },
                { "vivo_Y53", new DeviceProfileEntry(DeviceProfile.Lite, "Vivo Y53") },
                { "vivo_V25", new DeviceProfileEntry(DeviceProfile.Medium, "Vivo V25") },
                { "vivo_V27", new DeviceProfileEntry(DeviceProfile.High, "Vivo V27") },
                { "vivo_V29", new DeviceProfileEntry(DeviceProfile.High, "Vivo V29") },
                { "vivo_X90", new DeviceProfileEntry(DeviceProfile.Ultra, "Vivo X90") },
                { "vivo_X90 Pro", new DeviceProfileEntry(DeviceProfile.Ultra, "Vivo X90 Pro") },
                
                // Oppo devices
                { "oppo_A54", new DeviceProfileEntry(DeviceProfile.VeryLite, "Oppo A54") },
                { "oppo_A74", new DeviceProfileEntry(DeviceProfile.Lite, "Oppo A74") },
                { "oppo_A76", new DeviceProfileEntry(DeviceProfile.Lite, "Oppo A76") },
                { "oppo_A94", new DeviceProfileEntry(DeviceProfile.Medium, "Oppo A94") },
                { "oppo_Reno 7", new DeviceProfileEntry(DeviceProfile.High, "Oppo Reno 7") },
                { "oppo_Reno 8", new DeviceProfileEntry(DeviceProfile.High, "Oppo Reno 8") },
                { "oppo_Reno 9", new DeviceProfileEntry(DeviceProfile.High, "Oppo Reno 9") },
                { "oppo_Find X5", new DeviceProfileEntry(DeviceProfile.Ultra, "Oppo Find X5") },
                { "oppo_Find X6", new DeviceProfileEntry(DeviceProfile.Ultra, "Oppo Find X6") }
            };
        }

        /// <summary>
        /// Attempts to get the profile for a known device.
        /// Supports Model Names, Hardware IDs, and Product IDs.
        /// Allocation-safe: uses string comparison without allocations.
        /// </summary>
        /// <param name="manufacturer">The device manufacturer.</param>
        /// <param name="model">The device model.</param>
        /// <param name="hardware">The device hardware ID.</param>
        /// <param name="profile">The resulting profile if found.</param>
        /// <param name="reason">The reason for the match (diagnostic).</param>
        /// <returns>True if the device is known and profile was found.</returns>
        public static bool TryGetProfile(string manufacturer, string model, string hardware, out DeviceProfile profile, out string reason)
        {
            profile = DeviceProfile.Medium; // Default fallback
            reason = "Not found in registry";

            if (string.IsNullOrWhiteSpace(manufacturer))
                return false;

            // Normalize manufacturer for comparison
            var normalizedManufacturer = manufacturer.Trim().ToLowerInvariant();
            var normalizedHardware = !string.IsNullOrWhiteSpace(hardware) ? hardware.Trim().ToLowerInvariant() : null;

            // Try model-based lookup first
            if (!string.IsNullOrWhiteSpace(model))
            {
                var modelKey = $"{normalizedManufacturer}_{model.Trim()}";
                if (_deviceRegistry.TryGetValue(modelKey, out var entry))
                {
                    profile = entry.Profile;
                    reason = $"Matched by model: {entry.DeviceName}";
                    return true;
                }

                // Try partial model match
                foreach (var kvp in _deviceRegistry)
                {
                    if (kvp.Key.StartsWith(normalizedManufacturer + "_", StringComparison.OrdinalIgnoreCase))
                    {
                        var modelPart = kvp.Key.Substring(normalizedManufacturer.Length + 1);
                        if (model.Trim().IndexOf(modelPart, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            profile = kvp.Value.Profile;
                            reason = $"Matched by partial model: {kvp.Value.DeviceName}";
                            return true;
                        }
                    }
                }
            }

            // Try hardware-based lookup using HardwareId
            if (!string.IsNullOrWhiteSpace(normalizedHardware))
            {
                foreach (var kvp in _deviceRegistry)
                {
                    if (kvp.Key.StartsWith(normalizedManufacturer + "_", StringComparison.OrdinalIgnoreCase))
                    {
                        var entry = kvp.Value;
                        // Match against NormalizedHardwareId if provided (pre-normalized during registration)
                        if (!string.IsNullOrWhiteSpace(entry.NormalizedHardwareId) && 
                            normalizedHardware.Contains(entry.NormalizedHardwareId))
                        {
                            profile = entry.Profile;
                            reason = $"Matched by hardware ID: {entry.DeviceName}";
                            return true;
                        }
                        // Match against NormalizedProductId if provided (pre-normalized during registration)
                        if (!string.IsNullOrWhiteSpace(entry.NormalizedProductId) && 
                            normalizedHardware.Contains(entry.NormalizedProductId))
                        {
                            profile = entry.Profile;
                            reason = $"Matched by product ID: {entry.DeviceName}";
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Registers a new device profile.
        /// Intended for startup/developer registration only.
        /// Not part of runtime hot paths.
        /// Allows runtime addition of device profiles.
        /// </summary>
        /// <param name="manufacturer">The device manufacturer.</param>
        /// <param name="model">The device model.</param>
        /// <param name="profile">The device profile.</param>
        /// <param name="hardwareId">Optional hardware ID for matching.</param>
        /// <param name="productId">Optional product ID for matching.</param>
        public static void RegisterDevice(string manufacturer, string model, DeviceProfile profile, string hardwareId = null, string productId = null)
        {
            if (string.IsNullOrWhiteSpace(manufacturer) || string.IsNullOrWhiteSpace(model))
                return;

            var normalizedManufacturer = manufacturer.Trim().ToLowerInvariant();
            var normalizedModel = model.Trim();
            var key = $"{normalizedManufacturer}_{normalizedModel}";
            var deviceName = $"{char.ToUpper(normalizedManufacturer[0]) + normalizedManufacturer.Substring(1)} {normalizedModel}";

            _deviceRegistry[key] = new DeviceProfileEntry(profile, deviceName, hardwareId, productId);
        }
    }

    /// <summary>
    /// Device profile entry for the registry.
    /// Struct for allocation-safe storage.
    /// Supports multiple identifier types for flexible matching.
    /// Identifiers are normalized during registration for allocation-free lookups.
    /// </summary>
    public struct DeviceProfileEntry
    {
        public DeviceProfile Profile;
        public string DeviceName;
        public string HardwareId;
        public string ProductId;
        public string NormalizedHardwareId;
        public string NormalizedProductId;

        public DeviceProfileEntry(DeviceProfile profile, string deviceName, string hardwareId = null, string productId = null)
        {
            Profile = profile;
            DeviceName = deviceName;
            HardwareId = hardwareId;
            ProductId = productId;
            NormalizedHardwareId = !string.IsNullOrWhiteSpace(hardwareId) ? hardwareId.ToLowerInvariant() : null;
            NormalizedProductId = !string.IsNullOrWhiteSpace(productId) ? productId.ToLowerInvariant() : null;
        }
    }
}
