param([switch]$Install)
. "$PSScriptRoot\Common.ps1"

if (-not $IsWindows -and $PSVersionTable.PSVersion.Major -ge 6) { throw 'This application requires 64-bit Windows 11 Pro or Windows Server 2019.' }
if (-not [Environment]::Is64BitOperatingSystem) { throw 'A 64-bit Windows installation is required to build the x86 application.' }

$os = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
$build = [int]$os.CurrentBuildNumber
$isWindows11 = $os.InstallationType -eq 'Client' -and $build -ge 22000
$isServer2019 = $os.InstallationType -eq 'Server' -and $build -ge 17763
if (-not($isWindows11 -or $isServer2019)) { Write-Warning "This host ($($os.ProductName), build $build) is not a tested platform. Supported hosts are Windows 11 Pro and Windows Server 2019." }

$required = @('nuget', 'psql', 'pg_dump')
$missing = @($required | Where-Object { -not(Get-Command $_ -ErrorAction SilentlyContinue) })
$msbuild = @("${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe") | Where-Object { Test-Path $_ }
if (-not $msbuild) { $missing += 'Visual Studio 2022 Build Tools' }
if ($Install) {
    if (-not(Get-Command choco -ErrorAction SilentlyContinue)) { throw 'Chocolatey is required for -Install. Install it from an approved organizational source.' }
    choco install -y visualstudio2022buildtools visualstudio2022-workload-vctools netfx-4.8-devpack nuget.commandline postgresql15
    $missing = @()
}
if ($missing) { throw ('Missing prerequisites: ' + ($missing -join ', ') + '. See docs\windows-11-pro-setup.md.') }
$platform = if ($isWindows11) { 'Windows 11' }elseif ($isServer2019) { 'Windows Server 2019' }else { $os.ProductName }
Write-Host "Prerequisite checks passed on $platform (build $build)."
