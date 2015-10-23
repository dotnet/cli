#!/usr/bin/env bash
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo $(ls)

if [[ "$(uname)" == "Linux" ]]; then
    # Set Docker Container name to be unique
    local container_name=""

    #Jenkins
    [ ! -z "$BUILD_TAG" ] && container_name="$BUILD_TAG"
    #VSO
    [ ! -z "$BUILD_BUILDID" ] && container_name="$BUILD_BUILDID"

    export DOTNET_BUILD_CONTAINER_NAME="$container_name"
    
    $SCRIPT_DIR/dockerpostbuild.sh $@
fi

ret_code=$?
exit $ret_code
