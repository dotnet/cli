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
OLDPATH="$PATH"

REPOROOT="$DIR/../.."
source "$REPOROOT/scripts/common/_prettyprint.sh"

while [[ $# > 0 ]]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -c|--configuration)
            export CONFIGURATION=$2
            shift
            ;;
        --targets)
            IFS=',' read -r -a targets <<< $2
            shift
            ;;
        --nopackage)
            export DOTNET_BUILD_SKIP_PACKAGING=1
            ;;
        --skip-prereqs)
            # Allow CI to disable prereqs check since the CI has the pre-reqs but not ldconfig it seems
            export DOTNET_INSTALL_SKIP_PREREQS=1
            ;;
        --help)
            echo "Usage: $0 [--configuration <CONFIGURATION>] [--skip-prereqs] [--nopackage] [--docker <IMAGENAME>] [--help] [--targets <TARGETS...>]"
            echo ""
            echo "Options:"
            echo "  --configuration <CONFIGURATION>     Build the specified Configuration (Debug or Release, default: Debug)"
            echo "  --targets <TARGETS...>              Comma separated build targets to run (Init, Compile, Publish, etc.; Default is a full build and publish)"
            echo "  --nopackage                         Skip packaging targets"
            echo "  --skip-prereqs                      Skip checks for pre-reqs in dotnet_install"
            echo "  --docker <IMAGENAME>                Build in Docker using the Dockerfile located in scripts/docker/IMAGENAME"
            echo "  --help                              Display this help message"
            echo "  <TARGETS...>                        The build targets to run (Init, Compile, Publish, etc.; Default is a full build and publish)"
            exit 0
            ;;
        *)
            break
            ;;
    esac

    shift
done

function print_info_from_core_file {
  local core_file_name=$1
  local executable_name=$2

  if ! [ -e $executable_name ]; then
    echo "Unable to find executable $executable_name"
    return
  elif ! [ -e $core_file_name ]; then
    echo "Unable to find core file $core_file_name"
    return
  fi

  # Check for the existence of GDB on the path
  hash gdb 2>/dev/null || { echo >&2 "GDB was not found. Unable to print core file."; return; }

  echo "Printing info from core file $1"

  # Open the dump in GDB and print the stack from each thread. We can add more
  # commands here if desired.
  gdb --batch -ex "thread apply all bt full" -ex "quit" $executable_name $core_file_name
}

# Set nuget package cache under the repo
export NUGET_PACKAGES="$REPOROOT/.nuget/packages"

# Set up the environment to be used for building with clang.
if which "clang-3.5" > /dev/null 2>&1; then
    export CC="$(which clang-3.5)"
    export CXX="$(which clang++-3.5)"
elif which "clang-3.6" > /dev/null 2>&1; then
    export CC="$(which clang-3.6)"
    export CXX="$(which clang++-3.6)"
elif which clang > /dev/null 2>&1; then
    export CC="$(which clang)"
    export CXX="$(which clang++)"
else
    error "Unable to find Clang Compiler"
    error "Install clang-3.5 or clang3.6"
    exit 1
fi

# Load Branch Info
while read line; do
    if [[ $line != \#* ]]; then
        IFS='=' read -ra splat <<< "$line"
        export ${splat[0]}="${splat[1]}"
    fi
done < "$REPOROOT/branchinfo.txt"

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
[ -z "$DOTNET_INSTALL_DIR" ] && export DOTNET_INSTALL_DIR=$REPOROOT/.dotnet_stage0/$(uname)
[ -d "$DOTNET_INSTALL_DIR" ] || mkdir -p $DOTNET_INSTALL_DIR

$REPOROOT/scripts/obtain/dotnet-install.sh --channel $CHANNEL --verbose

# Put stage 0 on the PATH (for this shell only)
PATH="$DOTNET_INSTALL_DIR:$PATH"

# Increases the file descriptors limit for this bash. It prevents an issue we were hitting during restore
FILE_DESCRIPTOR_LIMIT=$( ulimit -n )
if [ $FILE_DESCRIPTOR_LIMIT -lt 1024 ]
then
    echo "Increasing file description limit to 1024"
    ulimit -n 1024
fi

# Temporary logic to turn core dumps on so we can catch segfaults on Unix
ulimit -c unlimited

# Restore the build scripts
echo "Restoring Build Script projects..."
(
    cd "$DIR/.."
    dotnet restore
)

# Build the builder
echo "Compiling Build Scripts..."
dotnet publish "$DIR" -o "$DIR/bin" --framework netcoreapp1.0

export PATH="$OLDPATH"
# Run the builder
echo "Invoking Build Scripts..."
echo "Configuration: $CONFIGURATION"

$DIR/bin/dotnet-cli-build ${targets[@]}

# ======================= BEGIN Core File Inspection =========================
if [ "$(uname -s)" == "Linux" ]; then
  # Depending on distro/configuration, the core files may either be named "core"
  # or "core.<PID>" by default. We read /proc/sys/kernel/core_uses_pid to 
  # determine which it is.
  core_name_uses_pid=0
  if [ -e /proc/sys/kernel/core_uses_pid ] && [ "1" == $(cat /proc/sys/kernel/core_uses_pid) ]; then
    core_name_uses_pid=1
  fi

  if [ $core_name_uses_pid == "1" ]; then
    # We don't know what the PID of the process was, so let's look at all core
    # files whose name matches core.NUMBER
    for f in core.*; do
      [[ $f =~ core.[0-9]+ ]] && print_info_from_core_file "$f" "dotnet" && rm "$f"
    done
  elif [ -f core ]; then
    print_info_from_core_file "core" "dotnet"
    rm "core"
  fi
fi

exit $?
