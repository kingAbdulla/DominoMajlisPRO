# Phase K-P Production Readiness Gate

This file is the local release gate for the real-money commerce, wallet, friends, public profile, and authoritative progression contract.

The mobile client now fails safely for real-money recharge products. It does not grant gems, coins, VIP access, paid bundles, paid offers, or paid cosmetics from a button press or from local client-reported purchase state.

Real-money entitlements remain blocked until Google Play products, a secure verification backend, backend-owned ledgers, and server-side acknowledgement or consumption are configured. This is required by the contract and by the Google Play Billing purchase lifecycle.

## Completed Local Safety Changes

- Removed the recharge center first-open wallet seed grant.
- Replaced direct recharge package, offer, and VIP crediting with a verification-blocked purchase result.
- Added platform product mapping fields to recharge package, offer, and VIP catalog models.
- Added purchase lifecycle and audit fields to recharge purchase history records.
- Normalized stored recharge catalogs so hardcoded money prices are replaced by platform billing placeholders.
- Disabled non-platform payment method entries for real-money recharge products.
- Added a production backend client that fails closed when Supabase functions or user session tokens are unavailable.
- Added commerce API contracts for catalog loading, Google Play purchase-token submission, and authoritative wallet loading.
- Added feature flag contracts and a fail-closed kill switch model for high-risk features.
- Added friends API contracts that use public PlayerId lookup and server-owned relationship mutations.
- Added progression API contracts for server-owned match submission and authoritative projection loading.
- Added wallet sync helpers so client balances can be replaced by server projections without local invention.
- Progress rewards now count only purchase history entries with `RealMoneyPurchase` and `EntitlementGranted`.
- Store wallet purchases refund the local debit if inventory registration fails.
- Renamed the misleading store footer promise from "Secure 100%" to an account security center concept.
- Verified the Android release build compiles after these changes.

## Purchase Surface Matrix

| Purchase surface | Handler | Grants currency locally? | Server verified now? | Production state | Required external fix |
| --- | --- | ---: | ---: | --- | --- |
| Recharge gem packages | `RechargePurchaseService.PurchasePackageAsync` | No | No | Fail-closed | Add Google Play Billing launch flow and backend token verification. |
| Recharge limited offers | `RechargePurchaseService.PurchaseOfferAsync` | No | No | Fail-closed | Map offer product IDs to server catalog and Google Play products. |
| VIP subscription | `RechargePurchaseService.SubscribeVipAsync` | No | No | Fail-closed | Add subscription products, renewal, grace period, and cancellation verification. |
| First recharge reward | `ClaimFirstRechargeAsync` | Promotional only after granted history | Local history only | Blocked for production authority | Move validation and grant to backend. |
| Recharge progress rewards | `ClaimProgressRewardAsync` | Promotional only after granted history | Local history only | Blocked for production authority | Move validation and grant to backend. |
| Store item purchase with gems or coins | `StoreCheckoutService.PurchaseAsync` | Debits local wallet, refunds on failed grant | No | Local-only economy | Replace with atomic backend wallet spend and ownership grant. |
| Legacy avatar/background purchase | `StorePurchaseService.PurchaseAsync` | Debits local wallet, refunds on failed grant | No | Local-only economy | Replace with atomic backend wallet spend and ownership grant. |
| Wheel of Fortune | `StoreFeatureService.SpinWheelAsync` | Promotional reward | No | Local-only reward | Move rate limits, reward choice, and grant to backend. |

## Store Control Gate

Every visible store CTA must be one of these before release:

- Functional destination with authenticated server-backed behavior.
- Functional local-only feature clearly separated from paid or competitive authority.
- Explicit "coming soon" state.
- Removed from production UI.

Current production blockers:

- Real-money recharge and supporter purchases cannot be marked complete until official platform billing is wired.
- Account security center needs server session and recovery APIs for full functionality.
- Exclusive content can remain visible only if filtered from the published catalog and tied to real ownership.
- Restore purchases requires platform transaction recovery plus backend verification.

## Required Server APIs

| Area | Required endpoints |
| --- | --- |
| Commerce | `GET /commerce/catalog`, `POST /commerce/purchases/google-play`, `GET /purchase-history`, `POST /purchases/restore` |
| Wallet | `GET /wallet`, `POST /wallet/spend`, `POST /admin/grants`, `GET /wallet/ledger` |
| Friends | `POST /friends/search`, `POST /friends/requests`, `POST /friends/accept`, `POST /friends/decline`, `POST /friends/cancel`, `POST /friends/remove`, `POST /friends/block`, `GET /friends/list` |
| Public profile | `GET /profiles/public/{playerId}`, `GET /profiles/me/privacy`, `PUT /profiles/me/privacy` |
| Progression | `POST /matches/submit`, `GET /progression/projection`, `GET /rankings/projection`, `GET /progression/ledger` |
| Runtime safety | `GET /runtime/feature-flags`, `GET /health`, `POST /observability/client-event` |

All mutation endpoints must authenticate, authorize ownership server-side, derive the acting user from token claims, validate idempotency keys, and write append-only audit records.

## Test Matrix Required Before Release

| Scenario group | Minimum required evidence |
| --- | --- |
| Billing | Success, cancel, pending, duplicate token, replay, refund, revocation, server timeout, account mismatch. |
| Wallet | Atomic debit/grant, insufficient balance, concurrent purchases, refund-on-failed-grant, tampered local cache. |
| Friends | Two real accounts, search by PlayerId, request lifecycle, duplicate request, block, privacy filtering, pagination. |
| Public profiles | Remote read-only profile, equipped identity sync, no private fields, viewer cannot mutate target data. |
| Progression | Server match validation, duplicate idempotency key, impossible score rejection, local XP tamper ignored. |
| Feature flags | Each high-risk feature disabled server-side and verified to fail closed in the client. |

## External Blockers

- Google Play Console products are not available inside the local repository.
- Google Play Developer API service credentials must live on a server, not in the mobile app.
- A secure backend purchase-verification function must be deployed before any real-money success state is enabled.
- Server database tables and RLS/authorization policies must be implemented for purchase ledger, wallet ledger, friends, profiles, and progression.
- Real-time purchase notifications, refund processing, and reconciliation jobs require backend infrastructure.
- Apple StoreKit remains future scope until iOS distribution is configured.

## Release Rule

Do not report the real-money system, server-backed friends, public profiles, or authoritative XP/rank as production complete until the server endpoints and platform-console setup above are implemented and verified with the required two-account and billing-sandbox tests.
