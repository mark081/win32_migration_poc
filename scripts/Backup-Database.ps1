param([string]$Destination)
. "$PSScriptRoot\Common.ps1"; $pgdump = Require-Command pg_dump; if (-not $Destination) { $dir = Join-Path $RepoRoot 'backups'; New-Item -ItemType Directory -Force $dir | Out-Null; $Destination = Join-Path $dir ("tool_lending_{0}.dump" -f (Get-Date -Format 'yyyyMMdd_HHmmss')) }
$env:PGPASSWORD = App-Password; & $pgdump -h 127.0.0.1 -U tool_lending_app -d tool_lending -Fc -f $Destination; if ($LASTEXITCODE) { throw 'Backup failed.' }; Write-Host "Backup created: $Destination"
