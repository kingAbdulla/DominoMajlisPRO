# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 08 — Store and Gallery Architecture

## Observed subsystem

The store/gallery subsystem lives under `GalleryEngine/`.

Key areas:

- `GalleryEngine/Admin/` — developer/admin manager pages.
- `GalleryEngine/Services/` — inventory, checkout, equipment, display, identity, wallet, catalog services.
- `GalleryEngine/Models/` — owned items, wallet, store purchase, team identity, payload models.
- `GalleryEngine/Pages/GalleryPage.xaml.cs` — user-facing gallery/store entry.
- `GalleryEngine/Components/` — reusable UI controls.

## Store concepts

Keep these concepts separate:

1. Published asset/product
2. Owned asset
3. Equipped asset
4. Default available asset

Publishing does not mean ownership. Default availability does not mean owned inventory.

## Store manager rules

- Only Name/Title/Description/Button Text may be free text in admin publishing flows.
- AssetType is mandatory.
- Category/subcategory are metadata/filtering only.
- AssetId should be generated automatically for new assets.
- Picker display should show clean display name, not raw GUID.

## Protected files

- `GalleryEngine/Admin/SpecializedStoreManagerPage.cs`
- `GalleryEngine/Admin/DeveloperStoreManagerPage.xaml.cs`
- `GalleryEngine/Services/StoreCheckoutService.cs`
- `GalleryEngine/Services/StorePurchaseService.cs`
- `GalleryEngine/Services/StoreEquipService.cs`
- `GalleryEngine/Services/StoreAssetCatalogService.cs`
- `GalleryEngine/Services/InventoryDisplayResolver.cs`
