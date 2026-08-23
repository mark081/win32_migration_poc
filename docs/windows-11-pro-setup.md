# Windows 11 Pro development setup

Windows 11 Pro is a supported development and demonstration host. It can build and run every component locally, including the x86 Win32 client, native rules DLL, .NET Framework API, PostgreSQL database, and Windows service. The recommended day-to-day workflow runs the API in console mode and does not require administrative rights after prerequisites and PostgreSQL are installed.

## Prerequisites

- Windows 11 Pro x64, build 22000 or later, fully patched
- Git for Windows
- Visual Studio Code
- .NET Framework 4.8 Developer Pack and runtime
- Visual Studio 2022 Build Tools with:
  - Desktop development with C++ workload
  - MSVC v143 x86/x64 build tools
  - Windows 10 or Windows 11 SDK
  - .NET Framework 4.8 targeting pack
- NuGet CLI 6.x on `PATH`
- PostgreSQL 15 x64, including `psql` and `pg_dump` on `PATH`
- Windows PowerShell 5.1

Recommended VS Code extensions are declared in `.vscode/extensions.json`. Install them when VS Code prompts, or open **Extensions: Show Recommended Extensions**.

## Clone and open the working branch

```powershell
git clone https://github.com/mark081/win32_migration_poc.git
cd win32_migration_poc
git switch connected
code .
```

When using VS Code Remote SSH from macOS, run these commands in a terminal on the Windows VM after connecting. Open the remote repository folder; extensions marked **Install in SSH: host** must be installed on the VM side.

## Configure PostgreSQL

During PostgreSQL installation, keep the database listener on `127.0.0.1:5432` and record the administrator password. Do not expose port 5432 through Windows Firewall or the cloud network security group.

Open a PowerShell terminal in VS Code:

```powershell
$env:PGPASSWORD = Read-Host 'PostgreSQL postgres password'
$env:TOOLLENDING_DB_PASSWORD = 'ChangeMe-LocalOnly!'
.\scripts\Initialize-Database.ps1
Remove-Item Env:PGPASSWORD
```

The application password shown above is intentionally local-demo-only. Use a unique value if the VM is shared or persistent.

## Build in VS Code

1. Run **Terminal: Run Task**.
2. Run **Prerequisites: Check**.
3. Run **Database: Initialize** once.
4. Press `Ctrl+Shift+B` to run **Build: Debug x86**.

The build restores NuGet packages and writes binaries to `artifacts\x86\Debug`.

## Run without installing a service

1. Run the task **API: Run console (Debug)** and leave its dedicated terminal open.
2. Wait for `Tool Lending API running; Enter stops it.`
3. Run **Client: Run (Debug)**, or press `F5` and select **Debug Win32 client (x86)**.
4. Press Enter in the API terminal when finished.

The desktop client calls `http://localhost:8088`. PostgreSQL remains local on `127.0.0.1:5432`.

The client run and debug commands intentionally do not rebuild while the API is running because Windows locks the active `AppServer.exe`. Stop the API before rebuilding Debug artifacts after a source change.

## Run the tests

Ensure the API is running, then run **Tests: All (Release)**. The tests rebuild Release binaries, execute native rules tests, exercise database routines, reset the seeded demo data, and run API integration tests.

## Optional Windows service mode

Service mode matches the deployment topology but is not required for development. Open VS Code or PowerShell as Administrator, then run:

```powershell
.\scripts\Build.ps1 -Configuration Release
.\scripts\Install-Service.ps1 -Configuration Release
Get-Service ToolLendingAppServer
```

Remove it later from an elevated prompt:

```powershell
.\scripts\Uninstall-Service.ps1
```

Do not run console mode and service mode simultaneously; only one process can listen on port 8088.

## Windows Firewall and Azure VM access

- Restrict inbound SSH (`22`) and RDP (`3389`) to your own public IP in the Azure network security group.
- Do not create inbound rules for PostgreSQL (`5432`) or the application API (`8088`) for this single-host demonstration.
- VS Code Remote SSH operates over port 22.
- Use RDP only when you need to see or interact with the Win32 desktop client.
- Stop and **deallocate** the VM when it is not in use.

## Troubleshooting

| Symptom | Resolution |
|---|---|
| `vswhere.exe was not found` | Install Visual Studio 2022 Build Tools, not only the .NET SDK. |
| `nuget`, `psql`, or `pg_dump` not found | Add their installation directories to the system `PATH`, then reconnect VS Code. |
| MSBuild cannot find v143 or the Windows SDK | Modify Build Tools and add the Desktop development with C++ workload. |
| API reports database unavailable | Start the PostgreSQL Windows service and verify the configured password. |
| Port 8088 is already in use | Stop the installed `ToolLendingAppServer` service or the other console instance. |
| Client cannot show on macOS | The executable runs on Windows; use RDP to interact with its window. |
| PowerShell blocks scripts | Use `Set-ExecutionPolicy -Scope Process Bypass` in that terminal only. |

For the server deployment workflow, see [Windows Server 2019 setup](windows-server-2019-setup.md).
