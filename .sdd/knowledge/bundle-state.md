---
type: Bundle State
title: Brownfield analysis state
description: Revision and working-tree baseline represented by this bundle.
sources:
  - resource: repo://.
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
