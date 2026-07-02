namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Centralized constant payload key names for Living Visual Identity Engine VisualEventBus events.
    /// Used to ensure consistency across all identity event publishers and subscribers.
    /// 
    /// Part of Phase 2.2 Stage B1.5 implementation.
    /// Phase 2.2 Status: Stage B1.5 - Payload Contract
    /// </summary>
    public static class VisualIdentityPayloadKeys
    {
        #region Owner Keys

        /// <summary>
        /// Player ID key for player identity events.
        /// </summary>
        public const string PlayerId = "PlayerId";

        /// <summary>
        /// Team ID key for team identity events.
        /// </summary>
        public const string TeamId = "TeamId";

        #endregion

        #region Asset Keys

        /// <summary>
        /// Avatar asset ID key for player avatar events.
        /// </summary>
        public const string AvatarAssetId = "AvatarAssetId";

        /// <summary>
        /// Previous avatar asset ID key for player avatar events.
        /// </summary>
        public const string PreviousAvatarAssetId = "PreviousAvatarAssetId";

        /// <summary>
        /// Background asset ID key for player profile background events.
        /// </summary>
        public const string BackgroundAssetId = "BackgroundAssetId";

        /// <summary>
        /// Previous background asset ID key for player profile background events.
        /// </summary>
        public const string PreviousBackgroundAssetId = "PreviousBackgroundAssetId";

        /// <summary>
        /// Frame asset ID key for player frame events.
        /// </summary>
        public const string FrameAssetId = "FrameAssetId";

        /// <summary>
        /// Previous frame asset ID key for player frame events.
        /// </summary>
        public const string PreviousFrameAssetId = "PreviousFrameAssetId";

        /// <summary>
        /// Effect asset ID key for player/team effect events.
        /// </summary>
        public const string EffectAssetId = "EffectAssetId";

        /// <summary>
        /// Previous effect asset ID key for player/team effect events.
        /// </summary>
        public const string PreviousEffectAssetId = "PreviousEffectAssetId";

        /// <summary>
        /// Emblem asset ID key for team emblem events.
        /// </summary>
        public const string EmblemAssetId = "EmblemAssetId";

        /// <summary>
        /// Previous emblem asset ID key for team emblem events.
        /// </summary>
        public const string PreviousEmblemAssetId = "PreviousEmblemAssetId";

        #endregion

        #region Team Color Keys

        /// <summary>
        /// Primary color hex key for team color events.
        /// </summary>
        public const string PrimaryColorHex = "PrimaryColorHex";

        /// <summary>
        /// Previous primary color hex key for team color events.
        /// </summary>
        public const string PreviousPrimaryColorHex = "PreviousPrimaryColorHex";

        /// <summary>
        /// Secondary color hex key for team color events.
        /// </summary>
        public const string SecondaryColorHex = "SecondaryColorHex";

        /// <summary>
        /// Previous secondary color hex key for team color events.
        /// </summary>
        public const string PreviousSecondaryColorHex = "PreviousSecondaryColorHex";

        #endregion

        #region Effect Metadata

        /// <summary>
        /// Effect type key for effect events (e.g., "Glow", "Ring", "Aura").
        /// </summary>
        public const string EffectType = "EffectType";

        #endregion

        #region Time

        /// <summary>
        /// UTC timestamp key for all identity events.
        /// </summary>
        public const string TimestampUtc = "TimestampUtc";

        #endregion
    }
}
