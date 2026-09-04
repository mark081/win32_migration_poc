---
type: System Overview
title: Tool Lending Connected-stage baseline
description: Runtime components, entry points, and principal boundaries.
resource: repo://.
tags: [win32, service, postgresql, connected]
sources:
  - resource: repo://README.md#L16-L59
  - resource: repo://src/DesktopClient/ClientTransport.cpp#L171-L354
  - resource: repo://src/DesktopClient/CapabilityRouter.cpp#L117-L188
  - resource: repo://src/DesktopClient/CheckoutMode.cpp#L36-L141
  - resource: repo://src/DesktopClient/main.cpp#L218-L446
  - resource: repo://src/AppServer/Program.cs#L14-L37
  - resource: repo://src/AppServer/Capabilities.cs#L126-L166
  - resource: repo://src/AppServer/CheckoutDecisions.cs#L322-L390
  - resource: repo://docs/architecture.md#L1-L26
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-04T02:50:11+00:00
status: draft
source_revision: abfa05c4e2f2554280a05f173ad8795452ab41a1
source_fingerprint: b906083c8b29b6cfcf3a1a53d5c57116a64f5e8dfc33b5eb2577817b28777e38
source_worktree: dirty
curation_status: generated
---

# Summary

The application is a Windows/x86 client-server system. A native Win32 client calls a self-hosted .NET Framework 4.8 Web API service, and only that service accesses PostgreSQL ([README.md:16](../../README.md#L16), [README.md:58](../../README.md#L58)). PostgreSQL holds durable workflow state and concurrency-sensitive business rules ([README.md:126](../../README.md#L126)).

## Entry points

- The interactive client starts in `wWinMain`, validates endpoint configuration, loads the Legacy credential, and creates a process-local capability router. Configured clients bootstrap through Connected HTTPS and route product calls there only while a current schema 1 compare/service capability is cached; all other states route Legacy ([main.cpp:130](../../src/DesktopClient/main.cpp#L130), [CapabilityRouter.cpp:124](../../src/DesktopClient/CapabilityRouter.cpp#L124)).
- Legacy and compare checkout modes retain one NativeRules decision. Compare additionally sends the structured native result to the read-only decision route. Service mode branches before the member-policy read and NativeRules call, accepts only a current version 1 service result with a stable reason, and continues to the existing confirmation and single idempotent command only after allow ([CheckoutMode.cpp:79](../../src/DesktopClient/CheckoutMode.cpp#L79), [main.cpp:218](../../src/DesktopClient/main.cpp#L218), [main.cpp:394](../../src/DesktopClient/main.cpp#L394)).
- The application service runs either in console mode or as a Windows service and binds the configured OWIN base address ([Program.cs:14](../../src/AppServer/Program.cs#L14), [Program.cs:37](../../src/AppServer/Program.cs#L37)).
- Database behavior is installed through versioned SQL scripts under `database/`; build and test orchestration lives under `scripts/` ([README.md:384](../../README.md#L384)).

## Boundaries and dependencies

- The client has an in-process link to `NativeRules.dll` and a network dependency on `/api/v1` ([DesktopClient.vcxproj:43](../../src/DesktopClient/DesktopClient.vcxproj#L43), [main.cpp:131](../../src/DesktopClient/main.cpp#L131)).
- The service owns Npgsql access, API authentication, DTO validation, idempotency coordination, database error translation, the additive capability response, and a read-only checkout-decision endpoint ([Capabilities.cs:126](../../src/AppServer/Capabilities.cs#L126), [CheckoutDecisions.cs:322](../../src/AppServer/CheckoutDecisions.cs#L322)).
- The Legacy endpoint defaults to unencrypted localhost HTTP for compatibility. An optional Connected endpoint is externally configured, requires HTTPS and its own credential file, and is selected only by the validated, expiring service capability ([ClientTransport.cpp:284](../../src/DesktopClient/ClientTransport.cpp#L284), [CapabilityRouter.cpp:170](../../src/DesktopClient/CapabilityRouter.cpp#L170)).

## Graph evidence

`EXTRACTED`: Graphify identifies `DesktopClient`, `NativeRules`, `AppServer`, `NativeRulesTests`, and `DesktopClient.UiTests` as projects contained by `ToolLending.sln`. Direct project-file inspection verifies the client-to-native-library link.

`EXTRACTED`: Graphify locates the service decision request/response helpers and the `ServiceCheckout` branch. Direct inspection verifies the service request contains no client-derived policy fields, tool ID, or idempotency key.
