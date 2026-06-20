# Domino Majlis PRO Constitution
## Article 02 — Architecture Constitution

---

# Architecture Status

The current architecture is officially approved.

The AI must preserve this architecture unless explicitly instructed otherwise.

---

# Application Type

Single Project .NET MAUI Application.

---

# Navigation

Navigation is based on AppShell.

Do not replace Shell Navigation.

Do not introduce alternative navigation frameworks.

Do not break existing routing.

---

# Design Pattern

MVVM is mandatory.

Views shall remain presentation only.

Business logic belongs inside Services.

ViewModels coordinate between UI and Services.

Models represent application data only.

---

# Folder Structure

Preserve the existing folder structure.

Never reorganize folders without explicit approval.

Never move files simply for preference.

---

# Dependency Injection

Use existing dependency injection.

Register new services only when necessary.

Avoid introducing duplicate services.

---

# Services

Prefer extending existing services over creating new ones.

Avoid duplicate business logic.

Never bypass existing services.

---

# Models

IDs are authoritative.

Display names are never authoritative.

Relationships must use IDs.

Never bind business logic using display names.

---

# Identity

Developer identity must never become Guest.

Founder identity is limited.

Honor identity is honorary.

Identity synchronization must remain consistent across all pages.

---

# AppEvents

AppEvents are the official synchronization mechanism.

After modifying:

- Players

- Teams

- Rankings

- Gallery

- Store

- Identity

- Match History

Ensure AppEvents are triggered correctly.

---

# JSON Data

JSON remains the official storage mechanism.

Never silently overwrite files.

Never delete user data without explicit instruction.

---

# Performance

Avoid unnecessary allocations.

Reuse existing services.

Avoid duplicate queries.

Avoid unnecessary page reloads.

---

# File Modification Policy

Modify the smallest safe number of files.

Avoid large refactoring.

Prefer targeted fixes.

---

# Forbidden Actions

Do NOT:

Replace architecture.

Rewrite working systems.

Duplicate existing services.

Introduce parallel identity systems.

Replace AppShell.

Replace MVVM.

Replace JSON storage.

Rename stable models without reason.

---

# Engineering Principle

Architecture stability has higher priority than implementation convenience.

Every modification must preserve long-term maintainability.

---

End of Article 02.