#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done

DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

source "$DIR/../_common.sh"

echo "$REPOROOT/test/PackagedCommands/Commands"
echo whereis dotnet
whereis dotnet
# Restore and compile the test app
dotnet restore "$REPOROOT/test/PackagedCommands/Commands" --runtime "osx.10.10-x64" --runtime "ubuntu.14.04-x64" --runtime "win7-x64" --no-cache --ignore-failed-sources --parallel
dotnet pack --output "$REPOROOT/artifacts/packages" "$REPOROOT/test/PackagedCommands/Commands/dotnet-hello/v1/dotnet-hello"
dotnet pack --output "$REPOROOT/artifacts/packages" "$REPOROOT/test/PackagedCommands/Commands/dotnet-hello/v2/dotnet-hello"

#compile tests with direct dependencies
for test in `ls -l "$REPOROOT/test/PackagedCommands/Consumers" | grep ^d | awk '{print $9}' | grep "Direct"`
do
    pushd "$REPOROOT/test/PackagedCommands/Consumers/$test"
    cp "project.json.template" "project.json"
    popd
done

pushd "$REPOROOT/test/PackagedCommands/Consumers"
dotnet restore -f "$REPOROOT/artifacts/packages" --no-cache --ignore-failed-sources --parallel
popd

#compile tests with direct dependencies
for test in `ls -l "$REPOROOT/test/PackagedCommands/Consumers" | grep ^d | awk '{print $9}' | grep "Direct"`
do
    pushd "$REPOROOT/test/PackagedCommands/Consumers/$test"
    dotnet compile
    popd
done

#run test
for test in `ls -l "$REPOROOT/test/PackagedCommands/Consumers" | grep ^d | awk '{print $9}' | grep "AppWith"`
do
    testName="test/PackagedCommands/Consumers/$test" 
    
    pushd "$REPOROOT/$testName"
    
    testOutput=$(dotnet hello) 
    
    rm "project.json"
    
    if [ $testOutput != "Hello" ] 
    then
        error "Test Failed: $testName/dotnet hello"
        error "             printed $testOutput"
        exit 1
    else
        echo "Test Passed: $testName"
    fi
    
    popd
done

exit 0