param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

. "$PSScriptRoot\Common.ps1"

if (-not [Environment]::UserInteractive) {
    throw 'UI tests require an interactive, unlocked Windows desktop session.'
}

$client = "$RepoRoot\artifacts\x86\$Configuration\DesktopClient.exe"
if (-not (Test-Path $client)) {
    throw "DesktopClient.exe was not found. Run scripts\Build.ps1 -Configuration $Configuration first."
}

$env:TOOL_LENDING_UI_EXE = $client
& (Require-Command dotnet) test "$RepoRoot\tests\DesktopClient.UiTests\DesktopClient.UiTests.csproj" `
    --configuration $Configuration `
    --no-restore

if ($LASTEXITCODE) {
    throw 'Desktop client UI tests failed.'
}

Write-Host 'Desktop client UI tests passed.'
