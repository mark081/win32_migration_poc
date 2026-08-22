param([switch]$Install)
. "$PSScriptRoot\Common.ps1"
$required=@('nuget','psql','pg_dump')
$missing=@($required|Where-Object{-not(Get-Command $_ -ErrorAction SilentlyContinue)})
$msbuild=@("${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe")|Where-Object{Test-Path $_}
if(-not $msbuild){$missing+='Visual Studio 2022 Build Tools'}
$release=[Environment]::OSVersion.Version
if($release.Major -ne 10){Write-Warning 'This demonstration is specified for Windows Server 2019.'}
if($Install){
 if(-not(Get-Command choco -ErrorAction SilentlyContinue)){throw 'Chocolatey is required for -Install. Install it from an approved organizational source.'}
 choco install -y visualstudio2022buildtools visualstudio2022-workload-vctools netfx-4.8-devpack nuget.commandline postgresql15
 $missing=@()
}
if($missing){throw ('Missing prerequisites: '+($missing -join ', ')+'. See docs\windows-server-2019-setup.md.')}
Write-Host 'Prerequisite checks passed.'
