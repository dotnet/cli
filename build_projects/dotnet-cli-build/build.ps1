#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$Configuration="Debug",
    [string[]]$Targets=@("Default"),
    [switch]$NoPackage,
    [switch]$NoRun,
    [switch]$Help)

if($Help)
{
    Write-Host "Usage: .\build.ps1 [-Configuration <CONFIGURATION>] [-Targets <TARGETS...>] [-Architecture <ARCHITECTURE>] [-NoPackage] [-Help]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <CONFIGURATION>     Build the specified Configuration (Debug or Release, default: Debug)"
    Write-Host "  -Targets <TARGETS...>              Comma separated build targets to run (Init, Compile, Publish, etc.; Default is a full build and publish)"
    Write-Host "  -NoPackage                         Skip packaging targets"
    Write-Host "  -NoRun                             Skip running the build"
    Write-Host "  -Help                              Display this help message"
    exit 0
}

$env:CONFIGURATION = $Configuration;
$RepoRoot = "$PSScriptRoot\..\.."
$env:NUGET_PACKAGES = "$RepoRoot\.nuget\packages"

if($NoPackage)
{
    $env:DOTNET_BUILD_SKIP_PACKAGING=1
}
else
{
    $env:DOTNET_BUILD_SKIP_PACKAGING=0
}

# Load Branch Info
cat "$RepoRoot\branchinfo.txt" | ForEach-Object {
    if(!$_.StartsWith("#") -and ![String]::IsNullOrWhiteSpace($_)) {
        $splat = $_.Split([char[]]@("="), 2)
        Set-Content "env:\$($splat[0])" -Value $splat[1]
    }
}

$env:path = "$RepoRoot\.dotnet_stage0\Windows;" + $env:path

# Restore the build scripts
Write-Host "Restoring Build Script projects..."
pushd "$PSScriptRoot\.."
dotnet restore
if($LASTEXITCODE -ne 0) { throw "Failed to restore" }
popd

# Publish the builder
Write-Host "Compiling Build Scripts..."
dotnet publish "$PSScriptRoot" -o "$PSScriptRoot\bin" --framework netcoreapp1.0
if($LASTEXITCODE -ne 0) { throw "Failed to compile build scripts" }

if(!$NoRun)
{
    # Run the builder
    Write-Host "Invoking Build Scripts..."
    Write-Host " Configuration: $env:CONFIGURATION"
    & "$PSScriptRoot\bin\dotnet-cli-build.exe" @Targets
    if($LASTEXITCODE -ne 0) { throw "Build failed" }
}
