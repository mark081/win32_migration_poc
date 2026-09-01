---
type: Architecture Component
title: Runtime components and responsibility boundaries
description: Current responsibilities of the Win32 client, NativeRules DLL, application service, and PostgreSQL.
resource: repo://src/
tags: [client, native-rules, app-server, database]
sources:
  - resource: repo://src/DesktopClient/main.cpp#L313-L387
  - resource: repo://src/NativeRules/NativeRules.cpp#L5-L28
  - resource: repo://src/AppServer/Controllers.cs#L121-L227
  - resource: repo://src/AppServer/Repository.cs#L174-L216
  - resource: repo://src/AppServer/ConnectedFeatures.cs#L341-L472
  - resource: repo://src/AppServer/ConnectedTelemetry.cs#L13-L453
  - resource: repo://src/NativeRules/NativeRules.h#L8-L26
  - resource: repo://database/002_routines.sql#L3-L56
  - resource: repo://docs/architecture.md#L26-L54
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-31T16:27:53+00:00
status: draft
source_revision: a92466a48e26afbd15a296ad2fb00482d0227c12
source_fingerprint: 4ed98567a0c7e21fcdfc11ab848937099febed3f0c1aef7b474f63b4b6a62fbe
source_worktree: dirty
curation_status: generated
---

# Summary

Business behavior is deliberately distributed. Client and DLL checks provide immediate feedback, the service governs the HTTP workflow, and stored routines remain authoritative for concurrency-sensitive decisions ([architecture.md:26](../../../docs/architecture.md#L26)).

## Responsibilities

- **Win32 client:** collects values, enforces required-field and numeric-shape checks, asks for confirmation, renders responses, and constructs API requests ([main.cpp:313](../../../src/DesktopClient/main.cpp#L313), [main.cpp:331](../../../src/DesktopClient/main.cpp#L331), [main.cpp:349](../../../src/DesktopClient/main.cpp#L349), [main.cpp:375](../../../src/DesktopClient/main.cpp#L375)). It also makes a duplicated checkout-eligibility decision from member state returned by the API and blocks the request locally when the DLL rejects it ([main.cpp:358](../../../src/DesktopClient/main.cpp#L358)).
- **NativeRules DLL:** implements tier checkout limits, maximum loan durations, and eligibility from active/overdue/open-loan/tier inputs. Its versioned structured export distinguishes allowed, inactive, overdue, limit-reached, and unsupported-tier results while the legacy boolean export delegates to it ([NativeRules.h:8](../../../src/NativeRules/NativeRules.h#L8), [NativeRules.cpp:25](../../../src/NativeRules/NativeRules.cpp#L25)).
- **Application service:** validates request DTOs, authenticates calls, reads eligibility context, coordinates idempotent transactions, calls stored routines, and translates stable database failures into HTTP responses ([Models.cs:6](../../../src/AppServer/Models.cs#L6), [Controllers.cs:46](../../../src/AppServer/Controllers.cs#L46), [Repository.cs:174](../../../src/AppServer/Repository.cs#L174)). It also owns cached fail-closed feature evaluation and non-authoritative telemetry foundations: typed flag/comparison records, hashed cohort/input identities, bounded in-memory metrics, JSON diagnostics, failure isolation, and an in-memory test sink ([ConnectedFeatures.cs:341](../../../src/AppServer/ConnectedFeatures.cs#L341), [ConnectedTelemetry.cs:13](../../../src/AppServer/ConnectedTelemetry.cs#L13)). No API or workflow consumes either foundation yet.
- **PostgreSQL:** owns tier functions and locked reservation, checkout, return, fee, audit, and durable state transitions ([002_routines.sql:3](../../../database/002_routines.sql#L3), [002_routines.sql:23](../../../database/002_routines.sql#L23), [002_routines.sql:45](../../../database/002_routines.sql#L45)).

## Ownership implication

Moving authoritative decisions into the application service would conflict with the current Connected policy if it displaced locked PostgreSQL rules. A compatible interpretation is to remove duplicated domain decisions from the client while keeping presentation/input prechecks in the client, service orchestration at the API boundary, and transactional authority in existing routines.

The migration cannot switch this responsibility at deployment time. It must preserve the existing client/native path when `connected.enabled` is false and use a child `legacy | compare | service` mode when the parent is true ([AGENTS.md:87](../../../AGENTS.md#L87)).

## Evidence confidence

- `EXTRACTED`: Graphify locates the client `Checkout()` entry point and service `Reserve`, `Checkout`, and `Return` repository methods.
- `AMBIGUOUS`: Graphify could not parse `NativeRules.h` because of its export/calling-convention syntax. Direct inspection verifies the three legacy exports plus `CheckoutEligibilityReasonV1` ([NativeRules.h:17](../../../src/NativeRules/NativeRules.h#L17)).
