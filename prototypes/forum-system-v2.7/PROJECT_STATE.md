# Forum System v2.7 — Official Records & AAA Documents

## Baseline
v2.6 remains preserved as the approved Administration & Data Governance baseline.

## Implemented in v2.7
### Identity of authorship
- Dedicated Data Entry Name field on every activity.
- Auto-suggests logged-in user's full name.
- Stored separately from Activity Responsible Person.
- Displayed in activity preview and official single-activity reports.
- Included in Audit context.
- Dedicated Report Writer field.
- Auto-suggests logged-in user's full name.
- Stored separately from Forum Manager and Approver.
- Displayed in report metadata, signature area, report table and Audit context.

### Official authority identity
- Configurable Country / superior authority.
- Configurable Ministry / superior organization.
- Configurable Directorate name.
- Address, contact, official email and official footer.
- Used in official report header/footer.
- Editable by Directorate Director or technical Admin, not forum employees.

### Report version governance
- New report revision links to previous report.
- Previous revision is marked non-current.
- Current/previous version status is visible in report list.
- Version information is displayed in report preview.

### Formal report enhancements
- Single-activity report preserves full official detail.
- Monthly/annual reports include an executive summary.
- Formal photographic appendix added for activity images.
- Photo caption includes activity and date.
- Word/PDF/Share/QR remain part of issued report workflow.

### Archive improvements
- Search by activity/report ID and name.
- Search by Data Entry Name / Report Writer.
- Filter by forum and year.

## Next recommended implementation
- Dedicated monthly report cover and multi-page layout.
- Dedicated annual report executive template.
- Real page-numbering engine for PDF/print.
- Director administrative user-management UI.
- Multi-series Director analytics and annual heatmap.
- Cloud backend with server-side RBAC, immutable Audit Log and database-backed QR verification.


## Architecture Constitution — ID-First Government MIS (Approved for v2.8+)

All 30 approved AAA/government-system requirements are adopted into the roadmap. The system MUST be ID-first across every domain and workflow. Display names, usernames, report titles, forum names, and human-readable labels are never relational keys.

### Canonical identifiers
- UserId: immutable person/account identity.
- ForumId: immutable forum identity.
- DepartmentId: immutable department/administrative-unit identity.
- ActivityId: immutable activity identity.
- ReportId: immutable report identity.
- ReportVersionId: immutable identity for each report revision.
- AttachmentId: immutable attachment identity.
- AttachmentVersionId: immutable identity for each attachment revision.
- NotificationId: immutable notification identity.
- AuditEventId: immutable audit event identity.
- RoleId: immutable role identity.
- PermissionId: immutable permission/capability identity.
- SessionId: immutable authenticated session identity when backend is introduced.
- VerificationId: public document-verification identity used by QR; must not expose internal IDs unnecessarily.
- SavedFilterId: immutable saved-filter identity.
- ApprovalActionId: immutable review/approval/rejection/change-request action identity.
- LockId: immutable review/edit-lock identity.

### Relationship rules
- All relationships use IDs only.
- Usernames are authentication/display attributes, never ownership keys.
- Full names are snapshots/display metadata, never relational keys.
- Activity.DataEntryUserId identifies who entered the activity.
- Activity.ResponsibleUserId may be nullable if responsible person is external; otherwise references UserId.
- Report.WriterUserId identifies report author.
- Report.ApprovedByUserId identifies approver.
- Activity.ForumId and Report.ForumId reference ForumId.
- Administrative reassignment changes relationship IDs without rewriting historical authorship.
- Historical snapshots preserve the displayed name/title at the moment of issue while the canonical relationship remains ID-based.
- Deletion/archival must never cause identifier reuse.
- IDs are generated once and remain immutable.
- UI may show friendly short reference numbers, but these are separate from canonical IDs.
- QR verification uses VerificationId/public token, not raw internal primary keys.
- Audit Log records ActorUserId + TargetEntityType + TargetEntityId + before/after values.
- Notifications target RecipientUserId and optionally TargetEntityId.
- Permissions are resolved from RoleId/PermissionId, not role-name string comparisons in the future backend.

### Approved v2.8 scope
1. Full official A4 single-activity report.
2. Independent executive monthly report.
3. Independent executive annual report.
4. AAA Director dashboard: multi-series charts + annual heatmap.
5. Director administrative user management.
6. Begin ID-first migration layer in prototype while preserving v2.7 compatibility.

### Full adopted roadmap
Also approved for subsequent implementation: quarterly/comparison/statistical report templates; forum performance score; stronger field-level audit/change history; real typed attachments and attachment versioning; formal photo appendix; government QR verification page; canonical document statuses; advanced search and saved filters; priority/actionable notifications; separate Manager/Employee dashboards; Viewer role; temporary access; data-completeness scoring; duplicate detection; autosave; review locking; configurable authority identity; UI/print separation; real A4 print preview; and Prototype Functional Freeze before cloud/backend migration.
