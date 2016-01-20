#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$NoCache="")

. $PSScriptRoot\..\common\_common.ps1

if ($NoCache -eq "True") {
    $NoCacheArg = "--no-cache"
    info "Bypassing NuGet Cache"
}
else {
    $NoCacheArg = ""
}

# Use Stage0 binaries
$StartPath = $env:PATH
$env:PATH = "$env:DOTNET_INSTALL_DIR\cli\bin;$StartPath"

# Restore packages
header "Restoring packages"
& dotnet restore "$RepoRoot\src" --quiet --runtime "$Rid"
& dotnet restore "$RepoRoot\test" --quiet --runtime "$Rid"
& dotnet restore "$RepoRoot\tools" --quiet --runtime "$Rid"

$oldErrorAction=$ErrorActionPreference
$ErrorActionPreference="SilentlyContinue"
& dotnet restore "$RepoRoot\testapp" --quiet --runtime "$Rid" 2>&1 | Out-Null
$ErrorActionPreference=$oldErrorAction

$env:PATH = $StartPath

