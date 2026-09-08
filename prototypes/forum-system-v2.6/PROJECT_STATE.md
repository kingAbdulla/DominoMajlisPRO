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
