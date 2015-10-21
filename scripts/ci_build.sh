#!/usr/bin/env bash

echo Build Number - $BUILD_NUMBER

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

$SCRIPT_DIR/../build.sh $@

ret_code=$?
exit $ret_code

