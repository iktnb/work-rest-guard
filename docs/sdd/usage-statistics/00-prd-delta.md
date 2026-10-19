# Daily routine and application statistics

Status: local implementation, pending release. Requested on 2026-10-19.

The user wants to understand their general daily routine and where time goes
across specific applications. Existing history records guard outcomes, not
computer usage. It cannot reconstruct past application time.

Add local foreground-application sampling, daily and period summaries, and a
timeline to Work Rest Guard. Keep the existing break engine and Obsidian Display
Guard independent. Collection must also work with guard protection disabled.

Count recent input as activity using a configurable idle threshold (default
5 minutes). This is an estimate of computer activity, never a productivity,
attention, rest, or sleep measurement. Only one foreground application gets
each active interval. Idle, locked, suspended, unknown and unrecorded time
must not be assigned to applications. An overlay on one monitor does not prove
that the user stopped using the computer.

Acceptance: AC-1 foreground attribution and idle; AC-2 gaps, clock changes,
lock/suspend and restart; AC-3 local storage, retention and corruption handling;
AC-4 correct ranges, daily totals and shares; AC-5 usable localized statistics
screen; AC-6 privacy controls and compatibility; AC-7 deployable local build.

Out of scope: window titles, URLs, browser tabs, document contents, cloud sync,
automatic productivity classification, changes to protection policy, historical
usage backfill, GitHub release publication, merge, and Obsidian vault modifications.

Delivery scope update, 2026-10-19: after reviewing the ready local build, the user
requested Git publication and launch. Local installation, a retained old binary
and data backup, and a source push to feature/usage-statistics are now authorized.
