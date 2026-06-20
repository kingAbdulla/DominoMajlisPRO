# 03 ARCHITECTURE CONSTITUTION

## Application Architecture
- .NET MAUI single-project application.
- Shell navigation via `AppShell`.
- C# + XAML.
- Services-based business logic.
- JSON storage in app data.
- GalleryEngine and Store Manager are project subsystems.

## Protected Subsystems
- `Services/ApplicationUserService.cs`: account/session/role authority.
- `Services/AppEvents.cs`: synchronization authority.
- `Services/TeamProfileService.cs`: Team profile lookup and storage.
- `GalleryEngine/Services/PlayerInventoryService.cs`: Player-owned Store inventory authority.
- `GalleryEngine/Services/TeamAssetInventoryService.cs`: Team-owned legacy asset authority.
- `GalleryEngine/Services/TeamEligibleAssetService.cs`: CreateTeamPage team asset eligibility gate.
- `GalleryEngine/Services/InventoryDisplayResolver.cs`: image/display resolver gateway.
- `GalleryEngine/Services/PlayerVisualIdentityResolver.cs`: Player visual identity resolver.
- `Pages/CreateTeamPage.xaml.cs`: team creation and team identity selection.

## Architecture Law
Extend existing services. Do not duplicate them. Do not bypass them from pages.

## Actual Project Snapshot
Services count: 38 source files.  
Pages/Admin/UI count: 87 source/XAML files.  
Models count: 56 model files.  
GalleryEngine files: 124 source/XAML files.
