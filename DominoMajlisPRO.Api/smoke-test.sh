#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${1:-http://127.0.0.1:8080}"
USERNAME="preview_$(date +%s)"
PASSWORD="Preview123!"

curl --fail --silent "$BASE_URL/api/health" >/dev/null

REGISTER_JSON=$(curl --fail --silent -X POST "$BASE_URL/api/preview/register" \
  -H 'Content-Type: application/json' \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")

TOKEN=$(printf '%s' "$REGISTER_JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin)["accessToken"])')

curl --fail --silent -X POST "$BASE_URL/api/preview/me/teams" \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"name":"فريق الاختبار"}' >/dev/null

TEAM_COUNT=$(curl --fail --silent "$BASE_URL/api/preview/me/teams" \
  -H "Authorization: Bearer $TOKEN" | python3 -c 'import json,sys; print(len(json.load(sys.stdin)))')

test "$TEAM_COUNT" -eq 1

curl --fail --silent -X POST "$BASE_URL/api/preview/logout" \
  -H "Authorization: Bearer $TOKEN" >/dev/null

STATUS=$(curl --silent --output /dev/null --write-out '%{http_code}' "$BASE_URL/api/preview/me/teams" \
  -H "Authorization: Bearer $TOKEN")

test "$STATUS" -eq 401
printf 'Preview API smoke test passed.\n'
