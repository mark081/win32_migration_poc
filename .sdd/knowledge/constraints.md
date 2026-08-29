---
type: Constraint
title: Connected-stage durable constraints
description: Architecture, compatibility, security, and scope constraints relevant to responsibility movement.
sources:
  - resource: repo://AGENTS.md
  - resource: repo://docs/north-star-architecture.md#L110-L149
  - resource: repo://docs/testing-evolution.md#L47-L61
  - resource: repo://src/AppServer/AppServer.csproj#L9-L12
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

- Preserve `/api/v1` outcomes, generated server/database identities, stable errors, idempotency, audit, and PostgreSQL ownership.
- Keep the desktop database-independent and retain the supported .NET Framework 4.8, C++, Windows, and x86 constraints ([AppServer.csproj:11](../../src/AppServer/AppServer.csproj#L11)).
- Encapsulate before extracting; do not introduce a new deployable service or cloud data authority for this change.
- Retain immediate operator validation and feedback in the Win32 client. Treat such input/presentation validation separately from domain decisions such as eligibility, limits, and duration.
- Preserve native regression evidence until equivalent service/API coverage exists; do not delete tests merely because a boundary moves ([testing-evolution.md:60](../../docs/testing-evolution.md#L60)).
- Maintain one authoritative owner for every datum and decision; this aligns with the North Star principle that each data element has one authoritative owner ([north-star-architecture.md:110](../../docs/north-star-architecture.md#L110)).
