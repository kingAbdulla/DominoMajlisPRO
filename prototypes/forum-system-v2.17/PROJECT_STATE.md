# Forum System v2.17 — Forum Profile, Support & Rehabilitation Registry

## Baseline
v2.16 is preserved unchanged and approved as rollback baseline.

## Director filter
- Added a real Forum filter to Director Dashboard.
- Filter is populated from canonical ForumId values.
- Activity KPI, participants, forum count, comparison cards, heatmap and monthly trend now respect the selected forum.
- "All forums" returns full directorate scope.

## Forum institutional profile
New ForumProfileId entity linked by ForumId:
- Forum name / ForumId
- Geographic postal/address description
- Optional browser GPS latitude/longitude
- Permanent staff (ملاك)
- Contract staff (عقود)
- Male staff
- Female staff
- Total staff
- Validation requires Permanent + Contracts = Male + Female.
Employee and Forum Manager can maintain only their own forum profile.
Updates create Audit events; employee changes notify Forum Manager.

## Rehabilitation & equipment registry
New RehabilitationRecordId entity:
- ForumId
- Type: equipment / rehabilitation / maintenance / furnishing / devices / other
- Date
- Entity name
- Entity classification: official / non-official
- Official book/order number and date
- Description and notes
Records are forum-scoped and auditable.

## Activity enhancements
- Target audience is now free text.
- Official visitor has Name + Job Title.
- Activity support: Yes/No.
- If Yes: SupportEntityId, entity name, official/non-official classification, support nature.
- Activity can link to one or more RehabilitationRecordId records.
- All fields participate in activity version snapshots, edit/autosave and preview.

## Report integration
- Single activity report includes support entity and linked rehabilitation/equipment records.
- Monthly reports include counts of supported activities and activities linked to rehabilitation/equipment.
- Director forum cards include institutional support/equipment indicators.
- Forum drilldown shows employee profile and rehabilitation/equipment count.

## ID-first
All new relationships use IDs. Names are display values only.
