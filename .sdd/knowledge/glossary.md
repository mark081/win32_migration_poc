---
type: Glossary
title: Connected-stage glossary
description: Terms used when discussing business-logic ownership.
sources:
  - resource: repo://AGENTS.md
  - resource: repo://docs/architecture.md#L26-L54
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T02:30:05.1990492+00:00
status: draft
source_revision: 94b5ac2445e715ebadd339124a94ca1a9378be61
source_fingerprint: 9858fe44281cc6d8e46efe70be16159507d7b40cafb88fea03e2f72921eb2b6b
source_worktree: clean
curation_status: generated
---

# Terms

- **Application service / service layer:** the existing .NET Framework Web API and repository layer that owns contracts, orchestration, idempotency coordination, and error mapping. It is not a new microservice.
- **Authoritative business rule:** a decision whose result controls committed workflow state. In the baseline, concurrency-sensitive rules are enforced by PostgreSQL routines.
- **Client precheck:** a non-authoritative check used for immediate operator feedback. It must not be relied upon for security or data integrity.
- **Input/presentation validation:** checks needed to create a usable request or explain malformed operator input; this remains a client responsibility for usability and is repeated authoritatively at the API boundary.
- **NativeRules:** the x86 C++ DLL containing duplicated checkout eligibility and tier policy calculations.
- **Observable parity:** equivalent API outcome, database state, audit evidence, and understandable UI behavior for the same scenario, even when responsibility moves.
