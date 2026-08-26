param([switch]$Check)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Common.ps1"

function Set-FormattedContent {
    param([string]$Path, [string]$Content)

    $current = Get-Content -LiteralPath $Path -Raw
    $normalized = $Content.TrimEnd("`r", "`n") + "`r`n"
    if ($current -ceq $normalized) {
        return
    }

    if ($Check) {
        $script:HasChanges = $true
        Write-Warning "Formatting required: $Path"
        return
    }

    [System.IO.File]::WriteAllText($Path, $normalized, [System.Text.UTF8Encoding]::new($false))
}

$HasChanges = $false

& dotnet tool restore | Out-Host
if ($LASTEXITCODE) { throw 'Unable to restore repository-local .NET tools.' }

$csharpFiles = Get-ChildItem "$RepoRoot\src", "$RepoRoot\tests" -Recurse -File -Filter *.cs |
    Where-Object { $_.FullName -notmatch '[\\/](obj|artifacts)[\\/]' } |
    ForEach-Object FullName
$csharpArgs = @('tool', 'run', 'csharpier', $(if ($Check) { 'check' } else { 'format' })) + $csharpFiles
& dotnet @csharpArgs | Out-Host
if ($LASTEXITCODE) { $HasChanges = $true }

$cppFiles = Get-ChildItem "$RepoRoot\src", "$RepoRoot\tests" -Recurse -File -Include *.cpp, *.h
$clangFormat = Get-Command clang-format -ErrorAction SilentlyContinue
if (-not $clangFormat) { throw 'clang-format 17 or newer is required to format C/C++ files.' }
foreach ($file in $cppFiles) {
    if ($Check) {
        & $clangFormat.Source --dry-run --Werror --style=file $file.FullName
        if ($LASTEXITCODE) { $HasChanges = $true }
    }
    else {
        & $clangFormat.Source -i --style=file $file.FullName
        if ($LASTEXITCODE) { throw "clang-format failed: $($file.FullName)" }
    }
}

$analyzer = Get-Module -ListAvailable PSScriptAnalyzer | Sort-Object Version -Descending | Select-Object -First 1
if (-not $analyzer -or $analyzer.Version -lt [version]'1.22.0') {
    $modulePath = "$RepoRoot\artifacts\format-tools\PSScriptAnalyzer"
    if (-not (Test-Path "$modulePath\PSScriptAnalyzer.psd1")) {
        & nuget install PSScriptAnalyzer -Version 1.22.0 -Source https://www.powershellgallery.com/api/v2 -OutputDirectory "$RepoRoot\artifacts\format-tools" -ExcludeVersion | Out-Host
        if ($LASTEXITCODE) { throw 'Unable to restore PSScriptAnalyzer.' }
    }
    Import-Module "$modulePath\PSScriptAnalyzer.psd1" -Force
}
else {
    Import-Module $analyzer.Path
}
$powerShellFiles = Get-ChildItem "$RepoRoot\scripts", "$RepoRoot\tests" -Recurse -File -Filter *.ps1
foreach ($sourceFile in $powerShellFiles) {
    $sourcePath = $sourceFile.FullName
    $source = (Get-Content $sourcePath -Raw) -replace "`r?`n", "`r`n"
    $formatted = Invoke-Formatter -ScriptDefinition $source -Settings "$RepoRoot\PSScriptAnalyzerSettings.psd1"
    Set-FormattedContent -Path $sourcePath -Content $formatted
}

$xmlFiles = Get-ChildItem "$RepoRoot\src", "$RepoRoot\tests" -Recurse -File -Include *.csproj, *.vcxproj, *.config |
    Where-Object { $_.FullName -notmatch '[\\/](obj|artifacts)[\\/]' }
foreach ($file in $xmlFiles) {
    $document = [xml](Get-Content $file.FullName -Raw)
    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.IndentChars = '  '
    $settings.NewLineChars = "`r`n"
    $settings.OmitXmlDeclaration = -not (Get-Content $file.FullName -First 1).StartsWith('<?xml')
    $builder = [System.Text.StringBuilder]::new()
    $writer = [System.Xml.XmlWriter]::Create($builder, $settings)
    $document.Save($writer)
    $writer.Dispose()
    $xml = $builder.ToString() -replace 'encoding="utf-16"', 'encoding="utf-8"'
    Set-FormattedContent -Path $file.FullName -Content $xml
}

if ($Check -and $HasChanges) { throw 'One or more files require formatting.' }
Write-Host $(if ($Check) { 'Formatting check passed.' } else { 'Code formatting completed.' })
