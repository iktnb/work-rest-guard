# Work Rest Guard

A small Windows tray application for local time statistics and gentle rest
reminders. Version **0.3.0** removes screen blocking, night restrictions, typed
challenges and the emergency exit hotkey.

The compact timer shows work time and **TIME TO REST** during a break reminder.
It never covers a monitor or takes keyboard focus. Default work/rest durations
are 90/15 minutes. The timer is hidden daily from **20:00 to 06:00** local time;
this changes visibility only. Collection continues through the night.

## Continuous time statistics

Use **Time statistics** in the tray, the Settings statistics tab, or start with
`--statistics`. Reports show application ranking, daily summaries and a timeline
for a selected day, seven days or thirty days. Refresh updates the snapshot and
shows how much time was actually recorded. Durations sort numerically.

Normal operation always records, even when reminders are disabled, no timer
monitor is selected, or Settings is closed. The tray has no recording pause or
exit item. Closing Settings leaves the application in the tray. Start with
Windows is enabled by default and can be changed in Settings.

Every five seconds the app samples the foreground process and time since last
keyboard/mouse input. The default idle threshold is five minutes. Only the
foreground program receives active time. Reading or video playback without
input can count as **No input**. These are estimates of computer activity;
timer completion does not prove actual rest, sleep, attention or productivity.

Windows lock/disconnect and observed suspend periods are separate states.
Missing observations, a stopped app or unexplained polling gaps remain
**Unrecorded**; failed observations are **Unknown state**. Application shares
include unattributed activity in their denominator. Night activity means activity
in the daily quiet hours configured at capture time. Local-day reports respect
the Windows timezone and daylight-saving time; timeline times include UTC
offsets. Boundaries normally have five-second precision. Delayed polls can span
up to fifteen seconds; longer unexplained gaps remain unrecorded.

Only process names, UTC interval boundaries, activity state, reminder phase and
a quiet-hours flag are saved. There are no window titles, URLs, browser tabs,
full executable paths, keystroke contents or document contents. All Chrome
usage is grouped as `chrome.exe`; websites cannot be distinguished. Nothing is
uploaded and the application makes no network requests.

Daily versioned JSON files live in `%LocalAppData%\WorkRestGuard\usage`, retaining
90 UTC calendar days. Writes are batched every 30 seconds. Suspend, normal
shutdown, maintenance stop and Refresh also flush. A crash or forced termination
may lose roughly the last 30 seconds, or more during disk/message-loop delays.
Corrupted or unsupported files are preserved and reported. Existing 30-day event
history stays separate; it cannot reconstruct application usage from before
recording began.

## Setup and lifecycle

1. Build the source below and place `WorkRestGuard.exe` in a stable folder such
   as `%USERPROFILE%\Apps\WorkRestGuard`.
2. Start it, select the timer monitor and use **Preview monitor** if needed.
3. Set reminder durations, daily timer hide/show times and the input threshold.
4. Save. Left-click the tray icon to reopen Settings.

The work timer begins with the first input. On expiry it shows the rest message
for the configured break duration, then waits for new input to begin another
cycle. **Start a break now** starts a reminder immediately. Work and reminders
can continue during quiet hours while the timer remains hidden.

The legacy `NightModeEnabled` setting is ignored and normalized to false.
Legacy paused-recording settings are normalized to enabled. Old night/snooze
runtime states recover to Ready. Historical event and usage files remain readable
without being rewritten. Older versions still enforce their old behavior if
installed again; use a backup of configuration when deliberately rolling back.

`--background` starts without opening Settings. `--history` and `--statistics`
select their respective tabs on initial launch. A second normal launch opens
Settings in the existing process. `--safe` is a diagnostic launch with recording
and reminders disabled for that process; it is not the normal startup mode.

For an update or uninstall, `WorkRestGuard.exe --stop-for-update` requests a
clean maintenance stop and flushes data. It returns 2 when no compatible running
instance exists. Wait for the existing process to exit before replacing the file.
There is no service, watchdog or protection against Windows process termination.
Recording resumes when the app starts again; it cannot observe a powered-off PC.

## Українською

Програма постійно записує локальну статистику часу у програмах. Після 90 хвилин
роботи маленький таймер показує **ЧАС ВІДПОЧИТИ** протягом 15 хвилин. Тривалість
можна змінити. Екран не блокується, підтверджень і аварійної комбінації немає.
З 20:00 до 06:00 таймер прихований, але статистика продовжує збиратися.
Закриття налаштувань залишає застосунок у треї. Автозапуск увімкнений за
замовчуванням. Записуються назви програм і тривалість активності, без вмісту
вікон, сайтів або натиснутих клавіш.

## Build and verify

Requirements: Windows x64 and the .NET 10 SDK. Version 0.3.0 is available from
source; no versioned GitHub release has been published for it yet.

```powershell
dotnet restore WorkRestGuard.slnx
dotnet build WorkRestGuard.slnx -c Release --no-restore
dotnet test tests\WorkRestGuard.Tests\WorkRestGuard.Tests.csproj -c Release --no-build
dotnet format WorkRestGuard.slnx --verify-no-changes --no-restore
dotnet publish src\WorkRestGuard\WorkRestGuard.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\win-x64
```

The native `tests/OverlayHarness` project previews timers, Settings and synthetic
statistics. `--lifecycle --capture <report.json>` exercises Settings close,
quiet hours, UI recovery and a flushing maintenance stop in an isolated profile.
It never constructs a full-screen blocking window.

Published GitHub releases include SHA-256 checksums and build-provenance
attestations. Local development builds are not attested or Authenticode signed.

## Uninstall

Clear **Start with Windows** in Settings, save, request the maintenance stop,
then delete the executable. Local statistics can be preserved or deleted
separately under `%LocalAppData%\WorkRestGuard`.

## License

MIT
