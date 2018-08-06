#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [switch]$Help,
    [switch]$Update)

if($Help)
{
    Write-Output "Usage: .\update-dependencies.ps1"
    Write-Output ""
    Write-Output "Options:"
    Write-Output "  -Help                 Display this help message"
    Write-Output "  -Update               Update dependencies (but don't open a PR)"
    exit 0
}

$Architecture='x64'

$RepoRoot = "$PSScriptRoot\..\.."
$ProjectPath = "$PSScriptRoot\update-dependencies.csproj"
$ProjectArgs = ""

if ($Update)
{
    $ProjectArgs = "--Update"
}

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
if (!$env:DOTNET_INSTALL_DIR)
{
    $env:DOTNET_INSTALL_DIR="$RepoRoot\.dotnet_stage0\update-dependencies"
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR))
{
    mkdir $env:DOTNET_INSTALL_DIR | Out-Null
}

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Install a stage 0
 Write-Output "Installing .NET Core CLI Stage 0"

 [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$dotnetInstallPath = Join-Path $env:DOTNET_INSTALL_DIR "dotnet-install.ps1"

Write-Output "dotnet-install path: $dotnetInstallPath"
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "$dotnetInstallPath"


if (!$env:DOTNET_TOOL_DIR)
{
    & "$dotnetInstallPath" -version "2.1.302" -Architecture $Architecture
    if($LASTEXITCODE -ne 0) { throw "Failed to install stage0" }
}
else
{
    Copy-Item -Force -Recurse $env:DOTNET_TOOL_DIR $env:DOTNET_INSTALL_DIR
}

# Put the stage0 on the path
$env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"

# Run the app
Write-Output "Invoking App $ProjectPath..."
dotnet run -p "$ProjectPath" "$ProjectArgs"
if($LASTEXITCODE -ne 0) { throw "Build failed" }
