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
  - resource: repo://src/AppServer/Repository.cs#L272-L321
  - resource: repo://src/AppServer/CheckoutRules.cs#L1-L153
  - resource: repo://src/AppServer/ConnectedFeatures.cs#L341-L472
  - resource: repo://src/AppServer/ConnectedTelemetry.cs#L13-L453
  - resource: repo://src/AppServer/Capabilities.cs#L25-L166
  - resource: repo://src/AppServer/CheckoutDecisions.cs#L44-L390
  - resource: repo://src/DesktopClient/ClientTransport.cpp#L57-L354
  - resource: repo://src/NativeRules/NativeRules.h#L8-L26
  - resource: repo://database/002_routines.sql#L3-L56
  - resource: repo://docs/architecture.md#L26-L54
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-02T23:45:00+00:00
status: draft
source_revision: f53a98427070af5f64bdce85b015fc66ca863210
source_fingerprint: 120f1a554cacbd7eaac6ef03c18228d1cbdd20d65d0e01b22a22c629dd4c2a7b
source_worktree: dirty
curation_status: generated
---

# Summary

Business behavior is deliberately distributed. Client and DLL checks provide immediate feedback, the service governs the HTTP workflow, and stored routines remain authoritative for concurrency-sensitive decisions ([architecture.md:26](../../../docs/architecture.md#L26)).

## Responsibilities

- **Win32 client:** collects values, enforces required-field and numeric-shape checks, asks for confirmation, renders responses, and constructs API requests. It still makes the Legacy checkout-eligibility decision and routes every product call to the Legacy endpoint. The transport now parses an external Legacy URL plus an optional HTTPS Connected URL, requires a separate Connected credential, applies bounded timeouts, and classifies failures; Connected selection is not implemented yet ([main.cpp:126](../../../src/DesktopClient/main.cpp#L126), [ClientTransport.cpp:279](../../../src/DesktopClient/ClientTransport.cpp#L279), [ClientTransport.cpp:323](../../../src/DesktopClient/ClientTransport.cpp#L323)).
- **NativeRules DLL:** implements tier checkout limits, maximum loan durations, and eligibility from active/overdue/open-loan/tier inputs. Its versioned structured export distinguishes allowed, inactive, overdue, limit-reached, and unsupported-tier results while the legacy boolean export delegates to it ([NativeRules.h:8](../../../src/NativeRules/NativeRules.h#L8), [NativeRules.cpp:25](../../../src/NativeRules/NativeRules.cpp#L25)).
- **Application service:** validates request DTOs, authenticates calls, coordinates idempotent transactions, calls stored routines, and translates stable database failures into HTTP responses ([Models.cs:6](../../../src/AppServer/Models.cs#L6), [Controllers.cs:46](../../../src/AppServer/Controllers.cs#L46)). The authenticated capability route returns routing metadata only. The new authenticated decision route re-evaluates that state, performs the existing read-only member query, calculates a service result, and emits compare evidence when applicable; it never submits a workflow command ([Capabilities.cs:63](../../../src/AppServer/Capabilities.cs#L63), [CheckoutDecisions.cs:136](../../../src/AppServer/CheckoutDecisions.cs#L136), [CheckoutDecisions.cs:322](../../../src/AppServer/CheckoutDecisions.cs#L322)).
- **PostgreSQL:** owns tier functions and locked reservation, checkout, return, fee, audit, and durable state transitions ([002_routines.sql:3](../../../database/002_routines.sql#L3), [002_routines.sql:23](../../../database/002_routines.sql#L23), [002_routines.sql:45](../../../database/002_routines.sql#L45)).

## Ownership implication

Moving authoritative decisions into the application service would conflict with the current Connected policy if it displaced locked PostgreSQL rules. A compatible interpretation is to remove duplicated domain decisions from the client while keeping presentation/input prechecks in the client, service orchestration at the API boundary, and transactional authority in existing routines.

The migration cannot switch this responsibility at deployment time. It must preserve the existing client/native path when `connected.enabled` is false and use a child `legacy | compare | service` mode when the parent is true ([AGENTS.md:87](../../../AGENTS.md#L87)).

## Evidence confidence

- `EXTRACTED`: Graphify locates the client `Checkout()` entry point and service `Reserve`, `Checkout`, and `Return` repository methods.
- `EXTRACTED`: Graphify locates the new `CheckoutRuleEvaluator` and `GetMemberEligibilityContext()` read. Direct source inspection verifies that neither calls a workflow write.
- `EXTRACTED`: Graphify locates `CapabilitiesController`, `CapabilityService`, the cached evaluator, and telemetry sink. Direct inspection verifies the route has no repository or command dependency.
- `EXTRACTED`: Graphify locates the checkout-decision route, service evaluator/read dependencies, comparison telemetry, and client transport. Direct inspection verifies the decision path has no write call and the transport retains WinHTTP's default TLS validation.
- `AMBIGUOUS`: Graphify could not parse `NativeRules.h` because of its export/calling-convention syntax. Direct inspection verifies the three legacy exports plus `CheckoutEligibilityReasonV1` ([NativeRules.h:17](../../../src/NativeRules/NativeRules.h#L17)).
