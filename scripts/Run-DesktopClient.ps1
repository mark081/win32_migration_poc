param(
    [Parameter(Mandatory = $true)]
    [ValidateScript({
        -not [string]::IsNullOrWhiteSpace($_) -and $_ -notmatch '[\r\n]'
    })]
    [string]$ApiKey,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

. "$PSScriptRoot\Common.ps1"

$client = "$RepoRoot\artifacts\x86\$Configuration\DesktopClient.exe"
if (-not (Test-Path $client)) {
    throw "DesktopClient.exe was not found for $Configuration. Run scripts\Build.ps1 -Configuration $Configuration first."
}

$credentialDirectory = Join-Path $env:TEMP ("ToolLending.LegacyClient." + [Guid]::NewGuid())
$credentialFile = Join-Path $credentialDirectory 'client.credential'
$originalCredentialFile = $env:TOOL_LENDING_LEGACY_CREDENTIAL_FILE

try {
    New-Item -ItemType Directory -Path $credentialDirectory -Force | Out-Null
    Set-Content -Path $credentialFile -Value $ApiKey -Encoding UTF8

    # The desktop client inherits this environment variable and reads the shared credential once
    # during startup. Only the temporary file path is placed in the environment; the key itself is
    # not added to another environment variable or written to the console.
    $env:TOOL_LENDING_LEGACY_CREDENTIAL_FILE = $credentialFile

    Write-Host "Starting $Configuration desktop client with a temporary Legacy credential file."
    $process = Start-Process -FilePath $client -PassThru
    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        throw "Desktop client exited with code $($process.ExitCode)."
    }
}
finally {
    if ($null -eq $originalCredentialFile) {
        Remove-Item Env:TOOL_LENDING_LEGACY_CREDENTIAL_FILE -ErrorAction SilentlyContinue
    }
    else {
        $env:TOOL_LENDING_LEGACY_CREDENTIAL_FILE = $originalCredentialFile
    }

    Remove-Item -LiteralPath $credentialDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
