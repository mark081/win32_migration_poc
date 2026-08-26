param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

. "$PSScriptRoot\Common.ps1"

if (-not [Environment]::UserInteractive) {
    throw 'UI tests require an interactive, unlocked Windows desktop session.'
}

$client = "$RepoRoot\artifacts\x86\$Configuration\DesktopClient.exe"
if (-not (Test-Path $client)) {
    throw "DesktopClient.exe was not found. Run scripts\Build.ps1 -Configuration $Configuration first."
}

$credentialDirectory = Join-Path $env:TEMP ("ToolLending.UiTests." + [Guid]::NewGuid())
$credentialFile = Join-Path $credentialDirectory 'client.credential'
$testCredential = 'ui-test-shared-key-do-not-display'

try {
    New-Item -ItemType Directory -Path $credentialDirectory -Force | Out-Null
    Set-Content -Path $credentialFile -Value $testCredential -Encoding UTF8

    $env:TOOL_LENDING_UI_EXE = $client
    $env:TOOL_LENDING_LEGACY_CREDENTIAL_FILE = $credentialFile
    $env:TOOL_LENDING_UI_TEST_SHARED_KEY = $testCredential

    & (Require-Command dotnet) test "$RepoRoot\tests\DesktopClient.UiTests\DesktopClient.UiTests.csproj" `
        --configuration $Configuration `
        --no-restore

    if ($LASTEXITCODE) {
        throw 'Desktop client UI tests failed.'
    }
}
finally {
    Remove-Item Env:TOOL_LENDING_UI_EXE -ErrorAction SilentlyContinue
    Remove-Item Env:TOOL_LENDING_LEGACY_CREDENTIAL_FILE -ErrorAction SilentlyContinue
    Remove-Item Env:TOOL_LENDING_UI_TEST_SHARED_KEY -ErrorAction SilentlyContinue
    Remove-Item $credentialDirectory -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Desktop client UI tests passed.'
