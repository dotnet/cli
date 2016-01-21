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

# Restore packages
# NOTE(anurse): I had to remove --quiet, because NuGet3 is too quiet when that's provided :(
header "Restoring packages"
dotnet restore "$RepoRoot\src" --runtime "$Rid" "$NoCacheArg"
dotnet restore "$RepoRoot\test" --runtime "$Rid" "$NoCacheArg"
dotnet restore "$RepoRoot\tools" --runtime "$Rid" "$NoCacheArg"

$oldErrorAction=$ErrorActionPreference
$ErrorActionPreference="SilentlyContinue"
dotnet restore "$RepoRoot\testapp" --runtime "$Rid" "$NoCacheArg" 2>&1 | Out-Null
$ErrorActionPreference=$oldErrorAction

