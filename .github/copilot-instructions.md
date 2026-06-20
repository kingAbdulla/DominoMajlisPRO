# Domino Majlis PRO — Repository Instructions

Read `/docs/00_READ_FIRST.md` before any engineering task.

Mandatory rules:
- Inspect actual files before editing.
- Preserve .NET MAUI AppShell architecture.
- Preserve MVVM/service architecture.
- Preserve XAML layout unless explicitly asked to redesign.
- Never use DisplayName/PlayerName/TeamName as primary identity keys.
- Use ApplicationUserId, PlayerId, TeamId, AssetId, ProductId as authoritative IDs.
- Do not create parallel AppEvents, inventory, session, or storage systems.
- Build after logical changes.
- Do not report completion without Android runtime verification for runtime-sensitive bugs.
