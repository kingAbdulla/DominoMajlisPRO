# 10 SERVICE REFERENCE

## ApplicationUserService
Authority for current user, session, role, ApplicationUserId, PlayerId. Must prevent duplicate Developer/Normal identity for same account. Must raise current user/profile/store events after changes.

## AppEvents
Global synchronization bus. Use existing events. Do not replace.

## PlayerInventoryService
Authoritative player-owned inventory. Must filter by PlayerId and current ApplicationUserId. Defaults must not be treated as owned.

## PlayerAssetInventoryService
Builds player asset catalog/inventory display. Defaults may be shown in selection catalogs but not as owned inventory.

## TeamEligibleAssetService
Creates list of assets available to a team. It must use Player1Id/Player2Id and default team assets. It must not leak assets from unrelated PlayerIds.

## TeamAssetInventoryService
Legacy/team inventory path. Should not be used as player purchase owner. TeamId is required for team-scoped persistence. Defaults are available but not owned.

## InventoryDisplayResolver
Canonical gateway for images and inventory display snapshot. Do not use raw ImageSource.FromFile across pages when resolver can be used.

## PlayerVisualIdentityResolver
Canonical player visual identity resolver. Must resolve by PlayerId first.

## PlayerStoreProgressService
Calculates collection progress. Must count owned current-player assets and exclude non-owned defaults. Must include owned team assets if they are part of current player inventory.

## StoreCmsJsonRepository
JSON safety layer. Must preserve atomic save, unique temp file, locks, missing/corrupt file tolerance.
