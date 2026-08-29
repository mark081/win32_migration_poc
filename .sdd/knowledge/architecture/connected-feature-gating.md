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
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T23:23:48.5187993+00:00
status: draft
source_revision: b8c67274c7ff3579be20e4811fbd93f2d0c5e698
source_fingerprint: cdec585793918f2fcb353b631b7d61f27993af00d37a19ba1c40a5d0a2081a85
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

`EXTRACTED`: a repository-wide direct source search at revision `60c93421a8798b983091d7971a3f079d010579e8` found `connected.enabled` and migration-mode references only in `AGENTS.md`; no runtime evaluator or provider integration is currently implemented. Planning must therefore include the minimum evaluation/capability plumbing before migrated rule execution.
