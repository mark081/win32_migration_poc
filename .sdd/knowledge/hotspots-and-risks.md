---
type: Risk Assessment
title: Responsibility-migration hotspots and risks
description: High-impact dependencies and failure modes for removing duplicated client domain logic.
sources:
  - resource: repo://src/DesktopClient/main.cpp#L124-L165
  - resource: repo://src/DesktopClient/main.cpp#L218-L446
  - resource: repo://src/NativeRules/NativeRules.cpp#L9-L28
  - resource: repo://src/AppServer/Repository.cs#L174-L216
  - resource: repo://src/AppServer/Repository.cs#L272-L321
  - resource: repo://src/AppServer/CheckoutRules.cs#L91-L153
  - resource: repo://src/AppServer/Capabilities.cs#L42-L166
  - resource: repo://src/AppServer/CheckoutDecisions.cs#L90-L390
  - resource: repo://src/DesktopClient/ClientTransport.cpp#L171-L354
  - resource: repo://src/DesktopClient/CapabilityRouter.cpp#L117-L188
  - resource: repo://src/DesktopClient/CheckoutMode.cpp#L36-L141
  - resource: repo://src/AppServer/Controllers.cs#L49-L83
  - resource: repo://database/002_routines.sql#L23-L42
  - resource: repo://docs/testing-evolution.md#L47-L61
  - resource: repo://AGENTS.md#L42-L103
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-04T02:50:11+00:00
status: draft
source_revision: abfa05c4e2f2554280a05f173ad8795452ab41a1
source_fingerprint: b906083c8b29b6cfcf3a1a53d5c57116a64f5e8dfc33b5eb2577817b28777e38
source_worktree: dirty
curation_status: generated
---

# Hotspots

1. **Checkout client gate:** `Checkout()` selects Legacy, compare, or service from the cached capability. Only Legacy/compare fetch member policy state and call NativeRules; service adds a network decision before confirmation and therefore has distinct denial, invalid-response, and unavailable behavior ([main.cpp:394](../../src/DesktopClient/main.cpp#L394)).
2. **Duplicated decision table:** tier limits and eligibility exist in both C++ and SQL ([NativeRules.cpp:9](../../src/NativeRules/NativeRules.cpp#L9), [002_routines.sql:3](../../database/002_routines.sql#L3)). The database already supplies limits in `MemberDto`, which can accidentally preserve policy leakage into the client ([Repository.cs:174](../../src/AppServer/Repository.cs#L174)).
3. **Transport coupling:** endpoint parsing, HTTPS enforcement, separate Connected credentials, bounded timeouts, failure categories, same-key bounded replay, and capability routing now share the desktop HTTP path. A current compare/service capability selects Connected; expiry or any validation/transport failure returns to Legacy ([CapabilityRouter.cpp:124](../../src/DesktopClient/CapabilityRouter.cpp#L124), [main.cpp:130](../../src/DesktopClient/main.cpp#L130)).
4. **Test coupling:** `NativeRulesTests` protects decision-table parity; removal requires replacement service-level characterization before retirement ([testing-evolution.md:60](../../docs/testing-evolution.md#L60)).
5. **Scope ambiguity:** “all business logic” could mistakenly include required-field checks, confirmation, formatting, and local failure presentation, despite policy assigning immediate operator validation to the client.
6. **Decision/write race:** a valid service allow can become stale before the operator confirms. The unchanged checkout command deliberately lets PostgreSQL revalidate and return a conflict; a serialization abort is mapped to `409 CONCURRENT_UPDATE` only after PostgreSQL has rolled back ([main.cpp:394](../../src/DesktopClient/main.cpp#L394), [Controllers.cs:49](../../src/AppServer/Controllers.cs#L49)).

# Risks

- A broad rewrite could alter error messages, confirmation flow, or stable API outcomes.
- Moving transactional decisions from stored routines into C# could weaken locking and create dual rule authority.
- Retiring the DLL before service parity tests exist would remove permanent regression evidence.
- Changing client request flow without timeout/retry/idempotency handling could turn clear local rejection into ambiguous network failure.
- An invalid or stale flag snapshot could accidentally activate Connected behavior unless every failure defaults to false; a client-side mode decision could also bypass service authority.
- Compare mode could duplicate a business write if shadow evaluation is coupled to command execution rather than a side-effect-free decision contract.
- Service-mode response parsing must continue to reject stale configuration, unknown reasons, and contradictory allow/reason pairs so routing metadata never becomes client authorization.
