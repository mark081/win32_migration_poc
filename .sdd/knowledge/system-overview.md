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
  - resource: repo://src/AppServer/Program.cs#L14-L37
  - resource: repo://src/AppServer/Capabilities.cs#L126-L166
  - resource: repo://src/AppServer/CheckoutDecisions.cs#L322-L390
  - resource: repo://docs/architecture.md#L1-L26
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-03T21:05:00+00:00
status: draft
source_revision: 17904f336dcb6b9e39221e28bb80a3a0860fc752
source_fingerprint: 019debc291402cec69724d410e6f848cb52d07394f8bf82c5a4cb3432c2fa2e4
source_worktree: dirty
curation_status: generated
---

# Summary

The application is a Windows/x86 client-server system. A native Win32 client calls a self-hosted .NET Framework 4.8 Web API service, and only that service accesses PostgreSQL ([README.md:16](../../README.md#L16), [README.md:58](../../README.md#L58)). PostgreSQL holds durable workflow state and concurrency-sensitive business rules ([README.md:126](../../README.md#L126)).

## Entry points

- The interactive client starts in `wWinMain`, validates endpoint configuration, loads the Legacy credential, and creates a process-local capability router. Configured clients bootstrap through Connected HTTPS and route product calls there only while a current schema 1 compare/service capability is cached; all other states route Legacy ([main.cpp:130](../../src/DesktopClient/main.cpp#L130), [CapabilityRouter.cpp:124](../../src/DesktopClient/CapabilityRouter.cpp#L124)).
- The application service runs either in console mode or as a Windows service and binds the configured OWIN base address ([Program.cs:14](../../src/AppServer/Program.cs#L14), [Program.cs:37](../../src/AppServer/Program.cs#L37)).
- Database behavior is installed through versioned SQL scripts under `database/`; build and test orchestration lives under `scripts/` ([README.md:384](../../README.md#L384)).

## Boundaries and dependencies

- The client has an in-process link to `NativeRules.dll` and a network dependency on `/api/v1` ([DesktopClient.vcxproj:43](../../src/DesktopClient/DesktopClient.vcxproj#L43), [main.cpp:131](../../src/DesktopClient/main.cpp#L131)).
- The service owns Npgsql access, API authentication, DTO validation, idempotency coordination, database error translation, the additive capability response, and a read-only checkout-decision endpoint ([Capabilities.cs:126](../../src/AppServer/Capabilities.cs#L126), [CheckoutDecisions.cs:322](../../src/AppServer/CheckoutDecisions.cs#L322)).
- The Legacy endpoint defaults to unencrypted localhost HTTP for compatibility. An optional Connected endpoint is externally configured, requires HTTPS and its own credential file, and is selected only by the validated, expiring service capability ([ClientTransport.cpp:284](../../src/DesktopClient/ClientTransport.cpp#L284), [CapabilityRouter.cpp:170](../../src/DesktopClient/CapabilityRouter.cpp#L170)).

## Graph evidence

`EXTRACTED`: Graphify identifies `DesktopClient`, `NativeRules`, `AppServer`, `NativeRulesTests`, and `DesktopClient.UiTests` as projects contained by `ToolLending.sln`. Direct project-file inspection verifies the client-to-native-library link.
