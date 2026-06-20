# 05 EXECUTION CONTRACT

## Mandatory Workflow
1. Mission understanding.
2. Architecture analysis.
3. Impact analysis.
4. Implementation plan.
5. Minimal implementation.
6. Build.
7. Runtime verification.
8. Regression verification.
9. Final report.

## Build Rule
Every logical group must be followed by:
`dotnet build "C:\Users\smart gen\source\repos\DominoMajlisPRO\DominoMajlisPRO.slnx" -c Debug`

## Android Rule
Runtime-sensitive features require Android emulator verification. If `adb` is unavailable, say blocked. Do not claim completion.

## Stop Conditions
Do not report success if any of these remain:
- Crash.
- RecyclerView inconsistency.
- Identity leak between accounts.
- Team asset ownership leak.
- Avatar equip failure.
- Store acquire/equip failure.
- AppEvents stale state.
- JSON save corruption.
