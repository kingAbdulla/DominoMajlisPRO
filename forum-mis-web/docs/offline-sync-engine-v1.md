# Offline Sync Engine v1 — BasraForumMIS

Status: Adopted architecture (2026-09-09)

## Goal
The web MIS must remain usable during temporary internet outages and synchronize safely when connectivity returns. Supabase remains the shared source of truth; the browser keeps a scoped offline working cache plus an explicit synchronization queue.

## Core rules
1. **Scoped local cache** — cache keys are partitioned by `Application UserID + ForumID`. A user must never receive another forum's cached working set through the offline layer.
2. **Stable IDs** — activities, reports, rehabilitation/equipment records, notifications and related records keep their canonical IDs while offline and online.
3. **Sync queue** — every local mutation is represented as an operation containing:
   - `CREATE | UPDATE | DELETE`
   - `EntityId / RowId`
   - `Collection`
   - `ForumId`
   - `UserId`
   - `LocalVersion`
   - `Timestamp`
   - `BaseUpdatedAt`
   - payload when applicable.
4. **Conflict detection** — before UPDATE/DELETE, compare the queued base server timestamp with the current cloud row. A changed cloud row becomes a conflict instead of being overwritten.
5. **No unsafe automatic merge** — reports, approved/official records and audit-sensitive records are never silently merged when a concurrent modification is detected. They enter `CONFLICT` and require review.
6. **Retry** — network/transient failures remain queued and retry automatically after reconnect, with bounded retry backoff.
7. **Status UI** — the header cloud badge must communicate:
   - تمت المزامنة
   - بانتظار المزامنة
   - يتم الرفع
   - تعارض يحتاج مراجعة
   - فشل المزامنة
   - وضع دون اتصال
   and expose last successful synchronization time in its tooltip/state.
8. **Realtime ordering** — incoming Realtime events trigger a pull only after local pending work is flushed/checked, so remote updates cannot silently erase unsynchronized local work.
9. **Supabase RLS is authoritative** — offline scoping improves privacy and UX, but never replaces server-side RLS.

## v1 implementation boundary
- Browser-scoped cache and queue in `cloud-adapter.js`.
- Automatic queueing from existing `onLocalSave` bridge.
- CREATE/UPDATE/DELETE detection by comparing the new local collection with the scoped cached collection.
- Reconnect flush, retries and conflict state.
- Cloud hydration fallback to scoped cache when offline.
- Existing LocalStorage application models remain compatible during migration; the offline layer owns its own scoped cache and queue.

## Next hardening steps
- Add a conflict-review screen with side-by-side local/cloud values and role-gated resolution.
- Add immutable audit events for conflict resolution.
- Consider IndexedDB for larger datasets/attachments once the v1 behavior is proven.
- Add server-side row version if timestamp-based optimistic concurrency becomes insufficient.
