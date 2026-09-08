# Forum System v2.8 — ID-First Executive Government MIS

## Baseline
v2.7 is preserved. v2.8 begins the approved ID-first architecture and the five priority AAA workstreams.

## Implemented now
### 1. ID-first migration layer
Canonical immutable identifiers are introduced while legacy IDs remain as compatibility aliases:
- ForumId
- UserId
- RoleId
- ActivityId
- ActivityVersionId
- ReportId
- ReportVersionId
- VerificationId
- AuditEventId
- NotificationId

Relationships now begin using canonical IDs for authorship and identity:
- Activity.DataEntryUserId
- Report.WriterUserId
- Report.ApprovedByUserId
- ForumId on Activity/Report
QR uses VerificationId in addition to human-readable report reference.
No identifier is reused after delete/archive.

### 2. Dedicated official report templates
- Full single-activity A4-style formal report.
- Independent executive monthly report with cover, summary, KPIs, demographics, activities table and recommendations.
- Independent annual institutional-performance report with cover, annual summary, monthly table, achievements and next-year recommendations.
- Quarterly/comparison/directorate-statistical templates are visible as planned next templates, not falsely implemented.

### 3. Director AAA analytics
- Multi-series comparative chart: participants + approved activities + issued reports.
- Annual 12-month heatmap for every forum.
- Directorate monthly trend chart.
- Forum Performance Score (internal Director-only indicator) and grade:
  Excellent / Very Good / Good / Needs Follow-up.
- Existing ranking, trend, alerts and drill-down retained.

### 4. Director administrative user management
Director can:
- View users by immutable UserId.
- Change administrative role.
- Move user between ForumIds.
- Assign/remove Forum Manager role.
- Administratively suspend/reactivate access.
Director cannot change passwords/security/session settings; those remain Admin-only.

### 5. ID-aware audit and notification foundation
Audit events now carry AuditEventId, ActorUserId and TargetEntityId.
Notifications carry NotificationId, RecipientUserId and TargetEntityId.
Existing display fields are kept only for compatibility and UI.

## Still approved for subsequent implementation
All previously approved items remain in roadmap:
quarterly/comparison/statistical reports, advanced field-level audit diffs, activity version comparison, real typed attachments, attachment versioning, formal QR verification page, canonical document statuses, advanced search/saved filters, actionable notifications, dedicated Manager/Employee dashboards, Viewer role, temporary permissions, completeness score, duplicate detection, autosave, review locking, full configurable identity, print pagination, and Functional Freeze before cloud.

## Critical architecture rule
Names, usernames and report titles are NEVER relational keys. IDs are canonical. Friendly references are for display only.
