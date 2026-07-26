# Phase 5 Static Page and Control Audit

Generated: 2026-07-26
Pages inventoried: 48
Interactive controls inventoried from XAML: 451

## Static Findings
- XAML handler scan found no confirmed missing handlers after checking partial classes manually for the initially flagged CreateTeamPage and PlayerProfilesPage handlers.
- AutomationId coverage in XAML is weak: the static scan found no explicit AutomationId attributes in XAML.
- Remediation added AutomationIdService and wired it through NavigationGuardService so routed pages receive deterministic runtime AutomationId values for important interactive controls.
- RuntimeTested is intentionally marked No for this phase because emulator/physical-device execution has not been completed in this evidence pass.
- User-facing DisplayAlert/DisplayActionSheet usage remains and is scheduled for Phase 8 migration.

## Generated Files
- phase-5-page-matrix-static.csv
- phase-5-control-matrix-static.csv
