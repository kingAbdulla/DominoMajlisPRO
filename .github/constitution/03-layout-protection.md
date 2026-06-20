# Domino Majlis PRO Constitution
## Article 03 — Layout Protection Constitution

---

# Purpose

This article protects the approved user interface.

Visual redesign is forbidden unless explicitly approved.

---

# General Rule

If a task can be completed without modifying XAML layout,
the layout must remain unchanged.

Business logic should be preferred over UI reconstruction.

---

# Protected Elements

The following elements are protected:

• Grid structure

• RowDefinitions

• ColumnDefinitions

• StackLayouts

• VerticalStackLayouts

• HorizontalStackLayouts

• Borders

• Frames

• ContentView hierarchy

• ScrollView hierarchy

• CollectionView hierarchy

• CarouselView hierarchy

• Shell navigation

• Existing page hierarchy

---

# Forbidden Actions

Do NOT:

Rebuild pages.

Replace layouts.

Move controls.

Delete controls.

Merge unrelated controls.

Split existing layouts.

Change navigation flow.

Replace ContentViews.

Change spacing for aesthetic reasons.

Change margins.

Change padding.

Change alignment.

Reorder sections.

Reorganize page hierarchy.

Replace existing responsive layout.

---

# Allowed Actions

Allowed:

Fix bindings.

Fix commands.

Fix rendering bugs.

Fix visibility.

Fix converters.

Fix resources.

Fix styles.

Fix animations.

Fix behaviors.

Fix data templates.

Fix performance.

Integrate services.

Integrate AppEvents.

Integrate identity synchronization.

Integrate Store logic.

Integrate Gallery logic.

---

# Existing Approved UI

The following UI is considered approved:

Player Pages

Team Pages

Gallery

Store

Rankings

History

Match Details

Settings

Developer Tools

Store Manager

Publishing Pages

Hall Of Fame

---

# Visual Identity

Icons may be updated.

Colors may be updated.

Images may be updated.

Assets may be updated.

Existing placeholders may receive new content.

These changes must not alter layout structure.

---

# Responsive Design

Do not break responsiveness.

Maintain support for:

Android

Windows

MacCatalyst

iOS

---

# When Layout Changes Are Allowed

Layout changes require explicit user approval.

Never assume redesign is acceptable.

---

# Engineering Principle

Stable UI is more valuable than unnecessary redesign.

Business improvements must integrate into the approved interface whenever possible.

---

End of Article 03.