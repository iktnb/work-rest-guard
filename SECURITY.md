# Security and privacy

Work Rest Guard is a user-level time logger with optional visual rest reminders.
It does not block screens or resist a user who terminates or modifies the process.

Please report security/privacy issues privately through GitHub's security
advisory feature. Do not include private usage history, monitor identifiers,
local paths or document contents in a public issue.

The application:

- runs as the signed-in user without administrator privileges or `uiAccess`;
- installs no service, driver or watchdog;
- stores configuration, state, bounded technical logs, 30 days of reminder/legacy
  event metadata and 90 UTC days of local usage intervals;
- samples foreground process names and time since last input during normal use;
- never reads window titles, browser URLs/tabs, full executable paths, document
  contents or keystroke contents;
- records continuously in normal mode, including when reminders are hidden or
  Settings is closed; there is no recording pause or exit control in the UI;
- supports a clean maintenance stop and a separate diagnostic `--safe` launch;
- makes no network requests and sends no telemetry.

Application names and usage times are personal data stored in the user's local
profile. Usage files have an explicit schema, strict interval validation, a
16 MiB read limit per UTC day, and atomic replacement. Malformed/unsupported
files are not overwritten by collection. Recording errors are displayed in the
statistics view and isolated from reminders. Recoverable UI errors flush data
and leave collection running. Fatal failures and OS process termination can
still stop a normal Windows application. There is no database, cloud account,
external analytics SDK or new runtime dependency.
