namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Centralized constant event names for Living Visual Identity Engine identity events.
    /// Used with VisualEventBus to publish and subscribe to visual identity changes.
    /// 
    /// Part of Phase 2.2 Stage B1 implementation.
    /// Phase 2.2 Status: Stage B1 - Event Definition
    /// </summary>
    public static class VisualIdentityEventNames
    {
        #region Player Identity Events

        /// <summary>
        /// Event name for player avatar changes.
        /// Category: EventCategory.Player
        /// 
        /// Expected Payload Fields:
        /// - "PlayerId" (string): The player ID whose avatar changed
        /// - "AvatarAssetId" (string): The new avatar asset ID
        /// - "PreviousAvatarAssetId" (string, optional): The previous avatar asset ID
        /// - "TimestampUtc" (DateTimeOffset): UTC event timestamp
        /// </summary>
        public const string PlayerAvatarChanged = "PlayerAvatarChanged";

        /// <summary>
        /// Event name for player profile background changes.
        /// Category: EventCategory.Player
        /// 
        /// Expected Payload Fields:
        /// - "PlayerId" (string): The player ID whose background changed
        /// - "BackgroundAssetId" (string): The new background asset ID
        /// - "PreviousBackgroundAssetId" (string, optional): The previous background asset ID
        /// - "TimestampUtc" (DateTimeOffset): UTC event timestamp
        /// </summary>
        public const string PlayerProfileBackgroundChanged = "PlayerProfileBackgroundChanged";

        /// <summary>
        /// Event name for player frame changes.
        /// Category: EventCategory.Player
        /// 
        /// Expected Payload Fields:
        /// - "PlayerId" (string): The player ID whose frame changed
        /// - "FrameAssetId" (string): The new frame asset ID
        /// - "PreviousFrameAssetId" (string, optional): The previous frame asset ID
        /// - "TimestampUtc" (DateTimeOffset): UTC event timestamp
        /// </summary>
        public const string PlayerFrameChanged = "PlayerFrameChanged";

        /// <summary>
        /// Event name for player effect changes.
        /// Category: EventCategory.Player
        /// 
        /// Expected Payload Fields:
        /// - "PlayerId" (string): The player ID whose effect changed
        /// - "EffectAssetId" (string): The new effect asset ID
        /// - "PreviousEffectAssetId" (string, optional): The previous effect asset ID
        /// - "EffectType" (string): The effect type (e.g., "Glow", "Ring", "Aura")
        /// - "TimestampUtc" (DateTimeOffset): UTC event timestamp
        /// </summary>
        public const string PlayerEffectChanged = "PlayerEffectChanged";

        #endregion

        #region Team Identity Events

        /// <summary>
        /// Event name for team emblem changes.
        /// Category: EventCategory.Team
        /// 
        /// Expected Payload Fields:
        /// - "TeamId" (string): The team ID whose emblem changed
        /// - "EmblemAssetId" (string): The new emblem asset ID
        /// - "PreviousEmblemAssetId" (string, optional): The previous emblem asset ID
        /// - "TimestampUtc" (DateTimeOffset): UTC event timestamp
        /// </summary>
        public const string TeamEmblemChanged = "TeamEmblemChanged";

        /// <summary>
        /// Event name for team color changes.
        /// Category: EventCategory.Team
        /// 
        /// Expected Payload Fields:
        /// - "TeamId" (string): The team ID whose color changed
        /// - "PrimaryColorHex" (string): The new primary color hex code (e.g., "#FF0000")
        /// - "SecondaryColorHex" (string, optional): The new secondary color hex code
        /// - "PreviousPrimaryColorHex" (string, optional): The previous primary color hex code
        /// - "PreviousSecondaryColorHex" (string, optional): The previous secondary color hex code
        /// - "TimestampUtc" (DateTimeOffset): UTC event timestamp
        /// </summary>
        public const string TeamColorChanged = "TeamColorChanged";

        /// <summary>
        /// Event name for team effect changes.
        /// Category: EventCategory.Team
        /// 
        /// Expected Payload Fields:
        /// - "TeamId" (string): The team ID whose effect changed
        /// - "EffectAssetId" (string): The new effect asset ID
        /// - "PreviousEffectAssetId" (string, optional): The previous effect asset ID
        /// - "EffectType" (string): The effect type (e.g., "Glow", "Ring", "Aura")
        /// - "TimestampUtc" (DateTimeOffset): UTC event timestamp
        /// </summary>
        public const string TeamEffectChanged = "TeamEffectChanged";

        #endregion
    }
}
