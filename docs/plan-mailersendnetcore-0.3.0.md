---
id: PLAN-MAILERSENDNETCORE-0-3-0
title: MailerSendNetCore 0.3.0 compatibility modernization
status: in_progress
owner: maintainer
updated: 2026-07-18
issue: 72
---

# Plan — MailerSendNetCore 0.3.0

## Objective

Publish `0.3.0` by correcting obsolete dependencies, dependency injection, MailerSend response
parsing, and retry implementation while preserving the established public consumption model.

The package is public, MIT-licensed, and used by known and unknown business consumers. Its
published API is therefore treated as a stable contract even though the version is below `1.0`.

## Compatibility rationale

`0.3.0` prioritizes compatibility over API cleanup because the package has real consumers that
cannot all be identified or upgraded together. Polly callback types and the current response model
therefore remain public compatibility contracts for this release.

This release may add small, low-risk response and exception diagnostics, but it must not force
consumer migrations. Removing the Polly dependency from public signatures, redesigning retry
configuration, or replacing response contracts is deferred to `1.0`, where those changes can be
explicitly versioned and documented.

## Compatibility guardrails

Version `0.3.0` must not:

- remove or rename public types or members;
- change existing signatures, return types, or property types;
- add required parameters;
- change current defaults;
- add `[Obsolete]` warnings that could break `TreatWarningsAsErrors` consumers;
- replace `MailerSendEmailResponse`;
- change `422` from a response into an exception;
- remove retry options or public callbacks;
- change `net10.0`.

Additive APIs and internal implementation changes are allowed. Removing the legacy API is reserved
for `1.0`.

## Baseline

Verified on 2026-07-18:

- branch: `master`;
- commit: `d0df482`;
- latest published version: `0.2.0`;
- target framework: `net10.0`;
- license: MIT;
- endpoints: single email, bulk email, and bulk status.

Published and locally observed consumers use:

- all `AddMailerSendEmailClient` registration styles;
- `IMailerSendEmailClient`;
- send and bulk methods;
- `MessageId`, `Errors`, and `Warnings`;
- `ApiException.StatusCode`;
- retry options and public Polly callbacks.

Confirmed defects:

1. `Microsoft.Extensions.Http.Polly` brings in deprecated `Polly.Extensions.Http 3.0.0`.
2. DI registration calls `BuildServiceProvider()`.
3. Single-email sending ignores the body of `202 Accepted`.
4. MailerSend returns a warning array, but the published DTO exposes one warning.
5. `ALL_SUPPRESSED` can return `202` without a message ID.
6. Retry can include non-idempotent `POST` operations.
7. Restore can inherit undeclared global package sources.
8. Package and test project metadata contain naming errors.

## Technical decisions

### 1. Dependencies and DI

- Add `nuget.config` with `<clear />`, NuGet.org, and source mapping for `*`.
- Remove `Microsoft.Extensions.Http.Polly`.
- Remove `Polly.Extensions.Http` from the graph.
- Keep direct Polly 8 because published callback signatures depend on Polly types.
- The project may download the published `0.2.0` dependency closure only as build-time
  `PackageDownload` inputs for SDK Package Validation. These packages must remain absent from the
  candidate `.nupkg` and consumer dependency graph.
- Do not add `Microsoft.Extensions.Http.Resilience`.
- Replace `AddPolicyHandler` with an internal `DelegatingHandler` that executes the Polly v7 policy
  API supported by Polly 8.
- Remove `BuildServiceProvider()`.
- Resolve options and handler state from the application container.
- Preserve every registration signature and its `IServiceCollection` return type.
- Do not enable `ValidateOnStart()` in this release.

Compatibility classification: removing the intermediate provider intentionally stops executing
options configuration during registration. In `0.2.0`, that timing was an unintended consequence
of `BuildServiceProvider()` and could execute configuration more than once. In `0.3.0`, failures
from an options callback surface when the application provider resolves the options or client.
This internal lifecycle correction is accepted for S1; signatures, defaults, and normal consumer
usage remain unchanged.

Expected graph:

```text
Microsoft.Extensions.Http.Polly: absent
Polly.Extensions.Http: absent
Polly 8: retained
Deprecated packages: zero
Known vulnerabilities: zero
```

### 2. Retry compatibility bridge

Keep:

- `UseRetryPolicy`;
- `RetryCount`;
- `RetryDelayInMilliseconds`;
- existing callback signatures;
- existing defaults and callback behavior.

Rules:

- `UseRetryPolicy=false` still sends one request;
- `UseRetryPolicy=true` preserves the complete legacy behavior, including retries for `POST`;
- `Retry-After`, retry count, configured delay, callbacks, and cancellation remain compatible.

Implementation:

- recreate the existing retry policy as an `IAsyncPolicy<HttpResponseMessage>` using
  `WaitAndRetryAsync`, preserving retry conditions, callback invocation order, `Retry-After`
  handling, and delay calculation;
- execute it from an internal `DelegatingHandler`;
- register the handler through `AddHttpMessageHandler`;
- preserve callback result, retry count, delay, and `Context`;
- dispose failed responses before the next attempt;
- ensure request content can be replayed for the existing email requests.

The unsafe legacy opt-in remains available for compatibility. README must explain that retrying
`POST` may duplicate email delivery after ambiguous transport failures and therefore recommend
disabling retries unless consumers explicitly accept that trade-off. Add a source comment beside
the callback-bearing options identifying them as legacy compatibility API scheduled for redesign
in `1.0`; do not add `[Obsolete]`.

A safer retry API and removal of the legacy behavior are deferred to `1.0`.

### 3. Compatible response correction

Keep these properties unchanged:

```csharp
public string? MessageId { get; set; }
public string? Message { get; set; }
public IDictionary<string, string[]>? Errors { get; set; }
public MailerSendEmailWarningResponse? Warnings { get; set; }
```

Add:

```csharp
private IReadOnlyList<MailerSendEmailWarningResponse> _warningItems =
    Array.Empty<MailerSendEmailWarningResponse>();

public IReadOnlyList<MailerSendEmailWarningResponse> WarningItems
{
    get => _warningItems;
    init => _warningItems = Array.AsReadOnly(
        (value ?? Array.Empty<MailerSendEmailWarningResponse>()).ToArray());
}

public bool IsQueued { get; }
public bool IsSuppressed { get; }
public bool HasErrors { get; }
```

Semantics:

- `WarningItems` supports consumer object initialization and serializer construction while storing
  a defensive read-only snapshot;
- `IsQueued` is true only when `MessageId` exists;
- `IsSuppressed` is true when `WarningItems` contains `ALL_SUPPRESSED`;
- `HasErrors` is true when `Errors` is not empty;
- `SOME_SUPPRESSED` may still be queued;
- `ALL_SUPPRESSED` without a message ID is not queued.

Parsing:

- read non-empty `202` bodies;
- map provider warnings through internal parsing rather than relying on post-construction mutation
  of `WarningItems`;
- put all warnings in `WarningItems`;
- put the first warning in legacy `Warnings`;
- preserve `X-Message-Id`;
- preserve empty-body `202`;
- preserve current `422` response behavior;
- correct warning JSON mappings without renaming public properties.

Centralize provider warning types internally:

```csharp
internal static class MailerSendWarningTypes
{
    public const string SomeSuppressed = "SOME_SUPPRESSED";
    public const string AllSuppressed = "ALL_SUPPRESSED";
}
```

Compare provider warning types with `StringComparison.OrdinalIgnoreCase`.

No outcome enum is introduced.

### 4. Compatible exception improvement

Keep `ApiException`, its existing constructor, integer `StatusCode`, `Response`, and `Headers`.

Add:

```csharp
public HttpStatusCode HttpStatusCode { get; }
public TimeSpan? RetryAfter { get; }
public bool IsTransient { get; }
```

Do not add an exception hierarchy.

Caller cancellation must remain `OperationCanceledException`. Tokens must not appear in logs,
exceptions, or test snapshots.

### 5. Package quality

- Correct the erroneous `Product=MailerSendNetCoreNetCore` metadata value to
  `Product=MailerSendNetCore`.
- Rename `MailserSend.UnitTests` to `MailerSendNetCore.UnitTests`.
- Enable .NET SDK Package Validation against published version `0.2.0`:

```xml
<EnablePackageValidation>true</EnablePackageValidation>
<PackageValidationBaselineVersion>0.2.0</PackageValidationBaselineVersion>
```

- Run baseline validation in compatibility mode, not strict equality mode, because this release
  intentionally adds compatible public members.
- Treat every package compatibility diagnostic as blocking until it is classified as:
  - a regression that must be fixed;
  - a verified compatible API or metadata difference that requires no suppression; or
  - an intentional incompatibility that triggers a stop condition and requires explicit approval
    before a documented suppression.
- Do not generate or accept blanket compatibility suppressions.
- Use the SDK validation built into `dotnet pack`; do not add a runtime dependency for this check.
- Add `CHANGELOG.md` and concise `0.2.0` → `0.3.0` release notes.
- Update README without removing existing usage patterns.
- Document `UseRetryPolicy=false` as the recommended safe configuration.
- Document that legacy API removal is planned only for `1.0`.
- Build and inspect the `.nupkg`.

## Out of scope

- New MailerSend endpoints.
- New result or exception hierarchies.
- Removing Polly types from published signatures.
- Removing retry options or callbacks.
- Changing current defaults.
- Changing target frameworks.
- Migrating from Newtonsoft.Json.
- Modifying consumer repositories in this Pull Request.
- Real MailerSend calls or credentials.
- Automatic publication on PR merge.

## Implementation slices

### S0 — Baseline and compatibility contract

- [x] Create Issue [#72](https://github.com/yanezricardo/MailerSendNetCore/issues/72).
- [x] Create branch `codex/mailersendnetcore-0.3.0`.
- [x] Capture restore, build, tests, graph, deprecations, and vulnerabilities.
- [x] Capture the public API of published `0.2.0`.
- [x] Add compile-time fixtures for existing public usage.
- [x] Confirm current MailerSend response examples.

Output: reproducible baseline and explicit compatibility contract.

### S1 — Dependencies, DI, and retry bridge

- [x] Add `nuget.config`.
- [x] Remove `Microsoft.Extensions.Http.Polly` and `Polly.Extensions.Http` from the candidate and
      consumer graphs.
- [x] Implement an internal `DelegatingHandler` using the Polly v7 API supported by Polly 8.
- [x] Preserve public signatures, options, defaults, and callbacks.
- [x] Remove `BuildServiceProvider()`.
- [x] Add DI and retry regression tests.

Output: clean graph and compatible retry behavior.

### S2 — HTTP response corrections

- [x] Parse `202` bodies.
- [x] Add `WarningItems`, `IsQueued`, `IsSuppressed`, and `HasErrors`.
- [x] Populate legacy `Warnings`.
- [x] Preserve `422`.
- [x] Add compatible `ApiException` diagnostics.
- [x] Correct cancellation behavior.
- [x] Add single and bulk regression tests.

Output: correct warnings and suppressions without replacing the public contract.

### S3 — Package and release preparation

- [x] Correct metadata and test project naming.
- [x] Add changelog, release notes, and README updates.
- [x] Enable and run .NET SDK Package Validation against published version `0.2.0`.
- [x] Run additional source compatibility fixtures against `0.2.0`.
- [x] Build and inspect the `.nupkg`.
- [x] Run minimal and known-consumer compilation checks.
- [x] Complete independent compatibility review.
- [ ] Prepare the Pull Request for approval.

Output: review-ready package candidate. Publication remains a separate human-approved action.

## Required tests

Compatibility fixtures must compile:

- every registration overload;
- options and callbacks;
- every `IMailerSendEmailClient` method;
- direct `MailerSendEmailClient` construction;
- reads of existing response properties;
- `WarningItems` initialization through a consumer object initializer;
- `catch (ApiException)` and integer `StatusCode`;
- current fluent message builders.

Regression tests must cover:

- DI without an intermediate provider;
- retry enabled and disabled;
- callback count and delay;
- `Retry-After`;
- cancellation during retry;
- request content replay across retries;
- disposal of failed responses before the next attempt;
- empty `202` with message ID;
- `SOME_SUPPRESSED`;
- `ALL_SUPPRESSED` without message ID;
- legacy `Warnings` plus complete `WarningItems`;
- `WarningItems` defensive-copy behavior and Newtonsoft.Json round-trip;
- compatible `422`;
- unexpected status and malformed body;
- bulk send and bulk status behavior.

Tests must not call MailerSend. Use controlled handlers and zero or minimal delays.

## Consumer validation

Required:

- pass .NET SDK Package Validation against published `0.2.0`;
- compile a minimal consumer against `0.2.0` and the candidate;
- restore the consumer from NuGet.org and the local package source.

When checkouts are available:

- compile Medicostop against the candidate;
- compile ProGarantia against the candidate;
- validate CreaHabitat separately before any future reintroduction.

An unavailable consumer checkout does not authorize an API break.

Validation completed on 2026-07-18:

- the same minimal source fixture compiled without warnings against published `0.2.0` and the local
  `0.3.0` package;
- a temporary, unmodified-source copy of Medicostop compiled without warnings against the local
  `0.3.0` package;
- no ProGarantia application checkout was available under the local GitHub workspace.

## Final validation

```bash
dotnet restore MailerSendNetCore.sln --configfile nuget.config
dotnet build MailerSendNetCore.sln --configuration Release --no-restore
dotnet test MailerSendNetCore.sln --configuration Release --no-build
dotnet package list \
  --project src/MailerSendNetCore/MailerSendNetCore.csproj \
  --include-transitive \
  --deprecated
dotnet package list \
  --project src/MailerSendNetCore/MailerSendNetCore.csproj \
  --include-transitive \
  --vulnerable
dotnet pack src/MailerSendNetCore/MailerSendNetCore.csproj \
  --configuration Release \
  --no-build \
  --output artifacts
git diff --check
```

Additionally:

- inspect `.nuspec`, assemblies, README, license, and dependencies;
- verify `Microsoft.Extensions.Http.Polly` and `Polly.Extensions.Http` are absent;
- verify `BuildServiceProvider()` is absent from `src/`;
- wait for green CI after the final push;
- publish only after explicit human approval.

## Acceptance criteria

- [x] No published API member is removed or renamed.
- [x] Existing signatures, defaults, options, and callbacks remain compatible.
- [x] .NET SDK Package Validation against `0.2.0` passes without unexplained suppressions.
- [x] Source compatibility fixtures pass.
- [x] `Microsoft.Extensions.Http.Polly` and `Polly.Extensions.Http` are absent from the candidate
      and consumer graphs.
- [x] No deprecated or vulnerable packages are reported.
- [x] DI does not build an intermediate provider.
- [x] Existing registration overloads work.
- [x] Existing response and exception properties remain available.
- [x] `202` warnings and suppressions are parsed correctly.
- [x] `ALL_SUPPRESSED` without a message ID is not queued.
- [x] `422` behavior remains compatible.
- [x] Legacy retry remains available.
- [x] README explains the duplicate-delivery risk of `POST` retries and presents enabling retries
      as an explicit operational trade-off.
- [x] Retrying the same request produces byte-equivalent HTTP content across attempts.
- [x] Cancellation propagates correctly.
- [x] Release build and tests pass without warnings.
- [x] The package artifact is valid.
- [x] Known consumers compile when available.
- [x] Independent review has no blocking compatibility finding.
- [x] Publication requires explicit human approval.

## Stop conditions

Stop and request a decision if:

- removing the deprecated transitive requires removing a public signature;
- package comparison reports an unexplained source or binary break;
- Polly 8 cannot preserve callback behavior;
- reproducing the legacy retry behavior requires deprecated Polly APIs that Polly 8 no longer
  supports;
- warnings cannot be fixed without changing the published `Warnings` type;
- the DI fix changes registration signatures or failure timing beyond the explicitly classified
  options lifecycle correction above;
- a dependency has unresolved vulnerability, license, or provenance concerns;
- testing requires credentials, real requests, or package publication.

## Definition of done

`0.3.0` is ready for publication approval when it has a clean dependency graph, no significant API
break from `0.2.0`, correct DI registration, compatible Polly 8 retries, correct warning and
suppression handling, green compatibility tests, and a validated `.nupkg`.

Reintroducing the package into CreaHabitat remains a separate delivery.

## Future work (`1.0`)

Possible work intentionally excluded from `0.3.0`:

- remove Polly types from the public API;
- replace retry callbacks with provider-independent abstractions;
- introduce a modern response model;
- redesign retry configuration;
- remove legacy compatibility APIs.
