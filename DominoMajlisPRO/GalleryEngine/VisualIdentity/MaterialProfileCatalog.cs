using System;
using System.Collections.Generic;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Catalog of material profiles for visual assets.
    /// Returns cloned instances to prevent mutation of catalog defaults.
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public static class MaterialProfileCatalog
    {
        private static readonly Dictionary<string, MaterialProfile> _catalog;
        private static readonly IReadOnlyList<string> _cachedProfileIds;
        private static readonly MaterialProfile _defaultProfile;
        
        static MaterialProfileCatalog()
        {
            _catalog = new Dictionary<string, MaterialProfile>(StringComparer.OrdinalIgnoreCase);
            
            // Initialize catalog with default profiles
            InitializeCatalog();
            
            // Cache profile IDs for read-only access
            var profileIds = new List<string>(_catalog.Keys);
            _cachedProfileIds = profileIds.AsReadOnly();
            
            // Create default fallback profile
            _defaultProfile = CreateDefaultFallbackProfile();
        }
        
        /// <summary>
        /// Gets a material profile by ID.
        /// Returns a cloned instance to prevent mutation of catalog defaults.
        /// </summary>
        /// <param name="profileId">The profile ID.</param>
        /// <returns>A cloned material profile, or null if not found.</returns>
        public static MaterialProfile GetProfile(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return null;
            
            if (_catalog.TryGetValue(profileId, out var profile))
            {
                return profile.Clone();
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets a material profile by emblem type.
        /// Returns a cloned instance to prevent mutation of catalog defaults.
        /// </summary>
        /// <param name="emblemType">The emblem type.</param>
        /// <returns>A cloned material profile, or null if not found.</returns>
        public static MaterialProfile GetProfileByEmblem(EmblemType emblemType)
        {
            var profileId = BuildDefaultProfileId(emblemType);
            return GetProfile(profileId);
        }
        
        /// <summary>
        /// Gets a material profile by ID, or returns default if not found.
        /// Returns a cloned instance to prevent mutation of catalog defaults.
        /// </summary>
        /// <param name="profileId">The profile ID.</param>
        /// <returns>A cloned material profile, or the default fallback profile.</returns>
        public static MaterialProfile GetOrDefault(string profileId)
        {
            var profile = GetProfile(profileId);
            return profile ?? _defaultProfile.Clone();
        }
        
        /// <summary>
        /// Gets a material profile by emblem type, or returns default if not found.
        /// Returns a cloned instance to prevent mutation of catalog defaults.
        /// </summary>
        /// <param name="emblemType">The emblem type.</param>
        /// <returns>A cloned material profile, or the default fallback profile.</returns>
        public static MaterialProfile GetOrDefaultByEmblem(EmblemType emblemType)
        {
            var profile = GetProfileByEmblem(emblemType);
            return profile ?? _defaultProfile.Clone();
        }
        
        /// <summary>
        /// Gets all profile IDs.
        /// Returns cached read-only list to avoid allocations.
        /// </summary>
        /// <returns>Read-only list of profile IDs.</returns>
        public static IReadOnlyList<string> GetAllProfileIds()
        {
            return _cachedProfileIds;
        }
        
        /// <summary>
        /// Gets the default fallback profile.
        /// Returns a cloned instance to prevent mutation.
        /// </summary>
        /// <returns>A cloned default material profile.</returns>
        public static MaterialProfile GetDefaultProfile()
        {
            return _defaultProfile.Clone();
        }
        
        /// <summary>
        /// Builds a default profile ID for an emblem type.
        /// </summary>
        /// <param name="emblemType">The emblem type.</param>
        /// <returns>The default profile ID string.</returns>
        private static string BuildDefaultProfileId(EmblemType emblemType)
        {
            return $"Material_{emblemType}";
        }
        
        /// <summary>
        /// Builds a deterministic GUID for an emblem type.
        /// Uses fixed GUIDs based on emblem type for deterministic identity.
        /// </summary>
        /// <param name="emblemType">The emblem type.</param>
        /// <returns>A deterministic GUID.</returns>
        private static Guid BuildDeterministicGuid(EmblemType emblemType)
        {
            return emblemType switch
            {
                EmblemType.Dragon => new Guid("00000000-0000-0000-0000-000000000001"),
                EmblemType.Lion => new Guid("00000000-0000-0000-0000-000000000002"),
                EmblemType.Eagle => new Guid("00000000-0000-0000-0000-000000000003"),
                EmblemType.Falcon => new Guid("00000000-0000-0000-0000-000000000004"),
                EmblemType.Wolf => new Guid("00000000-0000-0000-0000-000000000005"),
                EmblemType.Bull => new Guid("00000000-0000-0000-0000-000000000006"),
                EmblemType.Crown => new Guid("00000000-0000-0000-0000-000000000007"),
                EmblemType.Shield => new Guid("00000000-0000-0000-0000-000000000008"),
                _ => Guid.Empty
            };
        }
        
        /// <summary>
        /// Initializes the catalog with default material profiles.
        /// Enum.GetValues() is executed only during catalog initialization and is not part of any runtime hot path.
        /// </summary>
        private static void InitializeCatalog()
        {
            // Create profiles for each emblem type
            foreach (EmblemType emblem in Enum.GetValues(typeof(EmblemType)))
            {
                if (emblem == EmblemType.None)
                    continue;
                
                var profile = CreateProfileForEmblem(emblem);
                if (profile != null)
                {
                    var profileId = BuildDefaultProfileId(emblem);
                    _catalog[profileId] = profile;
                }
            }
            
            // Add a generic default profile
            var defaultProfile = CreateGenericDefaultProfile();
            if (defaultProfile != null)
            {
                _catalog["Material_Default"] = defaultProfile;
            }
        }
        
        /// <summary>
        /// Creates a material profile for a specific emblem type.
        /// </summary>
        /// <param name="emblemType">The emblem type.</param>
        /// <returns>A validated material profile, or null if validation fails.</returns>
        private static MaterialProfile CreateProfileForEmblem(EmblemType emblemType)
        {
            var profile = new MaterialProfile
            {
                MaterialProfileId = BuildDeterministicGuid(emblemType),
                DisplayName = $"{emblemType} Material",
                Description = $"Default material profile for {emblemType} emblem",
                MaterialType = MaterialType.Default,
                BaseColor = GetBaseColorForEmblem(emblemType),
                Metallic = GetMetallicForEmblem(emblemType),
                Roughness = GetRoughnessForEmblem(emblemType),
                Opacity = 1.0,
                SpecularStrength = 0.5,
                ReflectionStrength = 0.5,
                GlowResponse = GetGlowResponseForEmblem(emblemType),
                HeatResponse = 0.5,
                ColdResponse = 0.5,
                ShadowResponse = 0.5,
                ParticleInteraction = 0.0,
                LightAbsorption = 0.5,
                LightEmission = GetLightEmissionForEmblem(emblemType),
                EmissionColor = GetEmissionColorForEmblem(emblemType),
                ReflectionColor = "#FFFFFF",
                HeatTintColor = "#FF6600",
                ColdTintColor = "#0066FF",
                ShadowTintColor = "#000000",
                IsImmutable = true // Catalog profiles are immutable
            };
            
            // Clamp values to valid ranges
            profile.ClampValues();
            
            // Validate profile before adding to catalog
            if (!profile.Validate())
            {
                return null;
            }
            
            return profile;
        }
        
        /// <summary>
        /// Creates a generic default material profile.
        /// </summary>
        /// <returns>A validated material profile, or null if validation fails.</returns>
        private static MaterialProfile CreateGenericDefaultProfile()
        {
            var profile = new MaterialProfile
            {
                MaterialProfileId = new Guid("00000000-0000-0000-0000-000000000099"),
                DisplayName = "Default Material",
                Description = "Generic default material profile",
                MaterialType = MaterialType.Default,
                BaseColor = "#FFFFFF",
                Metallic = 0.0,
                Roughness = 0.5,
                Opacity = 1.0,
                SpecularStrength = 0.5,
                ReflectionStrength = 0.5,
                GlowResponse = 0.0,
                HeatResponse = 0.5,
                ColdResponse = 0.5,
                ShadowResponse = 0.5,
                ParticleInteraction = 0.0,
                LightAbsorption = 0.5,
                LightEmission = 0.0,
                EmissionColor = "#000000",
                ReflectionColor = "#FFFFFF",
                HeatTintColor = "#FF6600",
                ColdTintColor = "#0066FF",
                ShadowTintColor = "#000000",
                IsImmutable = true // Catalog profiles are immutable
            };
            
            // Clamp values to valid ranges
            profile.ClampValues();
            
            // Validate profile before adding to catalog
            if (!profile.Validate())
            {
                return null;
            }
            
            return profile;
        }
        
        /// <summary>
        /// Creates the default fallback profile.
        /// </summary>
        /// <returns>A validated default fallback profile.</returns>
        private static MaterialProfile CreateDefaultFallbackProfile()
        {
            var profile = new MaterialProfile
            {
                MaterialProfileId = new Guid("00000000-0000-0000-0000-000000000100"),
                DisplayName = "Fallback Material",
                Description = "Fallback material profile when no specific profile is found",
                MaterialType = MaterialType.Default,
                BaseColor = "#FFFFFF",
                Metallic = 0.0,
                Roughness = 0.5,
                Opacity = 1.0,
                SpecularStrength = 0.5,
                ReflectionStrength = 0.5,
                GlowResponse = 0.0,
                HeatResponse = 0.5,
                ColdResponse = 0.5,
                ShadowResponse = 0.5,
                ParticleInteraction = 0.0,
                LightAbsorption = 0.5,
                LightEmission = 0.0,
                EmissionColor = "#000000",
                ReflectionColor = "#FFFFFF",
                HeatTintColor = "#FF6600",
                ColdTintColor = "#0066FF",
                ShadowTintColor = "#000000",
                IsImmutable = true
            };
            
            profile.ClampValues();
            
            if (!profile.Validate())
            {
                // Fallback profile must always be valid
                throw new InvalidOperationException("Default fallback profile validation failed");
            }
            
            return profile;
        }
        
        // Helper methods for emblem-specific properties
        private static string GetBaseColorForEmblem(EmblemType emblemType)
        {
            return emblemType switch
            {
                EmblemType.Dragon => "#FF0000",
                EmblemType.Lion => "#FFD700",
                EmblemType.Eagle => "#8B4513",
                EmblemType.Falcon => "#A0522D",
                EmblemType.Wolf => "#696969",
                EmblemType.Bull => "#800000",
                EmblemType.Crown => "#FFD700",
                EmblemType.Shield => "#C0C0C0",
                _ => "#FFFFFF"
            };
        }
        
        private static double GetMetallicForEmblem(EmblemType emblemType)
        {
            return emblemType switch
            {
                EmblemType.Crown => 0.8,
                EmblemType.Shield => 0.7,
                EmblemType.Dragon => 0.6,
                _ => 0.0
            };
        }
        
        private static double GetRoughnessForEmblem(EmblemType emblemType)
        {
            return emblemType switch
            {
                EmblemType.Crown => 0.2,
                EmblemType.Shield => 0.3,
                EmblemType.Dragon => 0.4,
                _ => 0.5
            };
        }
        
        private static double GetGlowResponseForEmblem(EmblemType emblemType)
        {
            return emblemType switch
            {
                EmblemType.Dragon => 0.8,
                EmblemType.Crown => 0.6,
                EmblemType.Eagle => 0.4,
                _ => 0.0
            };
        }
        
        private static double GetLightEmissionForEmblem(EmblemType emblemType)
        {
            return emblemType switch
            {
                EmblemType.Dragon => 0.3,
                EmblemType.Crown => 0.2,
                _ => 0.0
            };
        }
        
        private static string GetEmissionColorForEmblem(EmblemType emblemType)
        {
            return emblemType switch
            {
                EmblemType.Dragon => "#FF4500",
                EmblemType.Crown => "#FFD700",
                _ => "#000000"
            };
        }
    }
}
