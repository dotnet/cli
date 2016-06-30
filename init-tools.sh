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
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
SOURCE="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

__init_tools_log=$SOURCE/init-tools.log
__PACKAGES_DIR=$SOURCE/.nuget/packages
__TOOLRUNTIME_DIR=$SOURCE/build_tools
__DOTNET_PATH=$SOURCE/.dotnet_stage0
__DOTNET_CMD=$__DOTNET_PATH/dotnet
if [ -z "$__BUILDTOOLS_SOURCE" ]; then __BUILDTOOLS_SOURCE=https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json; fi
__BUILD_TOOLS_PACKAGE_VERSION=$(cat $SOURCE/BuildToolsVersion.txt)
__BUILD_TOOLS_PATH=$__PACKAGES_DIR/Microsoft.DotNet.BuildTools/$__BUILD_TOOLS_PACKAGE_VERSION/lib
__PROJECT_JSON_PATH=$__TOOLRUNTIME_DIR/$__BUILD_TOOLS_PACKAGE_VERSION
__PROJECT_JSON_FILE=$__PROJECT_JSON_PATH/project.json
__PROJECT_JSON_CONTENTS="{ \"dependencies\": { \"Microsoft.DotNet.BuildTools\": \"$__BUILD_TOOLS_PACKAGE_VERSION\" }, \"frameworks\": { \"netcoreapp1.0\": { } } }"
__CHANNEL=$(awk -F "=" '/CHANNEL/ {print $2}' "$SOURCE/branchinfo.txt")

# Increases the file descriptors limit for this bash. It prevents an issue we were hitting during restore
FILE_DESCRIPTOR_LIMIT=$( ulimit -n )
if [ $FILE_DESCRIPTOR_LIMIT -lt 1024 ]
then
    echo "Increasing file description limit to 1024"
    ulimit -n 1024
fi

# Disable first run since we want to control all package sources
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if [ ! -e "$__PROJECT_JSON_FILE" ]; then
    "$SOURCE/scripts/obtain/dotnet-install.sh" --install-dir "$__DOTNET_PATH" --channel $__CHANNEL

    mkdir -p "$__PROJECT_JSON_PATH"
    echo "$__PROJECT_JSON_CONTENTS" > "$__PROJECT_JSON_FILE"

    if [ ! -d "$__BUILD_TOOLS_PATH" ]; then
        echo "Restoring BuildTools version $__BUILD_TOOLS_PACKAGE_VERSION..."
        echo "Running: $__DOTNET_CMD restore \"$__PROJECT_JSON_FILE\" --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE" >> "$__init_tools_log" 2>&1
        "$__DOTNET_CMD" restore "$__PROJECT_JSON_FILE" --packages "$__PACKAGES_DIR" --source "$__BUILDTOOLS_SOURCE" >> "$__init_tools_log" 2>&1
        if [ ! -e "$__BUILD_TOOLS_PATH/init-tools.sh" ]; then echo "ERROR: Could not restore build tools correctly. See '$__init_tools_log' for more details."; fi
    fi

    echo "Initializing BuildTools..."
    echo "Running: $__BUILD_TOOLS_PATH/init-tools.sh $SOURCE $__DOTNET_CMD $__TOOLRUNTIME_DIR" >> "$__init_tools_log" 2>&1
    "$__BUILD_TOOLS_PATH/init-tools.sh" "$SOURCE" "$__DOTNET_CMD" "$__TOOLRUNTIME_DIR" >> "$__init_tools_log" 2>&1
    echo "Done initializing tools."
else
    echo "Tools are already initialized"
fi