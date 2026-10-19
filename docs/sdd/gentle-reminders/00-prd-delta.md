# Non-blocking reminders and continuous statistics

Requested 2026-10-19: remove screen blocking and emergency shutdown; keep a
small rest reminder, hide its timer at night, and keep statistics running.
The user explicitly confirmed statistics without screen blocking.

Use the existing 90/15-minute guide. Rest is a reminder in the compact timer,
never a full-monitor overlay. The former 20:00–06:00 interval becomes a daily
timer-visibility schedule only. There is no night lock/grace/override mode.
Collection continues through every reminder/quiet period and while settings
are closed. Remove the user-facing exit and recording-pause routes and the
emergency hotkey. Normal Windows shutdown and explicit maintenance updates
still flush data and stop; this is not an unkillable service/watchdog.

AC-1: only Ready/Working/DayResting are produced; legacy night state recovers.
AC-2: a compact, non-activating rest reminder appears without blocking input.
AC-3: timer hidden 20:00–06:00 (configurable); statistics continue overnight.
AC-4: no exit/pause/hotkey/challenge UI; closing settings leaves logging alive.
AC-5: legacy config/history/usage preserved; collection cannot be disabled by
an old pause setting; clean maintenance shutdown flushes the last interval.
AC-6: build, tests, format, UI/runtime QA, install and Git push are verified.

Privacy scope stays process names and activity durations/states, not content.
No service, watchdog, new dependencies, GitHub release or main merge.
