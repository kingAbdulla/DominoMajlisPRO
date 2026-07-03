# Codex Execution Contract - MainPage Phase 1 Functional Completion

## Mission
Complete `MainPage` functional readiness for market preparation without redesigning the page.

## Protected Architecture / Layout Protection Policy
- Do not redesign `MainPage.xaml`.
- Do not move controls, alter the approved visual hierarchy, or change spacing unless a compile/runtime bug requires it.
- Work only on logic, state handling, empty states, synchronization, and validation.

## Required implementation
1. Stop `LoadTeams()` from auto-selecting first two teams unless a saved last selection exists.
2. Ensure `UpdateMatchPreview()` never leaves stale text or old emblems visible.
3. Reset cards when selected teams are deleted or missing.
4. Add duplicate-tap protection to `OnStartGame()`.
5. Preserve all existing identity resolver and team effect behavior.

## Verification
- Build Android target.
- Test zero teams, one team, two teams, team deletion, identity change, and repeated Start Match taps.
