#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

set -e

BIN_DIR="$( cd $1 && pwd )"

UNAME=`uname`

# Always recalculate the RID because the package always uses a specific RID, regardless of OS X version or Linux distro.
if [ "$OSNAME" == "osx" ]; then
    RID=osx.10.10-x64
elif [ "$OSNAME" == "ubuntu" ]; then
    RID=ubuntu.14.04-x64
elif [ "$OSNAME" == "centos" ]; then
    RID=centos.7.1-x64
else
    echo "Unknown OS: $OSNAME" 1>&2
    exit 1
fi

# Replace with a robust method for finding the right crossgen.exe
CROSSGEN_UTIL=$NUGET_PACKAGES/runtime.$RID.Microsoft.NETCore.Runtime.CoreCLR/1.0.1-rc2-23714/tools/crossgen

cd $BIN_DIR

# Crossgen currently requires itself to be next to mscorlib
cp $CROSSGEN_UTIL $BIN_DIR
chmod +x crossgen

./crossgen -nologo -platform_assemblies_paths $BIN_DIR mscorlib.dll

./crossgen -nologo -platform_assemblies_paths $BIN_DIR System.Collections.Immutable.dll

./crossgen -nologo -platform_assemblies_paths $BIN_DIR System.Reflection.Metadata.dll

./crossgen -nologo -platform_assemblies_paths $BIN_DIR Microsoft.CodeAnalysis.dll

./crossgen -nologo -platform_assemblies_paths $BIN_DIR Microsoft.CodeAnalysis.CSharp.dll

./crossgen -nologo -platform_assemblies_paths $BIN_DIR Microsoft.CodeAnalysis.VisualBasic.dll

./crossgen -nologo -platform_assemblies_paths $BIN_DIR csc.dll
[ -e csc.ni.exe ] && [ ! -e csc.ni.dll ] && mv csc.ni.exe csc.ni.dll

./crossgen -nologo -platform_assemblies_paths $BIN_DIR vbc.dll
[ -e vbc.ni.exe ] && [ ! -e vbc.ni.dll ] && mv vbc.ni.exe vbc.ni.dll
