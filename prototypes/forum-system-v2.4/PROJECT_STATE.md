# Forum System v2.4 — Active Development

## Saved checkpoint
v2.3 remains the approved baseline.

## v2.4 first implementation tranche
Implemented:
- Capability-based Permission Engine.
- Explicit role separation: Employee, Forum Manager, Directorate Director, Developer/Admin.
- Director is administratively above Manager and below Admin technically.
- Developer/Admin does not automatically receive administrative approval capabilities.
- Dedicated Director Dashboard.
- AAA comparative forum statistics cards.
- Directorate-level KPIs.
- Forum ranking by approved activities, participants or growth.
- Best-performing forum highlight.
- Attention highlight for forums requiring follow-up.
- Comparative bar chart.
- Executive alerts.
- Filters by year, month, activity type and status.
- Month-over-month trend.
- Drill-down into forum details.
- Permission Matrix UI.

## Director permissions in this implementation
CanEditAnyForum
CanApproveActivity
CanRejectActivity
CanRequestModification
CanReopenApprovedActivity
CanApproveReport
CanRevokeApproval
CanViewAllForums
CanViewDirectorDashboard
CanViewAuditLog
CanManageAdministrativeUsers

Director does NOT receive CanManageTechnicalSettings.

## Admin/Developer
Technical role with CanManageTechnicalSettings and broad visibility/audit access.
Administrative approval capabilities are intentionally not granted by default.

## Next v2.4 work
- Wire Director/Manager activity actions to real state transitions.
- Add immutable Audit Log.
- Add notification center with unread badge.
- Add revoke approval / reopen workflow with mandatory reason.
- Harden forum isolation.
- Add monthly/annual report templates and attachment/photo appendices.
