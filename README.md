# Work Rest Guard

Work Rest Guard is a small, open-source Windows tray application that places a
calm but firm break overlay over one selected work monitor. A second monitor
can remain available for Obsidian or other low-intensity use.

It combines two guardrails:

- a configurable work/break cycle, defaulting to **90 minutes of work and
  15 minutes of rest**;
- a daily night block, defaulting to **20:00–06:00**.

Work Rest Guard is a behavioral barrier, not a security boundary. It does not
install a driver or service, require administrator access, change Windows
policies, collect telemetry, or inspect window titles and document content.

## What happens during the day

After the first mouse or keyboard input, the 90-minute work timer begins. When
it expires, the selected monitor is covered for 15 minutes.

While the overlay is hidden, a compact non-activating timer stays in the
top-right corner of the work monitor. It shows the remaining work time,
finishing window, or night grace period without stealing focus. During a break
or night block, the large overlay countdown remains visible.

One finishing window can be used per cycle:

- 5 minutes;
- 15 minutes;
- 30 minutes.

When it ends, the full 15-minute break starts and the finishing buttons do not
return. The break completes automatically. A long, exact manual statement can
skip the current break and start a new 90-minute cycle.

## What happens at night

At 20:00 local Windows time, night mode takes priority and covers the work
monitor until 06:00. One 10-minute finishing window is available per night.

After that, access requires a separate night statement typed exactly by hand.
The override applies only to that night's block; the regular 90/15 cycle starts
again immediately.

Night mode covers only the selected work monitor. It does not cover the
secondary Obsidian monitor.

## Safety and recovery

- Hold **Ctrl+Alt+Shift+F11 for five seconds** for an emergency exit.
- Start with `--safe` to open settings without enabling an overlay.
- If the selected monitor disappears, Guard fails open and covers nothing.
- Configuration, runtime state, and a 30-day event history are stored under
  `%LocalAppData%\WorkRestGuard`.
- Exact statements, clipboard contents, window titles, and Obsidian data are
  never written to disk.

The overlay does not and cannot cover the Windows secure desktop used by UAC,
Ctrl+Alt+Delete, sign-in, or Windows Lock. Task Manager and process termination
remain system-level escape routes.

## First run

1. Download `WorkRestGuard.exe` from the latest GitHub Release.
2. Place it in a stable folder, for example
   `%USERPROFILE%\Apps\WorkRestGuard\`.
3. Run it and select the work monitor.
4. Use **Preview monitor** before saving.
5. Choose whether to start Work Rest Guard with Windows.

Left-click the tray icon once to open Settings. Settings remain available on
the unprotected monitor even while the work monitor is covered.

Protection-reducing changes require the appropriate day or night statement.
Stronger settings do not.

## Українською

Work Rest Guard перекриває лише вибраний робочий монітор:

- після 90 хвилин роботи запускає автоматичну 15-хвилинну перерву;
- один раз дозволяє доробити 5, 15 або 30 хвилин;
- щодня о 20:00 вмикає нічний блок до 06:00;
- один раз за ніч дозволяє ще 10 хвилин;
- послаблення вимагає точного ручного вводу довгого тексту;
- лівий монітор з Obsidian залишається доступним;
- історія за 30 днів зберігається лише локально.

Аварійний вихід: утримуй **Ctrl+Alt+Shift+F11** п'ять секунд.

## Verify a release

Each release includes a SHA-256 checksum and GitHub build-provenance
attestation.

```powershell
Get-FileHash .\WorkRestGuard.exe -Algorithm SHA256
gh attestation verify .\WorkRestGuard.exe -R iktnb/work-rest-guard
```

Version 0.1.1 is not Authenticode-signed. Windows SmartScreen may warn about a
newly downloaded executable even when its checksum and provenance are valid.

## Build and test

Requirements: Windows x64 and the .NET 10 SDK.

```powershell
dotnet restore WorkRestGuard.slnx
dotnet build WorkRestGuard.slnx -c Release --no-restore
dotnet test tests\WorkRestGuard.Tests\WorkRestGuard.Tests.csproj -c Release --no-build
dotnet format WorkRestGuard.slnx --verify-no-changes --no-restore
dotnet publish src\WorkRestGuard\WorkRestGuard.csproj -c Release -r win-x64 --self-contained true -o artifacts\win-x64
```

## Uninstall

1. In Settings, clear **Start with Windows**. This protection-reducing change
   requires the night statement.
2. Exit normally, or use the emergency hotkey.
3. Delete `WorkRestGuard.exe`.
4. Optionally delete `%LocalAppData%\WorkRestGuard`.

## License

MIT
