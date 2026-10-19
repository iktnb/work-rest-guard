# Design

Baseline ddc97d5, clean source, 70 passing tests and formatting/build gates
from the identical source in usage-statistics/06-qa-report.md. Reuse that
baseline evidence. During development 0.2.0 runs with protection disabled so
statistics are not needlessly interrupted.

Replace the guard engine's enforcement paths with a work/reminder cycle.
No runtime route constructs a full-screen form or registers an emergency key.
Remove the obsolete blocker/challenge/hotkey components and their superseded
feature tests. Retain historical enum values and serialized fields so past
events and usage spans still deserialize. Existing config keys NightStart and
NightEnd now define only the compact timer's hidden interval; the retired
NightModeEnabled is normalized false. No persistent schema or dependency change.

Collection is always enabled in normal operation, independently of reminders
and monitor selection; safe mode remains a diagnostic exception. Old pause
flags are normalized true. Closing Settings does not end ApplicationContext.
Schedule initial Settings after the message loop starts. A named maintenance
event provides explicit --stop-for-update flush/exit without adding a tray exit.
Recoverable UI exceptions log and flush but do not stop collection. Fatal OS or
process failures cannot be prevented; autostart runs the normal app at sign-in.

Reuse Theme, StatusTimerForm/Controller, SettingsForm native controls and the
statistics UI. Only work and rest text changes in the compact timer. Quiet-hours
membership uses local TimeOnly comparisons with an inclusive start/exclusive
end, so DST transitions do not invent a lock or stop logging. Usage's existing
night flag records this time-of-day classification without reinterpreting old
records. Cache monitor discovery until display change or a bounded fallback.

Alternative: retain a disabled blocker option. Rejected because it leaves an
accidental reactivation path contrary to the request. A Windows watchdog/service
is unnecessary and outside scope.

No new store or data growth: existing UTC-day files and 90-day retention apply.
Migration has no lock/backfill. Backup the current binary/config/state/history,
verify the build, perform a short maintenance replacement, restore reminder
visibility enabled and collection enabled, and retain the same startup path.
Rollback requires restoring the matching old config/binary because an old
binary still implements its old blocking behavior.
