# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 00 — READ FIRST

This repository contains a professional .NET MAUI application for real-world domino competition management. It is not a video game.

Before any AI or developer modifies code, the following documents must be read in order:

1. `01_PROJECT_MISSION.md`
2. `02_ENGINEERING_CONSTITUTION.md`
3. `03_ARCHITECTURE_CONSTITUTION.md`
4. `04_LAYOUT_PROTECTION.md`
5. `05_EXECUTION_CONTRACT.md`
6. `06_PROJECT_MAP.md`
7. `07_IDENTITY_ARCHITECTURE.md`
8. `08_STORE_GALLERY_ARCHITECTURE.md`
9. `09_INVENTORY_ASSET_OWNERSHIP.md`
10. `10_APP_EVENTS_SYNC.md`
11. `11_JSON_STORAGE_SAFETY.md`
12. `12_RUNTIME_VERIFICATION.md`
13. `13_KNOWN_BUGS_AND_PHASE_2_8.md`
14. `14_AI_BOOTSTRAP.md`
15. `15_SERVICE_REFERENCE.md`
16. `16_PAGE_REFERENCE.md`
17. `17_FILE_INDEX.md`

The repository is the implementation source of truth. These documents are the engineering policy source of truth.

If code conflicts with these documents, the agent must report the conflict and ask before changing architecture.

## Non-negotiable rules

- Analyze before coding.
- Preserve Shell navigation and MVVM-style separation.
- Preserve XAML layout unless the user explicitly requests redesign.
- Use `AccountId`, `ApplicationUserId`, `PlayerId`, `TeamId`, `AssetId`, and `ProductId` as identity keys.
- Treat names as display-only.
- Build after logical changes.
- Runtime verify on Android when the bug is Android/MAUI/UI related.
- Do not report completion while known crashes or verification failures remain.
