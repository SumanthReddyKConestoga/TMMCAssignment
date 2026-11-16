param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Test
)

$ErrorActionPreference = 'Stop'
$msbuild = $null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuild) {
    $msbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe'
}
if (-not (Test-Path -LiteralPath $msbuild)) {
    $msbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
}
if (-not (Test-Path -LiteralPath $msbuild)) {
    throw 'Building requires the .NET Framework 4.8 Developer Pack on Windows.'
}

& $msbuild (Join-Path $PSScriptRoot 'TMMC.LineCounter.sln') /nologo /verbosity:minimal "/p:Configuration=$Configuration"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Test) {
    & (Join-Path $PSScriptRoot "tests\TMMC.LineCounter.Tests\bin\$Configuration\TMMC.LineCounter.Tests.exe")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host ("Application: " + (Join-Path $PSScriptRoot "src\TMMC.LineCounter\bin\$Configuration\TMMC.LineCounter.exe"))
