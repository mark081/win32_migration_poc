---
type: Operations
title: Build, test, and deployment mechanisms
description: Supported build, regression suites, service operation, and environment constraints.
resource: repo://scripts/
tags: [build, test, deployment, windows, x86]
sources:
  - resource: repo://scripts/Build.ps1#L1-L47
  - resource: repo://scripts/Run-Tests.ps1#L1-L6
  - resource: repo://scripts/Run-UiTests.ps1#L1-L27
  - resource: repo://tests/NativeRulesTests/main.cpp#L12-L43
  - resource: repo://tests/AppServer.FeatureTests/Program.cs#L21-L252
  - resource: repo://tests/AppServer.FeatureTests/Program.cs#L267-L392
  - resource: repo://tests/AppServer.FeatureTests/Program.cs#L420-L758
  - resource: repo://tests/AppServer.FeatureTests/Program.cs#L775-L866
  - resource: repo://tests/Integration/ApiTests.ps1#L76-L108
  - resource: repo://tests/NativeRulesTests/main.cpp#L38-L107
  - resource: repo://tests/NativeRulesTests/main.cpp#L158-L203
  - resource: repo://tests/NativeRulesTests/main.cpp#L205-L235
  - resource: repo://tests/DesktopClient.UiTests/DesktopClientTests.cs#L166-L185
  - resource: repo://tests/AppServer.FeatureTests/Program.cs#L860-L1030
  - resource: repo://docs/testing-evolution.md#L47-L256
  - resource: repo://README.md#L274-L359
  - resource: repo://AGENTS.md#L205-L215
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-03T22:04:44+00:00
status: draft
source_revision: c02893fb2fbaf460282c8d6fa4da3ef6f4b5c164
source_fingerprint: 5c9a61f0a37d9c8b8be61b6ae0208c38d7bb4bfc132744a2d45b8f448229e28e
source_worktree: dirty
curation_status: generated
---

# Summary

The supported build is Windows/x86 using NuGet restore and MSBuild for the legacy solution, plus `dotnet` for the FlaUI project ([Build.ps1:17](../../../scripts/Build.ps1#L17), [Build.ps1:25](../../../scripts/Build.ps1#L25), [Build.ps1:32](../../../scripts/Build.ps1#L32)).

## Verification layers

- `Run-Tests.ps1` executes the NativeRules and feature-evaluator executables, PostgreSQL routine tests, then API integration tests ([Run-Tests.ps1:2](../../../scripts/Run-Tests.ps1#L2), [Run-Tests.ps1:4](../../../scripts/Run-Tests.ps1#L4), [Run-Tests.ps1:6](../../../scripts/Run-Tests.ps1#L6)).
- FlaUI tests require an interactive, unlocked desktop session ([Run-UiTests.ps1:6](../../../scripts/Run-UiTests.ps1#L6)).
- Native rule tests remain regression evidence until equivalent service-level tests demonstrate the same decision table ([testing-evolution.md:47](../../../docs/testing-evolution.md#L47), [testing-evolution.md:60](../../../docs/testing-evolution.md#L60)).
- The native suite characterizes all supported and unknown tier limits/durations plus eligibility below, at, and above tier limits, overdue, inactive, and unsupported-tier outcomes, including equivalence between the legacy boolean and versioned structured reason export ([main.cpp:14](../../../tests/NativeRulesTests/main.cpp#L14)).
- The feature suite covers disabled/missing/malformed/expired/future/stale/source-error/timeout-like states, cache refresh/call counts, parent dominance, child fallback, and targeting ([Program.cs:21](../../../tests/AppServer.FeatureTests/Program.cs#L21)). It also verifies the telemetry JSON schema, hashing/redaction, bounded metric series, duration summaries, one-attempt failure isolation, and the in-memory sink ([Program.cs:267](../../../tests/AppServer.FeatureTests/Program.cs#L267)).
- The same executable now checks the service checkout reason order, all supported tier/count and due-date boundaries, a fixed UTC business date, real PostgreSQL eligibility values, and full before/after fingerprints proving the repository read changes no workflow, idempotency, or audit data ([Program.cs:420](../../../tests/AppServer.FeatureTests/Program.cs#L420), [Program.cs:654](../../../tests/AppServer.FeatureTests/Program.cs#L654)).
- Capability checks cover disabled, Legacy, compare, and service response mapping; freshness/correlation fields; parent and child telemetry; and missing, malformed, and overlong client-version fallback ([Program.cs:775](../../../tests/AppServer.FeatureTests/Program.cs#L775), [Program.cs:810](../../../tests/AppServer.FeatureTests/Program.cs#L810)). API integration verifies authentication, the disabled-by-default response, safe schema, targeting-data exclusion, correlation preservation, and version fallback ([ApiTests.ps1:76](../../../tests/Integration/ApiTests.ps1#L76)).
- Decision checks cover service and compare results, stale routing, missing observations, failed reads, comparison evidence, and a real PostgreSQL before/after fingerprint. API integration covers authentication, validation, and disabled-by-default stale routing. The native executable also serves as a focused transport harness for endpoint defaults, HTTPS-only Connected configuration, separate credential failure, timeout bounds, and unavailable-service classification ([Program.cs:860](../../../tests/AppServer.FeatureTests/Program.cs#L860), [main.cpp:38](../../../tests/NativeRulesTests/main.cpp#L38)).
- The native harness also covers absent, compare, service, expired, unsupported-schema, malformed, duplicate-field, future-dated, stale, and unconfigured capability routing. Each unsafe case selects Legacy without workflow or network activity ([main.cpp:158](../../../tests/NativeRulesTests/main.cpp#L158)).
- Compare-client checks cover every native reason mapping and verify that the decision request contains one versioned observation but no tool ID, command, or idempotency key ([main.cpp:205](../../../tests/NativeRulesTests/main.cpp#L205)).
- FlaUI covers the retained checkout confirmation and proves cancellation leaves the client output unchanged, so neither a request nor success is reported before operator approval ([DesktopClientTests.cs:166](../../../tests/DesktopClient.UiTests/DesktopClientTests.cs#L166)).
- UI tests should protect wiring and visible behavior; domain permutations belong primarily in native, service, or API tests ([testing-evolution.md:115](../../../docs/testing-evolution.md#L115)).

## Change-sensitive scenarios

Moving client decisions affects at least `LOAN-001`, `LOAN-002`, `LOAN-003`, and `UI-001` in the cross-stage scenario matrix ([testing-evolution.md:246](../../../docs/testing-evolution.md#L246), [testing-evolution.md:256](../../../docs/testing-evolution.md#L256)). Connected promotion additionally requires local/remote parity, TLS, timeout/retry, fail-closed credential, UI failure, and latency evidence ([testing-evolution.md:178](../../../docs/testing-evolution.md#L178)).

Flag-specific evidence must cover parent disabled, targeted parent enabled, child enabled while parent disabled, provider missing/invalid/expired/unreachable/timeout fallback, observable reason/version/cohort metadata, and compare mode performing exactly one authoritative write ([AGENTS.md:210](../../../AGENTS.md#L210)).
