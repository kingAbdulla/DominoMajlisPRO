using System;
using System.Collections.Generic;
using DominoMajlisPRO.GalleryEngine.Models;
using DominoMajlisPRO.GalleryEngine.Services;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Visual Identity Resolver for Living Visual Identity Engine.
    /// Resolves visual identity requests to EffectDefinitionModel.
    /// Routes to existing Effects Engine v2 components.
    /// Caches resolved identities for performance.
    /// 
    /// Part of Phase 2.2 Stage A implementation.
    /// Phase 2.2 Status: Stage A - Runtime Bridge
    /// </summary>
    public static class VisualIdentityResolver
    {
        private static readonly Dictionary<string, EffectDefinitionModel> _playerEffectCache;
        private static readonly Dictionary<string, EffectDefinitionModel> _teamEffectCache;
        private static readonly object _cacheLock;
        private static int _maxCacheSize;
        private static long _cacheHits;
        private static long _cacheMisses;
        private static long _resolutionFailures;
        
        private const int DefaultMaxCacheSize = 100;
        
        static VisualIdentityResolver()
        {
            _playerEffectCache = new Dictionary<string, EffectDefinitionModel>();
            _teamEffectCache = new Dictionary<string, EffectDefinitionModel>();
            _cacheLock = new object();
            _maxCacheSize = DefaultMaxCacheSize;
            _cacheHits = 0;
            _cacheMisses = 0;
            _resolutionFailures = 0;
        }
        
        /// <summary>
        /// Gets or sets the maximum cache size.
        /// </summary>
        public static int MaxCacheSize
        {
            get => _maxCacheSize;
            set => _maxCacheSize = Math.Max(10, value);
        }
        
        /// <summary>
        /// Gets the cache hit count.
        /// </summary>
        public static long CacheHits => _cacheHits;
        
        /// <summary>
        /// Gets the cache miss count.
        /// </summary>
        public static long CacheMisses => _cacheMisses;
        
        /// <summary>
        /// Gets the cache hit ratio.
        /// </summary>
        public static double CacheHitRatio => _cacheHits + _cacheMisses > 0 ? (double)_cacheHits / (_cacheHits + _cacheMisses) : 0.0;
        
        /// <summary>
        /// Gets the resolution failure count.
        /// </summary>
        public static long ResolutionFailures => _resolutionFailures;
        
        /// <summary>
        /// Resolves a player visual identity to EffectDefinitionModel.
        /// Uses the approved pipeline: RuntimeContext → Equipped Identity (CatalogAssetDisplay) → PlayerEffectEngine.CreateDefinition.
        /// </summary>
        /// <param name="context">The runtime context.</param>
        /// <param name="equippedIdentity">The equipped identity data (must be CatalogAssetDisplay).</param>
        /// <param name="baseScale">The base scale for rendering.</param>
        /// <returns>EffectDefinitionModel or null if resolution fails.</returns>
        public static EffectDefinitionModel? ResolvePlayerEffect(VisualIdentityRuntimeContext context, object equippedIdentity, double baseScale = 1.18)
        {
            if (!context.Validate())
            {
                _resolutionFailures++;
                return null;
            }
            
            if (equippedIdentity == null)
            {
                _resolutionFailures++;
                return null;
            }
            
            // Check if equippedIdentity is CatalogAssetDisplay
            if (equippedIdentity is not CatalogAssetDisplay catalogAsset)
            {
                _resolutionFailures++;
                return null;
            }
            
            // Cache key based on full runtime identity for safe invalidation
            var cacheKey = BuildRuntimeCacheKey(context, catalogAsset);
            
            lock (_cacheLock)
            {
                if (_playerEffectCache.TryGetValue(cacheKey, out var cached))
                {
                    _cacheHits++;
                    return cached;
                }
            }
            
            _cacheMisses++;
            
            // Resolve through approved pipeline using existing PlayerEffectEngine
            try
            {
                var definition = PlayerEffectEngine.CreateDefinition(catalogAsset, baseScale);
                
                lock (_cacheLock)
                {
                    // Enforce cache size limit
                    if (_playerEffectCache.Count >= _maxCacheSize)
                    {
                        var firstKey = default(string);
                        foreach (var key in _playerEffectCache.Keys)
                        {
                            firstKey = key;
                            break;
                        }
                        if (firstKey != null)
                            _playerEffectCache.Remove(firstKey);
                    }
                    
                    _playerEffectCache[cacheKey] = definition;
                }
                
                return definition;
            }
            catch
            {
                _resolutionFailures++;
                return null;
            }
        }
        
        /// <summary>
        /// Resolves a team visual identity to EffectDefinitionModel.
        /// Uses the approved pipeline: RuntimeContext → Equipped Identity (CatalogAssetDisplay) → TeamEffectEngine.
        /// Note: TeamEffectEngine integration is pending - returns null for Stage A.
        /// </summary>
        /// <param name="context">The runtime context.</param>
        /// <param name="equippedIdentity">The equipped identity data (must be CatalogAssetDisplay).</param>
        /// <param name="baseScale">The base scale for rendering.</param>
        /// <returns>EffectDefinitionModel or null if resolution fails.</returns>
        public static EffectDefinitionModel? ResolveTeamEffect(VisualIdentityRuntimeContext context, object equippedIdentity, double baseScale = 1.18)
        {
            if (!context.Validate())
            {
                _resolutionFailures++;
                return null;
            }
            
            if (equippedIdentity == null)
            {
                _resolutionFailures++;
                return null;
            }
            
            // Check if equippedIdentity is CatalogAssetDisplay
            if (equippedIdentity is not CatalogAssetDisplay catalogAsset)
            {
                _resolutionFailures++;
                return null;
            }
            
            // Cache key based on full runtime identity for safe invalidation
            var cacheKey = BuildRuntimeCacheKey(context, catalogAsset);
            
            lock (_cacheLock)
            {
                if (_teamEffectCache.TryGetValue(cacheKey, out var cached))
                {
                    _cacheHits++;
                    return cached;
                }
            }
            
            _cacheMisses++;
            
            // TeamEffectEngine integration is pending for Stage A
            // Returns null with explicit diagnostics
            _resolutionFailures++;
            return null;
        }
        
        /// <summary>
        /// Builds a cache key based on full runtime identity.
        /// This allows safe invalidation when RuntimeProfile or DeviceProfiler changes.
        /// </summary>
        /// <param name="context">The runtime context.</param>
        /// <param name="catalogAsset">The catalog asset.</param>
        /// <returns>Cache key string.</returns>
        private static string BuildRuntimeCacheKey(VisualIdentityRuntimeContext context, CatalogAssetDisplay catalogAsset)
        {
            // Include all runtime-relevant factors in cache key
            return $"{context.OwnerType}_{context.OwnerId}_{catalogAsset.AssetId}_{context.EffectId}_{context.CurrentLOD}_{context.PerformanceMode}_{context.VisualRenderContext}";
        }
        
        /// <summary>
        /// Invalidates cached entries for a specific player.
        /// </summary>
        /// <param name="playerId">The player ID.</param>
        public static void InvalidatePlayerCache(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;
            
            lock (_cacheLock)
            {
                var keysToRemove = new List<string>();
                foreach (var key in _playerEffectCache.Keys)
                {
                    if (key.StartsWith($"Player_{playerId}_"))
                        keysToRemove.Add(key);
                }
                
                foreach (var key in keysToRemove)
                    _playerEffectCache.Remove(key);
            }
        }
        
        /// <summary>
        /// Invalidates cached entries for a specific team.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        public static void InvalidateTeamCache(string teamId)
        {
            if (string.IsNullOrWhiteSpace(teamId))
                return;
            
            lock (_cacheLock)
            {
                var keysToRemove = new List<string>();
                foreach (var key in _teamEffectCache.Keys)
                {
                    if (key.StartsWith($"Team_{teamId}_"))
                        keysToRemove.Add(key);
                }
                
                foreach (var key in keysToRemove)
                    _teamEffectCache.Remove(key);
            }
        }
        
        /// <summary>
        /// Invalidates cached entries for a specific runtime context.
        /// This allows safe invalidation when RuntimeProfile or DeviceProfiler changes.
        /// </summary>
        /// <param name="context">The runtime context.</param>
        public static void InvalidateContextCache(VisualIdentityRuntimeContext context)
        {
            if (!context.Validate())
                return;
            
            lock (_cacheLock)
            {
                var keysToRemove = new List<string>();
                
                foreach (var key in _playerEffectCache.Keys)
                {
                    if (key.Contains($"_{context.CurrentLOD}_") && key.Contains($"_{context.PerformanceMode}_"))
                        keysToRemove.Add(key);
                }
                
                foreach (var key in keysToRemove)
                    _playerEffectCache.Remove(key);
                
                keysToRemove.Clear();
                
                foreach (var key in _teamEffectCache.Keys)
                {
                    if (key.Contains($"_{context.CurrentLOD}_") && key.Contains($"_{context.PerformanceMode}_"))
                        keysToRemove.Add(key);
                }
                
                foreach (var key in keysToRemove)
                    _teamEffectCache.Remove(key);
            }
        }
        
        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _playerEffectCache.Clear();
                _teamEffectCache.Clear();
                _cacheHits = 0;
                _cacheMisses = 0;
                _resolutionFailures = 0;
            }
        }
        
        /// <summary>
        /// Gets cache diagnostics.
        /// </summary>
        /// <returns>Cache diagnostics snapshot.</returns>
        public static VisualIdentityResolverDiagnostics GetDiagnostics()
        {
            lock (_cacheLock)
            {
                return new VisualIdentityResolverDiagnostics(
                    _playerEffectCache.Count,
                    _teamEffectCache.Count,
                    _maxCacheSize,
                    _cacheHits,
                    _cacheMisses,
                    _resolutionFailures,
                    CacheHitRatio
                );
            }
        }
    }
    
    /// <summary>
    /// Visual Identity Resolver diagnostics snapshot.
    /// Immutable readonly struct for allocation-safe diagnostics.
    /// </summary>
    public readonly struct VisualIdentityResolverDiagnostics
    {
        public readonly int PlayerCacheCount;
        public readonly int TeamCacheCount;
        public readonly int MaxCacheSize;
        public readonly long CacheHits;
        public readonly long CacheMisses;
        public readonly long ResolutionFailures;
        public readonly double CacheHitRatio;
        
        public VisualIdentityResolverDiagnostics(
            int playerCacheCount,
            int teamCacheCount,
            int maxCacheSize,
            long cacheHits,
            long cacheMisses,
            long resolutionFailures,
            double cacheHitRatio)
        {
            PlayerCacheCount = playerCacheCount;
            TeamCacheCount = teamCacheCount;
            MaxCacheSize = maxCacheSize;
            CacheHits = cacheHits;
            CacheMisses = cacheMisses;
            ResolutionFailures = resolutionFailures;
            CacheHitRatio = cacheHitRatio;
        }
    }
}
