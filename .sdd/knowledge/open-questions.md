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
  at: 2026-09-02T23:45:00+00:00
status: draft
source_revision: f53a98427070af5f64bdce85b015fc66ca863210
source_fingerprint: 120f1a554cacbd7eaac6ef03c18228d1cbdd20d65d0e01b22a22c629dd4c2a7b
source_worktree: dirty
curation_status: generated
---

# Open questions

1. Does “all business logic out of the client” mean removing domain-policy decisions (eligibility, tier limits, maximum duration) while retaining required-field, numeric/date-shape, confirmation, and presentation logic as mandated by Connected policy?
2. Should the current `NativeRules.dll` be retired in this slice after equivalent service/API tests exist, or retained temporarily but no longer called by the production client?
3. Should service-owned eligibility be exposed as a dedicated versioned decision endpoint, expressed only through command outcomes, or included as advisory fields in an existing member response?
4. Is the intended slice limited to existing client-side domain decisions, or should it also address the prerequisite Connected transport gaps (configurable HTTPS endpoint, explicit timeouts, and actionable network errors) before making the client depend more heavily on the service?
5. Which feature-flag provider or versioned local snapshot mechanism is approved for this POC, and what refresh interval, expiry, cohort key, and configuration version semantics apply?
6. What workflow child key should govern checkout rule migration (for example `connected.checkout.rule-mode`), and what measurable exit criteria allow compare-to-service promotion and eventual child-flag removal?
7. Should capability information be a dedicated versioned endpoint/response or remain entirely service-internal until the client needs presentation/routing information?

# Evidence gaps

- `AMBIGUOUS`: Graphify did not extract `NativeRules.h`; direct source inspection resolves the exported API, but graph-based call-path confidence is reduced.
- `AMBIGUOUS`: `docs/demo.md` was omitted from semantic extraction. It is not an architectural source of truth and no material claim depends on it.
