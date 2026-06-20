# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 11 — JSON Storage Safety

The app uses JSON files as persistence.

## Rules

- Missing files return empty safe data.
- Corrupt JSON must not crash display pages.
- Saves should use per-file locks and temp-file atomic replacement where implemented.
- Never wipe inventory because image resolution fails.
- Do not write during read-only page rendering unless necessary.

## Store repository

`GalleryEngine/Admin/Core/StoreCmsJsonRepository.cs` is a critical protected file for store CMS persistence.

## Data integrity

Any reset/delete operation must be Developer-only and must preserve backup/audit policy where implemented.
