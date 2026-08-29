---
type: Data Architecture
title: Data and transactional rule ownership
description: PostgreSQL schemas, transaction support records, and authoritative rule boundary.
resource: repo://database/
tags: [postgresql, ownership, transactions, audit]
sources:
  - resource: repo://database/001_schema.sql#L1-L75
  - resource: repo://database/002_routines.sql#L3-L56
  - resource: repo://src/AppServer/Repository.cs#L304-L526
  - resource: repo://AGENTS.md
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

The existing PostgreSQL database is the sole durable owner of tools, members, reservations, loans, audit records, and idempotency records. The desktop has no database dependency; the service is the sole database caller.

## Transaction and rule boundaries

- Reservation, checkout, and return routines lock applicable records, validate domain invariants, update workflow state, and append audit records ([002_routines.sql:6](../../../database/002_routines.sql#L6), [002_routines.sql:23](../../../database/002_routines.sql#L23), [002_routines.sql:45](../../../database/002_routines.sql#L45)).
- The service wraps retryable writes and durable deduplication in serializable transactions; the idempotency response is committed with the business operation ([Repository.cs:446](../../../src/AppServer/Repository.cs#L446), [Repository.cs:489](../../../src/AppServer/Repository.cs#L489), [Repository.cs:492](../../../src/AppServer/Repository.cs#L492)).
- Tier limits and maximum durations are functions in PostgreSQL and duplicated in `NativeRules.dll` for client prechecks ([002_routines.sql:3](../../../database/002_routines.sql#L3), [NativeRules.cpp:9](../../../src/NativeRules/NativeRules.cpp#L9)).

## Durable constraint

Connected changes must not create a second authoritative writer or move workflow state out of this database. “Service layer” therefore cannot mean replacing transactionally locked stored-routine authority without separately approved scope.
