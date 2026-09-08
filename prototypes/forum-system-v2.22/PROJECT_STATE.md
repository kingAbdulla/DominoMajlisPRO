# Forum System v2.22 — Institutional Audit Canonical Forum Filter

## Baseline
v2.21 is preserved unchanged as rollback baseline.

## Audit forum filter repair
- Added independent auditForumFilterState; audit filtering no longer depends on transient select DOM state.
- auditTargetForumId resolves ForumId from the target entity when AuditEvent.forumId is absent.
- Resolution supports Activity, Report, Forum, RehabilitationRecord and ForumProfile targets.
- All comparisons use canonicalForumId.
- Director can select any forum and see only its institutional audit events.
- KPI cards (total events, decisions, updates, events today) now reflect the filtered audit scope rather than all directorate events.
- Active scope badge shows selected forum and audit event type.
- Audit rows display resolved forum name for traceability.
- Search includes resolved forum name.
- Employee/Forum Manager audit scope remains locked to their own forum where access exists.

## Additional ID consistency cleanup
- scopedActs() and scopedReports() now use sameForum() rather than raw ForumId string equality.
- Logic Audit warns when an entity-bound audit event cannot resolve a ForumId.
