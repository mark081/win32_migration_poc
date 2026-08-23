# Windows Server 2019 setup

This is the deployment-like workflow. For local or remote development on Windows 11 Pro, including VS Code tasks and debugging, see [Windows 11 Pro development setup](windows-11-pro-setup.md).

## Exact prerequisites

- Windows Server 2019 Standard or Datacenter, Desktop Experience, fully patched
- .NET Framework 4.8 Developer Pack and runtime
- Visual Studio 2022 Build Tools with `Microsoft.VisualStudio.Workload.VCTools`, MSVC x86/x64 tools, Windows 10 SDK, and .NET Framework 4.8 targeting pack
- NuGet CLI 6.x
- PostgreSQL 15 x64, command-line tools, and a local server listening only on loopback unless a LAN deployment is explicitly configured
- PowerShell 5.1

`scripts\Setup-Prerequisites.ps1` verifies these prerequisites and can install supported packages with Chocolatey when `-Install` is supplied. Review package sources and organizational policy before enabling installation.

## Installation

1. Copy the repository to `C:\ToolLending`.
2. Create a strong PostgreSQL password and set it only for the current shell:
   `$env:TOOLLENDING_DB_PASSWORD = Read-Host -AsSecureString` is not compatible with `psql`; for demo automation set a temporary plain environment variable and clear it afterward.
3. Run `Initialize-Database.ps1`. It creates the database role, database, schema, routines, and seed data.
4. Run `Build.ps1`, then `Install-Service.ps1` from an elevated prompt.
5. Run `Run-Tests.ps1`.
6. Start the desktop client from `artifacts\x86\Release`.

## Operations

- The service name is `ToolLendingAppServer`.
- The API listens on `http://localhost:8088/`.
- Configuration lives beside the service executable in `AppServer.exe.config`.
- Use `Backup-Database.ps1` and store copies off the server.
- Exercise `Reset-Demo.ps1` only in a disposable demo environment; it deletes and reseeds application data.
- Windows service recovery is configured for two automatic restarts. This is process recovery, not high availability.

## Production warning

The seeded API key and database defaults are demo-only. Restrict the listener and PostgreSQL port with Windows Firewall, use a managed service account, replace the API key, protect configuration ACLs, enable TLS if traffic leaves the host, and implement organizational backup/restore controls before any non-demo use.
