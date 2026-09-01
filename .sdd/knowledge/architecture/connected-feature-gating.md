---
type: Architecture Policy
title: Connected feature-gate policy
description: Mandatory parent circuit breaker and child migration modes for Connected runtime behavior.
resource: repo://AGENTS.md
tags: [connected, feature-flags, rollback, observability]
sources:
  - resource: repo://AGENTS.md#L42-L103
  - resource: repo://AGENTS.md#L161-L163
  - resource: repo://AGENTS.md#L210-L215
  - resource: repo://src/AppServer/ConnectedFeatures.cs#L11-L24
  - resource: repo://src/AppServer/ConnectedFeatures.cs#L341-L472
  - resource: repo://src/AppServer/ConnectedTelemetry.cs#L13-L385
  - resource: repo://src/AppServer/App.config#L6-L12
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

All new or migratory Connected runtime behavior is subordinate to the service-authoritative `connected.enabled` parent flag. False, missing, expired, malformed, unreachable, or unevaluable state must preserve the Legacy baseline ([AGENTS.md:42](../../../AGENTS.md#L42), [AGENTS.md:52](../../../AGENTS.md#L52)).

## Evaluation boundary

- Effective behavior is `connected.enabled AND childFeatureEvaluation`; cohort targeting may narrow but never bypass the parent ([AGENTS.md:57](../../../AGENTS.md#L57)).
- The service evaluates the parent. A client may consume a capability decision for presentation or routing but cannot independently authorize Connected behavior ([AGENTS.md:81](../../../AGENTS.md#L81)).
- Evaluation uses a provider cache or versioned local snapshot rather than an external call per operation, with defined refresh/expiry and non-sensitive reason/version telemetry ([AGENTS.md:83](../../../AGENTS.md#L83)).
- Authentication, authorization, isolation, input validation, and audit remain unconditional security controls ([AGENTS.md:85](../../../AGENTS.md#L85)).

## Business-rule migration mode

A workflow child mode is `legacy`, `compare`, or `service` beneath the parent gate. Legacy preserves client/native behavior; compare shadows the service decision while performing one authoritative write; service uses the service result with a documented rollback path ([AGENTS.md:87](../../../AGENTS.md#L87)). Public `/api/v1` domain contracts should remain stable across modes unless an explicit capability response is designed ([AGENTS.md:103](../../../AGENTS.md#L103)).

## Implemented-state evidence

`EXTRACTED`: the application service now contains a provider-neutral, cached JSON snapshot evaluator with stable reason codes, parent dominance, child-mode validation, cohort narrowing, and bounded freshness/capability expiry ([ConnectedFeatures.cs:11](../../../src/AppServer/ConnectedFeatures.cs#L11), [ConnectedFeatures.cs:341](../../../src/AppServer/ConnectedFeatures.cs#L341)). Missing, malformed, stale, future-issued, inaccessible, and exception/timeout-like source failures resolve to a disabled parent and Legacy mode ([ConnectedFeatures.cs:367](../../../src/AppServer/ConnectedFeatures.cs#L367), [ConnectedFeatures.cs:454](../../../src/AppServer/ConnectedFeatures.cs#L454)).

The configured snapshot path is empty by default, so this foundation activates no Connected runtime behavior ([App.config:6](../../../src/AppServer/App.config#L6)). It is not yet wired to a public capability API or any workflow router; those remain planned migration steps.

The service also contains an additive telemetry seam for future flag and comparison evidence. It hashes raw cohort and normalized-input identities before storage, emits only explicit JSON fields, bounds metric-series cardinality, and isolates sink exceptions without retrying telemetry calls ([ConnectedTelemetry.cs:21](../../../src/AppServer/ConnectedTelemetry.cs#L21), [ConnectedTelemetry.cs:165](../../../src/AppServer/ConnectedTelemetry.cs#L165), [ConnectedTelemetry.cs:257](../../../src/AppServer/ConnectedTelemetry.cs#L257), [ConnectedTelemetry.cs:339](../../../src/AppServer/ConnectedTelemetry.cs#L339)). This seam is not connected to feature evaluation or business operations yet, so it produces no runtime behavior at this wave.
