# Usage statistics

## Evidence and design

Repository: github.com/iktnb/work-rest-guard, baseline ebe644e, clean tree.
Baseline: .NET 10.0.302, Release build and all 37 tests pass; formatter passes.
WinForms, existing Theme colors, Theme.StyleButton, TabControl, DataGridView,
SettingsForm and OverlayHarness provide the UI patterns. No new packages.

Sample every five seconds on the existing UI timer, independently of GuardEnabled.
Read GetLastInputInfo and GetForegroundWindow/GetWindowThreadProcessId; resolve
only ProcessName. Never request window titles, process arguments or file paths.
Missing input or foreground information is unknown, not assumed activity.

A pure tracker attributes each interval to its previous observation, merges
adjacent equivalent spans, and refuses unobserved gaps over 15 seconds or clock
changes inconsistent with monotonic elapsed time. Sampling error for ordinary
app changes is up to one sampling interval. Suspend/resume signals can establish
a suspended interval. Lock/disconnect signals establish locked intervals while
the process remains alive. A crash or exit never creates future usage.

Store separate versioned UTC-day JSON files under LocalAppData/WorkRestGuard/usage.
Split at UTC midnight; aggregate later in the current Windows timezone using
real local-day UTC boundaries, including DST. Flush at most every 30 seconds
during normal collection, and on pause/suspend/exit or explicit refresh. Keep
90 UTC calendar days. Strictly read existing files; never overwrite corrupted
or unsupported-schema data. Failures appear in statistics and do not stop guard
protection. Existing config/state/history remain compatible; additive config
properties control tracking and idle threshold. No migration or backfill.

The store is authoritative. Reports derive from spans, with no second persistent
summary store. Nominal crash tail loss: about 30 seconds; delayed UI processing
or disk failures can extend it. Reads are range-scoped.
Estimated storage at 500 spans/day and roughly 200 bytes/span: 3 MB at one month,
9 MB after retention at six/twelve months. A pathological app switch every five
seconds for eight hours is about 35 MB/month, capped around 105 MB by retention.
These are estimates, not measurements.

## Reporting and UI

Statistics tab: selected end date, day/7-day/30-day period, explicit refresh,
collection state, coverage and units. Show active, idle, locked, suspended,
unrecorded/unknown durations and night-window activity. App shares use only
active time as denominator, including unattributed active time. Daily rows show
first/last sampled activity and longest continuous activity estimate. A timeline
for the selected date includes explicit unrecorded gaps. Do not equate idle
with rest or night blocking with sleep. Do not fill historical days with zero
activity when they contain no observations.

Use native keyboard behavior, accessible labels, numeric sorting, right-aligned
durations, readable empty/error states and scrollable narrow layouts. Reuse
Theme tokens; a local StatisticsControl composes existing controls rather than
introducing a global UI framework.

## Alternatives, rollout and rollback

Browser extensions/OS-wide event log analysis add permissions and scope; defer.
A single growing JSON file requires rewriting all history; daily files bound
write and recovery cost. SQLite would add a new dependency for this first version.

Build and verify locally before installation or Git publication. The user's
subsequent 2026-10-19 instruction authorizes installation/launch and source push
to feature/usage-statistics; a tag, GitHub release and main merge remain outside scope.
Rollback uses the existing binary, which ignores additive config fields and
the new usage directory. Preserve usage files on rollback. Privacy pause is
independent from protection and requires no typed challenge.

References: Microsoft Learn documentation for GetLastInputInfo,
GetForegroundWindow and GetWindowThreadProcessId (verified 2026-10-19).
