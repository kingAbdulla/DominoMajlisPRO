# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 03 — Architecture Constitution

## Actual repository architecture observed

The uploaded project contains a .NET MAUI single project with:

- Solution: `DominoMajlisPRO.slnx`
- Project folder: `DominoMajlisPRO/`
- App-level services in `Services/`
- UI pages in `Pages/`
- Domain models in `Models/`
- Gallery/store subsystem in `GalleryEngine/`
- Platform folders under `Platforms/`
- Resources under `Resources/`

## Core folders observed

```text
GalleryEngine/Admin
GalleryEngine/Components
GalleryEngine/Models
GalleryEngine/Pages
GalleryEngine/Services
Models
Pages
Services
```

## Architectural pillars

- `AppShell` / Shell navigation must remain the navigation foundation.
- Services own business logic.
- Pages own view behavior and binding glue only.
- Models represent data and persistence shape.
- GalleryEngine owns store/gallery/inventory/equipment concerns.
- AppEvents is the synchronization mechanism.
- JSON storage remains the persistence layer unless the user explicitly approves migration.

## Change policy

Prefer extending or correcting existing services:

- `ApplicationUserService`
- `PlayerProfileService`
- `TeamProfileService`
- `RankingService`
- `PlayerAssetInventoryService`
- `PlayerInventoryService`
- `TeamAssetInventoryService`
- `TeamEligibleAssetService`
- `PlayerVisualIdentityResolver`
- `InventoryDisplayResolver`
- `TeamIdentityResolver`

Do not duplicate these responsibilities in pages.
