param([string]$SptRoot = 'C:\SPT')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$run = Join-Path $PSScriptRoot ('artifacts\build-' + [Guid]::NewGuid().ToString('N'))
$bin = Join-Path $run 'bin'
$stage = Join-Path $run 'stage'
$modStage = Join-Path $stage 'BepInEx\plugins\ECU'
New-Item -ItemType Directory -Path $modStage -Force | Out-Null
$oldPackages = $env:NUGET_PACKAGES
try {
    $env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.packages'
    & dotnet build (Join-Path $PSScriptRoot 'src\ECU.Client.csproj') -c Release "-p:RestoreConfigFile=$PSScriptRoot\NuGet.Config" "-p:SptRoot=$SptRoot" -o $bin --nologo -warnaserror 2>&1 | Tee-Object (Join-Path $run 'build.txt')
    if ($LASTEXITCODE -ne 0) { throw 'ECU build failed.' }

    Copy-Item -LiteralPath (Join-Path $bin 'ECU.Client.dll') -Destination $modStage
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\icon.png') -Destination $modStage
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\INSTALL.txt') -Destination (Join-Path $stage 'ECU-INSTALL.txt')
    $zip = Join-Path $run 'ECU-v1.0.0-SSPTMM.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, [IO.Compression.CompressionLevel]::Optimal, $false)
    Get-FileHash -LiteralPath $zip -Algorithm SHA256 | Format-List | Out-String | Set-Content (Join-Path $run 'SHA256.txt')
    Write-Output "PACKAGE=$zip"
} finally {
    $env:NUGET_PACKAGES = $oldPackages
}
