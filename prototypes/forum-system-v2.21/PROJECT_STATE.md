# Forum System v2.21 — Director Canonical Forum Filter

## Baseline
v2.20 preserved unchanged as rollback baseline.

## Director filter repair
- Forum selector no longer depends on transient DOM selection.
- New directorForumFilterState stores canonical ForumId independently.
- applyDirectorForumFilter captures selection before dashboard rerender.
- populateDirectorForumFilter rebuilds options while preserving canonical selected ForumId.
- All forum comparisons use canonicalForumId/sameForum instead of raw string equality.
- Director KPIs, cards, executive alerts, heatmap and monthly multi-series chart consume the same canonical filter state.
- Active filter scope badge clearly displays selected forum/year/month.
- Director action buttons open Activities and Reports already scoped to the selected ForumId.
- Activity and Report list filtering also canonicalizes ForumId.
- Forum drilldown inherits current Director period/type/status filters rather than silently reverting to all-time data.
