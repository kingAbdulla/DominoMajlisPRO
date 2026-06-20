# Domino Majlis PRO — Copilot Instructions

Before any engineering task:

1. Read `/docs/00_READ_FIRST.md`.
2. Read every document under `/docs` in order.
3. Inspect the real implementation before changing code.
4. Preserve MVVM, AppShell, services, models, GalleryEngine, AppEvents, JSON storage, and approved XAML layout.
5. Use AccountId/ApplicationUserId, PlayerId, TeamId, AssetId, and ProductId as authoritative identifiers.
6. Never use display names as primary keys.
7. Build after logical changes.
8. Runtime verify Android/MAUI/store/identity changes.
9. Never report completion while known crashes or verification failures remain.
