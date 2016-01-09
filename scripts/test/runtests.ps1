#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$StageToTest)

. "$PSScriptRoot\..\common\_common.ps1"

$StartPath = $env:PATH
if ($StageToTest){
    $env:DOTNET_HOME = $StageToTest
    $env:PATH = "$StageToTest\bin;$env:PATH"
}

$failCount = 0

$TestBinRoot = "$RepoRoot\artifacts\tests"

$TestProjects = @(
    "E2E",
    "StreamForwarderTests"
    "Microsoft.DotNet.Tools.Publish.Tests"
    "Microsoft.DotNet.Tools.Compiler.Tests"
)

# Publish each test project
$TestProjects | ForEach-Object {
    dotnet publish --framework "dnxcore50" --runtime "$Rid" --output "$TestBinRoot" --configuration "$Configuration" "$RepoRoot\test\$_"
    if (!$?) {
        Write-Host Command failed: dotnet publish --framework "dnxcore50" --runtime "$Rid" --output "$TestBinRoot" --configuration "$Configuration" "$RepoRoot\test\$_"
        exit 1
    }
}

## Temporary Workaround for Native Compilation
## Need x64 Native Tools Dev Prompt Env Vars
## Tracked Here: https://github.com/dotnet/cli/issues/301
pushd "$env:VS140COMNTOOLS\..\..\VC"
cmd /c "vcvarsall.bat x64&set" |
foreach {
  if ($_ -match "=") {
    $v = $_.split("=", 2); set-item -force -literalpath "ENV:\$($v[0])" -value "$($v[1])"
  }
}
popd

# copy TestProjects folder which is used by the test cases
mkdir -Force "$TestBinRoot\TestProjects"
cp -rec -Force "$RepoRoot\test\TestProjects\*" "$TestBinRoot\TestProjects"

$failCount = 0
$failingTests = @()

pushd "$TestBinRoot"

# Run each test project
$TestProjects | ForEach-Object {
    & ".\corerun" "xunit.console.netcore.exe" "$_.dll" -xml "$_-testResults.xml" -notrait category=failing
    $exitCode = $LastExitCode
    if ($exitCode -ne 0) {
        $failingTests += "$_"
    }

    $failCount += $exitCode
}

popd

if ($failCount -ne 0) {
    Write-Host -ForegroundColor Red "The following tests failed."
    $failingTests | ForEach-Object {
        Write-Host -ForegroundColor Red "$_.dll failed. Logs in '$TestBinRoot\$_-testResults.xml'"
    }
} else {
    Write-Host -ForegroundColor Green "All the tests passed!"
}

$env:PATH=$StartPath

Exit $failCount
