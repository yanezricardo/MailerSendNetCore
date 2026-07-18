# Changelog

All notable changes to this project are documented in this file.

## [0.3.0] - 2026-07-18

### Added

- Complete MailerSend warning collection through `WarningItems`.
- Response state helpers: `IsQueued`, `IsSuppressed`, and `HasErrors`.
- `ApiException.HttpStatusCode`, `ApiException.RetryAfter`, and
  `ApiException.IsTransient`.
- Package Validation against published version `0.2.0`.
- Repository-scoped NuGet source configuration.

### Changed

- Replaced `Microsoft.Extensions.Http.Polly` with an internal retry handler using the Polly v7
  compatibility API supported by Polly 8.
- Corrected package product metadata and the test project name.
- Retry configuration is resolved from the application service provider without building an
  intermediate provider.

### Fixed

- Parse non-empty `202 Accepted` response bodies.
- Preserve `SOME_SUPPRESSED` and `ALL_SUPPRESSED` warning details.
- Treat `ALL_SUPPRESSED` without a message ID as not queued.
- Preserve request content across retries and dispose failed retry responses.
- Propagate caller cancellation.

### Compatibility

- Existing public types, registration overloads, callbacks, defaults, and `422` response behavior
  remain available.
- Legacy retry behavior, including `POST` retries, remains available when `UseRetryPolicy` is
  enabled.
- Polly callback types remain public for compatibility and are scheduled for redesign in `1.0`.

[0.3.0]: https://github.com/yanezricardo/MailerSendNetCore/compare/v0.2.0...v0.3.0
