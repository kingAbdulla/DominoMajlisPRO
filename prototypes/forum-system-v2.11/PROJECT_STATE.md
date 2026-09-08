# Forum System v2.10 — Official Report Registry & Compact A4

## Corrections implemented
- Ordinary Employee can prepare reports using PrepareReport.
- Employee can edit only their own non-issued report using EditOwnReport.
- Approval/issuance remains Manager/Director authority.
- Reports button opens report registry ONLY.
- Report creation is a separate screen opened by "+ إعداد تقرير جديد".
- Report opening is a separate viewer screen.
- Internal ReportId / ReportVersionId / VerificationId are hidden from normal report document and registry UI.
- IDs remain canonical internally and in Audit/verification architecture.

## Forum identity fix
- Manager forum settings resolve by canonical ForumId from authenticated user.
- Login through forum code 001 resolves ForumId and automatically shows the corresponding forum name.
- Manager forum selector is locked to their own ForumId.
- Director/Admin with broader scope can select from permitted forums.

## Official report redesign
- Removed oversized monthly/annual cover behavior.
- Compact A4 document width and spacing.
- Smaller official header, metadata blocks, sections, signature area and QR.
- No raw version identifier shown.
- Management notes and recommendations are real report fields.
- Reports remain white/print-oriented regardless of application UI.
- A4 print stylesheet uses real A4 page size and compact margins.

## Report registry workflow
Reports screen = registry.
Each row exposes only applicable working actions: Open, Edit, Approve, Archive, Delete.
No prepared report is rendered automatically when entering registry.

## Next recommended AAA work
- Report type-specific validation before save.
- Page numbering for multi-page print/PDF.
- Directorate-level report template and cross-forum comparison report.
- Manager and Employee role-specific dashboards.
- Actionable notification inbox with urgency and direct navigation.
- Field-by-field activity version comparison UI.
- Functional Freeze QA matrix for every role/action.


## Saved checkpoint — Reports Access Repair
- v2.10 remains the approved current baseline.
- Reports Registry access is now required for every authenticated role.
- Employee: forum-scoped registry + PrepareReport + own pre-issue edit.
- Forum Manager: forum-scoped registry + prepare/edit/approve.
- Director: all-forums registry + prepare/edit/approve/revoke.
- Viewer: all-forums read-only registry.
- Admin: all-forums technical read-only registry; no administrative approval authority.
- Reports button always opens the registry first; report builder and report viewer remain separate screens.
- Admin dashboard now exposes Activity Registry and Official Reports Registry.
- openReports() is defensive against missing controls/session state and cannot depend on a role-specific dashboard.


## v2.11 Event Synchronization & Official Workflow
- Reports navigation hardened with openReportsSafe() and direct registry fallback.
- Report Registry remains first screen for all roles.
- Audit schema upgraded: EventType, ActorUserId, RoleId, ForumId, TargetEntityType, TargetEntityId, summary, reason, before, after, field, source.
- Audit UI redesigned as institutional timeline/table with KPI summary and filters.
- Login no longer writes role name into reason/details.
- Notifications fixed to canonical RecipientUserId and current UserId.
- Notification lifetime defaults to 30 days.
- Activity/report submission notifies Forum Manager(s) and Director(s).
- Approval/rejection/change request/revoke notifies linked record stakeholders.
- Notifications carry entity type/id, priority, expiry, actionRequired and direct-open action.
- Notification badge now derives from active, unread notifications for canonical UserId.
- Expired notifications can be removed.
- Event propagation is ID-first; display names are not routing keys.
