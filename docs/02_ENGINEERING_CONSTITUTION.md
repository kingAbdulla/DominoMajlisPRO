# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 02 — Engineering Constitution

## Mandatory engineering workflow

Every change must follow:

1. Mission understanding
2. Architecture analysis
3. Impact analysis
4. Minimal implementation
5. Build
6. Runtime verification when practical
7. Regression report

## Forbidden engineering behavior

- Do not rewrite working systems to fix a small issue.
- Do not invent parallel services when existing services own the domain.
- Do not use display names as identifiers.
- Do not silently change storage format.
- Do not change scoring, Hall of Fame, or rankings rules unless the task explicitly requests it.
- Do not create UI redesign while fixing logic.

## Completion law

A task is not complete when code builds. A task is complete only when the requested behavior is verified and no known critical regression remains.
