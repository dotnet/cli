#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

. "$PSScriptRoot\..\common\_common.ps1"

$failCount = 0

$TestBinRoot = "$RepoRoot\artifacts\tests"

$TestScripts = @(
    "package-command-test.ps1",
    "argument-forwarding-tests.ps1"
)

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
loadTestList | foreach {
    & ".\corerun" "xunit.console.netcore.exe" "$($_.ProjectName).dll" -xml "$($_.ProjectName)-testResults.xml" -notrait category=failing
    $exitCode = $LastExitCode
    if ($exitCode -ne 0) {
        $failingTests += "$($_.ProjectName)"
    }

    $failCount += $exitCode
}

popd

$TestScripts | ForEach-Object {
    & "$RepoRoot\scripts\test\$_"
    $exitCode = $LastExitCode
    if ($exitCode -ne 0) {
        $failingTests += "$_"
        $failCount += 1
    }
}

if ($failCount -ne 0) {
    Write-Host -ForegroundColor Red "The following tests failed."
    $failingTests | foreach {
        Write-Host -ForegroundColor Red "$_.dll failed. Logs in '$TestBinRoot\$_-testResults.xml'"
    }
} else {
    Write-Host -ForegroundColor Green "All the tests passed!"
}

Exit $failCount
