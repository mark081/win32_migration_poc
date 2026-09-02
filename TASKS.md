---
title: Service-Owned Client Business Rules Implementation Plan
document_id: TLM-CONNECTED-TASK-001
version: 0.1
status: approved
created_at: 2026-08-29
updated_at: 2026-08-29
requirements: TLM-CONNECTED-REQ-001
design: TLM-CONNECTED-DES-001
reviewers: []
approvals: []
approved_at: 2026-08-29
---

# Implementation Plan: Service-Owned Client Business Rules

## Overview

Implement the approved requirements and design as a gated Connected checkout-rule migration in the existing Win32 client, .NET Framework AppServer, and PostgreSQL-backed workflow. New runtime behavior remains disabled unless the service safely evaluates `connected.enabled=true` and the checkout child mode permits it. PostgreSQL remains the only authoritative workflow writer and locked transactional rule owner.

The plan implements code and testability for Legacy, compare, and service modes; it does not enable compare/service in a real deployment, retire NativeRules, or approve service-mode promotion. Those remain explicit human governance decisions.

## Execution Contract

- Execute tasks only after `TASKS.md` receives explicit approval.
- The primary agent owns authoritative task status, integration, reconciliation, and final verification. Subagents may implement independent tasks only when the execution skill permits and must not edit `TASKS.md` status directly.
- Before each wave, verify `connected` branch and inspect the dirty worktree. Preserve the user-authored `AGENTS.md` and all unrelated changes.
- A task may be checked complete only after its acceptance criteria and named verification have evidence. Record commands, results, skips, and exact reasons beneath the task.
- Begin with a failing/characterization test where practical. Do not weaken Legacy assertions or claim an unrun test passed.
- All runtime code defaults to Legacy: absent, false, invalid, expired, unreachable, or timed-out parent evaluation is false; child state cannot bypass it.
- Never add real secrets, production endpoints, credentials, customer data, generated build output, local logs, or machine-specific configuration.
- Stop execution on a failed required verification, a material requirements/design change, a permission boundary, a data/rule ownership conflict, or a human governance gate.
- Tasks within the same wave are limited to separate primary file areas. The primary agent reconciles shared project/build files centrally after parallel work.
- Refresh Graphify and reconcile `.sdd/knowledge` only after implementation changes are integrated, following the brownfield update contract.

## Tasks

### 1. Baseline and characterization

- [x] 1.1 Capture the pre-change Release and rule-behavior baseline
  - Confirm branch/revision and run the clean Release build plus applicable native, database, API, and UI suites before changing runtime behavior. Add characterization assertions only where the approved service decision table lacks an observable Legacy baseline, including supported tiers, limit boundaries, active/overdue states, and due-date boundaries.
  - Acceptance: baseline commit `b9a5125` expectations and current revision results are recorded; existing failures/skips are explicitly dispositioned; no existing test is weakened; UI tests are run only in an interactive unlocked session.
  - Verification: `scripts/Build.ps1 -Configuration Release`, `scripts/Run-Tests.ps1 -Configuration Release`, and `scripts/Run-UiTests.ps1 -Configuration Release` when the session is valid; inspect scenario coverage for `LOAN-001` through `LOAN-004` and `UI-001`.
  - _Requirements: REQ-RULE-001, REQ-RULE-006, REQ-UI-001, REQ-TEST-001, REQ-TEST-002_
  - Completion evidence: prerequisites passed on Windows Server 2019; Release rebuild passed with 6 existing warnings and 0 errors; after `Reset-Demo.ps1 -Force` corrected contaminated local data, `Run-Tests.ps1 -Configuration Release` passed native, database, and API suites including `LOAN-001`–`LOAN-004`. Expanded `tests/NativeRulesTests/main.cpp` across all tier limit/duration and eligibility boundaries; clang-format 19 dry-run and `NativeRulesTests.exe` passed. FlaUI/`UI-001` was not run because Session 0 is non-interactive and not a valid unlocked RDP session. Repository-wide formatting remains red on pre-existing unrelated files; changed test formatting passes. Known dependency warning: `NU1904` for `System.Text.Encodings.Web` 4.6.0. `graphify update .` rebuilt 379 nodes/578 edges; OKF operations coverage revised, manifest `cdec585793918f2fcb353b631b7d61f27993af00d37a19ba1c40a5d0a2081a85`, strict bundle validation passed.

### 2. Independent foundations

- [x] 2.1 Implement the fail-closed feature snapshot and evaluator foundation
  - Add AppServer feature models, `IConnectedFeatureSnapshotSource`, JSON-file source, immutable cache, evaluation context, targeting, refresh/expiry rules, stable reasons, and service-authoritative parent/child evaluation. Add a synthetic disabled example snapshot and external configuration keys without secrets.
  - Acceptance: parent dominance holds for the complete failure/targeting truth table; invalid child state yields Legacy; no external source call occurs per operation; snapshot replacement is atomic; defaults activate no Connected behavior.
  - Verification: focused AppServer unit/property tests for false/missing/malformed/expired/source-error/timeout/target-miss states, bounds validation, call counts, and child-enabled/parent-disabled cases.
  - _Requirements: REQ-FLAG-001, REQ-FLAG-002, REQ-FLAG-003, REQ-FLAG-004, REQ-FLAG-005, REQ-FLAG-006, REQ-FLAG-007, REQ-SEC-001, REQ-TEST-003_
  - Completion evidence (2026-08-29): added the provider-neutral JSON snapshot source, immutable cached evaluator, bounded external configuration, stable fail-closed reasons, server-owned targeting context, disabled synthetic example, and `AppServer.FeatureTests` integration. The 13-case focused suite passed, including missing/false/malformed/expired/future/max-age/source-error/timeout-like/target-miss/refresh-call-count/parent-dominance paths; the complete Release non-UI gate passed after resetting the synthetic demo database and running the API. Defaults retain an empty snapshot path and activate no Connected workflow behavior.

- [x] 2.2 Extend NativeRules with a structured, ABI-compatible eligibility reason
  - Add the versioned native reason enum/export, make the existing boolean eligibility export delegate to the structured result, and extend native tests for every reason and boundary without removing current exports or assertions.
  - Acceptance: existing callers remain binary/source compatible; boolean outcomes are unchanged; structured results distinguish allowed, inactive, overdue, limit, and unsupported-tier cases.
  - Verification: build x86 Debug/Release NativeRules and run `NativeRulesTests.exe`; compare all legacy boolean outcomes with structured results.
  - _Requirements: REQ-RULE-001, REQ-FLAG-008, REQ-TEST-001, REQ-TEST-002, REQ-REL-002_
  - Completion evidence (2026-08-29): added the stable `CheckoutEligibilityReasonCode` values and versioned stdcall `CheckoutEligibilityReasonV1` export; the legacy boolean export now delegates to it. Debug and Release x86 builds/tests passed, every reason and supported-tier boundary is covered, boolean/structured outcomes agree, and `dumpbin` retained the three legacy exports while adding `_CheckoutEligibilityReasonV1@16`.

- [x] 2.3 Implement non-authoritative Connected telemetry contracts
  - Add `IConnectedTelemetrySink`, flag-evaluation/comparison record models, JSON diagnostic sink, bounded in-memory counters/durations, hashing/redaction helpers, and an in-memory test sink. Keep telemetry failure isolated from commands and database state.
  - Acceptance: required flag/comparison/correlation/version fields are emitted; secrets, authorization headers, names, raw bodies, and raw cohort attributes are absent; sink failure cannot fail or retry a business operation.
  - Verification: telemetry schema, redaction, counter-bound, duration, and failing-sink unit tests plus repository secret/log-content inspection.
  - _Requirements: REQ-SEC-002, REQ-OBS-001, REQ-OBS-002, REQ-OBS-003, REQ-OBS-004_
  - Completion evidence (2026-08-31): added explicit flag/comparison records, SHA-256 cohort/input hashing, token redaction, JSON-lines diagnostics, 128-series bounded counters and duration summaries, a one-attempt exception-isolating wrapper, and an in-memory test sink without a new dependency or database state. Release build passed with 6 existing warnings and 0 errors; the 18-case feature/telemetry executable passed schema, sensitive-content exclusion, bounds, duration, in-memory, and failing-sink checks. After resetting contaminated synthetic data and starting the built API temporarily, `Run-Tests.ps1 -Configuration Release` passed native, feature/telemetry, database, and API suites. CSharpier and `git diff --check` passed; the telemetry source scan found no prohibited sensitive field names. FlaUI was not run because commands execute in disconnected Session 0/services rather than an interactive unlocked desktop; no client/UI code changed. `graphify update .` rebuilt 621 nodes/1,083 edges; affected OKF concepts were reconciled, manifest `4ed98567a0c7e21fcdfc11ab848937099febed3f0c1aef7b474f63b4b6a62fbe` was written, and strict OKF v0.2 validation passed.

### 3. Service rule and additive API contracts

- [x] 3.1 Implement the side-effect-free checkout rule evaluator and repository context read
  - Add the clock abstraction, `MemberEligibilityContext` query, stable decision reasons, limit/duration facts, and `ICheckoutRuleEvaluator` using the approved ordering and explicit business date. Do not modify the checkout command or PostgreSQL ownership.
  - Acceptance: every NativeRules case and due-date boundary has an equivalent service result; decision evaluation performs reads only; PostgreSQL tier functions remain the policy input and `checkout_tool` remains authoritative.
  - Verification: service unit decision table, deterministic-clock tests, query tests, and database before/after assertions covering workflow, idempotency, and audit tables.
  - _Requirements: REQ-RULE-001, REQ-RULE-003, REQ-RULE-004, REQ-RULE-005, REQ-RULE-006, REQ-TEST-002_
  - Completion evidence (2026-09-01): added the pure service checkout evaluator, explicit UTC business-date clock, stable reason/fact result, and one parameterized repository read using the existing PostgreSQL tier functions plus open/overdue-loan facts. No route, checkout command, or PostgreSQL routine changed. Release build passed with 6 existing warnings and 0 errors; all 22 focused feature/telemetry/rule tests passed, including every supported tier/count and due-date boundary, reason precedence, fixed clock, real repository values, UTC/database date equality, and full before/after fingerprints of workflow, idempotency, and audit tables. After the standard synthetic reset and temporary built API startup, `Run-Tests.ps1 -Configuration Release` passed native, focused service, database, and API suites. CSharpier and `git diff --check` passed. FlaUI was not run because no desktop/UI code or behavior changed. `graphify update .` rebuilt 662 nodes/1,171 edges; runtime, data-ownership, operations, and risk concepts were reconciled; manifest `d9bb18609081dae2a8165da5fe0b78d64f1e4f7b55af1c0d791209fd9bfa0d69` was written; strict OKF v0.2 validation passed.

- [x] 3.2 Add the authenticated versioned capabilities API
  - Add `GET /api/v1/capabilities`, bounded `X-Client-Version` parsing, capability response schema/version/freshness/correlation fields, authentication coverage, and evaluation telemetry. Preserve all existing routes and old-client behavior.
  - Acceptance: the endpoint returns only non-sensitive routing metadata; service evaluation cannot be overridden by request data; unsupported/missing client versions safely narrow to Legacy; existing clients need not call it.
  - Verification: API contract tests for authentication, schema, freshness, targeting, safe reasons, version skew, tampered inputs, and all parent/child states.
  - _Requirements: REQ-FLAG-004, REQ-FLAG-006, REQ-API-002, REQ-SEC-001, REQ-OBS-001, REQ-OBS-002, REQ-COMP-001_
  - Completion evidence (2026-09-02): added authenticated `GET /api/v1/capabilities` as a thin adapter over the existing cached service evaluator. The schema exposes only version/freshness/effective-mode/reason/correlation metadata; valid correlation UUIDs are preserved, unsafe values are replaced, and missing, malformed, or overlong client versions force disabled/Legacy without allowing request data to elevate service state. Each request emits safe parent and child evaluation records through the existing failure-isolating telemetry sink. No checkout route, repository command, PostgreSQL routine, or data owner changed. Release build passed with the same 6 existing warnings and 0 errors; all 24 focused feature/telemetry/rule/capability tests passed. After resetting contaminated synthetic data and running the freshly built API, `Run-Tests.ps1 -Configuration Release` passed native, focused service, database, and API suites, including capability authentication, schema/freshness, correlation, non-sensitive payload, version fallback, and every effective parent/child mode. CSharpier and `git diff --check` passed. FlaUI was not run because no desktop/UI behavior changed. `graphify update .` rebuilt 691 nodes/1,233 edges; API, runtime, gate, operations, system, and risk concepts were revised while data ownership, constraints, glossary, and open questions were verified unchanged; manifest `93092c2d642772bd4df19a24befe6d8185a664e40c7abd21a873a0fadfbc2925` was written; strict OKF v0.2 validation passed.

- [x] 3.3 Add the authenticated side-effect-free checkout-decision API
  - Add `POST /api/v1/checkout-decisions`, explicit request/response models, model validation, server-side capability re-evaluation, stable allow/deny/error contracts, optional Legacy observation handling, and comparison telemetry integration.
  - Acceptance: the endpoint returns versioned decisions without an idempotency key; no workflow/idempotency/business-audit write occurs; stale capability and unavailable dependencies have stable safe errors; client observation never authorizes a command.
  - Verification: contract and integration tests for every decision/error, stale/tampered capability, missing observation, match/mismatch, telemetry failure, and database/audit/idempotency state equality.
  - _Requirements: REQ-RULE-003, REQ-RULE-005, REQ-FLAG-004, REQ-FLAG-008, REQ-FLAG-009, REQ-API-002, REQ-UI-002, REQ-NET-004, REQ-OBS-003_
  - Completion evidence (2026-09-02): added authenticated `POST /api/v1/checkout-decisions` with explicit versioned models, server-side capability re-evaluation, stable validation/stale/unavailable/unexpected errors, optional Legacy observation handling, and failure-isolated comparison telemetry. Service mode returns the service evaluation; compare mode returns the supplied Legacy result while recording normalized match, mismatch, and read-failure outcomes. The endpoint accepts no idempotency key and performs no workflow, idempotency, or business-audit write. Focused tests cover service and compare outcomes, match/mismatch/read failure, stale capability, missing observation, telemetry failure, and real-database state equality. The full Release gate passed all native/transport, 32 feature, database, and API integration tests after reseeding the synthetic fixture.

### 4. Client transport and capability routing

- [x] 4.1 Refactor WinHTTP into externally configured Legacy and Connected endpoint transport
  - Introduce endpoint parsing/configuration, preserve the Legacy localhost default, require HTTPS for Connected, retain normal certificate-chain/hostname validation, set explicit bounded timeouts, classify failures, and preserve one idempotency key across ambiguous-write replay. Do not activate remote routing without a valid capability.
  - Acceptance: no production endpoint/secret is compiled; invalid HTTPS/certificate/hostname/configuration fails closed; unavailable/timeout/auth/validation/conflict/unexpected categories are distinguishable; the UI thread never retries indefinitely.
  - Verification: C++ transport/configuration tests or a focused harness plus integration tests for TLS trust, hostname mismatch, timeout, unavailable service, credential failure, interrupted request, and same-key replay.
  - _Requirements: REQ-NET-001, REQ-NET-002, REQ-NET-003, REQ-NET-004, REQ-SEC-002, REQ-UI-003_
  - Completion evidence (2026-09-02): extracted the desktop WinHTTP work into one RAII transport with separately configured Legacy and Connected endpoints. Legacy retains its localhost default; Connected requires an absolute HTTPS URL plus a readable non-empty external credential file, rejects embedded URL credentials, retains WinHTTP certificate-chain and hostname checks, and uses bounded resolve/connect/send/receive timeouts. Failures are categorized for configuration, timeout, unavailable, authentication, authorization, validation, conflict, and unexpected outcomes. Ambiguous keyed writes receive at most one jittered replay with the exact same idempotency key; current product calls remain routed to Legacy until task 4.2 supplies a valid capability. The focused native harness passed configuration, status mapping, unavailable-service, trusted-TLS, hostname-mismatch, and same-key replay checks. Release build passed with the existing 6 warnings and 0 errors; the full non-UI gate passed. FlaUI was not run because this wave changes no desktop interaction or visible routing behavior.

- [ ] 4.2 Implement the client capability cache and endpoint router
  - Add supported-schema parsing, expiry/configuration-version checks, capability bootstrap through the Connected endpoint, in-memory caching, and routing that selects Legacy for every absent/stale/unsupported/tampered/failure case. Treat service capability as routing only.
  - Acceptance: only a current service response can select compare/service; client state cannot elevate service mode; rollback converges within refresh plus capability lifetime; old/unconfigured clients preserve Legacy behavior.
  - Verification: client router truth-table tests or harness covering all modes, expiry, version skew, malformed responses, parent-off/child-on, network/auth failures, and rollback timing.
  - _Requirements: REQ-FLAG-001, REQ-FLAG-002, REQ-FLAG-004, REQ-FLAG-006, REQ-FLAG-010, REQ-API-002, REQ-COMP-001_

### 5. Checkout mode integration

- [ ] 5.1 Integrate Legacy and compare checkout client flows
  - Preserve current input/confirmation behavior; wrap NativeRules in a structured adapter; in compare mode send one native observation to the decision API, use the returned Legacy-effective decision, display safe feedback, and issue at most one existing checkout command with one idempotency key.
  - Acceptance: disabled/Legacy behavior matches baseline; compare match/mismatch/service-error paths retain the Legacy operator result; comparison never produces a second business write; service/telemetry failure cannot report success.
  - Verification: instrumented native-call tests, compare contract/UI tests, database loan/audit/idempotency counts, mismatch/error injection, confirmation/cancellation, and stable message mapping.
  - _Requirements: REQ-FLAG-008, REQ-UI-001, REQ-UI-002, REQ-UI-003, REQ-API-001, REQ-OBS-003, REQ-TEST-003_

- [ ] 5.2 Integrate the service-mode checkout client flow
  - In service mode, call the decision API without NativeRules or client-derived eligibility, display stable service reasons, retain confirmation, and submit the unchanged idempotent checkout command only after allow. Handle database rejection after a stale positive decision as the existing conflict contract.
  - Acceptance: NativeRules decision-call count is zero in service mode; member DTO policy fields do not decide eligibility; decision allow is never shown as checkout success; PostgreSQL retains the final outcome.
  - Verification: instrumented service-mode UI/API tests for allow/deny, every stable reason, race-to-conflict, interrupted write, same-key replay, and no-success-before-commit.
  - _Requirements: REQ-RULE-002, REQ-RULE-004, REQ-FLAG-009, REQ-API-001, REQ-UI-002, REQ-UI-003, REQ-NET-003_

### 6. Cross-boundary verification and operations

- [ ] 6.1 Expand service, API, and database migration-mode integration coverage
  - Build a deterministic flag/snapshot fixture matrix and execute disabled, Legacy, compare, service, child-on/parent-off, invalid/expired/source-failure, stale capability, comparison mismatch, telemetry failure, concurrency, and database revalidation scenarios.
  - Acceptance: each case records expected effective mode, write count, normalized result, database/audit state, and telemetry reason; compare performs exactly one authoritative write; existing `/api/v1` tests remain compatible.
  - Verification: updated `tests/Integration/ApiTests.ps1`, database tests, focused service tests, and normalized evidence artifacts excluded from source control where generated.
  - _Requirements: REQ-RULE-003, REQ-RULE-004, REQ-FLAG-001, REQ-FLAG-002, REQ-FLAG-003, REQ-FLAG-007, REQ-FLAG-008, REQ-FLAG-009, REQ-API-001, REQ-SEC-001, REQ-TEST-002, REQ-TEST-003_

- [ ] 6.2 Expand Win32/FlaUI behavior and failure coverage
  - Characterize and test preserved input validation, confirmation/cancellation, accessibility, Legacy credential visibility, service reason messages, and distinct timeout/unavailable/authentication/conflict/unexpected outcomes without corrupted state.
  - Acceptance: `UI-001` and administration/return validation remain; affected eligibility feedback is actionable; service mode makes no native decision; no UI path reports false success.
  - Verification: `scripts/Run-UiTests.ps1 -Configuration Release` in an interactive unlocked session plus any non-UI client harness; report exact skips if the desktop session is invalid.
  - _Requirements: REQ-RULE-002, REQ-UI-001, REQ-UI-002, REQ-UI-003, REQ-NET-004, REQ-TEST-001, REQ-TEST-002_

- [ ] 6.3 Document configuration, deployment, observability, rollback, and flag lifecycle
  - Update README/architecture/testing/deployment documentation with the Connected objective/non-goals, retained Legacy dependencies, before/after ownership, endpoint/TLS/credential configuration, snapshot schema and atomic update, safe defaults, cohort context, refresh/expiry, telemetry fields/redaction, compare evidence, rollback exercise, supported version matrix, and child-flag removal condition.
  - Acceptance: docs identify `connected.enabled` owner, default false, child semantics, maximum convergence time, security controls, no-schema-change impact, operational burden, and exact local/Connected verification commands; examples contain only synthetic disabled values.
  - Verification: documentation review, internal link/config-example validation, secret scan, and consistency check against `AGENTS.md`, REQUIREMENTS, DESIGN, and implemented behavior.
  - _Requirements: REQ-FLAG-005, REQ-FLAG-006, REQ-FLAG-007, REQ-FLAG-010, REQ-NET-001, REQ-NET-002, REQ-SEC-002, REQ-OBS-001, REQ-OBS-002, REQ-OBS-003, REQ-OBS-004, REQ-COMP-001, REQ-REL-002_

### 7. Connected gate and reconciliation

- [ ] 7.1 Run local/Connected parity, TLS, failure, latency, and rollback evidence
  - Provision only synthetic test configuration, run the same contract/scenario matrix against Legacy-local and Connected HTTPS endpoints, simulate provider/network/write interruption, measure representative decision/write latency by mode, and rehearse child-to-Legacy and parent-off rollback without restart or redeployment.
  - Acceptance: normalized API outcomes, PostgreSQL state, and audit records are equivalent; certificate/hostname/authentication/timeouts/unavailability/idempotent replay pass; rollback converges within the documented bound; every skip has a governed disposition.
  - Verification: retained command/test report with source revision, fixture/configuration versions, modes, latency summary, correlation IDs for failures, and per-suite pass/fail/skip counts.
  - _Requirements: REQ-NET-001, REQ-NET-002, REQ-NET-003, REQ-NET-004, REQ-OBS-004, REQ-COMP-001, REQ-TEST-002, REQ-TEST-003, REQ-REL-001_

- [ ] 7.2 Run the clean Release Connected implementation gate
  - Inspect the worktree for only intentional source/spec/knowledge changes; run the supported Release build and all applicable native, database, API, contract, flag/fault, and UI suites; scan for secrets, generated output, local logs/databases, and machine-specific configuration.
  - Acceptance: all mandatory automated checks pass; no Legacy test is weakened; UI execution environment is valid or its skip explicitly blocks promotion; no prohibited artifact is included.
  - Verification: `scripts/Setup-Prerequisites.ps1`, `scripts/Build.ps1 -Configuration Release`, `scripts/Run-Tests.ps1 -Configuration Release`, `scripts/Run-UiTests.ps1 -Configuration Release` when valid, formatting/static checks, secret scan, and `git status --short --branch`.
  - _Requirements: REQ-API-001, REQ-SEC-001, REQ-SEC-002, REQ-TEST-001, REQ-TEST-002, REQ-TEST-003, REQ-REL-001_

- [ ] 7.3 Refresh Graphify and reconcile the brownfield knowledge bundle
  - After all code/docs are stable, follow `$analyze-brownfield-context` update mode: refresh Graphify, diff the prior source manifest, map every changed path to concepts, record explicit dispositions, update durable knowledge, write the new manifest, and validate strict OKF v0.2.
  - Acceptance: every potentially affected concept is unchanged-after-verification, revised, or marked stale/conflicted with reason; rule ownership, flag evaluation, contracts, failure behavior, tests, and operational burden match implementation; no generated secrets or customer data appear.
  - Verification: Graphify refresh evidence, manifest pre-diff, concept dispositions, new fingerprint, strict bundle validator, and final source inspection for material claims.
  - _Requirements: REQ-RULE-002, REQ-RULE-004, REQ-FLAG-004, REQ-API-001, REQ-REL-002_

- [ ] 7.4 Prepare the human promotion and rollback handoff
  - Summarize completed implementation tasks, verification, mode/configuration versions, comparison match/mismatch evidence, latency, security/redaction checks, rollback rehearsal, residual risks, and skipped tests. Keep all runtime examples disabled/Legacy and request a separate human decision before any compare or service rollout.
  - Acceptance: deployment alone leaves Connected behavior inactive; the handoff states that compare/service promotion and NativeRules/child-flag retirement are not approved by task completion; the agreed observation dataset/window remains an explicit governance input.
  - Verification: review the handoff against requirements/design release gates and demonstrate `connected.enabled=false`, child-on/parent-off, and provider-failure Legacy outcomes.
  - _Requirements: REQ-FLAG-010, REQ-OBS-002, REQ-OBS-003, REQ-REL-001, REQ-REL-002_

## Notes

- Every listed task is required for implementation completion; there are no optional product-code tasks.
- Task 7.4 completes an implementation handoff, not rollout approval. Compare/service activation requires a separate explicit governance decision based on Task 7.1 evidence.
- `REQ-REL-002` is satisfied here by retaining and documenting the retirement gate. Actual removal of NativeRules, the child flag, or the Legacy path is intentionally outside this plan.
- Generated test reports, Graphify caches not intended by repository convention, build artifacts, restored packages, logs, local snapshots, certificates, and machine configuration must not be committed.
- If implementation changes a requirement behavior, architecture boundary, provider strategy, data authority, public contract, or wave dependency, stop and return the affected approved artifact to draft.

## Task Dependency Graph

```json
{
  "waves": [
    {
      "id": 0,
      "tasks": ["1.1"]
    },
    {
      "id": 1,
      "tasks": ["2.1", "2.2"]
    },
    {
      "id": 2,
      "tasks": ["2.3"]
    },
    {
      "id": 3,
      "tasks": ["3.1"]
    },
    {
      "id": 4,
      "tasks": ["3.2"]
    },
    {
      "id": 5,
      "tasks": ["3.3", "4.1"]
    },
    {
      "id": 6,
      "tasks": ["4.2"]
    },
    {
      "id": 7,
      "tasks": ["5.1"]
    },
    {
      "id": 8,
      "tasks": ["5.2"]
    },
    {
      "id": 9,
      "tasks": ["6.1", "6.2", "6.3"]
    },
    {
      "id": 10,
      "tasks": ["7.1"]
    },
    {
      "id": 11,
      "tasks": ["7.2"]
    },
    {
      "id": 12,
      "tasks": ["7.3"]
    },
    {
      "id": 13,
      "tasks": ["7.4"]
    }
  ]
}
```
