# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 05 — Execution Contract

## Required sequence

For every engineering task:

1. Read this documentation.
2. Inspect real implementation files.
3. Identify affected services/pages/models.
4. Propose the minimal safe plan.
5. Apply changes in a small group.
6. Build.
7. Fix compile errors.
8. Runtime verify.
9. Report honestly.

## Build rules

- Use Debug build for development verification.
- Use Release signed APK for final Android runtime checks if Debug/Fast Deployment causes native marker crashes.
- Do not ignore relevant warnings in identity, JSON, image, or platform code.

## Final report rules

Every report must include:

- files modified;
- root cause;
- exact behavior changed;
- build result;
- runtime result;
- remaining blockers;
- whether phase is complete or not complete.
