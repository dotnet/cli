#!/usr/bin/env bash
# dotnet-restore script to be copied across to the final package

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

# work around restore timeouts on Mono
[ -z "$MONO_THREADS_PER_CPU" ] && export MONO_THREADS_PER_CPU=50

MAJOR_MAC_VERSION=$(sw_vers -productVersion | awk -F '.' '{print $1 "." $2}')

exec "$DIR/dnx/dnx" "$DIR/dnx/lib/Microsoft.Dnx.Tooling/Microsoft.Dnx.Tooling.dll" "restore" --runtime "osx.${MAJOR_MAC_VERSION}-x64" --runtime "ubuntu.14.04-x64" "$@"
