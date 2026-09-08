# Forum System v2.14 — Authority Isolation & Staged Reporting

## Stable baseline
v2.13 is preserved unchanged as the approved near-ideal rollback baseline.

## Implemented
### Staged report notifications
- New report preparation notifies Forum Manager(s) only.
- Director no longer receives "awaiting approval" for a report before Forum Manager decision.
- After Forum Manager approves/issues a report, Director receives an informational notification that the Forum Manager approved it.
- Informational Director notification is not action-required.
- Stale "awaiting approval" report notifications are automatically hidden once the report is issued.

### Duplicate report detection
- Report uniqueness key is now ForumId + ReportType + Scope + Subject, or ForumId + ActivityId for single-activity reports.
- Different report subjects in the same month/year no longer trigger a false duplicate warning.
- Same exact report still triggers versioning protection.
- scopeKeyV2 migrates legacy reports without deleting prior records.

### Cross-forum confidentiality
- Only Director and Admin can cross forum boundaries.
- Manager and Employee are locked to their authenticated ForumId.
- Viewer is also forum-scoped; it no longer receives global ViewAll.
- Activity forum filter for non-authorized roles contains only their forum and is disabled.
- Audit forum filter is similarly scoped.
- Settings forum selector is locked by ForumId.
- Saved filters cannot inject a different ForumId for local roles.
- Report builder ignores foreign forum filters for non-cross-forum users.

### Word export
- Word uses a dedicated Word-compatible export clone.
- Logo uses explicit width/height HTML attributes (72x72px) in addition to inline CSS.
- Signature and QR also have Word-readable dimensions.
- Header grid is converted to a fixed HTML table for Word compatibility.
- PDF retains the existing A4 pipeline that is already working correctly.
