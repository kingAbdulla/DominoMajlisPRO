# Domino Majlis PRO - ApplicationUserId Enforcement Fix Plan

**Issue**: FIX_PRIORITY.md #2 - Fix ApplicationUserId Enforcement in PlayerInventoryService  
**Phase**: Phase 1 - Immediate Critical Fixes  
**Risk Level**: CRITICAL  
**User Impact**: HIGH (cross-user inventory leaks, security vulnerability)  
**Estimated Hours**: 5  
**Status**: ANALYSIS COMPLETE - AWAITING APPROVAL

---

## Executive Summary

**Root Cause**: `PlayerInventoryService` hardcodes `ApplicationUserId = string.Empty` when adding player-owned items, violating the identity-first architecture principle. This breaks account isolation and creates a security vulnerability where inventory items cannot be traced to specific application users.

**Current State**: 
- `PlayerInventoryService.AddOwnedItemAsync()` and related methods set `ApplicationUserId = string.Empty`
- Other services (`StorePurchaseService`, `TeamAssetInventoryService`) correctly enforce ApplicationUserId
- Legacy data normalization also clears ApplicationUserId

**Proposed Fix**: Modify `PlayerInventoryService` to always resolve and set `ApplicationUserId` from the current session before adding inventory items, while preserving ApplicationUserId during normalization/merge operations.

---

## Exact Root Cause

### Location 1: PlayerInventoryService.cs - Line 43

```csharp
private static async Task<bool> AddOwnedItemCoreAsync(string playerId, string assetId, string storeTypeId, string source, DateTime? expireAt, string? seasonId, string? collectionId, bool raiseEvent)
{
    ValidateIdentity(playerId, assetId);
    var added = await AddOwnedAsync(new PlayerOwnedStoreItem
    {
        ApplicationUserId = string.Empty,  // ❌ VIOLATION: Should be set from current session
        PlayerId = playerId,
        AssetId = assetId,
        ...
    });
```

**Problem**: When adding a new owned item, `ApplicationUserId` is hardcoded to `string.Empty` instead of being resolved from the current session.

---

### Location 2: PlayerInventoryService.cs - Line 161

```csharp
private static PlayerOwnedStoreItem MergeOwnership(IGrouping<string, PlayerOwnedStoreItem> group)
{
    var records = group.OrderByDescending(x => x.IsEquipped).ThenByDescending(x => x.PurchasedAt).ToList();
    var merged = records[0];
    merged.ApplicationUserId = string.Empty;  // ❌ VIOLATION: Should preserve from source records
    merged.IsOwned = records.Any(x => x.IsOwned);
    ...
}
```

**Problem**: When merging duplicate inventory records, `ApplicationUserId` is cleared instead of preserving it from the source records.

---

### Location 3: PlayerInventoryService.cs - Line 180

```csharp
private static bool Normalize(PlayerOwnedStoreItem item)
{
    var before = $"{item.InventoryItemId}|{item.ApplicationUserId}|{item.PlayerId}|...";
    item.InventoryItemId = string.IsNullOrWhiteSpace(item.InventoryItemId) ? Guid.NewGuid().ToString() : item.InventoryItemId.Trim();
    item.ApplicationUserId = string.Empty;  // ❌ VIOLATION: Should preserve if present
    item.PlayerId = item.PlayerId?.Trim() ?? string.Empty;
    ...
}
```

**Problem**: Normalization clears `ApplicationUserId` instead of preserving it if already present.

---

## Exact Affected Files

### Primary File (Must Modify)
- **`DominoMajlisPRO/GalleryEngine/Services/PlayerInventoryService.cs`**
  - Line 43: `AddOwnedItemCoreAsync` method
  - Line 161: `MergeOwnership` method
  - Line 180: `Normalize` method

### Reference Files (Do NOT Modify)
- **`DominoMajlisPRO/Services/ApplicationUserService.cs`**
  - Provides `EnsureCurrentSessionAsync()` method
  - Provides `GetCurrentStoreOwnerAsync()` method
  - Already correctly implements identity resolution

- **`DominoMajlisPRO/GalleryEngine/Services/StorePurchaseService.cs`**
  - Line 41: Correctly resolves ApplicationUserId before adding inventory
  - Reference implementation for the fix

- **`DominoMajlisPRO/GalleryEngine/Services/TeamAssetInventoryService.cs`**
  - Line 85: Correctly resolves ApplicationUserId for team assets
  - Reference implementation for the fix

- **`DominoMajlisPRO/GalleryEngine/Services/InventoryRouter.cs`**
  - Line 152: Uses `GetCurrentStoreOwnerAsync()` to get identity context
  - Reference for identity resolution pattern

- **`DominoMajlisPRO/GalleryEngine/Services/StoreCheckoutService.cs`**
  - Line 13: Uses `GetCurrentStoreOwnerAsync()` for identity context
  - Reference for identity resolution pattern

### Data Files (Do NOT Modify Schema)
- **`application_users.json`**
  - Stores `ApplicationUserModel` records with `ApplicationUserId`
  - Read-only during fix

- **`current_user_session.json`**
  - Stores session state with `CurrentAccountId` (ApplicationUserId)
  - Read-only during fix

- **`player_owned_assets.json`**
  - Stores `PlayerOwnedStoreItem` records
  - Will be updated by normalization logic after fix

- **`player_owned_store_items.json`** (Legacy)
  - Legacy file, will be migrated by existing logic

---

## Exact Affected Methods

### PlayerInventoryService.cs

#### 1. `AddOwnedItemCoreAsync` (Line 38)
**Current Signature**:
```csharp
private static async Task<bool> AddOwnedItemCoreAsync(
    string playerId, 
    string assetId, 
    string storeTypeId, 
    string source, 
    DateTime? expireAt, 
    string? seasonId, 
    string? collectionId, 
    bool raiseEvent)
```

**Issue**: Does not accept or resolve `ApplicationUserId`

**Fix**: Resolve `ApplicationUserId` from current session before creating item

---

#### 2. `AddOwnedAsync` (Line 64)
**Current Signature**:
```csharp
internal static async Task<bool> AddOwnedAsync(PlayerOwnedStoreItem owned)
```

**Issue**: Accepts `PlayerOwnedStoreItem` but does not validate `ApplicationUserId`

**Fix**: Add validation to ensure `ApplicationUserId` is not empty for owned items

---

#### 3. `MergeOwnership` (Line 157)
**Current Signature**:
```csharp
private static PlayerOwnedStoreItem MergeOwnership(IGrouping<string, PlayerOwnedStoreItem> group)
```

**Issue**: Clears `ApplicationUserId` instead of preserving from source records

**Fix**: Preserve `ApplicationUserId` from the first non-empty source record

---

#### 4. `Normalize` (Line 176)
**Current Signature**:
```csharp
private static bool Normalize(PlayerOwnedStoreItem item)
```

**Issue**: Clears `ApplicationUserId` instead of preserving if present

**Fix**: Preserve `ApplicationUserId` if already present, only trim whitespace

---

## Risk Level

**CRITICAL** - This is a security vulnerability that violates the identity-first architecture principle.

### Security Risks
1. **Cross-user inventory leaks**: Items cannot be traced to specific application users
2. **Privilege escalation**: No way to verify ownership of inventory items
3. **Audit trail broken**: Cannot determine which user acquired which asset
4. **Account isolation violation**: Core architectural principle violated

### Data Integrity Risks
1. **Orphaned inventory**: Items with no owner ApplicationUserId
2. **Migration issues**: Legacy data cannot be properly migrated
3. **Query inconsistencies**: Cannot filter inventory by ApplicationUserId

---

## Proposed Minimal Patch

### Patch 1: Fix `AddOwnedItemCoreAsync` (Line 38-60)

**Before**:
```csharp
private static async Task<bool> AddOwnedItemCoreAsync(string playerId, string assetId, string storeTypeId, string source, DateTime? expireAt, string? seasonId, string? collectionId, bool raiseEvent)
{
    ValidateIdentity(playerId, assetId);
    var added = await AddOwnedAsync(new PlayerOwnedStoreItem
    {
        ApplicationUserId = string.Empty,
        PlayerId = playerId,
        AssetId = assetId,
        ...
    });
```

**After**:
```csharp
private static async Task<bool> AddOwnedItemCoreAsync(string playerId, string assetId, string storeTypeId, string source, DateTime? expireAt, string? seasonId, string? collectionId, bool raiseEvent)
{
    ValidateIdentity(playerId, assetId);
    
    // Resolve ApplicationUserId from current session for identity isolation
    var appUserId = (await ApplicationUserService.EnsureCurrentSessionAsync()).ApplicationUserId ?? string.Empty;
    
    var added = await AddOwnedAsync(new PlayerOwnedStoreItem
    {
        ApplicationUserId = appUserId,
        PlayerId = playerId,
        AssetId = assetId,
        ...
    });
```

**Rationale**: Follows the pattern used in `StorePurchaseService.cs` (line 41) and `TeamAssetInventoryService.cs` (line 85).

---

### Patch 2: Fix `MergeOwnership` (Line 157-174)

**Before**:
```csharp
private static PlayerOwnedStoreItem MergeOwnership(IGrouping<string, PlayerOwnedStoreItem> group)
{
    var records = group.OrderByDescending(x => x.IsEquipped).ThenByDescending(x => x.PurchasedAt).ToList();
    var merged = records[0];
    merged.ApplicationUserId = string.Empty;
    merged.IsOwned = records.Any(x => x.IsOwned);
    ...
}
```

**After**:
```csharp
private static PlayerOwnedStoreItem MergeOwnership(IGrouping<string, PlayerOwnedStoreItem> group)
{
    var records = group.OrderByDescending(x => x.IsEquipped).ThenByDescending(x => x.PurchasedAt).ToList();
    var merged = records[0];
    
    // Preserve ApplicationUserId from source records for identity isolation
    merged.ApplicationUserId = records.Select(r => r.ApplicationUserId).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    
    merged.IsOwned = records.Any(x => x.IsOwned);
    ...
}
```

**Rationale**: Follows the pattern used in `TeamAssetInventoryService.MergeOwnership` (line 384). Preserves the first non-empty ApplicationUserId from source records.

---

### Patch 3: Fix `Normalize` (Line 176-193)

**Before**:
```csharp
private static bool Normalize(PlayerOwnedStoreItem item)
{
    var before = $"{item.InventoryItemId}|{item.ApplicationUserId}|{item.PlayerId}|...";
    item.InventoryItemId = string.IsNullOrWhiteSpace(item.InventoryItemId) ? Guid.NewGuid().ToString() : item.InventoryItemId.Trim();
    item.ApplicationUserId = string.Empty;
    item.PlayerId = item.PlayerId?.Trim() ?? string.Empty;
    ...
}
```

**After**:
```csharp
private static bool Normalize(PlayerOwnedStoreItem item)
{
    var before = $"{item.InventoryItemId}|{item.ApplicationUserId}|{item.PlayerId}|...";
    item.InventoryItemId = string.IsNullOrWhiteSpace(item.InventoryItemId) ? Guid.NewGuid().ToString() : item.InventoryItemId.Trim();
    
    // Preserve ApplicationUserId if present for identity isolation
    item.ApplicationUserId = item.ApplicationUserId?.Trim() ?? string.Empty;
    
    item.PlayerId = item.PlayerId?.Trim() ?? string.Empty;
    ...
}
```

**Rationale**: Follows the pattern used in `TeamAssetInventoryService.Normalize` (line 421). Preserves ApplicationUserId if already present, only trims whitespace.

---

### Patch 4: Add Validation to `AddOwnedAsync` (Line 64-78)

**Before**:
```csharp
internal static async Task<bool> AddOwnedAsync(PlayerOwnedStoreItem owned)
{
    Normalize(owned);
    ValidateIdentity(owned.PlayerId, owned.AssetId);
    await Gate.WaitAsync();
    try
    {
        var records = await LoadAsync();
        if (records.Any(x => Same(x.PlayerId, owned.PlayerId) && Same(x.AssetId, owned.AssetId))) return false;
        records.Add(owned);
        await SaveAsync(records);
        return true;
    }
    finally { Gate.Release(); }
}
```

**After**:
```csharp
internal static async Task<bool> AddOwnedAsync(PlayerOwnedStoreItem owned)
{
    Normalize(owned);
    ValidateIdentity(owned.PlayerId, owned.AssetId);
    
    // Validate ApplicationUserId for identity isolation
    if (owned.IsOwned && string.IsNullOrWhiteSpace(owned.ApplicationUserId))
    {
        throw new InvalidOperationException("ApplicationUserId is required for owned inventory items.");
    }
    
    await Gate.WaitAsync();
    try
    {
        var records = await LoadAsync();
        if (records.Any(x => Same(x.PlayerId, owned.PlayerId) && Same(x.AssetId, owned.AssetId))) return false;
        records.Add(owned);
        await SaveAsync(records);
        return true;
    }
    finally { Gate.Release(); }
}
```

**Rationale**: Enforces that owned items must have an ApplicationUserId, preventing future violations.

---

## Files That Must NOT Be Touched

### ❌ DO NOT MODIFY - Data Schema Files
- `application_users.json` - User account data
- `current_user_session.json` - Session state
- `player_owned_assets.json` - Inventory data (will be updated by normalization, not manual edit)
- `player_owned_store_items.json` - Legacy inventory data
- `team_owned_assets.json` - Team inventory data
- `players.json` - Player profile data
- `teams.json` - Team profile data

### ❌ DO NOT MODIFY - UI/XAML Files
- Any `.xaml` files
- Any `.xaml.cs` files (except for testing if needed)

### ❌ DO NOT MODIFY - Other Services
- `ApplicationUserService.cs` - Already correctly implements identity resolution
- `TeamAssetInventoryService.cs` - Already correctly implements ApplicationUserId
- `StorePurchaseService.cs` - Already correctly implements ApplicationUserId
- `InventoryRouter.cs` - Already correctly uses identity context
- `StoreCheckoutService.cs` - Already correctly uses identity context
- `PlayerProfileService.cs` - Not related to inventory ApplicationUserId

### ❌ DO NOT MODIFY - Models
- `PlayerOwnedStoreItem.cs` - Model structure is correct
- `ApplicationUserModel.cs` - Model structure is correct
- `CurrentUserSessionModel.cs` - Model structure is correct

### ❌ DO NOT MODIFY - Backup Files
- Any `.bak` files (these will be removed in Phase 1 #1)
- Any `.tmp` files (these will be removed in Phase 1 #1)

---

## Build/Test Plan

### Pre-Implementation Checks
1. **Backup current state**
   - Copy `player_owned_assets.json` to `player_owned_assets.json.backup`
   - Copy `player_owned_store_items.json` to `player_owned_store_items.json.backup`

2. **Verify current behavior**
   - Run app and verify inventory can be added (with empty ApplicationUserId)
   - Note any existing inventory items with empty ApplicationUserId

### Implementation Steps
1. **Apply Patch 1**: Modify `AddOwnedItemCoreAsync` to resolve ApplicationUserId
2. **Apply Patch 2**: Modify `MergeOwnership` to preserve ApplicationUserId
3. **Apply Patch 3**: Modify `Normalize` to preserve ApplicationUserId
4. **Apply Patch 4**: Add validation to `AddOwnedAsync`

### Build Verification
1. **Build project** (Debug configuration)
   - Verify no compilation errors
   - Verify no warnings related to the changes

2. **Static analysis**
   - Verify no null reference warnings
   - Verify no async/await warnings

### Runtime Verification
1. **Test inventory addition**
   - Create new player account
   - Add inventory item via store
   - Verify `ApplicationUserId` is set in `player_owned_assets.json`
   - Verify `ApplicationUserId` matches current session

2. **Test inventory normalization**
   - Load existing inventory with empty ApplicationUserId
   - Verify normalization preserves existing ApplicationUserId if present
   - Verify normalization does not clear ApplicationUserId

3. **Test inventory merge**
   - Create duplicate inventory records with different ApplicationUserIds
   - Verify merge preserves ApplicationUserId from source records

4. **Test legacy migration**
   - Load legacy `player_owned_store_items.json`
   - Verify migration populates ApplicationUserId from current session

5. **Test cross-user isolation**
   - Create two different application users
   - Add inventory items for each user
   - Verify inventory is isolated by ApplicationUserId
   - Verify User A cannot see User B's inventory

### Expected Behavior After Fix
- New inventory items will always have `ApplicationUserId` set
- Existing inventory items with `ApplicationUserId` will be preserved
- Legacy inventory items will get `ApplicationUserId` from current session on load
- Validation will prevent adding owned items without `ApplicationUserId`

---

## Rollback Plan

### Rollback Triggers
1. **Build failure**: If project does not compile after changes
2. **Runtime crash**: If app crashes on inventory operations
3. **Data corruption**: If inventory data becomes corrupted
4. **Test failure**: If any runtime verification test fails

### Rollback Steps
1. **Revert code changes**
   - Restore `PlayerInventoryService.cs` from git
   - Verify clean revert

2. **Restore data backups**
   - Copy `player_owned_assets.json.backup` to `player_owned_assets.json`
   - Copy `player_owned_store_items.json.backup` to `player_owned_store_items.json`

3. **Verify rollback**
   - Build project
   - Run app
   - Verify inventory operations work as before

### Rollback Time Estimate
- Code revert: 5 minutes
- Data restore: 5 minutes
- Verification: 10 minutes
- **Total**: 20 minutes

---

## Migration Strategy for Existing Data

### Phase 1: One-Time Migration (On First Load)
When `PlayerInventoryService.LoadAsync()` is called after the fix:

1. **Detect items with empty ApplicationUserId**
   - Query all items where `ApplicationUserId` is empty or whitespace
   - Log count of affected items

2. **Populate ApplicationUserId from current session**
   - For items with empty ApplicationUserId, set to current session's ApplicationUserId
   - This assumes items were acquired by the current user (reasonable assumption for single-user scenarios)

3. **Save updated data**
   - Save normalized inventory to `player_owned_assets.json`
   - Log migration completion

### Phase 2: Ongoing Enforcement
- New items will always have ApplicationUserId set (via Patch 1)
- Normalization will preserve ApplicationUserId (via Patch 3)
- Merge will preserve ApplicationUserId (via Patch 2)
- Validation will prevent empty ApplicationUserId (via Patch 4)

### Migration Risk Assessment
- **Low Risk**: Migration is non-destructive (only adds data, doesn't remove)
- **Safe**: Uses current session as fallback for legacy data
- **Reversible**: Can be rolled back via backup restore

---

## Dependencies

### Depends On
- **Phase 1 #1** (Remove backup files) - Not strictly required, but recommended for clean environment

### Blocks
- **Phase 1 #6** (TeamEffectEngine EquipAsync validation) - Requires ApplicationUserId enforcement to be complete first
- **Phase 1 #26** (TeamAssetInventoryService default assets ownership) - Related to ApplicationUserId handling

---

## Success Criteria

### Code Quality
- ✅ All patches applied without compilation errors
- ✅ No new warnings introduced
- ✅ Code follows existing patterns (StorePurchaseService, TeamAssetInventoryService)

### Functional
- ✅ New inventory items have ApplicationUserId set
- ✅ Existing ApplicationUserId values are preserved
- ✅ Legacy items get ApplicationUserId from current session
- ✅ Validation prevents empty ApplicationUserId for owned items

### Data Integrity
- ✅ No data loss during migration
- ✅ No orphaned inventory items
- ✅ Cross-user inventory isolation enforced

### Security
- ✅ ApplicationUserId enforcement prevents cross-user leaks
- ✅ Audit trail can track which user acquired which asset
- ✅ Account isolation principle restored

---

## Estimated Timeline

| Task | Estimated Time |
|------|----------------|
| Apply Patch 1 | 30 minutes |
| Apply Patch 2 | 30 minutes |
| Apply Patch 3 | 30 minutes |
| Apply Patch 4 | 30 minutes |
| Build verification | 15 minutes |
| Runtime verification | 2 hours |
| Data migration testing | 1 hour |
| Documentation | 30 minutes |
| **Total** | **5 hours** |

---

## Additional Notes

### Why This Fix Is Minimal
- Only modifies `PlayerInventoryService.cs` (single file)
- Only changes 4 methods (targeted scope)
- Follows existing patterns from other services
- No schema changes required
- No UI changes required
- No breaking changes to public API

### Why This Fix Is Safe
- Uses existing `ApplicationUserService.EnsureCurrentSessionAsync()` method
- Follows pattern already used in `StorePurchaseService` and `TeamAssetInventoryService`
- Preserves existing data (doesn't clear ApplicationUserId if present)
- Validation is additive (prevents future violations)
- Migration is non-destructive

### Known Limitations
- Legacy data migration assumes items were acquired by current user (reasonable for single-user scenarios)
- Multi-user scenarios with existing legacy data may require manual review
- Default team assets intentionally have empty ApplicationUserId (this is correct and should not be changed)

---

**Plan Status**: READY FOR IMPLEMENTATION  
**Next Step**: Await user approval to proceed with implementation  
**Approval Required**: Yes - This is a CRITICAL security fix
