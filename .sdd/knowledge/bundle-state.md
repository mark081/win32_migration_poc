---
type: Bundle State
title: Brownfield analysis state
description: Revision and working-tree baseline represented by this bundle.
sources:
  - resource: repo://.
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T02:30:05.1990492+00:00
status: draft
source_revision: 94b5ac2445e715ebadd339124a94ca1a9378be61
source_fingerprint: 9858fe44281cc6d8e46efe70be16159507d7b40cafb88fea03e2f72921eb2b6b
source_worktree: clean
curation_status: generated
---

# Summary

Initial Connected-stage brownfield baseline generated from revision `94b5ac2445e715ebadd339124a94ca1a9378be61` before specification work.

Graphify extraction produced 246 nodes and 438 edges. Its C++ parser did not extract symbols from `src/NativeRules/NativeRules.h`, so native-rule claims in this bundle are verified directly against the header, implementation, callers, and tests. `docs/demo.md` produced no semantic nodes; it is not used as evidence for a material claim.
