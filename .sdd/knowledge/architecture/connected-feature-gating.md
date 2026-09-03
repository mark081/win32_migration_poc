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
  - resource: repo://src/AppServer/Capabilities.cs#L42-L166
  - resource: repo://src/AppServer/CheckoutDecisions.cs#L90-L390
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-03T22:04:44+00:00
status: draft
source_revision: c02893fb2fbaf460282c8d6fa4da3ef6f4b5c164
source_fingerprint: 5c9a61f0a37d9c8b8be61b6ae0208c38d7bb4bfc132744a2d45b8f448229e28e
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

The configured snapshot path is empty by default, so the public capability route reports a disabled parent and Legacy mode after deployment ([App.config:6](../../../src/AppServer/App.config#L6), [Capabilities.cs:63](../../../src/AppServer/Capabilities.cs#L63)). The route accepts a bounded client version only as a narrowing input and forces Legacy for missing or unsafe versions. It is routing metadata, not authorization, and no workflow router consumes it yet.

The capability route now uses the additive telemetry seam to emit separate `connected.enabled` and `connected.checkout.rule-mode` records with safe values, reason, configuration version, hashed server-owned practice context, and correlation ID ([Capabilities.cs:87](../../../src/AppServer/Capabilities.cs#L87), [Capabilities.cs:98](../../../src/AppServer/Capabilities.cs#L98)). Sink failure remains isolated, and telemetry is still not business audit or workflow state.

The checkout-decision route re-evaluates the parent and child from cached server state for every
request. A disabled/Legacy result or a mismatched configuration version returns
`CAPABILITY_STALE`; neither client capability text nor a Legacy observation can elevate the current
mode ([CheckoutDecisions.cs:136](../../../src/AppServer/CheckoutDecisions.cs#L136)). Compare mode
uses the Legacy observation only as its effective presentation result and records the independently
calculated service result; service mode uses the service result. Both remain advisory until the
unchanged PostgreSQL checkout command commits.
