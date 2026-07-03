# MainPage Production Readiness - Phase 1

## Scope
Functional stabilization only. No visual redesign, no layout hierarchy rebuild, and no image generation.

## Implemented in this branch
- Added `DominoMajlisPRO/MainPage.ProductionReadiness.cs` as a focused partial class.
- Added safe Empty State helpers for Team 1, Team 2, and Match Preview.
- Added post-handler and post-navigation readiness pass to repair stale or incomplete UI state after initial load.
- Added readiness gate helpers for match-start validation and duplicate start protection foundation.

## Current implementation status
| Area | Status | Notes |
| --- | --- | --- |
| MainPage XAML layout | Preserved | No layout redesign was made. |
| Team empty state | Added | Missing team cards now reset to explicit Arabic selection text and default shield. |
| Match preview empty state | Added | Preview now has explicit messages for missing team 1, missing team 2, or both missing. |
| Start-game protection foundation | Added | Helper methods added; next pass should integrate directly into existing `OnStartGame`. |
| Auto-selection behavior | Not fully changed yet | Existing `LoadTeams()` still auto-selects first two teams in `MainPage.xaml.cs`. This needs a direct edit in the next pass. |
| Test data filtering | Deferred | Avoided hardcoded content-specific blocking in code. Recommended: production data reset/seed policy instead. |

## Next required code pass
Directly edit `MainPage.xaml.cs` to:
1. Stop `LoadTeams()` from auto-selecting the first two teams unless there is a saved last selection.
2. Call `ApplyProductionEmptyStateIfNeeded()` inside `RefreshSelectedTeamsFromIds()` after selected teams are deleted or missing.
3. Replace the early return in `UpdateMatchPreview()` with the new empty-state flow.
4. Integrate `ConfirmProductionMatchReadinessAsync()` and `ReleaseProductionMatchStartGate()` into `OnStartGame()`.
5. Add a persisted last-selected-team pair if desired.

## Manual verification checklist
- Launch MainPage with zero teams.
- Launch MainPage with one team.
- Launch MainPage with two teams.
- Delete selected team and return to MainPage.
- Change team emblem/color/effect from Gallery and verify refresh.
- Try starting a match without two valid teams.
- Try repeated fast taps on Start Match.

## Risk
Low to medium. The added file is isolated, but direct integration into `OnStartGame()` is still pending.
