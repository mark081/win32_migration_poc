param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
. "$PSScriptRoot\Common.ps1"; $native = "$RepoRoot\artifacts\x86\$Configuration\NativeRulesTests.exe"; if (-not(Test-Path $native)) { throw 'Build the solution first.' }
& $native; if ($LASTEXITCODE) { throw 'Native tests failed.' }
$featureTests = "$RepoRoot\artifacts\x86\$Configuration\AppServer.FeatureTests.exe"; if (-not(Test-Path $featureTests)) { throw 'Build the solution first.' }
& $featureTests; if ($LASTEXITCODE) { throw 'Feature evaluator tests failed.' }
$env:PGPASSWORD = App-Password; & (Require-Command psql) @(App-PsqlArgs) -f "$RepoRoot\database\901_database_tests.sql"; if ($LASTEXITCODE) { throw 'Database tests failed.' }
& "$PSScriptRoot\Reset-Demo.ps1" -Force
& "$RepoRoot\tests\Integration\ApiTests.ps1"
