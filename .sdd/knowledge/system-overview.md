---
type: System Overview
title: Tool Lending Connected-stage baseline
description: Runtime components, entry points, and principal boundaries.
resource: repo://.
tags: [win32, service, postgresql, connected]
sources:
  - resource: repo://README.md#L16-L59
  - resource: repo://src/DesktopClient/main.cpp#L124-L165
  - resource: repo://src/AppServer/Program.cs#L14-L37
  - resource: repo://docs/architecture.md#L1-L26
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

The application is a Windows/x86 client-server system. A native Win32 client calls a self-hosted .NET Framework 4.8 Web API service, and only that service accesses PostgreSQL ([README.md:16](../../README.md#L16), [README.md:58](../../README.md#L58)). PostgreSQL holds durable workflow state and concurrency-sensitive business rules ([README.md:126](../../README.md#L126)).

## Entry points

- The interactive client starts in `wWinMain`, loads a practice-shared credential, creates the Win32 UI, and uses WinHTTP for API calls ([main.cpp:93](../../src/DesktopClient/main.cpp#L93), [main.cpp:124](../../src/DesktopClient/main.cpp#L124)).
- The application service runs either in console mode or as a Windows service and binds the configured OWIN base address ([Program.cs:14](../../src/AppServer/Program.cs#L14), [Program.cs:37](../../src/AppServer/Program.cs#L37)).
- Database behavior is installed through versioned SQL scripts under `database/`; build and test orchestration lives under `scripts/` ([README.md:384](../../README.md#L384)).

## Boundaries and dependencies

- The client has an in-process link to `NativeRules.dll` and a network dependency on `/api/v1` ([DesktopClient.vcxproj:43](../../src/DesktopClient/DesktopClient.vcxproj#L43), [main.cpp:131](../../src/DesktopClient/main.cpp#L131)).
- The service owns Npgsql access, API authentication, DTO validation, idempotency coordination, and database error translation ([README.md:122](../../README.md#L122)).
- The current endpoint is hard-coded to unencrypted localhost HTTP in the client, while the service base address and key are configuration values ([main.cpp:131](../../src/DesktopClient/main.cpp#L131), [App.config:4](../../src/AppServer/App.config#L4)).

## Graph evidence

`EXTRACTED`: Graphify identifies `DesktopClient`, `NativeRules`, `AppServer`, `NativeRulesTests`, and `DesktopClient.UiTests` as projects contained by `ToolLending.sln`. Direct project-file inspection verifies the client-to-native-library link.
