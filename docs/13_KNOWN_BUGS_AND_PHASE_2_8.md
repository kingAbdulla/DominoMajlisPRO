# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 13 — Known Bugs and Phase 2.8 Context

## Phase 2.8 focus

Identity isolation + PlayerId/TeamId binding + Store publishing + full emulator verification.

## Known runtime defects discovered

- Default avatars appeared in My Assets as owned. Correct behavior: defaults available in picker but not owned.
- Team assets leaked across accounts. Correct behavior: team assets must filter by Player1Id/Player2Id ownership.
- Avatar equipment may fail to switch if equip state is cached globally or not scoped by PlayerId.
- Team asset ownership/progress may not count team-owned acquisitions.
- CreateTeamPage edit flow can crash with Android RecyclerView inconsistency when item lists change during layout.
- Online/offline state may leak if bound globally or by display name.

## Required debugging order

1. Reproduce.
2. Capture logcat / stack trace.
3. Identify service/page responsible.
4. Fix minimal logic.
5. Build.
6. Reinstall.
7. Retest only failing flow first.
