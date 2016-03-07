#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

$CoreFxLkgVersion = Invoke-WebRequest https://raw.githubusercontent.com/eerhardt/versions/master/corefx/release/1.0.0-rc2/LKG.txt

Write-Host "got this $CoreFxLkgVersion"
