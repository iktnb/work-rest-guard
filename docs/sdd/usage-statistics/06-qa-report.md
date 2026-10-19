# QA report — local build 0.2.0

Date: 2026-10-19. Reviewer: Codex self-review. Verdict: PASS for the scoped
local implementation. Subsequent local installation was authorized and verified;
a GitHub release remains pending.

Source snapshot SHA-256 (sorted relative paths and bytes of 60 source/project
files under src/ and tests/, excluding bin/obj/):
`6be9074d7714b0d38344e4fd0c8a16c493cf54bce03b7446cffff98a3786986f`.

Published binary SHA-256:
`dee08cb5a792e033a85d4175b8b63b897ce22077d28e97f63fc4c0e598596e2d`.
Output: `artifacts/win-x64/WorkRestGuard.exe`, 51,712,217 bytes, self-contained
Windows x64 single file, version 0.2.0. Validation used baseline ebe644e plus
the source changes identified by the source snapshot above.

## Checks

| Check | Result |
|---|---|
| Baseline restore/build/test/format | PASS, 37 tests |
| `dotnet build WorkRestGuard.slnx -c Release --no-restore` | PASS, zero warnings/errors |
| `dotnet test tests/WorkRestGuard.Tests/WorkRestGuard.Tests.csproj -c Release --no-build --collect "XPlat Code Coverage" --results-directory artifacts/coverage-final` | PASS, 70 tests, zero skipped |
| `dotnet format WorkRestGuard.slnx --verify-no-changes --no-restore` | PASS |
| `git diff --check` | PASS |
| `dotnet list WorkRestGuard.slnx package --vulnerable --include-transitive --no-restore` | PASS, no vulnerable packages reported by NuGet for all three projects |
| .NET built-in analyzers and source security review | PASS, no new package, subprocess, credential or outbound-network path in the app |
| Single-file publish with repository CI arguments | PASS, exactly one executable |
| Published executable `--safe --statistics` | PASS, input idle and settings surface initialization confirmed; config/state/history hashes unchanged |
| Windows usage-source probe in temporary store | PASS, about 11.5 seconds recorded as idle, no storage error; separate foreground lookup resolved a process name |

Coverage: 259/281 lines (92.17%) across UsageModels, UsageStore, UsageReport,
UsageTrackingService and WindowsUsageSource. Whole-app line coverage is 25.49%,
including existing and new WinForms surfaces. The repository has no configured
coverage threshold. UI behavior is additionally checked in the Windows harness;
this is not a claim of complete application coverage.

## Acceptance evidence

| AC | Evidence | Result |
|---|---|---|
| AC-1 | UsageTrackerTests switching/idle/wraparound cases; native source probe | PASS |
| AC-2 | MissingPollsAndClockDiscontinuitiesAreNotUsage; ExplicitSuspendCanBridgeSleepButResetCannotBridgeOfflineTime; service lock/suspend/pause tests | PASS |
| AC-3 | UsageStoreTests real-file roundtrip, merge, UTC-midnight split, retention, invalid/newer-file preservation and overlap rejection | PASS |
| AC-4 | UsageReportTests denominator including unknown apps, clipping, case grouping, continuity, local midnight and Kyiv 23/25-hour DST days | PASS |
| AC-5 | StatisticsHarness screenshot/state runs for Ukrainian, English/narrow, enlarged text, daily rows, timeline, empty/error, long names and real Settings tab | PASS |
| AC-6 | OldConfigurationGetsDefaultsAndPrivacyPauseNeedsNoChallenge; protection-disabled collection; safe-mode test; storage and error-log failure isolation tests; source/schema inspection | PASS |
| AC-7 | Build/test/format/audit/publish/smoke and the evidence above | PASS |

Harness checks exercised day/7/30-day filters, descending numeric duration sort,
accessible names, native tab traversal through date/period/refresh/views/grid,
and absence of a hidden Save default button on the Statistics tab. Screenshots
were visually inspected at 820 and 1100 pixel widths and with enlarged text.
All screenshot content is synthetic; the user's event history was not modified.

## Review corrections and limits

- Kept observation errors visible after Refresh instead of clearing them after
  an unrelated successful flush.
- Isolated diagnostic-log write failures from protection.
- Exposed per-day recorded duration and missing-day dashes, localized date and
  numeric formatting, and maintained privacy-toggle state in an open form.
- Prevented Enter in read-only tabs from applying hidden settings. The harness
  caught the initial pre-handle tab-selection case; explicit initialization was
  added and the integration check rerun successfully.
- Clarified approximate sampling and nominal crash-tail persistence guarantees.

Real OS lock/sleep cycles were not triggered on the user's machine; their
signals were simulated through the runtime service. Physical keyboard and screen
reader sessions, a complete high-DPI matrix and multi-day soak/performance tests
were not performed. Native tab traversal and accessible names were verified
programmatically; no WCAG certification is claimed. Foreground sampling groups
by process executable, not websites or attention. Long idle reading/video and
closed/paused collection retain the documented limitations.

The first hidden-process smoke attempt used MainWindowHandle, which excludes
hidden windows. The final smoke uses input-idle/startup-log assertions plus the
actual Settings harness; no product startup failure was observed.

Evidence files (ignored build artifacts): `artifacts/qa/`,
`artifacts/coverage-final/c8572147-a123-4bb9-9a10-3a9aa159c709/coverage.cobertura.xml`,
and `artifacts/WorkRestGuard-0.2.0.sha256`.

## Authorized local installation

On the user's subsequent 2026-10-19 request, the verified binary was copied to
the stable per-user Apps/WorkRestGuard directory. The previous 0.1.2 executable
and configuration/state/history were backed up before replacement. Existing
data hashes were checked before launch. The enabled Windows startup entry was
corrected from an old build-artifact path to the stable installation path.
Launch uses the existing protection and night settings, with normal collection
enabled. Local verification details remain in the ignored installation evidence;
personal usage records are never committed to Git.
