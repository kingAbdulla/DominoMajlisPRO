# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 12 — Runtime Verification Guide

## Minimum Android verification after identity/store changes

1. Clear data.
2. Launch app.
3. Create Developer account: `DevPublisher`.
4. Publish test assets.
5. Switch/logout.
6. Create normal account: `NormalTester`.
7. Acquire player assets.
8. Equip player assets.
9. Acquire team assets.
10. Open CreateTeamPage.
11. Verify only eligible team assets appear.
12. Switch back to Developer and verify NormalTester-owned assets do not leak.
13. Open PlayerProfilesPage and PlayerDetailsPage.
14. Open GamePage, HistoryPage, MatchDetailsPage, RankingsPage.
15. Watch logcat for fatal exceptions.

## ADB reminder

If adb is not in PATH, use full path:

```powershell
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" devices
```

## MAUI RecyclerView crash evidence

`Java.Lang.IndexOutOfBoundsException: Inconsistency detected. Invalid item position` usually means a CollectionView/ItemsSource was mutated unsafely during layout. Fix with atomic ItemsSource replacement and selection suppression.
