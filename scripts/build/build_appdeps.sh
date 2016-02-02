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
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

source "$DIR/../common/_common.sh"

APPDEPS_PROJECT_DIR="$REPOROOT/src/dotnet/commands/dotnet-compile-native/appdep"

# Get Absolute Output Dir
pushd $1
OUTPUT_DIR="$(pwd)"
popd

## App Deps ##
pushd $APPDEPS_PROJECT_DIR
dotnet restore --runtime $RID --packages $APPDEPS_PROJECT_DIR/packages $DISABLE_PARALLEL
APPDEP_SDK=$APPDEPS_PROJECT_DIR/packages/toolchain*/*/
popd

mkdir -p $OUTPUT_DIR/appdepsdk
cp -a $APPDEP_SDK/. $OUTPUT_DIR/appdepsdk
