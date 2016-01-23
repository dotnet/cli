#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

. $PSScriptRoot\..\common\_common.ps1

info "Restoring Test Packages"

# Restore packages
& "$DnxRoot\dnu" restore "$RepoRoot\test" -f "$TestPackageDir"

$oldErrorAction=$ErrorActionPreference
$ErrorActionPreference="SilentlyContinue"
& "$DnxRoot\dnu" restore "$RepoRoot\testapp" "$Rid" 2>&1 | Out-Null
$ErrorActionPreference=$oldErrorAction

