# Forum Management System v2.0
- Removed standalone Reviews button. Managers act directly from Activities: Preview, Edit, Reject, Request Modification, Approve.
- Notification button has unread numeric badge.
- Archive supports Restore and, for authorized management, Restore & Approve for both activities and reports.
- Forum logo is configurable per forum and appears in official reports.
- Removed report watermark.
- Report layout upgraded to formal government-style structure with header, forum logo, report metadata, statistics, details, signatures, seal placeholder and QR verification.
- QR now encodes a real verification URL instead of plain text, preventing camera/Google from treating it as a search query.
- Verification page displays report ID, forum ID, status, version and issue date.
- Production cloud stage must replace query-string verification with a database-backed verification endpoint so revocation/archive status is always live.
