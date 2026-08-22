param([string]$AdminUser='postgres')
. "$PSScriptRoot\Common.ps1"; $psql=Require-Command psql
$adminPassword=$env:PGPASSWORD
if(-not $adminPassword){Write-Warning 'Set PGPASSWORD to the PostgreSQL administrator password before running unattended.'}
& $psql -h 127.0.0.1 -U $AdminUser -d postgres -v "app_password=$(App-Password)" -f "$RepoRoot\database\000_bootstrap.sql"
if($LASTEXITCODE){throw 'Database bootstrap failed.'}
$env:PGPASSWORD=App-Password
foreach($file in '001_schema.sql','002_routines.sql','003_seed.sql'){& $psql @(App-PsqlArgs) -f "$RepoRoot\database\$file";if($LASTEXITCODE){throw "$file failed."}}
$env:PGPASSWORD=$adminPassword
Write-Host 'Database initialized with demo data.'
