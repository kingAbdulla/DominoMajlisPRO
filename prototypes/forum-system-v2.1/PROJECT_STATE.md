# Forum System v2.1
- Added permanent-delete option for archived activities/reports, manager-only, with two-step confirmation and typed DELETE confirmation.
- Restored age-group dropdown with predefined categories.
- Fixed forum logo workflow: select -> preview -> explicit "Adopt Logo" -> persist to forum record -> appear in reports.
- Added duplicate-report detection using scopeKey. If a current report already exists for the same activity/month/year, user must either open it or explicitly create a new report version.
- New report versions increment independently (e.g. V1.0 -> V1.1) rather than creating confusing identical reports silently.
- Recommended next: add soft-delete retention period and duplicate detection for activities as well, plus report templates per activity type.
