#!/usr/bin/env bash
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

source "$DIR/../common/_common.sh"
source "$DIR/../common/_clang.sh"

COREHOST_PACKAGE_NAME="Microsoft.DotNet.CoreHost"
COREHOST_PACKAGE_VERSION=$(cat "$REPOROOT/src/corehost/packaging/.version")
COREHOST_PACKAGE_LIBHOST_RELATIVE_DIR="runtimes/${RID}/native"

if [[ "$OSNAME" == "osx" ]]; then
   COREHOST_LIBNAME=libhostpolicy.dylib
else
   COREHOST_LIBNAME=libhostpolicy.so
fi

main()
{
    header "Building corehost"

    pushd "$REPOROOT/src/corehost" 2>&1 >/dev/null
    [ -d "cmake/$RID" ] || mkdir -p "cmake/$RID"
    cd "cmake/$RID"

    echo ${COREHOST_PACKAGE_NAME} > "$REPOROOT/src/corehost/packaging/.name"
    echo ${COREHOST_PACKAGE_LIBHOST_RELATIVE_DIR} > "$REPOROOT/src/corehost/packaging/.relative"

    cmake ../.. -G "Unix Makefiles" -DCMAKE_BUILD_TYPE:STRING=$CONFIGURATION
    make

    # Publish to artifacts
    [ -d "$HOST_DIR" ] || mkdir -p $HOST_DIR
    cp "$REPOROOT/src/corehost/cmake/$RID/cli/corehost" $HOST_DIR
    cp "$REPOROOT/src/corehost/cmake/$RID/cli/dll/${COREHOST_LIBNAME}" $HOST_DIR

    package_corehost
    popd 2>&1 >/dev/null
}


set_nuspec_contents()
{
    local name=$1
    local version=$2
    local files=$3
    NUSPEC_CONTENTS="<?xml version=\"1.0\" encoding=\"utf-8\"?>\
<package xmlns=\"http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd\">\
    <metadata>\
        <id>${name}</id>\
        <version>${version}</version>\
        <title>.NET CoreCLR Runtime Host</title>\
        <authors>Microsoft</authors>\
        <owners>Microsoft</owners>\
        <licenseUrl>http://go.microsoft.com/fwlink/?LinkId=329770</licenseUrl>\
        <projectUrl>https://github.com/dotnet/cli</projectUrl>\
        <iconUrl>http://go.microsoft.com/fwlink/?LinkID=288859</iconUrl>\
        <requireLicenseAcceptance>true</requireLicenseAcceptance>\
        <description>Provides the runtime host for .NET CoreCLR</description>\
        <releaseNotes>Initial release</releaseNotes>\
        <copyright>Copyright © Microsoft Corporation</copyright>\
    </metadata>\
    <files>\
        ${files}\
    </files>\
</package>"
}

package_corehost()
{
    # Package CoreHost

    # Create nupkg for the host
    PACKAGE_NAME=runtime.${RID}.${COREHOST_PACKAGE_NAME}

    PACKAGE_CONTENTS="\
    <file src=\"corehost\" target=\"runtimes/${RID}/native/corehost\" />\
    <file src=\"${COREHOST_LIBNAME}\" target=\"runtimes/${RID}/native/${COREHOST_LIBNAME}\" />"

    set_nuspec_contents ${PACKAGE_NAME} ${COREHOST_PACKAGE_VERSION} "${PACKAGE_CONTENTS}"
    echo "${NUSPEC_CONTENTS}" > ${HOST_DIR}/${PACKAGE_NAME}.nuspec

    if hash mono 2>/dev/null; then
        mono $NUGET_DIR/nuget.exe pack "$HOST_DIR/${PACKAGE_NAME}.nuspec" -NoPackageAnalysis -NoDefaultExcludes -BasePath "$HOST_DIR" -OutputDirectory "$HOST_DIR"
    else
        echo "Skipping packaging corehost, mono not present."
    fi
}

main
