# Forum MIS — Cloud Setup (Supabase Free)

## What is implemented
The production web app now has a cloud adapter design for Supabase Auth, PostgreSQL/RLS, Realtime synchronization, and localStorage offline cache.

## One-time setup
1. Create a free Supabase project.
2. Open SQL Editor and run `supabase/schema.sql`.
3. In Authentication > Users create the first account with email `admin@forum-mis.local` and a strong password.
4. Run the final commented INSERT in schema.sql to create the admin profile.
5. Copy only:
   - Project URL
   - Publishable key (sb_publishable_...)
   Never copy a Secret/Service key into this repository or browser code.
6. Put the URL and publishable key in `forum-mis-web/cloud-config.js` and set `enabled: true`.
7. Sign in as admin and use the production migration control to upload the current local data once.

## Username convention
The UI can continue to accept short usernames. In cloud auth, username `manager` maps internally to `manager@forum-mis.local`.

## Security
RLS is enabled. The publishable key is intentionally browser-safe only because every table is protected by grants + RLS. Secret/service keys must never be shipped to the browser.
