# 04 LAYOUT PROTECTION POLICY

## Absolute Policy
Do not redesign approved UI unless the user explicitly says UI redesign is required.

## Protected Elements
- XAML hierarchy.
- Grid rows and columns.
- CollectionView/CarouselView placement.
- Borders, frames, page cards, premium black/gold style.
- Shell navigation.
- Page structure and section order.

## Allowed Without Redesign
- Binding fixes.
- Command wiring.
- Service integration.
- Data filtering logic.
- AppEvents refresh wiring.
- Image resolver fixes.
- RecyclerView-safe ItemsSource replacement, only when needed to prevent crashes.

## RecyclerView Safety Rule
For CollectionView/CarouselView on Android, do not mutate bound collections item-by-item during layout refresh. Prefer constructing a new list off-thread and assigning a new ItemsSource on the MainThread. Suppress selection events while replacing lists.
