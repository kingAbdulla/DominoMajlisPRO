# 06 IDENTITY ARCHITECTURE

## Identity Law
Names are display-only. IDs are authoritative.

## Account
- `ApplicationUserId` / `AccountId` is the login/session identity key.
- `CurrentAccountId`, `CurrentPlayerId`, and `CurrentRole` must update on login, logout, switch, Developer activation, and reset.

## Player
- `PlayerId` is the only player identity key.
- Avatar, profile background, frame, effect, title, player visual identity, online/offline state, inventory, equipment, and store progress must bind by `PlayerId`.

## Team
- `TeamId` is the only team identity key.
- Team emblem, team color, emblem background, rankings display, Hall of Fame display, GamePage display, history snapshot, and match details must bind by `TeamId`.

## Developer Role
Developer is a role/permission on an account/player identity. Developer activation must not create a duplicate normal player with the same display name.

## Legacy Compatibility
ID-first with name fallback is allowed only for legacy data migration. New writes must store IDs.

## Current Runtime Defects To Guard Against
- Team assets acquired by one account visible to another account on same device.
- Online/offline status appearing shared between accounts.
- Avatar equip not replacing previous equipped avatar.
- CreateTeamPage edit crash caused by unstable collection refresh or inconsistent selection state.
