# Forum System v2.3
- Report approval moved to Forum Manager (MANAGER), per official workflow.
- Removed duplicate manager title field from report header; report now shows manager name only.
- Expanded activity/report data model: execution time, venue, hall name, activity responsible person, forum, official letter number/date, objective, outcomes, recommendations, description.
- Report structure upgraded with official outgoing number/date, subject, recipient, detailed execution data, statistical indicators, signatures and verification QR.
- Removed English text under QR. Only Arabic verification instruction remains.
- Restored Share Report button for issued reports using Web Share API with clipboard fallback.
- Word and PDF export remain available.
- Developer/Admin remains technical; Manager owns activity decision and report approval.
- Suggested next: page numbering for multi-page reports, configurable ministry/directorate identity, official document classification, attachments appendix, cover page for monthly/annual reports, and digital signature certificate support if the authority formally adopts it.


## Progress checkpoint — 2026-09-08
v2.3 is the currently approved baseline and must be preserved before the next phase.

### Approved baseline
- Forum Manager (MANAGER) approves activities and reports for their forum.
- Employee enters/edits/submits records but does not approve.
- Developer/Admin is a technical role and is not an administrative approver by default.
- Official report contains forum identity, report/outgoing metadata, execution details, participant statistics, objectives, description, outcomes, recommendations, manager signature, official stamp placeholder and QR verification.
- Issued reports expose Word, PDF/Print and Share actions.
- QR presentation is Arabic-only; raw/English verification text is not displayed beneath it.
- Participant total remains automatically calculated from male + female.
- Forum identity supports forum logo and manager electronic-signature image.

### Recommended next phase: v2.4 — Government Workflow & Document Registry
1. Build a real outgoing-document registry (سجل الصادر) with immutable serial number after issuance.
2. Separate report lifecycle: Draft -> Manager Review -> Approved/Issued -> Archived, with revision/version history.
3. Add audit log for create/edit/approve/reject/archive/delete actions with actor and timestamp.
4. Add notification center with unread badge counts and role-targeted notifications.
5. Add attachments/photos appendix and formal photo pages in exported reports.
6. Add distinct monthly/annual report templates instead of reusing the single-activity layout.
7. Add configurable authority identity: ministry/directorate names, official header/footer and contact data.
8. Add archive/search/filter by forum, date, activity type, status, report/outgoing number.
9. Improve QR verification page to show a concise official verification record rather than query-string metadata.
10. Harden role/permission matrix and prepare future multi-forum administration without cross-forum data leakage.

No v2.4 implementation has started at this checkpoint.


## Roadmap correction — outgoing registry excluded
The proposed internal outgoing-document registry (سجل الصادر) is CANCELLED and must not be implemented.
Reason: outgoing correspondence numbering is a sensitive official process already controlled by fixed official registers at the Directorate and at each forum. This system must not generate, increment, reserve, modify, or simulate official outgoing numbers.

### v2.4 approved scope
- Document lifecycle: Draft -> Review -> Approved/Issued -> Archived, with controlled revisions and version history.
- Immutable audit log for create/edit/approve/reject/request-change/archive/delete actions, recording actor, role, timestamp and reason where applicable.
- Notification center with unread badge counts and role-targeted notifications.
- Attachments/photos and a formal photo appendix in reports.
- Distinct templates for single-activity, monthly and annual reports.
- Configurable government/authority identity and official report header/footer.
- Searchable archive and filters by forum, date, activity type, status and report number.
- Official Arabic QR verification page showing verification status and core report metadata without exposing technical query strings.
- Hardened role/permission matrix and strict forum data isolation.
- Existing official outgoing-number fields should not become an automated registry. If retained at all, they are reference-only/manual metadata entered from the authoritative external register and must never be auto-generated or altered by workflow logic.

v2.4 is approved for implementation subject to these constraints.


## v2.4 Director Governance Upgrade — Approved

### Director Role
Director represents the Directorate Manager and is administratively above Forum Manager and below technical Admin.
Director permissions must include:
- View all forums and all administrative activity/report data.
- Review, edit, approve, reject, request modification, reopen approved records, archive and revoke approval with mandatory reason.
- View and manage administrative reports across all forums.
- View complete immutable Audit Log.
- Administrative user management for forum managers/employees, without access to sensitive technical system settings.
- No ability to alter technical security, database structure, developer settings, or delete Audit Log.

### Director Dashboard — Mandatory AAA Comparative Statistics
Director must have a dedicated Directorate-level dashboard, not a copy of Forum Manager dashboard.
The dashboard must compare all forums side-by-side using premium AAA information design.

Mandatory dashboard capabilities:
- One comparison card per forum.
- Total activities per forum.
- Approved / pending / rejected / modification-request counts.
- Total participants per forum.
- Male / female distribution.
- Monthly and annual activity trend.
- Reports issued / pending approval.
- Partner organizations count.
- Official visitors / VIP visits count.
- Activity type distribution.
- Current-month vs previous-month comparison.
- Current-year vs previous-year comparison.
- Ranking of forums by configurable metrics.
- Highlight best-performing forum and forums needing attention.
- Drill-down from Directorate dashboard into a specific forum.
- Filters: year, month, date range, activity type, status.
- Comparative charts across forums.
- Summary row for Directorate totals.

AAA design requirements:
- Executive-grade visual hierarchy.
- KPI cards, trend indicators, comparison bars and charts.
- Clear Arabic RTL layout.
- No clutter; critical exceptions and pending items surfaced first.
- Responsive desktop/tablet/mobile presentation.
- Director dashboard must prioritize decision-making, not data entry.

### Permission Engine
v2.4 must introduce capability-based permissions rather than scattered role checks, including:
CanApproveActivity
CanRejectActivity
CanRequestModification
CanReopenApprovedActivity
CanApproveReport
CanRevokeApproval
CanViewAllForums
CanViewDirectorDashboard
CanViewAuditLog
CanManageAdministrativeUsers
CanManageTechnicalSettings

This v2.4 Director Dashboard and permission model are now part of the approved roadmap.
