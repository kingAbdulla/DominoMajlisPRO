# 07 STORE AND GALLERY ARCHITECTURE

## Core Concepts
Store/Gallery must separate three concepts:

### Published Asset/Product
Created by Developer/Admin. Visible in Store if published. Not owned by players automatically.

### Owned Asset
Stored in player inventory. Owned by exactly one PlayerId/ApplicationUserId pair. Created only after acquire/purchase/grant.

### Equipped Asset
A selected owned/default item applied to a player or team. Equipped state must be scoped by PlayerId and AssetType for player assets.

## Default Asset Rule
Default assets are available, but not purchased/owned.
- Default avatars may appear in avatar catalog/category sections.
- Default avatars must not appear in My Assets as Owned.
- Default team emblems/colors/backgrounds may appear in CreateTeamPage as default choices.
- Defaults must not count as purchased ownership.

## Team Asset Eligibility Rule
Available team assets in CreateTeamPage =
- default team assets
- plus assets owned by Player1Id
- plus assets owned by Player2Id

Never include all device-owned assets. Never include all published assets. Never include assets owned by other PlayerIds.

## Progress Bar Rule
Store progress should include all assets owned by current PlayerId, including player assets and owned team assets, but must exclude defaults that are not owned.

## Critical Services
- `PlayerInventoryService`: authoritative player owned items.
- `PlayerAssetInventoryService`: player asset inventory display.
- `TeamEligibleAssetService`: team creation eligibility.
- `TeamAssetInventoryService`: legacy/team scoped inventory.
- `PlayerStoreProgressService`: collection progress.
- `InventoryDisplayResolver`: image and progress snapshot.
