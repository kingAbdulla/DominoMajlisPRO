# Offline Sync Runtime Gate

## Purpose
Do not mark Offline Sync production-ready until every mandatory case below passes on a real device/browser against the deployed Supabase schema.

## Preconditions
- main contains the current Offline Sync hardening commits.
- Supabase schema.sql changes are applied to the target database.
- Test accounts exist for:
  - Employee in Forum A
  - Forum Manager in Forum A
  - Employee in Forum B
  - Developer
- Browser/device storage starts from a known clean state for the first run.

## Gate A — Offline Create
1. Sign in as Forum A employee while online.
2. Hydrate successfully.
3. Disconnect network.
4. Create a new Activity.
5. Verify local save succeeds and sync state shows pending/offline.
6. Close and reopen the app while still offline.
7. Verify the Activity is still visible from the scoped cache.
8. Reconnect.
9. Verify queue flush succeeds exactly once.
10. Verify Supabase cloud_documents row:
   - collection = activities
   - forum_id = Forum A
   - owner_user_id = employee user_id
   - payload contains the Activity
11. Verify queue is empty afterward.

PASS only if no duplicate row, no lost row, and owner/forum metadata are correct.

## Gate B — Manager edits employee-owned record
1. Sign out employee.
2. Sign in as Forum A manager.
3. Open the employee-owned Activity.
4. Go offline.
5. Edit the Activity.
6. Verify queued operation contains:
   - ownerUserId = original employee user_id
   - actorUserId = manager user_id
   - forumId = Forum A
7. Reconnect.
8. Verify update succeeds.
9. Verify cloud owner_user_id remains the employee.
10. Verify updated_by/auth actor corresponds to the manager session.

PASS only if ownership is preserved and manager authorization succeeds.

## Gate C — Forum isolation
1. Sign in as Forum B employee.
2. Attempt to hydrate/read Forum A records.
3. Verify Forum A Activity is not returned.
4. Attempt direct client query by known collection + row_id for Forum A record.
5. Verify RLS returns no accessible row.
6. Verify central_entities remain readable as intended.

PASS only if Forum A data is inaccessible while central_entities remain available.

## Gate D — Update/Delete conflict semantics

### D1 Remote update vs local update
1. Device A loads record.
2. Device B updates same record and syncs.
3. Device A edits stale copy offline and reconnects.
4. Verify queue item becomes CONFLICT.
5. Verify neither side is silently overwritten.

### D2 Remote delete vs local update
1. Device A loads record.
2. Device B deletes record and syncs.
3. Device A updates stale local record and reconnects.
4. Verify conflict is raised.
5. Verify record is not silently resurrected.

### D3 Remote update vs local delete
1. Device A loads record.
2. Device B updates and syncs.
3. Device A deletes stale copy and reconnects.
4. Verify conflict is raised.
5. Verify remote update is not silently deleted.

### D4 LOCAL resolution for DELETE conflict
1. Resolve D3 using LOCAL.
2. Verify an actual DELETE executes.
3. Verify no payload=null row is created.

## Gate E — Logout/session cleanup
1. Create at least one delayed local write.
2. Trigger logout immediately.
3. Verify flushTimer, pullTimer and writeTimers do not execute after logout.
4. Verify realtime channel is removed.
5. Verify queue/cache remain scoped and preserved for the original account.
6. Sign in as a different user and verify no old-user queue is processed.

## Gate F — Inactive account
1. Sign in with an Active account and hydrate.
2. Disable the account server-side.
3. Reconnect/reload while online.
4. Verify profile load is rejected.
5. Verify Supabase auth session is cleared.
6. Verify inactive profile cache is not reused.

## Gate G — Forum directory authority
1. As Developer, add a forum through Admin UI.
2. Verify forum_directory contains the exact ForumId/code/name/active state.
3. Verify lookup_forum(code) succeeds.
4. Disable the forum through Admin UI.
5. Verify forum_directory.active=false.
6. Verify lookup_forum(code) no longer returns the forum.
7. Re-enable and verify routing works again.

## Production decision
Offline Sync may be marked Production Ready only when:
- Gates A through G all pass.
- No FAILED or CONFLICT queue entries remain unexplained.
- No cross-forum leakage is observed.
- Ownership remains distinct from actor identity.
- Logout produces no post-session writes.
- The deployed Supabase schema matches the repository schema.
