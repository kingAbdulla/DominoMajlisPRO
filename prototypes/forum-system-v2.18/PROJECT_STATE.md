# Forum System v2.18 — Equipment Registry Workflow & Reusable Entity Linking

## Baseline
v2.17 is preserved unchanged as the approved rollback baseline.

## Equipment/Rehabilitation Registry workflow
- Each entry is a reusable RehabilitationRecordId entity.
- Registry is grouped by provider/entity name.
- Open, Edit, Delete, Reject actions are available according to role.
- Forum Manager manages/rejects own ForumId records; Director has directorate scope.
- Rejection requires a reason, Audit Event and creator notification.
- Deletion is soft/archive with mandatory reason and Audit Event.
- Legacy records normalize to status=مسجل and version=1.0.

## Reusable activity linking
- Existing registry records can be selected in activity support.
- Selection auto-fills entity name, classification, date, book number and support/equipment description.
- If no record exists, the user types it once; saving the activity automatically creates a RehabilitationRecordId and links it.

## Reusable report linking
- Report Builder asks whether equipment/support is included.
- Existing RehabilitationRecordId can be selected and auto-fills report fields.
- If no record exists, user types the data and saving the report auto-creates the registry record.
- Single activity reports preselect a valid equipment record already linked to the activity.
- Report stores supportRegistryId and rehabilitationRecordIds[].

## ID-first integrity
- Activity.supportRegistryId -> RehabilitationRecordId
- Activity.rehabilitationRecordIds[] -> RehabilitationRecordId[]
- Report.supportRegistryId -> RehabilitationRecordId
- Report.rehabilitationRecordIds[] -> RehabilitationRecordId[]
- Logic Audit checks broken equipment links.
