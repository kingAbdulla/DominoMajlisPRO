# Forum Management System — Project State

## Current Version
v1.8 — Administrative Review, Editing, Images & Official Reporting Prototype

## Mandatory Review Workflow
For every submitted activity, authorized management must be able to:
1. Preview the complete activity and all entered details.
2. Approve.
3. Reject with a mandatory reason.
4. Request modification with a mandatory reason.

The data-entry user must see the management decision and reason directly beside the activity.

## Editing Policy
Editing is permitted to:
- Department employee
- Forum manager / authorized manager
- Developer / system administrator

Any edit to a previously approved record reopens the record for review and increments its version. Audit history remains mandatory.

## Images
Activity images are optional.
Prototype limit: up to 3 images and approximately 1 MB per image because v1.8 still uses browser LocalStorage.
Cloud version must store images in managed object storage, not database rows.

## Reporting Constitution
Reports must look and behave as official administrative reports, not simple statistics.
Mandatory report content includes:
- Government/Directorate header
- Forum name
- Report ID
- Issue date
- Report type and status
- Activity references
- Activity date/location/type
- Participant statistics
- Organization/partner
- Official visitors
- Activity description
- Record version
- Approval metadata
- Optional documentary images
- Signature / seal placeholders
- QR Code

## QR Code
Every issued/approved report must contain a QR Code.
v1.8 uses an image-based QR generator rather than an external JavaScript QR library for Safari reliability.
Prototype QR contains safe verification metadata: ReportID, ForumID, status, issue date.
Cloud version must make the QR open a public verification page backed by the central database.

## Security Boundary
v1.8 remains a LocalStorage prototype and must not be used with real government data.
Production requires server-side authentication, server-side ForumID/DepartmentID authorization, centralized DB, backups, secure image storage, report verification endpoint, password hashing, and immutable audit logging.
