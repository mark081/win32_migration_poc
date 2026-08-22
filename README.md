# Legacy Tool Lending Demo

This repository is a deliberately legacy-shaped, single-site client/server application. It mirrors the architecture described in the West Monroe Eaglesoft assessment without copying the dental domain or imaging features.

## Stack

- Windows Server 2019
- x86 Win32 C++ desktop client
- x86 native C++ business-rules DLL
- .NET Framework 4.8 Windows service and ASP.NET Web API 2
- PostgreSQL 15 with authoritative stored procedures
- Npgsql data access; clients never connect to the database

The system manages community-library tools, members, reservations, checkouts, returns, and late fees. Business logic is intentionally split among the desktop UI, native DLL, application service, and database. This is an architecture demonstration, not a recommended greenfield design.

## Quick start

On a clean Windows Server 2019 host, open an elevated PowerShell prompt:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\Setup-Prerequisites.ps1
.\scripts\Initialize-Database.ps1
.\scripts\Build.ps1
.\scripts\Install-Service.ps1
.\scripts\Run-Tests.ps1
```

Launch `artifacts\x86\Release\ToolLendingClient.exe`. The seeded API key is `demo-local-key`; it is intentionally non-secret and must only be used for the local demo.

See [Windows Server setup](docs/windows-server-2019-setup.md), [architecture](docs/architecture.md), and [demo script](docs/demo.md).

## Repository map

- `src/NativeRules` — shared x86 C++ DLL
- `src/DesktopClient` — legacy forms-style Win32 UI
- `src/AppServer` — .NET Framework 4.8 Windows service/API
- `database` — schema, stored procedures, seed data, and SQL tests
- `tests` — native unit tests and API integration tests
- `scripts` — setup, build, install, backup, reset, and test automation

## Security boundary

The service listens on `http://localhost:8088/` by default. The database binds locally. The application is intentionally single-tenant and has no WAN, cloud, or high-availability behavior.
