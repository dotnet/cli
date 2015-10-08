#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [clean] [verbose]"    
    echo "BuildArch can be: x64, x86"
    echo "BuildType can be: Debug, Release"
    echo "clean - optional argument to force a clean build."
    echo "verbose - optional argument to enable verbose build output."

    exit 1
}

exit 0