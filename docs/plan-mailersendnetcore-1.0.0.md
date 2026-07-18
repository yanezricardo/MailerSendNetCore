---
id: PLAN-MAILERSENDNETCORE-1-0-0
title: MailerSendNetCore 1.0.0 public API redesign and migration
status: proposed
owner: maintainer
created: 2026-07-18
updated: 2026-07-18
baseline_version: 0.3.0
---

# Plan — MailerSendNetCore 1.0.0

## Objective

Publish a stable `1.0.0` contract that removes legacy provider and resilience details from the
public API, makes result and error semantics explicit, and establishes a safe long-term evolution
model.

The release may contain intentional breaking changes, but major-version authorization is not a
license to redesign unrelated APIs. Every break must correct a demonstrated contract problem,
remove accidental public surface, or provide a materially safer consumption model.

## Why `1.0.0`

`0.3.0` intentionally preserved legacy contracts to protect existing consumers. It therefore
retains:

- Polly types in public dependency-injection overloads;
- retry settings tied to the current implementation;
- optional automatic retries for non-idempotent `POST` requests;
- a response model that mixes accepted, suppressed, and validation outcomes;
- nullable runtime behavior that is not always represented accurately by public annotations;
- public HTTP parsing helpers that may be implementation details rather than SDK contracts.

`1.0.0` is appropriate when these contracts can be replaced through a documented migration,
validated prereleases, and consumer feedback.

## Stable-release principles

1. Keep the package ID `MailerSendNetCore`.
2. Keep existing namespaces and request types unless a concrete defect requires a rename.
3. Break a public contract only when the replacement has clear consumer value.
4. Remove provider-independent concerns from provider-specific or third-party types.
5. Do not retry email submission automatically by default.
6. Make response and failure semantics explicit and testable.
7. Treat nullability annotations, default values, exception behavior, and dependency versions as
   public contract.
8. Freeze the API at release candidate and apply semantic versioning after `1.0.0`.
9. Do not publish stable `1.0.0` before known consumers compile against the release candidate.
10. Keep merge and package publication as separate human-approved actions.

## Compatibility budget

Expected breaking changes are limited to:

- removing Polly types from public signatures;
- replacing legacy retry configuration and callbacks;
- replacing ambiguous email response semantics;
- making `422` validation handling explicit;
- correcting `ApiException` naming, nullability, and collection invariants;
- removing confirmed accidental public implementation helpers.

The following should remain compatible unless S0 proves otherwise:

- package ID;
- core namespaces;
- `IMailerSendEmailClient` as the primary abstraction;
- email parameter and recipient concepts;
- fluent request construction;
- single email, bulk email, and bulk status capabilities;
- cancellation support;
- `net10.0`.

## Proposed target contract

Names below are working names. S0 must freeze final names before implementation.

### 1. Registration and configuration

Provide one primary registration path:

```csharp
services.AddMailerSendEmailClient(options =>
{
    options.ApiToken = configuration["MailerSend:ApiToken"];
    options.ApiUrl = configuration["MailerSend:ApiUrl"];
});
```

Requirements:

- use package-owned option and callback types only;
- validate required configuration through `IValidateOptions<T>`;
- never build an intermediate service provider;
- preserve deferred application-container resolution;
- keep the documented default API URL;
- do not enable application-wide startup validation implicitly;
- keep an `IConfiguration` overload only if it adds clear value without ambiguous section binding;
- remove overloads accepting Polly `DelegateResult`, `Context`, or policy callbacks;
- remove the overload accepting a prebuilt mutable options instance unless consumer evidence
  justifies it.

### 2. Resilience

Default behavior:

- email submission sends one `POST` attempt;
- caller cancellation is propagated unchanged;
- no hidden retry occurs after an ambiguous transport result.

If package-managed resilience remains:

- expose package-owned configuration such as `MailerSendRetryOptions`;
- retry safe operations only, initially bulk status `GET`;
- respect `Retry-After`;
- use bounded attempts and bounded total delay;
- expose package-owned retry diagnostics rather than Polly types;
- keep implementation dependencies internal to the contract;
- do not retry email `POST` unless MailerSend documents an idempotency mechanism and the caller
  supplies the required idempotency value.

S0 must choose and document one implementation:

1. no internal retry; consumers own resilience; or
2. internal resilience with provider-independent public options.

The decision must compare dependency cost, observability, cancellation, disposal, request replay,
and commercial license compatibility. It must not be chosen only to preserve the current
implementation.

### 3. Email submission result

Replace the ambiguous response state with one explicit result contract. Proposed direction:

```csharp
public enum MailerSendEmailOutcome
{
    Queued,
    QueuedWithWarnings,
    Suppressed
}

public sealed class MailerSendEmailResult
{
    public MailerSendEmailOutcome Outcome { get; }
    public string? MessageId { get; }
    public IReadOnlyList<MailerSendWarning> Warnings { get; }
}
```

Required invariants:

- `Queued` requires a message ID and no warnings;
- `QueuedWithWarnings` requires a message ID and at least one warning;
- `Suppressed` has no message ID and includes suppression details when supplied;
- warning collections are never null and cannot be mutated by consumers;
- provider JSON DTOs remain internal;
- unknown future provider warning types are preserved as data rather than rejected.

S0 must confirm whether the current `MailerSendEmailResponse` name can be retained with a corrected
shape or whether a new `MailerSendEmailResult` name produces a clearer migration.

### 4. Errors and exceptions

Use a small exception model:

- `MailerSendException` as the package base exception;
- `MailerSendApiException` for non-success HTTP/provider failures;
- `MailerSendValidationException` for `422` with structured validation errors.

Do not add a broad exception hierarchy without distinct recovery behavior.

`MailerSendApiException` should expose:

```csharp
public HttpStatusCode StatusCode { get; }
public string? ResponseBody { get; }
public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }
public TimeSpan? RetryAfter { get; }
public bool IsTransient { get; }
```

Requirements:

- headers and error collections are never null;
- nullable annotations match runtime behavior;
- caller cancellation remains `OperationCanceledException`;
- API tokens and message content are not copied into exception messages;
- `422` no longer shares the success result contract if the S0 migration review confirms the
  proposed validation exception;
- rate limiting remains identifiable through status and `RetryAfter`; add a dedicated rate-limit
  exception only if consumers demonstrate different recovery logic.

### 5. Request contracts

Keep current request concepts and fluent builders where they are sound:

- sender and recipients;
- CC, BCC, and reply-to;
- subject, HTML, and text;
- template variables and personalization;
- attachments and tags;
- scheduled send time;
- bulk email submission.

Review and correct:

- required versus optional nullability;
- collection mutability and defensive copies;
- UTC and Unix-time semantics for scheduled sending;
- validation of empty recipient collections;
- attachment disposition validation;
- duplicate or ambiguous fluent overloads.

Avoid converting every request DTO into a new hierarchy. Preserve working names and fluent methods
when no defect or ambiguity exists.

### 6. Public surface cleanup

Inventory all exported members from `0.3.0`.

Candidates for internalization:

- `ObjectResponseResult<T>`;
- `HttpResponseMessageExtensions`;
- HTTP header and JSON parsing helpers;
- any public type used only by the generated/internal transport layer.

Removal requires evidence that the member is accidental implementation surface. Document every
removed type or member in the migration guide and API compatibility report.

## Version and prerelease strategy

Use the existing package ID with prerelease versions:

1. `1.0.0-alpha.1` — target contract available for design validation.
2. `1.0.0-beta.1` — feature complete, migration guide complete.
3. `1.0.0-rc.1` — API frozen, known consumers compiling.
4. `1.0.0` — identical public API to the accepted release candidate.

Additional prereleases are allowed when findings require correction. Do not publish stable merely
because a calendar date has been reached.

Package Validation strategy:

- compare the alpha API against published `0.3.0`;
- classify every break individually;
- use narrow, documented compatibility suppressions only for approved breaks;
- store a human-readable `0.3.0` → `1.0.0` API difference report;
- validate the final package against the accepted release candidate to prevent late breaks.

## Consumer migration strategy

Create a concise migration table before beta:

| `0.3.0` | `1.0.0` direction |
| --- | --- |
| Polly callback registration overloads | Package-owned configuration or consumer-owned resilience |
| `UseRetryPolicy` and legacy retry fields | Safe retry configuration or removal |
| `MailerSendEmailResponse.Warnings` | Non-null read-only warning collection |
| `IsQueued` / `IsSuppressed` inference | Explicit outcome |
| `422` returned as `MailerSendEmailResponse` | Structured validation exception, subject to S0 approval |
| `ApiException` with integer status and nullable runtime headers | Typed status and non-null collections |
| Public HTTP parsing helpers | Internal implementation |

Known-consumer validation:

- CreaHabitat;
- Medicostop;
- ProGarantia when its application checkout is available;
- a minimal standalone consumer;
- source fixtures covering every supported registration and client method.

Consumer migrations must occur in separate repositories and Pull Requests. Do not combine
application behavior changes with the SDK redesign. Do not deploy or publish consumer changes
without separate approval.

## Implementation slices

### S0 — Contract inventory and decisions

- [ ] Create the implementation Issue and branch.
- [ ] Download and archive the published `0.3.0` API baseline.
- [ ] Capture exported types, members, nullability, defaults, and dependency graph.
- [ ] Search known consumers for direct and transitive API usage.
- [ ] Confirm current MailerSend response and idempotency documentation.
- [ ] Decide the resilience ownership model.
- [ ] Decide the result and `422` contracts.
- [ ] Decide whether `net10.0` remains the only target.
- [ ] Record approved breaks and preserved members in an ADR or design document.
- [ ] Obtain independent design review before implementation.

Output: frozen target contract and explicit compatibility budget.

### S1 — Public contract implementation

- [ ] Implement package-owned options and DI overloads.
- [ ] Remove Polly types from public signatures.
- [ ] Implement the approved result/outcome contract.
- [ ] Implement the minimal exception model.
- [ ] Correct public nullability and collection invariants.
- [ ] Preserve request builders that remain valid.
- [ ] Internalize approved accidental public helpers.
- [ ] Add compile-time public API fixtures.

Output: complete target API without transport behavior regressions.

### S2 — Transport and resilience

- [ ] Implement the approved resilience ownership model.
- [ ] Keep email `POST` single-attempt by default.
- [ ] Implement safe-operation retry behavior if approved.
- [ ] Preserve `Retry-After`, cancellation, disposal, and response-body diagnostics.
- [ ] Preserve correct `202`, `SOME_SUPPRESSED`, and `ALL_SUPPRESSED` handling.
- [ ] Implement the approved `422` behavior.
- [ ] Preserve bulk email and bulk status capabilities.
- [ ] Add deterministic HTTP regression tests.

Output: safe and explicit transport behavior.

### S3 — Migration and package quality

- [ ] Write the `0.3.0` → `1.0.0` migration guide.
- [ ] Update README examples exclusively to the target API.
- [ ] Add API difference documentation.
- [ ] Update changelog and release notes.
- [ ] Validate metadata, symbols, repository commit, README, license, and dependencies.
- [ ] Run dependency license, provenance, deprecation, and vulnerability checks.
- [ ] Build and inspect the prerelease `.nupkg`.
- [ ] Compile the same consumer fixtures against `0.3.0` and the prerelease.

Output: reviewable alpha package and migration documentation.

### S4 — Prerelease consumer validation

- [ ] Publish alpha only after human approval.
- [ ] Migrate minimal and known consumers on isolated branches.
- [ ] Record migration friction and missing equivalents.
- [ ] Correct the contract before beta.
- [ ] Publish beta only after human approval.
- [ ] Freeze the API before release candidate.
- [ ] Compile all available known consumers against the release candidate.
- [ ] Complete independent compatibility and package review.

Output: release candidate supported by real consumer evidence.

### S5 — Stable release

- [ ] Confirm stable API matches the accepted release candidate.
- [ ] Run restore, release build, tests, format, package validation, and package inspection.
- [ ] Confirm zero unexplained compatibility diagnostics.
- [ ] Confirm zero reported vulnerable or deprecated runtime dependencies.
- [ ] Create the stable release Pull Request.
- [ ] Wait for green CI and human approval.
- [ ] Merge without automatic package publication unless explicitly approved.
- [ ] Publish `1.0.0` and verify it from a clean NuGet.org consumer.
- [ ] Create and verify GitHub Release `v1.0.0`.

Output: verified stable release.

## Required tests

### Public API

- every supported registration overload;
- every `IMailerSendEmailClient` method;
- direct client construction only if intentionally supported;
- request builders retained in `1.0.0`;
- target result and exception contracts;
- nullable annotations through compiler fixtures;
- API compatibility report against `0.3.0`;
- no public Polly type references.

### HTTP behavior

- queued `202` with message ID;
- queued-with-warnings `202`;
- all-suppressed `202` without message ID;
- empty `202`;
- malformed success body;
- structured `422`;
- `401`, `403`, `404`, `408`, `429`, and `5xx`;
- `Retry-After` delta and date forms;
- caller cancellation;
- response and request disposal;
- no implicit email `POST` retry;
- safe-operation retry only when configured and approved;
- bulk send and bulk status.

### Package

- restore only from declared sources;
- `dotnet pack` with SDK Package Validation;
- inspect `.nuspec` and assembly metadata;
- verify README, changelog, license, repository URL, and commit;
- verify dependency licenses and absence of deprecated/vulnerable packages;
- install from a local prerelease source into a clean consumer;
- install the final published package from NuGet.org into a clean consumer.

## Acceptance criteria

- [ ] Every breaking change has an approved rationale and migration entry.
- [ ] No accidental public break remains unexplained.
- [ ] No Polly type appears in the public API.
- [ ] Email `POST` is not retried implicitly.
- [ ] Resilience behavior is provider-independent and documented.
- [ ] Result outcomes are explicit and internally consistent.
- [ ] `422` behavior is explicit and documented.
- [ ] Public nullability matches runtime behavior.
- [ ] Public collections are non-null and read-only where they represent responses.
- [ ] Cancellation propagates unchanged.
- [ ] Existing single, bulk, and bulk-status capabilities have equivalents.
- [ ] Package Validation and the API difference report are clean or narrowly documented.
- [ ] Runtime dependencies have acceptable license, provenance, maintenance, and security status.
- [ ] Release build, tests, format, and package inspection pass without warnings.
- [ ] All available known consumers compile against the release candidate.
- [ ] Stable `1.0.0` matches the approved release-candidate API.
- [ ] Merge, prerelease publication, and stable publication remain human-approved actions.

## Out of scope

- Adding unrelated MailerSend endpoints.
- Renaming the package.
- Rewriting request DTOs without demonstrated defects.
- Migrating from Newtonsoft.Json solely for preference.
- Adding target frameworks without an approved support policy.
- Adding a broad exception hierarchy.
- Adding automatic email `POST` retries without documented provider idempotency.
- Migrating consumer repositories in the SDK Pull Request.
- Production deployment or configuration changes.

## Stop conditions

Stop the affected slice and request a decision if:

- the target contract removes a capability without an equivalent or documented removal;
- MailerSend idempotency or suppression semantics remain ambiguous;
- a dependency has unclear license, provenance, maintenance, or commercial compatibility;
- a proposed target framework changes the supported consumer set;
- a compatibility diagnostic cannot be explained and classified;
- a known consumer cannot migrate without unrelated application redesign;
- a prerelease reveals material contract ambiguity;
- release-candidate API changes become necessary;
- validation requires real credentials, production traffic, or unapproved publication.

## Definition of done

`1.0.0` is ready for stable publication approval when its public API is frozen, every intentional
break from `0.3.0` is documented, no Polly types leak through public signatures, unsafe implicit
email retries are removed, response and exception semantics are explicit, available known
consumers compile against the release candidate, and the final package passes independent review
and clean-consumer validation.
