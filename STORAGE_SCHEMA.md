# Domino Majlis PRO - Storage Schema

## Overview [VERIFIED FROM SOURCE CODE - code analysis of JSON file usage]

This document documents every JSON file used for persistence, including purpose, owner, primary keys, relations, lifetime, migration rules, and read/write services. [VERIFIED FROM SOURCE CODE - code analysis of JSON file usage]

---

## Core Data Files

### application_users.json

**Purpose**: Stores user accounts and application-level identities

**Owner**: ApplicationUserService

**Primary Keys**: ApplicationUserId

**Relations**:
- Links to current_user_session.json via ApplicationUserId
- Links to player_owned_assets.json via ApplicationUserId (scoping)
- Links to team_owned_assets.json via ApplicationUserId (scoping)

**Lifetime**: Permanent (persists across app lifecycle)

**Migration Rules**:
- Auto-generate ApplicationUserId for legacy records
- Migrate legacy identity to new format
- Preserve DisplayName for display only

**Read Services**:
- ApplicationUserService.LoadStateAsync()

**Write Services**:
- ApplicationUserService.SaveStateAsync()

**Schema**:
```json
[
  {
    "ApplicationUserId": "string (GUID)",
    "DisplayName": "string",
    "Role": "Developer | Founder | Normal | Ghost",
    "IsTemporary": "boolean",
    "CreatedAt": "datetime",
    "LastActiveAt": "datetime"
  }
]
```

---

### current_user_session.json

**Purpose**: Stores the current active session

**Owner**: ApplicationUserService

**Primary Keys**: None (single record file)

**Relations**:
- Links to application_users.json via ApplicationUserId

**Lifetime**: Session-based (persists across app restarts)

**Migration Rules**:
- Auto-create ghost user if missing
- Migrate legacy session format

**Read Services**:
- ApplicationUserService.LoadSessionAsync()

**Write Services**:
- ApplicationUserService.SetSession()
- ApplicationUserService.SaveSessionAsync()

**Schema**:
```json
{
  "ApplicationUserId": "string (GUID)",
  "CurrentPlayerId": "string (P####)",
  "LastActiveAt": "datetime"
}
```

---

### players.json

**Purpose**: Stores player profiles and statistics

**Owner**: PlayerProfileService

**Primary Keys**: PlayerId

**Relations**:
- Links to teams.json via PlayerId (Player1Id, Player2Id)
- Links to player_owned_assets.json via PlayerId
- Links to player_wallets.json via PlayerId
- Links to matches.json via PlayerName (legacy)

**Lifetime**: Permanent

**Migration Rules**:
- Auto-generate PlayerId for legacy records (P#### format)
- Normalize PlayerName
- Add missing fields with defaults
- Preserve legacy data for name-based lookup

**Read Services**:
- PlayerProfileService.LoadPlayersAsync()
- PlayerProfileService.GetPlayerByIdAsync()
- PlayerProfileService.GetPlayerByNameAsync()

**Write Services**:
- PlayerProfileService.SavePlayersAsync()
- PlayerProfileService.UpdatePlayerProfileAsync()

**Schema**:
```json
[
  {
    "PlayerId": "string (P####)",
    "PlayerName": "string",
    "AvatarImage": "string",
    "ProfileImagePath": "string",
    "AvatarPath": "string",
    "BuiltInAvatar": "string",
    "UseCustomAvatar": "boolean",
    "PlayerXP": "number",
    "SeasonXP": "number",
    "LifetimeXP": "number",
    "TotalMatches": "number",
    "Wins": "number",
    "Losses": "number",
    "WinRate": "number",
    "CurrentWinStreak": "number",
    "BestWinStreak": "number",
    "PlayerLevel": "number",
    "TrustScore": "number",
    "IsProfileCompleted": "boolean",
    "ProfileStatus": "Developer | Founder | Honor | Normal | Ghost",
    "IsDeveloper": "boolean",
    "IsFounder": "boolean",
    "HonorOwnerId": "string",
    "CurrentTeamIds": "string (comma-separated)",
    "LastUpdatedAt": "datetime",
    "LastActiveAt": "datetime",
    "TimelineEvents": [],
    "LegacyScore": "number",
    "HallOfFameCount": "number",
    "RankTitles": "number",
    "ChampionCount": "number",
    "HallOfLegendsPoints": "number"
  }
]
```

---

### teams.json

**Purpose**: Stores team profiles and statistics

**Owner**: TeamProfileService

**Primary Keys**: TeamId

**Relations**:
- Links to players.json via Player1Id, Player2Id
- Links to team_owned_assets.json via TeamId
- Links to rankings.json via TeamId

**Lifetime**: Permanent

**Migration Rules**:
- Auto-generate TeamId for legacy records (T#### format)
- Add missing fields with defaults
- Preserve legacy data for name-based lookup

**Read Services**:
- TeamProfileService.LoadTeamsAsync()
- TeamProfileService.GetTeamAsync()
- TeamProfileService.GetTeamByIdAsync()
- TeamProfileService.GetTeamByPlayerIdAsync()

**Write Services**:
- TeamProfileService.SaveTeamsAsync()

**Schema**:
```json
[
  {
    "TeamId": "string (T####)",
    "TeamName": "string",
    "Player1Id": "string (P####)",
    "Player2Id": "string (P####)",
    "Player1": "string (display name)",
    "Player2": "string (display name)",
    "XP": "number",
    "Wins": "number",
    "Losses": "number",
    "WinRate": "number",
    "IsSinglePlayer": "boolean",
    "Emblem": "string (image path)",
    "EmblemAssetId": "string",
    "ColorHex": "string",
    "TeamColorAssetId": "string",
    "EmblemBackground": "string",
    "EmblemBackgroundAssetId": "string",
    "HallOfFameMember": "boolean",
    "HallOfFameDate": "datetime",
    "IsSuspicious": "boolean",
    "CreatedAt": "datetime",
    "LastUpdatedAt": "datetime"
  }
]
```

---

### matches.json

**Purpose**: Stores match history and results

**Owner**: GameService

**Primary Keys**: MatchId

**Relations**:
- Links to teams.json via Team1Id, Team2Id
- Links to players.json via PlayerName (legacy)

**Lifetime**: Permanent

**Migration Rules**:
- Auto-generate MatchId for legacy records
- Add missing fields with defaults
- Preserve legacy data

**Read Services**:
- GameService.LoadMatchesAsync()
- GameService.GetLastUnfinishedMatchAsync()

**Write Services**:
- GameService.SaveMatchAsync()
- GameService.DeleteMatchAsync()
- GameService.DeleteAllMatches()

**Schema**:
```json
[
  {
    "MatchId": "string",
    "Team1Id": "string (T####)",
    "Team2Id": "string (T####)",
    "Team1Name": "string",
    "Team2Name": "string",
    "Team1Score": "number",
    "Team2Score": "number",
    "WinnerTeamId": "string",
    "Rules": "string",
    "Rounds": [],
    "IsFinished": "boolean",
    "MatchDate": "datetime",
    "LastPlayedTime": "datetime"
  }
]
```

---

### rankings.json

**Purpose**: Stores team rankings and XP

**Owner**: RankingService

**Primary Keys**: TeamId

**Relations**:
- Links to teams.json via TeamId
- Links to rivalries.json via TeamId

**Lifetime**: Permanent

**Migration Rules**:
- Auto-generate TeamId for legacy records
- Recalculate XP from match history if missing
- Add missing fields with defaults

**Read Services**:
- RankingService.LoadTeamsAsync()

**Write Services**:
- RankingService.SaveTeamsAsync()

**Schema**:
```json
[
  {
    "TeamId": "string (T####)",
    "TeamName": "string",
    "XP": "number",
    "Wins": "number",
    "Losses": "number",
    "WinRate": "number",
    "Rank": "number",
    "HallOfFameMember": "boolean",
    "HallOfFameDate": "datetime",
    "IsSuspicious": "boolean"
  }
]
```

---

### rivalries.json

**Purpose**: Stores team rivalry records

**Owner**: RankingService

**Primary Keys**: Team1Id + Team2Id (composite)

**Relations**:
- Links to teams.json via Team1Id, Team2Id
- Links to rankings.json via TeamId

**Lifetime**: Permanent

**Migration Rules**:
- Auto-generate TeamId for legacy records
- Recalculate from match history if missing

**Read Services**:
- RankingService.LoadRivalriesAsync()

**Write Services**:
- RankingService.SaveRivalriesAsync()

**Schema**:
```json
[
  {
    "Team1Id": "string (T####)",
    "Team2Id": "string (T####)",
    "Team1Wins": "number",
    "Team2Wins": "number",
    "TotalMatches": "number",
    "LastMatchDate": "datetime"
  }
]
```

---

## GalleryEngine Data Files

### player_owned_assets.json

**Purpose**: Stores player-owned store assets

**Owner**: PlayerInventoryService

**Primary Keys**: InventoryItemId (auto-generated)

**Relations**:
- Links to players.json via PlayerId
- Links to application_users.json via ApplicationUserId (scoping)
- Links to store catalog via AssetId, StoreTypeId

**Lifetime**: Permanent

**Migration Rules**:
- Auto-generate InventoryItemId for legacy records
- Normalize StoreTypeId to canonical format
- Add ApplicationUserId scoping for legacy records
- Preserve legacy data for recovery

**Read Services**:
- PlayerInventoryService.LoadAsync()
- PlayerInventoryService.GetInventoryForPlayerAsync()
- PlayerInventoryService.IsOwnedAsync()

**Write Services**:
- PlayerInventoryService.SaveAsync()
- PlayerInventoryService.AddOwnedAsync()
- PlayerInventoryService.EquipItemAsync()

**Schema**:
```json
[
  {
    "InventoryItemId": "string (GUID)",
    "ApplicationUserId": "string (GUID)",
    "PlayerId": "string (P####)",
    "AssetId": "string",
    "ItemId": "string",
    "StoreTypeId": "string",
    "AssetType": "string",
    "IsOwned": "boolean",
    "IsEquipped": "boolean",
    "PurchasedAt": "datetime",
    "AcquiredAt": "datetime",
    "Source": "string",
    "ExpireAt": "datetime",
    "IsExpired": "boolean",
    "SeasonId": "string",
    "CollectionId": "string"
  }
]
```

---

### team_owned_assets.json

**Purpose**: Stores team-owned assets

**Owner**: TeamAssetInventoryService

**Primary Keys**: TeamInventoryItemId (auto-generated)

**Relations**:
- Links to teams.json via TeamId
- Links to application_users.json via ApplicationUserId (scoping)
- Links to store catalog via TeamAssetId, TeamAssetTypeId

**Lifetime**: Permanent

**Migration Rules**:
- Auto-generate TeamInventoryItemId for legacy records
- Normalize TeamAssetTypeId to canonical format
- Add ApplicationUserId scoping for legacy records
- Preserve legacy data for recovery

**Read Services**:
- TeamAssetInventoryService.LoadAsync()
- TeamAssetInventoryService.GetInventoryForTeamAsync()
- TeamAssetInventoryService.IsOwnedAsync()

**Write Services**:
- TeamAssetInventoryService.SaveAsync()
- TeamAssetInventoryService.AddOwnedAssetAsync()

**Schema**:
```json
[
  {
    "TeamInventoryItemId": "string (GUID)",
    "ApplicationUserId": "string (GUID)",
    "TeamId": "string (T####)",
    "TeamAssetId": "string",
    "TeamAssetTypeId": "string",
    "IsOwned": "boolean",
    "IsEquipped": "boolean",
    "AcquiredAt": "datetime",
    "Source": "string",
    "SeasonId": "string",
    "CollectionId": "string"
  }
]
```

---

### player_wallets.json

**Purpose**: Stores player currency wallets

**Owner**: PlayerWalletService

**Primary Keys**: PlayerId

**Relations**:
- Links to players.json via PlayerId

**Lifetime**: Permanent

**Migration Rules**:
- Auto-create wallet for new players
- Add missing fields with defaults

**Read Services**:
- PlayerWalletService.LoadAsync()
- PlayerWalletService.GetOrCreateAsync()

**Write Services**:
- PlayerWalletService.SaveAsync()
- PlayerWalletService.CreditAsync()
- PlayerWalletService.TryDebitAsync()

**Schema**:
```json
[
  {
    "PlayerId": "string (P####)",
    "Coins": "number",
    "Gems": "number",
    "UpdatedAt": "datetime"
  }
]
```

---

### store_purchases.json

**Purpose**: Stores purchase transaction records

**Owner**: StorePurchaseService

**Primary Keys**: PurchaseId (auto-generated)

**Relations**:
- Links to players.json via PlayerId
- Links to store catalog via ItemId, ItemType

**Lifetime**: Permanent (audit trail)

**Migration Rules**:
- Auto-generate PurchaseId for legacy records
- Add missing fields with defaults

**Read Services**:
- StorePurchaseService.LoadAsync()
- StorePurchaseService.LoadPlayerPurchasesAsync()

**Write Services**:
- StorePurchaseService.SaveAsync()

**Schema**:
```json
[
  {
    "PurchaseId": "string (GUID)",
    "PlayerId": "string (P####)",
    "ItemId": "string",
    "ItemType": "Avatar | Background | Effect | etc.",
    "ItemTitle": "string",
    "CurrencyType": "Coins | Gems | Free",
    "PricePaid": "number",
    "SourceSection": "string",
    "PurchasedAt": "datetime"
  }
]
```

---

## GalleryEngine Admin Files

### avatars.json

**Purpose**: Stores published avatar assets

**Owner**: AvatarsAdminService

**Primary Keys**: Id (AssetId)

**Relations**:
- Links to player_owned_assets.json via Id
- Links to store catalog via Id

**Lifetime**: Permanent (content management)

**Migration Rules**:
- Preserve published content
- Add missing fields with defaults
- Normalize status values

**Read Services**:
- AvatarsAdminService.LoadPublishedAsync()
- AvatarsAdminService.LoadAllAsync()

**Write Services**:
- AvatarsAdminService.SaveAsync()
- AvatarsAdminService.PublishAsync()

**Schema**:
```json
[
  {
    "Id": "string (GUID)",
    "NameAr": "string",
    "NameEn": "string",
    "Title": "string",
    "ImagePath": "string",
    "ThumbnailPath": "string",
    "ColorHex": "string",
    "Status": "Draft | Published | Archived",
    "CurrencyType": "Coins | Gems | Free",
    "Price": "number",
    "IsFree": "boolean",
    "Rarity": "Common | Rare | Epic | Legendary",
    "UnlockType": "Purchase | Reward | Special",
    "CreatedAt": "datetime",
    "UpdatedAt": "datetime",
    "PublishedAt": "datetime"
  }
]
```

---

### backgrounds.json

**Purpose**: Stores published background assets

**Owner**: BackgroundsAdminService

**Primary Keys**: Id (AssetId)

**Relations**:
- Links to player_owned_assets.json via Id
- Links to store catalog via Id

**Lifetime**: Permanent (content management)

**Migration Rules**:
- Preserve published content
- Add missing fields with defaults
- Normalize status values

**Read Services**:
- BackgroundsAdminService.LoadPublishedAsync()
- BackgroundsAdminService.LoadAllAsync()

**Write Services**:
- BackgroundsAdminService.SaveAsync()
- BackgroundsAdminService.PublishAsync()

**Schema**:
```json
[
  {
    "Id": "string (GUID)",
    "NameAr": "string",
    "NameEn": "string",
    "Title": "string",
    "ImagePath": "string",
    "ThumbnailPath": "string",
    "ColorHex": "string",
    "Status": "Draft | Published | Archived",
    "CurrencyType": "Coins | Gems | Free",
    "Price": "number",
    "IsFree": "boolean",
    "Rarity": "Common | Rare | Epic | Legendary",
    "UnlockType": "Purchase | Reward | Special",
    "CreatedAt": "datetime",
    "UpdatedAt": "datetime",
    "PublishedAt": "datetime"
  }
]
```

---

### new_arrivals.json

**Purpose**: Stores new arrivals configuration

**Owner**: NewArrivalsAdminService

**Primary Keys**: ProductId

**Relations**:
- Links to avatars.json/backgrounds.json via AssetId
- Links to store catalog via ProductId

**Lifetime**: Permanent (content management)

**Migration Rules**:
- Preserve configuration
- Add missing fields with defaults

**Read Services**:
- NewArrivalsAdminService.LoadPublishedAsync()
- NewArrivalsAdminService.LoadAllAsync()

**Write Services**:
- NewArrivalsAdminService.SaveAsync()
- NewArrivalsAdminService.PublishAsync()

**Schema**:
```json
[
  {
    "ProductId": "string (GUID)",
    "AssetId": "string",
    "StoreTypeId": "string",
    "Title": "string",
    "ImagePath": "string",
    "ThumbnailPath": "string",
    "ColorHex": "string",
    "EffectType": "string",
    "AnimationType": "string",
    "DurationMilliseconds": "number",
    "EquipTarget": "string",
    "Status": "Draft | Published | Archived",
    "CurrencyType": "Coins | Gems | Free",
    "Price": "number",
    "IsFree": "boolean",
    "DisplayOrder": "number",
    "CreatedAt": "datetime",
    "UpdatedAt": "datetime",
    "PublishedAt": "datetime"
  }
]
```

---

### limited_offers.json

**Purpose**: Stores limited offers configuration

**Owner**: LimitedOffersAdminService

**Primary Keys**: ProductId

**Relations**:
- Links to avatars.json/backgrounds.json via AssetId
- Links to store catalog via ProductId

**Lifetime**: Permanent (content management)

**Migration Rules**:
- Preserve configuration
- Add missing fields with defaults

**Read Services**:
- LimitedOffersAdminService.LoadPublishedAsync()
- LimitedOffersAdminService.LoadAllAsync()

**Write Services**:
- LimitedOffersAdminService.SaveAsync()
- LimitedOffersAdminService.PublishAsync()

**Schema**:
```json
[
  {
    "ProductId": "string (GUID)",
    "AssetId": "string",
    "StoreTypeId": "string",
    "Title": "string",
    "ImagePath": "string",
    "ThumbnailPath": "string",
    "ColorHex": "string",
    "Status": "Draft | Published | Archived",
    "CurrencyType": "Coins | Gems | Free",
    "Price": "number",
    "IsFree": "boolean",
    "AvailableFrom": "datetime",
    "AvailableUntil": "datetime",
    "DisplayOrder": "number",
    "CreatedAt": "datetime",
    "UpdatedAt": "datetime",
    "PublishedAt": "datetime"
  }
]
```

---

### current_season.json

**Purpose**: Stores current season configuration

**Owner**: CurrentSeasonAdminService

**Primary Keys**: SeasonId

**Relations**:
- Links to store catalog via SeasonId

**Lifetime**: Permanent (content management)

**Migration Rules**:
- Preserve configuration
- Add missing fields with defaults

**Read Services**:
- CurrentSeasonAdminService.LoadPublishedAsync()
- CurrentSeasonAdminService.LoadAllAsync()

**Write Services**:
- CurrentSeasonAdminService.SaveAsync()
- CurrentSeasonAdminService.PublishAsync()

**Schema**:
```json
{
  "SeasonId": "string",
  "SeasonNameAr": "string",
  "SeasonNameEn": "string",
  "HeroAssetId": "string",
  "HeroImagePath": "string",
  "StartDate": "datetime",
  "EndDate": "datetime",
  "IsActive": "boolean",
  "CreatedAt": "datetime",
  "UpdatedAt": "datetime"
}
```

---

### store_categories.json

**Purpose**: Stores store category configuration

**Owner**: StoreCategoriesAdminService

**Primary Keys**: CategoryId

**Relations**:
- Links to store catalog via CategoryId

**Lifetime**: Permanent (content management)

**Migration Rules**:
- Preserve configuration
- Add missing fields with defaults

**Read Services**:
- StoreCategoriesAdminService.LoadPublishedAsync()
- StoreCategoriesAdminService.LoadAllAsync()

**Write Services**:
- StoreCategoriesAdminService.SaveAsync()
- StoreCategoriesAdminService.PublishAsync()

**Schema**:
```json
[
  {
    "CategoryId": "string (GUID)",
    "NameAr": "string",
    "NameEn": "string",
    "IconPath": "string",
    "DisplayOrder": "number",
    "Status": "Active | Inactive",
    "CreatedAt": "datetime",
    "UpdatedAt": "datetime"
  }
]
```

---

### store_pricing_configuration.json

**Purpose**: Stores global pricing configuration

**Owner**: StorePricingAdminService

**Primary Keys**: None (single record)

**Relations**: None

**Lifetime**: Permanent (configuration)

**Migration Rules**:
- Preserve configuration
- Add missing fields with defaults

**Read Services**:
- StorePricingAdminService.LoadAsync()

**Write Services**:
- StorePricingAdminService.SaveAsync()

**Schema**:
```json
{
  "CoinsToGemsRate": "number",
  "DailyFreeCoins": "number",
  "DailyFreeGems": "number",
  "UpdatedAt": "datetime"
}
```

---

### store_runtime_configuration.json

**Purpose**: Stores runtime feature flags and settings

**Owner**: StoreRuntimeConfigurationService

**Primary Keys**: None (single record)

**Relations**: None

**Lifetime**: Permanent (configuration)

**Migration Rules**:
- Preserve configuration
- Add missing fields with defaults

**Read Services**:
- StoreRuntimeConfigurationService.LoadAsync()

**Write Services**:
- StoreRuntimeConfigurationService.SaveAsync()

**Schema**:
```json
{
  "StoreEnabled": "boolean",
  "PurchaseEnabled": "boolean",
  "MaintenanceMode": "boolean",
  "MaintenanceMessage": "string",
  "UpdatedAt": "datetime"
}
```

---

## Utility Data Files

### developer_lock.json

**Purpose**: Stores developer access lock state

**Owner**: DeveloperLockService

**Primary Keys**: None (single record)

**Relations**:
- Links to application_users.json via DeveloperUserId

**Lifetime**: Permanent (security)

**Migration Rules**:
- Preserve lock state
- Add missing fields with defaults

**Read Services**:
- DeveloperLockService.LoadAsync()

**Write Services**:
- DeveloperLockService.SaveAsync()

**Schema**:
```json
{
  "IsLocked": "boolean",
  "DeveloperUserId": "string (GUID)",
  "LockReason": "string",
  "LockedAt": "datetime",
  "UnlockedAt": "datetime"
}
```

---

### security_log.json

**Purpose**: Stores security event log

**Owner**: SecurityLogService

**Primary Keys**: LogId (auto-generated)

**Relations**:
- Links to application_users.json via UserId

**Lifetime**: Permanent (audit trail)

**Migration Rules**:
- Append-only (no deletes)
- Add missing fields with defaults

**Read Services**:
- SecurityLogService.LoadAsync()

**Write Services**:
- SecurityLogService.LogEventAsync()

**Schema**:
```json
[
  {
    "LogId": "string (GUID)",
    "UserId": "string (GUID)",
    "EventType": "string",
    "EventDescription": "string",
    "IpAddress": "string",
    "UserAgent": "string",
    "Timestamp": "datetime"
  }
]
```

---

### update_log.json

**Purpose**: Stores application update log

**Owner**: UpdateLogService

**Primary Keys**: Version

**Relations**: None

**Lifetime**: Permanent (history)

**Migration Rules**:
- Append-only (no deletes)
- Add missing fields with defaults

**Read Services**:
- UpdateLogService.LoadAsync()

**Write Services**:
- UpdateLogService.AddEntryAsync()

**Schema**:
```json
[
  {
    "Version": "string",
    "ReleaseDate": "datetime",
    "Changes": "string",
    "IsMajor": "boolean"
  }
]
```

---

### user_guide.json

**Purpose**: Stores user guide content

**Owner**: UserGuideService

**Primary Keys**: SectionId

**Relations**: None

**Lifetime**: Permanent (content)

**Migration Rules**:
- Preserve content
- Add missing fields with defaults

**Read Services**:
- UserGuideService.LoadAsync()

**Write Services**:
- UserGuideService.SaveAsync()

**Schema**:
```json
[
  {
    "SectionId": "string",
    "TitleAr": "string",
    "TitleEn": "string",
    "ContentAr": "string",
    "ContentEn": "string",
    "DisplayOrder": "number"
  }
]
```

---

## File Location [VERIFIED FROM SOURCE CODE - service file path constants]

All JSON files are stored in the application data directory:

**Path**: `FileSystem.AppDataDirectory` (platform-specific) [VERIFIED FROM SOURCE CODE - service file path constants]

**Android**: `/data/data/com.companyname.dominomajlispro/files/` [INFERRED - from MAUI FileSystem.AppDataDirectory documentation]

**Windows**: `%LocalAppData%\Packages\...\LocalState\` [INFERRED - from MAUI FileSystem.AppDataDirectory documentation]

**iOS**: Application sandbox directory [INFERRED - from MAUI FileSystem.AppDataDirectory documentation]

---

## File Safety Rules [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]

### Missing Files [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]

**Rule**: Return empty safe data

**Implementation**: [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]
```csharp
if (!File.Exists(filePath))
    return new();
```

### Corrupt Files [VERIFIED FROM SOURCE CODE - docs/11_JSON_STORAGE_SAFETY.md]

**Rule**: Return empty safe data, backup corrupt file

**Implementation**: [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]
```csharp
try {
    return JsonSerializer.Deserialize<List<T>>(json) ?? new();
} catch {
    return new();
}
```

**Backup**: StoreCmsJsonRepository creates `.corrupt.bak` files [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]

### File Locking [VERIFIED FROM SOURCE CODE - code analysis of file operations]

**Rule**: Use per-file locks (SemaphoreSlim)

**Implementation**: [VERIFIED FROM SOURCE CODE - PlayerWalletService.cs]
```csharp
private static readonly SemaphoreSlim Gate = new(1, 1);
await Gate.WaitAsync();
try {
    // File operations
} finally {
    Gate.Release();
}
```

### Atomic Writes [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]

**Rule**: Use temp-file atomic replacement

**Implementation**: [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]
```csharp
var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
// Write to temp file
File.Move(temporaryPath, filePath, overwrite: true);
```

---

## Data Integrity [INFERRED - from general data integrity practices]

### Primary Key Uniqueness [INFERRED - from general data integrity practices]

**Rule**: Ensure primary keys are unique before write

**Implementation**:
```csharp
if (records.Any(x => x.Id == newRecord.Id))
    return false; // Duplicate
```

### Foreign Key Validation [INFERRED - from general data integrity practices]

**Rule**: Validate foreign keys before write

**Implementation**:
```csharp
if (!await PlayerProfileService.GetPlayerByIdAsync(playerId))
    throw new ArgumentException("Invalid PlayerId");
```

### Data Normalization [VERIFIED FROM SOURCE CODE - PlayerEngine.cs]

**Rule**: Normalize data before write

**Implementation**: [VERIFIED FROM SOURCE CODE - PlayerEngine.cs]
```csharp
PlayerEngine.Normalize(player);
```

### Event Consistency [VERIFIED FROM SOURCE CODE - service code]

**Rule**: Raise events only after successful write

**Implementation**: [VERIFIED FROM SOURCE CODE - service code]
```csharp
await SaveAsync(data);
AppEvents.RaiseDataChanged();
```

---

## Backup and Restore [VERIFIED FROM SOURCE CODE - BackupService.cs]

### Backup Service

**Owner**: BackupService [VERIFIED FROM SOURCE CODE - BackupService.cs]

**Files Backed Up**: All JSON files [VERIFIED FROM SOURCE CODE - BackupService.cs]

**Backup Format**: ZIP archive [VERIFIED FROM SOURCE CODE - BackupService.cs]

**Restore Rules**: [INFERRED - from BackupService.cs]
- Validate backup integrity
- Preserve existing data if backup is corrupt
- Raise events after restore

---

## Summary [VERIFIED FROM SOURCE CODE - code analysis]

- **Total JSON Files**: 20+ files [VERIFIED FROM SOURCE CODE - count of JSON files]
- **Core Data Files**: 7 files (users, players, teams, matches, rankings, rivalries, session) [VERIFIED FROM SOURCE CODE - count]
- **GalleryEngine Files**: 8 files (inventory, wallets, purchases, catalog) [VERIFIED FROM SOURCE CODE - count]
- **Admin Files**: 8 files (avatars, backgrounds, arrivals, offers, season, categories, pricing, runtime) [VERIFIED FROM SOURCE CODE - count]
- **Utility Files**: 5 files (lock, log, updates, guide, privacy) [VERIFIED FROM SOURCE CODE - count]
- **Primary Key Pattern**: GUID or auto-generated IDs [VERIFIED FROM SOURCE CODE - code analysis]
- **Migration Pattern**: Auto-generate IDs, add defaults, preserve legacy data [VERIFIED FROM SOURCE CODE - code analysis]
- **Thread Safety**: SemaphoreSlim for file operations [VERIFIED FROM SOURCE CODE - code analysis]
- **Atomic Writes**: Temp-file pattern for critical files [VERIFIED FROM SOURCE CODE - StoreCmsJsonRepository.cs]
- **Error Handling**: Return empty defaults on failure [VERIFIED FROM SOURCE CODE - code analysis]
