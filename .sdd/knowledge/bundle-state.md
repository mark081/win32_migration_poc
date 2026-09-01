---
type: Bundle State
title: Brownfield analysis state
description: Revision and working-tree baseline represented by this bundle.
sources:
  - resource: repo://.
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-31T16:27:53+00:00
status: draft
source_revision: a92466a48e26afbd15a296ad2fb00482d0227c12
source_fingerprint: 4ed98567a0c7e21fcdfc11ab848937099febed3f0c1aef7b474f63b4b6a62fbe
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
