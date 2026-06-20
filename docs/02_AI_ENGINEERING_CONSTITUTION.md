# 02 AI ENGINEERING CONSTITUTION

## Required AI Behavior
- Analyze before coding.
- Inspect actual files before proposing changes.
- Do not invent snippets when repository access exists.
- Preserve the current MAUI architecture.
- Build after each logical group of changes.
- Treat Android emulator verification as mandatory for runtime-sensitive tasks.
- Never report completion if emulator verification is skipped or blocked.

## Forbidden AI Behavior
- Do not redesign UI unless explicitly requested.
- Do not move controls, grids, page sections, margins, padding, or navigation flow.
- Do not use DisplayName, PlayerName, TeamName, or account visible name as primary identity keys.
- Do not create parallel identity, event, inventory, or storage systems.
- Do not declare Phase completion after build success only.

## Required Report Format
Every implementation report must include:
- Mission summary.
- Files modified.
- Exact changes.
- Build result.
- Runtime verification result.
- Remaining risks.
- Whether Android verification passed, failed, or is blocked.
