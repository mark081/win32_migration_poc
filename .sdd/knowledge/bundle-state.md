---
type: Bundle State
title: Brownfield analysis state
description: Revision and working-tree baseline represented by this bundle.
sources:
  - resource: repo://.
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

Connected-stage brownfield baseline reconciled at revision `60c93421a8798b983091d7971a3f079d010579e8` with the uncommitted `AGENTS.md` feature-gate policy represented as intentional dirty-worktree input.

Graphify extraction produced 246 nodes and 438 edges. Its C++ parser did not extract symbols from `src/NativeRules/NativeRules.h`, so native-rule claims in this bundle are verified directly against the header, implementation, callers, and tests. `docs/demo.md` produced no semantic nodes; it is not used as evidence for a material claim.

## Reconciliation disposition

- Revised: constraints, runtime components, API interface, data ownership, build/test operations, hotspots and risks, glossary, and open questions to reflect the new mandatory feature-gate policy.
- Created: Connected feature-gate policy concept.
- Unchanged after verification: system overview; the policy changes required migration behavior but introduces no implemented runtime component at this revision.
- Evidence limitation unchanged: Graphify still cannot parse `NativeRules.h`; native behavior remains verified directly.

## Wave 0 update disposition

- Revised: build/test operations to record expanded NativeRules tier, duration, and eligibility-boundary characterization.
- Unchanged after verification: system overview, runtime architecture, API interface, data ownership, feature-gate policy, constraints, hotspots, glossary, and open questions; Wave 0 changed tests only and introduced no runtime or ownership behavior.
- Graph refresh: `graphify update .` rebuilt 379 nodes and 578 edges; the `NativeRules.h` parser limitation remains.

## Wave 1 update disposition

- Revised: Connected feature-gate policy and runtime components for the new fail-closed service evaluator foundation; build/test operations for its focused suite; runtime components and risks for the versioned NativeRules reason export and partially integrated flag foundation.
- Unchanged after verification: system overview, API interface, data ownership, constraints, glossary, and open questions. Wave 1 adds internal foundations only; it does not change public routes, data ownership, workflow routing, or production behavior.
- Graph refresh: `graphify update .` rebuilt 528 nodes and 907 edges. Graphify still reports `NativeRules.h` as partially extracted, so its ABI is verified directly from the header, implementation, tests, and `dumpbin` evidence.

## Wave 2 update disposition

- Revised: runtime components and Connected feature-gate policy for the additive telemetry contracts, JSON diagnostic sink, hashing/redaction boundary, bounded metrics, and sink-failure isolation; build/test operations for the focused telemetry checks; glossary for the non-authoritative telemetry definition.
- Unchanged after verification: system overview, API interface, data ownership, constraints, hotspots and risks, and open questions. Wave 2 adds an internal, unwired service seam only; it changes no public route, workflow call path, data owner, database state, or runtime feature activation.
- Graph refresh: `graphify update .` rebuilt 621 nodes and 1,083 edges. Graphify still reports `NativeRules.h` as partially extracted; the Wave 2 telemetry source and test dependencies were extracted successfully.
- Pre-update manifest diff: added `src/AppServer/ConnectedTelemetry.cs` and modified the AppServer/test project plus feature-test harness. Reconciled fingerprint: `4ed98567a0c7e21fcdfc11ab848937099febed3f0c1aef7b474f63b4b6a62fbe`.

## Wave 3 update disposition

- Revised: runtime components for the new read-only eligibility query and service evaluator; data ownership to record that the new decision performs no write and does not replace PostgreSQL checkout validation; build/test operations for the expanded decision, clock, repository, and before/after database checks; hotspots and risks to show that the foundations remain unwired from product routes.
- Unchanged after verification: system overview, API interface, Connected feature-gate policy, constraints, glossary, and open questions. Wave 3 adds internal read/decision code only; it changes no route, client flow, database routine, workflow write, data owner, or runtime feature activation.
- Graph refresh: `graphify update .` rebuilt 662 nodes and 1,171 edges. Graphify still reports `NativeRules.h` as partially extracted; the new C# evaluator and repository read were extracted successfully.
- Pre-update manifest diff: added `src/AppServer/CheckoutRules.cs` and the feature-test configuration; modified the repository, AppServer/test projects, and focused test harness. Documentation-only changes from the reviewed foundation commit were also detected and verified not to alter runtime behavior.

## Wave 4 update disposition

- Revised: API interface for the authenticated versioned capability contract; runtime components and system overview for its service entry point; Connected feature-gate policy for bounded client-version narrowing and parent/child telemetry; build/test operations for capability contract and authentication coverage; hotspots and risks for the still-unwired client/decision paths.
- Unchanged after verification: data ownership, constraints, glossary, and open questions. The route reads only cached feature state, emits non-authoritative diagnostics, and has no repository, workflow, idempotency, audit, or database dependency.
- Graph refresh: `graphify update .` rebuilt 691 nodes and 1,233 edges. Graphify still reports `NativeRules.h` as partially extracted; the capability route, service adapter, evaluator/telemetry dependencies, and tests were extracted successfully.
- Pre-update manifest diff: added `src/AppServer/Capabilities.cs`; modified the AppServer project, focused test harness, and API integration suite.

## Wave 5 update disposition

- Revised: system overview and runtime components for the checkout-decision route and configured
  transport; API interface for the authenticated read-only contract and stable errors; Connected
  feature-gate policy for per-decision server re-evaluation; data ownership for verified read-only
  behavior; build/test operations for decision and transport coverage; hotspots and risks for the
  remaining unwired client capability router.
- Unchanged after verification: constraints, glossary, and open questions. Wave 5 changes no
  approved scope, term meaning, database routine/schema, data owner, or unresolved design decision.
- Graph refresh: final `graphify update .` rebuilt 799 nodes and 1,497 edges. The existing
  `NativeRules.h` partial-extraction warning remains; direct source/test inspection still supplies
  its evidence. Graphify also reported that saved community labels lag the new community set; this
  affects generated labels, not verified source relationships.
- Pre-update manifest diff: added `CheckoutDecisions.cs`, `ClientTransport.cpp`, and
  `ClientTransport.h`; modified the application/client/test projects, UI entry point, focused/API
  tests, README, and architecture documentation. Reconciled fingerprint:
  `03f5d984f90e6165e528dd9797b210af9be4925f02a51cfdb8dc2b55f7f3d4fc`.

## Wave 6 update disposition

- Revised: system overview, runtime components, API interface, build/test operations, and hotspots
  for the new process-local capability cache and endpoint router.
- Unchanged after verification: Connected feature-gate policy, data ownership, constraints,
  glossary, and open questions. The router changes transient endpoint selection only; it adds no
  durable data, business authorization, database call, provider policy, term, or unresolved design
  decision.
- Graph refresh: `graphify update .` rebuilt 826 nodes and 1,556 edges. The existing
  `NativeRules.h` partial-extraction warning remains and direct source/test evidence still covers it.
- Pre-update manifest diff: added `CapabilityRouter.cpp` and `.h`; modified the desktop transport,
  entry point, client/test projects, native harness, README, and architecture documentation. The
  previously reconciled Wave 5 checkout-decision and transport paths were also present because its
  manifest sidecar still identified the pre-Wave-5 commit. Reconciled fingerprint:
  `019debc291402cec69724d410e6f848cb52d07394f8bf82c5a4cb3432c2fa2e4`.

## Wave 7 update disposition

- Revised: system overview, runtime components, API interface, build/test operations, and hotspots
  for the Legacy/compare checkout client integration and structured NativeRules adapter.
- Unchanged after verification: Connected feature-gate policy, data ownership, constraints,
  glossary, and open questions. Compare remains beneath the existing parent/child evaluation,
  creates no durable decision state, and retains the existing single PostgreSQL-backed writer.
- Graph refresh: `graphify update .` rebuilt 845 nodes and 1,608 edges. The existing
  `NativeRules.h` partial-extraction warning remains and direct source/test evidence covers it.
- Pre-update manifest diff: added `CheckoutMode.cpp` and `.h`; modified capability cache access,
  desktop checkout flow, client/test projects, native and FlaUI harnesses, README, and architecture docs.
  Reconciled fingerprint: `5c9a61f0a37d9c8b8be61b6ae0208c38d7bb4bfc132744a2d45b8f448229e28e`.
