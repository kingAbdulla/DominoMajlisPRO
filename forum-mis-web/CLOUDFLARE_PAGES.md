# Cloudflare Pages deployment

This project is a plain static site. No framework or build step is required.

## Recommended Cloudflare Pages configuration

- Git provider: GitHub
- Repository: kingAbdulla/DominoMajlisPRO
- Production branch: main
- Root directory: forum-mis-web
- Framework preset: None
- Build command: leave blank
- Build output directory: .
- Environment variables: none required for the current static deployment

After the first deployment, Cloudflare Pages will assign a `*.pages.dev` URL.

## Build watch path

To avoid rebuilding Pages for unrelated DominoMajlisPRO changes, configure:

- Include: `forum-mis-web/**`

## Production URL plan

1. Temporary production-like URL: `<project>.pages.dev`
2. Final official URL: connect a subdomain owned by the directorate/organization.
3. Keep Supabase as the database, authentication and sync backend.

## Verification after first deploy

- Root URL opens directly with no intermediary warning page.
- Forum portal code `001` resolves to `FRM-F001` / منتدى الرعاية العلمية.
- Directorate portal does not request a forum code.
- Existing authenticated session can restore from cache.
- Offline create queues locally and uploads after reconnect.
- Browser refresh gets the current HTML/config rather than a stale copy.

The `_headers` file is intentionally configured so HTML and cloud-config do not remain stale in browser cache, while cloud-adapter may use a short cache with stale-while-revalidate.
