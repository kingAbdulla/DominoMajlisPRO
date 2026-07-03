using System;
using System.Collections.Generic;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity.Particles
{
    /// <summary>
    /// Object pool for Particle instances.
    /// Zero allocations in hot paths.
    /// Manages particle lifecycle with integer-based stable identifiers.
    /// Render-agnostic architecture.
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public static class ParticlePool
    {
        private static readonly Stack<Particle> _availableParticles;
        private static readonly List<Particle> _activeParticles;
        private static readonly Dictionary<int, Particle> _activeParticleMap;
        private static readonly Dictionary<int, int> _activeParticleIndexMap; // Maps particle ID to index in active list for O(1) removal
        private static int _nextParticleId;
        private static int _poolSize;
        private static int _maxPoolSize;
        private static int _activeCount;
        
        // Constants for pool management
        private const int DefaultInitialPoolSize = 100;
        private const int DefaultMaxPoolSize = 1000;
        private const int PoolGrowthSize = 50;
        
        // Overflow policy
        private enum OverflowPolicy
        {
            RejectAcquire,      // Return null when pool is exhausted
            RecycleFirstActive  // Recycle the first active particle (not necessarily oldest by lifetime)
        }
        
        private static OverflowPolicy _overflowPolicy = OverflowPolicy.RejectAcquire;
        
        static ParticlePool()
        {
            _availableParticles = new Stack<Particle>(DefaultInitialPoolSize);
            _activeParticles = new List<Particle>(DefaultInitialPoolSize);
            _activeParticleMap = new Dictionary<int, Particle>(DefaultInitialPoolSize);
            _activeParticleIndexMap = new Dictionary<int, int>(DefaultInitialPoolSize);
            _nextParticleId = 1;
            _poolSize = 0;
            _maxPoolSize = DefaultMaxPoolSize;
            _activeCount = 0;
            
            // Pre-allocate initial pool
            GrowPool(DefaultInitialPoolSize);
        }
        
        /// <summary>
        /// Gets the current number of active particles.
        /// </summary>
        public static int ActiveCount => _activeCount;
        
        /// <summary>
        /// Gets the current pool size.
        /// </summary>
        public static int PoolSize => _poolSize;
        
        /// <summary>
        /// Gets the maximum pool size.
        /// </summary>
        public static int MaxPoolSize => _maxPoolSize;
        
        /// <summary>
        /// Gets the number of available particles in the pool.
        /// </summary>
        public static int AvailableCount => _availableParticles.Count;
        
        /// <summary>
        /// Acquires a particle from the pool.
        /// Zero allocations when pool has available particles.
        /// Returns null if pool is exhausted and overflow policy is RejectAcquire.
        /// </summary>
        /// <returns>A particle instance with a unique ID, or null if pool exhausted.</returns>
        public static Particle Acquire()
        {
            Particle particle;
            
            if (_availableParticles.Count > 0)
            {
                // Reuse existing particle
                particle = _availableParticles.Pop();
            }
            else
            {
                // Handle overflow based on policy
                if (_overflowPolicy == OverflowPolicy.RejectAcquire)
                {
                    // Reject acquire when pool is exhausted
                    return null;
                }
                else if (_overflowPolicy == OverflowPolicy.RecycleFirstActive)
                {
                    // Recycle first active particle (not necessarily oldest by lifetime)
                    if (_activeParticles.Count > 0)
                    {
                        particle = _activeParticles[0];
                        Release(particle);
                        particle = _availableParticles.Pop();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            
            // Assign stable integer ID (first particle receives ID = 1)
            int particleId = _nextParticleId;
            particle.SetParticleId(particleId);
            _nextParticleId++;
            if (_nextParticleId <= 0)
                _nextParticleId = 1; // Wrap around on overflow
            
            // Track active particle
            int index = _activeParticles.Count;
            _activeParticles.Add(particle);
            _activeParticleMap[particleId] = particle;
            _activeParticleIndexMap[particleId] = index;
            _activeCount++;
            
            return particle;
        }
        
        /// <summary>
        /// Releases a particle back to the pool.
        /// Zero allocations. Uses O(1) swap-remove for active particles list via index table.
        /// </summary>
        /// <param name="particle">The particle to release.</param>
        public static void Release(Particle particle)
        {
            if (particle == null)
                return;
            
            int particleId = particle.ParticleId;
            
            // Remove from active tracking using O(1) swap-remove via index table
            if (_activeParticleMap.TryGetValue(particleId, out var trackedParticle) && trackedParticle == particle)
            {
                _activeParticleMap.Remove(particleId);
                
                // Get index from index table (O(1))
                if (_activeParticleIndexMap.TryGetValue(particleId, out int index))
                {
                    // Swap with last element and remove (O(1))
                    int lastIndex = _activeParticles.Count - 1;
                    if (index != lastIndex)
                    {
                        _activeParticles[index] = _activeParticles[lastIndex];
                        // Update index for the swapped particle
                        int swappedParticleId = _activeParticles[index].ParticleId;
                        _activeParticleIndexMap[swappedParticleId] = index;
                    }
                    _activeParticles.RemoveAt(lastIndex);
                    _activeParticleIndexMap.Remove(particleId);
                }
                
                _activeCount--;
            }
            
            // Reset particle state for reuse
            particle.ResetForReuse();
            
            // Return to pool if under max size
            if (_poolSize < _maxPoolSize)
            {
                _availableParticles.Push(particle);
            }
            // If over max size, let particle be garbage collected
        }
        
        /// <summary>
        /// Releases a particle by ID.
        /// Zero allocations.
        /// </summary>
        /// <param name="particleId">The particle ID to release.</param>
        public static void ReleaseById(int particleId)
        {
            if (_activeParticleMap.TryGetValue(particleId, out var particle))
            {
                Release(particle);
            }
        }
        
        /// <summary>
        /// Gets an active particle by ID.
        /// Zero allocations.
        /// </summary>
        /// <param name="particleId">The particle ID.</param>
        /// <returns>The particle, or null if not found.</returns>
        public static Particle GetActiveParticle(int particleId)
        {
            _activeParticleMap.TryGetValue(particleId, out var particle);
            return particle;
        }
        
        /// <summary>
        /// Updates all active particles.
        /// Zero allocations.
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        public static void UpdateActiveParticles(double deltaTime)
        {
            // Update in reverse order to safely remove expired particles
            for (int i = _activeParticles.Count - 1; i >= 0; i--)
            {
                var particle = _activeParticles[i];
                particle.Update(deltaTime);
                
                if (particle.IsExpired)
                {
                    Release(particle);
                }
            }
        }
        
        /// <summary>
        /// Gets all active particles.
        /// Returns the internal list for zero-allocation access.
        /// Do not modify the returned list.
        /// </summary>
        /// <returns>Read-only access to active particles list.</returns>
        public static IReadOnlyList<Particle> GetActiveParticles()
        {
            return _activeParticles;
        }
        
        /// <summary>
        /// Clears all active particles and returns them to the pool.
        /// Zero allocations.
        /// </summary>
        public static void Clear()
        {
            // Release all active particles
            for (int i = _activeParticles.Count - 1; i >= 0; i--)
            {
                var particle = _activeParticles[i];
                particle.ResetForReuse();
                
                if (_poolSize < _maxPoolSize)
                {
                    _availableParticles.Push(particle);
                }
            }
            
            _activeParticles.Clear();
            _activeParticleMap.Clear();
            _activeParticleIndexMap.Clear();
            _activeCount = 0;
        }
        
        /// <summary>
        /// Sets the maximum pool size.
        /// </summary>
        /// <param name="maxSize">The new maximum pool size.</param>
        public static void SetMaxPoolSize(int maxSize)
        {
            if (maxSize > 0)
                _maxPoolSize = maxSize;
        }
        
        /// <summary>
        /// Pre-allocates particles to the specified pool size.
        /// </summary>
        /// <param name="targetSize">The target pool size.</param>
        public static void PreAllocate(int targetSize)
        {
            if (targetSize <= _poolSize)
                return;
            
            int growth = Math.Min(targetSize - _poolSize, _maxPoolSize - _poolSize);
            if (growth > 0)
            {
                GrowPool(growth);
            }
        }
        
        /// <summary>
        /// Grows the pool by the specified amount.
        /// Zero allocations per particle after initial growth.
        /// </summary>
        /// <param name="count">Number of particles to add.</param>
        private static void GrowPool(int count)
        {
            int actualGrowth = Math.Min(count, _maxPoolSize - _poolSize);
            
            for (int i = 0; i < actualGrowth; i++)
            {
                var particle = new Particle();
                _availableParticles.Push(particle);
                _poolSize++;
            }
        }
        
        /// <summary>
        /// Resets the pool to initial state.
        /// Clears all active particles and resets pool size.
        /// </summary>
        public static void Reset()
        {
            Clear();
            
            _availableParticles.Clear();
            _poolSize = 0;
            _nextParticleId = 1;
            
            // Re-initialize with default pool size
            GrowPool(DefaultInitialPoolSize);
        }
        
        /// <summary>
        /// Validates the pool state.
        /// Diagnostic/debug utility only - not executed in any runtime hot path.
        /// </summary>
        /// <returns>True if pool state is valid, false otherwise.</returns>
        public static bool Validate()
        {
            // Check counts consistency
            if (_activeCount != _activeParticles.Count)
                return false;
            
            if (_activeCount != _activeParticleMap.Count)
                return false;
            
            if (_activeCount != _activeParticleIndexMap.Count)
                return false;
            
            // Check for duplicate IDs
            var idSet = new HashSet<int>();
            foreach (var particle in _activeParticles)
            {
                if (particle.ParticleId <= 0)
                    return false;
                
                if (idSet.Contains(particle.ParticleId))
                    return false;
                
                idSet.Add(particle.ParticleId);
            }
            
            // Check index map consistency
            for (int i = 0; i < _activeParticles.Count; i++)
            {
                int particleId = _activeParticles[i].ParticleId;
                if (!_activeParticleIndexMap.TryGetValue(particleId, out int index))
                    return false;
                
                if (index != i)
                    return false;
            }
            
            // Check pool size consistency
            if (_poolSize < _activeCount)
                return false;
            
            if (_poolSize > _maxPoolSize)
                return false;
            
            return true;
        }
    }
}
