#!/bin/bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

source "$DIR/../../scripts/common/_common.sh"

if [ -z "$DOTNET_CLI_VERSION" ]; then
    echo "Provide a version number (DOTNET_CLI_VERSION) $DOTNET_CLI_VERSION" && exit 1
fi

STAGE2_DIR=$REPOROOT/artifacts/$RID/stage2

if [ ! -d "$STAGE2_DIR" ]; then
    echo "Missing stage2 output in $STAGE2_DIR" 1>&2
    exit 1
fi

PACKAGE_DIR=$REPOROOT/artifacts/packages/pkg
[ -d "$PACKAGE_DIR" ] || mkdir -p $PACKAGE_DIR

PACKAGE_ID=dotnet-osx-x64.${DOTNET_CLI_VERSION}.pkg
PACKAGE_NAME=$PACKAGE_DIR/$PACKAGE_ID
#chmod -R 755 $STAGE2_DIR
pkgbuild --root $STAGE2_DIR \
         --version $DOTNET_CLI_VERSION \
         --scripts $DIR/scripts \
         --identifier com.microsoft.dotnet.cli.pkg.dotnet-osx-x64 \
         --install-location /usr/local/share/dotnet \
         $DIR/$PACKAGE_ID

cat $DIR/Distribution-Template | sed "/{VERSION}/s//$DOTNET_CLI_VERSION/g" > $DIR/Dist

productbuild --version $DOTNET_CLI_VERSION --identifier com.microsoft.dotnet.cli --package-path $DIR --resources $DIR/resources --distribution $DIR/Dist $PACKAGE_NAME

#Clean temp files
rm $DIR/$PACKAGE_ID
rm $DIR/Dist

$REPOROOT/scripts/publish/publish.sh $PACKAGE_NAME
