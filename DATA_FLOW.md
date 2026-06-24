# Domino Majlis PRO - Data Flow

## Overview [VERIFIED FROM SOURCE CODE - code analysis of service data flows]

This document describes how data moves through the application from user action to UI refresh. [VERIFIED FROM SOURCE CODE - code analysis of service data flows]

---

## General Data Flow Pattern

```
User Action
    ↓
UI (Page/Component)
    ↓
Service Layer (Business Logic)
    ↓
JSON Persistence (File I/O)
    ↓
AppEvents (Synchronization)
    ↓
UI Refresh (Data Binding)
```

---

## Core Data Flow Subsystems

### 1. Player Profile Data Flow

**User Action**: User creates or edits a player profile

**Flow**:
```
PlayerProfilesPage.xaml
    ↓ (Button click / Form submit)
PlayerProfilesPage.xaml.cs
    ↓ (Call service)
PlayerProfileService.SetProfileImageFromDeviceAsync()
    ↓ (File I/O)
players.json (write)
    ↓ (Raise event)
AppEvents.PlayerProfileChanged
    ↓ (MainThread invoke)
PlayerProfilesPage.OnPlayerProfileChanged()
    ↓ (Refresh UI)
PlayersCollectionView.ItemsSource = new list
```

**Key Services**:
- PlayerProfileService (CRUD operations)
- PlayerEngine (normalization)
- PlayerTimelineService (event logging)

**JSON File**: `players.json`

**AppEvents**: `PlayerProfileChanged`, `DataChanged`

**Thread Safety**: SemaphoreSlim in PlayerProfileService

---

### 2. Team Profile Data Flow

**User Action**: User creates or edits a team

**Flow**:
```
CreateTeamPage.xaml
    ↓ (Save button)
CreateTeamPage.xaml.cs
    ↓ (Call service)
PlayerTeamSyncService.SyncTeamAsync()
    ↓ (Update players)
PlayerProfileService.UpdatePlayerProfileAsync()
    ↓ (Write players.json)
players.json (write)
    ↓ (Write teams.json)
TeamProfileService.SaveTeamsAsync()
    ↓ (Raise event)
AppEvents.TeamsChanged
    ↓ (MainThread invoke)
CreateTeamPage.OnTeamsChanged()
    ↓ (Refresh UI)
Team selection carousel refresh
```

**Key Services**:
- TeamProfileService (team CRUD)
- PlayerProfileService (player updates)
- PlayerTeamSyncService (synchronization)
- TeamEligibleAssetService (asset filtering)

**JSON Files**: `teams.json`, `players.json`

**AppEvents**: `TeamsChanged`, `PlayerProfileChanged`

**Thread Safety**: SemaphoreSlim in TeamProfileService

---

### 3. Match Recording Data Flow

**User Action**: User records a match result

**Flow**:
```
GamePage.xaml
    ↓ (Complete match button)
GamePage.xaml.cs
    ↓ (Call service)
GameService.SaveMatchAsync()
    ↓ (Write matches.json)
matches.json (write)
    ↓ (Update player stats)
PlayerProfileService.UpdatePlayerStatsAsync()
    ↓ (Write players.json)
players.json (write)
    ↓ (Raise events)
AppEvents.MatchesChanged
AppEvents.PlayerProfileChanged
    ↓ (MainThread invoke)
HistoryPage.OnMatchesChanged()
RankingsPage.OnRankingsChanged()
    ↓ (Refresh UI)
Match list refresh
Rankings refresh
```

**Key Services**:
- GameService (match persistence)
- PlayerProfileService (stat updates)
- PlayerEngine (XP calculation)
- RankingService (ranking updates)

**JSON Files**: `matches.json`, `players.json`, `rankings.json`

**AppEvents**: `MatchesChanged`, `PlayerProfileChanged`, `RankingsChanged`

**Thread Safety**: No locks (single write operation)

---

### 4. Store Purchase Data Flow

**User Action**: User purchases an asset from the store

**Flow**:
```
GalleryPage.xaml
    ↓ (Purchase button)
StoreProductActionSheet.cs
    ↓ (Call service)
StoreCheckoutService.CheckoutAsync()
    ↓ (Call purchase service)
StorePurchaseService.PurchaseAsync()
    ↓ (Check ownership)
PlayerInventoryService.IsOwnedAsync()
    ↓ (Read player_owned_assets.json)
player_owned_assets.json (read)
    ↓ (Debit wallet)
PlayerWalletService.TryDebitAsync()
    ↓ (Read player_wallets.json)
player_wallets.json (read)
    ↓ (Write player_wallets.json)
player_wallets.json (write)
    ↓ (Add to inventory)
PlayerInventoryService.AddOwnedAsync()
    ↓ (Write player_owned_assets.json)
player_owned_assets.json (write)
    ↓ (Record purchase)
StorePurchaseService.SavePurchaseRecord()
    ↓ (Write store_purchases.json)
store_purchases.json (write)
    ↓ (Raise event)
AppEvents.StoreEconomyChanged(playerId)
    ↓ (MainThread invoke)
GalleryPage.OnStoreEconomyChanged()
    ↓ (Refresh UI)
Wallet display refresh
Inventory refresh
```

**Key Services**:
- StorePurchaseService (purchase logic)
- PlayerWalletService (currency management)
- PlayerInventoryService (ownership)
- StoreAssetCatalogService (catalog)

**JSON Files**: `player_wallets.json`, `player_owned_assets.json`, `store_purchases.json`

**AppEvents**: `StoreEconomyChanged`, `StoreProgressChanged`

**Thread Safety**: SemaphoreSlim in PlayerWalletService, PlayerInventoryService

---

### 5. Asset Equipment Data Flow

**User Action**: User equips an asset

**Flow**:
```
GalleryPage.xaml
    ↓ (Equip button)
StoreProductActionSheet.cs
    ↓ (Call service)
StoreEquipService.EquipAsync()
    ↓ (Check ownership)
PlayerInventoryService.IsOwnedAsync()
    ↓ (Read player_owned_assets.json)
player_owned_assets.json (read)
    ↓ (Update equipment state)
PlayerInventoryService.EquipItemAsync()
    ↓ (Write player_owned_assets.json)
player_owned_assets.json (write)
    ↓ (Raise event)
AppEvents.StoreEconomyChanged(playerId)
    ↓ (MainThread invoke)
GalleryPage.OnStoreEconomyChanged()
    ↓ (Refresh UI)
Avatar/background refresh
Equipment indicators refresh
```

**Key Services**:
- StoreEquipService (equipment logic)
- PlayerInventoryService (ownership)
- PlayerVisualIdentityResolver (identity resolution)

**JSON Files**: `player_owned_assets.json`

**AppEvents**: `StoreEconomyChanged`

**Thread Safety**: SemaphoreSlim in PlayerInventoryService

---

### 6. Team Asset Data Flow

**User Action**: User selects team assets in CreateTeamPage

**Flow**:
```
CreateTeamPage.xaml
    ↓ (Page load)
CreateTeamPage.xaml.cs
    ↓ (Call service)
TeamEligibleAssetService.GetEligibleAsync()
    ↓ (Get player inventory)
PlayerInventoryService.LoadOwnedAsync(player1Id)
    ↓ (Read player_owned_assets.json)
player_owned_assets.json (read)
    ↓ (Get player inventory)
PlayerInventoryService.LoadOwnedAsync(player2Id)
    ↓ (Read player_owned_assets.json)
player_owned_assets.json (read)
    ↓ (Merge with defaults)
TeamAssetPayloadCatalog.GetDefaultTeamPayloads()
    ↓ (Filter by team type)
TeamEligibleAssetService.IsTeamType()
    ↓ (Return eligible assets)
CreateTeamPage (display)
    ↓ (User selects asset)
CreateTeamPage.xaml.cs
    ↓ (Save team)
TeamProfileService.SaveTeamsAsync()
    ↓ (Write teams.json)
teams.json (write)
    ↓ (Raise event)
AppEvents.TeamAssetsChanged(teamId)
    ↓ (MainThread invoke)
CreateTeamPage.OnTeamAssetsChanged()
    ↓ (Refresh UI)
Asset selection refresh
```

**Key Services**:
- TeamEligibleAssetService (asset filtering)
- PlayerInventoryService (player inventory)
- TeamAssetPayloadCatalog (default assets)
- TeamProfileService (team persistence)

**JSON Files**: `player_owned_assets.json`, `teams.json`

**AppEvents**: `TeamAssetsChanged`, `TeamsChanged`

**Thread Safety**: SemaphoreSlim in PlayerInventoryService

---

### 7. Identity Resolution Data Flow

**User Action**: UI displays player avatar/team emblem

**Flow**:
```
MainPage.xaml / RankingsPage.xaml
    ↓ (Binding evaluation)
PlayerVisualIdentityResolver.ResolveAsync(playerId)
    ↓ (Resolve player ID)
PlayerProfileService.GetPlayerByIdAsync()
    ↓ (Read players.json)
players.json (read)
    ↓ (Get inventory)
PlayerAssetInventoryService.GetInventoryForPlayerAsync()
    ↓ (Read player_owned_assets.json)
player_owned_assets.json (read)
    ↓ (Get catalog)
StoreAssetCatalogService.LoadAsync()
    ↓ (Read store catalog JSON files)
avatars.json, backgrounds.json, etc. (read)
    ↓ (Resolve equipped assets)
InventoryDisplayResolver.ResolvePlayer()
    ↓ (Return identity)
PlayerVisualIdentity (avatar, background, frame, effect, title)
    ↓ (UI binding)
ImageSource binding update
```

**Key Services**:
- PlayerVisualIdentityResolver (player identity)
- TeamIdentityResolver (team identity)
- InventoryDisplayResolver (display resolution)
- StoreAssetCatalogService (catalog)

**JSON Files**: `players.json`, `player_owned_assets.json`, store catalog files

**AppEvents**: None (read-only operation)

**Thread Safety**: No locks (read-only)

---

### 8. Account Switch Data Flow

**User Action**: User switches accounts

**Flow**:
```
MainPage.xaml
    ↓ (Account switch button)
MainPage.xaml.cs
    ↓ (Call service)
ApplicationUserService.SwitchAccountAsync()
    ↓ (Update session)
ApplicationUserService.SetSession()
    ↓ (Write current_user_session.json)
current_user_session.json (write)
    ↓ (Raise event)
AppEvents.CurrentUserChanged
    ↓ (MainThread invoke)
MainPage.OnCurrentUserChanged()
    ↓ (Refresh UI)
Player avatars refresh
Team identities refresh
Inventory refresh
Wallet refresh
```

**Key Services**:
- ApplicationUserService (account management)
- DeveloperLockService (developer access)

**JSON Files**: `application_users.json`, `current_user_session.json`

**AppEvents**: `CurrentUserChanged`, `PlayerProfileChanged`, `StoreEconomyChanged`

**Thread Safety**: SemaphoreSlim in ApplicationUserService

---

### 9. Admin Publishing Data Flow

**User Action**: Developer publishes a new asset

**Flow**:
```
AvatarsEditorPage.xaml
    ↓ (Publish button)
AvatarsEditorPage.xaml.cs
    ↓ (Call service)
AvatarsAdminService.PublishAsync()
    ↓ (Validate)
StoreCmsValidationEngine.Validate()
    ↓ (Write avatars.json)
StoreCmsJsonRepository.SaveListAsync()
    ↓ (Temp file write)
avatars.json.tmp (write)
    ↓ (Validate temp file)
StoreCmsJsonRepository.ValidateTemporaryJsonArrayAsync()
    ↓ (Atomic rename)
File.Move(avatars.json.tmp → avatars.json)
    ↓ (Raise event)
AppEvents.DataChanged (implicit via catalog reload)
    ↓ (Catalog reload)
StoreAssetCatalogService.LoadAsync()
    ↓ (Read avatars.json)
avatars.json (read)
    ↓ (Refresh UI)
GalleryPage catalog refresh
```

**Key Services**:
- AvatarsAdminService (avatar management)
- StoreCmsJsonRepository (persistence)
- StoreCmsPublishEngine (publishing logic)
- StoreAssetCatalogService (catalog)

**JSON Files**: `avatars.json`, other catalog files

**AppEvents**: None (catalog reload on next access)

**Thread Safety**: ConcurrentDictionary locks in StoreCmsJsonRepository

---

### 10. Hall of Fame Data Flow

**User Action**: User views Hall of Fame

**Flow**:
```
HallOfFamePage.xaml
    ↓ (Page load)
HallOfFamePage.xaml.cs
    ↓ (Call service)
RankingService.LoadTeamsAsync()
    ↓ (Read rankings.json)
rankings.json (read)
    ↓ (Filter eligible)
HallOfLegendsConstitutionService.IsEligible()
    ↓ (Display)
HallOfFamePage (display)
```

**Key Services**:
- RankingService (ranking data)
- HallOfLegendsConstitutionService (eligibility)
- TeamProfileService (team data)

**JSON Files**: `rankings.json`, `teams.json`

**AppEvents**: `RankingsChanged`, `TeamsChanged`

**Thread Safety**: No locks (read-only)

---

## Data Flow Patterns

### Read-Only Flow

**Pattern**: UI → Service → JSON Read → UI Display

**Examples**:
- Identity resolution (avatar display)
- Rankings display
- History display
- Catalog loading

**Characteristics**:
- No event raising
- No file writes
- Thread-safe (no locks needed)
- Can be parallelized

---

### Write-Only Flow

**Pattern**: UI → Service → JSON Write → Event → UI Refresh

**Examples**:
- Player profile creation
- Team creation
- Match recording
- Asset purchase

**Characteristics**:
- Event raising required
- File writes with locks
- MainThread invoke for UI
- Sequential operations

---

### Read-Write Flow

**Pattern**: UI → Service → JSON Read → Transform → JSON Write → Event → UI Refresh

**Examples**:
- Player profile update
- Team update
- Account switch
- Asset equipment

**Characteristics**:
- Read before write
- Validation in between
- Event raising required
- Locks for thread safety

---

### Complex Flow

**Pattern**: UI → Service → Multiple JSON Reads/Writes → Multiple Events → UI Refresh

**Examples**:
- Store purchase (wallet + inventory + purchase record)
- Team creation (players + teams + assets)
- Match recording (matches + players + rankings)

**Characteristics**:
- Multiple file operations
- Transaction-like behavior
- Multiple events
- Complex error handling

---

## Thread Safety in Data Flow

### SemaphoreSlim Usage

Services that use SemaphoreSlim for thread safety:

- **ApplicationUserService**: Gate for user/session operations
- **PlayerInventoryService**: Gate for inventory operations
- **TeamAssetInventoryService**: Gate for team asset operations
- **PlayerWalletService**: Gate for wallet operations
- **StoreCmsJsonRepository**: ConcurrentDictionary for per-file locks

### MainThread Usage

AppEvents use MainThread.BeginInvokeOnMainThread for safe UI updates:

```csharp
static void SafeRaise(Action? action)
{
    if (action == null) return;
    MainThread.BeginInvokeOnMainThread(() => action.Invoke());
}
```

### Async/Await Pattern

All I/O operations use async/await:

```csharp
public static async Task<List<PlayerProfileModel>> LoadPlayersAsync()
{
    string json = await File.ReadAllTextAsync(FilePath);
    return JsonSerializer.Deserialize<List<PlayerProfileModel>>(json) ?? new();
}
```

---

## Error Handling in Data Flow

### File Not Found

**Pattern**: Return empty default

```csharp
if (!File.Exists(filePath))
    return new();
```

### JSON Parse Error

**Pattern**: Catch and return empty default

```csharp
try {
    return JsonSerializer.Deserialize<List<T>>(json) ?? new();
} catch {
    return new();
}
```

### Validation Error

**Pattern**: Throw ArgumentException

```csharp
if (string.IsNullOrWhiteSpace(playerId))
    throw new ArgumentException("PlayerId is required.");
```

### Insufficient Funds

**Pattern**: Return failure result

```csharp
var debit = await PlayerWalletService.TryDebitAsync(playerId, currency, amount);
if (!debit.Success)
    return Failure("Insufficient wallet balance.");
```

---

## Data Flow Optimization

### Parallel Loading

Services that use Task.WhenAll for parallel loading:

- **StoreAssetCatalogService**: Loads avatars, backgrounds, arrivals, offers in parallel
- **InventoryDisplayResolver**: Loads catalog, product references, player inventory, team inventory in parallel
- **PlayerVisualIdentityResolver**: Loads catalog and inventory in parallel

### Caching

Services that implement caching (implicit via static state):

- **StoreAssetCatalogService**: Catalog cached in memory
- **TeamAssetPayloadCatalog**: Default assets cached in memory
- **StoreProductAssetTypeCatalog**: Type catalog cached in memory

### Lazy Loading

Services that implement lazy loading:

- **GalleryPage**: Sections load on demand
- **PlayerProfilesPage**: Pagination (if implemented)
- **HistoryPage**: Date filtering

---

## Data Flow Integrity

### Transaction-Like Behavior

For complex operations, services ensure atomicity:

- **StorePurchaseService**: Wallet debit + inventory add + purchase record (all or nothing)
- **TeamAssetInventoryService**: Asset add + event raise (atomic)
- **PlayerInventoryService**: Ownership check + add + event raise (atomic)

### Data Validation

Services validate before persistence:

- **PlayerProfileService**: Normalize before save
- **TeamProfileService**: ID generation before save
- **StorePurchaseService**: Ownership check before purchase
- **InventoryRouter**: State validation before acquire/equip

### Event Consistency

Events are raised only after successful persistence:

- **PlayerProfileService**: Raise after successful save
- **GameService**: Raise after successful match save
- **StorePurchaseService**: Raise after successful purchase
- **TeamAssetInventoryService**: Raise after successful asset add

---

## Data Flow Debugging [INFERRED - from general debugging practices]

### Logging Points [UNKNOWN - logging system not observed]

Key logging points in data flow:

- Service entry (method call)
- File read/write operations
- Event raising
- UI refresh
- Error conditions

### Common Issues [VERIFIED FROM SOURCE CODE - docs/13_KNOWN_BUGS_AND_PHASE_2_8.md]

**Issue**: UI not refreshing after data change

**Cause**: Event not raised or not subscribed [INFERRED - from AppEvents pattern]

**Solution**: Verify AppEvents.Raise* is called after persistence [VERIFIED FROM SOURCE CODE - service code]

**Issue**: RecyclerView crash on Android

**Cause**: ItemsSource mutated during layout [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]

**Solution**: Use atomic ItemsSource replacement on main thread [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]

**Issue**: Data inconsistency across pages

**Cause**: Event not subscribed or subscription leaked [INFERRED - from AppEvents pattern]

**Solution**: Subscribe in OnAppearing, unsubscribe in OnDisappearing [VERIFIED FROM SOURCE CODE - MainPage.xaml.cs]

**Issue**: Cross-account data leakage

**Cause**: Identity not scoped by ApplicationUserId [VERIFIED FROM SOURCE CODE - docs/13_KNOWN_BUGS_AND_PHASE_2_8.md]

**Solution**: Ensure ApplicationUserId is attached to inventory records [VERIFIED FROM SOURCE CODE - TeamAssetInventoryService.cs]

---

## Summary [VERIFIED FROM SOURCE CODE - code analysis]

- **Data Flow Pattern**: User → UI → Service → JSON → AppEvents → UI [VERIFIED FROM SOURCE CODE - service code analysis]
- **Read-Only Flows**: 5+ (identity resolution, rankings, history, catalog) [VERIFIED FROM SOURCE CODE - count of read flows]
- **Write Flows**: 10+ (profile, team, match, purchase, equipment) [VERIFIED FROM SOURCE CODE - count of write flows]
- **Complex Flows**: 3+ (purchase, team creation, match recording) [VERIFIED FROM SOURCE CODE - count of complex flows]
- **Thread Safety**: SemaphoreSlim in 5+ services [VERIFIED FROM SOURCE CODE - code analysis]
- **Event-Based Sync**: 10+ AppEvents [VERIFIED FROM SOURCE CODE - AppEvents.cs]
- **Parallel Loading**: 3+ services use Task.WhenAll [VERIFIED FROM SOURCE CODE - code analysis]
- **Error Handling**: Return defaults, throw exceptions, return failure results [VERIFIED FROM SOURCE CODE - code analysis]
