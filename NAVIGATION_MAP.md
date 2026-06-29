# Domino Majlis PRO - Navigation Map

## Overview [VERIFIED FROM SOURCE CODE - code analysis of Pages folder]

This document documents every page, navigation flow, shell routes, dialogs, and bottom sheets in the application. [VERIFIED FROM SOURCE CODE - code analysis of Pages folder]

---

## Navigation Architecture

### Shell-Based Navigation

The application uses MAUI Shell as the navigation foundation. All navigation flows through Shell routes.

**Entry Point**: `MainPage` (wrapped in NavigationPage)

**Shell**: `AppShell` (currently minimal, routes defined in code-behind)

---

## MainPage (Hub)

**File**: `MainPage.xaml` / `MainPage.xaml.cs`

**Purpose**: Central hub for all application features

**Navigation From**: App entry (NavigationPage)

**Navigation To**:
- GamePage (match recording)
- RankingsPage (leaderboards)
- PlayerProfilesPage (player management)
- HistoryPage (match history)
- HallOfFamePage (hall of legends)
- GalleryPage (store/gallery)
- CreateTeamPage (team creation)
- CertificatePage (certificates)
- StatisticsPage (statistics)
- RulesPage (game rules)
- DeveloperLoginPage (developer access)
- HonorsAdminPage (honors management)
- Settings (inline sections)

**AppEvents Subscriptions**:
- DataChanged
- RankingsChanged
- TeamsChanged
- MatchesChanged
- PlayerProfileChanged
- CurrentUserChanged
- TeamAssetsChanged
- StoreEconomyChanged

**Special Features**:
- Team selection carousel
- Match setup
- Settings sections (expandable)
- Secret honors access (long press logo)
- Season card display

---

## Player Pages

### PlayerProfilesPage

**File**: `Pages/PlayerProfilesPage.xaml` / `Pages/PlayerProfilesPage.xaml.cs`

**Purpose**: Player list and management

**Navigation From**: MainPage

**Navigation To**:
- PlayerDetailsPage (on player selection)

**AppEvents Subscriptions**:
- PlayerProfileChanged
- StoreEconomyChanged
- StoreProgressChanged

**Features**:
- Player list with avatars
- Collection counts
- Inventory payload display
- Add player functionality
- Player statistics summary

---

### PlayerDetailsPage

**File**: `Pages/PlayerDetailsPage.xaml` / `Pages/PlayerDetailsPage.xaml.cs`

**Purpose**: Individual player details and management

**Navigation From**: PlayerProfilesPage

**Navigation To**: None (detail page)

**AppEvents Subscriptions**:
- PlayerProfileChanged
- StoreEconomyChanged
- StoreProgressChanged

**Features**:
- Player profile display
- Avatar management
- Timeline events
- Statistics
- Achievements
- Honor display
- Trust ring visualization

---

## Team Pages

### CreateTeamPage

**File**: `Pages/CreateTeamPage.xaml` / `Pages/CreateTeamPage.xaml.cs`

**Purpose**: Team creation and editing

**Navigation From**: MainPage

**Navigation To**: None (returns to caller)

**AppEvents Subscriptions**:
- TeamsChanged
- TeamAssetsChanged
- StoreEconomyChanged

**Features**:
- Team name input
- Player selection (Player1, Player2)
- Emblem selection (carousel)
- Team color selection (carousel)
- Emblem background selection
- Team effects
- Single player mode toggle

**Special Notes**:
- Uses TeamEligibleAssetService for asset filtering
- RecyclerView safety critical (carousel mutations during layout)

---

### RankingsPage

**File**: `Pages/RankingsPage.xaml` / `Pages/RankingsPage.xaml.cs`

**Purpose**: Team rankings and leaderboards

**Navigation From**: MainPage

**Navigation To**: None (list page)

**AppEvents Subscriptions**:
- RankingsChanged
- TeamsChanged
- PlayerProfileChanged

**Features**:
- Team rankings list
- XP display
- Win rate display
- Rank badges
- Filter options

---

### HallOfFamePage

**File**: `Pages/HallOfFamePage.xaml` / `Pages/HallOfFamePage.xaml.cs`

**Purpose**: Hall of Fame / Hall of Legends display

**Navigation From**: MainPage

**Navigation To**: None (list page)

**AppEvents Subscriptions**:
- RankingsChanged
- TeamsChanged
- PlayerProfileChanged

**Features**:
- Hall of Fame members
- Hall of Legends eligibility
- Team legend results
- Constitution display
- Trust score requirements

---

## Match Pages

### GamePage

**File**: `Pages/GamePage.xaml` / `Pages/GamePage.xaml.cs`

**Purpose**: Match recording and gameplay

**Navigation From**: MainPage

**Navigation To**: None (game page)

**AppEvents Subscriptions**:
- MatchesChanged
- TeamsChanged
- PlayerProfileChanged

**Features**:
- Team selection
- Round recording
- Score tracking
- Match completion
- Rules display
- Effects refresh

---

### HistoryPage

**File**: `Pages/HistoryPage.xaml` / `Pages/HistoryPage.xaml.cs`

**Purpose**: Match history list

**Navigation From**: MainPage

**Navigation To**:
- MatchDetailsPage (on match selection)

**AppEvents Subscriptions**:
- MatchesChanged
- TeamsChanged
- PlayerProfileChanged

**Features**:
- Match history list
- Date filtering
- Team display
- Match results

**Special Notes**:
- RecyclerView safety critical (ItemsSource mutations)

---

### MatchDetailsPage

**File**: `Pages/MatchDetailsPage.xaml` / `Pages/MatchDetailsPage.xaml.cs`

**Purpose**: Individual match details

**Navigation From**: HistoryPage

**Navigation To**: None (detail page)

**AppEvents Subscriptions**:
- MatchesChanged
- TeamsChanged
- PlayerProfileChanged

**Features**:
- Match details display
- Round-by-round results
- Player/team information
- Match statistics

---

## Gallery Pages

### GalleryPage

**File**: `GalleryEngine/Pages/GalleryPage.xaml` / `GalleryEngine/Pages/GalleryPage.xaml.cs`

**Purpose**: Store/gallery for purchasing assets

**Navigation From**: MainPage

**Navigation To**:
- GalleryEngine Admin Pages (if developer)

**AppEvents Subscriptions**: None (uses component subscriptions)

**Features**:
- Hero slider
- New arrivals section
- Limited offers section
- Avatars section
- Backgrounds section
- Categories section
- Wallet display
- Navigation between sections

**Components Used**:
- HeroSliderView
- NewArrivalsSectionView
- LimitedOffersSectionView
- AvatarsSectionView
- BackgroundsSectionView
- CategoriesSectionView
- PremiumStoreHeaderView
- StoreBottomNavigationView

---

## GalleryEngine Admin Pages

### AvatarsEditorPage

**File**: `GalleryEngine/Admin/AvatarsEditorPage.xaml` / `GalleryEngine/Admin/AvatarsEditorPage.xaml.cs`

**Purpose**: Avatar content management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (editor page)

**AppEvents Subscriptions**: None

**Features**:
- Avatar creation/editing
- Image upload
- Pricing configuration
- Currency selection
- Rarity setting
- Publishing workflow

---

### BackgroundsEditorPage

**File**: `GalleryEngine/Admin/BackgroundsEditorPage.xaml` / `GalleryEngine/Admin/BackgroundsEditorPage.xaml.cs`

**Purpose**: Background content management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (editor page)

**AppEvents Subscriptions**: None

**Features**:
- Background creation/editing
- Image upload
- Pricing configuration
- Currency selection Rarity setting
- Publishing workflow

---

### NewArrivalsEditorPage

**File**: `GalleryEngine/Admin/NewArrivalsEditorPage.xaml` / `GalleryEngine/Admin/NewArrivalsEditorPage.xaml.cs`

**Purpose**: New arrivals management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (editor page)

**AppEvents Subscriptions**: None

**Features**:
- New arrivals configuration
- Product linking
- Display order
- Publishing workflow

---

### LimitedOffersEditorPage

**File**: `GalleryEngine/Admin/LimitedOffersEditorPage.xaml` / `GalleryEngine/Admin/LimitedOffersEditorPage.xaml.cs`

**Purpose**: Limited offers management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (editor page)

**AppEvents Subscriptions**: None

**Features**:
- Limited offers configuration
- Time-based availability
- Pricing configuration
- Publishing workflow

---

### CurrentSeasonEditorPage

**File**: `GalleryEngine/Admin/CurrentSeasonEditorPage.xaml` / `GalleryEngine/Admin/CurrentSeasonEditorPage.xaml.cs`

**Purpose**: Season management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (editor page)

**AppEvents Subscriptions**: None

**Features**:
- Season configuration
- Hero selection
- Season metadata
- Publishing workflow

---

### StoreCategoriesEditorPage

**File**: `GalleryEngine/Admin/StoreCategoriesEditorPage.xaml` / `GalleryEngine/Admin/StoreCategoriesEditorPage.xaml.cs`

**Purpose**: Store categories management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (editor page)

**AppEvents Subscriptions**: None

**Features**:
- Category creation/editing
- Display order
- Icon configuration

---

### DeveloperStoreManagerPage

**File**: `GalleryEngine/Admin/DeveloperStoreManagerPage.xaml` / `GalleryEngine/Admin/DeveloperStoreManagerPage.xaml.cs`

**Purpose**: Developer store management hub (developer only)

**Navigation From**: MainPage (if developer)

**Navigation To**:
- AvatarsEditorPage
- BackgroundsEditorPage
- NewArrivalsEditorPage
- LimitedOffersEditorPage
- CurrentSeasonEditorPage
- StoreCategoriesEditorPage
- CurrencyPricingManagerPage
- SpecializedStoreManagerPage (emblems, effects, etc.)
- StoreSettingsManagerPage
- InventoryAuditPage

**AppEvents Subscriptions**: None

**Features**:
- Admin section cards
- Navigation to all admin pages
- Store statistics
- Configuration access

---

### CurrencyPricingManagerPage

**File**: `GalleryEngine/Admin/CurrencyPricingManagerPage.cs`

**Purpose**: Currency pricing management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (manager page)

**AppEvents Subscriptions**: None

**Features**:
- Currency pricing configuration
- Exchange rates
- Pricing rules

---

### SpecializedStoreManagerPage

**File**: `GalleryEngine/Admin/SpecializedStoreManagerPage.cs`

**Purpose**: Specialized asset management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (manager page)

**AppEvents Subscriptions**: None

**Features**:
- Emblems management
- Effects management
- Emblem backgrounds management
- Frames management
- Titles management
- Bundles management
- Team colors management

---

### StoreSettingsManagerPage

**File**: `GalleryEngine/Admin/StoreSettingsManagerPage.cs`

**Purpose**: Store settings management (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (manager page)

**AppEvents Subscriptions**: None

**Features**:
- Runtime configuration
- Store settings
- Feature flags

---

### InventoryAuditPage

**File**: `GalleryEngine/Admin/InventoryAuditPage.cs`

**Purpose**: Inventory audit and health check (developer only)

**Navigation From**: DeveloperStoreManagerPage

**Navigation To**: None (audit page)

**AppEvents Subscriptions**:
- StoreEconomyChanged

**Features**:
- Inventory health summary
- Catalog health check
- Missing assets detection
- Orphaned assets detection

---

## Utility Pages

### CertificatePage

**File**: `Pages/CertificatePage.xaml` / `Pages/CertificatePage.xaml.cs`

**Purpose**: Certificate generation and display

**Navigation From**: MainPage

**Navigation To**:
- CertificatePrintPage (print action)

**AppEvents Subscriptions**: None

**Features**:
- Certificate preview
- Team selection
- Customization options
- Print/share functionality

---

### CertificatePrintPage

**File**: `Pages/CertificatePrintPage.xaml` / `Pages/CertificatePrintPage.xaml.cs`

**Purpose**: Certificate printing

**Navigation From**: CertificatePage

**Navigation To**: None (print page)

**AppEvents Subscriptions**: None

**Features**:
- Print preview
- PDF generation
- Print options

---

### StatisticsPage

**File**: `Pages/StatisticsPage.xaml` / `Pages/StatisticsPage.xaml.cs`

**Purpose**: Application statistics

**Navigation From**: MainPage

**Navigation To**: None (statistics page)

**AppEvents Subscriptions**: None

**Features**:
- Total matches
- Total players
- Total teams
- Win rates
- Activity statistics

---

### RulesPage

**File**: `Pages/RulesPage.xaml` / `Pages/RulesPage.xaml.cs`

**Purpose**: Game rules display

**Navigation From**: MainPage

**Navigation To**: None (rules page)

**AppEvents Subscriptions**: None

**Features**:
- Game rules text
- Scoring rules
- Competition rules

---

## Developer Pages

### DeveloperLoginPage

**File**: `Pages/DeveloperLoginPage.xaml` / `Pages/DeveloperLoginPage.xaml.cs`

**Purpose**: Developer authentication and access

**Navigation From**: MainPage (settings section)

**Navigation To**:
- DeveloperStoreManagerPage (on successful login)

**AppEvents Subscriptions**: None

**Features**:
- Developer code entry
- Authentication
- Developer role activation
- Security logging

---

### HonorsAdminPage

**File**: `Pages/HonorsAdminPage.xaml` / `Pages/HonorsAdminPage.xaml.cs`

**Purpose**: Honors management (developer only)

**Navigation From**: MainPage (if developer)

**Navigation To**: None (admin page)

**AppEvents Subscriptions**: None

**Features**:
- Honor key generation
- Honor assignment
- Honor revocation
- Honor display configuration

---

## Dialogs and Bottom Sheets

### Current Implementation

The application currently uses inline dialogs and modal pages rather than dedicated dialog/bottom sheet components. Future enhancements may include:

- Action sheets for asset purchase confirmation
- Bottom sheets for asset preview
- Dialogs for settings
- Modal dialogs for confirmations

### StoreProductActionSheet

**File**: `GalleryEngine/Components/StoreSections/StoreProductActionSheet.cs`

**Purpose**: Asset purchase/equip action sheet

**Usage**: GalleryPage components

**Features**:
- Purchase option
- Equip option
- Preview option
- Cancel option

---

## Navigation Flow Diagrams

### Main Flow

```
App Entry
    ↓
MainPage (Hub)
    ├─→ GamePage
    ├─→ RankingsPage
    ├─→ PlayerProfilesPage → PlayerDetailsPage
    ├─→ HistoryPage → MatchDetailsPage
    ├─→ HallOfFamePage
    ├─→ GalleryPage
    ├─→ CreateTeamPage
    ├─→ CertificatePage → CertificatePrintPage
    ├─→ StatisticsPage
    ├─→ RulesPage
    ├─→ DeveloperLoginPage → DeveloperStoreManagerPage
    └─→ HonorsAdminPage
```

### Developer Flow

```
DeveloperLoginPage
    ↓
DeveloperStoreManagerPage
    ├─→ AvatarsEditorPage
    ├─→ BackgroundsEditorPage
    ├─→ NewArrivalsEditorPage
    ├─→ LimitedOffersEditorPage
    ├─→ CurrentSeasonEditorPage
    ├─→ StoreCategoriesEditorPage
    ├─→ CurrencyPricingManagerPage
    ├─→ SpecializedStoreManagerPage
    ├─→ StoreSettingsManagerPage
    └─→ InventoryAuditPage
```

### Gallery Flow

```
GalleryPage
    ├─→ Hero Slider (carousel)
    ├─→ New Arrivals Section
    ├─→ Limited Offers Section
    ├─→ Avatars Section
    ├─→ Backgrounds Section
    ├─→ Categories Section
    └─→ StoreProductActionSheet (on item tap)
```

---

## Shell Routes

### Current Implementation

The application currently uses code-based navigation rather than declarative Shell routes. Navigation is performed using:

```csharp
await Shell.Current.GoToAsync(nameof(TargetPage));
```

### Route Parameters

Pages that accept parameters:
- **PlayerDetailsPage**: PlayerId
- **MatchDetailsPage**: MatchId
- **CertificatePage**: TeamId
- **GalleryPage**: (no parameters, uses current session)

### Future Shell Route Enhancements

Consider adding declarative routes in AppShell.xaml for:
- Better deep linking support
- Clearer navigation structure
- Route parameter validation

---

## Navigation Safety Rules [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]

### RecyclerView Safety [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]

For pages with CollectionView/CarouselView:
- Build new lists off main thread [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]
- Assign ItemsSource = null before replacement [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]
- Assign new list on main thread [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]
- Suppress selection handlers during reload [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]
- Never mutate bound collections from background threads [VERIFIED FROM SOURCE CODE - docs/04_LAYOUT_PROTECTION.md]

**Affected Pages**: [VERIFIED FROM SOURCE CODE - code analysis of page XAML]
- CreateTeamPage (carousels)
- HistoryPage (match list)
- PlayerProfilesPage (player list)
- RankingsPage (rankings list)
- HallOfFamePage (legends list)

### Back Navigation [INFERRED - from MAUI navigation patterns]

All pages must:
- Preserve back stack [INFERRED - from Shell navigation pattern]
- Handle back button press [INFERRED - from MAUI lifecycle]
- Save unsaved changes before navigation [INFERRED - from general UX pattern]
- Confirm destructive actions [INFERRED - from general UX pattern]

### Modal Pages [INFERRED - from MAUI navigation patterns]

Modal pages (dialogs, sheets) must:
- Use `Navigation.PushModalAsync` [INFERRED - from MAUI navigation API]
- Provide clear close/dismiss options [INFERRED - from general UX pattern]
- Return results to caller [INFERRED - from general navigation pattern]
- Handle cancellation gracefully [INFERRED - from general UX pattern]

---

## Navigation Performance [INFERRED - from general performance considerations]

### Lazy Loading [INFERRED - from general performance patterns]

Heavy pages should implement lazy loading:
- GalleryPage (section-by-section loading) [INFERRED - from GalleryPage structure]
- PlayerProfilesPage (pagination) [UNKNOWN - pagination not observed]
- HistoryPage (date filtering) [VERIFIED FROM SOURCE CODE - HistoryPage.xaml.cs]
- RankingsPage (lazy loading) [UNKNOWN - lazy loading not observed]

### Caching [INFERRED - from general performance patterns]

Consider caching for:
- Catalog data (StoreAssetCatalogService) [INFERRED - from service pattern]
- Player identities (PlayerVisualIdentityResolver) [INFERRED - from resolver pattern]
- Team identities (TeamIdentityResolver) [INFERRED - from resolver pattern]

### Preloading [INFERRED - from general performance patterns]

Preload critical data:
- App startup: ApplicationUserService [VERIFIED FROM SOURCE CODE - MainPage.xaml.cs]
- MainPage load: Team profiles, player profiles [VERIFIED FROM SOURCE CODE - MainPage.xaml.cs]
- GalleryPage load: Catalog data [INFERRED - from GalleryPage pattern]

---

## Navigation Accessibility [INFERRED - from general accessibility requirements]

### RTL Support [VERIFIED FROM SOURCE CODE - docs/01_PROJECT_MISSION.md]

All pages must support RTL (Right-to-Left) layout:
- Arabic text direction [VERIFIED FROM SOURCE CODE - docs/01_PROJECT_MISSION.md]
- Layout mirroring [INFERRED - from RTL requirements]
- Icon positioning [INFERRED - from RTL requirements]

### Keyboard Navigation [UNKNOWN - not observed in current implementation]

Consider adding keyboard navigation support for:
- Desktop platforms
- Accessibility compliance
- Power users

### Screen Reader Support [UNKNOWN - not observed in current implementation]

Ensure proper accessibility labels:
- Button descriptions
- Image descriptions
- List item announcements

---

## Summary [VERIFIED FROM SOURCE CODE - code analysis]

- **Total Pages**: 20+ pages [VERIFIED FROM SOURCE CODE - count of pages]
- **Admin Pages**: 10+ developer-only pages [VERIFIED FROM SOURCE CODE - count of admin pages]
- **Navigation Type**: Shell-based with code navigation [VERIFIED FROM SOURCE CODE - MainPage.xaml.cs]
- **Modal Pages**: CertificatePrintPage, DeveloperLoginPage [VERIFIED FROM SOURCE CODE - code analysis]
- **Dialogs**: StoreProductActionSheet (component) [VERIFIED FROM SOURCE CODE - StoreProductActionSheet.cs]
- **RecyclerView Critical Pages**: CreateTeamPage, HistoryPage, PlayerProfilesPage, RankingsPage, HallOfFamePage [VERIFIED FROM SOURCE CODE - page XAML analysis]
- **AppEvents Subscribers**: 9+ pages [VERIFIED FROM SOURCE CODE - page code-behind]
- **RTL Support**: Required for all pages [VERIFIED FROM SOURCE CODE - docs/01_PROJECT_MISSION.md]
