---
type: Constraint
title: Connected-stage durable constraints
description: Architecture, compatibility, security, and scope constraints relevant to responsibility movement.
sources:
  - resource: repo://AGENTS.md#L42-L103
  - resource: repo://AGENTS.md#L161-L163
  - resource: repo://docs/north-star-architecture.md#L110-L149
  - resource: repo://docs/testing-evolution.md#L47-L61
  - resource: repo://src/AppServer/AppServer.csproj#L9-L12
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T23:50:00+00:00
status: draft
source_revision: a92466a48e26afbd15a296ad2fb00482d0227c12
source_fingerprint: 4ed98567a0c7e21fcdfc11ab848937099febed3f0c1aef7b474f63b4b6a62fbe
source_worktree: dirty
curation_status: generated
---

# Summary

- Preserve `/api/v1` outcomes, generated server/database identities, stable errors, idempotency, audit, and PostgreSQL ownership.
- Keep the desktop database-independent and retain the supported .NET Framework 4.8, C++, Windows, and x86 constraints ([AppServer.csproj:11](../../src/AppServer/AppServer.csproj#L11)).
- Encapsulate before extracting; do not introduce a new deployable service or cloud data authority for this change.
- Retain immediate operator validation and feedback in the Win32 client. Treat such input/presentation validation separately from domain decisions such as eligibility, limits, and duration.
- Preserve native regression evidence until equivalent service/API coverage exists; do not delete tests merely because a boundary moves ([testing-evolution.md:60](../../docs/testing-evolution.md#L60)).
- Maintain one authoritative owner for every datum and decision; this aligns with the North Star principle that each data element has one authoritative owner ([north-star-architecture.md:110](../../docs/north-star-architecture.md#L110)).
- Preserve the behavioral baseline identified by policy at Connected commit `b9a5125`; merely deploying new code must not activate migrated behavior ([AGENTS.md:42](../../AGENTS.md#L42)).
- Gate migrated business-rule execution beneath service-authoritative `connected.enabled`, safely defaulting to false, with a workflow child `legacy | compare | service` mode ([AGENTS.md:52](../../AGENTS.md#L52), [AGENTS.md:87](../../AGENTS.md#L87)).
- Keep security controls independent of feature evaluation and make evaluation cached/snapshotted, observable, and free of sensitive targeting data ([AGENTS.md:83](../../AGENTS.md#L83)).
- Retain the parent circuit breaker throughout Connected; remove a temporary child flag only after proof, expansion, and intentional Legacy-path retirement ([AGENTS.md:161](../../AGENTS.md#L161)).
