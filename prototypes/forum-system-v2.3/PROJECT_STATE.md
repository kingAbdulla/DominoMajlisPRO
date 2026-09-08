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
