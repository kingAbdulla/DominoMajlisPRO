# Forum Management System — Project State

## Current Version
v1.9 — Government MIS Governance Prototype

## Official Functional Constitution
The system is now treated as a long-lived governmental Management Information System (MIS), not an online spreadsheet.

### Identity and Governance
- Government/Directorate identity is configurable from central settings.
- Forums remain dynamic and can be added without code changes.
- Forum gateway codes identify forum context only; user authentication and authorization are separate.
- Technical Developer and Administrative Director are separate roles.
- Developer does not automatically equal administrative approver.
- Emergency/technical actions must be auditable.

### Core Roles
- Department Employee
- Forum Manager
- Administration Director
- Developer/System Administrator
- View-only role planned for external organizations and observers.

### Activities
- Stable Activity Record ID.
- Independent record version number.
- Total participants = male + female automatically and is not manually editable.
- Optional official letter number/date.
- Optional activity owner, objective, outcomes, recommendations.
- Optional documentary images.
- Optional official attachments metadata.
- Approved records are never hard-deleted; archive/cancel/version workflows are used.

### Review Workflow
Submitted activity -> Preview complete details -> Approve / Reject / Request Modification.
Reject and Request Modification require mandatory reasons.
The submitting employee sees the decision and reason.
Every action is added to Timeline and AuditLog.

### Activity Detail Model
Each activity has:
- Data
- Images and attachments
- Administrative decisions
- Timeline
- Linked reports

### Timeline
Every important event must have:
- timestamp
- user
- action/event
- note/reason

### Notifications
Employees receive approval/rejection/modification decisions.
Managers receive pending-review notifications.
Director receives pending-report-approval notifications.

### Reporting Constitution
- Report is a first-class entity, not an instant printout.
- Report lifecycle: Draft Report -> Pending Report Approval -> Issued.
- QR is only authoritative on an issued report.
- Draft/non-issued reports display a visible watermark.
- Issued reports support Word, PDF/Print and sharing.
- Report includes government identity, Forum, ReportID, report version, status, issue metadata, activity references, participant statistics, approval data and signature placeholders.
- Report version is independent from Activity version.

### QR Verification
- Every issued report must have a QR Code.
- Prototype QR encodes safe verification metadata.
- Production QR must open a public verification endpoint that reveals only safe data:
  ReportID, issuing forum, issue date, version, current status.
- Revoked/cancelled reports must verify as revoked, not valid.

### Audit Log
AuditLog is mandatory and cannot be editable by ordinary users.
It records login, create, edit, submit, review, approval, rejection, modification, archive, report creation and report issuance.

### Archive
Records are grouped and retrievable by year without separate spreadsheet files.
Archived approved records remain searchable.

## Current Prototype Limitation
v1.9 still uses browser LocalStorage and should NOT be used with real government data.
Production/cloud stage requires:
- server-side authentication
- password hashing
- server-side ForumID/DepartmentID authorization
- centralized database
- immutable audit log
- object storage for images/documents
- backups
- report verification endpoint
- HTTPS deployment
