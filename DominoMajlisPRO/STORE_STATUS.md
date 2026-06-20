# Domino Majlis PRO Premium Store — Stable Checkpoint

Stable as of 2026-06-18. Windows and Android must remain at zero build errors after every Store change.

Phase 1 final stabilization completed on 2026-06-19.
Phase 2.1 functional and propagation pass completed on 2026-06-19.
Phase 2.2 universal asset role propagation, canonical Owned categories,
and player event log controls completed on 2026-06-19.

## Stable architecture

- **Navigation Core:** `StoreNavigationState` is the single lightweight state for Store views, selected category, and 12-item paging.
- **Category Chips:** the default RTL chips always remain available; CMS content may add categories without removing the defaults.
- **Quick Actions:** route through Store navigation without loading every product on Home.
- **Bottom Navigation:** Store Home and Limited Offers use `StoreNavigationState`; Rewards and Account retain premium modal behavior.
- **Show All:** New Arrivals and Limited Offers open their dedicated paged views; Browse Categories navigates through category cards rather than product purchase actions.
- **CMS taxonomy:** `Category` defines the asset type, `Collection` provides non-hierarchical grouping metadata, and optional `Season` associates seasonal content. Legacy `ParentCategoryId` JSON is read as `Collection`.
- **Store queries:** `StoreAssetQueryService` centralizes Published, Visible, non-expired, newest-first, and AssetId-deduplicated Store content queries with invalid-record protection.
- **Hero Slider:** all valid Published and Visible active season heroes are shown, ordered by SortOrder/newest date and deduplicated only by AssetId. Publishing another season does not remove prior published seasons; auto-slide remains enabled.
- **Product Action Sheet:** `StoreProductActionSheet` remains the reusable modal Preview / Purchase / Cancel surface with blocked touch-through and opening/closing animations.
- **Preview Overlay:** `StoreProductPreviewOverlay` is a separate full-screen, visual-only premium preview. It must never mutate ownership, wallet, inventory, equipment, or persisted player data.
- **Inventory display:** `InventoryDisplayResolver` resolves player and team ownership through `StoreAssetCatalogService`. User-facing inventory surfaces never fall back to AssetId, ProductId, or GUID values; incomplete entries display `عنصر غير مكتمل البيانات`.
- **Collection progress:** `PlayerStoreProgressService` projects totals and per-type counts from the same Inventory Engine snapshot used by My Items, including supported player and team asset families.
- **Refresh contract:** inventory, team asset, and published-catalog events refresh Store state, My Items, player pages, Collection Progress, and Inventory Audit without duplicate subscriptions.

## Phase X System Completion Matrix

| Phase | System | Completion | Status |
|---|---|---:|---|
| Phase 1 | Canonical Asset Types and Inventory Routing | 100% | Stable |
| Phase 1 | Developer Inventory Audit v2.1 | 100% | Stable |
| Phase 1 | Inventory Display Resolver | 100% | Stable |
| Phase 1 | Collection Progress Sync | 100% | Stable |
| Phase 1 | Inventory Refresh Synchronization | 100% | Stable |
| Phase 1 | Windows and Android Build Gate | 100% | 0 errors; no new warnings |
| Phase 2 | Canonical paid product checkout | 80% | Avatar/background and canonical routed products complete; purchase history/refund hardening remains |
| Phase 2 | Player asset propagation | 90% | Main player header, account hub, player lists, player details, and Hall player cards use role-correct catalog assets; device runtime matrix remains |
| Phase 2 | Team asset propagation | 90% | TeamColor, Emblem, and EmblemBackground verified in Main, Game, History, Match Details, Rankings, and Hall of Fame without cross-role replacement |
| Phase 2 | Canonical Owned categories | 100% | My Items and picker routing share the canonical eight-category AssetType catalog |
| Phase 2 | Player event log controls | 100% | Initial 10, 10-item Show More batches, single delete, Delete All confirmation, and semantic duplicate prevention complete |
| Phase 2 | Immediate synchronization | 85% | Inventory/player/team/store events integrated without duplicate subscriptions |
| Phase 2 | Responsive asset replacement | 90% | Existing dimensions and hierarchy preserved; overlays are non-layout-changing and images retain existing AspectFit/AspectFill behavior |
| Phase 2 | Store quick-action completion | 25% | Daily Offers works; Wheel, Season Pass, Rewards, Account, and protected Top Up remain |
| Phase 2 | Overall | 68% | Phase 2.2 complete; quick-action destinations and physical-device runtime matrix remain |

## Protected systems

Do not modify Store CMS Core, purchase, economy, wallet, inventory persistence, player persistence, `StoreProductActionSheet`, `StoreProductPreviewOverlay`, GalleryPage layout, match logic, rankings, trust, Hall of Fame, or anti-cheat unless a task explicitly authorizes that exact system.

## Next recommended task

**Phase 2 continuation** — complete the non-protected quick-action destinations and run the physical-device/window-size verification matrix.
