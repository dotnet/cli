#!/usr/bin/env bash

set -e

CONFIGURAITON=$1

[ -z "$CONFIGURATION" ] && CONFIGURATION=Debug

# TODO: Replace this with a dotnet generation
TFM=dnxcore50

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
REPOROOT="$( cd -P "$DIR/.." && pwd )"

START_PATH=$PATH

echo "Bootstrapping dotnet.exe using DNX"

if [ -z "$RID" ]; then
    UNAME=$(uname)
    if [ "$UNAME" == "Darwin" ]; then
        RID=osx.10.10-x64
    elif [ "$UNAME" == "Linux" ]; then
        # Detect Distro?
        RID=ubuntu.14.04-x64
    else
        echo "Unknown OS: $UNAME" 1>&2
        exit 1
    fi
fi

OUTPUT_ROOT=$REPOROOT/artifacts/$RID
DNX_DIR=$OUTPUT_ROOT/dnx
STAGE0_DIR=$OUTPUT_ROOT/stage0
STAGE1_DIR=$OUTPUT_ROOT/stage1
STAGE2_DIR=$OUTPUT_ROOT/stage2

echo "Cleaning artifacts folder"
rm -rf $OUTPUT_ROOT

echo "Installing stage0"
# Use a sub-shell to ensure the DNVM gets cleaned up
mkdir -p $STAGE0_DIR
$DIR/install-stage0.sh $STAGE0_DIR $DIR/dnvm2.sh

export PATH=$STAGE0_DIR/bin:$PATH

export DOTNET_CLR_HOSTS_PATH=$REPOROOT/ext/CLRHost/$RID

if ! type dnx > /dev/null 2>&1; then
    echo "Installing and use-ing the latest CoreCLR x64 DNX ..."
    mkdir -p $DNX_DIR

    export DNX_HOME=$DNX_DIR
    export DNX_USER_HOME=$DNX_DIR
    export DNX_GLOBAL_HOME=$DNX_DIR

    if ! type dnvm > /dev/null 2>&1; then
        curl -o $DNX_DIR/dnvm.sh https://raw.githubusercontent.com/aspnet/Home/dev/dnvm.sh
        source $DNX_DIR/dnvm.sh
    fi

    dnvm install latest -u -r coreclr
    rc=$?; if [[ $rc != 0 ]]; then exit $rc; fi

    # Make sure we got a DNX
    if ! type dnx > /dev/null 2>&1; then
        echo "DNX is required to bootstrap stage1" 1>&2
        exit 1
    fi
fi

echo "Running 'dnu restore' to restore packages"

dnu restore "$REPOROOT" --runtime osx.10.10-x64 --runtime ubuntu.14.04-x64 --runtime osx.10.11-x64

# Clean up stage1
[ -d "$STAGE1_DIR" ] && rm -Rf "$STAGE1_DIR"

echo "Building basic dotnet tools using Stage 0"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE1_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Cli"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE1_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Tools.Compiler"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE1_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Tools.Compiler.Csc"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE1_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Tools.Publish"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE1_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Tools.Resgen"

# Add stage1 to the path and use it to build stage2
export PATH=$STAGE1_DIR:$START_PATH

echo "Building stage2 dotnet using stage1 ..."
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE2_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Cli"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE2_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Tools.Compiler"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE2_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Tools.Compiler.Csc"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE2_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Tools.Publish"
dotnet publish --framework "$TFM" --runtime $RID --output "$STAGE2_DIR" --configuration "$CONFIGURATION" "$REPOROOT/src/Microsoft.DotNet.Tools.Resgen"

# Smoke-test the output
export PATH=$STAGE2_DIR:$START_PATH

rm "$REPOROOT/test/TestApp/project.lock.json"
dnu restore "$REPOROOT/test/TestApp" --runtime "$RID"
dotnet publish "$REPOROOT/test/TestApp" --framework "$TFM" --runtime "$RID" --output "$REPOROOT/artifacts/$RID/smoketest"

OUTPUT=$($REPOROOT/artifacts/$RID/smoketest/TestApp)
[ "$OUTPUT" == "This is a test app" ] || (echo "Smoke test failed!" && exit 1)

# Check that a compiler error is reported
set +e
dotnet compile "$REPOROOT/test/compile/failing/SimpleCompilerError" --framework "$TFM"
rc=$?
if [ $rc == 0 ]; then
    echo "Compiler failure test failed! The compiler did not fail to compile!"
    exit 1
fi
set -e
