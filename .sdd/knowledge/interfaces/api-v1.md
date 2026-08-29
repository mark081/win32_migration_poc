---
type: Interface
title: Version 1 HTTP API
description: Current desktop-to-service contract, authentication, idempotency, and error behavior.
resource: repo://src/AppServer/
tags: [api-v1, http, authentication, idempotency]
sources:
  - resource: repo://src/AppServer/Controllers.cs#L10-L246
  - resource: repo://src/AppServer/Models.cs#L6-L112
  - resource: repo://src/AppServer/ApiKeyHandler.cs#L10-L24
  - resource: repo://src/DesktopClient/main.cpp#L124-L165
  - resource: repo://tests/Integration/ApiTests.ps1#L70-L239
  - resource: repo://AGENTS.md#L81-L103
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-08-29T22:25:34.1947474+00:00
status: draft
source_revision: 60c93421a8798b983091d7971a3f079d010579e8
source_fingerprint: 080c303d93f7bcd7e5c6b158ac8e39f35a5fe7b68a8da9ebb08586f34678428a
source_worktree: dirty
curation_status: generated
---

# Summary

The service exposes versioned `/api/v1` routes for tools, members, reservations, checkouts, returns, audit, and health ([Controllers.cs:121](../../../src/AppServer/Controllers.cs#L121), [Controllers.cs:238](../../../src/AppServer/Controllers.cs#L238)). DTO data annotations validate shape and basic ranges at the service boundary ([Models.cs:6](../../../src/AppServer/Models.cs#L6)).

## Contract behavior

- Every call is protected by a static `X-Api-Key`; `X-Actor` is truncated audit context rather than verified user identity ([ApiKeyHandler.cs:18](../../../src/AppServer/ApiKeyHandler.cs#L18), [Controllers.cs:14](../../../src/AppServer/Controllers.cs#L14)).
- Every write requires a UUID `Idempotency-Key`, creates a request ID, and maps expected `TLxxx`, uniqueness, idempotency-conflict, and unexpected errors to stable HTTP categories ([Controllers.cs:28](../../../src/AppServer/Controllers.cs#L28), [Controllers.cs:46](../../../src/AppServer/Controllers.cs#L46)).
- Integration tests characterize DTO validation, generated identities, replay, reservation and checkout failures, returns, audit, and competing checkout behavior ([ApiTests.ps1:77](../../../tests/Integration/ApiTests.ps1#L77), [ApiTests.ps1:95](../../../tests/Integration/ApiTests.ps1#L95), [ApiTests.ps1:167](../../../tests/Integration/ApiTests.ps1#L167), [ApiTests.ps1:216](../../../tests/Integration/ApiTests.ps1#L216)).

## Current network limitation

The client hard-codes `localhost:8088`, requests plain HTTP, and sets no explicit WinHTTP timeouts ([main.cpp:127](../../../src/DesktopClient/main.cpp#L127)). This does not yet satisfy the Connected remote/TLS/failure-handling gate.

## Migration compatibility constraint

The service must remain authoritative for the parent feature decision, but flag values and migration internals should not leak into domain payloads. Any client capability response must be explicitly versioned while existing `/api/v1` outcomes remain stable across `legacy`, `compare`, and `service` modes ([AGENTS.md:81](../../../AGENTS.md#L81), [AGENTS.md:103](../../../AGENTS.md#L103)).
