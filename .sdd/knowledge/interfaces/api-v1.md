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
  - resource: repo://src/AppServer/CheckoutDecisions.cs#L44-L390
  - resource: repo://src/DesktopClient/ClientTransport.cpp#L171-L354
  - resource: repo://src/DesktopClient/CapabilityRouter.cpp#L124-L188
  - resource: repo://src/DesktopClient/CheckoutMode.cpp#L36-L141
  - resource: repo://src/DesktopClient/main.cpp#L218-L446
  - resource: repo://tests/Integration/ApiTests.ps1#L70-L239
  - resource: repo://AGENTS.md#L81-L103
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

The service exposes versioned `/api/v1` routes for tools, members, reservations, checkouts, returns, audit, health, Connected routing capabilities, and read-only checkout decisions ([Controllers.cs:121](../../../src/AppServer/Controllers.cs#L121), [Capabilities.cs:126](../../../src/AppServer/Capabilities.cs#L126), [CheckoutDecisions.cs:322](../../../src/AppServer/CheckoutDecisions.cs#L322)). DTO data annotations validate shape and basic ranges at the service boundary ([Models.cs:6](../../../src/AppServer/Models.cs#L6)).

## Contract behavior

- Every call is protected by a static `X-Api-Key`; `X-Actor` is truncated audit context rather than verified user identity ([ApiKeyHandler.cs:18](../../../src/AppServer/ApiKeyHandler.cs#L18), [Controllers.cs:14](../../../src/AppServer/Controllers.cs#L14)).
- Every write requires a UUID `Idempotency-Key`, creates a request ID, and maps expected `TLxxx`, uniqueness, idempotency-conflict, and unexpected errors to stable HTTP categories. A PostgreSQL `40001` serialization abort is already rolled back and maps to `409 CONCURRENT_UPDATE` rather than an unexpected failure ([Controllers.cs:28](../../../src/AppServer/Controllers.cs#L28), [Controllers.cs:49](../../../src/AppServer/Controllers.cs#L49)).
- Integration tests characterize DTO validation, generated identities, replay, reservation and checkout failures, returns, audit, and competing checkout behavior ([ApiTests.ps1:77](../../../tests/Integration/ApiTests.ps1#L77), [ApiTests.ps1:95](../../../tests/Integration/ApiTests.ps1#L95), [ApiTests.ps1:167](../../../tests/Integration/ApiTests.ps1#L167), [ApiTests.ps1:216](../../../tests/Integration/ApiTests.ps1#L216)).
- `GET /api/v1/capabilities` is protected by the same API-key handler and returns schema/configuration versions, evaluation/expiry times, effective parent and child routing values, a safe reason, and a correlation ID. Missing, malformed, or overlong `X-Client-Version` values force a disabled/Legacy response; request headers cannot elevate the evaluator's service-owned result ([Capabilities.cs:42](../../../src/AppServer/Capabilities.cs#L42), [Capabilities.cs:63](../../../src/AppServer/Capabilities.cs#L63), [Capabilities.cs:126](../../../src/AppServer/Capabilities.cs#L126)).
- Capability responses exclude practice keys, raw targeting values, provider data, credentials, and business authorization. A valid UUID `X-Correlation-ID` is preserved; other values are replaced before diagnostics ([Capabilities.cs:25](../../../src/AppServer/Capabilities.cs#L25), [Capabilities.cs:159](../../../src/AppServer/Capabilities.cs#L159)).
- `POST /api/v1/checkout-decisions` requires no idempotency key because it performs no write. It re-evaluates service-owned capability state, returns versioned allow/deny facts for compare or service mode, reports stale routing as `409 CAPABILITY_STALE`, unavailable PostgreSQL reads as `503 DECISION_UNAVAILABLE`, and unexpected failures without raw exception details ([CheckoutDecisions.cs:136](../../../src/AppServer/CheckoutDecisions.cs#L136), [CheckoutDecisions.cs:322](../../../src/AppServer/CheckoutDecisions.cs#L322)).
- The compare client sends contract version 1, member/date input, its cached configuration version, and exactly one structured native observation. It accepts the response only when mode, reason, and result agree with that Legacy observation; otherwise it discards the capability and retains Legacy behavior ([CheckoutMode.cpp:63](../../../src/DesktopClient/CheckoutMode.cpp#L63), [main.cpp:186](../../../src/DesktopClient/main.cpp#L186)).
- The service client sends member/date input and its cached configuration version without a native observation, member policy fields, tool ID, or idempotency key. It accepts only a matching version 1 `service` response whose allow value and stable reason agree; denial and failure never submit the checkout command ([CheckoutMode.cpp:85](../../../src/DesktopClient/CheckoutMode.cpp#L85), [main.cpp:218](../../../src/DesktopClient/main.cpp#L218)).

## Current network boundary

The client defaults its Legacy endpoint to `http://localhost:8088/` but accepts an external Legacy
URL. An optional Connected endpoint must be HTTPS and have its own readable, non-empty credential
file. WinHTTP uses explicit bounded timeouts and its normal certificate/hostname checks; failures
have stable categories and a keyed ambiguous write is replayed at most once with the same key
([ClientTransport.cpp:284](../../../src/DesktopClient/ClientTransport.cpp#L284),
[ClientTransport.cpp:328](../../../src/DesktopClient/ClientTransport.cpp#L328)). The client sends its
bounded version header, accepts only current schema 1 compare/service capability responses, and
routes every other response or transport failure to Legacy ([CapabilityRouter.cpp:124](../../../src/DesktopClient/CapabilityRouter.cpp#L124), [CapabilityRouter.cpp:137](../../../src/DesktopClient/CapabilityRouter.cpp#L137)).

## Migration compatibility constraint

The service remains authoritative for the parent feature decision. The additive capability response is routing metadata rather than domain authorization, and existing clients need not call it; existing workflow payloads and outcomes remain unchanged ([AGENTS.md:81](../../../AGENTS.md#L81), [Capabilities.cs:21](../../../src/AppServer/Capabilities.cs#L21)).
