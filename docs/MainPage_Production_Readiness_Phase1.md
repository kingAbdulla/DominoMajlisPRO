# MainPage Production Readiness - Phase 1

## Scope
Functional stabilization only. No visual redesign, no layout hierarchy rebuild, and no image generation.

## Implemented in this branch
- Added `DominoMajlisPRO/MainPage.ProductionReadiness.cs` as a focused partial class.
- Added safe Empty State helpers for Team 1, Team 2, and Match Preview.
- Added post-handler and post-navigation readiness pass to repair stale or incomplete UI state after initial load.
- Added a guarded start-match tap pipeline in `DominoMajlisPRO/MainPage.StartMatchProductionGuard.cs`.
- Added initial auto-selection suppression so `LoadTeams()` no longer leaves the first two teams selected on first production render.

## Current implementation status
| Area | Status | Notes |
| --- | --- | --- |
| MainPage XAML layout | Preserved | No layout redesign was made. |
| Team empty state | Added | Missing team cards reset to explicit Arabic selection text and default shield. |
| Match preview empty state | Added | Preview shows explicit messages for missing team 1, missing team 2, or both missing. |
| Start-game protection | Added | Runtime guard validates readiness before forwarding to the existing `OnStartGame`. |
| Auto-selection behavior | Suppressed at runtime | Initial first-two-team auto-selection is cleared unless the user has already selected recent teams in the same page session. |
| Test data filtering | Deferred | Recommended: production data reset/seed policy instead of hardcoded name filtering. |

## Remaining direct-code improvement
A cleaner future pass should edit `MainPage.xaml.cs` directly to remove the old auto-selection code from `LoadTeams()` instead of suppressing it after initial load.

## Manual verification checklist
- Launch MainPage with zero teams.
- Launch MainPage with one team.
- Launch MainPage with two teams.
- Delete selected team and return to MainPage.
- Change team emblem/color/effect from Gallery and verify refresh.
- Try starting a match without two valid teams.
- Try repeated fast taps on Start Match.

## Risk
Medium until local build verification is completed. The implementation is isolated and preserves layout, but Android build/runtime testing is still required.
