---
type: Glossary
title: Connected-stage glossary
description: Terms used when discussing business-logic ownership.
sources:
  - resource: repo://AGENTS.md
  - resource: repo://docs/architecture.md#L26-L54
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T22:25:34.1947474+00:00
status: draft
source_revision: 60c93421a8798b983091d7971a3f079d010579e8
source_fingerprint: 080c303d93f7bcd7e5c6b158ac8e39f35a5fe7b68a8da9ebb08586f34678428a
source_worktree: dirty
curation_status: generated
---

# Terms

- **Application service / service layer:** the existing .NET Framework Web API and repository layer that owns contracts, orchestration, idempotency coordination, and error mapping. It is not a new microservice.
- **Authoritative business rule:** a decision whose result controls committed workflow state. In the baseline, concurrency-sensitive rules are enforced by PostgreSQL routines.
- **Client precheck:** a non-authoritative check used for immediate operator feedback. It must not be relied upon for security or data integrity.
- **Input/presentation validation:** checks needed to create a usable request or explain malformed operator input; this remains a client responsibility for usability and is repeated authoritatively at the API boundary.
- **NativeRules:** the x86 C++ DLL containing duplicated checkout eligibility and tier policy calculations.
- **Observable parity:** equivalent API outcome, database state, audit evidence, and understandable UI behavior for the same scenario, even when responsibility moves.
- **Parent circuit breaker:** `connected.enabled`, evaluated authoritatively by the service and required to be true before any child Connected behavior can activate.
- **Rule mode:** a workflow child selection of `legacy`, `compare`, or `service`; it narrows the parent gate and cannot override a false parent.
- **Compare mode:** side-effect-free shadow evaluation of the service rule with normalized difference telemetry while the Legacy path remains active and exactly one authoritative write occurs.
