---
type: System Overview
title: Tool Lending Connected-stage baseline
description: Runtime components, entry points, and principal boundaries.
resource: repo://.
tags: [win32, service, postgresql, connected]
sources:
  - resource: repo://README.md#L16-L59
  - resource: repo://src/DesktopClient/ClientTransport.cpp#L171-L354
  - resource: repo://src/AppServer/Program.cs#L14-L37
  - resource: repo://src/AppServer/Capabilities.cs#L126-L166
  - resource: repo://src/AppServer/CheckoutDecisions.cs#L322-L390
  - resource: repo://docs/architecture.md#L1-L26
generated:
  by: analyze-brownfield-context/1.0
  at: 2026-09-02T23:45:00+00:00
status: draft
source_revision: f53a98427070af5f64bdce85b015fc66ca863210
source_fingerprint: 120f1a554cacbd7eaac6ef03c18228d1cbdd20d65d0e01b22a22c629dd4c2a7b
source_worktree: dirty
curation_status: generated
---

# Summary

The application is a Windows/x86 client-server system. A native Win32 client calls a self-hosted .NET Framework 4.8 Web API service, and only that service accesses PostgreSQL ([README.md:16](../../README.md#L16), [README.md:58](../../README.md#L58)). PostgreSQL holds durable workflow state and concurrency-sensitive business rules ([README.md:126](../../README.md#L126)).

## Entry points

- The interactive client starts in `wWinMain`, validates endpoint configuration, loads the Legacy credential, creates the Win32 UI, and uses the bounded WinHTTP transport for API calls. Product routing remains Legacy until capability caching is implemented ([main.cpp:126](../../src/DesktopClient/main.cpp#L126), [ClientTransport.cpp:279](../../src/DesktopClient/ClientTransport.cpp#L279)).
- The application service runs either in console mode or as a Windows service and binds the configured OWIN base address ([Program.cs:14](../../src/AppServer/Program.cs#L14), [Program.cs:37](../../src/AppServer/Program.cs#L37)).
- Database behavior is installed through versioned SQL scripts under `database/`; build and test orchestration lives under `scripts/` ([README.md:384](../../README.md#L384)).

## Boundaries and dependencies

- The client has an in-process link to `NativeRules.dll` and a network dependency on `/api/v1` ([DesktopClient.vcxproj:43](../../src/DesktopClient/DesktopClient.vcxproj#L43), [main.cpp:131](../../src/DesktopClient/main.cpp#L131)).
- The service owns Npgsql access, API authentication, DTO validation, idempotency coordination, database error translation, the additive capability response, and a read-only checkout-decision endpoint ([Capabilities.cs:126](../../src/AppServer/Capabilities.cs#L126), [CheckoutDecisions.cs:322](../../src/AppServer/CheckoutDecisions.cs#L322)).
- The Legacy endpoint defaults to unencrypted localhost HTTP for compatibility. An optional Connected endpoint is externally configured, requires HTTPS and its own credential file, and is not yet selected by product routing ([ClientTransport.cpp:279](../../src/DesktopClient/ClientTransport.cpp#L279)).

## Graph evidence

`EXTRACTED`: Graphify identifies `DesktopClient`, `NativeRules`, `AppServer`, `NativeRulesTests`, and `DesktopClient.UiTests` as projects contained by `ToolLending.sln`. Direct project-file inspection verifies the client-to-native-library link.
