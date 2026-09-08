# v2.8.1 Functional Controls QA

## Defect fixed
The report Open action failed because the v2.8 migration created canonical ForumId values on Forum entities while existing Activity/Report foreign keys could remain legacy F001-style values. Report rendering then resolved no forum and failed before displaying the report.

## Corrective action
- Canonical FK repair now runs on every v2.8 load and repairs already-migrated localStorage.
- Forum/User/Activity/Report resolvers accept canonical and compatibility IDs.
- Report activity relationships are migrated to ActivityId.
- Report writer/approver relationships use UserId.
- Open report is read-capable for every role that can see the report.
- Report edit is operational for Forum Manager and Director.
- Editing an issued report NEVER mutates the issued version: it creates a new ReportId + ReportVersionId + VerificationId, marks the previous version historical, and returns the new version to review.
- Draft report edits update the draft and generate audit events.
- Disabled future report options were removed: no nonfunctional report-type choices remain.
- QR verification now routes to v2.8/verify.html using only VerificationId.
- Export/share actions validate that a report is actually open.
- All static onclick handlers in v2.8 resolve to defined functions.

## Architecture invariant
Names and usernames are display fields. Relationships and operations use immutable IDs.
