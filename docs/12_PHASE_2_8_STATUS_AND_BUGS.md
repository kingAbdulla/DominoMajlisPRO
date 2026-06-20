# 12 PHASE 2.8 STATUS AND BUGS

## Phase 2.8 Goal
Identity isolation + PlayerId/TeamId binding + Store publishing + Android emulator verification.

## Completed / Partially Completed
- Build succeeds.
- Default avatars no longer appear as owned in My Assets after latest fix.
- ApplicationUserService received role attachment hardening.
- TeamProfileService received TeamId-first lookup.
- Some ID-first hardening applied in player/team services.

## Remaining Runtime Bugs
1. `Java.Lang.IndexOutOfBoundsException` / RecyclerView inconsistency when entering CreateTeamPage and editing team.
2. Team assets still leak across accounts on same device.
3. Avatar equip does not reliably switch to a newly selected owned avatar.
4. Team-owned assets do not count correctly in Store progress bar.
5. Online/offline state may still leak or display stale state across account switching.

## Likely Root Causes
- CreateTeamPage mutates CollectionView-bound lists during reload/selection.
- TeamEligibleAssetService and/or CreateTeamPage still merge defaults/owned items without strict PlayerId filter.
- Equip logic may not enforce one-equipped-per-type for current PlayerId only.
- Progress calculation may ignore Team asset types or exclude team-owned asset categories.

## Next Fix Order
1. Stabilize CreateTeamPage collection updates.
2. Fix team asset eligibility by Player1Id/Player2Id.
3. Fix avatar equip logic by CurrentPlayerId + AssetType.
4. Fix PlayerStoreProgressService / InventoryDisplayResolver counts.
5. Retest Android emulator.
