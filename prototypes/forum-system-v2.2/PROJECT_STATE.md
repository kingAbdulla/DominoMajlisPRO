# Forum System v2.2
- Restored Word export for issued reports.
- QR is visible and uses a verification URL.
- Added Forum Manager name/title fields to report builder.
- Added configurable Forum Manager name/title and electronic signature image in forum settings.
- Report layout redesigned to look more official: formal header, report metadata block, purpose, activity details, statistics, approval section, signatures, QR verification, footer reference.
- Role separation enforced:
  Employee: create/edit/submit only.
  Forum Manager: review/approve activities in own forum, edit activities, prepare report drafts.
  Administration Director: view all and approve/issue final reports.
  Developer/Admin: technical identity/settings only by default; not an administrative approver.
- Report lifecycle: Forum Manager prepares -> Pending Administration Director Approval -> Issued.
- Developer role no longer receives activity/report approval buttons by default.
- Suggested next: configurable Directorate/Ministry names, official outgoing number (رقم الصادر), report subject, recipient organization, document classification, and certificate-based e-signature if officially adopted.
