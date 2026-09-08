# Forum System v2.13 — Report Registry & Export Hardening

## Critical fixes
- Restored missing canEditReport(), the direct cause of every report row failing in v2.12.
- Role rules:
  Employee: edit own non-issued/non-revoked report.
  Forum Manager: edit reports only for own ForumId.
  Director: edit all administrative reports.
  Viewer/Admin: no administrative report editing.
- Report normalizer no longer depends on StatusCodes before initialization.
- Per-row recovery remains as secondary defense, not the normal path.

## Image/export hardening
- New forum logo uploads are normalized to max 512x512 PNG.
- Manager signature uploads are normalized to max 800x300 PNG.
- Existing stored identity images can be normalized with one button.
- Export clone adds inline hard constraints to logo, signature, QR, and photo images.
- PDF printing waits for all export images to load/decode before invoking print.
- Word uses the same hardened export clone + CSS with UTF-8 BOM.
- Large intrinsic source images can no longer create a standalone giant print page under normal rendering.

## QA invariant
A registry error banner is now considered an exceptional recovery path. Normal report records must render real rows/actions.
