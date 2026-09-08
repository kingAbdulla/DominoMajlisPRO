# Forum Management System — Project State

## Current Version
v1.7 — Workflow & Reporting Prototype

## Adopted Architecture
- Zero-Cost First Architecture.
- Web-first; Excel is export/backup only.
- Dynamic Forums registry; new forums can be added without code changes.
- Forum gateway code (example: 001) identifies the forum only; user authentication is separate.
- Each user is bound to ForumID and optionally DepartmentID.
- Users cannot cross forum boundaries.
- Roles: System Administrator, Forum Manager, Department Employee, View-only (planned).
- Activity total participants is calculated automatically: MaleCount + FemaleCount, never manually editable.
- Audit trail and review history are mandatory.
- No hard deletion of approved operational records; use archive/status workflow.

## v1.7 Adopted Workflow
- Draft -> Submitted for Review -> Approved / Returned for Edit.
- Approved records cannot be directly edited by ordinary employees.
- Editing an approved record requires a Modification Request.
- Forum Manager / Administration Manager approves or rejects modification requests.
- Approved modification requests reopen the record and increment its version.
- All approval/rejection/modification actions must be recorded in AuditLog/ReviewLog.

## Reporting Constitution
- Dedicated "Prepare Report" action.
- Report types: Single Activity, Monthly, Annual; more types to follow.
- Reports use approved data only by default.
- Report preview before export.
- Export to Word.
- Export/Print to PDF.
- Share through device share sheet when supported.
- Report registry with ReportID, status, creator, date, forum and included records.
- Every issued/approved report MUST contain a QR Code.
- QR Code payload must at minimum identify: ReportID, ForumID, report status, issue timestamp.
- In the cloud version, the QR must resolve to a public verification page that reveals only safe verification metadata, not protected internal data.

## Current Prototype Limitation
v1.7 stores data in browser LocalStorage and is for interface/workflow testing only. It must NOT be used for real government data. Cloud authentication, server-side authorization, centralized database, backups and public QR verification are planned for the next cloud stage.
