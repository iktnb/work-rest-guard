# Implementation tasks

Dependency order: usage model/tracker → daily store → report aggregation →
Windows sampling/runtime integration → localized statistics UI → verification.

## work-rest-guard

- UsageModels/UsageTracker and tests: observation semantics and gaps (AC-1, AC-2).
- UsageStore and tests: atomic per-day persistence and retention (AC-3).
- UsageReport and tests: local-day reporting, app ranking and timeline (AC-4).
- WindowsUsageSource, UsageTrackingService, GuardApplicationContext: collection,
  lock/power events and independent settings (AC-1, AC-2, AC-6).
- StatisticsControl, UsageText, SettingsForm, tray menu: view and controls (AC-5).
- OverlayHarness, README, SECURITY and full quality gates (AC-5, AC-6, AC-7).
