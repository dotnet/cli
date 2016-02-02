#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

. $PSScriptRoot\..\common\_common.ps1

$doInstall = $true

if ((Test-Path "$NuGetDir\nuget.exe")) {
    Write-Host "nuget.exe already downloaded."
    $doInstall = $false
}

if ($doInstall)
{
    Remove-Item -Recurse -Force -ErrorAction Ignore $NuGetDir
    mkdir -Force "$NuGetDir" | Out-Null

    Write-Host "Downloading nuget.exe"
    $NuGetUrl="https://api.nuget.org/downloads/nuget.exe"
    Invoke-WebRequest -UseBasicParsing "$NuGetUrl" -OutFile "$NuGetDir\nuget.exe"
}

