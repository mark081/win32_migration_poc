---
title: Service-Owned Client Business Rules
document_id: TLM-CONNECTED-REQ-001
version: 0.1
status: approved
created_at: 2026-08-29
updated_at: 2026-08-29
owner: Repository maintainers
intended_users: Tool Lending operators and Connected-stage delivery teams
intended_region: POC environments only
notation: EARS
reviewers: []
approvals: []
approved_at: 2026-08-29
---

# Requirements: Service-Owned Client Business Rules

## Purpose and intended-use boundary

Define a gated Connected-stage migration that removes domain-policy decisions from the Win32 client's active service-mode path and executes them in the existing application service, without changing documented business outcomes or moving authoritative workflow data and locked transactional rules out of PostgreSQL.

This specification applies only to synthetic POC data and the existing Tool Lending Win32, .NET Framework service, and PostgreSQL system. It does not authorize production deployment, a new deployable service, or a Hybrid/SaaS data migration.

Current-system evidence comes from the approved brownfield baseline: [runtime components](.sdd/knowledge/architecture/runtime-components.md), [feature-gate policy](.sdd/knowledge/architecture/connected-feature-gating.md), [API interface](.sdd/knowledge/interfaces/api-v1.md), [data ownership](.sdd/knowledge/data/ownership.md), and [migration risks](.sdd/knowledge/hotspots-and-risks.md).

## Problem statement

The Win32 checkout path currently fetches member state, calls `NativeRules.dll`, and blocks checkout locally based on duplicated tier-limit and eligibility policy. PostgreSQL repeats and authoritatively enforces those decisions during the transaction. This duplication can drift and makes Connected evolution dependent on client-shipped rule code.

The migration must centralize the decision contract in the existing service while preserving:

- the Legacy behavior at Connected baseline commit `b9a5125` whenever the parent gate is not safely enabled;
- the public `/api/v1` business outcomes and stable error semantics;
- immediate, understandable operator feedback;
- PostgreSQL data, locking, audit, and transactional-rule authority; and
- a measured compare mode and practical rollback path.

## Goals and success measures

1. The service produces the same checkout eligibility, tier-limit, and maximum-loan-duration decisions as the characterized native rules for 100% of the approved decision-table cases.
2. In service mode, the Win32 client makes no checkout eligibility, tier-limit, or maximum-duration policy decision locally.
3. With `connected.enabled` false or unevaluable, all affected workflows produce the Legacy baseline behavior.
4. Compare mode records normalized differences for all exercised rule cases while performing exactly one authoritative business write per command.
5. Local and Connected endpoints produce equivalent normalized API results, PostgreSQL state, and audit records for the affected scenario matrix.
6. Every enabled, disabled, parent/child, provider-failure, network-failure, rollback, and version-compatibility path has repeatable automated evidence or an explicitly documented environment-bound test disposition.

## Non-goals

- Moving PostgreSQL-owned data or locked transactional rules into C# or a new data store.
- Removing required-field, numeric/date-shape, confirmation, accessibility, response-formatting, or error-presentation behavior from the Win32 UI.
- Removing the x86 client, `NativeRules.dll`, PostgreSQL routines, or Legacy tests before replacement and rollback evidence passes.
- Changing reservation, checkout, return, or late-fee business outcomes.
- Replacing the Win32 UI, introducing microservices, or beginning Hybrid/SaaS workflow ownership.
- Adding offline command processing, multi-tenancy, cloud transactional authority, or direct client database access.
- Selecting a proprietary feature-flag provider in requirements; the design must keep provider integration replaceable.
- Publishing business events or introducing an outbox unless separately scoped.

## Stakeholders and actors

| Actor | Interest or responsibility |
| --- | --- |
| Tool Lending operator | Receives immediate, actionable eligibility and failure feedback without changed business outcomes. |
| Win32 client | Collects input, requests service decisions/commands, presents results, and retains Legacy routing for rollback. |
| Application service | Authoritatively evaluates Connected flags, owns the migrated decision contract, orchestrates writes, and emits safe telemetry. |
| PostgreSQL | Remains authoritative for workflow data, locked transactional rules, audit business records, and durable idempotency. |
| Deployment/operator team | Configures targeting, observes comparisons, expands rollout, and invokes rollback. |
| Test/release reviewer | Verifies parity, failure behavior, flag safety, compatibility, and Connected promotion evidence. |

## Scope and workflow

### In-scope rule inventory

- Tier checkout limit.
- Tier maximum loan duration.
- Checkout eligibility derived from member active state, overdue-loan state, open-loan count, and tier limit.
- The client-side checkout gate that currently prevents an ineligible checkout request.

### Preserved client responsibilities

- Required-field and syntactic input checks needed to form a request.
- Confirmation/cancellation interaction.
- Accessible presentation of eligibility, validation, authentication, conflict, timeout, unavailable-service, and unexpected failures.
- Legacy/native execution when selected by the service-authoritative capability decision.

### Migration workflow

1. The service evaluates `connected.enabled` from a cached or versioned local snapshot using a safe-false fallback.
2. When the parent is true, the service evaluates `connected.checkout.rule-mode` as `legacy`, `compare`, or `service`; any invalid child value resolves to `legacy`.
3. The client consumes an authenticated, versioned service capability decision only for presentation and routing.
4. Legacy mode preserves the existing NativeRules-based gate.
5. Compare mode retains the Legacy result, also executes a side-effect-free service decision, and records a normalized comparison.
6. Service mode uses the service decision for eligibility feedback and proceeds to the existing idempotent checkout command only when permitted.
7. The existing PostgreSQL checkout routine revalidates all authoritative rules under its current transaction and locking behavior in every mode.

## EARS conventions

- **Ubiquitous:** `The system shall ...`
- **Event-driven:** `When <trigger>, the system shall ...`
- **State-driven:** `While <state>, the system shall ...`
- **Optional:** `Where <feature>, the system shall ...`
- **Unwanted behavior:** `If <condition>, then the system shall ...`

Priorities are **Must**, **Should**, and **Could**. Verification methods are automated test, inspection, analysis, or a named environment-bound test.

## Requirements

### A. Service-owned rule contract

**REQ-RULE-001 — Complete rule inventory**
- **Priority:** Must
- **Requirement:** The system shall implement service-layer decisions for tier checkout limit, maximum loan duration, and checkout eligibility using the characterized NativeRules decision table.
- **Acceptance criteria:** Every case currently asserted by `NativeRulesTests`, plus all supported tiers and boundary counts, has an equivalent service-level assertion.
- **Verification:** Automated unit and contract tests mapped to `LOAN-001`, `LOAN-002`, and `LOAN-003`.

**REQ-RULE-002 — Service-mode client boundary**
- **Priority:** Must
- **Requirement:** While effective checkout rule mode is `service`, the Win32 client shall not calculate or use tier checkout limits, maximum loan durations, overdue status, open-loan counts, or member active state to decide checkout eligibility.
- **Acceptance criteria:** Production service-mode client flow contains no NativeRules eligibility call and does not derive an allow/deny decision from member DTO policy fields.
- **Verification:** Code inspection, native-call instrumentation, and UI/API integration tests.

**REQ-RULE-003 — Side-effect-free decision**
- **Priority:** Must
- **Requirement:** When the service evaluates checkout eligibility independently of a checkout command, the system shall perform no workflow write, idempotency write, or audit business-record write as a side effect of that decision.
- **Acceptance criteria:** Database-state and audit snapshots remain unchanged after decision-only requests in all outcomes.
- **Verification:** API integration tests with normalized before/after database comparison.

**REQ-RULE-004 — Authoritative transaction revalidation**
- **Priority:** Must
- **Requirement:** When any checkout command is submitted, the system shall execute the existing PostgreSQL checkout routine so that locked transactional rules determine the committed outcome regardless of a preceding client or service decision.
- **Acceptance criteria:** Concurrency, overdue, inactive, limit, duration, availability, and reservation-owner outcomes retain current `TLxxx` behavior and transaction atomicity.
- **Verification:** Database and API integration tests, including competing checkout.

**REQ-RULE-005 — Decision response**
- **Priority:** Must
- **Requirement:** When checkout eligibility is evaluated by the service, the system shall return a versioned decision containing allowed/denied status, a stable reason code, operator-safe message data, applicable limit/duration facts needed for presentation, correlation ID, and decision-contract version.
- **Acceptance criteria:** The response contains no flag-provider values, secret targeting attributes, raw exception details, or authorization decision inferred by the client.
- **Verification:** Contract schema tests and security inspection.

**REQ-RULE-006 — Time consistency**
- **Priority:** Must
- **Requirement:** When a rule depends on the current date, the system shall use an explicit service/database date boundary and deterministic test clock inputs so local and Connected evaluations can be normalized.
- **Acceptance criteria:** Boundary-date tests do not depend on client workstation clock or timezone.
- **Verification:** Automated unit, database, and contract tests.

### B. Parent gate and migration modes

**REQ-FLAG-001 — Parent circuit breaker**
- **Priority:** Must
- **Requirement:** The system shall activate migrated checkout rule behavior only when the service evaluates `connected.enabled` as true and the selected child mode permits that behavior.
- **Acceptance criteria:** A child value cannot activate compare or service behavior while the parent is false.
- **Verification:** Truth-table unit and integration tests.

**REQ-FLAG-002 — Safe fallback**
- **Priority:** Must
- **Requirement:** If `connected.enabled` is missing, false, expired, malformed, unreachable, times out, or cannot be evaluated, then the system shall select Legacy checkout behavior.
- **Acceptance criteria:** No provider failure activates migrated behavior or corrupts workflow state.
- **Verification:** Provider-adapter fault tests and end-to-end Legacy outcome comparison.

**REQ-FLAG-003 — Child mode semantics**
- **Priority:** Must
- **Requirement:** While `connected.enabled` is true, the system shall interpret `connected.checkout.rule-mode` only as `legacy`, `compare`, or `service`, and shall fall back to `legacy` for a missing, expired, or invalid child value.
- **Acceptance criteria:** Mode truth table is deterministic and documented.
- **Verification:** Unit and API integration tests.

**REQ-FLAG-004 — Service authority**
- **Priority:** Must
- **Requirement:** The service shall be the authoritative evaluator of parent and child flags, and the client shall not independently elevate or override the effective mode.
- **Acceptance criteria:** Tampered, absent, or stale client capability state cannot activate service behavior contrary to the service's current safe decision.
- **Verification:** Contract, tampering, and version-skew tests.

**REQ-FLAG-005 — Cached evaluation**
- **Priority:** Must
- **Requirement:** The system shall evaluate flags from a provider SDK cache or versioned local snapshot without making an external network call for every business operation.
- **Acceptance criteria:** Refresh, expiry, last-known-state, and safe-false rules are explicit and testable; expired parent state resolves false.
- **Verification:** Adapter unit tests, provider-outage integration tests, and call-count instrumentation.

**REQ-FLAG-006 — Capability freshness**
- **Priority:** Must
- **Requirement:** When the client consumes a capability decision, the service shall provide a schema version, configuration version, evaluation reason, evaluated-at time, and expiry sufficient for the client to reject stale or unsupported routing information safely.
- **Acceptance criteria:** Unsupported or expired capability data selects Legacy behavior and produces an actionable non-sensitive diagnostic.
- **Verification:** Contract and version-skew tests.

**REQ-FLAG-007 — Cohort narrowing**
- **Priority:** Must
- **Requirement:** Where environment, practice, deployment ring, or client version targeting is configured, the system shall use those attributes only to narrow behavior already permitted by `connected.enabled`.
- **Acceptance criteria:** No cohort rule bypasses a false parent; sensitive targeting values are not logged.
- **Verification:** Targeting matrix and log-redaction tests.

**REQ-FLAG-008 — Compare semantics**
- **Priority:** Must
- **Requirement:** While effective checkout rule mode is `compare`, the system shall retain the Legacy decision for operator flow, execute the service decision in shadow mode, record a normalized comparison, and perform no more than one authoritative checkout write.
- **Acceptance criteria:** Match, mismatch, timeout, and service-error comparisons do not duplicate commands, audit records, loans, or idempotency records.
- **Verification:** API/UI integration tests with database and audit counts.

**REQ-FLAG-009 — Service semantics**
- **Priority:** Must
- **Requirement:** While effective checkout rule mode is `service`, the system shall use the service decision for operator eligibility feedback and shall retain the existing idempotent PostgreSQL-backed command as the only write path.
- **Acceptance criteria:** Native result cannot override the service decision, and the database can still reject a stale positive decision safely.
- **Verification:** UI/API integration and race-condition tests.

**REQ-FLAG-010 — Rollback**
- **Priority:** Must
- **Requirement:** When an operator changes the child mode to `legacy` or disables `connected.enabled`, the system shall restore the Legacy checkout decision path without data migration, service restart, or client redeployment after the configured refresh/expiry boundary.
- **Acceptance criteria:** In-flight behavior and maximum rollback convergence time are documented and tested.
- **Verification:** Rollback exercise and telemetry inspection.

### C. Contract and client behavior

**REQ-API-001 — `/api/v1` compatibility**
- **Priority:** Must
- **Requirement:** The system shall preserve documented `/api/v1` checkout command request/response behavior, stable `TLxxx` codes, generated identities, idempotency replay, and HTTP status categories across migration modes.
- **Acceptance criteria:** Existing integration tests pass unchanged except for additive test setup or assertions that do not alter the public domain contract.
- **Verification:** Contract suite against Legacy-local and Connected endpoints.

**REQ-API-002 — Versioned capability contract**
- **Priority:** Must
- **Requirement:** Where the client requires service routing information, the system shall expose it through an authenticated, explicitly versioned capability contract separate from domain authorization.
- **Acceptance criteria:** Old clients receive Legacy-compatible behavior; unsupported versions fail clearly without enabling Connected behavior.
- **Verification:** Authentication, compatibility, and version-skew contract tests.

**REQ-UI-001 — Preserve usability validation**
- **Priority:** Must
- **Requirement:** The Win32 client shall retain required-field, positive-number, date-shape, confirmation/cancellation, accessibility, and error-presentation checks that do not determine domain eligibility.
- **Acceptance criteria:** Existing `UI-001`, member creation, tool creation, confirmation, and return-input behavior remains observable.
- **Verification:** FlaUI characterization and regression tests in an interactive unlocked session.

**REQ-UI-002 — Actionable decision feedback**
- **Priority:** Must
- **Requirement:** When the service denies checkout eligibility, the client shall display an actionable, operator-safe message mapped from a stable service reason code without exposing internal exceptions or flag state.
- **Acceptance criteria:** Inactive, overdue, limit, unknown-tier, timeout, and unexpected outcomes are distinguishable.
- **Verification:** UI automation and contract-to-message mapping tests.

**REQ-UI-003 — No false success**
- **Priority:** Must
- **Requirement:** The client shall report checkout success only after the authoritative PostgreSQL-backed command commits and the service returns the committed result.
- **Acceptance criteria:** Decision success alone is never displayed as completed checkout.
- **Verification:** Interrupted-write and response-order integration tests.

### D. Network and failure behavior

**REQ-NET-001 — Configurable secure endpoint**
- **Priority:** Must
- **Requirement:** While effective checkout rule mode is `compare` or `service`, the client shall use an externally configured HTTPS service endpoint with certificate-chain and hostname validation enabled.
- **Acceptance criteria:** No production endpoint or credential is compiled into the client; invalid certificate and hostname cases fail closed.
- **Verification:** Configuration, certificate, hostname, and local/Connected contract tests.

**REQ-NET-002 — Explicit timeouts**
- **Priority:** Must
- **Requirement:** The client and service shall apply explicit connect, request, operation, and flag-evaluation timeouts for the affected workflow.
- **Acceptance criteria:** Timeout values are externalized/documented, bounded, and never cause indefinite UI-thread retry.
- **Verification:** Configuration inspection and simulated timeout tests.

**REQ-NET-003 — Safe retry**
- **Priority:** Must
- **Requirement:** If a decision read fails transiently, then the client may use bounded backoff with jitter; if a checkout write is ambiguous, then the client shall resolve it only by replaying the same idempotency key or querying status.
- **Acceptance criteria:** No retry creates an unrelated write or a second loan.
- **Verification:** Interrupted-request and idempotent replay tests.

**REQ-NET-004 — Understandable unavailable behavior**
- **Priority:** Must
- **Requirement:** If the remote service is unavailable or a decision times out, then the client shall distinguish timeout, unavailable service, authentication, authorization, validation, conflict, and unexpected failure without altering local or server state.
- **Acceptance criteria:** Each category has a stable operator-visible outcome and correlation ID when available.
- **Verification:** UI/API fault-injection tests.

### E. Security, privacy, audit, and observability

**REQ-SEC-001 — Unconditional controls**
- **Priority:** Must
- **Requirement:** The system shall enforce authentication, authorization, practice isolation where present, API input validation, idempotency, and audit independently of every feature-flag value.
- **Acceptance criteria:** No flag combination weakens an existing control.
- **Verification:** Security regression matrix and code inspection.

**REQ-SEC-002 — Secret handling**
- **Priority:** Must
- **Requirement:** The system shall externalize endpoint credentials and flag-provider credentials and shall not log or commit secrets, authorization headers, credential contents, sensitive request bodies, or sensitive targeting attributes.
- **Acceptance criteria:** Repository and log scans contain no prohibited values; missing configured credentials fail closed without demo fallback in Connected execution.
- **Verification:** Secret scan, configuration tests, and log-redaction tests.

**REQ-OBS-001 — Correlation continuity**
- **Priority:** Must
- **Requirement:** When the client requests a decision or command, the system shall preserve a correlation/request ID across client diagnostics, service logs, rule comparison, database audit for committed operations, and error responses.
- **Acceptance criteria:** A representative workflow can be traced end-to-end without logging sensitive content.
- **Verification:** Structured-log and audit correlation test.

**REQ-OBS-002 — Flag evaluation evidence**
- **Priority:** Must
- **Requirement:** When the service evaluates a flag, the system shall emit structured evidence containing flag key, effective value/mode, evaluation reason, configuration version, non-sensitive cohort key, correlation ID, and evaluation timestamp.
- **Acceptance criteria:** Missing, invalid, expired, provider-error, parent-disabled, legacy, compare, and service reasons are distinguishable.
- **Verification:** Structured telemetry schema and fault-path tests.

**REQ-OBS-003 — Normalized comparison evidence**
- **Priority:** Must
- **Requirement:** While in compare mode, the system shall record the Legacy decision, service decision, normalized input identity, normalized reason codes, match status, contract versions, timing, and correlation ID without storing secrets or unnecessary personal data.
- **Acceptance criteria:** Reviewers can calculate match rate and identify mismatches without accessing raw sensitive payloads.
- **Verification:** Telemetry schema inspection and scenario tests.

**REQ-OBS-004 — Operational metrics**
- **Priority:** Should
- **Requirement:** The system shall expose counts and latency distributions for flag evaluations, decision outcomes, comparison matches/mismatches, provider failures, decision failures, and rollback mode changes.
- **Acceptance criteria:** Metrics separate Legacy, compare, and service behavior without making telemetry authoritative for workflow state.
- **Verification:** Metrics integration tests and representative read/write latency baseline.

### F. Compatibility, verification, and retirement

**REQ-COMP-001 — Old-client/new-service compatibility**
- **Priority:** Must
- **Requirement:** When an existing client that does not understand capabilities calls the updated service, the system shall preserve Legacy-compatible `/api/v1` behavior and shall not require the client to execute Connected rule mode.
- **Acceptance criteria:** Supported version matrix and clear unsupported-version behavior are documented and tested.
- **Verification:** Version-skew contract tests.

**REQ-TEST-001 — Permanent Legacy evidence**
- **Priority:** Must
- **Requirement:** The system shall retain and pass applicable native, database, API, and UI Legacy suites while the migration flag exists.
- **Acceptance criteria:** No existing assertion is weakened or deleted solely because responsibility moved.
- **Verification:** Clean Release build and standard test scripts, with UI tests run only in a valid interactive session.

**REQ-TEST-002 — Equivalent service rule coverage**
- **Priority:** Must
- **Requirement:** Before service mode is eligible for rollout, the system shall demonstrate equivalent service-level coverage for every NativeRules decision-table case and affected scenario `LOAN-001`, `LOAN-002`, `LOAN-003`, `LOAN-004`, and `UI-001`.
- **Acceptance criteria:** Expected decisions, HTTP outcomes, database state, and audit evidence are normalized and equal where applicable.
- **Verification:** Unit, contract, database, API, concurrency, and UI suites.

**REQ-TEST-003 — Mode and provider matrix**
- **Priority:** Must
- **Requirement:** The system shall test parent disabled, parent enabled with each child mode, child enabled with parent disabled, missing/invalid/expired snapshots, provider unreachable/timeout, stale client capability, compare mismatch, and rollback.
- **Acceptance criteria:** Every path has an explicit expected mode, write count, operator outcome, and telemetry reason.
- **Verification:** Automated truth-table and fault-injection suite.

**REQ-REL-001 — Promotion gate**
- **Priority:** Must
- **Requirement:** Before promoting from compare to service mode, the system shall require an approved evidence record showing zero unexplained decision mismatches for the agreed scenario dataset, exactly one authoritative write per command, acceptable latency against a recorded baseline, and successful rollback rehearsal.
- **Acceptance criteria:** Promotion is a human governance decision; code deployment alone cannot change the mode.
- **Verification:** Release checklist and retained test/telemetry evidence.

**REQ-REL-002 — Child-flag retirement**
- **Priority:** Must
- **Requirement:** After service mode is proven, expanded, and the Legacy implementation is intentionally approved for retirement, the system shall remove the checkout child flag and dead migration path while retaining `connected.enabled` throughout the Connected stage.
- **Acceptance criteria:** Retirement requires a separately approved change with replacement test evidence and rollback implications documented.
- **Verification:** Governance review, code inspection, and regression suite.

## Data and provenance minimums

The migration shall not create a new workflow data store. The following evidence is additive and non-authoritative:

| Evidence | Minimum fields | Retention/authority |
| --- | --- | --- |
| Flag evaluation | Key, effective value/mode, reason, configuration version, non-sensitive cohort key, evaluated-at, correlation ID | Operational evidence; never workflow authority |
| Rule comparison | Normalized scenario/input identity, Legacy decision/reason, service decision/reason, match status, contract versions, duration, correlation ID | Migration evidence; no raw secrets or unnecessary PII |
| Business audit | Existing actor, operation, entity, request ID, committed details | Existing PostgreSQL audit remains authoritative |
| Idempotency | Existing operation, key, request hash, committed response/status | Existing PostgreSQL record remains authoritative |

Every retained release result shall identify source revision, build configuration, environment, fixture version, mode/configuration version, per-suite results, skips, and correlation IDs for failures.

## User stories

- As an operator, I want eligibility feedback that remains clear when rules move to the service so I do not mistake a network failure for a business denial.
- As a release reviewer, I want compare evidence without duplicate writes so I can prove parity safely.
- As an operator responsible for rollout, I want one parent circuit breaker and a Legacy child mode so I can recover without redeploying.
- As a security reviewer, I want flag failures to default to Legacy and security controls to remain unconditional.
- As a maintainer, I want one service rule contract and retained database authority so client releases no longer carry active domain policy in service mode.

## Validation and release gates

### Requirements gate

- `REQUIREMENTS.md` passes the specification validator.
- All blocking questions are resolved or explicitly assigned as later design/release gates.
- Requirements receive explicit approval before design begins.

### Design gate

- Every Must requirement maps to a component, interface, correctness property, and verification strategy.
- Provider/cache semantics, capability contract, comparison location, telemetry sink, and rollback convergence are explicit.
- Design receives explicit approval before task planning.

### Implementation gate

- Clean Release build succeeds.
- Native, database, API, contract, flag/fault, and applicable FlaUI suites pass.
- Tests run against local and Connected endpoints with normalized parity evidence.
- Certificate, hostname, credential, timeout, unavailable, interrupted-write, and idempotent replay tests pass.
- Compare mode proves one write and no unexplained mismatch for the approved dataset.
- Flag observability and log redaction are verified.
- Rollback is rehearsed and documented.
- Skipped environment-bound tests state the exact reason and block promotion unless explicitly governed.

## Decisions and open questions

### Requirements decisions

| ID | Decision | Basis |
| --- | --- | --- |
| DEC-001 | “All business logic” means the enumerated domain-policy decisions, not UI usability validation. | Connected policy retains immediate operator validation; brownfield scope inventory identifies the active client domain rules. |
| DEC-002 | PostgreSQL retains locked transactional rule and workflow-data authority. | Connected ownership rules prohibit dual writers and early data migration. |
| DEC-003 | `NativeRules.dll` and its tests remain during the migration. | Removal is an explicit non-goal until equivalent evidence and intentional retirement approval exist. |
| DEC-004 | The child key is `connected.checkout.rule-mode`. | It follows the mandated workflow-mode pattern and is reversible before design. |
| DEC-005 | Provider selection is deferred to design behind a replaceable evaluator contract. | Requirements specify behavior, caching, expiry, failure, and evidence rather than a vendor. |

### Open questions

| ID | Question | Owner | Blocking stage/status |
| --- | --- | --- | --- |
| OQ-001 | Which replaceable provider or versioned local snapshot implementation best fits .NET Framework 4.8 and the POC deployment model? | Design reviewer | Blocks DESIGN approval, not requirements draft. |
| OQ-002 | What refresh interval, maximum snapshot age, and rollback convergence objective are operationally acceptable? | Design/release reviewer | Blocks DESIGN approval. |
| OQ-003 | Should the client capability contract be a dedicated `/api/v1/capabilities` resource or another explicitly versioned mechanism? | API design reviewer | Blocks DESIGN approval. |
| OQ-004 | Where will non-authoritative flag/comparison telemetry be retained in this POC without introducing workflow authority? | Operations/design reviewer | Blocks DESIGN approval. |
| OQ-005 | What dataset size and observation window are required for compare-to-service promotion beyond the mandatory characterized scenario matrix? | Release reviewer | Blocks service-mode promotion, not design implementation. |

## Glossary

| Term | Meaning |
| --- | --- |
| Business logic | Domain-policy decisions for checkout eligibility, tier limits, and loan duration; excludes UI input-shape and presentation behavior. |
| Capability decision | Authenticated, versioned service response that tells a client which presentation/routing path is safely permitted; it is not domain authorization. |
| Compare mode | Legacy-authoritative operator flow plus side-effect-free service shadow evaluation and normalized difference evidence. |
| Connected behavior | Runtime migration behavior that may activate only beneath `connected.enabled`. |
| Parent circuit breaker | Service-authoritative `connected.enabled` flag, safely false whenever evaluation is not valid and current. |
| Rule mode | Checkout child selection `legacy`, `compare`, or `service`, effective only while the parent is true. |
| Service layer | Existing .NET Framework application service responsible for API contracts, orchestration, decision contract, idempotency coordination, and error mapping. |
| Transactional authority | Existing PostgreSQL routines that lock, validate, write workflow state, and append authoritative audit records. |

## Assumptions, dependencies, and change control

### Assumptions

- The approved brownfield baseline correctly identifies all active client domain-policy decisions in the current workflow.
- The requested move targets the existing application service, not a new deployable service.
- Immediate input validation and confirmation remain client responsibilities because they do not authorize or commit domain outcomes.
- A provider-neutral local snapshot/evaluator can satisfy the POC without a per-operation external dependency.
- Service-mode rollout does not authorize retirement of NativeRules or Legacy tests in this change.

### Dependencies

- Updated `AGENTS.md` feature-gate policy and behavioral baseline commit `b9a5125`.
- Existing `/api/v1`, PostgreSQL routines, idempotency records, audit log, and stable `TLxxx` errors.
- Existing NativeRules, database, API, and FlaUI characterization suites.
- A supported feature-evaluation/snapshot mechanism compatible with .NET Framework 4.8/x86 and repository dependency policy.
- Interactive, unlocked Windows desktop access for FlaUI verification.

### Change control

Any change to scope, rule outcomes, authoritative data/rule ownership, public API compatibility, feature-gate semantics, security controls, or release gates returns this document to `draft`, invalidates affected downstream approvals, and requires revalidation. Editorial changes that do not alter meaning may retain approval when reported.
