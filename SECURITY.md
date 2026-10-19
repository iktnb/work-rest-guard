# Security and safety

Work Rest Guard is intentionally a user-level behavioral barrier. It is not
designed to resist an administrator or a user who terminates or modifies the
process.

Please report security or privacy issues privately through GitHub's security
advisory feature. Do not include personal monitor identifiers, local paths,
typed confirmation text, or private document contents in a public issue.

The application:

- runs as the signed-in user;
- does not request `uiAccess` or administrator privileges;
- does not install a service, driver, or watchdog;
- stores only configuration, state, bounded technical logs, and 30 days of
  local guard event metadata, plus 90 UTC days of optional local usage intervals;
- optionally reads the foreground process name and time since last input;
- never reads window titles, browser URLs/tabs, full executable paths, document
  contents or keystroke contents; application names and usage times remain
  personal data stored under the signed-in user's local profile;
- allows usage recording to be paused independently of guard protection;
- never sends telemetry or network requests.

Usage files have an explicit schema version, strict interval validation, a
16 MiB read limit per UTC day, and atomic replacement. Unsupported or malformed
files are not overwritten during collection. Usage errors are isolated from
guard operation, including failures to write the diagnostic log. There is no
database, cloud account, external analytics SDK, or additional dependency.
