using System;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Visual identity runtime context for Living Visual Identity Engine.
    /// Lightweight context object containing ownership and rendering information.
    /// Render-agnostic architecture.
    /// 
    /// Part of Phase 2.1 Runtime Integration implementation.
    /// Phase 2.1 Status: Foundation Lock Approved
    /// Do not modify unless a compiler error or real runtime bug is discovered.
    /// </summary>
    public readonly struct VisualIdentityRuntimeContext
    {
        /// <summary>
        /// Gets the visual render context.
        /// </summary>
        public readonly VisualRenderContext VisualRenderContext;
        
        /// <summary>
        /// Gets the owner ID (PlayerId or TeamId).
        /// </summary>
        public readonly string OwnerId;
        
        /// <summary>
        /// Gets the owner type.
        /// </summary>
        public readonly VisualOwnerType OwnerType;
        
        /// <summary>
        /// Gets the asset ID.
        /// </summary>
        public readonly string AssetId;
        
        /// <summary>
        /// Gets the effect ID.
        /// </summary>
        public readonly string EffectId;
        
        /// <summary>
        /// Gets whether this is a preview context.
        /// </summary>
        public readonly bool IsPreview;
        
        /// <summary>
        /// Gets whether this is a developer preview context.
        /// </summary>
        public readonly bool IsDeveloperPreview;
        
        /// <summary>
        /// Gets the current LOD level.
        /// </summary>
        public readonly LODLevel CurrentLOD;
        
        /// <summary>
        /// Gets the performance mode.
        /// </summary>
        public readonly PerformanceMode PerformanceMode;
        
        /// <summary>
        /// Creates a new visual identity runtime context.
        /// </summary>
        public VisualIdentityRuntimeContext(
            VisualRenderContext visualRenderContext,
            string ownerId,
            VisualOwnerType ownerType,
            string assetId = null,
            string effectId = null,
            bool isPreview = false,
            bool isDeveloperPreview = false,
            LODLevel currentLOD = LODLevel.Medium,
            PerformanceMode performanceMode = PerformanceMode.Medium)
        {
            VisualRenderContext = visualRenderContext;
            OwnerId = ownerId ?? string.Empty;
            OwnerType = ownerType;
            AssetId = assetId ?? string.Empty;
            EffectId = effectId ?? string.Empty;
            IsPreview = isPreview;
            IsDeveloperPreview = isDeveloperPreview;
            CurrentLOD = currentLOD;
            PerformanceMode = performanceMode;
        }
        
        /// <summary>
        /// Creates a player context.
        /// </summary>
        public static VisualIdentityRuntimeContext CreatePlayerContext(
            string playerId,
            VisualRenderContext visualRenderContext = VisualRenderContext.Store,
            string assetId = null,
            string effectId = null,
            bool isPreview = false)
        {
            return new VisualIdentityRuntimeContext(
                visualRenderContext,
                playerId,
                VisualOwnerType.Player,
                assetId,
                effectId,
                isPreview,
                false,
                LODManager.CurrentLOD,
                PerformanceManager.CurrentMode
            );
        }
        
        /// <summary>
        /// Creates a team context.
        /// </summary>
        public static VisualIdentityRuntimeContext CreateTeamContext(
            string teamId,
            VisualRenderContext visualRenderContext = VisualRenderContext.Store,
            string assetId = null,
            string effectId = null,
            bool isPreview = false)
        {
            return new VisualIdentityRuntimeContext(
                visualRenderContext,
                teamId,
                VisualOwnerType.Team,
                assetId,
                effectId,
                isPreview,
                false,
                LODManager.CurrentLOD,
                PerformanceManager.CurrentMode
            );
        }
        
        /// <summary>
        /// Creates a developer preview context.
        /// </summary>
        public static VisualIdentityRuntimeContext CreateDeveloperPreviewContext(
            string effectId,
            VisualRenderContext visualRenderContext = VisualRenderContext.Store)
        {
            return new VisualIdentityRuntimeContext(
                visualRenderContext,
                "developer",
                VisualOwnerType.System,
                null,
                effectId,
                true,
                true,
                LODManager.CurrentLOD,
                PerformanceManager.CurrentMode
            );
        }
        
        /// <summary>
        /// Validates the context data.
        /// Not a hot path - used for consistency checks.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool Validate()
        {
            // Validate VisualRenderContext
            if (!Enum.IsDefined(typeof(VisualRenderContext), VisualRenderContext))
                return false;
            
            // Validate VisualOwnerType
            if (!Enum.IsDefined(typeof(VisualOwnerType), OwnerType))
                return false;
            
            // Validate LODLevel
            if (!Enum.IsDefined(typeof(LODLevel), CurrentLOD))
                return false;
            
            // Validate PerformanceMode
            if (!Enum.IsDefined(typeof(PerformanceMode), PerformanceMode))
                return false;
            
            // OwnerId is required for Player and Team
            if (OwnerType == VisualOwnerType.Player || OwnerType == VisualOwnerType.Team)
            {
                if (string.IsNullOrWhiteSpace(OwnerId))
                    return false;
            }
            
            // EffectId is required for DeveloperPreview
            if (IsDeveloperPreview)
            {
                if (string.IsNullOrWhiteSpace(EffectId))
                    return false;
            }
            
            return true;
        }
    }
}
