param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
. "$PSScriptRoot\Common.ps1"; $nuget = Require-Command nuget
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"; if (-not(Test-Path $vswhere)) { throw 'Visual Studio Installer vswhere.exe was not found.' }
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1; if (-not $msbuild) { throw 'MSBuild was not found.' }
& $nuget restore "$RepoRoot\ToolLending.sln" -PackagesDirectory "$RepoRoot\packages"; if ($LASTEXITCODE) { throw 'NuGet restore failed.' }
& $msbuild "$RepoRoot\ToolLending.sln" /m /t:Rebuild "/p:Configuration=$Configuration" /p:Platform=x86; if ($LASTEXITCODE) { throw 'Build failed.' }
$config = "$RepoRoot\artifacts\x86\$Configuration\AppServer.exe.config"; $escaped = [Security.SecurityElement]::Escape((App-Password)); (Get-Content $config -Raw).Replace('Password=CHANGE_ME', "Password=$escaped") | Set-Content $config -Encoding UTF8
Write-Host "Build completed: artifacts\x86\$Configuration"
