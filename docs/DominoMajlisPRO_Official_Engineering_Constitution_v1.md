# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 01
### Authority, Mission, Engineering Law, and Root Architecture

Status: Official Draft Segment  
Generated: 2026-06-20  
Scope: Domino Majlis PRO .NET MAUI source snapshot supplied by the project owner.

---

# 0. How to use this document

This document is one segment of the final single-file constitution:

`DominoMajlisPRO_Official_Engineering_Constitution_v1.md`

When all segments are complete, concatenate them in numerical order:

1. PART 01
2. PART 02
3. PART 03
4. PART 04
5. PART 05
6. PART 06
7. PART 07
8. PART 08
9. PART 09
10. PART 10

The final merged file must be placed at:

`docs/DominoMajlisPRO_Official_Engineering_Constitution_v1.md`

This constitution is intended for:

- ChatGPT
- GitHub Copilot
- Codex
- Cursor
- Claude
- Gemini
- Human developers

No AI or developer may treat the project as understood until this document has been read.

---

# 1. Supreme authority

This constitution is the official engineering authority for Domino Majlis PRO.

If a prompt, tool suggestion, AI-generated patch, refactor proposal, or developer preference conflicts with this constitution, this constitution wins unless the project owner explicitly overrides it.

The project owner is the final architectural authority.

The AI is not the architect.

The AI is an engineering assistant bound by this constitution.

---

# 2. Project identity

Project name:

`Domino Majlis PRO`

Application type:

Professional real-world domino match recording, competition management, identity, ranking, gallery, store, asset, and statistics platform.

Technology:

- .NET MAUI Single Project
- C#
- XAML
- AppShell navigation
- MVVM-oriented service architecture
- JSON-backed local persistence
- Android primary runtime target
- Windows development/debug target
- iOS and MacCatalyst target frameworks present in project configuration

The project is not a video game.

The project records and manages real-world domino activity.

No feature may convert the application into arcade gameplay, simulated domino mechanics, or game-like match automation.

---

# 3. Core mission

Domino Majlis PRO exists to provide a trusted real-world domino competition platform.

Its mission includes:

- Recording real matches.
- Managing players and teams.
- Preserving match history.
- Ranking teams and players.
- Managing identity and visual identity.
- Supporting developer-controlled gallery/store publishing.
- Maintaining Hall of Fame integrity.
- Applying anti-cheat policy based on evidence.
- Supporting player assets and team assets.
- Providing reliable visual identity propagation across pages.
- Preserving data integrity across sessions, accounts, teams, and devices.

Engineering must protect this mission before adding features.

---

# 4. Root engineering values

The following priorities are mandatory and ordered:

1. Data integrity
2. Identity integrity
3. Architecture stability
4. Runtime safety
5. Build correctness
6. AppEvents synchronization correctness
7. Store and inventory correctness
8. PlayerId and TeamId binding correctness
9. User experience stability
10. Performance
11. Visual polish

Visual polish never overrides identity, data, or architecture.

---

# 5. Absolute forbidden behavior

The following actions are forbidden unless the project owner explicitly requests them:

- Rebuilding working pages from scratch.
- Replacing Shell navigation.
- Replacing MVVM/service architecture.
- Replacing JSON persistence.
- Replacing AppEvents with a parallel event system.
- Moving controls in XAML to solve logic problems.
- Redesigning approved pages while fixing bugs.
- Binding identity by display name.
- Binding team identity by team name.
- Binding account role by visible name.
- Treating default assets as purchased assets.
- Treating published store assets as owned by every player.
- Treating device-level inventory as player-level ownership.
- Reporting task completion without build success.
- Reporting phase completion without runtime verification.
- Hiding known remaining defects.
- Claiming emulator verification passed when it was not executed.

---

# 6. Official source tree observed in supplied snapshot

The supplied source snapshot contains the following important root-level project areas:

```text
DominoMajlisPRO/
├── App.xaml
├── App.xaml.cs
├── AppShell.xaml
├── AppShell.xaml.cs
├── MainPage.xaml
├── MainPage.xaml.cs
├── MauiProgram.cs
├── DominoMajlisPRO.csproj
├── Controls/
├── Models/
├── Pages/
├── Services/
├── Storage/
├── ViewModels/
├── GalleryEngine/
├── Resources/
├── Platforms/
├── Localization/
├── Themes/
└── STORE_STATUS.md
```

The source snapshot also contains repository governance files:

```text
.github/
├── copilot-instructions.md
└── constitution/
    ├── 01-project-mission.md
    ├── 02-architecture.md
    ├── 03-layout-protection.md
    └── 04-execution-contract.md
```

These existing documents are part of the historical constitution and are superseded by the final merged constitution once placed under `/docs`.

---

# 7. Primary architectural pillars

The application depends on these architectural pillars:

## 7.1 AppShell navigation

Files:

- `AppShell.xaml`
- `AppShell.xaml.cs`

Rules:

- Preserve AppShell.
- Do not introduce alternative navigation frameworks.
- Do not bypass routing.
- Do not silently change route names or page registration.

## 7.2 MAUI single project structure

File:

- `DominoMajlisPRO.csproj`

Rules:

- Preserve MAUI single-project model.
- Do not split the project into multiple projects unless explicitly approved.
- Do not remove target frameworks casually.
- Do not add platform-specific hacks unless required and isolated.

## 7.3 Services layer

Folder:

- `Services/`

Representative files:

- `ApplicationUserService.cs`
- `AppEvents.cs`
- `DeveloperLockService.cs`
- `GameService.cs`
- `HonorIdentityService.cs`
- `PlayerProfileService.cs`
- `PlayerTeamSyncService.cs`
- `RankingService.cs`
- `TeamProfileService.cs`
- `SecurityLogService.cs`

Rules:

- Business logic belongs in services.
- Pages must not become business logic containers.
- Existing services should be extended before new services are created.
- Duplicate services are forbidden unless explicitly approved.

## 7.4 GalleryEngine subsystem

Folder:

- `GalleryEngine/`

Representative areas:

- `GalleryEngine/Admin/`
- `GalleryEngine/Admin/Core/`
- `GalleryEngine/Admin/Services/`
- `GalleryEngine/Components/StoreSections/`
- `GalleryEngine/Models/`
- `GalleryEngine/Pages/`
- `GalleryEngine/Services/`

Representative services:

- `PlayerInventoryService.cs`
- `PlayerAssetInventoryService.cs`
- `TeamAssetInventoryService.cs`
- `TeamEligibleAssetService.cs`
- `PlayerVisualIdentityResolver.cs`
- `TeamIdentityResolver.cs`
- `InventoryDisplayResolver.cs`
- `StorePurchaseService.cs`
- `StoreEquipService.cs`
- `StoreCheckoutService.cs`
- `PlayerStoreProgressService.cs`

Rules:

- GalleryEngine is the official Gallery/Store subsystem.
- Store publishing, asset acquisition, inventory, equipment, and display resolution must remain separated.
- Gallery display components must not mutate ownership.
- Admin publishing must not grant ownership automatically.

## 7.5 JSON persistence

Representative files/services:

- `Storage/StorageService.cs`
- `GalleryEngine/Admin/Core/StoreCmsJsonRepository.cs`

Rules:

- JSON is the official persistence model.
- Missing files should produce empty/default data where safe.
- Corrupt JSON must not crash read-only display pages.
- Failed save must preserve previous valid JSON.
- Atomic save behavior must be protected.

## 7.6 AppEvents synchronization

File:

- `Services/AppEvents.cs`

Rules:

- AppEvents is the central synchronization bus.
- Do not create a parallel event bus.
- Pages must refresh from authoritative services after relevant AppEvents.
- Event loops must be prevented.
- Events must be raised after state is saved, not before.

---

# 8. Identity law

This is one of the highest laws in the project.

Names are display only.

The following must never be used as primary keys:

- Player name
- Team name
- Account display name
- Developer display name
- Visible label text
- UI picker display string

Authoritative identifiers:

```text
Account identity  => AccountId / ApplicationUserId
Player identity   => PlayerId
Team identity     => TeamId
Asset identity    => AssetId / CanonicalAssetId
Product identity  => ProductId
Purchase identity => PurchaseId
Inventory identity=> InventoryItemId
```

Any code path that uses names for identity must be treated as legacy compatibility only and gradually migrated to ID-first logic.

---

# 9. Player identity rules

Player identity is always bound by `PlayerId`.

The following must bind to `PlayerId`:

- Avatar
- Profile background
- Frame
- Effect
- Title
- Online/offline state
- Player inventory
- Player equipment
- Player visual identity
- Player profile
- Player rank
- Player timeline
- Player achievements
- Player store progress

A player display name may be shown in UI.

A player display name must not determine ownership, equipment, role, or session.

---

# 10. Team identity rules

Team identity is always bound by `TeamId`.

The following must bind to `TeamId`:

- Team emblem
- Team color
- Team emblem background
- Team profile
- Rankings display identity
- Hall of Fame display identity
- Game page team identity
- History team identity
- Match details team identity
- Team statistics

Team name is display-only and historical snapshot text.

Team name must not be used to determine ownership or current team identity when TeamId exists.

---

# 11. Account/session rules

The current session must know:

- CurrentAccountId
- CurrentPlayerId
- CurrentRole

Switching accounts must refresh:

- Role visibility
- Store ownership
- Player inventory
- Equipped player identity
- Online/offline state
- MainPage player slot
- PlayerProfilesPage
- PlayerDetailsPage
- Store pages
- CreateTeamPage eligibility

Developer role is a permission attached to an account/player identity.

Developer activation must not create a second unrelated player with the same display name.

---

# 12. Store ownership law

Store architecture uses three separate concepts:

## 12.1 Published asset/product

Created by Developer/Admin.

Exists in the catalog/store CMS.

Globally visible only according to publish state and visibility.

Not owned by a player until acquired.

Publishing does not equip.

Publishing does not grant ownership.

## 12.2 Owned asset

Created only after purchase, acquisition, grant, reward, or explicit default grant.

Must be bound to `PlayerId`.

Must include AssetId and AssetType.

Must not leak to other players.

## 12.3 Equipped asset

Player-scoped selected asset.

One equipped item per PlayerId + AssetType unless explicitly designed otherwise.

Equipping one Avatar for Player A must not modify Player B.

---

# 13. Default asset law

Default assets are available.

Default assets are not automatically owned.

Default avatars may appear in avatar catalog sections such as:

- All
- Arabs
- Women
- Men
- Royal
- Legendary
- Military
- Sports

Default avatars must not appear in “My Assets / مقتنياتي” as purchased or owned.

Default team emblems/colors/backgrounds may appear in CreateTeamPage as defaults.

Default team assets must not count as player-owned purchases.

Default assets must not inflate collection progress as purchased ownership.

---

# 14. Team asset eligibility law

CreateTeamPage must use this rule:

```text
Available Team Assets =
Default Team Assets
+
Team Assets owned by Player1Id
+
Team Assets owned by Player2Id
```

If only one player exists:

```text
Available Team Assets =
Default Team Assets
+
Team Assets owned by Player1Id
```

Forbidden:

- Showing all published team assets.
- Showing all device-owned team assets.
- Showing assets owned by unrelated players.
- Showing assets because display names match.
- Mutating ownership during team selection.
- Using TeamId as purchase owner.
- Using player/team names for eligibility.

Team assets owned by Abdullah must appear only when Abdullah’s PlayerId is part of the team being created or edited.

---

# 15. Layout protection law

The UI is protected.

Do not redesign XAML while fixing logic.

Protected:

- Grid hierarchy
- RowDefinitions
- ColumnDefinitions
- StackLayout hierarchy
- CollectionView structure
- CarouselView structure
- ContentView hierarchy
- Page structure
- Navigation flow
- Margins
- Padding
- Spacing
- Card structure

Allowed without redesign approval:

- Binding fixes
- Command fixes
- Visibility fixes
- Resolver fixes
- Service integration
- AppEvents integration
- Data-template safe binding fixes
- RecyclerView-safe ItemsSource replacement

---

# 16. RecyclerView safety law

MAUI CollectionView and RecyclerView crashes must be treated as synchronization defects.

Known crash pattern:

```text
Java.Lang.IndexOutOfBoundsException
Inconsistency detected. Invalid item position
MauiRecyclerView
ReorderableItemsViewAdapter
LinearLayoutManager
```

Likely causes:

- Mutating ObservableCollection while CollectionView is measuring.
- Clearing and adding items rapidly.
- Updating ItemsSource off the UI thread.
- Updating collection during selection event.
- Replacing items while modal/editor is open.
- Re-entrant AppEvents refresh.
- Mixed selection state after account/team switch.
- CollectionView receives inconsistent count after async reload.

Required fix pattern:

1. Suppress selection events during reload.
2. Build a new list off-UI.
3. Replace ItemsSource atomically on MainThread.
4. Avoid Clear/Add loops on bound ObservableCollection.
5. Reset selected index/item after ItemsSource swap.
6. Prevent re-entrant reloads with `_isLoading` or `_suppressSelection`.
7. Do not mutate bound collection from background thread.

---

# 17. Build and verification law

No task is complete until:

- Build succeeds.
- Runtime behavior is verified when practical.
- No known critical regression remains.
- Final report states remaining issues honestly.

For Android defects:

- Use emulator or physical device.
- Capture logcat.
- Search for FATAL EXCEPTION.
- Fix root cause.
- Rebuild.
- Reinstall.
- Retest failed step.

Build success alone is not completion.

---

# 18. Phase 2.8 current stabilization context

The current stabilization area includes:

- Identity isolation.
- PlayerId/TeamId binding.
- Store publishing.
- Store acquisition.
- Inventory ownership.
- Equipment.
- Team assets.
- Player assets.
- CreateTeamPage eligibility.
- Android emulator verification.

Known validated progress:

- Default avatars were corrected to stop appearing as owned inventory.
- Android emulator testing found no initial launch crash after signed release APK installation.
- Runtime defects remain around team asset leakage, avatar switching, CreateTeamPage RecyclerView crash, and progress calculation.

Known current defects requiring targeted work:

1. `IndexOutOfBoundsException` in CreateTeamPage when editing team.
2. Team assets leak across accounts/players.
3. Purchased/equipped avatar may not switch correctly after choosing another.
4. Team assets are not counted in progress bar if progress should include team-owned assets.
5. Online/offline state needs account/player isolation verification.
6. CollectionView reload safety must be implemented in CreateTeamPage.

---

# 19. AI operating protocol

Before answering any engineering request, an AI must:

1. Read this constitution.
2. Inspect the real repository implementation.
3. Identify affected files.
4. Preserve protected architecture.
5. Preserve protected layout.
6. Prefer ID-first logic.
7. Build after changes.
8. Request runtime logs when runtime issue is reported.
9. Never invent files or APIs without checking.
10. Never claim completion without evidence.

If the repository is unavailable, the AI must say so.

If build cannot be run, the AI must say so.

If emulator verification cannot be run, the AI must say so.

---

# 20. End of Part 01

This part establishes the supreme authority, identity laws, architecture laws, ownership laws, layout protection, RecyclerView safety, and current Phase 2.8 stabilization context.

Continue with PART 02 for detailed architecture map, subsystem dependencies, and service/page responsibility matrix.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 02
### Complete Architecture Map, Responsibility Matrix, Dependency Rules, and Runtime Flow

Status: Official Draft Segment  
Generated: 2026-06-20  
Scope: Continuation of PART 01.  
Final merge target: `docs/DominoMajlisPRO_Official_Engineering_Constitution_v1.md`

---

# 21. Purpose of Part 02

PART 02 defines the operational architecture map of Domino Majlis PRO.

This part exists so that any AI or developer can understand the project as an interconnected system rather than isolated files.

No engineering task may be started by inspecting only one page when the defect crosses services, inventory, identity, store, AppEvents, or JSON persistence.

---

# 22. High-level system map

Domino Majlis PRO is organized around these system layers:

```text
Application Host
    ↓
AppShell Navigation
    ↓
Pages / Views
    ↓
ViewModels / Page State
    ↓
Services
    ↓
GalleryEngine Services
    ↓
Models
    ↓
JSON Storage
    ↓
AppEvents Synchronization
```

The direction of dependency must remain controlled.

Pages may call services.

Services may read/write models and storage.

Models must not call pages.

Models must not call services.

Storage must not know about UI.

Display-only pages must not mutate inventory.

---

# 23. Application host layer

Primary files:

- `App.xaml`
- `App.xaml.cs`
- `MauiProgram.cs`
- `AppShell.xaml`
- `AppShell.xaml.cs`

Responsibilities:

- Initialize application.
- Register services.
- Register fonts/resources.
- Configure navigation.
- Establish Shell routes.
- Connect the root page structure.
- Apply app-level startup policy.

Rules:

- Do not move business logic into `App.xaml.cs`.
- Do not create session state in UI-only files.
- Do not register duplicate services.
- Do not create alternate navigation containers.
- Do not bypass Shell to solve routing issues.

---

# 24. Page layer

Representative folders/files:

```text
Pages/
MainPage.xaml
MainPage.xaml.cs
CreateTeamPage.xaml
CreateTeamPage.xaml.cs
PlayerProfilesPage.xaml
PlayerProfilesPage.xaml.cs
PlayerDetailsPage.xaml
PlayerDetailsPage.xaml.cs
RankingsPage.xaml
RankingsPage.xaml.cs
HallOfFamePage.xaml
HallOfFamePage.xaml.cs
HistoryPage.xaml
HistoryPage.xaml.cs
MatchDetailsPage.xaml
MatchDetailsPage.xaml.cs
SettingsPage.xaml
DeveloperLoginPage.xaml
CertificatePrintPage.xaml
```

Page responsibilities:

- Present data.
- Bind to models/viewmodels.
- Call services for business operations.
- React to AppEvents.
- Navigate safely.
- Refresh UI from authoritative services.

Page prohibitions:

- Do not own business rules.
- Do not own identity rules.
- Do not own ownership rules.
- Do not calculate inventory.
- Do not calculate team asset eligibility.
- Do not mutate JSON directly.
- Do not become a parallel service.

---

# 25. Page responsibility matrix

## 25.1 MainPage

Purpose:

- Home/dashboard entry.
- Shows current player/session visual state.
- Opens major app areas.
- May show approved avatar slot if it exists.

Rules:

- Must bind visual identity by `CurrentPlayerId`.
- Must not apply ProfileBackground unless an approved profile-card slot exists.
- Must not mutate inventory.
- Must not calculate ownership.
- Must refresh after session/player profile AppEvents.

Protected:

- Layout.
- Navigation cards.
- Existing settings path.

## 25.2 CreateTeamPage

Purpose:

- Create teams.
- Edit teams.
- Select team identity.
- Save team profile by TeamId.

Critical dependencies:

- `TeamProfileService`
- `TeamEligibleAssetService`
- `TeamAssetInventoryService`
- `PlayerInventoryService`
- `PlayerTeamSyncService`
- `TeamIdentityResolver`
- `AppEvents`

Rules:

- Team selection must save by TeamId.
- Player eligibility must use Player1Id/Player2Id.
- Team assets must be filtered by owning PlayerId.
- Defaults are available but not owned.
- Published assets must not automatically appear.
- Must use RecyclerView-safe reload patterns.
- Must not mutate ownership while selecting team identity.

Known risk:

- `Java.Lang.IndexOutOfBoundsException` on edit/team asset reload.
- Fix must be CollectionView/ItemsSource safety, not visual redesign.

## 25.3 PlayerProfilesPage

Purpose:

- Show players.
- Show player avatar/identity.
- Navigate to details.

Rules:

- Resolve visual identity by PlayerId.
- Do not use DisplayName as identity key.
- Refresh after AppEvents.
- Do not mutate inventory.

## 25.4 PlayerDetailsPage

Purpose:

- Show detailed player profile.
- Show equipped avatar/background/frame/effect/title where approved.
- Show player statistics and honors.

Rules:

- Must resolve visual identity through resolver/service.
- Must not display raw file path.
- Must not bypass `InventoryDisplayResolver`.
- Must not mutate inventory.

## 25.5 Gallery/Store pages

Purpose:

- Display published assets/products.
- Allow acquisition/purchase/equip where supported.
- Display store sections.

Rules:

- Published does not mean owned.
- Acquisition writes PlayerId-bound ownership.
- Equip writes PlayerId-bound equipped state.
- Store display does not grant ownership.
- Store page must refresh ownership after session change.

## 25.6 RankingsPage

Purpose:

- Show team ranking data.
- Display team identity.

Rules:

- Ranking calculations are protected.
- Display identity must resolve by TeamId.
- Do not change scoring/ranking formula for visual identity bugs.
- Sync identity by TeamId only.

## 25.7 HallOfFamePage

Purpose:

- Show Hall of Fame eligible/permanent teams.

Rules:

- Hall qualification rules are protected.
- Fraud/integrity policy is protected.
- Display identity must resolve by TeamId.
- Do not change Hall logic while fixing visual identity.

## 25.8 HistoryPage

Purpose:

- Display historical matches.

Rules:

- Historical identity uses saved match snapshots.
- Do not recalculate historical identity from current inventory if snapshot exists.
- Preserve historical team names at match time.

## 25.9 MatchDetailsPage

Purpose:

- Display match details and team/player identities for a saved match.

Rules:

- Use match snapshot first.
- Use TeamId fallback only if historical snapshot is missing.
- Do not mutate current team profile from historical detail page.

## 25.10 GamePage

Purpose:

- Manage match workflow.
- Show team identity.
- Record scores.

Rules:

- Do not change scoring rules while fixing identity.
- Team visual display must bind to TeamId or match snapshot.
- Do not bind active teams by name.

---

# 26. Services layer map

Primary services observed in the snapshot include:

```text
Services/
├── AppEvents.cs
├── ApplicationUserService.cs
├── DeveloperLockService.cs
├── HonorIdentityService.cs
├── PlayerProfileService.cs
├── PlayerTeamSyncService.cs
├── TeamProfileService.cs
├── RankingService.cs
├── GameService.cs
├── MatchService.cs
├── SecurityLogService.cs
├── CertificateService.cs
└── ...
```

GalleryEngine services include:

```text
GalleryEngine/Services/
├── PlayerInventoryService.cs
├── PlayerAssetInventoryService.cs
├── TeamAssetInventoryService.cs
├── TeamEligibleAssetService.cs
├── PlayerVisualIdentityResolver.cs
├── TeamIdentityResolver.cs
├── InventoryDisplayResolver.cs
├── PlayerStoreProgressService.cs
├── StoreCheckoutService.cs
├── StoreEquipService.cs
├── StorePurchaseService.cs
└── ...
```

Admin/store services include:

```text
GalleryEngine/Admin/
├── SpecializedStoreManagerPage.cs
├── Core/StoreCmsJsonRepository.cs
├── Services/StoreCmsPublishEngine.cs
├── Services/StoreCmsAssetPickerService.cs
├── Services/StoreAdminService.cs
└── ...
```

---

# 27. Service responsibility matrix

## 27.1 ApplicationUserService

Purpose:

- Manage current application user/account identity.
- Track ApplicationUserId / AccountId.
- Bind current PlayerId.
- Bind role state.
- Prevent Developer/Normal duplication.

Rules:

- Must prefer existing AccountId/ApplicationUserId.
- Must not create duplicate users by DisplayName.
- Developer role attaches to current account/player where appropriate.
- Account switch must update CurrentAccountId, CurrentPlayerId, CurrentRole.
- Must raise relevant AppEvents after session state changes.

Forbidden:

- Binding role by display name.
- Creating a second player simply because Developer role is activated.
- Sharing inventory state across accounts.

## 27.2 PlayerProfileService

Purpose:

- Load/save player profiles.
- Resolve player identity.
- Update player data.

Rules:

- `PlayerId` is authoritative.
- Name lookup is legacy fallback only.
- Update operations must avoid null/blank identity mutations.
- Visual identity must not be resolved by name when PlayerId exists.

Known risk:

- Incorrect ID-first branch may swallow method body if braces are broken.
- Null/blank playerName handling must return early.

## 27.3 TeamProfileService

Purpose:

- Load/save team profiles.
- Resolve teams.

Rules:

- TeamId-first lookup.
- TeamName fallback for legacy data only.
- Team visual fields must save by TeamId.
- Do not identify active team by TeamName when TeamId exists.

## 27.4 PlayerTeamSyncService

Purpose:

- Synchronize player/team relationships.
- Maintain relationship between players and teams.

Rules:

- Must use PlayerId and TeamId.
- Must not use display name as relationship key.
- Must refresh affected pages through AppEvents.

## 27.5 RankingService

Purpose:

- Calculate/persist ranking.
- Display ranking information.

Rules:

- Ranking formulas are protected.
- Visual identity sync is allowed.
- TeamId should be used for display identity lookup.
- Do not change ranking math while fixing team visual binding.

## 27.6 AppEvents

Purpose:

- Central synchronization mechanism.

Rules:

- Raise after save.
- Do not raise before persistence.
- Avoid loops.
- Do not create parallel events.
- Pages must unsubscribe when disposed if applicable.

Required event domains:

- Player changed.
- Team changed.
- Match changed.
- Ranking changed.
- Store asset changed.
- Inventory changed.
- Equipment changed.
- Session/current user changed.
- Gallery changed.

## 27.7 PlayerInventoryService

Purpose:

- Own player inventory.
- Load PlayerId-bound owned assets.
- Prevent cross-account ownership leakage.

Rules:

- Load owned by PlayerId.
- Save owned by PlayerId.
- Do not include defaults as owned.
- Do not include published global assets as owned.
- Do not return another PlayerId’s inventory.

## 27.8 PlayerAssetInventoryService

Purpose:

- Player asset inventory/catalog/equipment support.

Rules:

- Defaults may be available.
- Defaults must not be marked owned unless explicit grant.
- My Assets uses owned items only.
- Avatar/ProfileBackground/Frame/Effect/Title must be scoped to PlayerId.

## 27.9 TeamAssetInventoryService

Purpose:

- Team-related asset handling.

Rules:

- Team defaults are available.
- Team defaults are not owned.
- Player-purchased team assets must be scoped to PlayerId.
- Team selections save to TeamId.
- Do not make team asset globally visible as owned.

## 27.10 TeamEligibleAssetService

Purpose:

- Determine which team assets are available when creating/editing a team.

Official rule:

```text
Eligible =
Defaults
+
Assets owned by Player1Id
+
Assets owned by Player2Id
```

Rules:

- Do not return unrelated PlayerId assets.
- Do not return all published assets.
- Do not use DisplayName for eligibility.
- Keep legacy fallback only when ID is unavailable.
- Do not mutate ownership.
- Do not save during read-only eligibility checks.

## 27.11 PlayerVisualIdentityResolver

Purpose:

- Resolve player visual identity for display pages.

Rules:

- Resolve by PlayerId.
- Use inventory/equipment/profile data safely.
- Use `InventoryDisplayResolver` for image sources.
- Do not return another player’s equipped identity.
- Do not cache globally without PlayerId key.

## 27.12 TeamIdentityResolver

Purpose:

- Resolve team display identity.

Rules:

- Resolve by TeamId.
- Use current team profile for live displays.
- Use match snapshot for historical displays.
- Do not resolve by TeamName if TeamId exists.

## 27.13 InventoryDisplayResolver

Purpose:

- Convert stored image IDs/paths into safe display ImageSource or equivalent.

Rules:

- Is canonical image gateway.
- Missing images must not crash.
- Android file paths must be safe.
- Do not bypass this resolver with raw `ImageSource.FromFile` from persisted JSON.

## 27.14 StoreCmsJsonRepository

Purpose:

- Read/write store CMS JSON.

Rules:

- Atomic temp saves.
- Per-file lock.
- Directory exists before save.
- Missing file returns empty list.
- Corrupt file does not crash display pages.
- Failed save preserves previous valid JSON.
- No shared `.tmp` race.

## 27.15 SpecializedStoreManagerPage

Purpose:

- Developer/Admin publishing tool.

Rules:

- Must not redesign layout.
- Must generate AssetId automatically for new assets when possible.
- Must deduplicate picker entries by AssetType + AssetId.
- Must reset form after publish.
- Must use controlled selectors.
- Must not allow unsupported AssetType in a manager.

---

# 28. GalleryEngine architecture

GalleryEngine is a subsystem containing:

- Admin publishing tools.
- Store/Gallery models.
- Store section components.
- Player inventory.
- Team inventory.
- Asset resolvers.
- Purchase/acquisition/equip services.
- Store progress.

Core architectural separation:

```text
Published Catalog Asset
        ≠
Owned Inventory Item
        ≠
Equipped Asset
        ≠
Displayed Visual Identity
```

These concepts must never be collapsed.

---

# 29. Store publishing flow

Official publishing flow:

```text
Developer/Admin
    ↓
Store Manager
    ↓
Controlled inputs
    ↓
AssetType validation
    ↓
Auto AssetId generation
    ↓
StoreCmsPublishEngine / StoreAdminService
    ↓
StoreCmsJsonRepository
    ↓
Published catalog JSON
    ↓
AppEvents
    ↓
Store/Gallery UI refresh
```

Publishing must not:

- Grant ownership.
- Equip asset.
- Add item to all inventories.
- Modify unrelated PlayerId.
- Modify TeamId.
- Show GUID/raw ID as display text.

---

# 30. Acquisition flow

Official acquisition flow:

```text
Current session
    ↓
CurrentPlayerId
    ↓
Store item selected
    ↓
Purchase/Acquire service
    ↓
PlayerInventoryService
    ↓
Player-owned inventory JSON
    ↓
IsOwned = true
    ↓
IsEquipped = false unless explicitly equipped by design
    ↓
AppEvents inventory/store changed
```

Rules:

- Must bind to CurrentPlayerId.
- Must not write to Developer inventory unless Developer is current player.
- Must not write to every player.
- Must not use DisplayName.

---

# 31. Equip flow

Official equip flow:

```text
CurrentPlayerId
    ↓
Owned asset selected
    ↓
Validate ownership
    ↓
Unequip other assets of same AssetType for same PlayerId
    ↓
Equip selected asset for same PlayerId
    ↓
Update profile visual fields where approved
    ↓
Save
    ↓
Raise AppEvents
    ↓
Refresh display pages
```

Rules:

- One Avatar per PlayerId.
- One ProfileBackground per PlayerId unless otherwise designed.
- One Frame per PlayerId unless otherwise designed.
- One Title per PlayerId unless otherwise designed.
- Effect policy must be explicit; default is one if multi-effect is not supported.

Known current risk:

- Purchased avatar may not switch when selecting a new avatar.
- Likely causes include stale cached equipped state, missing unequip for same PlayerId, event refresh failure, or resolver reading profile fallback before inventory equipped state.

---

# 32. Team selection flow

Official CreateTeamPage flow:

```text
Open CreateTeamPage
    ↓
Resolve current selected players
    ↓
Obtain Player1Id / Player2Id
    ↓
Load defaults
    ↓
Load Player1Id-owned team assets
    ↓
Load Player2Id-owned team assets
    ↓
Merge/deduplicate by AssetType + AssetId
    ↓
Render selectors safely
    ↓
User selects emblem/color/background
    ↓
Save team profile by TeamId
    ↓
Raise team/AppEvents
```

Rules:

- Do not mutate ownership.
- Do not use TeamName for save if TeamId exists.
- Do not show assets owned by players outside the team.
- Do not include published-but-unowned assets.

---

# 33. Display flow

Display-only pages must follow this pattern:

```text
Page loads
    ↓
Read authoritative ID
    ↓
Call resolver/service
    ↓
Render returned display model
    ↓
Do not mutate ownership
```

Display-only pages include:

- MainPage
- PlayerProfilesPage
- PlayerDetailsPage
- GamePage
- RankingsPage
- HallOfFamePage
- HistoryPage
- MatchDetailsPage

---

# 34. Historical identity flow

For completed matches:

```text
SavedMatch
    ↓
Historical snapshot fields
    ↓
HistoryPage / MatchDetailsPage
```

Rules:

- Historical display should use snapshot first.
- Do not recalculate past identity from current inventory.
- If snapshot is missing, TeamId resolver may be used as fallback.
- Do not change historical facts when current team profile changes.

---

# 35. AppEvents flow

Correct order:

```text
Modify state
    ↓
Save JSON/profile/team/inventory
    ↓
Raise AppEvents
    ↓
Pages refresh from services
```

Incorrect order:

```text
Raise AppEvents
    ↓
Save later
```

This causes stale refreshes.

Required event use cases:

- Account switch
- Login/logout
- Developer role activation
- Player visual identity update
- Team identity update
- Asset publish
- Asset acquire
- Asset equip
- Team save
- Match save
- Rankings update
- Gallery update

---

# 36. Async and threading rules

General rules:

- Use async/await.
- Avoid `.Result`.
- Avoid `.Wait()`.
- Avoid fire-and-forget unless explicitly isolated and logged.
- UI updates must occur on MainThread.
- CollectionView ItemsSource replacement must be atomic.
- Save operations must be awaited before events are raised.

Forbidden:

- Blocking UI thread on async inventory/store calls.
- Updating ObservableCollection from background thread.
- Triggering re-entrant reload from selection event without suppression.

---

# 37. CollectionView/RecyclerView advanced safety

For pages using CollectionView/CarouselView/RecyclerView-like controls:

Safe reload algorithm:

```text
_isReloading = true
try
{
    selectedItem = null
    build new List<T> off UI
    dedupe/sort/filter off UI
    MainThread:
        ItemsSource = null or new immutable list
        ItemsSource = built list
        SelectedItem = null
}
finally
{
    _isReloading = false
}
```

Event handlers must begin with:

```text
if (_isReloading || _suppressSelection)
    return;
```

Never:

- Clear and Add repeatedly while bound.
- Remove selected item while event is executing.
- Replace ItemsSource and modify ObservableCollection at the same time.
- Reload from multiple AppEvents concurrently.
- Keep old selected index after list count changes.

This applies strongly to:

- CreateTeamPage asset selectors.
- Player asset selection pages.
- Store manager pickers.
- Any CollectionView with dynamic inventory.

---

# 38. Dependency injection law

Services must be registered once.

Rules:

- Prefer singleton for stateless services.
- Use scoped/transient only if state requires it and MAUI lifetime is understood.
- Do not instantiate services manually in pages if DI provides them.
- Do not create two instances of session/state services.
- Do not duplicate inventory services.

If a service holds current-user state, its lifetime must be deliberate and documented.

---

# 39. JSON repository law

JSON file operations must follow:

```text
Load:
    if missing => default/empty
    if corrupt => safe fallback + log
    no display-page crash

Save:
    ensure directory
    write unique temp
    flush
    replace/move atomically
    preserve old on failure
    lock per file
```

Do not:

- Use one shared `.tmp` name.
- Write during read-only render.
- Delete inventory on image failure.
- Wipe JSON on parse exception without backup.
- Save partial corrupted data after exception.

---

# 40. AssetId and display-name law

AssetId is internal.

DisplayName is user-facing.

Picker UI should show:

1. NameAr if Arabic UI.
2. NameEn if English UI.
3. DisplayName.
4. Name.
5. AssetId only as last fallback.

Picker save must store:

- AssetId / CanonicalAssetId

Never store:

- Combined display string.
- Raw picker label.
- DisplayName as canonical identity.

---

# 41. AssetType routing law

Managers must accept only allowed AssetTypes.

Examples:

```text
Avatar Manager             => Avatar
Profile Background Manager => ProfileBackground
Frame Manager              => Frame
Effect Manager             => Effect
Title Manager              => Title
Emblem Manager             => Emblem / EmblemBackground if explicitly designed
Team Color Manager         => TeamColor
```

If a manager cannot accept an AssetType, that AssetType must not appear in its picker.

The error “نوع الأصل غير مسموح في هذا المدير” must appear only for genuinely invalid AssetType selection.

---

# 42. Progress calculation law

Store/player progress must distinguish:

- Player assets.
- Team assets.
- Defaults.
- Owned/purchased assets.
- Published but unowned assets.
- Equipped assets.

If progress is intended to represent total collection ownership, it may include owned player assets and owned team assets.

If progress is intended to represent player visual identity only, team assets must be excluded.

This must be explicit in code and UI label.

Current reported issue:

- Team assets acquired by player are not counted in progress bar.
- Fix requires determining intended progress definition and updating `PlayerStoreProgressService` or equivalent progress aggregator to include PlayerId-owned team assets when the UI says total collection/progress.

Do not count defaults as purchased progress.

---

# 43. Current critical defect map

The following defects are confirmed by runtime testing and must be treated as active until fixed:

## 43.1 CreateTeamPage edit crash

Error:

```text
Java.Lang.IndexOutOfBoundsException
Inconsistency detected. Invalid item position
MauiRecyclerView
ReorderableItemsViewAdapter
LinearLayoutManager
```

Likely location:

- `Pages/CreateTeamPage.xaml.cs`
- Team asset selector reload/edit flow
- CollectionView ItemsSource mutation

Required fix:

- RecyclerView-safe reload.
- Selection suppression.
- Atomic ItemsSource replacement.
- Avoid modifying bound collection during edit.

## 43.2 Team asset ownership leakage

Symptom:

- Team assets acquired by one account/player visible to other accounts/players on same device.

Likely locations:

- `TeamEligibleAssetService`
- `TeamAssetInventoryService`
- `CreateTeamPage.xaml.cs`
- `PlayerInventoryService`
- session current-player resolution

Required fix:

- Eligibility must use Player1Id/Player2Id.
- Player-owned team assets must be scoped to PlayerId.
- Current account switch must refresh PlayerId ownership state.
- No global owned cache.

## 43.3 Avatar switch failure

Symptom:

- Player equips avatar, then selecting a different avatar does not update.

Likely locations:

- `PlayerAssetInventoryService`
- `PlayerInventoryService`
- `StoreEquipService`
- `PlayerVisualIdentityResolver`
- `PlayerProfileService`
- AppEvents refresh handlers

Required fix:

- One-equipped-per-PlayerId+AssetType.
- Unequip previous same type for same PlayerId.
- Save before AppEvents.
- Resolver reads equipped state after save.
- UI refreshes after AppEvents.

## 43.4 Team assets missing from progress

Symptom:

- Player-acquired team assets do not count in progress bar.

Likely location:

- `PlayerStoreProgressService`

Required fix:

- Include PlayerId-owned team assets if progress definition is total collection.
- Do not count defaults.
- Do not count published unowned assets.

---

# 44. Engineering decision records — foundation

## ADR-001: IDs are authoritative

Decision:

- PlayerId, TeamId, AccountId, AssetId, ProductId are authoritative.
- Names are display-only.

Reason:

- Prevent duplicate display name identity leakage.
- Prevent Developer/Normal confusion.
- Support account switching.
- Support historical snapshots.

## ADR-002: Defaults are available, not owned

Decision:

- Default assets appear in catalogs/selectors but not My Assets.

Reason:

- Prevent false collection ownership.
- Prevent default assets leaking as purchased inventory.
- Preserve clean progress count.

## ADR-003: Published is not owned

Decision:

- Publishing creates catalog visibility only.

Reason:

- Prevent Developer publishing from granting assets to every player.

## ADR-004: Team assets are player-owned but team-applied

Decision:

- Team asset purchases belong to PlayerId.
- Team selections save to TeamId.

Reason:

- A player may own an emblem/color and apply it only to teams where that player participates.

## ADR-005: Historical snapshots are protected

Decision:

- History displays saved snapshot first.

Reason:

- Current inventory/team changes must not rewrite historical truth.

## ADR-006: Layout protection

Decision:

- Bug fixes must not redesign XAML.

Reason:

- Prevent regressions and visual instability.

## ADR-007: AppEvents is the only sync bus

Decision:

- Use existing AppEvents only.

Reason:

- Prevent duplicate sync mechanisms and event loops.

## ADR-008: JSON remains official persistence

Decision:

- Preserve JSON storage.

Reason:

- Project architecture and existing data depend on it.

---

# 45. End of Part 02

PART 02 defines the project architecture map, service/page responsibility matrix, GalleryEngine flows, ownership flows, runtime data flows, threading rules, and active defect map.

Continue with PART 03 for detailed Identity, Session, Account Switching, Developer Role, Player Asset, and Team Asset constitutions.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 03
### Identity, Session, Account Switching, Player Assets, Team Assets

---

# 46. Identity Constitution

Identity is the foundation of Domino Majlis PRO.

Every persistent entity shall have one authoritative identifier.

Authoritative identifiers:

- ApplicationUserId
- AccountId
- PlayerId
- TeamId
- MatchId
- AssetId
- InventoryItemId
- PurchaseId
- SeasonId

Display names are presentation only.

Changing a visible name must never change ownership.

---

# 47. Session Constitution

Every active session contains:

- CurrentAccountId
- CurrentPlayerId
- CurrentRole
- CurrentSeasonId
- Authentication state

Changing account shall immediately invalidate cached ownership.

The following caches must refresh after account switching:

- Player inventory
- Equipped assets
- Team eligibility
- Player profile
- Player visual identity
- Store ownership
- Progress values

---

# 48. Account Switching

Required sequence:

1. Resolve target account.
2. Resolve PlayerId.
3. Save session.
4. Clear cached ownership.
5. Reload inventory.
6. Reload equipped assets.
7. Raise AppEvents.
8. Refresh pages.

Forbidden:

- Keeping previous player's inventory.
- Sharing equipped avatar.
- Sharing team assets.
- Sharing online state.

---

# 49. Player Asset Constitution

Player assets include:

- Avatar
- Profile Background
- Frame
- Effect
- Title

Ownership:

Owner = PlayerId

Equipment:

Exactly one equipped asset per supported AssetType unless explicitly documented otherwise.

---

# 50. Team Asset Constitution

Team assets include:

- Emblem
- Team Color
- Emblem Background
- Team Banner (future)

Ownership:

Owner = PlayerId

Application:

Applied to TeamId.

A purchased emblem belongs to the player forever but is visible only while that player participates in the edited team.

---

# 51. Eligibility Law

CreateTeamPage shall expose:

Default Team Assets

PLUS

Assets owned by Player1Id

PLUS

Assets owned by Player2Id

Nothing else.

Published assets are never automatically eligible.

---

# 52. Ownership Isolation

Two players on one device must behave exactly as if they were on two different devices.

No purchase performed by Player A may appear for Player B.

No equipment performed by Player A may appear for Player B.

No cached inventory may survive account switching.

---

# 53. Online State

Online/offline belongs to ApplicationUserId.

It must not be global.

It must refresh after login, logout and account switching.

---

# 54. Progress Constitution

Progress services shall explicitly define:

Player Collection Progress

and

Total Collection Progress.

Defaults are excluded.

Published-but-unowned assets are excluded.

If Total Collection includes team assets, they must be counted through PlayerId ownership.

---

# 55. Runtime Verification Checklist

Identity:

☐ Account isolation

☐ Player isolation

☐ Team isolation

☐ Inventory isolation

☐ Equipment isolation

☐ Session isolation

☐ Store isolation

☐ Progress isolation

☐ Online/offline isolation

☐ AppEvents synchronization

---

# 56. Engineering Rules

Never fix identity bugs by redesigning UI.

Never fix ownership bugs by making assets global.

Never bypass PlayerId.

Never bypass TeamId.

Always preserve historical snapshots.

---

# 57. Known Active Engineering Risks

- RecyclerView synchronization during CreateTeamPage editing.
- Avatar re-equip refresh.
- Team asset ownership filtering.
- Progress aggregation.
- Session cache invalidation.

---

# 58. AI Review Protocol

Before changing code an AI shall:

1. Read Constitution.
2. Read affected services.
3. Read affected pages.
4. Trace AppEvents.
5. Trace JSON persistence.
6. Build.
7. Verify runtime.
8. Report remaining risks honestly.

---

# End of PART 03

PART 04 will define Gallery Engine, Store CMS, Admin Publishing, Inventory persistence, JSON schemas, and Store Manager Constitution in detail.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 04
### Gallery Engine, Store CMS, Inventory Persistence, JSON Constitution

---

# 59. Gallery Engine Constitution

GalleryEngine is an independent subsystem responsible for every visual asset in Domino Majlis PRO.

Responsibilities include:

- Asset Catalog
- Asset Publishing
- Store Rendering
- Inventory
- Equipment
- Display Resolution
- Collection Progress
- Gallery Components

GalleryEngine must never contain unrelated business logic.

---

# 60. Gallery Architecture

GalleryEngine is divided into:

```text
GalleryEngine
│
├── Models
├── Services
├── Components
├── Pages
├── Admin
├── Admin/Core
├── Admin/Services
├── Admin/Models
└── Assets
```

Each folder has one responsibility.

---

# 61. Store CMS Constitution

Store CMS is the authoritative publishing system.

Publishing workflow:

Developer

↓

Draft

↓

Validation

↓

Publish

↓

Catalog

↓

Player Acquisition

Drafts must never become visible until published.

---

# 62. Publishing Rules

Publishing creates:

Catalog Visibility

ONLY.

Publishing never:

- Grants ownership.
- Equips assets.
- Updates Player Inventory.
- Updates Team Inventory.

---

# 63. Inventory Constitution

Inventory contains only owned assets.

Inventory does not contain:

- Drafts
- Published catalog entries
- Defaults
- Hidden assets

Inventory belongs to PlayerId.

---

# 64. Equipment Constitution

Equipment references Inventory.

Equipment never references Catalog directly.

Flow:

Catalog

↓

Acquire

↓

Inventory

↓

Equip

↓

Resolver

↓

UI

---

# 65. JSON Constitution

Every JSON file must have:

Stable schema.

Version tolerance.

Graceful missing field handling.

Atomic save.

Recovery on corruption.

---

# 66. JSON File Rules

No JSON file may be overwritten until:

Temporary file written.

Flush completed.

Validation completed.

Atomic replace executed.

---

# 67. Asset Catalog Constitution

Every asset has:

AssetId

AssetType

CategoryId

SeasonId

DisplayName

Rarity

Visibility

Publish State

Owner information is forbidden inside catalog assets.

---

# 68. Player Inventory JSON

Player inventory contains:

PlayerId

InventoryItemId

AssetId

OwnedDate

Source

IsOwned

IsEquipped

No display-only fields should determine ownership.

---

# 69. Team Inventory Constitution

Team inventory stores:

TeamId

Applied AssetId

Current Emblem

Current Color

Applied assets only.

Ownership remains PlayerId.

---

# 70. Default Asset Rules

Default assets:

Available.

Not owned.

Not counted in progress.

Not displayed in My Assets.

Available only where default selection is allowed.

---

# 71. Store Progress Constitution

Progress services must distinguish:

Owned.

Equipped.

Default.

Published.

Expired.

Hidden.

Future assets.

---

# 72. Canonical Input Framework

Developer tools must use controlled input.

Allowed free text:

Title

Name

Description

Button Text

Everything else:

Dropdown

Picker

Selector

Toggle

Search Picker

---

# 73. AssetType Validation

Each manager accepts only supported AssetTypes.

Unsupported AssetTypes must fail validation before save.

---

# 74. Developer Store Manager Constitution

Developer Store Manager is the CMS.

Responsibilities:

Publish.

Hide.

Delete.

Schedule.

Edit.

Preview.

Never perform player ownership.

---

# 75. Draft Constitution

Drafts:

Invisible.

Editable.

Local.

Versioned.

Publishing converts Draft to Published.

---

# 76. Store Runtime Flow

Developer

↓

Publish

↓

Catalog JSON

↓

Store Page

↓

Acquire

↓

Inventory

↓

Equip

↓

Resolver

↓

Player UI

---

# 77. Gallery Component Rules

Gallery components must be reusable.

Components never:

Save JSON.

Modify ownership.

Calculate eligibility.

---

# 78. Future Expansion

Future asset types:

Animation

Sound

Board Theme

Table Theme

Voice Pack

Pet

Badge FX

Each new AssetType must integrate through GalleryEngine.

---

# 79. Engineering Principle

GalleryEngine must remain Data Driven.

No hardcoded seasons.

No hardcoded categories.

No hardcoded inventory.

---

# End of PART 04

PART 05 will document:
AppEvents Constitution,
Synchronization,
Thread Safety,
CollectionView Rules,
RecyclerView Protection,
Performance Constitution.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 05
### AppEvents, Synchronization, Thread Safety, Performance Constitution

# 80. AppEvents Constitution

AppEvents is the only synchronization bus.

Every page must refresh through AppEvents instead of directly manipulating other pages.

Required event groups:

- CurrentUserChanged
- PlayerProfileChanged
- TeamProfileChanged
- MatchChanged
- RankingsChanged
- StoreChanged
- InventoryChanged
- EquipmentChanged
- GalleryChanged
- SecurityChanged

No parallel synchronization framework is permitted.

---

# 81. Synchronization Law

Correct order:

Business Logic

↓

Persistence

↓

AppEvents

↓

UI Refresh

Never raise AppEvents before persistence succeeds.

---

# 82. Event Ownership

Services publish events.

Pages consume events.

Models never publish events.

---

# 83. Thread Safety Constitution

Rules:

- Use async/await.
- Avoid .Wait().
- Avoid .Result.
- Never block UI thread.
- Await every persistence operation.
- Marshal UI updates onto MainThread.

---

# 84. CollectionView Constitution

CollectionView data must be replaced atomically.

Forbidden:

- Clear/Add loops while bound.
- Concurrent reloads.
- Updating ItemsSource from background threads.

Use suppression flags during reload.

---

# 85. RecyclerView Protection

Known Android crash:

Java.Lang.IndexOutOfBoundsException

Mitigation:

- Build new list off-thread.
- Swap ItemsSource once.
- Reset selection.
- Prevent re-entry.

---

# 86. Performance Constitution

Priority:

1. Correctness
2. Stability
3. Identity integrity
4. Memory
5. Rendering
6. Animation

Never sacrifice correctness for FPS.

---

# 87. Memory Rules

Avoid duplicate caches.

Cache keys must include:

- PlayerId
- TeamId
- SeasonId (where applicable)

Global caches for ownership are forbidden.

---

# 88. Async Engineering Rules

Every async operation must:

Validate input.

Execute.

Persist.

Raise AppEvents.

Return.

Exceptions must be logged, not silently ignored.

---

# 89. Logging Constitution

Security logs.

Developer logs.

Runtime logs.

Performance logs.

Each serves a separate purpose.

Never mix audit logs with debug logs.

---

# 90. Runtime Verification

Every completed feature must pass:

✓ Build

✓ Runtime

✓ Identity isolation

✓ Session switching

✓ AppEvents refresh

✓ Android verification

before being declared complete.

---

# End of PART 05

PART 06 will document Security, Developer Identity, Honor System, Anti-Cheat, Hall of Fame, and Audit Constitution.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 06
### Security, Developer Identity, Honor System, Anti-Cheat, Hall of Fame, Audit Constitution

# 91. Security Constitution

Security exists to protect:

- Identity
- Ownership
- Rankings
- Hall of Fame
- Store
- Sessions
- Developer authority

Security must never unnecessarily reduce usability.

---

# 92. Developer Identity

Developer is the highest engineering authority inside the application.

Rules:

- Unique identity.
- Cannot be duplicated.
- Attached to one ApplicationUser.
- Never created by display name.
- Bound through ApplicationUserId and PlayerId.

Developer activation must attach to an existing account whenever possible.

---

# 93. Founder Identity

Founder is honorary.

Founder is not automatically Developer.

Founder permissions are explicitly granted.

---

# 94. Honor Identity

Honor identities are ceremonial.

Honor identities must never bypass engineering security.

Honor activation must be logged.

---

# 95. Developer Lock Constitution

Developer tools must remain hidden from ordinary users.

Visibility requires:

- Developer role.
- Successful verification.
- Active session.

No UI shortcut may bypass Developer Lock.

---

# 96. Security Log Constitution

Security events requiring audit:

- Developer activation.
- Honor activation.
- Role changes.
- Store publishing.
- Full reset.
- Identity migration.
- Hall decisions.
- Anti-cheat actions.

Permanent logs are immutable.

Temporary logs follow retention policy.

---

# 97. Audit Constitution

Every administrative action shall include:

Timestamp.

Actor.

Reason.

Affected entity.

Outcome.

Audit records must be traceable.

---

# 98. Anti-Cheat Constitution

Domino Majlis PRO records real-world domino.

The anti-cheat engine shall be evidence-based.

The following alone are NOT evidence:

- Repeated opponents.
- Fast matches.
- Winning streaks.
- Repeated victories.
- Frequent play.

Evidence must come from verified analysis.

---

# 99. Integrity Principle

Presumption of Integrity.

Every player and team is considered legitimate until evidence proves otherwise.

Suspicion alone never justifies punishment.

---

# 100. Hall of Fame Constitution

Hall of Fame requires:

Achievement.

Integrity.

Trust.

Verified performance.

Confirmed fraud permanently disqualifies only after documented evidence and review.

---

# 101. Hall Review Process

Stages:

Watch

↓

Investigation

↓

Evidence

↓

Decision

↓

Audit

↓

Notification

No step may be skipped.

---

# 102. Ranking Protection

Identity fixes must never alter ranking calculations.

Anti-cheat fixes must never modify historical rankings without audit.

---

# 103. Recovery Constitution

Recovery tools include:

Developer recovery.

Identity recovery.

JSON recovery.

Emergency backup.

Every destructive operation must recommend backup first.

---

# 104. Full Reset Constitution

Full Reset is Developer-only.

Required sequence:

Backup.

Developer verification.

Strong warning.

Typed confirmation.

Delete JSON.

Restart.

Audit log.

---

# 105. Engineering Ethics

Never hide defects.

Never fake verification.

Never report success without evidence.

Always disclose remaining known issues.

---

# End of PART 06

PART 07 will cover:
Roadmap, Release Engineering, Testing Constitution, ADR Library, Future Expansion, Developer Handbook.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 07
### Roadmap, Testing, Release Engineering, ADR Library, Developer Handbook

# 106. Project Roadmap Constitution

Development shall proceed in controlled phases.

Each phase requires:

- Defined objective
- Protected architecture
- Build success
- Runtime verification
- Regression review
- Completion report

No phase is complete without satisfying all criteria.

---

# 107. Testing Constitution

Every major feature shall be validated through:

- Unit-level verification where practical.
- Windows build.
- Android runtime.
- Session switching.
- Identity isolation.
- Ownership isolation.
- Store workflow.
- Team workflow.
- History workflow.

Regression testing is mandatory after identity or inventory changes.

---

# 108. Release Engineering

Release builds must:

- Compile without errors.
- Use release configuration.
- Avoid Fast Deployment artifacts.
- Preserve JSON compatibility.
- Preserve existing user data whenever possible.

Every release receives a version, changelog, and verification report.

---

# 109. Versioning

Suggested semantic format:

Major.Minor.Patch

Major:
Architecture changes.

Minor:
New features.

Patch:
Bug fixes.

---

# 110. Architecture Decision Records

Every major architectural decision should receive an ADR.

Each ADR contains:

- Context
- Decision
- Alternatives
- Consequences
- Status

Historical ADRs must remain immutable.

---

# 111. Developer Handbook

Every contributor shall:

- Read the Constitution.
- Inspect affected code.
- Avoid layout redesign.
- Prefer ID-first logic.
- Build after changes.
- Test before claiming success.
- Report remaining risks honestly.

---

# 112. AI Engineering Rules

An AI assistant must never:

- Invent APIs.
- Invent services.
- Invent file paths.
- Claim repository inspection without inspection.
- Claim runtime verification without runtime execution.

If information is unavailable, the AI must explicitly state that fact.

---

# 113. Documentation Constitution

Documentation is treated as source code.

Engineering decisions must be reflected in documentation.

Documentation changes shall accompany significant architectural changes.

---

# 114. Future Expansion

Future modules may include:

- Cloud synchronization
- Cross-device identity
- Tournament engine
- Analytics
- Web administration
- Public API
- Notifications
- Seasonal events

New modules must comply with this Constitution.

---

# 115. Final Engineering Principles

Always prefer:

Correctness over convenience.

Architecture over shortcuts.

Identity over names.

Evidence over assumptions.

Verification over optimism.

Truth over appearance.

---

# 116. Final Statement

This Constitution defines the engineering law governing Domino Majlis PRO.

All future development should preserve:

- Identity integrity
- Architectural stability
- Runtime correctness
- Maintainability
- Transparency
- Evidence-based engineering

End of PART 07.

Further editions may extend the Constitution while preserving backward compatibility with Version 1.0.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 08
### Engineering Standards, Coding Standards, Project Governance, Long-Term Evolution

# 117. Coding Constitution

Every code change shall be:

- Minimal
- Targeted
- Reversible
- Buildable
- Testable
- Documented

Large rewrites are forbidden unless explicitly approved.

---

# 118. File Modification Rules

Before editing a file:

1. Read the file.
2. Understand dependencies.
3. Search references.
4. Apply the smallest safe change.
5. Build immediately.
6. Verify runtime if affected.

Never patch blindly.

---

# 119. Naming Standards

Identifiers:

- PascalCase for types.
- camelCase for locals.
- Meaningful names.
- No hidden abbreviations.

IDs must end with:

- PlayerId
- TeamId
- AccountId
- AssetId
- SeasonId

---

# 120. Service Design Rules

Services should be:

- Focused.
- Stateless where possible.
- Reusable.
- Dependency-injected.
- Independently testable.

Avoid circular dependencies.

---

# 121. Model Constitution

Models represent persisted data.

Models must not:

- Navigate.
- Open dialogs.
- Read UI state.
- Execute business workflows.

---

# 122. ViewModel Constitution

ViewModels coordinate UI state.

Business logic belongs in services.

ViewModels may orchestrate but should not duplicate service logic.

---

# 123. Dependency Review

Every new dependency must answer:

Why is it needed?

Can an existing service perform the task?

Will it duplicate architecture?

---

# 124. Technical Debt

Technical debt must be:

- Recorded.
- Prioritized.
- Reviewed.
- Removed safely.

Never hide technical debt.

---

# 125. Regression Policy

Every identity, inventory, session or synchronization fix requires regression testing for:

- Login
- Logout
- Account switching
- Team creation
- Team editing
- Store purchase
- Equip
- Hall of Fame
- Rankings
- History

---

# 126. Release Checklist

Before release:

✓ Build succeeds

✓ Runtime succeeds

✓ No critical crashes

✓ Session verified

✓ Inventory verified

✓ Ownership verified

✓ Team assets verified

✓ Progress verified

✓ Hall verified

✓ Anti-cheat unaffected

---

# 127. Repository Governance

Repository must remain:

- Organized
- Documented
- Versioned
- Traceable

Major architecture changes require documentation updates.

---

# 128. AI Collaboration Standard

Every AI working on the repository shall:

Read Constitution.

Inspect implementation.

Respect protected architecture.

Respect protected layouts.

Prefer ID-first.

Report uncertainty honestly.

---

# 129. Long-Term Vision

Domino Majlis PRO shall evolve without sacrificing:

Identity integrity.

Architectural consistency.

Historical correctness.

Player trust.

Developer transparency.

---

# 130. Closing Declaration

This Constitution is the governing engineering reference for Domino Majlis PRO.

Future versions extend it but never invalidate established architectural law without explicit owner approval.

END OF PART 08.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 09
### Operational Playbook, Maintenance, Quality Assurance, Disaster Recovery

# 131. Maintenance Constitution

Maintenance shall preserve architecture before adding functionality.

Every maintenance task must classify itself as:

- Bug Fix
- Refactor
- Performance
- Documentation
- Security
- Feature
- Migration

---

# 132. Quality Assurance

QA scenarios shall always include:

- Clean install
- Upgrade install
- Existing user data
- New account
- Multiple accounts
- Team creation
- Team editing
- Store acquisition
- Equip/unequip
- Ranking refresh
- Hall of Fame

---

# 133. Backup Policy

Before destructive operations:

- Export JSON if possible.
- Preserve audit logs.
- Preserve security logs.
- Preserve player identities.

---

# 134. Disaster Recovery

Recovery priority:

1. Identity
2. JSON
3. Inventory
4. Teams
5. Matches
6. Rankings
7. Visual assets

---

# 135. Compatibility

Backward compatibility is preferred.

Schema migrations must be explicit.

Silent breaking changes are forbidden.

---

# 136. Performance Review

Performance optimization must never:

- Break identity
- Break synchronization
- Break ownership
- Break persistence

---

# 137. Code Review Checklist

Review:

- ID-first compliance
- AppEvents order
- JSON safety
- Async correctness
- Thread safety
- Layout protection
- Runtime verification

---

# 138. Release Sign-off

A release is approved only when:

✓ Build succeeds

✓ Android verification passes

✓ Critical defects resolved

✓ Documentation updated

✓ Constitution still respected

---

# 139. Future Constitution Versions

Future versions:

v1.1
v1.2
v2.0

shall extend, not silently replace, architectural law.

---

# 140. End of PART 09

PART 10 contains glossary, terminology, constitutional summary and adoption statement.
# Domino Majlis PRO — Official Engineering Constitution
## Version 1.0 — PART 10
### Glossary, Constitutional Summary, Adoption Statement

# 141. Engineering Glossary

AccountId
: Canonical account identity.

ApplicationUserId
: Internal application user identifier.

PlayerId
: Canonical player identity.

TeamId
: Canonical team identity.

AssetId
: Canonical asset identifier.

PurchaseId
: Purchase transaction identifier.

InventoryItemId
: Owned inventory record identifier.

SeasonId
: Season identifier.

AppEvents
: Official synchronization mechanism.

GalleryEngine
: Official subsystem for assets, store, inventory, equipment and rendering.

---

# 142. Constitutional Priorities

Highest priorities:

1. Identity Integrity
2. Data Integrity
3. Architecture Stability
4. Runtime Correctness
5. Synchronization
6. Ownership Isolation
7. Security
8. Maintainability
9. Performance
10. Visual Quality

---

# 143. Mandatory Engineering Checklist

Before every commit:

✓ Build

✓ Review affected services

✓ Review affected pages

✓ Validate AppEvents

✓ Validate JSON

✓ Validate PlayerId usage

✓ Validate TeamId usage

✓ Confirm no layout redesign

✓ Record remaining issues

---

# 144. AI Compliance Checklist

Every AI assistant shall verify:

- Constitution read.
- Repository inspected.
- File dependencies understood.
- Minimal changes applied.
- Build completed.
- Runtime limitations disclosed honestly.
- No fabricated implementation claims.

---

# 145. Constitutional Oath

Every engineering change shall preserve:

Truth.

Evidence.

Identity.

Architecture.

Historical correctness.

Player trust.

Developer transparency.

---

# 146. Adoption Statement

This document is adopted as the official engineering constitution for Domino Majlis PRO.

Future revisions shall extend this constitution while preserving established engineering law unless the project owner explicitly authorizes constitutional change.

---

# 147. Final Declaration

The Domino Majlis PRO Engineering Constitution exists to ensure that every developer, AI assistant, and future contributor understands not only how the project is built, but why it is built that way.

Engineering decisions must remain consistent with the mission of Domino Majlis PRO as a professional real-world domino management platform.

---

# END OF CONSTITUTION VERSION 1.0

Merge order:

PART 01

PART 02

PART 03

PART 04

PART 05

PART 06

PART 07

PART 08

PART 09

PART 10

Output filename:

DominoMajlisPRO_Official_Engineering_Constitution_v1.md
