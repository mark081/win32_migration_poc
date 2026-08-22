param([switch]$Force)
. "$PSScriptRoot\Common.ps1";if(-not $Force){$answer=Read-Host 'This deletes all demo application data. Type RESET';if($answer -ne 'RESET'){throw 'Reset cancelled.'}}
$env:PGPASSWORD=App-Password;& (Require-Command psql) @(App-PsqlArgs) -f "$RepoRoot\database\900_reset.sql";if($LASTEXITCODE){throw 'Reset failed.'};Write-Host 'Demo data reset.'
