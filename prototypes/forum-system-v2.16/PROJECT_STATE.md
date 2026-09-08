# Forum System v2.16 — Approval Inbox & Workflow Timeline

## Baseline
v2.15 is the approved stable baseline and remains unchanged.

## Approval Inbox
- Forum Manager gets a formal decision inbox scoped strictly to own ForumId.
- Inbox contains submitted activities and reports awaiting Forum Manager approval.
- Director gets a Directorate Decision & Monitoring Center.
- Director does NOT re-approve reports already approved by Forum Manager.
- Director sees overdue/escalated pending items and a separate read-only stream of recently issued reports.
- SLA defaults to 48 hours and is configurable in technical security settings.
- Each queue item shows age, SLA state, forum, status and direct action buttons.
- Manager can approve/request change/reject activities and approve reports directly from the inbox.
- Director may intervene in pending activities, especially overdue items.

## Workflow Timeline
- Activity and Report timeline is derived from the canonical Audit Log.
- No separate divergent history source is used for official workflow display.
- Each event shows action, real summary/reason, actor UserId display, role and timestamp.
- Decision/rejection/cancellation events receive distinct timeline states.
- Report viewer now includes official administrative workflow history.

## SLA fields
- Activity submittedAt is stored when first submitted for review.
- Report submittedAt is stored on creation.
- Decision age is calculated from submitted/created time.
- Active decision inbox refreshes after data changes.

## Governance
- Employee/Viewer do not see the Approval Inbox.
- Forum Manager sees only own ForumId queue.
- Director sees Directorate scope.
- Admin remains technical and does not receive administrative decision controls.
