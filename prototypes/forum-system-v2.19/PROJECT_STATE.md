# Forum System v2.19 — Activity-Type Reporting & Central Entity Registry

## Baseline
v2.18 is preserved unchanged as approved rollback baseline.

## Report activity-type scope
- Monthly and annual report builder now has Activity Type selector.
- Options: All activity types or a specific type (course, workshop, seminar, championship, campaign, visit, exhibition, conference, scientific, cultural, sports).
- The selected type filters the actual approved activity dataset before report creation.
- Participant totals, demographics, partners, support, tables and all calculated report KPIs use only filtered records.
- Report stores activityTypeFilter plus reportMonth/reportYear.
- ReportKey includes Activity Type so reports of different activity types do not collide.
- Official report metadata shows the selected activity scope.
- Single-activity reports inherit the activity's type automatically.

## Central Entity Registry
New centralEntities store and EntityId architecture:
- EntityId
- Official name
- Classification: official/non-official
- Category
- Short name
- Phone/email/address/notes
- Active/Archived status
- Usage counters for equipment, activity support and partnerships.

## Reuse & migration
- Existing rehabilitation providers are automatically harvested into central registry and linked via entityId.
- Existing activity support organizations and partner names are harvested without deleting legacy display text.
- Equipment registry has a Central Entity selector and stores entityId.
- Activity support has a Central Entity selector and stores supportOrganizationId.
- Report equipment/support uses the central entity through its RehabilitationRecord entityId.
- If a typed organization does not exist, the system automatically creates one EntityId and reuses it thereafter.
- Duplicate central entity names are blocked by normalized name matching.

## Integrity
- Logic Audit validates RehabilitationRecord.entityId and Activity.supportOrganizationId links.
- Backup/restore includes centralEntities.
