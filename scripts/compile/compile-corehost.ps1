#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

. $PSScriptRoot\..\common\_common.ps1

$CoreHostRelativeDir = "runtimes\\$RID\\native"
$CoreHostPackageName = "Microsoft.DotNet.CoreHost"
function main
{
    header "Building corehost"
    pushd "$RepoRoot\src\corehost"
    try {
        if (!(Test-Path "cmake\$Rid")) {
            mkdir "cmake\$Rid" | Out-Null
        }
        cd "cmake\$Rid"

        # Pass parameters to cmake through config files.
        $Utf8Encoding = New-Object System.Text.UTF8Encoding($False)
        [System.IO.File]::WriteAllLines("$RepoRoot\src\corehost\packaging\.relative", $CoreHostRelativeDir, $Utf8Encoding)
        [System.IO.File]::WriteAllLines("$RepoRoot\src\corehost\packaging\.name", $CoreHostPackageName, $Utf8Encoding)

        cmake ..\.. -G "Visual Studio 14 2015 Win64"
        $pf = $env:ProgramFiles
        if (Test-Path "env:\ProgramFiles(x86)") {
            $pf = (cat "env:\ProgramFiles(x86)")
        }
        $BuildConfiguration = $Configuration
        if ($Configuration -eq "Release") {
            $BuildConfiguration = "RelWithDebInfo"
        }
        & "$pf\MSBuild\14.0\Bin\MSBuild.exe" ALL_BUILD.vcxproj /p:Configuration="$BuildConfiguration"
        if (!$?) {
            Write-Host "Command failed: $pf\MSBuild\14.0\Bin\MSBuild.exe" ALL_BUILD.vcxproj /p:Configuration="$BuildConfiguration"
            Exit 1
        }
    
        if (!(Test-Path $HostDir)) {
            mkdir $HostDir | Out-Null
        }
        cp "$RepoRoot\src\corehost\cmake\$Rid\cli\$BuildConfiguration\corehost.exe" $HostDir
        cp "$RepoRoot\src\corehost\cmake\$Rid\cli\dll\$BuildConfiguration\hostpolicy.dll" $HostDir
    
        if (Test-Path "$RepoRoot\src\corehost\cmake\$Rid\cli\$BuildConfiguration\corehost.pdb")
        {
            cp "$RepoRoot\src\corehost\cmake\$Rid\cli\$BuildConfiguration\corehost.pdb" $HostDir
        }
        if (Test-Path "$RepoRoot\src\corehost\cmake\$Rid\cli\dll\$BuildConfiguration\hostpolicy.pdb")
        {
            cp "$RepoRoot\src\corehost\cmake\$Rid\cli\dll\$BuildConfiguration\hostpolicy.pdb" $HostDir
        }

        Package-Host
    } finally {
        popd
    }
}

function Package-Host
{
    $RidPrefix = "runtime.$RID"
    $PackageName = $CoreHostPackageName
    $PackageVersion=[System.IO.File]::ReadAllText("$RepoRoot\src\corehost\packaging\.version").Trim()

    $BinFiles = @{
        'ID'="$RidPrefix.$PackageName"
        'JSON'=$False
        'FILES'= @"
    <file src="corehost.exe" target="$CoreHostRelativeDir\corehost.exe"></file>
    <file src="hostpolicy.dll" target="$CoreHostRelativeDir\hostpolicy.dll"></file>
"@
    }

    $PdbFiles = @{
        'ID'="$RidPrefix.$PackageName.Symbols"
        'JSON'=$False
        'FILES'= @"
    <file src="corehost.pdb" target="$CoreHostRelativeDir\corehost.pdb"></file>
    <file src="hostpolicy.pdb" target="$CoreHostRelativeDir\hostpolicy.pdb"></file>
"@
    }

    $BinJsonFile = @{
        'ID'="$PackageName"
        'JSON'=$True
        'FILES'= @"
    <file src="$PackageName.runtime.json" target="runtime.json"></file>
"@
    }

    $PdbJsonFile = @{
        'ID'="$PackageName.Symbols"
        'JSON'=$True
        'FILES'= @"
    <file src="$PackageName.Symbols.runtime.json" target="runtime.json"></file>
"@
    }

    $NuSpecContents = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
    <metadata>
        <id>{0}</id>
        <version>{1}</version>
        <title>Host for CoreCLR</title>
        <authors>Microsoft</authors>
        <owners>Microsoft</owners>
        <licenseUrl>http://go.microsoft.com/fwlink/?LinkId=329770</licenseUrl>
        <projectUrl>https://github.com/dotnet/cli</projectUrl>
        <iconUrl>http://go.microsoft.com/fwlink/?LinkID=288859</iconUrl>
        <requireLicenseAcceptance>true</requireLicenseAcceptance>
        <description>Provides the runtime host for .NET CoreCLR</description>
        <releaseNotes>Initial release</releaseNotes>
        <copyright>Copyright © Microsoft Corporation</copyright>
    </metadata>
    <files>
        {2}
    </files>
</package>
"@

    $RuntimeJsonContents = @"
{{
  "runtimes": {{
    "win7-x64": {{
      "{0}": {{
        "runtime.win7-x64.{0}": "{1}"
      }}
    }},
    "osx.10.10-x64": {{
      "{0}": {{
        "runtime.osx.10.10-x64.{0}": "{1}"
      }}
    }},
    "ubuntu.14.04-x64": {{
      "{0}": {{
        "runtime.ubuntu.14.04-x64.{0}": "{1}"
      }}
    }},
    "centos.7-x64": {{
      "{0}": {{
        "runtime.centos.7-x64.{0}": "{1}"
      }}
    }}
  }}
}}
"@

    $Files = @($BinFiles, $PdbFiles, $BinJsonFile, $PdbJsonFile)

    ForEach ($File in $Files) {
        $Contents = $NuSpecContents -f $File.ID, $PackageVersion, $File.FILES
        $Utf8Encoding = New-Object System.Text.UTF8Encoding($False)
        $NuSpecFileName = "$HostDir\$($File.ID).nuspec"
        $RuntimeJsonFileName = "$HostDir\$($File.ID).runtime.json"
        [System.IO.File]::WriteAllLines($NuSpecFileName, $Contents, $Utf8Encoding)
        If ($File.JSON -eq $True) {
            $JsonContent = $RuntimeJsonContents -f $File.ID, $PackageVersion
            [System.IO.File]::WriteAllLines($RuntimeJsonFileName, $JsonContent, $Utf8Encoding)
        }
        Invoke-Expression "$NuGetDir\nuget.exe pack `"$NuSpecFileName`" -NoPackageAnalysis -NoDefaultExcludes -BasePath `"$HostDir`" -OutputDirectory `"$HostDir`""
    }
}

main
