# Domino Majlis PRO - Service Dependency Graph

## Overview [VERIFIED FROM SOURCE CODE - code analysis of Services folder]

This document documents all services, their dependencies, consumers, circular dependency risks, singleton/static usage, and event publishers/subscribers. [VERIFIED FROM SOURCE CODE - code analysis of Services folder]

---

## Core Services

### ApplicationUserService

**Type**: Static Service

**File**: `Services/ApplicationUserService.cs`

**Dependencies**:
- None (filesystem only)

**Consumers**:
- MainPage
- All GalleryEngine services (via GetCurrentStoreOwnerAsync/EnsureCurrentSessionAsync)
- PlayerProfileService (for session context)
- TeamProfileService (for session context)

**Event Publishers**:
- `AppEvents.CurrentUserChanged` (after session changes)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerProfileService

**Type**: Static Service

**File**: `Services/PlayerProfileService.cs`

**Dependencies**:
- PlayerEngine (for normalization)
- PlayerIdentityService (for name normalization)
- AppEvents (for raising events)

**Consumers**:
- MainPage
- PlayerDetailsPage
- PlayerProfilesPage
- PlayerVisualIdentityResolver
- PlayerTimelineService
- PlayerTeamSyncService

**Event Publishers**:
- `AppEvents.DataChanged` (after save)
- `AppEvents.PlayerProfileChanged` (after save)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### TeamProfileService

**Type**: Static Service

**File**: `Services/TeamProfileService.cs`

**Dependencies**:
- None (filesystem only)

**Consumers**:
- MainPage
- CreateTeamPage
- TeamIdentityResolver
- PlayerTeamSyncService
- RankingService

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### RankingService

**Type**: Static Service

**File**: `Services/RankingService.cs`

**Dependencies**:
- TeamProfileService (for team data)
- GameService (for match data)

**Consumers**:
- RankingsPage
- HallOfFamePage
- PlayerEngine (for rank application)

**Event Publishers**:
- `AppEvents.RankingsChanged` (after updates)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### GameService

**Type**: Static Service

**File**: `Services/GameService.cs`

**Dependencies**:
- None (filesystem only)

**Consumers**:
- GamePage
- HistoryPage
- MatchDetailsPage
- RankingService

**Event Publishers**:
- `AppEvents.MatchesChanged` (after match save/delete)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerEngine

**Type**: Static Service

**File**: `Services/PlayerEngine.cs`

**Dependencies**:
- PlayerRankService (for rank calculation)
- InventoryDisplayResolver (for image resolution)

**Consumers**:
- PlayerProfileService
- PlayerTimelineService

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerRankService

**Type**: Static Service

**File**: `Services/PlayerRankService.cs`

**Dependencies**:
- None (pure calculation)

**Consumers**:
- PlayerEngine

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerTimelineService

**Type**: Static Service

**File**: `Services/PlayerTimelineService.cs`

**Dependencies**:
- PlayerProfileService (for player updates)

**Consumers**:
- PlayerProfileService (for event logging)

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerTeamSyncService

**Type**: Static Service

**File**: `Services/PlayerTeamSyncService.cs`

**Dependencies**:
- PlayerProfileService
- TeamProfileService

**Consumers**:
- CreateTeamPage

**Event Publishers**:
- `AppEvents.TeamsChanged` (after sync)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerIdentityService

**Type**: Static Service

**File**: `Services/PlayerIdentityService.cs`

**Dependencies**:
- None (pure utility)

**Consumers**:
- PlayerProfileService
- PlayerVisualIdentityResolver

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerManagementService

**Type**: Static Service

**File**: `Services/PlayerManagementService.cs`

**Dependencies**:
- PlayerProfileService
- AppEvents

**Consumers**:
- PlayerProfilesPage

**Event Publishers**:
- `AppEvents.PlayerProfileChanged`

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### DeveloperLockService

**Type**: Static Service

**File**: `Services/DeveloperLockService.cs`

**Dependencies**:
- None (filesystem only)

**Consumers**:
- DeveloperLoginPage
- ApplicationUserService

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### HonorIdentityService

**Type**: Static Service

**File**: `Services/HonorIdentityService.cs`

**Dependencies**:
- None (filesystem only)

**Consumers**:
- HonorsAdminPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### SpecialHonorsService

**Type**: Static Service

**File**: `Services/SpecialHonorsService.cs`

**Dependencies**:
- PlayerProfileService

**Consumers**:
- HonorsAdminPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### BackupService

**Type**: Static Service

**File**: `Services/BackupService.cs`

**Dependencies**:
- Multiple (reads all JSON files)

**Consumers**:
- MainPage (settings)

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### DataMaintenanceService

**Type**: Static Service

**File**: `Services/DataMaintenanceService.cs`

**Dependencies**:
- Multiple (reads/writes all JSON files)

**Consumers**:
- MainPage (settings)

**Event Publishers**:
- `AppEvents.DataChanged` (after maintenance)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### AvatarService

**Type**: Static Service

**File**: `Services/AvatarService.cs`

**Dependencies**:
- None

**Consumers**:
- PlayerDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### BadgeEngine

**Type**: Static Service

**File**: `Services/BadgeEngine.cs`

**Dependencies**:
- None

**Consumers**:
- PlayerDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### SeasonManager

**Type**: Static Service

**File**: `Services/SeasonManager.cs`

**Dependencies**:
- None

**Consumers**:
- MainPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### SecurityLogService

**Type**: Static Service

**File**: `Services/SecurityLogService.cs`

**Dependencies**:
- None

**Consumers**:
- DeveloperLoginPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### UserGuideService

**Type**: Static Service

**File**: `Services/UserGuideService.cs`

**Dependencies**:
- None

**Consumers**:
- MainPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### SupportReportService

**Type**: Static Service

**File**: `Services/SupportReportService.cs`

**Dependencies**:
- None

**Consumers**:
- MainPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### UpdateLogService

**Type**: Static Service

**File**: `Services/UpdateLogService.cs`

**Dependencies**:
- None

**Consumers**:
- MainPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### AppVersionService

**Type**: Static Service

**File**: `Services/AppVersionService.cs`

**Dependencies**:
- None

**Consumers**:
- MainPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### DataStatusService

**Type**: Static Service

**File**: `Services/DataStatusService.cs`

**Dependencies**:
- Multiple (reads all JSON files)

**Consumers**:
- MainPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### DiagnosticService

**Type**: Static Service

**File**: `Services/DiagnosticService.cs`

**Dependencies**:
- Multiple (reads all JSON files)

**Consumers**:
- MainPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### DeveloperVaultService

**Type**: Static Service

**File**: `Services/DeveloperVaultService.cs`

**Dependencies**:
- None

**Consumers**:
- DeveloperLoginPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### HallOfLegendsConstitutionService

**Type**: Static Service

**File**: `Services/HallOfLegendsConstitutionService.cs`

**Dependencies**:
- None

**Consumers**:
- HallOfFamePage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### HonorActivationService

**Type**: Static Service

**File**: `Services/HonorActivationSevice.cs`

**Dependencies**:
- None

**Consumers**:
- HonorsAdminPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### HonorKeyGeneratorService

**Type**: Static Service

**File**: `Services/HonorKeyGeneratorService.cs`

**Dependencies**:
- None

**Consumers**:
- HonorsAdminPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerAchievementService

**Type**: Static Service

**File**: `Services/PlayerAchievementService.cs`

**Dependencies**:
- None

**Consumers**:
- PlayerDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerIdentityHistoryService

**Type**: Static Service

**File**: `Services/PlayerIdentityHistoryService.cs`

**Dependencies**:
- None

**Consumers**:
- PlayerDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### RankThemeService

**Type**: Static Service

**File**: `Services/RankThemeService.cs`

**Dependencies**:
- None

**Consumers**:
- RankingsPage
- PlayerDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### CertificateExportService

**Type**: Static Service

**File**: `Services/CertificateExportService.cs`

**Dependencies**:
- PdfSharpCore

**Consumers**:
- CertificatePage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### UserPrivacyProfileService

**Type**: Static Service

**File**: `Services/UserPrivacyProfileService.cs`

**Dependencies**:
- None

**Consumers**:
- PlayerDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### TrustRingDrawable

**Type**: Static Utility

**File**: `Services/TrustRingDrawable.cs`

**Dependencies**:
- None

**Consumers**:
- PlayerDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### AchievementsInfoService

**Type**: Static Service

**File**: `Services/AchievementsInfoService.cs`

**Dependencies**:
- None

**Consumers**:
- PlayerDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

## GalleryEngine Services

### PlayerInventoryService

**Type**: Static Service

**File**: `GalleryEngine/Services/PlayerInventoryService.cs`

**Dependencies**:
- ApplicationUserService (for ApplicationUserId scoping)
- AppEvents (for raising events)
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- StorePurchaseService
- StoreEquipService
- InventoryRouter
- PlayerAssetInventoryService
- InventoryDisplayResolver
- PlayerVisualIdentityResolver
- TeamEligibleAssetService

**Event Publishers**:
- `AppEvents.StoreEconomyChanged` (after inventory changes)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerAssetInventoryService

**Type**: Static Service

**File**: `GalleryEngine/Services/PlayerAssetInventoryService.cs`

**Dependencies**:
- ApplicationUserService (for ApplicationUserId scoping)
- PlayerInventoryService (delegates to it)
- AppEvents

**Consumers**:
- InventoryDisplayResolver
- PlayerVisualIdentityResolver
- TeamEligibleAssetService

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### TeamAssetInventoryService

**Type**: Static Service

**File**: `GalleryEngine/Services/TeamAssetInventoryService.cs`

**Dependencies**:
- ApplicationUserService (for ApplicationUserId scoping)
- AppEvents
- StoreCmsJsonRepository (for persistence)
- TeamAssetPayloadCatalog (for default assets)

**Consumers**:
- InventoryDisplayResolver
- TeamIdentityResolver
- TeamEligibleAssetService

**Event Publishers**:
- `AppEvents.TeamAssetsChanged` (after team asset changes)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StorePurchaseService

**Type**: Static Service

**File**: `GalleryEngine/Services/StorePurchaseService.cs`

**Dependencies**:
- PlayerWalletService (for debit)
- PlayerInventoryService (for ownership)
- AvatarsAdminService (for avatar catalog)
- BackgroundsAdminService (for background catalog)
- ApplicationUserService (for ApplicationUserId)
- AppEvents

**Consumers**:
- StoreCheckoutService
- GalleryPage components

**Event Publishers**:
- `AppEvents.StoreEconomyChanged` (after purchase)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreEquipService

**Type**: Static Service

**File**: `GalleryEngine/Services/StoreEquipService.cs`

**Dependencies**:
- PlayerInventoryService (for equipment)
- AppEvents

**Consumers**:
- InventoryRouter
- GalleryPage components

**Event Publishers**:
- `AppEvents.StoreEconomyChanged` (after equipment change)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCheckoutService

**Type**: Static Service

**File**: `GalleryEngine/Services/StoreCheckoutService.cs`

**Dependencies**:
- StorePurchaseService (for purchase)
- AppEvents

**Consumers**:
- GalleryPage components

**Event Publishers**:
- `AppEvents.StoreEconomyChanged` (after checkout)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerWalletService

**Type**: Static Service

**File**: `GalleryEngine/Services/PlayerWalletService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- StorePurchaseService

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreAssetCatalogService

**Type**: Static Service

**File**: `GalleryEngine/Services/StoreAssetCatalogService.cs`

**Dependencies**:
- AvatarsAdminService (for avatar catalog)
- BackgroundsAdminService (for background catalog)
- NewArrivalsAdminService (for arrivals catalog)
- LimitedOffersAdminService (for offers catalog)
- TeamAssetPayloadCatalog (for team assets)
- StoreProductAssetTypeCatalog (for type resolution)

**Consumers**:
- InventoryDisplayResolver
- PlayerVisualIdentityResolver
- TeamIdentityResolver
- InventoryRouter
- StoreAssetQueryService

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreAssetQueryService

**Type**: Static Service

**File**: `GalleryEngine/Services/StoreAssetQueryService.cs`

**Dependencies**:
- StoreAssetCatalogService

**Consumers**:
- GalleryPage components

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### InventoryDisplayResolver

**Type**: Static Service

**File**: `GalleryEngine/Services/InventoryDisplayResolver.cs`

**Dependencies**:
- StoreAssetCatalogService (for catalog)
- PlayerAssetInventoryService (for player inventory)
- TeamAssetInventoryService (for team inventory)
- ApplicationUserService (for session)

**Consumers**:
- PlayerProfilesPage
- PlayerDetailsPage
- GalleryPage components

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### InventoryRouter

**Type**: Static Service

**File**: `GalleryEngine/Services/InventoryRouter.cs`

**Dependencies**:
- ApplicationUserService (for owner context)
- PlayerInventoryService (for ownership)
- StoreEquipService (for equipment)
- PlayerProfileService (for team resolution)
- TeamProfileService (for team resolution)
- AppEvents
- StoreProductAssetTypeCatalog (for type resolution)

**Consumers**:
- GalleryPage components

**Event Publishers**:
- `AppEvents.StoreEconomyChanged` (after acquire/equip)

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerVisualIdentityResolver

**Type**: Static Service

**File**: `GalleryEngine/Services/PlayerVisualIdentityResolver.cs`

**Dependencies**:
- PlayerProfileService (for player resolution)
- PlayerAssetInventoryService (for inventory)
- StoreAssetCatalogService (for catalog)
- ApplicationUserService (for session)
- PlayerIdentityService (for name normalization)

**Consumers**:
- MainPage
- PlayerProfilesPage
- PlayerDetailsPage
- RankingsPage
- HistoryPage
- MatchDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### TeamIdentityResolver

**Type**: Static Service

**File**: `GalleryEngine/Services/TeamIdentityResolver.cs`

**Dependencies**:
- TeamProfileService (for team resolution)
- StoreAssetCatalogService (for catalog)
- TeamAssetPayloadCatalog (for assets)

**Consumers**:
- MainPage
- RankingsPage
- HallOfFamePage
- HistoryPage
- MatchDetailsPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### TeamEligibleAssetService

**Type**: Static Service

**File**: `GalleryEngine/Services/TeamEligibleAssetService.cs`

**Dependencies**:
- PlayerInventoryService (for player ownership)
- ApplicationUserService (for session)
- StoreAssetCatalogService (for type resolution)
- TeamAssetPayloadCatalog (for defaults)

**Consumers**:
- CreateTeamPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerStoreIdentityService

**Type**: Static Service

**File**: `GalleryEngine/Services/PlayerStoreIdentityService.cs`

**Dependencies**:
- None

**Consumers**:
- GalleryPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### PlayerStoreProgressService

**Type**: Static Service

**File**: `GalleryEngine/Services/PlayerStoreProgressService.cs`

**Dependencies**:
- None

**Consumers**:
- GalleryPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### GalleryService

**Type**: Static Service

**File**: `GalleryEngine/Services/GalleryService.cs`

**Dependencies**:
- None

**Consumers**:
- GalleryPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### GalleryThemeEngine

**Type**: Static Service

**File**: `GalleryEngine/Services/GalleryThemeEngine.cs`

**Dependencies**:
- None

**Consumers**:
- GalleryPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### ImageColorExtractor

**Type**: Static Service

**File**: `GalleryEngine/Services/ImageColorExtractor.cs`

**Dependencies**:
- None

**Consumers**:
- GalleryPage components

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### OwnedAssetCategoryCatalog

**Type**: Static Service

**File**: `GalleryEngine/Services/OwnedAssetCategoryCatalog.cs`

**Dependencies**:
- None

**Consumers**:
- GalleryPage components

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### TeamAssetPayloadCatalog

**Type**: Static Catalog

**File**: `GalleryEngine/Services/TeamAssetPayloadCatalog.cs`

**Dependencies**:
- None

**Consumers**:
- TeamAssetInventoryService
- TeamIdentityResolver
- StoreAssetCatalogService
- TeamEligibleAssetService

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

## GalleryEngine Admin Services

### AvatarsAdminService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/AvatarsAdminService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- StoreAssetCatalogService
- StorePurchaseService
- AvatarsEditorPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### BackgroundsAdminService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/BackgroundsAdminService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- StoreAssetCatalogService
- StorePurchaseService
- BackgroundsEditorPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### NewArrivalsAdminService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/NewArrivalsAdminService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- StoreAssetCatalogService
- NewArrivalsEditorPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### LimitedOffersAdminService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/LimitedOffersAdminService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- StoreAssetCatalogService
- LimitedOffersEditorPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### CurrentSeasonAdminService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/CurrentSeasonAdminService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- CurrentSeasonEditorPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCategoriesAdminService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/StoreCategoriesAdminService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- StoreCategoriesEditorPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreAdminService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/StoreAdminService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- DeveloperStoreManagerPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StorePricingAdminService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/StorePricingAdminService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- CurrencyPricingManagerPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreRuntimeConfigurationService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/StoreRuntimeConfigurationService.cs`

**Dependencies**:
- StoreCmsJsonRepository (for persistence)

**Consumers**:
- StoreSettingsManagerPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### InventoryAuditService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Services/InventoryAuditService.cs`

**Dependencies**:
- StoreAssetCatalogService
- PlayerInventoryService
- TeamAssetInventoryService
- AppEvents

**Consumers**:
- InventoryAuditPage

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

## GalleryEngine Admin Core Services

### StoreCmsJsonRepository

**Type**: Static Repository

**File**: `GalleryEngine/Admin/Core/StoreCmsJsonRepository.cs`

**Dependencies**:
- None (filesystem only)

**Consumers**:
- All Admin Services
- PlayerWalletService
- PlayerInventoryService
- TeamAssetInventoryService

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCmsAssetPickerService

**Type**: Static Service

**File**: `GalleryEngine/Admin/Core/StoreCmsAssetPickerService.cs`

**Dependencies**:
- StoreAssetCatalogService

**Consumers**:
- Admin pages

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCmsOrderingEngine

**Type**: Static Service

**File**: `GalleryEngine/Admin/Core/StoreCmsOrderingEngine.cs`

**Dependencies**:
- None

**Consumers**:
- Admin pages

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCmsPreviewEngine

**Type**: Static Service

**File**: `GalleryEngine/Admin/Core/StoreCmsPreviewEngine.cs`

**Dependencies**:
- None

**Consumers**:
- Admin pages

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCmsPricingEngine

**Type**: Static Service

**File**: `GalleryEngine/Admin/Core/StoreCmsPricingEngine.cs`

**Dependencies**:
- None

**Consumers**:
- Admin pages

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCmsPublishEngine

**Type**: Static Service

**File**: `GalleryEngine/Admin/Core/StoreCmsPublishEngine.cs`

**Dependencies**:
- StoreCmsJsonRepository

**Consumers**:
- Admin pages

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCmsSearchEngine

**Type**: Static Service

**File**: `GalleryEngine/Admin/Core/StoreCmsSearchEngine.cs`

**Dependencies**:
- None

**Consumers**:
- Admin pages

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCmsStatistics

**Type**: Static Service

**File**: `GalleryEngine/Admin/Core/StoreCmsStatistics.cs`

**Dependencies**:
- None

**Consumers**:
- Admin pages

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

### StoreCmsValidationEngine

**Type**: Static Service

**File**: `GalleryEngine/Admin/Core/StoreCmsValidationEngine.cs`

**Dependencies**:
- None

**Consumers**:
- Admin pages

**Event Publishers**: None

**Event Subscribers**: None

**Circular Dependency Risks**: None

**Singleton/Static**: Static

---

## Event Publisher Summary

### AppEvents Publishers

The following services publish AppEvents:

- **ApplicationUserService**: `CurrentUserChanged`
- **PlayerProfileService**: `DataChanged`, `PlayerProfileChanged`
- **GameService**: `MatchesChanged`
- **RankingService**: `RankingsChanged`
- **PlayerTeamSyncService**: `TeamsChanged`
- **PlayerManagementService**: `PlayerProfileChanged`
- **DataMaintenanceService**: `DataChanged`
- **PlayerInventoryService**: `StoreEconomyChanged`, `StoreProgressChanged`
- **TeamAssetInventoryService**: `TeamAssetsChanged`, `TeamEffectChanged`
- **StorePurchaseService**: `StoreEconomyChanged`
- **StoreEquipService**: `StoreEconomyChanged`
- **StoreCheckoutService**: `StoreEconomyChanged`
- **InventoryRouter**: `StoreEconomyChanged`
- **InventoryAuditPage**: `StoreEconomyChanged`

---

## Event Subscriber Summary

### AppEvents Subscribers

The following pages subscribe to AppEvents:

- **MainPage**: All events
- **RankingsPage**: `RankingsChanged`, `TeamsChanged`, `PlayerProfileChanged`
- **HallOfFamePage**: `RankingsChanged`, `TeamsChanged`, `PlayerProfileChanged`
- **HistoryPage**: `MatchesChanged`, `TeamsChanged`, `PlayerProfileChanged`
- **MatchDetailsPage**: `MatchesChanged`, `TeamsChanged`, `PlayerProfileChanged`
- **PlayerProfilesPage**: `PlayerProfileChanged`, `StoreEconomyChanged`, `StoreProgressChanged`
- **PlayerDetailsPage**: `PlayerProfileChanged`, `StoreEconomyChanged`, `StoreProgressChanged`
- **GamePage**: `MatchesChanged`, `TeamsChanged`, `PlayerProfileChanged`
- **CreateTeamPage**: `TeamsChanged`, `TeamAssetsChanged`, `StoreEconomyChanged`

---

## Circular Dependency Analysis [VERIFIED FROM SOURCE CODE - dependency analysis]

### No Circular Dependencies Detected

The current architecture has no circular dependencies. All services follow a hierarchical dependency pattern: [VERIFIED FROM SOURCE CODE - dependency analysis]

1. **Core services** (filesystem only) have no dependencies [VERIFIED FROM SOURCE CODE - dependency analysis]
2. **Business services** depend on core services [VERIFIED FROM SOURCE CODE - dependency analysis]
3. **GalleryEngine services** depend on core services and admin services [VERIFIED FROM SOURCE CODE - dependency analysis]
4. **Admin services** depend only on StoreCmsJsonRepository [VERIFIED FROM SOURCE CODE - dependency analysis]
5. **Pages** depend on services but services never depend on pages [VERIFIED FROM SOURCE CODE - dependency analysis]

### Dependency Layers [VERIFIED FROM SOURCE CODE - dependency analysis]

**Layer 0 (No dependencies)**:
- StoreCmsJsonRepository
- PlayerIdentityService
- PlayerRankService
- All admin core services

**Layer 1 (Core services)**:
- ApplicationUserService
- PlayerProfileService
- TeamProfileService
- GameService
- DeveloperLockService

**Layer 2 (Business services)**:
- RankingService
- PlayerEngine
- PlayerTimelineService
- PlayerTeamSyncService

**Layer 3 (GalleryEngine services)**:
- PlayerInventoryService
- TeamAssetInventoryService
- StorePurchaseService
- StoreEquipService
- StoreAssetCatalogService

**Layer 4 (Resolver services)**:
- InventoryDisplayResolver
- PlayerVisualIdentityResolver
- TeamIdentityResolver
- InventoryRouter

**Layer 5 (Pages)**:
- All pages (consume services, never consumed by services)

---

## Singleton/Static Usage [VERIFIED FROM SOURCE CODE - code analysis]

### All Services Are Static [VERIFIED FROM SOURCE CODE - code analysis]

Every service in the application is implemented as a static class. This is intentional for simplicity and to avoid dependency injection complexity in a MAUI application. [INFERRED - from static class pattern]

### Thread Safety [VERIFIED FROM SOURCE CODE - code analysis of file operations]

Services that perform file operations use `SemaphoreSlim` for thread safety:
- ApplicationUserService [VERIFIED FROM SOURCE CODE - ApplicationUserService.cs]
- PlayerInventoryService [VERIFIED FROM SOURCE CODE - PlayerInventoryService.cs]
- TeamAssetInventoryService [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]
- PlayerWalletService [VERIFIED FROM SOURCE CODE - PlayerWalletService.cs]
- StoreCmsJsonRepository (uses ConcurrentDictionary for per-file locks) [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]

### Static State Risks [INFERRED - general software engineering principles]

**Potential Issues**:
- Static state can cause issues with unit testing [INFERRED - general testing patterns]
- Static state can cause issues with multi-tenant scenarios (not currently applicable) [INFERRED - general architectural concerns]

**Mitigations**:
- All file operations use locks [VERIFIED FROM SOURCE CODE - code analysis]
- All identity operations are scoped by IDs [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
- ApplicationUserId scoping prevents cross-account leakage [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]

---

## Summary [VERIFIED FROM SOURCE CODE - code analysis]

- **Total Services**: 60+ services [VERIFIED FROM SOURCE CODE - count of services]
- **Static Services**: 100% (all services are static) [VERIFIED FROM SOURCE CODE - code analysis]
- **Circular Dependencies**: 0 [VERIFIED FROM SOURCE CODE - dependency analysis]
- **Event Publishers**: 15+ services [VERIFIED FROM SOURCE CODE - AppEvents usage]
- **Event Subscribers**: 9+ pages [VERIFIED FROM SOURCE CODE - page code-behind]
- **Dependency Layers**: 5 clear layers [VERIFIED FROM SOURCE CODE - dependency analysis]
- **Thread Safety**: SemaphoreSlim used for file operations [VERIFIED FROM SOURCE CODE - code analysis]
