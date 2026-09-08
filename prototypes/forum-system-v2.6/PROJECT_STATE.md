# Forum System v2.6 — Administration & Data Governance

## Approved architecture
Four distinct layers are now enforced:
1. Employee — data entry/submission.
2. Forum Manager — forum administration and operational decisions.
3. Directorate Director — higher administrative authority across all forums.
4. Admin/Developer — technical governance only.

## Admin Dashboard
Implemented working prototype sections:
- Technical Admin dashboard.
- Account management: create, enable/disable accounts, bind roles/forums.
- Forum management: create, activate/deactivate forums.
- Data Integrity center.
- Technical security placeholder.
- Backup center placeholder.
- QR technical settings placeholder.
- Audit Log access.
- Permission Matrix.
- System-health KPIs and recent audit events.

Admin does NOT receive administrative approval/rejection rights.

## Delete Governance
- Forum Manager and Director may delete only non-approved operational records (draft/rejected/request-change) with mandatory reason.
- Non-issued reports may be deleted by authorized administrative roles with reason.
- Approved activities and issued reports cannot be directly deleted; they use revoke/archive workflows.
- Permanent deletion from archive is exceptional and Director-only, with mandatory reason + typed DELETE confirmation.
- Admin is not an administrative-delete authority.
- Every deletion is written to Audit Log.

## Data Integrity
Prototype checks:
- Orphan report/activity references.
- User bound to missing forum.
- Duplicate forum gateway codes.
- Participant total mismatch.
- High-level counts and health status.

## Next work
- Director administrative user-management UI (separate from technical Admin user management).
- Notification rules for account/forum changes.
- Multi-series Director charts and annual heatmap.
- Cloud backend: real server-side RBAC, immutable audit storage, backups, and database constraints.


## Approved checkpoint — v2.6
v2.6 is now the approved current baseline and must be preserved before further changes.

### Newly approved reporting/data-entry identity requirement
- Every activity record must store a dedicated Data Entry / Recorder name field (اسم مدخل البيانات / من قام بتسجيل النشاط).
- Every report must store a dedicated Report Author / Report Writer name field (اسم كاتب التقرير / من قام بإعداد التقرير).
- These fields are distinct from:
  - activity responsible person (مسؤول النشاط)
  - forum manager (مدير المنتدى)
  - approving authority (المعتمد)
- The system should auto-suggest the logged-in user's full name, while allowing authorized administrative correction if required.
- The values must appear in:
  - activity details
  - report metadata
  - audit/timeline context where relevant
  - exported Word/PDF report
  - archive/search filters in future versions

### Formal AAA direction — approved principles
- Executive government-grade Arabic RTL visual hierarchy.
- Strong separation between data entry identity, activity ownership, reviewer, approver, and technical administrator.
- Formal document lifecycle and immutable historical trace.
- Consistent government document metadata and reference structure.
- Dedicated official report templates by report type.
- Formal attachments/photo appendix.
- Searchable annual archive.
- Director executive analytics and Admin technical governance remain separate.
- No automation of official outgoing-number registers; outgoing references remain manual/reference-only when used.
