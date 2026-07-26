# Phase 6 Store Team Context Evidence

Date: 2026-07-26

## Scope

This remediation targets the Phase 6 multi-team store defect: team products and player-owned team visual products must not be equipped to an implicit or arbitrary team when a player belongs to more than one team.

## Changes

- Added an explicit optional `TeamId` to `InventoryProductContext`.
- Updated `InventoryRouter` to validate and use explicit `TeamId` for:
  - team-owned products,
  - team effects,
  - team name effects,
  - team name frames.
- Added a team lookup API based on `PlayerId` in `TeamProfileService`.
- Added team selection before store acquire/equip actions when the current product requires team context and multiple eligible teams exist.
- Added service-level team context validation in `StoreCheckoutService` before paid wallet debit.
- Merged persisted `TeamAssetInventoryService` records into `TeamEligibleAssetService` so CreateTeamPage can see assets saved directly against a `TeamId`.
- Verified that no `preview_frame` or `preview_fram` references remain in C#, XAML, JSON, or XML under the application project.

## Static Verification

- Searched for `preview_fram|preview_frame`: no matches.
- Confirmed team context is no longer resolved only by `CurrentTeamIds`; explicit `TeamId` is accepted and membership is validated.
- Confirmed CreateTeamPage eligible assets now include:
  - defaults,
  - team inventory records,
  - player-owned team visual records from team members.

## Build Verification

Command:

```powershell
dotnet build "C:\Users\smart gen\source\repos\DominoMajlisPRO\DominoMajlisPRO\DominoMajlisPRO.csproj" -c Release -f net10.0-android --no-restore
```

Result:

- Build succeeded.
- Warnings: 448.
- Errors: 0.

## Remaining Runtime Verification Required

The following must still be executed on the Android emulator before this phase can be marked complete:

- Player belongs to one team, buys/equips a team item, CreateTeamPage shows it.
- Player belongs to multiple teams, store asks which team receives the item.
- Cancelling team selection leaves wallet and inventory unchanged.
- Paid team item validates `TeamId` before wallet debit.
- Equipped TeamEffect appears on MainPage, GamePage, RankingsPage, and CreateTeamPage preview.
- TeamNameEffect and TeamNameFrame appear on the selected team only.
- Same player in another team does not receive the selected team's equipped item unless explicitly equipped there.

## Current Verdict

Static and build verification passed. Runtime/emulator verification is still required by the release contract before declaring Phase 6 complete.
