#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

. $PSScriptRoot\..\common\_common.ps1

# Restore packages
header "Restoring packages"
& "$DnxRoot\dnu" restore "$RepoRoot\test\TestPackages" --quiet --runtime "$Rid"
