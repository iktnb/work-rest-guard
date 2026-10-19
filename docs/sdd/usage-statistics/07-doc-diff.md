# Documentation and delivery

Status: implemented and technically verified; local installation and launch
were subsequently authorized by the user on 2026-10-19. Source publication is
scoped to feature/usage-statistics. A PR, merge, tag and GitHub release are outside
the requested delivery scope.

README documents the Statistics tab/tray action and --statistics flag, activity
definition, configurable idle threshold, day/7/30-day ranges, active-only app
shares, local timezone/DST, pause, safe mode, 90-day retention, missing data,
privacy, crash-tail behavior, error handling and rollback.

SECURITY documents the expanded local-data scope, process-name-only observation,
recording pause and strict/atomic daily storage. Existing guard event history
remains separate. No Obsidian vault or Obsidian Display Guard source was changed.

The ready-to-run single-file executable is in artifacts/win-x64; launching the
new build starts recording with the existing local configuration unless paused
or launched with --safe. Collection cannot backfill earlier application time.
An old running instance must exit before the new one can run, because the
existing single-instance mutex remains in place.

The verified 0.2.0 binary was installed in the stable per-user Apps directory,
with the old 0.1.2 executable and configuration/state/history retained in a dated
backup. The enabled Windows startup command now targets this installation
instead of an old release build folder. Existing protection settings and data
were preserved; collection starts on launch. Runtime evidence and backup paths
remain local and are not part of the source repository.
