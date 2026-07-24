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
  local event metadata;
- never sends telemetry or network requests.
