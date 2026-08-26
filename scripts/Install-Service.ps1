param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
. "$PSScriptRoot\Common.ps1"; $exe = "$RepoRoot\artifacts\x86\$Configuration\AppServer.exe"; if (-not(Test-Path $exe)) { throw 'Build AppServer first.' }
if (Get-Service ToolLendingAppServer -ErrorAction SilentlyContinue) { Stop-Service ToolLendingAppServer -ErrorAction SilentlyContinue; sc.exe delete ToolLendingAppServer | Out-Null; Start-Sleep -Seconds 2 }
sc.exe create ToolLendingAppServer binPath= "`"$exe`"" start= auto DisplayName= "Tool Lending Application Server" | Out-Null
sc.exe failure ToolLendingAppServer reset= 86400 actions= restart/5000/restart/15000/none/0 | Out-Null
Start-Service ToolLendingAppServer; Write-Host 'ToolLendingAppServer installed and started.'
