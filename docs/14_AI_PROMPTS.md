# 14 AI PROMPTS

## Mandatory Start Prompt
Read `/docs/00_READ_FIRST.md` and `.github/copilot-instructions.md` before doing anything. Then inspect the actual repository implementation. Do not provide manual snippets unless repository tools are unavailable. Preserve architecture and layout. Build after each logical group. Do not report completion without runtime verification.

## GitHub Connected ChatGPT Prompt
Use the latest GitHub repository state as the source of truth. Rebuild your internal architecture map before answering. Do not ask me to paste files that already exist in the repository. Inspect actual implementation before suggesting modifications.

## Phase 2.8 Bug Prompt
Runtime verification found remaining bugs: RecyclerView crash in CreateTeamPage edit, team assets leaking across accounts, avatar equip not switching, team assets missing from progress. Inspect CreateTeamPage, TeamEligibleAssetService, PlayerInventoryService, PlayerAssetInventoryService, PlayerStoreProgressService, InventoryDisplayResolver, and AppEvents. Apply minimal fixes, preserve XAML layout, build, then provide emulator retest steps.
