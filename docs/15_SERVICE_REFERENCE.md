# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---
# 15 — Service Reference

## `Services/AchievementsInfoService.cs`

Class/interface: `AchievementsInfoService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/AppEvents.cs`

Class/interface: `AppEvents`

Relevant terms observed: AppEvents

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/AppVersionService.cs`

Class/interface: `AppVersionService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/ApplicationUserService.cs`

Class/interface: `ApplicationUserService, StoreOwnerContext, ApplicationUserState, LegacyApplicationIdentity`

Relevant terms observed: AppEvents, PlayerId, TeamId, ApplicationUserId, AccountId, DisplayName, PlayerName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/AvatarService.cs`

Class/interface: `AvatarService`

Relevant terms observed: DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/BackupService.cs`

Class/interface: `BackupService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/BadgeEngine.cs`

Class/interface: `BadgeEngine`

Relevant terms observed: PlayerId, TeamId, TeamName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/CertificateExportService.cs`

Class/interface: `CertificateExportService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/DataMaintenanceService.cs`

Class/interface: `DataMaintenanceService`

Relevant terms observed: TeamId, TeamName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/DataStatusService.cs`

Class/interface: `DataStatusModel, DataStatusService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/DeveloperLockService.cs`

Class/interface: `DeveloperLockService`

Relevant terms observed: DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/DeveloperVaultService.cs`

Class/interface: `DeveloperVaultService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/DiagnosticService.cs`

Class/interface: `DiagnosticResultModel, DiagnosticService`

Relevant terms observed: TeamId

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/GameService.cs`

Class/interface: `GameService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/HallOfLegendsConstitutionService.cs`

Class/interface: `HallOfLegendsConstitutionService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/HonorActivationSevice.cs`

Class/interface: `HonorActivationService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/HonorIdentityService.cs`

Class/interface: `HonorIdentityService`

Relevant terms observed: DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/HonorKeyGeneratorService.cs`

Class/interface: `HonorKeyGeneratorService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerAchievementService.cs`

Class/interface: `PlayerAchievementService`

Relevant terms observed: PlayerId

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerEngine.cs`

Class/interface: `PlayerEngine`

Relevant terms observed: DisplayName, PlayerName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerIdentityHistoryService.cs`

Class/interface: `PlayerIdentityHistoryService`

Relevant terms observed: PlayerId, TeamId

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerIdentityService.cs`

Class/interface: `PlayerIdentityService`

Relevant terms observed: PlayerId, PlayerName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerManagementService.cs`

Class/interface: `PlayerManagementService`

Relevant terms observed: AppEvents, PlayerId

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerProfileService.cs`

Class/interface: `PlayerProfileService`

Relevant terms observed: AppEvents, PlayerId, PlayerName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerRankService.cs`

Class/interface: `PlayerRankResult, PlayerRankService, RankBracket`

Relevant terms observed: DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerTeamSyncService.cs`

Class/interface: `PlayerTeamSyncService`

Relevant terms observed: PlayerId, TeamId, DisplayName, PlayerName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/PlayerTimelineService.cs`

Class/interface: `PlayerTimelineService`

Relevant terms observed: TeamId, TeamName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/RankThemeService.cs`

Class/interface: `RankTheme, RankThemeService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/RankingService.cs`

Class/interface: `RankingService`

Relevant terms observed: TeamId, TeamName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/SeasonManager.cs`

Class/interface: `SeasonManager`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/SecurityLogService.cs`

Class/interface: `SecurityLogService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/SpecialHonorsService.cs`

Class/interface: `SpecialHonorsService`

Relevant terms observed: PlayerId

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/SupportReportService.cs`

Class/interface: `SupportReportService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/TeamProfileService.cs`

Class/interface: `TeamProfileService`

Relevant terms observed: PlayerId, TeamId, TeamName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/TrustRingDrawable.cs`

Class/interface: `TrustRingDrawable`

Observed methods include: Draw

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/UpdateLogService.cs`

Class/interface: `UpdateLogService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/UserGuideService.cs`

Class/interface: `UserGuideService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `Services/UserPrivacyProfileService.cs`

Class/interface: `UserPrivacyProfileService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/GalleryService.cs`

Class/interface: `GalleryService`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/GalleryThemeEngine.cs`

Class/interface: `GalleryThemeEngine, GalleryTheme`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/ImageColorExtractor.cs`

Class/interface: `ImageColorExtractor`

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/InventoryDisplayResolver.cs`

Class/interface: `InventoryDisplayResolver`

Relevant terms observed: ApplicationUserId, AssetId, IsOwned, DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/InventoryRouter.cs`

Class/interface: `InventoryOwnerScope, InventoryEquipTarget, InventoryProductContext, InventoryRoute, InventoryState, InventoryActionResult, InventoryRouter`

Relevant terms observed: AppEvents, PlayerId, TeamId, AssetId, IsOwned

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/OwnedAssetCategoryCatalog.cs`

Class/interface: `OwnedAssetCategory, OwnedAssetCategoryCatalog`

Relevant terms observed: DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/PlayerAssetInventoryService.cs`

Class/interface: `PlayerAssetInventoryService`

Relevant terms observed: AppEvents, PlayerId, AssetId, IsOwned

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/PlayerInventoryService.cs`

Class/interface: `PlayerInventoryService, in`

Relevant terms observed: AppEvents, PlayerId, ApplicationUserId, AssetId, IsOwned

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/PlayerStoreIdentityService.cs`

Class/interface: `PlayerStoreIdentityService`

Relevant terms observed: PlayerId

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/PlayerStoreProgressService.cs`

Class/interface: `PlayerStoreProgressService`

Relevant terms observed: PlayerId, TeamId

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/PlayerVisualIdentityResolver.cs`

Class/interface: `PlayerVisualIdentityResolver`

Relevant terms observed: PlayerId, ApplicationUserId, AssetId, IsOwned, PlayerName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/PlayerWalletService.cs`

Class/interface: `PlayerWalletService`

Relevant terms observed: PlayerId

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/StoreAssetCatalogService.cs`

Class/interface: `StoreAssetCatalogService, ProductLink`

Relevant terms observed: AssetId, DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/StoreAssetQueryService.cs`

Class/interface: `StoreAssetSearchEntry, StoreAssetQueryService`

Relevant terms observed: AssetId, DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/StoreCheckoutService.cs`

Class/interface: `StoreCheckoutService`

Relevant terms observed: AppEvents, PlayerId, TeamId, AssetId, IsOwned

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/StoreEquipService.cs`

Class/interface: `StoreAcquireResult, StoreEquipService`

Relevant terms observed: AppEvents, PlayerId, AssetId, IsOwned

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/StorePurchaseService.cs`

Class/interface: `StorePurchaseService, PublishedStoreItem`

Relevant terms observed: AppEvents, PlayerId, ApplicationUserId, IsOwned

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/TeamAssetInventoryService.cs`

Class/interface: `TeamAssetInventoryService`

Relevant terms observed: AppEvents, TeamId, ApplicationUserId, AssetId, IsOwned

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/TeamAssetPayloadCatalog.cs`

Class/interface: `TeamAssetPayloadCatalog`

Relevant terms observed: AssetId, DisplayName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/TeamEligibleAssetService.cs`

Class/interface: `TeamEligibleAssetService`

Relevant terms observed: TeamId, ApplicationUserId, AssetId, IsOwned

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.

## `GalleryEngine/Services/TeamIdentityResolver.cs`

Class/interface: `TeamIdentityResolver`

Relevant terms observed: TeamId, AssetId, TeamName

Policy: inspect this file before changing related behavior. Preserve existing public contracts unless task explicitly requires migration.
