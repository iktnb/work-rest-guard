# Tasks

Order: reminder/quiet-hour logic â†’ lifecycle/settings â†’ retire enforcement â†’ QA.

## work-rest-guard

- GuardEngine, TimerPolicy, Models and tests: AC-1, AC-3, AC-5.
- StatusTimerForm/Controller and SettingsForm/UiText: AC-2, AC-4.
- GuardApplicationContext, Program, UsageTrackingService and lifecycle harness:
  continuous collection, UI recovery, maintenance shutdown (AC-3, AC-4, AC-5).
- Remove obsolete enforcement implementation/tests; preserve legacy data readers.
- Update docs, test/publish, install/verify and push current feature branch (AC-6).

Implementation, review and QA complete; installation verified. See 06-implementation.md and 07-QA.md.
