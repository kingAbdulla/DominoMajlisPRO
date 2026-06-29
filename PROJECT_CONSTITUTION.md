# Domino Majlis PRO - Project Constitution

## Overview

This document defines the architectural rules and engineering principles that govern the Domino Majlis PRO codebase. All modifications must adhere to these rules unless explicitly approved by the user.

---

## Identity First Architecture

### Core Principle

All identity operations must use ID-based lookups as the primary mechanism. Display names are for presentation only and must never be used as primary keys for data operations. [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]

### Authoritative Identity Keys

- **ApplicationUserId** / **AccountId**: Account/session identifier [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
- **PlayerId**: Player identity (format: P####) [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
- **TeamId**: Team identity (format: T####) [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
- **AssetId**: Store asset identifier (canonical asset ID) [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
- **ProductId**: Store product identifier (where applicable) [INFERRED - from StorePurchaseService code]

### Display-Only Fields (Never Use as Keys)

- DisplayName [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
- PlayerName [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
- TeamName [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
- Developer display name [INFERRED - from DeveloperLockService code]
- Any visible UI text [INFERRED - general architectural pattern]

### ID-First Lookup Rules

1. All service methods must attempt ID lookup first [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
2. Name-based fallback is allowed only for legacy data migration [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
3. New writes must always use IDs [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
4. Display name matching must use normalized comparison [INFERRED - from PlayerIdentityService code]
5. Similar display names must not cross-link accounts [INFERRED - from ApplicationUserService code]

### Identity Scoping

- Player-owned assets must be scoped by `PlayerId` [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Team assets must be scoped by `TeamId` and filtered by `Player1Id`/`Player2Id` ownership [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- ApplicationUserId must be attached to inventory records for account isolation [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]
- Developer inventory must not leak to normal accounts [VERIFIED FROM SOURCE CODE - docs/13_KNOWN_BUGS_AND_PHASE_2_8.md]

---

## Layout Protection Policy

### Protected Elements

The following XAML layout elements are protected and must not be modified without explicit user request: [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]

- XAML hierarchy structure
- Grid definitions and layouts
- Margins, padding, and spacing
- Card structure and composition
- Navigation flow and routing
- CollectionView/CarouselView structure
- Approved page composition
- RTL (Right-to-Left) layout direction [INFERRED - from Arabic-first requirement in docs/01_PROJECT_MISSION.md]

### Allowed Modifications (Without Redesign Approval) [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]

- Binding fixes
- Command fixes
- ViewModel updates
- Service integration
- AppEvents subscription/unsubscription fixes
- ItemsSource refresh safety fixes
- Display resolver fixes
- ImageSource resolution fixes
- Visibility binding fixes

### MAUI RecyclerView Safety

For CollectionView-backed UI on Android, follow these rules to prevent crashes: [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]

- Build new lists off the main thread
- Assign `ItemsSource = null` if needed before replacement
- Assign new list/collection on the main thread
- Suppress selection-change handlers during reload
- Never modify bound collections from background threads

**Critical**: This prevents `Java.Lang.IndexOutOfBoundsException: Inconsistency detected. Invalid item position` crashes. [VERIFIED FROM SOURCE CODE - docs/12_RUNTIME_VERIFICATION.md]

---

## Player vs Team Asset Separation

### Player Assets

**Scope**: Owned by individual players via `PlayerId` [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]

**Asset Types**: [INFERRED - from StoreProductAssetTypeCatalog and InventoryRouter code]
- Avatars
- Profile Backgrounds
- Frames
- Effects (player)
- Titles

**Ownership Rules**:
- Must include `PlayerId`, `AssetId`, `AssetType`, `IsOwned = true` [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Equipping affects only the current player [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Scoped by `ApplicationUserId` for account isolation [VERIFIED FROM SOURCE CODE - PlayerInventoryService.cs]

### Team Assets

**Scope**: Available to teams based on player ownership [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]

**Asset Types**: [INFERRED - from TeamAssetPayloadCatalog and InventoryRouter code]
- Emblems
- Team Colors
- Emblem Backgrounds
- Team Effects

**Availability Formula**: [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
```
Available = Default team assets
          + Assets owned by Player1Id
          + Assets owned by Player2Id
```

**Rules**:
- Do not include assets owned by other accounts on the same device [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Do not include all published assets [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Do not use display names for ownership determination [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Default assets are available choices, not owned items [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]

### Default Assets

**Definition**: Assets available to all users by default [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]

**Rules**:
- Default avatars may appear in avatar selection catalogs [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Default avatars must NOT appear in "My Assets" as owned [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Default team assets may appear in CreateTeamPage as defaults [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- Default team assets must NOT be saved as player-owned purchases [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]

---

## GalleryEngine Architecture

### Subsystem Isolation

GalleryEngine is an isolated subsystem for store/gallery/inventory concerns. It must not be tightly coupled to core game logic. [VERIFIED FROM SOURCE CODE - docs/08_STORE_GALLERY_ARCHITECTURE.md]

### Core Components

**Admin Layer** (`GalleryEngine/Admin/`):
- Content management services (AvatarsAdminService, BackgroundsAdminService, etc.)
- CMS core services (StoreCmsJsonRepository, StoreCmsPublishEngine, etc.)
- Admin pages for developers

**Services Layer** (`GalleryEngine/Services/`):
- PlayerInventoryService - Player asset ownership
- TeamAssetInventoryService - Team asset ownership
- StorePurchaseService - Purchase flow
- StoreEquipService - Equipment logic
- PlayerWalletService - Currency management
- StoreAssetCatalogService - Catalog loading
- InventoryDisplayResolver - UI resolution
- PlayerVisualIdentityResolver - Player avatar/bg resolution
- TeamIdentityResolver - Team identity resolution
- InventoryRouter - Routing logic

**Models Layer** (`GalleryEngine/Models/`):
- PlayerOwnedStoreItem
- TeamOwnedAssetItem
- PlayerWalletModel
- TeamIdentityModel
- StorePurchaseRecord

### Store Concepts

1. **Published Asset/Product**: Available in catalog [VERIFIED FROM SOURCE CODE - docs/08_STORE_GALLERY_ARCHITECTURE.md]
2. **Owned Asset**: Purchased/acquired by user [VERIFIED FROM SOURCE CODE - docs/08_STORE_GALLERY_ARCHITECTURE.md]
3. **Equipped Asset**: Currently in use [VERIFIED FROM SOURCE CODE - docs/08_STORE_GALLERY_ARCHITECTURE.md]
4. **Default Available Asset**: Available to all by default [VERIFIED FROM SOURCE CODE - docs/08_STORE_GALLERY_ARCHITECTURE.md]

**Rule**: Publishing does not imply ownership. Default availability does not imply owned inventory. [VERIFIED FROM SOURCE CODE - docs/08_STORE_GALLERY_ARCHITECTURE.md]

### Protected Files

The following files are critical and must not be modified without understanding their full impact:

- `GalleryEngine/Admin/SpecializedStoreManagerPage.cs`
- `GalleryEngine/Admin/DeveloperStoreManagerPage.xaml.cs`
- `GalleryEngine/Services/StoreCheckoutService.cs`
- `GalleryEngine/Services/StorePurchaseService.cs`
- `GalleryEngine/Services/StoreEquipService.cs`
- `GalleryEngine/Services/StoreAssetCatalogService.cs`
- `GalleryEngine/Services/InventoryDisplayResolver.cs`
- `GalleryEngine/Admin/Core/StoreCmsJsonRepository.cs`

---

## AppEvents Synchronization Contract

### Central Event Bus

`Services/AppEvents.cs` is the single source of truth for application-wide synchronization. Do not create parallel event buses. [VERIFIED FROM SOURCE CODE - docs/10_APP_EVENTS_SYNC.md]

### Available Events [VERIFIED FROM SOURCE CODE - Services/AppEvents.cs]

- `DataChanged` - Global data refresh
- `RankingsChanged` - Rankings updated
- `TeamsChanged` - Team data updated
- `MatchesChanged` - Match history updated
- `PlayerProfileChanged` - Player data updated
- `CurrentUserChanged` - Account/session switched
- `StoreEconomyChanged(string playerId)` - Wallet/inventory updated
- `StoreProgressChanged(string playerId)` - Collection progress updated
- `TeamAssetsChanged(string teamId)` - Team inventory updated
- `TeamEffectChanged(string teamId)` - Team effects updated

### Event Raising Rules [VERIFIED FROM SOURCE CODE - docs/10_APP_EVENTS_SYNC.md]

Events must be raised AFTER:
- Player profile update
- Avatar/equipment change
- Team identity save
- Match update
- Ranking update
- Account/session switch
- Developer role activation
- Store publish/acquire/equip
- Reset/import/backup restore

### Subscription Rules [VERIFIED FROM SOURCE CODE - docs/10_APP_EVENTS_SYNC.md]

- Subscribe/unsubscribe carefully to avoid memory leaks
- Unsubscribe in `OnDisappearing` or page disposal
- UI updates must happen on the main thread
- Display pages should refresh from saved identity state, not mutate inventory

### Thread Safety [VERIFIED FROM SOURCE CODE - Services/AppEvents.cs]

All AppEvents use `MainThread.BeginInvokeOnMainThread` for safe UI updates.

---

## JSON Persistence Rules

### File-Based Storage

The application uses JSON files as the persistence layer. All file operations must follow these rules. [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]

### Safety Rules [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]

1. **Missing files**: Return empty safe data (never crash)
2. **Corrupt JSON**: Must not crash display pages; return empty data
3. **File locks**: Use per-file locks (SemaphoreSlim) for thread safety
4. **Atomic writes**: Use temp-file atomic replacement where implemented
5. **Image failures**: Never wipe inventory because image resolution fails
6. **Read-only rendering**: Do not write during page rendering unless necessary

### Critical Repository [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]

`GalleryEngine/Admin/Core/StoreCmsJsonRepository.cs` is the protected repository for store CMS persistence. It implements:
- Concurrent file locking [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]
- Temp-file atomic writes [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]
- Corrupt file backup [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]
- JSON validation [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]

### Data Integrity [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]

Any reset/delete operation must:
- Be Developer-only
- Preserve backup/audit policy where implemented
- Require explicit confirmation

---

## Naming Conventions [INFERRED - from code analysis of file and class naming patterns]

### File Naming

- **Services**: `*Service.cs` (e.g., `PlayerProfileService.cs`)
- **Models**: `*Model.cs` (e.g., `PlayerProfileModel.cs`)
- **Pages**: `*Page.xaml` and `*Page.xaml.cs`
- **Admin Pages**: `*EditorPage.xaml` and `*EditorPage.xaml.cs`

### Class Naming

- **Service Classes**: Static classes with `Service` suffix
- **Model Classes**: Records or classes with `Model` suffix
- **Page Classes**: Match file name without suffix
- **Enums**: PascalCase with descriptive names

### Field Naming

- **Private fields**: camelCase with underscore prefix if needed
- **Public properties**: PascalCase
- **Constants**: PascalCase
- **Async methods**: `Async` suffix

### Identity Fields

- **IDs**: PascalCase with `Id` suffix (e.g., `PlayerId`, `TeamId`)
- **Foreign keys**: Include entity name (e.g., `Player1Id`, `Player2Id`)
- **Display names**: `Name` or `DisplayName` suffix

---

## Service Responsibilities [VERIFIED FROM SOURCE CODE - docs/15_SERVICE_REFERENCE.md]

### Core Services Layer [VERIFIED FROM SOURCE CODE - docs/15_SERVICE_REFERENCE.md]

**ApplicationUserService**:
- Account/session management [VERIFIED FROM SOURCE CODE - ApplicationUserService.cs]
- Identity choice flow [VERIFIED FROM SOURCE CODE - ApplicationUserService.cs]
- Developer lock integration [VERIFIED FROM SOURCE CODE - ApplicationUserService.cs]
- Session persistence [VERIFIED FROM SOURCE CODE - ApplicationUserService.cs]

**PlayerProfileService**:
- Player CRUD operations [VERIFIED FROM SOURCE CODE - PlayerProfileService.cs]
- Profile image management [VERIFIED FROM SOURCE CODE - PlayerProfileService.cs]
- Avatar assignment [VERIFIED FROM SOURCE CODE - PlayerProfileService.cs]
- Timeline event logging [VERIFIED FROM SOURCE CODE - PlayerProfileService.cs]

**TeamProfileService**:
- Team CRUD operations [VERIFIED FROM SOURCE CODE - TeamProfileService.cs]
- Team-player relationships [VERIFIED FROM SOURCE CODE - TeamProfileService.cs]
- Team ID generation [VERIFIED FROM SOURCE CODE - TeamProfileService.cs]

**RankingService**:
- Ranking calculations [VERIFIED FROM SOURCE CODE - RankingService.cs]
- XP management [VERIFIED FROM SOURCE CODE - RankingService.cs]
- Hall of Fame eligibility [VERIFIED FROM SOURCE CODE - RankingService.cs]
- Rivalry tracking [VERIFIED FROM SOURCE CODE - RankingService.cs]

**GameService**:
- Match recording [VERIFIED FROM SOURCE CODE - GameService.cs]
- Match history persistence [VERIFIED FROM SOURCE CODE - GameService.cs]
- Unfinished match recovery [VERIFIED FROM SOURCE CODE - GameService.cs]

**PlayerEngine**:
- Stats calculation [VERIFIED FROM SOURCE CODE - PlayerEngine.cs]
- Profile normalization [VERIFIED FROM SOURCE CODE - PlayerEngine.cs]
- Rank application [VERIFIED FROM SOURCE CODE - PlayerEngine.cs]
- Legacy score computation [VERIFIED FROM SOURCE CODE - PlayerEngine.cs]

### GalleryEngine Services [VERIFIED FROM SOURCE CODE - docs/15_SERVICE_REFERENCE.md]

**PlayerInventoryService**:
- Player asset ownership [VERIFIED FROM SOURCE CODE - PlayerInventoryService.cs]
- Equipment management [VERIFIED FROM SOURCE CODE - PlayerInventoryService.cs]
- Ownership validation [VERIFIED FROM SOURCE CODE - PlayerInventoryService.cs]

**TeamAssetInventoryService**:
- Team asset ownership [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]
- Default asset merging [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]
- ApplicationUserId scoping [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]

**StorePurchaseService**:
- Purchase flow [VERIFIED FROM SOURCE CODE - StorePurchaseService.cs]
- Wallet debiting [VERIFIED FROM SOURCE CODE - StorePurchaseService.cs]
- Inventory acquisition [VERIFIED FROM SOURCE CODE - StorePurchaseService.cs]
- Purchase recording [VERIFIED FROM SOURCE CODE - StorePurchaseService.cs]

**StoreEquipService**:
- Equipment logic [VERIFIED FROM SOURCE CODE - StoreEquipService.cs]
- Equipment state management [VERIFIED FROM SOURCE CODE - StoreEquipService.cs]
- Equipment validation [VERIFIED FROM SOURCE CODE - StoreEquipService.cs]

**PlayerWalletService**:
- Currency management [VERIFIED FROM SOURCE CODE - PlayerWalletService.cs]
- Credit/debit operations [VERIFIED FROM SOURCE CODE - PlayerWalletService.cs]
- Wallet creation [VERIFIED FROM SOURCE CODE - PlayerWalletService.cs]

**StoreAssetCatalogService**:
- Catalog loading [VERIFIED FROM SOURCE CODE - StoreAssetCatalogService.cs]
- Asset resolution [VERIFIED FROM SOURCE CODE - StoreAssetCatalogService.cs]
- Canonical type mapping [VERIFIED FROM SOURCE CODE - StoreAssetCatalogService.cs]

**InventoryDisplayResolver**:
- UI display resolution [VERIFIED FROM SOURCE CODE - InventoryDisplayResolver.cs]
- Image source resolution [VERIFIED FROM SOURCE CODE - InventoryDisplayResolver.cs]
- Collection snapshot generation [VERIFIED FROM SOURCE CODE - InventoryDisplayResolver.cs]

**PlayerVisualIdentityResolver**:
- Player avatar resolution [VERIFIED FROM SOURCE CODE - PlayerVisualIdentityResolver.cs]
- Player background resolution [VERIFIED FROM SOURCE CODE - PlayerVisualIdentityResolver.cs]
- Identity composition [VERIFIED FROM SOURCE CODE - PlayerVisualIdentityResolver.cs]

**TeamIdentityResolver**:
- Team emblem resolution [VERIFIED FROM SOURCE CODE - TeamIdentityResolver.cs]
- Team color resolution [VERIFIED FROM SOURCE CODE - TeamIdentityResolver.cs]
- Team identity composition [VERIFIED FROM SOURCE CODE - TeamIdentityResolver.cs]

### Service Rules [INFERRED - from code analysis of Services folder]

1. **Static classes**: All services are static for simplicity
2. **Async methods**: All I/O operations must be async
3. **Validation**: Validate identity keys before operations
4. **Event raising**: Raise AppEvents after persistence
5. **Error handling**: Return safe defaults on failure
6. **Thread safety**: Use SemaphoreSlim for file operations

---

## Model Responsibilities [INFERRED - from code analysis of Models folder]

### Core Models [INFERRED - from code analysis of Models folder]

**PlayerProfileModel**:
- Player identity and stats [VERIFIED FROM SOURCE CODE - PlayerProfileModel.cs]
- Avatar configuration [VERIFIED FROM SOURCE CODE - PlayerProfileModel.cs]
- Timeline events [VERIFIED FROM SOURCE CODE - PlayerProfileModel.cs]
- Profile completion status [VERIFIED FROM SOURCE CODE - PlayerProfileModel.cs]

**TeamProfileModel**:
- Team identity and stats [VERIFIED FROM SOURCE CODE - TeamProfileModel.cs]
- Player relationships [VERIFIED FROM SOURCE CODE - TeamProfileModel.cs]
- Asset IDs (emblem, color, background) [VERIFIED FROM SOURCE CODE - TeamProfileModel.cs]
- Hall of Fame status [VERIFIED FROM SOURCE CODE - TeamProfileModel.cs]

**SavedMatch**:
- Match data [VERIFIED FROM SOURCE CODE - SavedMatch.cs]
- Round results [VERIFIED FROM SOURCE CODE - SavedMatch.cs]
- Player/team references [VERIFIED FROM SOURCE CODE - SavedMatch.cs]
- Match metadata [VERIFIED FROM SOURCE CODE - SavedMatch.cs]

**ApplicationUserModel**:
- Account identity [VERIFIED FROM SOURCE CODE - ApplicationUserModel.cs]
- Role assignment [VERIFIED FROM SOURCE CODE - ApplicationUserModel.cs]
- Session data [VERIFIED FROM SOURCE CODE - ApplicationUserModel.cs]

### GalleryEngine Models [INFERRED - from code analysis of GalleryEngine/Models folder]

**PlayerOwnedStoreItem**:
- Ownership record [VERIFIED FROM SOURCE CODE - PlayerOwnedStoreItem.cs]
- Equipment state [VERIFIED FROM SOURCE CODE - PlayerOwnedStoreItem.cs]
- Acquisition metadata [VERIFIED FROM SOURCE CODE - PlayerOwnedStoreItem.cs]
- Expiration tracking [VERIFIED FROM SOURCE CODE - PlayerOwnedStoreItem.cs]

**TeamOwnedAssetItem**:
- Team ownership record [VERIFIED FROM SOURCE CODE - TeamOwnedAssetItem.cs]
- Asset type classification [VERIFIED FROM SOURCE CODE - TeamOwnedAssetItem.cs]
- Default/owned distinction [VERIFIED FROM SOURCE CODE - TeamOwnedAssetItem.cs]

**PlayerWalletModel**:
- Currency balances [VERIFIED FROM SOURCE CODE - PlayerWalletModel.cs]
- Transaction timestamp [VERIFIED FROM SOURCE CODE - PlayerWalletModel.cs]

**TeamIdentityModel**:
- Resolved team visual identity [VERIFIED FROM SOURCE CODE - TeamIdentityModel.cs]
- Asset paths [VERIFIED FROM SOURCE CODE - TeamIdentityModel.cs]
- Customization flags [VERIFIED FROM SOURCE CODE - TeamIdentityModel.cs]

### Model Rules [INFERRED - from code analysis of Models folder]

1. **Records preferred**: Use records for immutable data
2. **Validation**: Normalize data in services, not models
3. **Serialization**: Models must be JSON-serializable
4. **Identity**: Include ID fields for all entities
5. **Defaults**: Provide sensible default values

---

## Navigation Rules

### Shell Navigation Foundation

AppShell is the navigation foundation. All navigation must go through Shell routes.

### Navigation Flow

**MainPage** (Hub):
- Entry point for all features
- Team selection and match setup
- Navigation to all pages

**Page Navigation**:
- Use `Shell.Current.GoToAsync()` for navigation
- Use route-based navigation where possible
- Avoid direct page instantiation

### Page Categories

**Player Pages**:
- PlayerProfilesPage - Player list and management
- PlayerDetailsPage - Individual player details

**Team Pages**:
- CreateTeamPage - Team creation/editing
- RankingsPage - Team rankings
- HallOfFamePage - Hall of legends

**Match Pages**:
- GamePage - Match recording
- HistoryPage - Match history
- MatchDetailsPage - Match details

**Gallery Pages**:
- GalleryPage - Store/gallery
- GalleryEngine Admin Pages - Developer tools

**Utility Pages**:
- CertificatePage - Certificate generation
- CertificatePrintPage - Certificate printing
- StatisticsPage - Statistics
- RulesPage - Game rules
- DeveloperLoginPage - Developer access
- HonorsAdminPage - Honors management

### Navigation Rules [INFERRED - from code analysis of Pages folder and AppShell]

1. **Shell-based**: Use Shell navigation
2. **Route parameters**: Pass IDs as route parameters
3. **Back navigation**: Preserve back stack
4. **Modal pages**: Use for dialogs/sheets
5. **Deep linking**: Support deep links where applicable [UNKNOWN - not observed in current implementation]

---

## Data Ownership [INFERRED - from code analysis of service file ownership]

### Service Ownership

Each service owns its domain:

- **ApplicationUserService** → `application_users.json`, `current_user_session.json` [VERIFIED FROM SOURCE CODE - ApplicationUserService.cs]
- **PlayerProfileService** → `players.json` [VERIFIED FROM SOURCE CODE - PlayerProfileService.cs]
- **TeamProfileService** → `teams.json` [VERIFIED FROM SOURCE CODE - TeamProfileService.cs]
- **RankingService** → `rankings.json`, `rivalries.json` [VERIFIED FROM SOURCE CODE - RankingService.cs]
- **GameService** → `matches.json` [VERIFIED FROM SOURCE CODE - GameService.cs]
- **PlayerInventoryService** → `player_owned_assets.json` [VERIFIED FROM SOURCE CODE - PlayerInventoryService.cs]
- **TeamAssetInventoryService** → `team_owned_assets.json` [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]
- **PlayerWalletService** → `player_wallets.json` [VERIFIED FROM SOURCE CODE - PlayerWalletService.cs]
- **StorePurchaseService** → `store_purchases.json` [VERIFIED FROM SOURCE CODE - StorePurchaseService.cs]
- **StoreAssetCatalogService** → `store_catalog.json` (via admin services) [INFERRED - from admin services]

### Cross-Service Rules [INFERRED - from architectural principles]

1. **No direct file access**: Services must use owning service for data [INFERRED - from service pattern]
2. **Event-based sync**: Use AppEvents for cross-service communication [VERIFIED FROM SOURCE CODE - docs/10_APP_EVENTS_SYNC.md]
3. **Identity passing**: Pass IDs, not full objects [INFERRED - from service method signatures]
4. **Service boundaries**: Respect service ownership boundaries [INFERRED - from service organization]

### Data Isolation [INFERRED - from architectural principles]

- **Account isolation**: Use ApplicationUserId for account-scoped data [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]
- **Player isolation**: Use PlayerId for player-scoped data [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- **Team isolation**: Use TeamId for team-scoped data [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]
- **Developer isolation**: Developer data must not leak to normal accounts [VERIFIED FROM SOURCE CODE - docs/13_KNOWN_BUGS_AND_PHASE_2_8.md]

---

## Error Handling Philosophy [INFERRED - from code analysis of error handling patterns]

### Core Principles [INFERRED - from error handling patterns in services]

1. **Never crash on missing data**: Return safe defaults [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]
2. **Never crash on corrupt JSON**: Return empty collections [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]
3. **Log errors**: Use logging for diagnostics [UNKNOWN - logging system not observed]
4. **User-friendly messages**: Show Arabic error messages to users [UNKNOWN - error message system not observed]
5. **Graceful degradation**: Provide fallback behavior [INFERRED - from error handling patterns]

### Error Handling Patterns [INFERRED - from code analysis]

**File Operations**:
```csharp
try {
    return await LoadAsync();
} catch {
    return new(); // Safe default
}
```

**Validation**:
```csharp
if (string.IsNullOrWhiteSpace(playerId))
    throw new ArgumentException("PlayerId is required.");
```

**Null Coalescing**:
```csharp
var result = value ?? defaultValue;
```

### Exception Rules [INFERRED - from code analysis of exception usage]

1. **ArgumentException**: For invalid arguments [VERIFIED FROM SOURCE CODE - PlayerInventoryService.cs]
2. **ArgumentNullException**: For null arguments [INFERRED - from validation patterns]
3. **InvalidOperationException**: For invalid state [UNKNOWN - not observed]
4. **IOException**: For file operations (catch and return default) [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]
5. **JsonException**: For JSON parsing (catch and return default) [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]

---

## Build Safety Rules [VERIFIED FROM SOURCE CODE - docs/05_EXECUTION_CONTRACT.md]

### Build Requirements

1. **Debug build**: Use for development verification [VERIFIED FROM SOURCE CODE - docs/05_EXECUTION_CONTRACT.md]
2. **Release build**: Use for final Android runtime checks [VERIFIED FROM SOURCE CODE - docs/05_EXECUTION_CONTRACT.md]
3. **Warning resolution**: Fix relevant warnings in identity, JSON, image, or platform code [VERIFIED FROM SOURCE CODE - docs/05_EXECUTION_CONTRACT.md]
4. **Fast Deployment**: Be aware of potential native marker crashes [VERIFIED FROM SOURCE CODE - docs/05_EXECUTION_CONTRACT.md]

### Build Verification [INFERRED - from docs/05_EXECUTION_CONTRACT.md general workflow]

After logical changes:
1. Build the project
2. Fix compile errors
3. Address warnings
4. Test on target platform (Android for UI changes)

### Build Artifacts [INFERRED - from DominoMajlisPRO.csproj]

- **Android APK**: `com.companyname.dominomajlispro.apk`
- **iOS**: Not currently supported in build [UNKNOWN - not verified from csproj]
- **Windows**: Supported for development

---

## Backward Compatibility Policy [INFERRED - from code analysis of migration logic in services]

### Data Migration

1. **Version fields**: Include version fields in models where needed [INFERRED - from PlayerEngine.CurrentProfileVersion]
2. **Migration logic**: Implement migration in service load methods [VERIFIED FROM SOURCE CODE - TeamProfileService.cs ID generation]
3. **Legacy fallback**: Support name-based lookup for legacy data [VERIFIED FROM SOURCE CODE - TeamProfileService.cs GetTeamAsync]
4. **Default values**: Provide defaults for new fields [VERIFIED FROM SOURCE CODE - PlayerEngine.Normalize]

### Legacy Support [INFERRED - from code analysis of legacy handling]

- **Name-based lookup**: Allowed for legacy data only [VERIFIED FROM SOURCE CODE - TeamProfileService.cs]
- **ID generation**: Auto-generate IDs for legacy records [VERIFIED FROM SOURCE CODE - TeamProfileService.cs]
- **Field migration**: Migrate old field names to new ones [UNKNOWN - not observed in current implementation]
- **Graceful upgrade**: Handle missing fields gracefully [VERIFIED FROM SOURCE CODE - PlayerEngine.Normalize]

### Breaking Changes [INFERRED - general software engineering practice]

Breaking changes require:
1. Data migration script [UNKNOWN - no migration scripts observed]
2. Version bump [UNKNOWN - versioning system not observed]
3. User notification [UNKNOWN - notification system not observed]
4. Testing on real data [INFERRED - from docs/12_RUNTIME_VERIFICATION.md]

---

## Future Extension Rules [INFERRED - from general architectural principles]

### Adding New Features

1. **Analyze first**: Understand existing architecture [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
2. **Extend services**: Prefer extending existing services [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
3. **Add events**: Add AppEvents for new data flows [INFERRED - from AppEvents pattern]
4. **Update models**: Add new model fields with defaults [INFERRED - from model pattern]
5. **Document changes**: Update this constitution [INFERRED - from documentation practice]

### Adding New Services

1. **Check existing**: Ensure no duplicate service exists [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
2. **Follow patterns**: Use static service pattern [INFERRED - from existing service pattern]
3. **Add events**: Integrate with AppEvents [INFERRED - from AppEvents pattern]
4. **Add tests**: Add verification logic [UNKNOWN - no unit tests observed]
5. **Document**: Add to service reference [INFERRED - from docs/15_SERVICE_REFERENCE.md]

### Adding New Pages

1. **Check navigation**: Ensure route exists [INFERRED - from navigation pattern]
2. **Follow layout**: Use existing layout patterns [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]
3. **Subscribe events**: Subscribe to relevant AppEvents [INFERRED - from page pattern]
4. **Handle lifecycle**: Implement OnAppearing/OnDisappearing [VERIFIED FROM SOURCE CODE - MainPage.xaml.cs]
5. **Test navigation**: Verify back navigation works [INFERRED - from navigation pattern]

### Adding New Asset Types

1. **Update catalog**: Add to StoreProductAssetTypeCatalog [INFERRED - from InventoryRouter pattern]
2. **Update router**: Add routing logic to InventoryRouter [VERIFIED FROM SOURCE CODE - InventoryRouter.cs]
3. **Update resolver**: Add resolution logic [INFERRED - from resolver pattern]
4. **Update admin**: Add admin editor if needed [INFERRED - from admin pattern]
5. **Test ownership**: Verify ownership scoping [VERIFIED FROM SOURCE CODE - docs/09_INVENTORY_ASSET_OWNERSHIP.md]

---

## Non-Negotiable Rules [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]

1. **Analyze before coding**: Never code without understanding [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
2. **Preserve Shell navigation**: Do not bypass Shell [INFERRED - from docs/00_READ_FIRST.md]
3. **Preserve MVVM-style separation**: Pages are views, services are viewmodels [INFERRED - from docs/00_READ_FIRST.md]
4. **Preserve XAML layout**: Do not redesign without explicit request [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]
5. **Use ID keys**: Always use IDs for identity operations [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
6. **Treat names as display-only**: Never use names as keys [VERIFIED FROM SOURCE CODE - docs/07_IDENTITY_ARCHITECTURE.md]
7. **Build after logical changes**: Always build after code changes [VERIFIED FROM SOURCE CODE - docs/05_EXECUTION_CONTRACT.md]
8. **Runtime verify**: Test on Android for UI/identity/store changes [VERIFIED FROM SOURCE CODE - docs/12_RUNTIME_VERIFICATION.md]
9. **Report honestly**: Do not report completion with known crashes [VERIFIED FROM SOURCE CODE - docs/05_EXECUTION_CONTRACT.md]
10. **Minimal changes**: Prefer minimal fixes over rewrites [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]

---

## Completion Definition [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]

A task is complete when:
1. Requested behavior is verified [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
2. Code builds without errors [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
3. Relevant warnings are addressed [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
4. Runtime verification passes (when applicable) [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
5. No known critical regressions remain [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
6. Documentation is updated (if needed) [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]

A task is NOT complete when:
1. Code only builds [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
2. Known crashes exist [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
3. Verification failures remain [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
4. Architecture is violated [VERIFIED FROM SOURCE CODE - docs/02_ENGINEERING_CONSTITUTION.md]
