---
type: Open Questions
title: Questions requiring human or specification decisions
description: Ambiguities that evidence alone cannot resolve.
sources:
  - resource: repo://AGENTS.md
  - resource: repo://src/DesktopClient/main.cpp#L313-L387
  - resource: repo://docs/testing-evolution.md#L47-L61
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T02:30:05.1990492+00:00
status: draft
source_revision: 94b5ac2445e715ebadd339124a94ca1a9378be61
source_fingerprint: 9858fe44281cc6d8e46efe70be16159507d7b40cafb88fea03e2f72921eb2b6b
source_worktree: clean
curation_status: generated
---

# Open questions

1. Does “all business logic out of the client” mean removing domain-policy decisions (eligibility, tier limits, maximum duration) while retaining required-field, numeric/date-shape, confirmation, and presentation logic as mandated by Connected policy?
2. Should the current `NativeRules.dll` be retired in this slice after equivalent service/API tests exist, or retained temporarily but no longer called by the production client?
3. Should service-owned eligibility be exposed as a dedicated versioned decision endpoint, expressed only through command outcomes, or included as advisory fields in an existing member response?
4. Is the intended slice limited to existing client-side domain decisions, or should it also address the prerequisite Connected transport gaps (configurable HTTPS endpoint, explicit timeouts, and actionable network errors) before making the client depend more heavily on the service?

# Evidence gaps

- `AMBIGUOUS`: Graphify did not extract `NativeRules.h`; direct source inspection resolves the exported API, but graph-based call-path confidence is reduced.
- `AMBIGUOUS`: `docs/demo.md` was omitted from semantic extraction. It is not an architectural source of truth and no material claim depends on it.
