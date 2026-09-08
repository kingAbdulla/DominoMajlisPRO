# Forum System v2.23 — Governance & Operational Hardening

## Baseline
v2.22 preserved unchanged as rollback baseline.

## Unified Filter Engine
- Added unifiedFilter(items, criteria, options).
- Director activity/report filtering now runs through the same engine.
- Engine supports ForumId, year, month, type, status and free-text query.
- Forum matching remains canonical via sameForum/canonicalForumId.

## Global Search
- New institutional search across Activities, Reports, Central Entities, Rehabilitation/Equipment records and Users.
- Search scope respects current role and ForumId.
- Director/Admin can search all forums; Employee/Manager remain scoped to own forum.
- Results open the real target record, not placeholders.

## Entity Relationship View
- Central Entity Registry now has an "العلاقات" action.
- Relationship view shows equipment/rehabilitation, supported activities, partnerships and reports linked to EntityId.
- Each relationship opens the underlying record.

## Data Quality Dashboard
- Director/Admin dashboard for data quality.
- Checks ForumId, User→Forum, Activity totals, missing IDs, Report→Activity links, Rejected Report reasons, Rehabilitation→Entity links and probable Entity duplicates.
- Produces quality score, critical issue count, warning count and checked record count.

## Health Monitor
- System Health score covers broken report links, broken rehabilitation links, unresolved Audit forum scope, duplicate central entity names and expired notifications.
- Provides health status and issue counters.

## Governance principle
- New modules are operational and ID-first.
- No new relationship is keyed by display name.
- v2.23 begins consolidation of previously page-specific logic into shared governance services.
