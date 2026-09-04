---
type: Architecture Component
title: Runtime components and responsibility boundaries
description: Current responsibilities of the Win32 client, NativeRules DLL, application service, and PostgreSQL.
resource: repo://src/
tags: [client, native-rules, app-server, database]
sources:
  - resource: repo://src/DesktopClient/main.cpp#L218-L446
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
  - resource: repo://src/DesktopClient/CapabilityRouter.cpp#L117-L188
  - resource: repo://src/DesktopClient/CheckoutMode.cpp#L36-L141
  - resource: repo://src/NativeRules/NativeRules.h#L8-L26
  - resource: repo://database/002_routines.sql#L3-L56
  - resource: repo://docs/architecture.md#L26-L54
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-04T02:50:11+00:00
status: draft
source_revision: e83f34f7b78525afe5ded64b61c785176c58f0c6
source_fingerprint: 158d4ba6c2507188982256829a775edf51f7b072c2d5c8d1f8c78009445477fe
source_worktree: dirty
curation_status: generated
---

# Summary

Business behavior is deliberately distributed. Client and DLL checks provide immediate feedback, the service governs the HTTP workflow, and stored routines remain authoritative for concurrency-sensitive decisions ([architecture.md:26](../../../docs/architecture.md#L26)).

## Responsibilities

- **Win32 client:** collects values, enforces required-field and numeric/date-shape checks, asks for confirmation, renders responses, and constructs API requests. When Connected is configured, a process-local router fetches the authenticated capability, accepts only current schema 1 `compare` or `service` responses, and otherwise sends product calls to Legacy. The transport retains separate endpoint credentials, bounded timeouts, and normal TLS validation ([main.cpp:130](../../../src/DesktopClient/main.cpp#L130), [CapabilityRouter.cpp:124](../../../src/DesktopClient/CapabilityRouter.cpp#L124), [CapabilityRouter.cpp:137](../../../src/DesktopClient/CapabilityRouter.cpp#L137)).
  Legacy and compare modes call the structured native rule once; compare also sends that observation to the read-only decision route and retains the native result on comparison failure. Service mode does not fetch member policy fields or call NativeRules. It accepts only a matching version 1 service result with a known consistent reason; failures invalidate the capability and stop the attempt. Every allow still reaches the same confirmation and at most one idempotent checkout command ([CheckoutMode.cpp:79](../../../src/DesktopClient/CheckoutMode.cpp#L79), [main.cpp:218](../../../src/DesktopClient/main.cpp#L218), [main.cpp:394](../../../src/DesktopClient/main.cpp#L394)).
- **NativeRules DLL:** implements tier checkout limits, maximum loan durations, and eligibility from active/overdue/open-loan/tier inputs. Its versioned structured export distinguishes allowed, inactive, overdue, limit-reached, and unsupported-tier results while the legacy boolean export delegates to it ([NativeRules.h:8](../../../src/NativeRules/NativeRules.h#L8), [NativeRules.cpp:25](../../../src/NativeRules/NativeRules.cpp#L25)).
- **Application service:** validates request DTOs, authenticates calls, coordinates idempotent transactions, calls stored routines, and translates stable database failures into HTTP responses, including a rolled-back PostgreSQL serialization abort as `409 CONCURRENT_UPDATE` ([Models.cs:6](../../../src/AppServer/Models.cs#L6), [Controllers.cs:49](../../../src/AppServer/Controllers.cs#L49)). The authenticated capability route returns routing metadata only. The authenticated decision route re-evaluates that state, performs the existing read-only member query, calculates a service result, and emits compare evidence when applicable; it never submits a workflow command ([Capabilities.cs:63](../../../src/AppServer/Capabilities.cs#L63), [CheckoutDecisions.cs:136](../../../src/AppServer/CheckoutDecisions.cs#L136), [CheckoutDecisions.cs:322](../../../src/AppServer/CheckoutDecisions.cs#L322)).
- **PostgreSQL:** owns tier functions and locked reservation, checkout, return, fee, audit, and durable state transitions ([002_routines.sql:3](../../../database/002_routines.sql#L3), [002_routines.sql:23](../../../database/002_routines.sql#L23), [002_routines.sql:45](../../../database/002_routines.sql#L45)).

## Ownership implication

Moving authoritative decisions into the application service would conflict with the current Connected policy if it displaced locked PostgreSQL rules. A compatible interpretation is to remove duplicated domain decisions from the client while keeping presentation/input prechecks in the client, service orchestration at the API boundary, and transactional authority in existing routines.

The migration cannot switch this responsibility at deployment time. It must preserve the existing client/native path when `connected.enabled` is false and use a child `legacy | compare | service` mode when the parent is true ([AGENTS.md:87](../../../AGENTS.md#L87)).

## Evidence confidence

- `EXTRACTED`: Graphify locates the client `Checkout()` entry point and service `Reserve`, `Checkout`, and `Return` repository methods.
- `EXTRACTED`: Graphify locates the new `CheckoutRuleEvaluator` and `GetMemberEligibilityContext()` read. Direct source inspection verifies that neither calls a workflow write.
- `EXTRACTED`: Graphify locates `CapabilitiesController`, `CapabilityService`, the cached evaluator, and telemetry sink. Direct inspection verifies the route has no repository or command dependency.
- `EXTRACTED`: Graphify locates the checkout-decision route, service evaluator/read dependencies, comparison telemetry, and client transport. Direct inspection verifies the decision path has no write call and the transport retains WinHTTP's default TLS validation.
- `EXTRACTED`: Graphify locates `EndpointRouter`, its cache, the WinHTTP capability bootstrap, and the UI `Http()` routing call. Direct inspection verifies every rejected or expired response returns to Legacy.
- `EXTRACTED`: Graphify locates the native observation adapter, compare serializer, decision call, and single checkout command call. Direct inspection verifies the decision body contains no tool ID or idempotency key.
- `EXTRACTED`: Graphify locates the service-mode branch, request builder, response validator, stable message mapping, and unchanged checkout POST. Direct inspection verifies NativeRules and member-policy reads occur only in Legacy/compare modes.
- `AMBIGUOUS`: Graphify could not parse `NativeRules.h` because of its export/calling-convention syntax. Direct inspection verifies the three legacy exports plus `CheckoutEligibilityReasonV1` ([NativeRules.h:17](../../../src/NativeRules/NativeRules.h#L17)).
