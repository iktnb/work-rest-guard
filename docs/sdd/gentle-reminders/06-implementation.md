# Implementation

Version 0.3.0 implements AC-1 through AC-5. GuardEngine now emits only Ready,
Working and DayResting; legacy runtime phases recover without enforcement.
TimerPolicy applies daily local quiet hours independently of collection.
StatusTimerForm renders both work and rest in the same non-activating small
window. Settings expose reminder visibility and quiet times, without challenges.

Removed the full-screen overlay/controllers, hotkey window, challenge controls,
exact typing and old night resolver. Their obsolete enforcement tests were
replaced by reminder, quiet-hour and compatibility cases. Historical enums/data
readers remain. Normal UsageTrackingService ignores legacy pause flags.

ApplicationContext owns the message loop independently of Settings. Collection
runs before timer UI work; UI exception recovery leaves it enabled. A named
maintenance signal flushes before clean shutdown. Native lifecycle harnesses use
an isolated data directory and unnamed signals, never touching installed data.
Monitor enumeration is cached for 30 seconds and invalidated on display changes;
quiet hours skip monitor discovery entirely.

No dependency or JSON schema version change. README and SECURITY describe the
new lifecycle, data scope and limits. See 07-QA.md for validation and delivery.
