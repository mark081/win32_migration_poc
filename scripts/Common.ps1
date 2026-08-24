$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path -Parent $PSScriptRoot
function Require-Command([string]$Name) { $c = Get-Command $Name -ErrorAction SilentlyContinue; if (-not $c) { throw "Required command '$Name' was not found." }; $c.Source }
function App-Password { if ($env:TOOLLENDING_DB_PASSWORD) { $env:TOOLLENDING_DB_PASSWORD }else { 'ChangeMe-LocalOnly!' } }
function App-PsqlArgs { @('-h', '127.0.0.1', '-p', '5432', '-U', 'tool_lending_app', '-d', 'tool_lending', '-v', 'ON_ERROR_STOP=1') }
