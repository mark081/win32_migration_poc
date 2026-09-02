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
  - resource: repo://src/AppServer/Capabilities.cs#L25-L166
  - resource: repo://src/DesktopClient/main.cpp#L124-L165
  - resource: repo://tests/Integration/ApiTests.ps1#L70-L239
  - resource: repo://AGENTS.md#L81-L103
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-02T16:37:01+00:00
status: draft
source_revision: 5fc24fa195f089ac9f1fbe59d50df9a15ef403e3
source_fingerprint: 93092c2d642772bd4df19a24befe6d8185a664e40c7abd21a873a0fadfbc2925
source_worktree: dirty
curation_status: generated
---

# Summary

The service exposes versioned `/api/v1` routes for tools, members, reservations, checkouts, returns, audit, health, and additive Connected routing capabilities ([Controllers.cs:121](../../../src/AppServer/Controllers.cs#L121), [Capabilities.cs:126](../../../src/AppServer/Capabilities.cs#L126)). DTO data annotations validate shape and basic ranges at the service boundary ([Models.cs:6](../../../src/AppServer/Models.cs#L6)).

## Contract behavior

- Every call is protected by a static `X-Api-Key`; `X-Actor` is truncated audit context rather than verified user identity ([ApiKeyHandler.cs:18](../../../src/AppServer/ApiKeyHandler.cs#L18), [Controllers.cs:14](../../../src/AppServer/Controllers.cs#L14)).
- Every write requires a UUID `Idempotency-Key`, creates a request ID, and maps expected `TLxxx`, uniqueness, idempotency-conflict, and unexpected errors to stable HTTP categories ([Controllers.cs:28](../../../src/AppServer/Controllers.cs#L28), [Controllers.cs:46](../../../src/AppServer/Controllers.cs#L46)).
- Integration tests characterize DTO validation, generated identities, replay, reservation and checkout failures, returns, audit, and competing checkout behavior ([ApiTests.ps1:77](../../../tests/Integration/ApiTests.ps1#L77), [ApiTests.ps1:95](../../../tests/Integration/ApiTests.ps1#L95), [ApiTests.ps1:167](../../../tests/Integration/ApiTests.ps1#L167), [ApiTests.ps1:216](../../../tests/Integration/ApiTests.ps1#L216)).
- `GET /api/v1/capabilities` is protected by the same API-key handler and returns schema/configuration versions, evaluation/expiry times, effective parent and child routing values, a safe reason, and a correlation ID. Missing, malformed, or overlong `X-Client-Version` values force a disabled/Legacy response; request headers cannot elevate the evaluator's service-owned result ([Capabilities.cs:42](../../../src/AppServer/Capabilities.cs#L42), [Capabilities.cs:63](../../../src/AppServer/Capabilities.cs#L63), [Capabilities.cs:126](../../../src/AppServer/Capabilities.cs#L126)).
- Capability responses exclude practice keys, raw targeting values, provider data, credentials, and business authorization. A valid UUID `X-Correlation-ID` is preserved; other values are replaced before diagnostics ([Capabilities.cs:25](../../../src/AppServer/Capabilities.cs#L25), [Capabilities.cs:159](../../../src/AppServer/Capabilities.cs#L159)).

## Current network limitation

The client hard-codes `localhost:8088`, requests plain HTTP, and sets no explicit WinHTTP timeouts ([main.cpp:127](../../../src/DesktopClient/main.cpp#L127)). This does not yet satisfy the Connected remote/TLS/failure-handling gate.

## Migration compatibility constraint

The service remains authoritative for the parent feature decision. The additive capability response is routing metadata rather than domain authorization, and existing clients need not call it; existing workflow payloads and outcomes remain unchanged ([AGENTS.md:81](../../../AGENTS.md#L81), [Capabilities.cs:21](../../../src/AppServer/Capabilities.cs#L21)).
