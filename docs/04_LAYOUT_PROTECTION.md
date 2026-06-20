# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 04 — Layout Protection Policy

This project has protected visual layouts. AI agents must not redesign pages unless the user explicitly asks for UI redesign.

## Protected

- XAML hierarchy
- Grid definitions
- margins / padding / spacing
- card structure
- navigation flow
- CollectionView / CarouselView structure
- approved page composition

## Allowed without redesign approval

- Binding fixes
- Command fixes
- ViewModel updates
- Service integration
- AppEvents subscription/unsubscription fixes
- ItemsSource refresh safety fixes
- Display resolver fixes
- ImageSource resolution fixes
- Visibility binding fixes

## MAUI RecyclerView safety

For CollectionView-backed UI on Android, avoid mutating `ObservableCollection` while RecyclerView is laying out. Prefer atomic replacement on the UI thread:

- build a new list off to the side;
- assign `ItemsSource = null` if needed;
- assign a new list/collection on the main thread;
- suppress selection-change handlers during reload;
- never modify the bound collection from background threads.

This is critical for `Java.Lang.IndexOutOfBoundsException: Inconsistency detected. Invalid item position` crashes.
