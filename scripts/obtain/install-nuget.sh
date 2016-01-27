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

say() {
    printf "%b\n" "install-nuget $1"
}

doInstall=true

NUGET_URL="https://api.nuget.org/downloads/nuget.exe"

say "Preparing to install nuget.exe to $NUGET_DIR"

if [ -e "$NUGET_DIR/nuget.exe" ] ; then
    say "You already have nuget.exe"
    doInstall=false
else
    say "nuget.exe... downloading"
fi

if [ $doInstall = true ] ; then
    rm -rf $NUGET_DIR

    mkdir -p $NUGET_DIR
    curl -o $NUGET_DIR/nuget.exe $NUGET_URL --silent
fi

