# Codex Execution Contract - MainPage Phase 1 Functional Completion

## Mission
Complete `MainPage` functional readiness for market preparation without redesigning the page.

## Protected Architecture / Layout Protection Policy
- Do not redesign `MainPage.xaml`.
- Do not move controls, alter the approved visual hierarchy, or change spacing unless a compile/runtime bug requires it.
- Work only on logic, state handling, empty states, synchronization, and validation.

## Files to inspect first
- `DominoMajlisPRO/MainPage.xaml`
- `DominoMajlisPRO/MainPage.xaml.cs`
- `DominoMajlisPRO/MainPage.ProductionReadiness.cs`
- `DominoMajlisPRO/MainPage.HeaderAvatarParentSync.cs`
- `DominoMajlisPRO/MainPage.HeaderAvatarRuntimeEnforcer.cs`
- `DominoMajlisPRO/MainPage.HeaderAvatarNavigationSync.cs`
- `DominoMajlisPRO/MainPage.HeaderEffectSync.cs`
- `DominoMajlisPRO/MainPage.ArabicRuntimeTextRepair.cs`
- `DominoMajlisPRO/MainPage.SettingsSymbols.cs`

## Required implementation
1. Replace the current `LoadTeams()` auto-selection behavior:
   - Do not auto-select the first two teams in production.
   - Prefer a saved last selection if a persistence mechanism already exists.
   - Otherwise show explicit empty state.

2. Update `UpdateMatchPreview()`:
   - Never return while leaving stale text or old emblems visible.
   - If both teams are missing: show `اختر فريقين لبدء المواجهة`.
   - If team 1 is missing: show `اختر الفريق الأول`.
   - If team 2 is missing: show `اختر الفريق الثاني`.
   - Keep `PreviewRulesLabel` synchronized.

3. Update `RefreshSelectedTeamsFromIds()`:
   - When a selected team no longer exists, reset its card through the empty-state helper.
   - Always call the match preview empty-state flow after refresh.

4. Update `OnTeamPicked()`:
   - When selecting team 1 and it clears team 2 because of duplicate selection, reset team 2 using the empty-state helper.
   - Always update the preview state after selection.

5. Update `OnStartGame()`:
   - Add duplicate-tap protection.
   - Use a readiness gate before continuing.
   - Release the gate if validation fails or navigation throws.
   - Preserve all existing anti-self-match and single-vs-double validation.

6. Keep Visual Identity intact:
   - Preserve `TeamIdentityResolver.ResolveAsync`.
   - Preserve `TeamEffectEngine.ApplyAroundAsync`.
   - Preserve player header avatar/frame/effect logic.

## Runtime verification
- Build Android target.
- Launch app with zero teams.
- Launch with one team.
- Launch with two teams.
- Delete/edit selected team and return to MainPage.
- Change team emblem/color/effect from Gallery and verify immediate reflection.
- Fast-tap Start Match repeatedly.

## Crash Safety Gate
- No unhandled exceptions.
- No null UI access.
- No stale selected team after deletion.
- No duplicate navigation to `GamePage` from repeated taps.

## Final report required
Report:
- Files changed.
- Functions changed.
- Build result.
- Manual verification result.
- Remaining risk.
