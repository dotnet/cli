#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# The required System.Net.Http assembly is only available in PSv3+ / .NET v4.5+.
#requires -Version 3

<#
.SYNOPSIS
    Installs the dotnet CLI and .NET Core SDK and/or runtime(s).
.DESCRIPTION
    If the implied or specified version already exists in the target directory, 
    no action is taken.
    However, you can install additional versions, side by side.
.PARAMETER Channel
    Default: LTS
    Download from the Channel specified. Possible values:
    - Current - most current release
    - LTS - most current supported release
    - 2-part version in a format A.B - represents a specific release
          examples: 2.0, 1.0
    - Branch name
          examples: release/2.0.0, Master
.PARAMETER Version
    Default: latest
    Represents a build version on specific channel. Possible values:
    - latest - most latest build on specific channel
    - coherent - most latest coherent build on specific channel
          coherent applies only to SDK downloads and is treated the same as
          latest when using -Runtime / -SharedRuntime
    - 3-part version in a format A.B.C - represents specific version of build
          examples: 2.0.0-preview2-006120, 1.1.0
.PARAMETER InstallDir
    Default: $env:LocalAppData\Microsoft\dotnet (Windows); $HOME/.dotnet (Unix)
             or, if preset, the value of environment var. DOTNET_INSTALL_DIR
    The path of the local target directory for the installation.
    Note that the dotnet CLI will be placed directly in that directory.
.PARAMETER Architecture
    Default: the current OS architecture.
    The architecture of the dotnet binaries to be installed.
    Possible values are: amd64, x64, x86, arm64, arm, x86 (Windows only)
.PARAMETER SharedRuntime
    This parameter is obsolete and may be removed in a future version of this 
    script. Please use '-Runtime dotnet' instead.
.PARAMETER Runtime
    Installs just a shared runtime, not the entire SDK.
    Possible values:
        - dotnet     - the Microsoft.NETCore.App shared runtime
        - aspnetcore - the Microsoft.AspNetCore.App shared runtime
.PARAMETER RID
    Unix-like platforms only: Installs the binaries for the given
    platform RID (use linux-x64 for portable linux).
.PARAMETER WhatIf
    If specified, will not perform installation and instead display what 
    command line to use to consistently to install the  implied or requested
    version. For example, if you specify version 'latest' it will display a
    command line with a specific version that can be used deterministically in
    a build script.
    It also displays the download URLs and target installation directory, if 
    you prefer to download and install yourself.
.PARAMETER NoPath
    If specified, this script will only display the binaries location after
    installation, without updating the PATH environment variable.
    By default, this script will update PATH by prepending the binaries folder,
    but only for the current process. 
    Important: Persistently adding this folder to your PATH variable requires
    manual action, as appropriate for your platform or shell.
.PARAMETER Verbose
    Displays diagnostic information during the installation.
.PARAMETER AzureFeed
    Default: https://dotnetcli.azureedge.net/dotnet
    This parameter typically is not changed by the user.
    It allows changing the URL for the Azure feed used by this installer.
.PARAMETER UncachedFeed
    This parameter typically is not changed by the user.
    It allows changing the URL for the uncached feed used by this installer.
.PARAMETER FeedCredential
    Used as a query string to append to the Azure feed.
    It allows changing the URL to use non-public blob storage accounts.
.PARAMETER ProxyAddress
    If specified, the given proxy will be used when making web request.
.PARAMETER ProxyUseDefaultCredentials
    Use default credentials for the given proxy.
.PARAMETER SkipNonVersionedFiles
    Skips installing non-versioned files if they already exist, such as the 
    dotnet binary.
.PARAMETER NoCdn
    Disable downloading from the Azure CDN, and use the uncached feed directly.
#>
[cmdletbinding(PositionalBinding=$false)]
param(
    [ArgumentCompleter({ param($cmd, $param, $wordToComplete) 'Current', 'LTS' -like "$wordToComplete*" })]
    [string]$Channel='LTS',
    [ArgumentCompleter({ param($cmd, $param, $wordToComplete) 'latest', 'coherent' -like "$wordToComplete*" })]
    [string]$Version='latest',
    [string]$InstallDir,
    [ArgumentCompleter({ param($cmd, $param, $wordToComplete) $vals = 'amd64', 'x64', 'arm64', 'arm'; if ($env:OS -eq 'Windows_NT') { $vals += 'x86' }; $vals -like "$wordToComplete*" })]
    [string]$Architecture,
    [ValidateSet('dotnet', 'aspnetcore')]
    [string]$Runtime,
    [Alias('RuntimeId')]
    [string]$RID,
    [Obsolete("This parameter may be removed in a future version of this script. The recommended alternative is '-Runtime dotnet'.")]
    [switch]$SharedRuntime,
    [Alias('DryRun')]
    [switch]$WhatIf,
    [switch]$NoPath,
    [string]$AzureFeed='https://dotnetcli.azureedge.net/dotnet',
    [string]$UncachedFeed='https://dotnetcli.blob.core.windows.net/dotnet',
    [string]$FeedCredential,
    [string]$ProxyAddress,
    [switch]$ProxyUseDefaultCredentials,
    [switch]$SkipNonVersionedFiles,
    [switch]$NoCdn
)

Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

function Say($str) {
    Write-Host "dotnet-install: $str"
}

function SayVerbose($str) {
    Write-Verbose "dotnet-install: $str"
}
function SayWarning($str) {
    Write-Warning "dotnet-install: $str"
}

function SayInvocation($Invocation) {
    $command = $Invocation.MyCommand;
    $args = (($Invocation.BoundParameters.Keys | ForEach-Object { "-$_ `"$($Invocation.BoundParameters[$_])`"" }) -join " ")
    SayVerbose "$command $args"
}

# Makes sure that $IsWindows reflects $True on Windows (predefined only in PS Core).
if (-not (Get-Variable -ErrorAction SilentlyContinue -Scope Global IsWindows)) {
    $IsWindows = $env:OS -eq 'Windows_NT'
}

# As a courtesy, allow case variations of abstract -Channel and -Runtime values.
# (For abstract -Runtime and -Version values, case doesn't matter.)
if ($Channel) {
    $caseExactValue = @{
        current = 'Current'
        lts = 'LTS'
    }[$Channel]
    if ($caseExactValue) { $Channel = $caseExactValue }
}

if ($NoCdn) {
    $AzureFeed = $UncachedFeed
}

# For backward compatibility, allow '<allow>' to represent default behavior.
if ($Version -eq '<auto>') { $Version = $null }
if ($InstallDir -eq '<auto>') { $InstallDir = $null }

if ($SharedRuntime) {
    if ($Runtime) {
        SayWarning "-Runtime overrides -SharedRuntime."
    } else {
        $Runtime = "dotnet"
    }
}

if ($RID -and $IsWindows) {
    SayWarning "Ignoring -RID argument, because it isn't supported on Windows."
}

# example path with regex: shared/1.0.0-beta-12345/somepath
$VersionRegEx="/\d+\.\d+[^/]+/"
$OverrideNonVersionedFiles = !$SkipNonVersionedFiles

function Invoke-WithRetry([ScriptBlock]$ScriptBlock, [int]$MaxAttempts = 3, [int]$SecondsBetweenAttempts = 1) {
    $Attempts = 0

    while ($true) {
        try {
            return $ScriptBlock.Invoke()
        }
        catch {
            $Attempts++
            if ($Attempts -lt $MaxAttempts) {
                Start-Sleep $SecondsBetweenAttempts
            }
            else {
                throw
            }
        }
    }
}

# This platform list is finite - if the SDK/Runtime has supported Linux distribution-specific assets,
#   then and only then should the Linux distribution appear in this list.
# Adding a Linux distribution to this list does not imply distribution-specific support.
function Get-LegacyOsNameFromPlatform($Platform) {
    switch -wildcard ($Platform) {
        'centos.7'      { return 'centos' }
        'debian.8'      { return 'debian' }
        'debian.9'      { return 'debian.9' }
        'fedora.23'     { return 'fedora.23' }
        'fedora.24'     { return 'fedora.24' }
        'fedora.27'     { return 'fedora.27' }
        'fedora.28'     { return 'fedora.28' }
        'opensuse.13.2' { return 'opensuse.13.2' }
        'opensuse.42.1' { return 'opensuse.42.1' }
        'opensuse.42.3' { return 'opensuse.42.3' }
        'rhel.7*'       { return 'rhel' }
        'ubuntu.14.04'  { return 'ubuntu' }
        'ubuntu.16.04'  { return 'ubuntu.16.04' }
        'ubuntu.16.10'  { return 'ubuntu.16.10' }
        'ubuntu.18.04'  { return 'ubuntu.18.04' }
        'alpine.3.4.3'  { return 'alpine' }
    }
}

function Get-LinuxPlatformName {
    SayInvocation $MyInvocation

    if ($RID) {
        return $RID -replace '^(.+)-.*', '$1'
    } else {
        if (Test-Path /etc/os-release) {
            return sh -c '. /etc/os-release; printf %s "$ID.$VERSION_ID"'
        } elseif (Test/etc/redhat-release) {
            $redHatRelease = Get-Content /etc/redhat-release
            if ($redHatRelease -like "CentOS release 6.*" -or $redHatRelease -like "Red Hat Enterprise Linux Server release 6.*") {
                return "rhel.6"
            }
        }
    }

    SayVerbose "Linux specific platform name and version could not be detected: uname -a = $(uname -a)"
}

function Get-CurrentOsName {
    SayInvocation $MyInvocation

    if ($IsWindows) {
        return 'win'
    } else { # Unix
        switch (uname) {
            'Darwin'  { return 'osx' }
            'FreeBSD' { return 'freebsd' }
            'Linux' {
                $linuxPlatformName = Get-LinuxPlatformName
                if (-not $linuxPlatformName) { return 'linux' }
                
                switch -wildcard ($linuxPlatformName) {
                    'rhel.6'  { return $_ }
                    'alpine*' { return 'linux-musl' }
                    default   { return 'linux' }
                }
            }
        }

        throw "OS name could not be detected: uname -a = $(uname -a)"
    }
}

function Get-LegacyOSName {
    SayInvocation $MyInvocation

    if ($IsWindows) {
        return 'win'
    } else { # Unix
        if ((uname) -eq 'Darwin') {
            return 'osx' 
        } elseif ($RID) {
            $platform = $RID -replace '(.+)-.*', '$1'
            $legacyOsNameFromPlatform = Get-LegacyOsNameFromPlatform $platform
            if ($legacyOsNameFromPlatform) { 
                return $legacyOsNameFromPlatform
            } else {
                return $platform
            }
        } else {
            if (Test-Path /etc/os-release) {
                $platform = sh -c '. /etc/os-release; printf %s "$ID.$VERSION_ID"'
                $legacyOsNameFromPlatform = Get-LegacyOsNameFromPlatform $platform
                if ($legacyOsNameFromPlatform) { 
                    return $legacyOsNameFromPlatform
                }
            }
        }

        SayVerbose "Distribution specific OS name and version could not be detected: uname -a = $(uname -a)"
    }
}

function Test-PreReqs {
    SayInvocation $MyInvocation

    if ($env:DOTNET_INSTALL_SKIP_PREREQS -eq '1') { return }

    if ($IsWindows) {
        # Min. PS version is enforced via the `#requires -version` directive.
    } else { # Unix
        if ((uname) -eq 'Linux') {
            try {
                $ldconfigBin = (Get-Command -ErrorAction SilentlyContinue ldconfig, /sbin/ldconfig)[0].Source
            } catch {
                SayWarning "Cannot locate utility ldconfig, skipping prerequisites check."
                return
            }

            $libPathList = $env:LD_LIBRARY_PATH -replace ':', ' '
            # Note: In combination with $ErrorActionPreference = 'Stop', 2>$null would trigger a terminating error if there is stderr output.
            $availableLibs = & { $ErrorActionPreference = 'Continue'; (& $ldconfigBin -NXv $libPathList 2>$null) -join "`n" }
            foreach ($lib in 'libunwind', 'libssl', 'libicu', 'libcurl.so') {
                if ($availableLibs -notlike "*$lib*") { SayWarning "Unable to locate $lib. Probable prerequisite missing; please install $lib." }
            }
        }
    }
}

function Get-MachineArchitecture {
    SayInvocation $MyInvocation

    if ($IsWindows) {
        $ENV:PROCESSOR_ARCHITECTURE
    } else { # Unix-like platforms.
        switch($(try { uname -m } catch {})) {
            'armv7l'  { return 'arm' }
            'aarch64' { return 'arm64' }
            default   { return 'x64' } # Always default to 'x64'
        }
    }
}

function Get-CLIArchitectureFromArchitecture([string]$Architecture) {
    SayInvocation $MyInvocation

    if (-not $Architecture) { $Architecture = Get-MachineArchitecture }

    switch ($Architecture) { # Note: input may be uppercase.
        'amd64'  { return 'x64' }
        'x64'    { return 'x64' }
        'x86'    { return 'x86' } # Windows only
        'arm'    { return 'arm' }
        'arm64'  { return 'arm64' }
        default { throw "Architecture '$Architecture' not supported. If you think this is a bug, please report it at https://github.com/dotnet/cli/issues" }
    }
}

# The version text returned from the feeds is 1- to 2-line string:
# for the SDK and the dotnet runtime (2 lines):
#   Line 1: # commit_hash (e.g., '4c506e0f35ce663f28fcb758387a98daa544e070')
#   Line 2: # 3-component version number (e.g., 2.1.503)
# for the aspnetcore runtime (1 line):
#   Line 1: # 3-compontent version number
function Get-VersionInfoFromVersionText([string]$VersionText) {
    SayInvocation $MyInvocation

    $Data = -split $VersionText

    $VersionInfo = @{
        CommitHash = $(if ($Data.Count -gt 1) { $Data[0] })
        Version = $Data[-1] # last line is always the version number.
    }
    return $VersionInfo
}

function Add-Assembly([string] $Name) {
    try {
        Add-Type -AssemblyName $Name | Out-Null
    }
    catch {
        # On Nano Server, Powershell Core Edition is used.  Add-Type is unable to resolve base class assemblies because they are not GAC'd.
        # Loading the base class assemblies is not necessary as the types will automatically get resolved.
    }
}

function GetHTTPResponse([Uri] $Uri)
{
    Invoke-WithRetry(
    {

        $HttpClient = $null

        try {
            # HttpClient is used vs Invoke-WebRequest in order to support Nano Server which doesn't support the Invoke-WebRequest cmdlet.
            Add-Assembly -Name System.Net.Http

            if(-not $ProxyAddress) {
                try {
                    # Despite no proxy being explicitly specified, we may still be behind a default proxy
                    $DefaultProxy = [System.Net.WebRequest]::DefaultWebProxy;
                    if($DefaultProxy -and (-not $DefaultProxy.IsBypassed($Uri))) {
                        $ProxyAddress = $DefaultProxy.GetProxy($Uri).OriginalString
                        $ProxyUseDefaultCredentials = $true
                    }
                } catch {
                    # Eat the exception and move forward as the above code is an attempt
                    #    at resolving the DefaultProxy that may not have been a problem.
                    $ProxyAddress = $null
                    SayVerbose "Exception ignored: $_.Exception.Message - moving forward..."
                }
            }

            if($ProxyAddress) {
                $HttpClientHandler = New-Object System.Net.Http.HttpClientHandler
                $HttpClientHandler.Proxy =  New-Object System.Net.WebProxy -Property @{Address=$ProxyAddress;UseDefaultCredentials=$ProxyUseDefaultCredentials}
                $HttpClient = New-Object System.Net.Http.HttpClient -ArgumentList $HttpClientHandler
            }
            else {

                $HttpClient = New-Object System.Net.Http.HttpClient
            }
            # Default timeout for HttpClient is 100s.  For a 50 MB download this assumes 500 KB/s average, any less will time out
            # 10 minutes allows it to work over much slower connections.
            $HttpClient.Timeout = New-TimeSpan -Minutes 20
            $Response = $HttpClient.GetAsync("${Uri}${FeedCredential}").Result
            if (-not $Response -or -not $Response.IsSuccessStatusCode) {
                 # The feed credential is potentially sensitive info. Do not log FeedCredential to console output.
                $ErrorMsg = "Failed to download $Uri."
                if ($Response) {
                    $ErrorMsg += "  $Response"
                }

                throw $ErrorMsg
            }

             return $Response
        }
        finally {
             if ($HttpClient) {
                $HttpClient.Dispose()
            }
        }
    })
}


function Get-LatestVersionInfo([string]$AzureFeed, [string]$Channel, [switch]$Coherent) {
    SayInvocation $MyInvocation

    $VersionFileUrl = $null
    if ($Runtime -eq "dotnet") {
        $VersionFileUrl = "$UncachedFeed/Runtime/$Channel/latest.version"
    }
    elseif ($Runtime -eq "aspnetcore") {
        $VersionFileUrl = "$UncachedFeed/aspnetcore/Runtime/$Channel/latest.version"
    }
    elseif (-not $Runtime) { # SDK with both runtimes
        if ($Coherent) {
            $VersionFileUrl = "$UncachedFeed/Sdk/$Channel/latest.coherent.version"
        }
        else {
            $VersionFileUrl = "$UncachedFeed/Sdk/$Channel/latest.version"
        }
    }
    else {
        throw "Invalid value for `$Runtime"
    }

    $Response = GetHTTPResponse -Uri $VersionFileUrl
    $StringContent = $Response.Content.ReadAsStringAsync().Result

    switch ($Response.Content.Headers.ContentType) {
        { 'application/octet-stream', 'text/plain', 'text/plain; charset=UTF-8' -contains $_ } { $VersionText = $StringContent }
        default { throw "``$_`` is an unknown .version file content type." }
    }

    $VersionInfo = Get-VersionInfoFromVersionText $VersionText

    return $VersionInfo
}


function Get-SpecificVersionFromVersion([string]$AzureFeed, [string]$Channel, [string]$Version) {
    SayInvocation $MyInvocation

    switch ($Version) {
        'latest' {
            $LatestVersionInfo = Get-LatestVersionInfo -AzureFeed $AzureFeed -Channel $Channel
            return $LatestVersionInfo.Version
        }
        'coherent' {
            $LatestVersionInfo = Get-LatestVersionInfo -AzureFeed $AzureFeed -Channel $Channel -Coherent
            return $LatestVersionInfo.Version
        }
        default { return $Version }
    }
}

function Get-DownloadLink([string]$AzureFeed, [string]$SpecificVersion, [string]$CLIArchitecture) {
    SayInvocation $MyInvocation

    $osName = Get-CurrentOsName

    $archiveExtension = if ($IsWindows) { 'zip' } else { 'tar.gz'}

    if ($Runtime -eq "dotnet") {
        $PayloadURL = "$AzureFeed/Runtime/$SpecificVersion/dotnet-runtime-$SpecificVersion-$osName-$CLIArchitecture.$archiveExtension"
    }
    elseif ($Runtime -eq "aspnetcore") {
        $PayloadURL = "$AzureFeed/aspnetcore/Runtime/$SpecificVersion/aspnetcore-runtime-$SpecificVersion-$osName-$CLIArchitecture.$archiveExtension"
    }
    elseif (-not $Runtime) {
        $PayloadURL = "$AzureFeed/Sdk/$SpecificVersion/dotnet-sdk-$SpecificVersion-$osName-$CLIArchitecture.$archiveExtension"
    }
    else {
        throw "Invalid value for `$Runtime"
    }

    SayVerbose "Constructed primary payload URL: $PayloadURL"

    return $PayloadURL
}

function Get-LegacyDownloadLink([string]$AzureFeed, [string]$SpecificVersion, [string]$CLIArchitecture) {
    SayInvocation $MyInvocation

    $distroSpecificOsName = Get-LegacyOSName

    $archiveExtension = if ($IsWindows) { 'zip' } else { 'tar.gz'}

    if (-not $Runtime) {
        $PayloadURL = "$AzureFeed/Sdk/$SpecificVersion/dotnet-dev-$distroSpecificOsName-$CLIArchitecture.$SpecificVersion.$archiveExtension"
    }
    elseif ($Runtime -eq "dotnet") {
        $PayloadURL = "$AzureFeed/Runtime/$SpecificVersion/dotnet-$distroSpecificOsName-$CLIArchitecture.$SpecificVersion.$archiveExtension"
    }
    else {
        return $null
    }

    SayVerbose "Constructed legacy payload URL: $PayloadURL"

    return $PayloadURL
}

function Get-DefaultInstallationPath {
    SayInvocation $MyInvocation

    $InstallRoot = $env:DOTNET_INSTALL_DIR
    if (-not $InstallRoot) {
        $InstallRoot = if ($IsWindows) {
                         "$env:LocalAppData\Microsoft\dotnet"
                       } else { # Unix-like platforms
                         "$HOME/.dotnet"
                       }
    }
    return $InstallRoot
}

function Resolve-InstallationPath([string]$InstallDir) {
    SayInvocation $MyInvocation

    if (-not $InstallDir) {
        return Get-DefaultInstallationPath
    }
    return $InstallDir
}

function Get-RepeatableInvocationCommandLine([string]$SpecificVersion) {
    $thisScriptName = Split-Path -Leaf -Path $PSCommandPath

    # Retain all explicitly bound parameters, except -WhatIf.
    # Always include the resolved -Version.
    $versionPresent = $false
    $paramTokens = foreach ($p in $script:PSBoundParameters.GetEnumerator()) {
        if ($p.Value -is [switch]) {
            if ($p.Key -ne 'WhatIf') {
                if ($p.Value) { "-$($p.Key)" }
            }
        } else {
            $value = $p.Value
            if ($p.Key -eq 'Version') {
                $versionPresent = $true
                if ($value -notmatch '\d') { $value = $SpecificVersion }
            }
            # Quote the argument, if needed; this is fully robust, but should do for this script.
            if ($value -match ' ') { $value = '"{0}"' -f $value }
            "-$($p.Key)", $value
        }
    }
    if (-not $versionPresent) {
        $paramTokens += '-Version', $SpecificVersion
    }

    return "$thisScriptName $paramTokens"
}

# Note: Appears to be unused.
function Get-VersionInfoFromVersionFile([string]$InstallRoot, [string]$RelativePathToVersionFile) {
    SayInvocation $MyInvocation

    $VersionFile = Join-Path -Path $InstallRoot -ChildPath $RelativePathToVersionFile
    SayVerbose "Local version file: $VersionFile"

    if (Test-Path $VersionFile) {
        $VersionText = cat $VersionFile
        SayVerbose "Local version file text: $VersionText"
        return Get-VersionInfoFromVersionText $VersionText
    }

    SayVerbose "Local version file not found."

    return $null
}

function Test-DotnetPackageInstalled([string]$InstallRoot, [string]$RelativePathToPackage, [string]$SpecificVersion) {
    SayInvocation $MyInvocation

    $DotnetPackagePath = Join-Path -Path $InstallRoot -ChildPath $RelativePathToPackage | Join-Path -ChildPath $SpecificVersion
    SayVerbose "Test-DotnetPackageInstalled: Path to a package: $DotnetPackagePath"
    return Test-Path $DotnetPackagePath -PathType Container
}

function Get-PathPrefixWithVersion($path) {
    $match = [regex]::match($path, $VersionRegEx)
    if ($match.Success) {
        return $entry.FullName.Substring(0, $match.Index + $match.Length)
    }

    return $null
}

function Get-ListOfDirectoriesAndVersionsToUnpackFromDotnetZipPackage([System.IO.Compression.ZipArchive]$Zip, [string]$OutPath) {
    SayInvocation $MyInvocation

    $ret = foreach ($entry in $Zip.Entries) {
        $dir = Get-PathPrefixWithVersion $entry.FullName
        if ($dir) {
            $path = Join-Path -Path $OutPath -ChildPath $dir
            if (-Not (Test-Path $path -PathType Container)) {
                $dir
            }
        }
    }

    $ret = $ret | Sort-Object -Unique

    SayVerbose "Directories to unpack: $($ret -join ";")"

    return $ret
}

# Example archive content and extraction algorithm:
# Rule: files if extracted are always being extracted to the same relative path locally
# ./
#       dotnet  # file does not exist locally, extract
#       b.dll   # file exists locally, override only if $OverrideNonVersionedFiles set
#       aaa/    # same rules as for files
#           ...
#       abc/1.0.0/  # directory contains version and exists locally
#           ...     # do not extract content under versioned part
#       abc/asd/    # same rules as for files
#            ...
#       def/ghi/1.0.1/  # directory contains version and does not exist locally
#           ...         # extract content
function Expand-DotnetPackage([string]$ArchivePath, [string]$OutPath) {
    SayInvocation $MyInvocation

    if ($IsWindows) { # Windows: .zip file
        Add-Assembly -Name System.IO.Compression.FileSystem
        $Zip = $null
        try {
            $Zip = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
    
            $DirectoriesToUnpack = Get-ListOfDirectoriesAndVersionsToUnpackFromDotnetZipPackage -Zip $Zip -OutPath $OutPath
    
            foreach ($entry in $Zip.Entries) {
                $PathWithVersion = Get-PathPrefixWithVersion $entry.FullName
                if ((-not $PathWithVersion) -or ($DirectoriesToUnpack -contains $PathWithVersion)) {
                    $DestinationPath = Join-Path -Path $OutPath -ChildPath $entry.FullName
                    $DestinationDir = Split-Path -Parent $DestinationPath
                    $OverrideFiles = $OverrideNonVersionedFiles -or (-not (Test-Path $DestinationPath))
                    if ((-not $DestinationPath.EndsWith([IO.Path]::DirectorySeparatorChar)) -and $OverrideFiles) {
                        New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null
                        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $DestinationPath, $OverrideNonVersionedFiles)
                    }
                }
            }
        }
        finally {
            if ($Zip) {
                $Zip.Dispose()
            }
        }
    } else {  # Unix: .tar.gz file
        $sharedTempDir = if ($env:TMPDIR) { $env:TMPDIR } else { '/tmp' }
        $tempOutPath = mktemp -d (Join-Path $sharedTempDir dotnet.XXXXXXXXX)

        tar -xzf $ArchivePath -C $tempOutPath >$null
        $failed = $LASTEXITCODE -ne 0

        if (-not $failed) {
            $foldersWithVersionRegex='(^.*/[0-9]+\.[0-9]+[^/]+/).*'
            $files = @(find $tempOutPath -type f | sort)
            $uniqueFoldersWithVersionNumbers = @(foreach ($file in $files) { if ($file -match $foldersWithVersionRegex) { $Matches[1] } }) | Get-Unique
            Copy-FileOrDirsFromList -RootPath $tempOutPath -OutPath $OutPath -FilesOrDirs $uniqueFoldersWithVersionNumbers
            Copy-FileOrDirsFromList -RootPath $tempOutPath -OutPath $OutPath -FilesOrDirs ($files -notmatch $foldersWithVersionRegex) -Override:$OverrideNonVersionedFiles
        }

        rm -rf $tempOutPath

        if ($failed) {
            Throw "Extraction failed."
        }
    }
}

function Copy-FileOrDirsFromList {
    param(
        [string]   $RootPath,
        [string]   $OutPath,
        [string[]] $FilesOrDirs,
        [switch]   $Override
    )

    $RootPath = Remove-TrailingSlash $RootPath
    $OutPath  = Remove-TrailingSlash $OutPath

    $OverrideOption = if (-not $Override) {
        if ((Get-CurrentOsName) -eq 'linux-musl') {
            '-u' 
        } else {
            '-n'
        }
    }

    foreach ($fileOrDir in $FilesOrDirs) {
        $relativePath = $fileOrDir.Substring($RootPath.Length + 1)
        $targetPath = Join-Path $OutPath $relativePath
        if ($Override -or (-not (Test-Path $targetPath))) {
            mkdir -p (Split-Path -Parent $targetPath)
            cp -R $OverrideOption $fileOrDir $targetPath
        }
    }
}

function DownloadFile($Source, [string]$OutPath) {
    if ($Source -notlike "http*") {
        $Source = Convert-Path $Source
        Say "Copying file from $Source to $OutPath"
        Copy-Item $Source $OutPath
        return
    }

    $Stream = $null

    try {
        $Response = GetHTTPResponse -Uri $Source
        $Stream = $Response.Content.ReadAsStreamAsync().Result
        $File = [System.IO.File]::Create($OutPath)
        $Stream.CopyTo($File)
        $File.Close()
    }
    finally {
        if ($Stream) {
            $Stream.Dispose()
        }
    }
}

function Remove-TrailingSlash([string] $Path) {
    return $Path.TrimEnd([System.IO.Path]::DirectorySeparatorChar)
}

function Add-SdkInstallRootToStartOfPath([string]$InstallRoot, [string]$BinFolderRelativePath) {
    $BinPath = Remove-TrailingSlash (Join-Path -Path $InstallRoot -ChildPath $BinFolderRelativePath)
    if (-not $NoPath) {
        if (($env:PATH -split [IO.Path]::PathSeparator) -notcontains $BinPath) {
            Say "Prepending to current process PATH: `"$BinPath`". Note: This change will not be visible if PowerShell was run as a child process. Persisting this change must be done manually, as appropriate for your platform or shell."
            $env:PATH = $BinPath + [IO.Path]::PathSeparator + $env:PATH
        } else {
            SayVerbose "Current process PATH already contains `"$BinPath`""
        }
    }
    else {
        Say "Binaries of dotnet can be found in $BinPath"
    }
}

$BinFolderRelativePath="" # The `dotnet` executable is placed directly in the installation folder.
$CLIArchitecture = Get-CLIArchitectureFromArchitecture $Architecture
$SpecificVersion = Get-SpecificVersionFromVersion -AzureFeed $AzureFeed -Channel $Channel -Version $Version
$DownloadLink = Get-DownloadLink -AzureFeed $AzureFeed -SpecificVersion $SpecificVersion -CLIArchitecture $CLIArchitecture
$LegacyDownloadLink = Get-LegacyDownloadLink -AzureFeed $AzureFeed -SpecificVersion $SpecificVersion -CLIArchitecture $CLIArchitecture
$InstallRoot = Resolve-InstallationPath $InstallDir

if ($WhatIf) {
    Say "Payload URLs:"
    Say "  Primary - $DownloadLink"
    if ($LegacyDownloadLink) {
        Say "  Legacy  - $LegacyDownloadLink"
    }
    Say "Target directory:      $InstallRoot"
    Say "Repeatable invocation: $(Get-RepeatableInvocationCommandLine $SpecificVersion)"
    exit 0
}

Test-PreReqs

SayVerbose "InstallRoot: $InstallRoot"

if ($Runtime -eq 'dotnet') {
    $assetName = '.NET Core Runtime'
    $dotnetPackageRelativePath = 'shared/Microsoft.NETCore.App'
}
elseif ($Runtime -eq 'aspnetcore') {
    $assetName = 'ASP.NET Core Runtime'
    $dotnetPackageRelativePath = 'shared/Microsoft.AspNetCore.App'
}
elseif (-not $Runtime) {
    $assetName = '.NET Core SDK'
    $dotnetPackageRelativePath = 'sdk'
}
else {
    throw "Invalid value for `$Runtime"
}

#  Check if the SDK version is already installed.
$isAssetInstalled = Test-DotnetPackageInstalled -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $SpecificVersion
if ($isAssetInstalled) {
    Say "$assetName version $SpecificVersion is already installed."
    Add-SdkInstallRootToStartOfPath -InstallRoot $InstallRoot -BinFolderRelativePath $BinFolderRelativePath
    exit 0
}

New-Item -ItemType Directory -Force -Path $InstallRoot | Out-Null

$installDrive = (Get-Item -Force $InstallRoot).PSDrive.Name
$diskInfo = Get-PSDrive -Name $installDrive
if ($diskInfo.Free / 1MB -le 100) {
    Say "There is not enough disk space on drive ${installDrive}:"
    exit 0
}

# Note: On Windows, the archive type is .zip, on Unix it is .tar.gz
$ArchivePath = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
SayVerbose "Archive path: $ArchivePath"

$downloadFailed = $false
Say "Downloading link: $DownloadLink"
try {
    DownloadFile -Source $DownloadLink -OutPath $ArchivePath
}
catch {
    Say "Cannot download: $DownloadLink"
    if ($LegacyDownloadLink) {
        $DownloadLink = $LegacyDownloadLink
        $ArchivePath = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
        SayVerbose "Legacy archive path: $ArchivePath"
        Say "Downloading legacy link: $DownloadLink"
        try {
            DownloadFile -Source $DownloadLink -OutPath $ArchivePath
        }
        catch {
            Say "Cannot download: $DownloadLink"
            $downloadFailed = $true
        }
    }
    else {
        $downloadFailed = $true
    }
}

if ($downloadFailed) {
    throw "Could not find/download: `"$assetName`" with version = $SpecificVersion`nRefer to: https://aka.ms/dotnet-os-lifecycle for information on .NET Core support"
}

Say "Extracting archive from $DownloadLink"
Expand-DotnetPackage -ArchivePath $ArchivePath -OutPath $InstallRoot

#  Check if the SDK version is now installed; if not, fail the installation.
$isAssetInstalled = Test-DotnetPackageInstalled -InstallRoot $InstallRoot -RelativePathToPackage $dotnetPackageRelativePath -SpecificVersion $SpecificVersion
if (!$isAssetInstalled) {
    throw "`"$assetName`" with version $SpecificVersion failed to install with an unknown error."
}

Remove-Item $ArchivePath

Add-SdkInstallRootToStartOfPath -InstallRoot $InstallRoot -BinFolderRelativePath $BinFolderRelativePath

Say "Installation finished"
exit 0
