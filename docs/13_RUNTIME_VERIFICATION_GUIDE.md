# 13 RUNTIME VERIFICATION GUIDE

## Android Setup
Use ADB path if PATH is missing:
`C:\Program Files (x86)\Androidndroid-sdk\platform-toolsdb.exe`

## Required Commands
```powershell
$env:Path += ";C:\Program Files (x86)\Androidndroid-sdk\platform-tools"
adb devices
```

## Verification Flow
1. Install signed Release APK.
2. Clear data.
3. Launch app.
4. Create Developer account: `DevPublisher`.
5. Publish player and team assets.
6. Switch/create Normal account: `NormalTester`.
7. Acquire and equip player assets.
8. Verify My Assets excludes default avatars.
9. Acquire team assets.
10. Verify NormalTester sees owned team assets only when NormalTester is in the team.
11. Switch Developer and verify NormalTester assets are hidden.
12. Open/edit CreateTeamPage repeatedly and verify no RecyclerView crash.
13. Check PlayerProfilesPage, PlayerDetailsPage, MainPage approved slot.
14. Check GamePage, HistoryPage, MatchDetailsPage, RankingsPage, HallOfFamePage display paths.

## Crash Capture
```powershell
adb logcat -d > adb_log.txt
```
Search for: `FATAL EXCEPTION`, `IndexOutOfBoundsException`, `MauiRecyclerView`, `InvalidOperationException`.
