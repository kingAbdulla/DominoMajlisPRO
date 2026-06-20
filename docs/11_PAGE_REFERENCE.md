# 11 PAGE REFERENCE

## CreateTeamPage
Purpose: create/edit teams and choose team visual identity.

Rules:
- Use Player1Id/Player2Id for eligibility.
- Use TeamId for saved team identity.
- Default team assets are always visible.
- Player-owned team assets visible only when owner PlayerId is in team.
- Replace ItemsSource atomically on Android to prevent RecyclerView inconsistency.
- Suppress selection events during reload.

## PlayerDetailsPage / PlayerProfilesPage
Must display visual identity by PlayerId. Must not mutate inventory. Must not resolve by display name except legacy fallback.

## MainPage
Display-only page. Must not calculate ownership. Must read current session/player identity and approved display slots.

## GamePage / HistoryPage / MatchDetailsPage
Game display must use saved TeamId or match snapshot. Historical matches must not recalculate identity from current inventory.

## RankingsPage / HallOfFamePage
Do not change ranking/Hall qualification logic while fixing identity display. Use TeamId-first visual display.

## GalleryPage / Store pages
Store visibility != ownership. Published items are globally visible if published, but are not owned until acquired.
