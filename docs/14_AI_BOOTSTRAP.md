# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 14 — AI Bootstrap

Any AI agent starting work on Domino Majlis PRO must begin with this prompt:

```text
Read /docs/00_READ_FIRST.md.
Read all documentation under /docs in order.
Read .github/copilot-instructions.md.
Inspect the real implementation before proposing changes.
Build an internal architecture map.
Do not modify code until analysis is complete.
Preserve architecture and layout.
Use IDs as authoritative identity keys.
Do not report completion without build and runtime verification.
```

## Daily AI workflow

After each GitHub push:

```text
Refresh the repository from latest main branch.
Re-read changed files.
Update architecture map.
Continue from latest implementation, not from memory.
```
