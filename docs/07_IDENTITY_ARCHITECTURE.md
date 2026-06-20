# Domino Majlis PRO — Official Engineering Manual v1.0

Status: Official project reference for AI agents and developers.
Source basis: uploaded DominoMajlisPRO repository snapshot.

---

# 07 — Identity Architecture

## Authoritative keys

- Account/session: `ApplicationUserId` / `AccountId` where present.
- Player identity: `PlayerId`.
- Team identity: `TeamId`.
- Store asset identity: `AssetId` / canonical asset ID.
- Product identity: `ProductId` where store product records exist.

## Display-only fields

The following must never be used as primary identity keys:

- `DisplayName`
- player name
- team name
- developer display name
- visible UI text

## Observed identity-related files

- `Models/ApplicationUserModel.cs`
- `Models/CurrentUserSessionModel.cs`
- `Services/ApplicationUserService.cs`
- `Services/DeveloperLockService.cs`
- `Services/HonorIdentityService.cs`
- `Services/PlayerProfileService.cs`
- `Services/TeamProfileService.cs`
- `Services/PlayerTeamSyncService.cs`
- `GalleryEngine/Services/PlayerVisualIdentityResolver.cs`
- `GalleryEngine/Services/TeamIdentityResolver.cs`

## Required behavior

- Developer activation must attach role/permission to the current account/player where possible.
- Switching accounts must update current user, current player, current role, inventory view, equipment, online/offline state, and store ownership state.
- A normal account must not inherit Developer inventory or Developer equipment.
- Similar display names must not cross-link accounts.

## Legacy compatibility

ID-first lookup may retain name fallback only for legacy persisted data. Any fallback must be documented and should not be used for new writes.
