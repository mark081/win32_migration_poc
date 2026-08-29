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
  - resource: repo://docs/testing-evolution.md#L47-L256
  - resource: repo://README.md#L274-L359
  - resource: repo://AGENTS.md#L205-L215
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T23:23:48.5187993+00:00
status: draft
source_revision: b8c67274c7ff3579be20e4811fbd93f2d0c5e698
source_fingerprint: cdec585793918f2fcb353b631b7d61f27993af00d37a19ba1c40a5d0a2081a85
source_worktree: dirty
curation_status: generated
---

# Summary

The supported build is Windows/x86 using NuGet restore and MSBuild for the legacy solution, plus `dotnet` for the FlaUI project ([Build.ps1:17](../../../scripts/Build.ps1#L17), [Build.ps1:25](../../../scripts/Build.ps1#L25), [Build.ps1:32](../../../scripts/Build.ps1#L32)).

## Verification layers

- `Run-Tests.ps1` executes the NativeRules executable, PostgreSQL routine tests, then API integration tests ([Run-Tests.ps1:2](../../../scripts/Run-Tests.ps1#L2), [Run-Tests.ps1:4](../../../scripts/Run-Tests.ps1#L4), [Run-Tests.ps1:6](../../../scripts/Run-Tests.ps1#L6)).
- FlaUI tests require an interactive, unlocked desktop session ([Run-UiTests.ps1:6](../../../scripts/Run-UiTests.ps1#L6)).
- Native rule tests remain regression evidence until equivalent service-level tests demonstrate the same decision table ([testing-evolution.md:47](../../../docs/testing-evolution.md#L47), [testing-evolution.md:60](../../../docs/testing-evolution.md#L60)).
- The native suite characterizes all supported and unknown tier limits/durations plus eligibility below, at, and above tier limits, overdue, inactive, and unsupported-tier outcomes ([main.cpp:12](../../../tests/NativeRulesTests/main.cpp#L12)).
- UI tests should protect wiring and visible behavior; domain permutations belong primarily in native, service, or API tests ([testing-evolution.md:115](../../../docs/testing-evolution.md#L115)).

## Change-sensitive scenarios

Moving client decisions affects at least `LOAN-001`, `LOAN-002`, `LOAN-003`, and `UI-001` in the cross-stage scenario matrix ([testing-evolution.md:246](../../../docs/testing-evolution.md#L246), [testing-evolution.md:256](../../../docs/testing-evolution.md#L256)). Connected promotion additionally requires local/remote parity, TLS, timeout/retry, fail-closed credential, UI failure, and latency evidence ([testing-evolution.md:178](../../../docs/testing-evolution.md#L178)).

Flag-specific evidence must cover parent disabled, targeted parent enabled, child enabled while parent disabled, provider missing/invalid/expired/unreachable/timeout fallback, observable reason/version/cohort metadata, and compare mode performing exactly one authoritative write ([AGENTS.md:210](../../../AGENTS.md#L210)).
