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
