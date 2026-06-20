# 08 APP EVENTS AND SYNCHRONIZATION

## Official Event Bus
`Services/AppEvents.cs` is the official app synchronization mechanism.

## Existing Events
- `DataChanged`
- `RankingsChanged`
- `TeamsChanged`
- `MatchesChanged`
- `PlayerProfileChanged`
- `CurrentUserChanged`
- `StoreEconomyChanged(playerId)`
- `StoreProgressChanged(playerId)`
- `TeamAssetsChanged(teamId)`

## Required Raises
Raise relevant events after:
- login/logout/switch account
- Developer activation
- publish asset/product
- acquire/purchase asset
- equip player asset
- select team emblem/color/background
- save team
- reset data

## Rule
Do not create parallel event systems. Do not refresh pages by stale global state. All user-specific updates must include PlayerId where available.
