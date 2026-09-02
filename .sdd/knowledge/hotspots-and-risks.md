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
  - resource: repo://database/002_routines.sql#L23-L42
  - resource: repo://docs/testing-evolution.md#L47-L61
  - resource: repo://AGENTS.md#L42-L103
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-02T16:37:01+00:00
status: draft
source_revision: 5fc24fa195f089ac9f1fbe59d50df9a15ef403e3
source_fingerprint: 93092c2d642772bd4df19a24befe6d8185a664e40c7abd21a873a0fadfbc2925
source_worktree: dirty
curation_status: generated
---

# Hotspots

1. **Checkout client gate:** `Checkout()` fetches member state and prevents the POST when `NativeRules` says the member is ineligible ([main.cpp:349](../../src/DesktopClient/main.cpp#L349)). Removing this changes timing and presentation of rejection even if the authoritative outcome is unchanged.
2. **Duplicated decision table:** tier limits and eligibility exist in both C++ and SQL ([NativeRules.cpp:9](../../src/NativeRules/NativeRules.cpp#L9), [002_routines.sql:3](../../database/002_routines.sql#L3)). The database already supplies limits in `MemberDto`, which can accidentally preserve policy leakage into the client ([Repository.cs:174](../../src/AppServer/Repository.cs#L174)).
3. **Transport coupling:** hard-coded localhost HTTP and absent explicit timeouts mean removing local prechecks increases dependence on a network path that is not yet Connected-ready ([main.cpp:124](../../src/DesktopClient/main.cpp#L124)).
4. **Test coupling:** `NativeRulesTests` protects decision-table parity; removal requires replacement service-level characterization before retirement ([testing-evolution.md:60](../../docs/testing-evolution.md#L60)).
5. **Scope ambiguity:** “all business logic” could mistakenly include required-field checks, confirmation, formatting, and local failure presentation, despite policy assigning immediate operator validation to the client.
6. **Partially integrated service foundations:** the authenticated capability route now exposes the cached service-owned routing decision and telemetry, but no client router or checkout-decision route consumes it. The service rule evaluator remains unwired from product workflow, and the empty default snapshot keeps routing Legacy ([Capabilities.cs:63](../../src/AppServer/Capabilities.cs#L63), [Repository.cs:272](../../src/AppServer/Repository.cs#L272), [CheckoutRules.cs:117](../../src/AppServer/CheckoutRules.cs#L117)).

# Risks

- A broad rewrite could alter error messages, confirmation flow, or stable API outcomes.
- Moving transactional decisions from stored routines into C# could weaken locking and create dual rule authority.
- Retiring the DLL before service parity tests exist would remove permanent regression evidence.
- Changing client request flow without timeout/retry/idempotency handling could turn clear local rejection into ambiguous network failure.
- An invalid or stale flag snapshot could accidentally activate Connected behavior unless every failure defaults to false; a client-side mode decision could also bypass service authority.
- Compare mode could duplicate a business write if shadow evaluation is coupled to command execution rather than a side-effect-free decision contract.
