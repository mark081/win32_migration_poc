---
type: Bundle State
title: Brownfield analysis state
description: Revision and working-tree baseline represented by this bundle.
sources:
  - resource: repo://.
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T22:25:34.1947474+00:00
status: draft
source_revision: 60c93421a8798b983091d7971a3f079d010579e8
source_fingerprint: 080c303d93f7bcd7e5c6b158ac8e39f35a5fe7b68a8da9ebb08586f34678428a
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
