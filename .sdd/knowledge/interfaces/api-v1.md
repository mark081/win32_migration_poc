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

The service exposes versioned `/api/v1` routes for tools, members, reservations, checkouts, returns, audit, and health ([Controllers.cs:121](../../../src/AppServer/Controllers.cs#L121), [Controllers.cs:238](../../../src/AppServer/Controllers.cs#L238)). DTO data annotations validate shape and basic ranges at the service boundary ([Models.cs:6](../../../src/AppServer/Models.cs#L6)).

## Contract behavior

- Every call is protected by a static `X-Api-Key`; `X-Actor` is truncated audit context rather than verified user identity ([ApiKeyHandler.cs:18](../../../src/AppServer/ApiKeyHandler.cs#L18), [Controllers.cs:14](../../../src/AppServer/Controllers.cs#L14)).
- Every write requires a UUID `Idempotency-Key`, creates a request ID, and maps expected `TLxxx`, uniqueness, idempotency-conflict, and unexpected errors to stable HTTP categories ([Controllers.cs:28](../../../src/AppServer/Controllers.cs#L28), [Controllers.cs:46](../../../src/AppServer/Controllers.cs#L46)).
- Integration tests characterize DTO validation, generated identities, replay, reservation and checkout failures, returns, audit, and competing checkout behavior ([ApiTests.ps1:77](../../../tests/Integration/ApiTests.ps1#L77), [ApiTests.ps1:95](../../../tests/Integration/ApiTests.ps1#L95), [ApiTests.ps1:167](../../../tests/Integration/ApiTests.ps1#L167), [ApiTests.ps1:216](../../../tests/Integration/ApiTests.ps1#L216)).

## Current network limitation

The client hard-codes `localhost:8088`, requests plain HTTP, and sets no explicit WinHTTP timeouts ([main.cpp:127](../../../src/DesktopClient/main.cpp#L127)). This does not yet satisfy the Connected remote/TLS/failure-handling gate.
