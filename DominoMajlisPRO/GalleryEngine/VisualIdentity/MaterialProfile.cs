using System;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Material profile for visual assets.
    /// JSON serializable, render-agnostic data model.
    /// Identity is assigned by Catalog, Publisher, Migration, or Asset creation pipeline.
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public class MaterialProfile
    {
        private Guid _materialProfileId;
        private string _displayName;
        private string _description;
        private MaterialType _materialType;
        
        // Base properties
        private string _baseColor;
        private double _metallic;
        private double _roughness;
        private double _opacity;
        
        // Material response fields
        private double _specularStrength;
        private double _reflectionStrength;
        private double _glowResponse;
        private double _heatResponse;
        private double _coldResponse;
        private double _shadowResponse;
        private double _particleInteraction;
        private double _lightAbsorption;
        private double _lightEmission;
        
        // Color/tint fields
        private string _emissionColor;
        private string _reflectionColor;
        private string _heatTintColor;
        private string _coldTintColor;
        private string _shadowTintColor;
        
        private bool _isImmutable;
        
        /// <summary>
        /// Gets or sets the material profile ID.
        /// Assigned by Catalog, Publisher, Migration, or Asset creation pipeline.
        /// </summary>
        public Guid MaterialProfileId
        {
            get => _materialProfileId;
            set => _materialProfileId = value;
        }
        
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value ?? string.Empty;
        }
        
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description
        {
            get => _description;
            set => _description = value ?? string.Empty;
        }
        
        /// <summary>
        /// Gets or sets the material type.
        /// </summary>
        public MaterialType MaterialType
        {
            get => _materialType;
            set => _materialType = value;
        }
        
        /// <summary>
        /// Gets or sets the base color (hex string).
        /// </summary>
        public string BaseColor
        {
            get => _baseColor;
            set => _baseColor = value ?? "#FFFFFF";
        }
        
        /// <summary>
        /// Gets or sets the metallic value [0,1].
        /// </summary>
        public double Metallic
        {
            get => _metallic;
            set => _metallic = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the roughness value [0,1].
        /// </summary>
        public double Roughness
        {
            get => _roughness;
            set => _roughness = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the opacity value [0,1].
        /// </summary>
        public double Opacity
        {
            get => _opacity;
            set => _opacity = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the specular strength [0,1].
        /// </summary>
        public double SpecularStrength
        {
            get => _specularStrength;
            set => _specularStrength = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the reflection strength [0,1].
        /// </summary>
        public double ReflectionStrength
        {
            get => _reflectionStrength;
            set => _reflectionStrength = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the glow response [0,1].
        /// </summary>
        public double GlowResponse
        {
            get => _glowResponse;
            set => _glowResponse = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the heat response [0,1].
        /// </summary>
        public double HeatResponse
        {
            get => _heatResponse;
            set => _heatResponse = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the cold response [0,1].
        /// </summary>
        public double ColdResponse
        {
            get => _coldResponse;
            set => _coldResponse = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the shadow response [0,1].
        /// </summary>
        public double ShadowResponse
        {
            get => _shadowResponse;
            set => _shadowResponse = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the particle interaction [0,1].
        /// </summary>
        public double ParticleInteraction
        {
            get => _particleInteraction;
            set => _particleInteraction = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the light absorption [0,1].
        /// </summary>
        public double LightAbsorption
        {
            get => _lightAbsorption;
            set => _lightAbsorption = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the light emission [0,1].
        /// </summary>
        public double LightEmission
        {
            get => _lightEmission;
            set => _lightEmission = Math.Clamp(value, 0, 1);
        }
        
        /// <summary>
        /// Gets or sets the emission color (hex string).
        /// </summary>
        public string EmissionColor
        {
            get => _emissionColor;
            set => _emissionColor = value ?? "#000000";
        }
        
        /// <summary>
        /// Gets or sets the reflection color (hex string).
        /// </summary>
        public string ReflectionColor
        {
            get => _reflectionColor;
            set => _reflectionColor = value ?? "#FFFFFF";
        }
        
        /// <summary>
        /// Gets or sets the heat tint color (hex string).
        /// </summary>
        public string HeatTintColor
        {
            get => _heatTintColor;
            set => _heatTintColor = value ?? "#FF6600";
        }
        
        /// <summary>
        /// Gets or sets the cold tint color (hex string).
        /// </summary>
        public string ColdTintColor
        {
            get => _coldTintColor;
            set => _coldTintColor = value ?? "#0066FF";
        }
        
        /// <summary>
        /// Gets or sets the shadow tint color (hex string).
        /// </summary>
        public string ShadowTintColor
        {
            get => _shadowTintColor;
            set => _shadowTintColor = value ?? "#000000";
        }
        
        /// <summary>
        /// Gets or sets whether the profile is immutable.
        /// Published profiles are protected from modification.
        /// </summary>
        public bool IsImmutable
        {
            get => _isImmutable;
            set => _isImmutable = value;
        }
        
        /// <summary>
        /// Initializes a new material profile.
        /// MaterialProfileId should be assigned by Catalog, Publisher, Migration, or Asset creation pipeline.
        /// </summary>
        public MaterialProfile()
        {
            _materialProfileId = Guid.Empty;
            _displayName = string.Empty;
            _description = string.Empty;
            _materialType = MaterialType.Default;
            
            // Base properties
            _baseColor = "#FFFFFF";
            _metallic = 0.0;
            _roughness = 0.5;
            _opacity = 1.0;
            
            // Material response fields
            _specularStrength = 0.5;
            _reflectionStrength = 0.5;
            _glowResponse = 0.0;
            _heatResponse = 0.5;
            _coldResponse = 0.5;
            _shadowResponse = 0.5;
            _particleInteraction = 0.0;
            _lightAbsorption = 0.5;
            _lightEmission = 0.0;
            
            // Color/tint fields
            _emissionColor = "#000000";
            _reflectionColor = "#FFFFFF";
            _heatTintColor = "#FF6600";
            _coldTintColor = "#0066FF";
            _shadowTintColor = "#000000";
            
            _isImmutable = false;
        }
        
        /// <summary>
        /// Clones the material profile.
        /// Produces an exact clone without modifying identity or display name.
        /// Preview customization belongs to Developer Studio, not the data model.
        /// </summary>
        /// <returns>An exact clone of the material profile.</returns>
        public MaterialProfile Clone()
        {
            var clone = new MaterialProfile
            {
                MaterialProfileId = _materialProfileId,
                DisplayName = _displayName,
                Description = _description,
                MaterialType = _materialType,
                
                // Base properties
                BaseColor = _baseColor,
                Metallic = _metallic,
                Roughness = _roughness,
                Opacity = _opacity,
                
                // Material response fields
                SpecularStrength = _specularStrength,
                ReflectionStrength = _reflectionStrength,
                GlowResponse = _glowResponse,
                HeatResponse = _heatResponse,
                ColdResponse = _coldResponse,
                ShadowResponse = _shadowResponse,
                ParticleInteraction = _particleInteraction,
                LightAbsorption = _lightAbsorption,
                LightEmission = _lightEmission,
                
                // Color/tint fields
                EmissionColor = _emissionColor,
                ReflectionColor = _reflectionColor,
                HeatTintColor = _heatTintColor,
                ColdTintColor = _coldTintColor,
                ShadowTintColor = _shadowTintColor,
                
                IsImmutable = _isImmutable // Preserve original immutability state
            };
            
            return clone;
        }
        
        /// <summary>
        /// Clamps all numeric values to valid ranges.
        /// </summary>
        public void ClampValues()
        {
            _metallic = Math.Clamp(_metallic, 0, 1);
            _roughness = Math.Clamp(_roughness, 0, 1);
            _opacity = Math.Clamp(_opacity, 0, 1);
            _specularStrength = Math.Clamp(_specularStrength, 0, 1);
            _reflectionStrength = Math.Clamp(_reflectionStrength, 0, 1);
            _glowResponse = Math.Clamp(_glowResponse, 0, 1);
            _heatResponse = Math.Clamp(_heatResponse, 0, 1);
            _coldResponse = Math.Clamp(_coldResponse, 0, 1);
            _shadowResponse = Math.Clamp(_shadowResponse, 0, 1);
            _particleInteraction = Math.Clamp(_particleInteraction, 0, 1);
            _lightAbsorption = Math.Clamp(_lightAbsorption, 0, 1);
            _lightEmission = Math.Clamp(_lightEmission, 0, 1);
        }
        
        /// <summary>
        /// Validates the material profile configuration.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool Validate()
        {
            // Validate MaterialProfileId (required only for published/immutable profiles)
            if (_isImmutable && _materialProfileId == Guid.Empty)
                return false;
            
            // Validate DisplayName
            if (string.IsNullOrWhiteSpace(_displayName))
                return false;
            
            // Validate MaterialType
            if (!Enum.IsDefined(typeof(MaterialType), _materialType))
                return false;
            
            // Validate color strings (basic hex format check)
            if (!IsValidHexColor(_baseColor))
                return false;
            
            if (!IsValidHexColor(_emissionColor))
                return false;
            
            if (!IsValidHexColor(_reflectionColor))
                return false;
            
            if (!IsValidHexColor(_heatTintColor))
                return false;
            
            if (!IsValidHexColor(_coldTintColor))
                return false;
            
            if (!IsValidHexColor(_shadowTintColor))
                return false;
            
            // Validate numeric ranges
            if (double.IsNaN(_metallic) || double.IsInfinity(_metallic) || _metallic < 0 || _metallic > 1)
                return false;
            
            if (double.IsNaN(_roughness) || double.IsInfinity(_roughness) || _roughness < 0 || _roughness > 1)
                return false;
            
            if (double.IsNaN(_opacity) || double.IsInfinity(_opacity) || _opacity < 0 || _opacity > 1)
                return false;
            
            if (double.IsNaN(_specularStrength) || double.IsInfinity(_specularStrength) || _specularStrength < 0 || _specularStrength > 1)
                return false;
            
            if (double.IsNaN(_reflectionStrength) || double.IsInfinity(_reflectionStrength) || _reflectionStrength < 0 || _reflectionStrength > 1)
                return false;
            
            if (double.IsNaN(_glowResponse) || double.IsInfinity(_glowResponse) || _glowResponse < 0 || _glowResponse > 1)
                return false;
            
            if (double.IsNaN(_heatResponse) || double.IsInfinity(_heatResponse) || _heatResponse < 0 || _heatResponse > 1)
                return false;
            
            if (double.IsNaN(_coldResponse) || double.IsInfinity(_coldResponse) || _coldResponse < 0 || _coldResponse > 1)
                return false;
            
            if (double.IsNaN(_shadowResponse) || double.IsInfinity(_shadowResponse) || _shadowResponse < 0 || _shadowResponse > 1)
                return false;
            
            if (double.IsNaN(_particleInteraction) || double.IsInfinity(_particleInteraction) || _particleInteraction < 0 || _particleInteraction > 1)
                return false;
            
            if (double.IsNaN(_lightAbsorption) || double.IsInfinity(_lightAbsorption) || _lightAbsorption < 0 || _lightAbsorption > 1)
                return false;
            
            if (double.IsNaN(_lightEmission) || double.IsInfinity(_lightEmission) || _lightEmission < 0 || _lightEmission > 1)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Validates if a string is a valid hex color.
        /// </summary>
        /// <param name="color">The color string to validate.</param>
        /// <returns>True if valid hex color, false otherwise.</returns>
        private static bool IsValidHexColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return false;
            
            if (color.Length != 7 || color[0] != '#')
                return false;
            
            for (int i = 1; i < 7; i++)
            {
                if (!IsHexDigit(color[i]))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Checks if a character is a valid hex digit.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if hex digit, false otherwise.</returns>
        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }
    }
}
