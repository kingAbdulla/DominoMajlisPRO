# Phase 2.8 Final Hardening Reference

```text
DOMINO MAJLIS PRO — PHASE 2.8 FINAL HARDENING
IDENTITY ISOLATION + PLAYERID/TEAMID BINDING + STORE PUBLISHING + FULL EMULATOR VERIFICATION

Read STORE_STATUS.md first.
Follow Domino Majlis PRO Constitution.
Follow Layout Protection Policy.
This is NOT a UI redesign.

==================================================

MISSION
Fix the current Store / Inventory / Identity / Equipment architecture so that every asset, account, player, team, page, and synchronization path is bound by stable IDs only.

Then perform a full real Android emulator verification from zero data:

Reset data.
Create/login as Developer.
Publish every supported asset type.
Logout/switch.
Create/login as a new Normal player account with a different username.
Acquire the assets published by Developer.
Equip every asset in its correct place.
Verify display on all related pages.
Fix every crash or defect found.
Do not report completion until the complete practical emulator flow passes 100%.
==================================================

ABSOLUTE COMPLETION RULE
Do NOT stop at build success.
Do NOT stop at partial verification.
Do NOT stop after publishing only.
Do NOT stop after visual inspection of one page.

Completion requires:

Windows build success.
Android build success.
Android emulator practical flow success.
No fatal crash.
No FileNotFoundException.
No InvalidOperationException.
No identity leakage.
No ownership leakage.
No duplicate AssetId display.
No wrong account/player/team binding.
All tested pages open, save, navigate away/back, and refresh correctly.
If any crash or defect appears:
FIX IT.
Rebuild.
Rerun the failed emulator flow.
Continue until 100% pass.

Only report after the full flow is confirmed.

==================================================

PROTECTED ARCHITECTURE / LAYOUT PROTECTION
Do NOT redesign UI.
Do NOT modify layout hierarchy.
Do NOT move controls.
Do NOT change grids, margins, padding, spacing, navigation flow, visual composition, card structure, or page structure unless the existing control is completely unreachable and no binding-only fix is possible.

Allowed changes:

Architecture
Binding
Identity synchronization
Role/session logic
Store routing
AssetId generation
JSON safety
Ownership logic
Inventory logic
Display resolver logic
AppEvents synchronization
Button command wiring
Existing approved slot binding
Do not apply ProfileBackground to MainPage unless an approved profile-card slot already exists.

==================================================

ROOT PROBLEMS OBSERVED
Observed emulator problems:

After app reset, creating player Abdulla and then activating Developer with Abdulla created two identities:
Abdulla Normal
Abdulla Developer
This caused account/player/role confusion.
Store-published assets by Developer became visible or affected the Normal player incorrectly.
Avatar changed unexpectedly and became stuck.
Switching to the Normal Abdulla account showed random avatar/identity behavior.
Online/Offline status changed incorrectly.
Avatar could be selected, but PlayerDetailsPage still showed default icon or wrong image.
Phase28Avatar appeared as Equipped but displayed default icon, meaning image resolver/binding is broken.
Default Avatars appeared in “My Assets / مقتنياتي” as Owned. This is incorrect.
AssetId Picker showed duplicate entries:



Phase28Avatar duplicated
emblem-background-dark/gold/transparent duplicated
Some published items displayed GUID/technical names instead of clean display names.
AssetType routing showed:
“نوع الأصل غير مسموح في هذا المدير”
even when selecting an asset from a visible picker.
Some new manager sections do not clear/reset the form after publishing.
Store Manager “Settings” card/button does not open the approved settings window.
Settings appeared in a different design/path than the approved MainPage settings window.
Store Manager card counts are inaccurate and sometimes show broad/global image counts instead of type-specific counts.
CreateTeamPage found some published emblems, which is good, but eligibility must be verified by PlayerId/TeamId, not by name.
==================================================

NON-NEGOTIABLE IDENTITY CONSTITUTION
The entire application must stop relying on names for identity.

Names are display-only.

Never use:

Player name
Team name
Account display name
Developer display name
as a primary key or binding key.

Forbidden lookup patterns:

Find player by Name
Find team by TeamName
Bind ownership by DisplayName
Bind session by current visible name
Bind developer role by name
Bind inventory by name
Required identity keys:

Account:
AccountId is the login/session identity key.
Each account must have one active bound PlayerId unless explicitly designed otherwise.
Current session must store CurrentAccountId and CurrentPlayerId.
Player:
PlayerId is the only player identity key.
Avatar, ProfileBackground, Frame, Effect, Title, online/offline, inventory, equipment, player rank, player profile, and player visual identity must bind to PlayerId.
Team:
TeamId is the only team identity key.
Team emblem, team color, emblem background, rankings, hall of fame, history, game display, match snapshots, and team statistics must bind to TeamId.
Developer:
Developer is a Role/Permission on the current account/player identity.
Activating Developer must NOT create a second unrelated player just because the same display name was entered.
If the current logged-in account already has PlayerId, Developer activation should attach role metadata to the current identity/account.
If a Developer profile entity exists separately, it must have DeveloperId/AccountId and must not masquerade as a second Normal Player with same name.
Ownership:
Player-owned assets are owned by PlayerId only.
Team identity selections are saved by TeamId.
Team eligibility for selecting owned team assets is based on Player1Id and Player2Id.
Published store products:
Developer-published assets/products are globally visible only if PublishState/Visibility allows.
They are not owned by Normal players until acquired/purchased/granted.
Publishing an asset does not equip it for any player.
Publishing an asset does not add it to every player’s owned inventory.
==================================================

ACCOUNT / ROLE / SESSION FIX
Fix account/session behavior:

After reset, when a user creates account Abdulla, create:
AccountId
PlayerId
DisplayName = Abdulla
Role = Normal unless explicitly activated
When activating Developer:
Do not create a second player record with the same display name unless user explicitly creates a new player account.
Attach Developer role to the current account/player identity.
Store Developer permission by AccountId/PlayerId, not by name.
If the user later creates a Normal account:
It must use a different username in the test, such as AbdullaNormal, Abdulla2, or NormalTester.
It must receive a distinct AccountId and PlayerId.
It must not inherit Developer inventory/equipment.
It must not inherit Developer role.
It must not inherit Developer online status.
Account switching must update:
CurrentAccountId
CurrentPlayerId
CurrentRole
Current inventory view
Current equipped identity
Online/Offline status
Store ownership state
Player profile display
MainPage player slot
Online/Offline must be bound to CurrentPlayerId/AccountId, never by display name.
==================================================

PLAYERID / TEAMID BINDING AUDIT
Audit and fix all affected services/pages so they use stable IDs.

Search and inspect any logic that uses names for identity:

PlayerProfileService
PlayerEngine
PlayerInventoryService
PlayerVisualIdentityResolver
InventoryDisplayResolver
Store Manager services
Account/Login/Register services
Developer activation services
Honor/Role services if involved
CreateTeamPage
PlayerProfilesPage
PlayerDetailsPage
MainPage
GamePage
RankingsPage
HallOfFamePage
HistoryPage
MatchDetailsPage
Team services
Ranking services
AppEvents handlers
Replace identity lookup by name with:

PlayerId for players
TeamId for teams
AccountId for login/session
AssetId/CanonicalAssetId for assets
Keep names only for display.

==================================================

STORE ASSET ARCHITECTURE
Use three separate concepts:

Published Asset/Product
Exists in store catalog.
Created by Developer/Admin.
Identified by AssetId/CanonicalAssetId.
Visible according to PublishState/Visibility.
Owned Asset
Exists in player_owned_assets.json.
Owned by PlayerId.
Created only after acquire/purchase/grant.
Has IsOwned = true.
Has Source = Purchased/Granted/Reward/DefaultGrant if explicitly needed.
Equipped Asset
Also PlayerId-bound.
One equipped item per PlayerId + AssetType unless multi-equip is explicitly supported.
IsEquipped = true.
Equipping one Avatar must unequip other Avatars for the same PlayerId only.
Default assets are not purchased assets.

==================================================

DEFAULT ASSET RULE
Default assets must always be available for selection where appropriate.

But default assets must NOT appear in “My Assets / مقتنياتي” as Owned/Purchased unless the product intentionally grants a copy and clearly marks Source = DefaultGrant.

Required behavior:

Avatar selection page:
Default Avatars may appear as Default/Available choices.
My Assets / مقتنياتي:
Show only assets actually owned/acquired/purchased/granted by CurrentPlayerId.
Do not show all default avatars as Owned.
CreateTeamPage:
Default team emblems/colors/backgrounds always visible.
Purchased/owned team assets visible only if owned by Player1Id or Player2Id.
Inventory count:
Owned count must never include all defaults as purchased ownership.
Collection count must not show Owned > Total.
==================================================

ASSETID GENERATION FIX
Remove manual AssetId selection from “Create New Asset” publishing flows where possible.

For creating new assets:

AssetId must be generated automatically.
Use AssetType + clean slug from Name/Title + short unique suffix.
Ensure uniqueness against all existing assets of same type.
Do not expose GUID as display text.
Do not require developer to manually type AssetId.
Do not require developer to choose existing AssetId when creating a new asset.
Examples:

avatar-fire-legend-8f3a21
team-emblem-golden-lion-42ac91
team-color-royal-purple-91bd02
emblem-background-dark-gold-33fa80
frame-golden-royal-a19f22
effect-lightning-7d41ac
title-grand-master-e03a12
For linking to an existing asset/product:

Use AssetId Picker.
Picker shows DisplayName.
Saves Canonical AssetId internally.
==================================================

ASSETID PICKER / DEDUPLICATION
Fix all AssetId picker sources:

Deduplicate by:
AssetType
CanonicalAssetId / AssetId
Do not show duplicated:
Phase28Avatar
emblem-background-dark
emblem-background-gold
emblem-background-transparent
Picker display should show:
DisplayName / NameAr / NameEn
AssetType
Optional status
Picker must not show:
raw GUID
file path
technical internal ID
duplicate defaults
If Category is empty:
Picker must still work by AssetType.
Category must be optional metadata/filter.
AssetType is mandatory and primary.
==================================================

ASSETTYPE ROUTING FIX
Each manager must show and accept only its allowed AssetTypes.

Examples:

Avatar manager: Avatar only.
ProfileBackground manager: ProfileBackground only.
Frame manager: Frame only.
Effect manager: Effect only.
Title manager: Title only.
Emblem manager: Emblem and/or EmblemBackground only if explicitly intended.
TeamColor manager: TeamColor only.
New Arrivals product linking can link to supported product asset types according to the section’s rules.
Fix the false error:
“نوع الأصل غير مسموح في هذا المدير”

The error should appear only if the selected AssetType is truly invalid for the current manager.

If the current manager cannot accept Avatar, Avatar must not appear in that picker.

==================================================

DISPLAY NAME FIX
All UI pickers/cards/lists must show clean names:

Priority:

NameAr if Arabic UI
NameEn if English UI
DisplayName
Name
AssetId only as last internal fallback, not normal UI
Do not show GUID to users.
Do not show technical names such as raw Phase28Avatar + GUID unless no display name exists.

==================================================

IMAGE SAFETY FIX
InventoryDisplayResolver must be the only gateway for converting stored paths/ids into ImageSource.

Fix Phase28Avatar showing as equipped but displaying default icon:

Verify stored image path/asset reference.
Verify resolver can resolve it.
Verify Android file path permissions.
Verify fallback only appears when actual image missing.
Do not use raw ImageSource.FromFile from JSON/profile/store/inventory/team data outside resolver.
Missing images must not crash pages.

==================================================

EQUIP LOGIC FIX
For Player Asset Manager:

When equipping Avatar:

Use CurrentPlayerId.
Set selected Avatar IsEquipped = true.
Set all other Avatar items for same PlayerId to IsEquipped = false.
Do not modify another PlayerId.
Update PlayerProfileModel Avatar fields only if those fields already exist.
Raise existing AppEvents.
Repeat same one-equipped-per-type logic for:

ProfileBackground
Frame
Effect
Title
If multiple effects are not supported, enforce one equipped effect.

==================================================

TEAM ASSET LOGIC FIX
CreateTeamPage must use:

Available Team Assets =
Default Team Assets
+
Assets owned by Player1Id
+
Assets owned by Player2Id

If only one player:
Default Team Assets + Assets owned by Player1Id

When selecting:

Emblem saves to TeamProfileModel by TeamId:

Emblem
EmblemAssetId
TeamColor saves to TeamProfileModel by TeamId:


ColorHex
TeamColorAssetId
EmblemBackground saves to TeamProfileModel by TeamId:


EmblemBackground
EmblemBackgroundAssetId
Also sync display identity to rankings.json by TeamId only.

Do not mutate ownership during team selection.
Do not use TeamId as purchase owner.
Do not use team/player names for eligibility.

==================================================

STORE MANAGER FORM RESET FIX
After successful publish/save in every manager section:

Reset form fields safely.
Clear image preview if appropriate.
Clear generated AssetId for next item.
Reset optional category/filter state if appropriate.
Keep required default picker states if needed.
Do not clear published data.
Do not navigate unexpectedly.
Show success state if existing pattern supports it.
Apply to sections where reset is currently missing:

Emblems
Team Colors
Emblem Backgrounds
Avatar
ProfileBackground
Frame
Effect
Title
New Arrivals if affected
Limited Offers if affected
Product Cards if affected
Category Cards if affected
==================================================

STORE SETTINGS ROUTING FIX
The Store Manager “إعدادات المتجر” card/button must open the approved settings window/component.

Observed:

Store settings button currently does nothing or opens a different design/path.
MainPage has an approved settings bottom sheet/window.
Required:

Reuse/open the same approved settings component/path used by MainPage, if available.
Do not create a second settings design.
Do not redesign settings.
Wire the Store Manager settings card click command correctly.
If store-specific settings are needed, open them inside the approved settings container/pattern.
==================================================

STORE MANAGER CARD COUNTS FIX
Fix counts shown on Store Manager cards.

Each card must count only its own relevant type:

Avatars: AssetType Avatar only.
Backgrounds: ProfileBackground only or approved background types only.
Emblems: Emblem only.
Emblem Backgrounds: EmblemBackground only.
Team Colors: TeamColor only.
Effects: Effect only.
Frames: Frame only.
Titles: Title only.
New Arrivals: New Arrivals products only.
Limited Offers: Limited Offers only.
Categories: Category cards only.
Product Cards: Product cards only.
Currency/Pricing: currency/pricing records only.
Do not show global image count for every card.

==================================================

JSON SAFETY
Keep and verify StoreCmsJsonRepository safe save behavior:

Unique temp file.
Per-file lock.
Directory exists.
Missing file returns empty list.
Corrupt file does not crash display pages.
Failed save preserves previous valid JSON.
No fixed shared .tmp race.
No .tmp FileNotFoundException.
No saving during read-only page rendering unless strictly necessary.
No inventory wipe on image failure.
==================================================

APPEVENTS SYNCHRONIZATION
Use existing AppEvents only.
Do not create a parallel event system.

After these actions:

Publish asset/product
Acquire/purchase asset
Equip player asset
Select team emblem/color/background
Save team
Switch account
Activate Developer role
Logout/login
Reset data
Refresh related pages without app restart:

MainPage
Store pages
Player Asset Manager
CreateTeamPage
PlayerProfilesPage
PlayerDetailsPage
GamePage
RankingsPage
HallOfFamePage
HistoryPage
MatchDetailsPage
==================================================

DISPLAY PAGE RULE
Display-only pages must not calculate ownership and must not mutate inventory:

MainPage
GamePage
RankingsPage
HallOfFamePage
HistoryPage
MatchDetailsPage
PlayerDetailsPage
PlayerProfilesPage
They should only read saved equipped/display identity by:

PlayerId for player visual identity
TeamId for team visual identity
Match snapshot for historical matches
==================================================

MANDATORY PRACTICAL ANDROID EMULATOR FLOW
Run this exact full flow on Android emulator after implementation.

Do not skip.

A. CLEAN START
Reset all app data using the app’s reset/developer reset flow.
Relaunch app.
Confirm no crash.
Confirm login/register/continue as ghost appears.
B. CREATE DEVELOPER ACCOUNT
Register/login as Developer test user:
Suggested username: DevPublisher
Do not use Abdulla for both Developer and Normal test accounts.
If Developer activation is required:

Activate Developer role for the current account.
Confirm no duplicate Normal player with same name is created.
Confirm AccountId and PlayerId are stable.
Confirm Role = Developer.
Confirm online status belongs to DevPublisher only.
Open MainPage.
Confirm developer store/admin entry is visible only for Developer.
C. DEVELOPER PUBLISHES EVERY SUPPORTED SECTION
As DevPublisher, open Store Manager and publish at least one real test item for every supported section/type:

Player assets:

Avatar
ProfileBackground
Frame
Effect
Title
Team assets:
6. Emblem
7. TeamColor
8. EmblemBackground

Store/product sections if supported:
9. New Arrivals
10. Limited Offers
11. Product Card
12. Category Card
13. Bundle if supported
14. Currency/Pricing if supported
15. Store Settings if supported

For every published item:

Use controlled selectors only.
Generate AssetId automatically.
Confirm no manual AssetId typing.
Confirm generated AssetId is unique.
Confirm DisplayName is clean.
Confirm image resolves.
Publish/save.
Confirm form resets after successful publish.
Confirm item appears in the relevant published list.
Confirm no duplicate picker entry.
Confirm no crash.
If a section/type is not implemented, report Not Supported. Do not claim Passed.

D. CREATE NORMAL PLAYER ACCOUNT
Logout/switch account.
Create a new Normal player account with a different name:
Suggested username: AbdullaNormal or NormalTester.
Confirm:
New AccountId.
New PlayerId.
Role = Normal.
No Developer permissions.
No Developer inventory ownership.
No Developer equipped assets.
Online status belongs to NormalTester only.
Developer assets are visible in store only as published products, not as already owned unless free/default and acquired.
E. NORMAL PLAYER ACQUIRES PUBLISHED ASSETS
As NormalTester:

Open Store.
Find every item published by DevPublisher.
Acquire/purchase/grant all published test items according to current app flow.
Confirm acquisition writes player_owned_assets.json with:
Current NormalTester PlayerId
AssetId
AssetType
IsOwned = true
IsEquipped = false unless immediately equipped by design
Source
AcquiredAt
ProductId if applicable
Confirm DevPublisher’s inventory is not modified.
Confirm default assets are not incorrectly added as purchased/owned.
F. NORMAL PLAYER EQUIPS PLAYER ASSETS
Open Player Asset Manager / My Assets.

Test:

Avatar:
Confirm purchased Avatar appears.
Confirm default avatars are not listed as Owned in My Assets.
Equip purchased Avatar.
Confirm only one Avatar equipped for NormalTester.
Confirm DevPublisher unaffected.
Confirm PlayerProfilesPage displays it.
Confirm PlayerDetailsPage displays it.
Confirm MainPage approved avatar slot displays it if slot exists.
Confirm image is not fallback unless image truly missing.
ProfileBackground:
Equip.
Confirm it appears only in approved PlayerDetailsPage/Profile slot.
Confirm not applied to MainPage unless approved slot exists.
Frame:
Equip.
Confirm it appears in approved player profile slot if supported.
Otherwise report Not Supported.
Effect:
Equip.
Confirm approved display/effect slot if supported.
Otherwise report Not Supported.
Title:
Equip.
Confirm player title appears in approved player profile/title slot if supported.
Otherwise report Not Supported.
Navigate away and back after every equip.
Confirm equipment persists.

G. NORMAL PLAYER TESTS TEAM ASSETS
Open CreateTeamPage.
Create/select team with NormalTester as Player1.
If two-player team is needed, add/create Player2 with distinct PlayerId.
Open Team Emblem selector.
Confirm:
Default emblems visible.
Purchased Emblem visible because NormalTester owns it.
Non-owned assets not visible.
No duplicates.
DisplayName clean.
AssetId internal only.
Select purchased Emblem.
Select TeamColor.
Select EmblemBackground if supported.
Save team.
Confirm teams.json by TeamId contains:
Emblem
EmblemAssetId
ColorHex
TeamColorAssetId
EmblemBackground
EmblemBackgroundAssetId
Confirm ownership remains PlayerId-based and is not mutated by team selection.
H. VERIFY TEAM DISPLAY PAGES
Using the saved team:

Open MainPage.
Open GamePage.
Start a test match if needed.
Confirm team emblem/color/background display from TeamId saved identity.
Save/finish a small test match if needed.
Open HistoryPage.
Open MatchDetailsPage.
Confirm historical identity uses saved snapshot and does not recalculate from current inventory.
Open RankingsPage.
Confirm team identity display syncs by TeamId.
Open HallOfFamePage if relevant.
Confirm Hall logic unchanged and identity display is by TeamId only.
Do not change ranking calculations.
Do not change Hall qualification.
Do not change match scoring.

I. ACCOUNT ISOLATION TEST
Switch back to DevPublisher.
Confirm Developer role visible.
Confirm DevPublisher inventory/equipment did not receive NormalTester acquisitions/equipment.
Confirm DevPublisher avatar is independent.
Switch back to NormalTester.
Confirm NormalTester avatar/team ownership/equipment persists.
Confirm no leakage by display name.
Create another user with similar display name if needed:
Abdulla2
Confirm no cross-linking with AbdullaNormal or DevPublisher.
J. STRESS / CRASH TEST
Run these actions:

Open PlayerProfilesPage 20 times.
Open PlayerDetailsPage 10 times.
Navigate rapidly:
MainPage ↔ Store ↔ Store Manager ↔ PlayerProfilesPage ↔ PlayerDetailsPage ↔ CreateTeamPage ↔ GamePage ↔ HistoryPage ↔ RankingsPage
Reopen Store Manager.
Open every manager card.
Open Store Settings card and verify it opens approved settings window.
Open My Assets.
Equip Avatar multiple times.
Switch accounts.
Reopen app if needed.
Confirm:

No crash.
No fatal exception.
No FileNotFoundException.
No InvalidOperationException.
No .tmp leak.
No JSON corruption.
No duplicate AssetId display.
No wrong player/team/account binding.
==================================================

WINDOWS VERIFICATION
After code changes:

Build entire solution on Windows.
Confirm 0 errors.
Review warnings related to:

nullability
binding
JSON
asset routing
image paths
obsolete API affecting these flows
Fix relevant warnings if they can cause runtime risk.
Do not spend time on unrelated cosmetic warnings.

==================================================

STOP CONDITIONS
If any of these occur, do not produce final success report:

Crash remains.
Emulator flow incomplete.
Build fails.
Avatar equip fails.
PlayerId ownership leaks.
TeamId identity fails.
Developer/Normal account mixing remains.
Store settings button does not work.
Default avatars still appear as Owned in My Assets.
Duplicate AssetId entries remain.
Published item cannot be acquired by NormalTester.
Acquired item cannot be equipped.
Team emblem/color/background cannot be selected/saved/displayed.
History/Rankings/GamePage fail to show saved team identity.
Any data wipe/corruption occurs.
Fix the issue and rerun verification.

==================================================

FINAL REPORT ONLY AFTER 100% PRACTICAL PASS
Report only:

Windows build result
Android emulator result
Crash Safety percentage
Data Integrity percentage
Identity Isolation percentage
PlayerId binding verified: Yes/No
TeamId binding verified: Yes/No
AccountId session binding verified: Yes/No
Developer account created: Yes/No
Normal account created with different username: Yes/No
Duplicate Abdulla Developer/Normal issue fixed: Yes/No
Developer published Avatar: Yes/No
Developer published ProfileBackground: Yes/No/Not Supported
Developer published Frame: Yes/No/Not Supported
Developer published Effect: Yes/No/Not Supported
Developer published Title: Yes/No/Not Supported
Developer published Emblem: Yes/No
Developer published TeamColor: Yes/No
Developer published EmblemBackground: Yes/No/Not Supported
Normal acquired all published assets: Yes/No
Avatar equipped and displayed in PlayerProfilesPage: Yes/No
Avatar equipped and displayed in PlayerDetailsPage: Yes/No
MainPage approved avatar slot verified: Yes/No/Not Supported
ProfileBackground restriction respected: Yes/No
Frame verified: Yes/No/Not Supported
Effect verified: Yes/No/Not Supported
Title verified: Yes/No/Not Supported
Default avatars excluded from My Assets Owned list: Yes/No
Default team assets preserved: Yes/No
Store AssetId auto-generation verified: Yes/No
Duplicate AssetId picker entries fixed: Yes/No
DisplayName/GUID UI issue fixed: Yes/No
AssetType routing fixed: Yes/No
Store Manager form reset fixed: Yes/No
Store settings opens approved settings window: Yes/No
Store Manager card counts fixed: Yes/No
CreateTeamPage Emblem selection verified: Yes/No
CreateTeamPage TeamColor selection verified: Yes/No
CreateTeamPage EmblemBackground selection verified: Yes/No/Not Supported
GamePage team identity verified: Yes/No
HistoryPage identity snapshot verified: Yes/No
MatchDetailsPage identity snapshot verified: Yes/No
RankingsPage TeamId display sync verified: Yes/No
HallOfFamePage untouched and display-safe: Yes/No
AppEvents sync verified: Yes/No
JSON repository safety verified: Yes/No
Image resolver safety verified: Yes/No
Modified files
Remaining issues, if any
Exact emulator steps completed
```
