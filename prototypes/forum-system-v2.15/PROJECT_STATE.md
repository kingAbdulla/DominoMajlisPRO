# Forum System v2.15 — Canonical Sync & Workflow Integrity

## Baseline
v2.14 remains preserved as rollback baseline.

## Full logic audit corrections
- Introduced canonicalForumId(), entityForumId(), sameForum().
- Director statistics, month growth, drilldown, report counts and archive filters now use canonical ForumId.
- Director cards now receive forum.forumId, not legacy forum.id.
- Dashboard statistics use Number() aggregation and live arrays.
- Added syncActiveViews() so active dashboards/registries refresh after core data changes.
- Added Admin/Director "فحص المنطق" to validate ForumId relations, participant totals, report->activity links and dashboard consistency.

## Report semantics
- Single Activity Report = exactly one selected activity/course.
- Monthly Report = all approved activities for selected month in current forum.
- Annual Report = all approved activities for selected year in current forum.
- Activity selector is hidden for Monthly/Annual reports to eliminate misleading UI.
- Monthly/Annual titles are generated if blank.
- Single activity selection auto-fills subject/month/year.
- Report builder defaults to Single Activity Report.

## Manager delete
- Forum Manager now has a visible Delete action for every report in own forum.
- Issued reports are never physically destroyed: Delete moves them to archive with reason, DeletedByUserId, DeletedAt and Audit Event.
- Non-issued Manager deletion also moves to archive for institutional traceability.
- Other roles retain existing DeleteDraft/Archive rules.

## Canonical fixes outside Director
- Admin user creation now stores UserId + ForumId + RoleId.
- Admin forum creation now stores ForumId.
- Integrity checks resolve canonical ActivityId/ForumId.
- Archive filters respect strict forum confidentiality.
- Existing cross-forum rules remain: only Director/Admin may cross forum boundaries.

## QA principle
No dashboard metric may depend on display name, username, legacy forum string, or legacy id. All operational joins use immutable IDs.
