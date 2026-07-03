using System;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity.Particles
{
    /// <summary>
    /// Particle for Living Visual Identity Engine.
    /// Render-agnostic particle with pooling support.
    /// Particle identity is managed by ParticlePool using integer-based stable identifiers.
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public sealed class Particle
    {
        // Stable identity managed by ParticlePool (integer-based for pooled particles)
        private int _particleId;
        
        // Configuration
        private double _initialSize;
        private double _finalSize;
        private double _lifetime;
        private int _layerIndex;
        private VisualBlendMode _blendMode;
        private VisualRenderContext _renderContext;
        private ParticleEmitterType _emitterType;
        private int _ownerEmitterId;
        private int _randomSeed;
        
        // Runtime state
        private double _age;
        private double _normalizedAge;
        private bool _isExpired;
        private double _currentSize;
        private double _currentOpacity;
        private double _currentScale;
        
        // Position and velocity
        private double _x;
        private double _y;
        private double _velocityX;
        private double _velocityY;
        
        /// <summary>
        /// Gets the particle ID.
        /// Managed by ParticlePool using integer-based stable identifiers.
        /// Stable across particle lifetime unless reassigned by pool.
        /// </summary>
        public int ParticleId => _particleId;
        
        /// <summary>
        /// Gets or sets the initial particle size.
        /// </summary>
        public double InitialSize
        {
            get => _initialSize;
            set => _initialSize = Math.Max(0, value);
        }
        
        /// <summary>
        /// Gets or sets the final particle size.
        /// </summary>
        public double FinalSize
        {
            get => _finalSize;
            set => _finalSize = Math.Max(0, value);
        }
        
        /// <summary>
        /// Gets or sets the particle lifetime in seconds.
        /// </summary>
        public double Lifetime
        {
            get => _lifetime;
            set => _lifetime = Math.Max(0, value);
        }
        
        /// <summary>
        /// Gets or sets the layer index for rendering order.
        /// </summary>
        public int LayerIndex
        {
            get => _layerIndex;
            set => _layerIndex = Math.Max(0, value);
        }
        
        /// <summary>
        /// Gets or sets the blend mode.
        /// </summary>
        public VisualBlendMode BlendMode
        {
            get => _blendMode;
            set => _blendMode = value;
        }
        
        /// <summary>
        /// Gets or sets the render context.
        /// </summary>
        public VisualRenderContext RenderContext
        {
            get => _renderContext;
            set => _renderContext = value;
        }
        
        /// <summary>
        /// Gets or sets the emitter type.
        /// </summary>
        public ParticleEmitterType EmitterType
        {
            get => _emitterType;
            set => _emitterType = value;
        }
        
        /// <summary>
        /// Gets or sets the owner emitter ID.
        /// </summary>
        public int OwnerEmitterId
        {
            get => _ownerEmitterId;
            set => _ownerEmitterId = value;
        }
        
        /// <summary>
        /// Gets or sets the random seed.
        /// Reserved for deterministic emitter behavior and future reproducible particle simulations.
        /// Currently unused but reserved for Phase 2 deterministic simulation features.
        /// </summary>
        public int RandomSeed
        {
            get => _randomSeed;
            set => _randomSeed = value;
        }
        
        /// <summary>
        /// Gets the current particle age in seconds.
        /// </summary>
        public double Age => _age;
        
        /// <summary>
        /// Gets the normalized age [0,1].
        /// </summary>
        public double NormalizedAge => _normalizedAge;
        
        /// <summary>
        /// Gets whether the particle is expired.
        /// </summary>
        public bool IsExpired => _isExpired;
        
        /// <summary>
        /// Gets the current particle size.
        /// </summary>
        public double CurrentSize => _currentSize;
        
        /// <summary>
        /// Gets the current particle opacity [0,1].
        /// </summary>
        public double CurrentOpacity => _currentOpacity;
        
        /// <summary>
        /// Gets the current particle scale.
        /// </summary>
        public double CurrentScale => _currentScale;
        
        /// <summary>
        /// Gets or sets the X position.
        /// </summary>
        public double X
        {
            get => _x;
            set => _x = value;
        }
        
        /// <summary>
        /// Gets or sets the Y position.
        /// </summary>
        public double Y
        {
            get => _y;
            set => _y = value;
        }
        
        /// <summary>
        /// Gets or sets the X velocity.
        /// </summary>
        public double VelocityX
        {
            get => _velocityX;
            set => _velocityX = value;
        }
        
        /// <summary>
        /// Gets or sets the Y velocity.
        /// </summary>
        public double VelocityY
        {
            get => _velocityY;
            set => _velocityY = value;
        }
        
        /// <summary>
        /// Initializes a new particle.
        /// Particle ID should be assigned by ParticlePool using SetParticleId().
        /// </summary>
        public Particle()
        {
            _particleId = 0;
            _initialSize = 1.0;
            _finalSize = 1.0;
            _lifetime = 1.0;
            _layerIndex = 0;
            _blendMode = VisualBlendMode.Normal;
            _renderContext = VisualRenderContext.Store;
            _emitterType = ParticleEmitterType.None;
            _ownerEmitterId = 0;
            _randomSeed = 0;
            
            _age = 0;
            _normalizedAge = 0;
            _isExpired = false;
            _currentSize = 1.0;
            _currentOpacity = 1.0;
            _currentScale = 1.0;
            
            _x = 0;
            _y = 0;
            _velocityX = 0;
            _velocityY = 0;
        }
        
        /// <summary>
        /// Sets the particle ID.
        /// Called by ParticlePool to assign stable integer-based identity.
        /// </summary>
        /// <param name="particleId">The particle ID.</param>
        public void SetParticleId(int particleId)
        {
            _particleId = particleId;
        }
        
        /// <summary>
        /// Updates the particle state.
        /// Zero allocations.
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        public void Update(double deltaTime)
        {
            if (_isExpired || _lifetime <= 0)
                return;
            
            // Validate deltaTime
            if (double.IsNaN(deltaTime) || deltaTime < 0)
                deltaTime = 0;
            
            // Update age
            _age += deltaTime;
            
            // Calculate normalized age
            _normalizedAge = _lifetime > 0 ? _age / _lifetime : 1.0;
            _normalizedAge = Math.Clamp(_normalizedAge, 0, 1);
            
            // Check expiration
            _isExpired = _age >= _lifetime;
            
            // Update position
            _x += _velocityX * deltaTime;
            _y += _velocityY * deltaTime;
            
            // Update scale over lifetime
            _currentScale = CalculateScaleOverLifetime();
            
            // Update size
            _currentSize = _initialSize + (_finalSize - _initialSize) * _currentScale;
            
            // Update opacity
            _currentOpacity = CalculateOpacityOverLifetime();
            
            // Clamp opacity to [0,1]
            _currentOpacity = Math.Clamp(_currentOpacity, 0, 1);
        }
        
        /// <summary>
        /// Calculates scale over lifetime.
        /// Phase 1: Simple linear interpolation from 0 to 1.
        /// Phase 2: Will support animation curves (Grow, Shrink, EaseIn, EaseOut, Pulse)
        /// without changing Particle architecture.
        /// </summary>
        /// <returns>The scale factor.</returns>
        private double CalculateScaleOverLifetime()
        {
            return _normalizedAge;
        }
        
        /// <summary>
        /// Calculates opacity over lifetime.
        /// Fade curve: fade in at start, fade out at end.
        /// </summary>
        /// <returns>The opacity [0,1].</returns>
        private double CalculateOpacityOverLifetime()
        {
            // Fade in (0-0.2)
            if (_normalizedAge < 0.2)
            {
                return _normalizedAge / 0.2;
            }
            
            // Fade out (0.8-1.0)
            if (_normalizedAge > 0.8)
            {
                return 1.0 - ((_normalizedAge - 0.8) / 0.2);
            }
            
            // Full opacity (0.2-0.8)
            return 1.0;
        }
        
        /// <summary>
        /// Resets the particle for reuse.
        /// Does NOT change ParticleId - identity is managed by ParticlePool.
        /// Resets runtime state only.
        /// CurrentOpacity is initialized to 1.0 to match the fade curve's starting value
        /// (fade-in from 0 to 1 at the beginning of particle lifetime).
        /// </summary>
        public void ResetForReuse()
        {
            // Reset runtime state
            _age = 0;
            _normalizedAge = 0;
            _isExpired = false;
            _currentSize = _initialSize;
            _currentOpacity = 1.0; // Matches fade curve starting value (fade-in from 0 to 1)
            _currentScale = 1.0;
            
            // Reset position and velocity
            _x = 0;
            _y = 0;
            _velocityX = 0;
            _velocityY = 0;
            
            // Note: ParticleId is NOT reset here.
            // Identity is managed by ParticlePool and remains stable across reuse.
        }
        
        /// <summary>
        /// Validates the particle configuration.
        /// Lightweight validation suitable for engine code.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool Validate()
        {
            // Validate size
            if (double.IsNaN(_initialSize) || double.IsInfinity(_initialSize) || _initialSize < 0)
                return false;
            
            if (double.IsNaN(_finalSize) || double.IsInfinity(_finalSize) || _finalSize < 0)
                return false;
            
            // Validate lifetime
            if (double.IsNaN(_lifetime) || double.IsInfinity(_lifetime) || _lifetime < 0)
                return false;
            
            // Validate layer index
            if (_layerIndex < 0)
                return false;
            
            // Lightweight enum validation - check for non-negative values
            // Future enum expansion will not break this validation
            var blendModeValue = (int)_blendMode;
            if (blendModeValue < 0)
                return false;
            
            var contextValue = (int)_renderContext;
            if (contextValue < 0)
                return false;
            
            return true;
        }
    }
}
