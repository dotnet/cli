#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

. "$PSScriptRoot\..\_common.ps1"

# Restore and compile the test app
dotnet restore "$RepoRoot\test\PackagedCommands\Commands" --runtime "osx.10.10-x64" --runtime "ubuntu.14.04-x64" --runtime "win7-x64"
if (!$?) {
    Write-Host "Command failed: dotnet restore"
    Exit 1
}

"v1", "v2" |
foreach {
    dotnet pack --output "$RepoRoot\artifacts\packages" "$RepoRoot\test\PackagedCommands\Commands\dotnet-hello\$_\dotnet-hello"
    if (!$?) {
        Write-Host "Command failed: dotnet pack"
        Exit 1
    }
}

# workaround for dotnet-restore from the root failing for these tests since their dependencies aren't built yet
dir "$RepoRoot\test\PackagedCommands\Consumers" | where {$_.PsIsContainer} | where  {$_.Name.Contains("Direct")} |
foreach {
    pushd "$RepoRoot\test\PackagedCommands\Consumers\$_"
    copy project.json.template project.json
    popd
}

#restore command consumers
pushd "$RepoRoot\test\PackagedCommands\Consumers"
dotnet restore -f "$RepoRoot\artifacts\packages" 
if (!$?) {
    Write-Host "Command failed: dotnet restore"
    Exit 1
}
popd

#compile apps
dir "$RepoRoot\test\PackagedCommands\Consumers" | where {$_.PsIsContainer} | where  {$_.Name.Contains("Direct")} |
foreach {
    pushd "$RepoRoot\test\PackagedCommands\Consumers\$_"
    dotnet compile
    popd
}

#run test
dir "$RepoRoot\test\PackagedCommands\Consumers" | where {$_.PsIsContainer} | where  {$_.Name.Contains("AppWith")} |
foreach {
    $testName = "test\PackagedCommands\Consumers\$_" 
    pushd "$RepoRoot\$testName"
    dotnet hello |
    foreach{
        if ($_ -ne "hello"){
            Write-Host "Test Failed: $testName\dotnet hello"
            Write-Host "             printed $_"
            Exit 1
        }
    }
    
    if (!$?) {
        Write-Host "Test failed: $testName\dotnet restore"
        Write-Host "             returned $LastExitCode"
        Exit 1  
    }
    popd
}

# cleanup for workaround
dir "$RepoRoot\test\PackagedCommands\Consumers" | where {$_.PsIsContainer} | where  {$_.Name.Contains("Direct")} |
foreach {
    pushd "$RepoRoot\test\PackagedCommands\Consumers\$_"
    del project.json
    popd
}