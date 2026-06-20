# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 09 — Inventory and Asset Ownership

## Default assets

Default assets are available choices, not owned assets.

Rules:

- Default avatars may appear in avatar selection catalogs.
- Default avatars must not appear in `My Assets` as owned.
- Default team emblems/colors/backgrounds may appear in CreateTeamPage as defaults.
- Default team assets must not be saved as player-owned purchases.

## Player-owned assets

Player-owned assets must be scoped by `PlayerId`.

A player-owned item must include:

- `PlayerId` / owner player id
- `AssetId`
- `AssetType`
- `IsOwned = true`
- acquired source/date where applicable

Equipping must affect only the current player.

## Team assets

Team asset availability in CreateTeamPage:

```text
Available = default team assets
          + assets owned by Player1Id
          + assets owned by Player2Id
```

Do not include assets owned by another account on the same device.
Do not include all published assets.
Do not use display names for ownership.

## Observed files

- `GalleryEngine/Services/PlayerAssetInventoryService.cs`
- `GalleryEngine/Services/PlayerInventoryService.cs`
- `GalleryEngine/Services/TeamAssetInventoryService.cs`
- `GalleryEngine/Services/TeamEligibleAssetService.cs`
- `Pages/CreateTeamPage.xaml.cs`
- `GalleryEngine/Models/PlayerOwnedStoreItem.cs`
- `GalleryEngine/Models/TeamOwnedAssetItem.cs`
