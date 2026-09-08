# Forum System v2.20 — Report Decision & Full Edit Integrity

## Baseline
v2.19 remains preserved unchanged as rollback baseline.

## Report rejection workflow
- Forum Manager receives a Reject button in report registry, report viewer, and approval inbox.
- Reject is available only while report awaits Forum Manager decision and only inside Manager ForumId.
- Rejection reason is mandatory.
- Report becomes Rejected, stores RejectedByUserId/RejectedAt/RejectionReason.
- Writer/stakeholders receive urgent actionable notification.
- Rejection is written to canonical Audit Log and appears in Workflow Timeline.
- Rejected reports remain editable and can be corrected/resubmitted.

## Full report editing
- Removed the reduced administrative-only edit form as operational edit path.
- Edit now opens the exact same complete Report Builder used for creation.
- All fields are restored: report type, activity/scope, month/year, activity type filter, subject, recipient, outgoing reference, manager, writer, support/equipment link, central entity, notes and recommendations.
- Pending/rejected reports are updated in place then resubmitted to Forum Manager.
- Issued reports are never modified in place: a complete new version is created and prior version becomes Superseded.

## Support/equipment duplication fix
- An explicit RehabilitationRecordId selected in the report is authoritative for report rendering.
- Activity-linked equipment is inherited only when the report has no explicit equipment selection.
- Equipment/support records are deduplicated using EntityId first, normalized provider name second.
- Single activity report no longer renders the same support entity once as prose and again as equipment row.
- Logic Audit detects duplicate equipment links inside reports.

## Notifications
- Stale approval notifications disappear after Issued, Rejected or Revoked states.
