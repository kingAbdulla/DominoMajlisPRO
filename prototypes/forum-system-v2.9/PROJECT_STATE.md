# Forum System v2.9 — Zero-Baseline Functional Governance

## Clean baseline
All seeded operational records are removed:
- Activities = 0
- Reports = 0
- Audit events = 0 on first load
- Notifications = 0 on first load
A new v2.9 localStorage namespace prevents v2.8 demo records from returning.

## Implemented in this tranche
- ID-first operational records retained.
- Viewer read-only role.
- Temporary access expiry support.
- Activity Data Completeness Score; review submission requires 100%.
- Duplicate activity detection using name + date + ForumId + location.
- Local autosave and recovery of activity drafts.
- Review Lock using LockId + reviewer UserId + expiry.
- Activity version history with ActivityVersionId.
- Field-level Audit diffs on activity edits.
- Typed attachments: images, PDF, Word.
- AttachmentId + AttachmentVersionId and same-filename version replacement.
- Downloadable attachments in activity preview.
- Advanced activity search/filter.
- Director Saved Filters using SavedFilterId.
- Canonical statusCode alongside Arabic display status.
- Functional backup export/import.
- Functional security policy settings.
- Functional QR settings.
- No future report types are exposed as disabled/nonfunctional choices.

## Still continuing
All remaining approved AAA roadmap items stay active: quarterly/comparison/directorate statistical reports; richer Manager/Employee dashboards; notification priorities/actions; better activity version comparison UI; richer attachment categories; report pagination/A4 preview; expanded Director charts; and final Functional Freeze before cloud.
