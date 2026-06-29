using System;
using System.Collections.Generic;
using DominoMajlisPRO.GalleryEngine.VisualIdentity.Particles;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Visual effect instance for Living Visual Identity Engine.
    /// Runtime instance model for one active effect.
    /// Render-agnostic architecture.
    /// Particle ownership list is small and not a heavy runtime path for Phase 2.1.
    /// Instance ID wraps around on overflow (documented for Phase 2.1).
    /// 
    /// Part of Phase 2.1 Runtime Integration implementation.
    /// Phase 2.1 Status: Ready for Review
    /// </summary>
    public class VisualEffectInstance
    {
        private static int _nextInstanceId = 1;
        
        private readonly int _instanceId;
        private readonly string _effectDefinitionId;
        private readonly string _ownerId;
        private readonly VisualOwnerType _ownerType;
        private readonly VisualIdentityRuntimeContext _context;
        private readonly EffectStateMachine _stateMachine;
        private readonly AnimationTimeline _timeline;
        private readonly List<int> _particleIds;
        
        private bool _isActive;
        private bool _isPaused;
        private double _elapsedTime;
        
        /// <summary>
        /// Gets the effect instance ID.
        /// </summary>
        public int InstanceId => _instanceId;
        
        /// <summary>
        /// Gets the effect definition ID.
        /// </summary>
        public string EffectDefinitionId => _effectDefinitionId;
        
        /// <summary>
        /// Gets the owner ID.
        /// </summary>
        public string OwnerId => _ownerId;
        
        /// <summary>
        /// Gets the owner type.
        /// </summary>
        public VisualOwnerType OwnerType => _ownerType;
        
        /// <summary>
        /// Gets the runtime context.
        /// </summary>
        public VisualIdentityRuntimeContext Context => _context;
        
        /// <summary>
        /// Gets the effect state machine.
        /// </summary>
        public EffectStateMachine StateMachine => _stateMachine;
        
        /// <summary>
        /// Gets the animation timeline.
        /// </summary>
        public AnimationTimeline Timeline => _timeline;
        
        /// <summary>
        /// Gets the particle IDs owned by this instance.
        /// </summary>
        public IReadOnlyList<int> ParticleIds => _particleIds;
        
        /// <summary>
        /// Gets whether the instance is active.
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// Gets whether the instance is paused.
        /// </summary>
        public bool IsPaused => _isPaused;
        
        /// <summary>
        /// Gets the elapsed time in seconds.
        /// </summary>
        public double ElapsedTime => _elapsedTime;
        
        /// <summary>
        /// Creates a new visual effect instance.
        /// </summary>
        public VisualEffectInstance(
            string effectDefinitionId,
            string ownerId,
            VisualOwnerType ownerType,
            VisualIdentityRuntimeContext context)
        {
            // Instance ID assignment: wraps around on overflow.
            // For Phase 2.1, this is acceptable as instance count is small.
            // Future Phase may implement safer ID generation.
            _instanceId = _nextInstanceId++;
            if (_nextInstanceId <= 0)
                _nextInstanceId = 1;
            
            _effectDefinitionId = effectDefinitionId ?? string.Empty;
            _ownerId = ownerId ?? string.Empty;
            _ownerType = ownerType;
            _context = context;
            _stateMachine = new EffectStateMachine();
            _timeline = new AnimationTimeline();
            _particleIds = new List<int>();
            
            _isActive = false;
            _isPaused = false;
            _elapsedTime = 0.0;
        }
        
        /// <summary>
        /// Starts the effect instance.
        /// </summary>
        /// <returns>True if started successfully.</returns>
        public bool Start()
        {
            if (_isActive)
                return false;
            
            _isActive = true;
            _isPaused = false;
            _elapsedTime = 0.0;
            
            _stateMachine.Start("EffectInstance.Start");
            _timeline.Play();
            
            return true;
        }
        
        /// <summary>
        /// Stops the effect instance.
        /// </summary>
        /// <returns>True if stopped successfully.</returns>
        public bool Stop()
        {
            if (!_isActive)
                return false;
            
            _isActive = false;
            _isPaused = false;
            
            _stateMachine.Stop("EffectInstance.Stop");
            _timeline.Stop();
            
            // Release particles using ParticlePool API
            foreach (var particleId in _particleIds)
            {
                ParticlePool.ReleaseById(particleId);
            }
            _particleIds.Clear();
            
            return true;
        }
        
        /// <summary>
        /// Pauses the effect instance.
        /// </summary>
        /// <returns>True if paused successfully.</returns>
        public bool Pause()
        {
            if (!_isActive || _isPaused)
                return false;
            
            _isPaused = true;
            _timeline.Pause();
            
            return true;
        }
        
        /// <summary>
        /// Resumes the effect instance.
        /// </summary>
        /// <returns>True if resumed successfully.</returns>
        public bool Resume()
        {
            if (!_isActive || !_isPaused)
                return false;
            
            _isPaused = false;
            _timeline.Resume();
            
            return true;
        }
        
        /// <summary>
        /// Updates the effect instance.
        /// Zero allocations in hot path.
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        public void Update(double deltaTime)
        {
            if (!_isActive || _isPaused)
                return;
            
            // Validate deltaTime
            if (double.IsNaN(deltaTime) || deltaTime < 0)
                deltaTime = 0;
            
            _elapsedTime += deltaTime;
            
            // Update state machine
            _stateMachine.Update(deltaTime);
            
            // Update timeline
            _timeline.Update(deltaTime);
            
            // Update particles using ParticlePool API
            // Particle ownership list is small for Phase 2.1, not a heavy runtime path
            foreach (var particleId in _particleIds)
            {
                var particle = ParticlePool.GetActiveParticle(particleId);
                if (particle != null)
                {
                    particle.Update(deltaTime);
                }
            }
        }
        
        /// <summary>
        /// Adds a particle to this instance.
        /// Particle ownership list is small for Phase 2.1, not a heavy runtime path.
        /// </summary>
        /// <param name="particleId">The particle ID.</param>
        public void AddParticle(int particleId)
        {
            if (_particleIds.Contains(particleId))
                return;
            
            _particleIds.Add(particleId);
        }
        
        /// <summary>
        /// Removes a particle from this instance.
        /// Particle ownership list is small for Phase 2.1, not a heavy runtime path.
        /// </summary>
        /// <param name="particleId">The particle ID.</param>
        public void RemoveParticle(int particleId)
        {
            _particleIds.Remove(particleId);
        }
        
        /// <summary>
        /// Gets diagnostics for this instance.
        /// Allocation-safe: returns a struct.
        /// </summary>
        /// <returns>Effect instance diagnostics.</returns>
        public VisualEffectInstanceDiagnostics GetDiagnostics()
        {
            return new VisualEffectInstanceDiagnostics(
                _instanceId,
                _effectDefinitionId,
                _ownerId,
                _ownerType,
                _stateMachine.CurrentState,
                _stateMachine.StateElapsedTime,
                _timeline.State,
                _timeline.CurrentTime,
                _particleIds.Count,
                _isActive,
                _isPaused,
                _elapsedTime
            );
        }
    }
    
    /// <summary>
    /// Visual effect instance diagnostics information.
    /// Readonly struct for allocation-safe diagnostics reporting.
    /// </summary>
    public readonly struct VisualEffectInstanceDiagnostics
    {
        public readonly int InstanceId;
        public readonly string EffectDefinitionId;
        public readonly string OwnerId;
        public readonly VisualOwnerType OwnerType;
        public readonly EffectState CurrentState;
        public readonly double StateElapsedTime;
        public readonly TimelineState TimelineState;
        public readonly double TimelineTime;
        public readonly int ParticleCount;
        public readonly bool IsActive;
        public readonly bool IsPaused;
        public readonly double ElapsedTime;
        
        public VisualEffectInstanceDiagnostics(
            int instanceId,
            string effectDefinitionId,
            string ownerId,
            VisualOwnerType ownerType,
            EffectState currentState,
            double stateElapsedTime,
            TimelineState timelineState,
            double timelineTime,
            int particleCount,
            bool isActive,
            bool isPaused,
            double elapsedTime)
        {
            InstanceId = instanceId;
            EffectDefinitionId = effectDefinitionId;
            OwnerId = ownerId;
            OwnerType = ownerType;
            CurrentState = currentState;
            StateElapsedTime = stateElapsedTime;
            TimelineState = timelineState;
            TimelineTime = timelineTime;
            ParticleCount = particleCount;
            IsActive = isActive;
            IsPaused = isPaused;
            ElapsedTime = elapsedTime;
        }
    }
}
