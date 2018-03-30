#!/usr/bin/env pwsh
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ProgressPreference = 'SilentlyContinue'

$pandocVersion = $(Invoke-WebRequest https://api.github.com/repos/jgm/pandoc/releases/latest | ConvertFrom-Json).tag_name;
$pandocVersionedName = "pandoc-$pandocVersion"
$pandoc = "$HOME/$pandocVersionedName/pandoc.exe"

# caching, so we don't have to download pandoc again
if (!(Test-Path $pandoc)) {
  Write-Host "Downloading $pandocVersionedName"
  Invoke-WebRequest -OutFile  "$pandocVersionedName.zip" `
    https://github.com/jgm/pandoc/releases/download/$pandocVersion/$pandocVersionedName-windows.zip

  Write-Host "Extracting $pandocVersionedName.zip"
  Expand-Archive "$pandocVersionedName.zip" -Force -DestinationPath $HOME/ > $null
  Write-Host "Removing master.zip"
  Remove-Item "$pandocVersionedName.zip"
}

$MANPAGE_TOOL_DIR=$PSScriptRoot

Push-Location $MANPAGE_TOOL_DIR/../sdk

Write-Host "Downloading dotnet/docs master"
Invoke-WebRequest -OutFile master.zip https://github.com/dotnet/docs/archive/master.zip > $null
Write-Host "Extracting master.zip"
Expand-Archive master.zip -Force -DestinationPath ./ > $null
Write-Host "Removing master.zip"
Remove-Item master.zip

Get-ChildItem docs-master/docs/core/tools/dotnet*.md | foreach {
  $mdFile = $_
  $manFile = [io.path]::ChangeExtension($mdFile, '1')
  Write-Host "working on $mdFile"
  &$pandoc -s -t man -V section=1 -V header=".NET Core" --column=500 --filter "$MANPAGE_TOOL_DIR/man-pandoc-filter.py" "$mdFile" -o "$manFile"
}

Remove-Item -Recurse docs-master

Pop-Location
