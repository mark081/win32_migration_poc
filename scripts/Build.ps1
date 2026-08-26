param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

. "$PSScriptRoot\Common.ps1"
$nuget = Require-Command nuget
$dotnet = Require-Command dotnet
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw 'Visual Studio Installer vswhere.exe was not found.'
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
    -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
if (-not $msbuild) {
    throw 'MSBuild was not found.'
}

# The legacy .NET Framework project uses packages.config and must be restored by NuGet.
& $nuget restore "$RepoRoot\src\AppServer\packages.config" `
    -PackagesDirectory "$RepoRoot\packages"
if ($LASTEXITCODE) {
    throw 'NuGet restore failed.'
}

& $msbuild "$RepoRoot\ToolLending.sln" /m /t:Rebuild `
    "/p:Configuration=$Configuration" /p:Platform=x86
if ($LASTEXITCODE) {
    throw 'Legacy solution build failed.'
}

# Build the SDK-style FlaUI project with dotnet because the VM's Visual Studio Build Tools
# MSBuild does not resolve the separately installed .NET 8 SDK.
$uiTests = "$RepoRoot\tests\DesktopClient.UiTests\DesktopClient.UiTests.csproj"
& $dotnet restore $uiTests
if ($LASTEXITCODE) {
    throw 'UI test package restore failed.'
}

& $dotnet build $uiTests --configuration $Configuration --no-restore
if ($LASTEXITCODE) {
    throw 'UI test build failed.'
}

$config = "$RepoRoot\artifacts\x86\$Configuration\AppServer.exe.config"
$escaped = [Security.SecurityElement]::Escape((App-Password))
(Get-Content $config -Raw).Replace('Password=CHANGE_ME', "Password=$escaped") |
    Set-Content $config -Encoding UTF8
Write-Host "Build completed: artifacts\x86\$Configuration"
