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

# Use Stage0 Binaries
export PATH="$DOTNET_INSTALL_DIR/bin:$PATH"

header "Restoring packages"

dotnet restore "$REPOROOT/src" --quiet "$NOCACHE"
dotnet restore "$REPOROOT/test" --quiet "$NOCACHE"
dotnet restore "$REPOROOT/tools" --quiet "$NOCACHE"
set +e
dotnet restore "$REPOROOT/testapp" --quiet "$NOCACHE" >/dev/null 2>&1
set -e
