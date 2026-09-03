---
type: Risk Assessment
title: Responsibility-migration hotspots and risks
description: High-impact dependencies and failure modes for removing duplicated client domain logic.
sources:
  - resource: repo://src/DesktopClient/main.cpp#L124-L165
  - resource: repo://src/DesktopClient/main.cpp#L349-L373
  - resource: repo://src/NativeRules/NativeRules.cpp#L9-L28
  - resource: repo://src/AppServer/Repository.cs#L174-L216
  - resource: repo://src/AppServer/Repository.cs#L272-L321
  - resource: repo://src/AppServer/CheckoutRules.cs#L91-L153
  - resource: repo://src/AppServer/Capabilities.cs#L42-L166
  - resource: repo://src/AppServer/CheckoutDecisions.cs#L90-L390
  - resource: repo://src/DesktopClient/ClientTransport.cpp#L171-L354
  - resource: repo://src/DesktopClient/CapabilityRouter.cpp#L117-L188
  - resource: repo://src/DesktopClient/CheckoutMode.cpp#L36-L74
  - resource: repo://database/002_routines.sql#L23-L42
  - resource: repo://docs/testing-evolution.md#L47-L61
  - resource: repo://AGENTS.md#L42-L103
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-03T22:04:44+00:00
status: draft
source_revision: c02893fb2fbaf460282c8d6fa4da3ef6f4b5c164
source_fingerprint: 5c9a61f0a37d9c8b8be61b6ae0208c38d7bb4bfc132744a2d45b8f448229e28e
source_worktree: dirty
curation_status: generated
---

# Hotspots

1. **Checkout client gate:** `Checkout()` fetches member state and prevents the POST when `NativeRules` says the member is ineligible ([main.cpp:349](../../src/DesktopClient/main.cpp#L349)). Removing this changes timing and presentation of rejection even if the authoritative outcome is unchanged.
2. **Duplicated decision table:** tier limits and eligibility exist in both C++ and SQL ([NativeRules.cpp:9](../../src/NativeRules/NativeRules.cpp#L9), [002_routines.sql:3](../../database/002_routines.sql#L3)). The database already supplies limits in `MemberDto`, which can accidentally preserve policy leakage into the client ([Repository.cs:174](../../src/AppServer/Repository.cs#L174)).
3. **Transport coupling:** endpoint parsing, HTTPS enforcement, separate Connected credentials, bounded timeouts, failure categories, same-key bounded replay, and capability routing now share the desktop HTTP path. A current compare/service capability selects Connected; expiry or any validation/transport failure returns to Legacy ([CapabilityRouter.cpp:124](../../src/DesktopClient/CapabilityRouter.cpp#L124), [main.cpp:130](../../src/DesktopClient/main.cpp#L130)).
4. **Test coupling:** `NativeRulesTests` protects decision-table parity; removal requires replacement service-level characterization before retirement ([testing-evolution.md:60](../../docs/testing-evolution.md#L60)).
5. **Scope ambiguity:** “all business logic” could mistakenly include required-field checks, confirmation, formatting, and local failure presentation, despite policy assigning immediate operator validation to the client.
6. **Partially integrated migration path:** Legacy and compare checkout are integrated, including one native observation and read-only comparison call, but service mode still follows the native path. Task 5.2 must remove the native decision call only in service mode while retaining PostgreSQL command authority ([CheckoutMode.cpp:36](../../src/DesktopClient/CheckoutMode.cpp#L36), [main.cpp:350](../../src/DesktopClient/main.cpp#L350)).

# Risks

- A broad rewrite could alter error messages, confirmation flow, or stable API outcomes.
- Moving transactional decisions from stored routines into C# could weaken locking and create dual rule authority.
- Retiring the DLL before service parity tests exist would remove permanent regression evidence.
- Changing client request flow without timeout/retry/idempotency handling could turn clear local rejection into ambiguous network failure.
- An invalid or stale flag snapshot could accidentally activate Connected behavior unless every failure defaults to false; a client-side mode decision could also bypass service authority.
- Compare mode could duplicate a business write if shadow evaluation is coupled to command execution rather than a side-effect-free decision contract.
