using System;
using System.Collections.Generic;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Visual effect runtime registry for Living Visual Identity Engine.
    /// Manages active effect instances.
    /// No UI references. No layout references.
    /// 
    /// Allocation notes for Phase 2.1:
    /// - GetByContext() allocates a new List (diagnostic/query-only, not hot path)
    /// - CleanupExpired() allocates a List<int> (maintenance operation, not hot path)
    /// - GetContextKey() allocates strings (acceptable for Phase 2.1 context indexing)
    /// 
    /// Part of Phase 2.1 Runtime Integration implementation.
    /// Phase 2.1 Status: Ready for Review
    /// </summary>
    public static class VisualEffectRuntimeRegistry
    {
        private static readonly Dictionary<int, VisualEffectInstance> _instances;
        private static readonly Dictionary<string, List<VisualEffectInstance>> _instancesByOwner;
        private static readonly Dictionary<string, List<VisualEffectInstance>> _instancesByContext;
        private static readonly object _lock;
        
        // Diagnostics counters
        private static int _ownerCount;
        private static int _contextCount;
        private static int _maxInstancesPerOwner;
        private static int _cleanupCount;
        
        static VisualEffectRuntimeRegistry()
        {
            _instances = new Dictionary<int, VisualEffectInstance>();
            _instancesByOwner = new Dictionary<string, List<VisualEffectInstance>>(StringComparer.OrdinalIgnoreCase);
            _instancesByContext = new Dictionary<string, List<VisualEffectInstance>>(StringComparer.OrdinalIgnoreCase);
            _lock = new object();
            _ownerCount = 0;
            _contextCount = 0;
            _maxInstancesPerOwner = 0;
            _cleanupCount = 0;
        }
        
        /// <summary>
        /// Gets the number of active instances.
        /// </summary>
        public static int ActiveInstanceCount => _instances.Count;
        
        /// <summary>
        /// Gets the number of unique owners.
        /// </summary>
        public static int OwnerCount => _ownerCount;
        
        /// <summary>
        /// Gets the number of unique contexts.
        /// </summary>
        public static int ContextCount => _contextCount;
        
        /// <summary>
        /// Gets the maximum instances per owner.
        /// </summary>
        public static int MaxInstancesPerOwner => _maxInstancesPerOwner;
        
        /// <summary>
        /// Gets the total cleanup count.
        /// </summary>
        public static int CleanupCount => _cleanupCount;
        
        /// <summary>
        /// Registers an effect instance.
        /// </summary>
        /// <param name="instance">The instance to register.</param>
        /// <returns>True if registered successfully.</returns>
        public static bool RegisterInstance(VisualEffectInstance instance)
        {
            if (instance == null)
                return false;
            
            lock (_lock)
            {
                if (_instances.ContainsKey(instance.InstanceId))
                    return false;
                
                // Add to owner index (supports multiple instances per owner)
                if (!_instancesByOwner.ContainsKey(instance.OwnerId))
                {
                    _instancesByOwner[instance.OwnerId] = new List<VisualEffectInstance>();
                    _ownerCount++;
                }
                
                _instances[instance.InstanceId] = instance;
                _instancesByOwner[instance.OwnerId].Add(instance);
                
                // Update max instances per owner
                if (_instancesByOwner[instance.OwnerId].Count > _maxInstancesPerOwner)
                    _maxInstancesPerOwner = _instancesByOwner[instance.OwnerId].Count;
                
                // Index by context
                var contextKey = GetContextKey(instance.Context);
                if (!_instancesByContext.ContainsKey(contextKey))
                {
                    _instancesByContext[contextKey] = new List<VisualEffectInstance>();
                    _contextCount++;
                }
                _instancesByContext[contextKey].Add(instance);
                
                return true;
            }
        }
        
        /// <summary>
        /// Unregisters an effect instance.
        /// </summary>
        /// <param name="instanceId">The instance ID.</param>
        /// <returns>True if unregistered successfully.</returns>
        public static bool UnregisterInstance(int instanceId)
        {
            lock (_lock)
            {
                return UnregisterInstanceInternal(instanceId);
            }
        }
        
        /// <summary>
        /// Internal unregister method - assumes lock is already held.
        /// </summary>
        private static bool UnregisterInstanceInternal(int instanceId)
        {
            if (!_instances.TryGetValue(instanceId, out var instance))
                return false;
            
            _instances.Remove(instanceId);
            
            // Remove from owner index
            if (_instancesByOwner.TryGetValue(instance.OwnerId, out var ownerList))
            {
                ownerList.Remove(instance);
                if (ownerList.Count == 0)
                {
                    _instancesByOwner.Remove(instance.OwnerId);
                    _ownerCount--;
                }
            }
                
            // Remove from context index
            var contextKey = GetContextKey(instance.Context);
            if (_instancesByContext.TryGetValue(contextKey, out var contextList))
            {
                contextList.Remove(instance);
                if (contextList.Count == 0)
                {
                    _instancesByContext.Remove(contextKey);
                    _contextCount--;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets an instance by ID.
        /// </summary>
        /// <param name="instanceId">The instance ID.</param>
        /// <returns>The instance, or null if not found.</returns>
        public static VisualEffectInstance GetInstance(int instanceId)
        {
            lock (_lock)
            {
                return _instances.TryGetValue(instanceId, out var instance) ? instance : null;
            }
        }
        
        /// <summary>
        /// Gets instances by owner.
        /// Diagnostic/query-only method - allocates a new List.
        /// </summary>
        /// <param name="ownerId">The owner ID.</param>
        /// <returns>List of instances for the owner, or empty list if not found.</returns>
        public static List<VisualEffectInstance> GetByOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return new List<VisualEffectInstance>();
            
            lock (_lock)
            {
                if (_instancesByOwner.TryGetValue(ownerId, out var instances))
                {
                    return new List<VisualEffectInstance>(instances);
                }
                return new List<VisualEffectInstance>();
            }
        }
        
        /// <summary>
        /// Gets instances by context.
        /// Diagnostic/query-only method - allocates a new List.
        /// </summary>
        /// <param name="context">The runtime context.</param>
        /// <returns>List of instances matching the context.</returns>
        public static List<VisualEffectInstance> GetByContext(VisualIdentityRuntimeContext context)
        {
            var contextKey = GetContextKey(context);
            
            lock (_lock)
            {
                if (_instancesByContext.TryGetValue(contextKey, out var instances))
                {
                    return new List<VisualEffectInstance>(instances);
                }
                return new List<VisualEffectInstance>();
            }
        }
        
        /// <summary>
        /// Updates all active instances.
        /// Zero allocations in hot path.
        /// Does not hold lock while calling instance.Update().
        /// 
        /// Phase 2.1 Note: Currently allocates a snapshot array per frame.
        /// Accepted for Phase 2.1 as instance count is expected to be small.
        /// Will be replaced with reusable buffer in Phase 2.2.
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        public static void UpdateAll(double deltaTime)
        {
            // Validate deltaTime
            if (double.IsNaN(deltaTime) || deltaTime < 0)
                deltaTime = 0;
            
            // Snapshot instances to avoid holding lock during Update
            VisualEffectInstance[] instancesSnapshot;
            lock (_lock)
            {
                instancesSnapshot = new VisualEffectInstance[_instances.Count];
                _instances.Values.CopyTo(instancesSnapshot, 0);
            }
            
            // Update instances outside lock
            foreach (var instance in instancesSnapshot)
            {
                instance.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// Cleans up expired instances.
        /// Maintenance operation - allocates a List<int>.
        /// </summary>
        /// <param name="maxLifetime">Maximum lifetime in seconds.</param>
        /// <returns>Number of instances cleaned up.</returns>
        public static int CleanupExpired(double maxLifetime = 300.0)
        {
            int cleaned = 0;
            var toRemove = new List<int>();
            
            lock (_lock)
            {
                foreach (var kvp in _instances)
                {
                    if (!kvp.Value.IsActive || kvp.Value.ElapsedTime > maxLifetime)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                
                // Use internal unregister to avoid nested lock
                foreach (var instanceId in toRemove)
                {
                    if (UnregisterInstanceInternal(instanceId))
                    {
                        cleaned++;
                        _cleanupCount++;
                    }
                }
            }
            
            return cleaned;
        }
        
        /// <summary>
        /// Clears all instances.
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                foreach (var instance in _instances.Values)
                {
                    instance.Stop();
                }
                
                _instances.Clear();
                _instancesByOwner.Clear();
                _instancesByContext.Clear();
                
                // Reset diagnostics counters
                _ownerCount = 0;
                _contextCount = 0;
                _maxInstancesPerOwner = 0;
            }
        }
        
        /// <summary>
        /// Gets a context key for indexing.
        /// 
        /// Phase 2.1 Note: Currently allocates interpolated strings.
        /// Accepted for Phase 2.1 as context indexing is not a hot path.
        /// Will be replaced with lightweight ContextKey struct in Phase 2.2.
        /// </summary>
        private static string GetContextKey(VisualIdentityRuntimeContext context)
        {
            return $"{context.OwnerType}_{context.OwnerId}_{context.AssetId}_{context.EffectId}";
        }
    }
}
