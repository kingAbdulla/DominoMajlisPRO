# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 10 — AppEvents Synchronization

`Services/AppEvents.cs` is the central synchronization mechanism.

## Events must be raised after

- player profile update;
- avatar/equipment change;
- team identity save;
- match update;
- ranking update;
- account/session switch;
- developer role activation;
- store publish/acquire/equip;
- reset/import/backup restore.

## Rules

- Do not create a parallel event bus.
- Subscribe/unsubscribe carefully to avoid loops and memory leaks.
- UI updates must happen on the main thread.
- Display pages should refresh from saved identity state, not mutate inventory.
