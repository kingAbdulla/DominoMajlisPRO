# Domino Majlis PRO - Architecture Diagram

## Tech Stack
- **Framework**: .NET MAUI (net10.0)
- **Platforms**: Android, iOS, MacCatalyst, Windows
- **Language**: C#
- **Persistence**: JSON files (local storage)
- **UI**: XAML with Shell navigation
- **External Libraries**: PdfSharpCore, System.Text.Encoding.CodePages

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         MAUI APPLICATION                          │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    App.xaml / MauiProgram                   │  │
│  │  - Font registration                                       │  │
│  │  - Arabic text recovery service                            │  │
│  │  - Navigation page initialization                           │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              │                                    │
│                              ▼                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                      AppShell (Shell)                       │  │
│  │  - Navigation foundation                                   │  │
│  │  - Route definitions                                       │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              │                                    │
│                              ▼                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    MainPage (Hub)                           │  │
│  │  - Team selection & match setup                             │  │
│  │  - Navigation to all features                               │  │
│  │  - AppEvents subscription                                   │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Layer Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐   │
│  │   Pages/        │  │   Controls/    │  │ GalleryEngine/  │   │
│  │   - GamePage    │  │   - MatchTeam  │  │   Pages/        │   │
│  │   - Rankings    │  │     Card       │  │   - GalleryPage │   │
│  │   - History     │  │   - HallBottom │  │   - Admin Pages│   │
│  │   - PlayerProf  │  │     NavView     │  │                 │   │
│  │   - HallOfFame  │  │   - HallSide    │  │                 │   │
│  │   - Certificate │  │     MenuView    │  │                 │   │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         SERVICES LAYER                            │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              Core Business Services                         │  │
│  │  - ApplicationUserService (identity & sessions)            │  │
│  │  - PlayerProfileService (player CRUD)                       │  │
│  │  - TeamProfileService (team CRUD)                            │  │
│  │  - RankingService (rankings & XP)                            │  │
│  │  - GameService (match recording)                            │  │
│  │  - PlayerEngine (stats calculation)                         │  │
│  │  - PlayerTimelineService (events)                           │  │
│  │  - HonorIdentityService (honors)                             │  │
│  │  - DeveloperLockService (dev access)                       │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              GalleryEngine Services                          │  │
│  │  - PlayerInventoryService (player assets)                  │  │
│  │  - TeamAssetInventoryService (team assets)                  │  │
│  │  - StorePurchaseService (purchasing)                        │  │
│  │  - StoreEquipService (equipment)                            │  │
│  │  - PlayerWalletService (currency)                            │  │
│  │  - StoreAssetCatalogService (catalog)                       │  │
│  │  - InventoryDisplayResolver (UI resolution)                  │  │
│  │  - PlayerVisualIdentityResolver (avatar/bg)                │  │
│  │  - TeamIdentityResolver (team identity)                     │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                          MODELS LAYER                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐   │
│  │  Core Models    │  │ Gallery Models  │  │  Admin Models   │   │
│  │  - Player       │  │  - PlayerOwned  │  │  - AvatarRecord │   │
│  │  - Team         │  │    StoreItem    │  │  - Background   │   │
│  │  - MatchModel   │  │  - TeamOwned    │  │    Record       │   │
│  │  - SavedMatch   │  │    AssetItem    │  │  - LimitedOffer │   │
│  │  - ApplicationUser│  - PlayerWallet │  │    Record       │   │
│  │  - PlayerProfile│  │  - TeamIdentity │  │  - StorePricing │   │
│  │  - TeamProfile  │  │    Model        │  │    Config       │   │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      PERSISTENCE LAYER                           │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                   JSON File Storage                         │  │
│  │  - application_users.json (user accounts)                  │  │
│  │  - current_user_session.json (active session)              │  │
│  │  - players.json (player profiles)                          │  │
│  │  - rankings.json (team rankings)                           │  │
│  │  - matches.json (match history)                            │  │
│  │  - rivalries.json (team rivalries)                         │  │
│  │  - player_owned_assets.json (player inventory)             │  │
│  │  - team_owned_assets.json (team inventory)                 │  │
│  │  - store_purchases.json (purchase records)                 │  │
│  │  - store_catalog.json (published items)                     │  │
│  │  - developer_lock.json (dev access control)                │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Flow Diagram

```
┌──────────────┐
│   User Action│
└──────┬───────┘
       │
       ▼
┌──────────────────┐
│   Page (XAML)    │
│   - Button Click │
│   - Form Submit  │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Page Code-Behind │
│ - Event Handler  │
│ - Validation     │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│   Service Layer  │
│ - Business Logic │
│ - Data Transform │
│ - Validation     │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│  JSON Persistence│
│ - File I/O       │
│ - Serialization  │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│   AppEvents      │
│ - Raise Event    │
│ - Notify Subs    │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│  UI Refresh      │
│ - Data Binding   │
│ - Collection Update│
└──────────────────┘
```

---

## AppEvents Synchronization System

```
┌─────────────────────────────────────────────────────────────┐
│                    AppEvents (Static)                        │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Events:                                               │  │
│  │  - DataChanged (global data refresh)                   │  │
│  │  - RankingsChanged (rankings updated)                  │  │
│  │  - TeamsChanged (team data updated)                    │  │
│  │  - MatchesChanged (match history updated)               │  │
│  │  - PlayerProfileChanged (player data updated)          │  │
│  │  - CurrentUserChanged (account switched)               │  │
│  │  - StoreEconomyChanged (wallet/inventory updated)      │  │
│  │  - StoreProgressChanged (collection progress updated)  │  │
│  │  - TeamAssetsChanged (team inventory updated)          │  │
│  │  - TeamEffectChanged (team effects updated)            │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
           ┌──────────────────┼──────────────────┐
           │                  │                  │
           ▼                  ▼                  ▼
    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
    │  MainPage   │    │  Pages      │    │  Gallery    │
    │  Subscribes │    │  Subscribes │    │  Pages      │
    └─────────────┘    └─────────────┘    └─────────────┘
```

---

## Identity Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Identity Hierarchy                         │
│                                                              │
│  ApplicationUserId (Account/Session)                          │
│         │                                                     │
│         ├── PlayerId (Player Identity)                        │
│         │      │                                              │
│         │      ├── PlayerProfile (stats, avatar, history)     │
│         │      ├── PlayerInventory (owned assets)             │
│         │      └── PlayerWallet (coins, gems)                 │
│         │                                                     │
│         └── TeamId (Team Identity)                            │
│                │                                              │
│                ├── TeamProfile (XP, wins, members)             │
│                ├── TeamInventory (team assets)                │
│                └── TeamIdentity (emblem, colors, effects)   │
│                                                              │
│  AssetId (Store Asset Identity)                              │
│         │                                                     │
│         ├── Canonical Asset ID (catalog reference)           │
│         ├── ProductId (store product reference)              │
│         └── Ownership Record (player/team specific)           │
│                                                              │
│  Display-Only Fields (NOT identity keys):                     │
│  - DisplayName, PlayerName, TeamName, DeveloperName          │
└─────────────────────────────────────────────────────────────┘
```

---

## GalleryEngine Subsystem

```
┌─────────────────────────────────────────────────────────────┐
│                   GalleryEngine Architecture                  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Admin Layer (Developer/Content Management)            │  │
│  │  - AvatarsEditorPage                                   │  │
│  │  - BackgroundsEditorPage                               │  │
│  │  - StoreCategoriesEditorPage                           │  │
│  │  - LimitedOffersEditorPage                             │  │
│  │  - CurrentSeasonEditorPage                             │  │
│  │  - DeveloperStoreManagerPage                           │  │
│  │  - StoreCms* Services (catalog management)             │  │
│  └──────────────────────────────────────────────────────┘  │
│                          │                                    │
│                          ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Store Services (Business Logic)                     │  │
│  │  - StoreAssetCatalogService (catalog loading)        │  │
│  │  - StorePurchaseService (purchase flow)               │  │
│  │  - StoreCheckoutService (checkout logic)              │  │
│  │  - StoreEquipService (equipment logic)                │  │
│  │  - PlayerWalletService (currency management)          │  │
│  └──────────────────────────────────────────────────────┘  │
│                          │                                    │
│                          ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Inventory Services (Ownership & Equipment)           │  │
│  │  - PlayerInventoryService (player assets)              │  │
│  │  - TeamAssetInventoryService (team assets)             │  │
│  │  - InventoryDisplayResolver (UI resolution)             │  │
│  │  - InventoryRouter (routing logic)                     │  │
│  └──────────────────────────────────────────────────────┘  │
│                          │                                    │
│                          ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Identity Services (Visual Identity)                  │  │
│  │  - PlayerVisualIdentityResolver (player avatar/bg)    │  │
│  │  - TeamIdentityResolver (team emblem/colors)          │  │
│  │  - PlayerStoreIdentityService (store identity)         │  │
│  └──────────────────────────────────────────────────────┘  │
│                          │                                    │
│                          ▼                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  User-Facing Pages                                    │  │
│  │  - GalleryPage (store/gallery UI)                     │  │
│  │  - Components (PremiumCard, HeroBanner, etc.)         │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Key Service Relationships

```
ApplicationUserService
    │
    ├──→ PlayerProfileService (player CRUD)
    ├──→ TeamProfileService (team CRUD)
    ├──→ DeveloperLockService (dev access)
    └──→ HonorIdentityService (honors)

PlayerProfileService
    │
    ├──→ PlayerEngine (stats calculation)
    ├──→ PlayerTimelineService (event logging)
    └──→ PlayerRankService (ranking)

RankingService
    │
    ├──→ TeamProfileService (team data)
    ├──→ GameService (match data)
    └──→ HallOfLegendsConstitutionService (HoF logic)

GameService
    │
    └──→ SavedMatch (match persistence)

GalleryEngine Services
    │
    ├──→ StorePurchaseService
    │       ├──→ PlayerWalletService
    │       ├──→ PlayerInventoryService
    │       └──→ StoreAssetCatalogService
    │
    ├──→ StoreEquipService
    │       ├──→ PlayerInventoryService
    │       └──→ TeamAssetInventoryService
    │
    ├──→ InventoryDisplayResolver
    │       ├──→ StoreAssetCatalogService
    │       ├──→ PlayerAssetInventoryService
    │       ├──→ TeamAssetInventoryService
    │       └──→ ApplicationUserService (session)
    │
    └──→ PlayerVisualIdentityResolver
            ├──→ PlayerInventoryService
            └──→ InventoryDisplayResolver
```

---

## Page Navigation Flow

```
MainPage (Hub)
    │
    ├──→ GamePage (match recording)
    │
    ├──→ RankingsPage (leaderboards)
    │       └──→ PlayerDetailsPage
    │
    ├──→ PlayerProfilesPage (player management)
    │       └──→ PlayerDetailsPage
    │
    ├──→ HistoryPage (match history)
    │       └──→ MatchDetailsPage
    │
    ├──→ HallOfFamePage (legends)
    │
    ├──→ GalleryPage (store/gallery)
    │       └──→ GalleryEngine Admin Pages (if dev)
    │
    ├──→ CreateTeamPage (team creation)
    │
    ├──→ CertificatePage (certificates)
    │       └──→ CertificatePrintPage
    │
    ├──→ StatisticsPage (stats)
    │
    ├──→ RulesPage (game rules)
    │
    ├──→ HonorsAdminPage (honors management - dev)
    │
    └──→ DeveloperLoginPage (dev access)
```

---

## File Organization Summary

```
DominoMajlisPRO/
├── App.xaml / App.xaml.cs (application entry)
├── AppShell.xaml / AppShell.xaml.cs (navigation shell)
├── MainPage.xaml / MainPage.xaml.cs (main hub)
├── MauiProgram.cs (MAUI initialization)
│
├── Services/ (40+ service files)
│   ├── AppEvents.cs (synchronization)
│   ├── ApplicationUserService.cs (identity)
│   ├── PlayerProfileService.cs (players)
│   ├── TeamProfileService.cs (teams)
│   ├── RankingService.cs (rankings)
│   ├── GameService.cs (matches)
│   └── ... (other services)
│
├── Pages/ (20+ page files)
│   ├── GamePage.xaml/cs
│   ├── RankingsPage.xaml/cs
│   ├── PlayerProfilesPage.xaml/cs
│   ├── HistoryPage.xaml/cs
│   ├── HallOfFamePage.xaml/cs
│   └── ... (other pages)
│
├── Models/ (26 model files)
│   ├── Player.cs
│   ├── Team.cs
│   ├── MatchModel.cs
│   ├── PlayerProfileModel.cs
│   ├── TeamProfileModel.cs
│   └── ... (other models)
│
├── GalleryEngine/
│   ├── Services/ (22 gallery services)
│   ├── Models/ (15 gallery models)
│   ├── Pages/ (gallery pages)
│   ├── Admin/ (60+ admin files)
│   │   ├── Services/ (admin services)
│   │   ├── Models/ (admin models)
│   │   └── Core/ (CMS core)
│   └── Components/ (UI components)
│
├── Controls/ (custom controls)
├── Resources/ (images, fonts, etc.)
├── Platforms/ (platform-specific code)
└── Storage/ (storage utilities)
```

---

## Key Design Patterns

1. **Service Layer Pattern**: All business logic in static service classes
2. **Repository Pattern**: Services act as repositories for JSON files
3. **Event-Driven Architecture**: AppEvents for loose coupling
4. **MVVM-Lite**: Pages as views, Services as viewmodels (no explicit VM layer)
5. **Identity-First Design**: IDs as primary keys, names as display-only
6. **Subsystem Isolation**: GalleryEngine as isolated store/gallery subsystem
7. **Shell Navigation**: MAUI Shell for navigation foundation
8. **JSON Persistence**: Local file-based storage with serialization

---

## Data Integrity Safeguards

1. **ID-First Lookups**: All queries use IDs before falling back to names
2. **SemaphoreSlim Gates**: Thread-safe file operations
3. **Event-Based Sync**: AppEvents ensure UI consistency
4. **Normalization**: PlayerEngine normalizes data before save
5. **Validation**: Services validate identity keys before operations
6. **Session Management**: ApplicationUserService tracks active session
7. **Developer Isolation**: Developer inventory separate from normal accounts
