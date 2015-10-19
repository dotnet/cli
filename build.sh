#!/usr/bin/env bash

# Install the specified Mono toolset from our Azure blob storage.
install_mono_toolset()
{
    local target=/tmp/$1
    echo "Installing Mono toolset $1"

    if [ -d $target ]; then
        echo "Already installed"
        return
    fi

    pushd /tmp

    rm -r $target 2>/dev/null
    rm $1.tar.bz2 2>/dev/null
    curl -O https://dotnetci.blob.core.windows.net/roslyn/$1.tar.bz2
    tar -jxf $1.tar.bz2
    if [ $? -ne 0 ]; then
        echo "Unable to download toolset"
        exit 1
    fi

    popd
}


nugetVersion=latest
nugetPath=.nuget/nuget.exe

url=https://dist.nuget.org/win-x86-commandline/$nugetVersion/nuget.exe

if test ! -e .nuget; then
    mkdir .nuget
fi

if test ! -f $nugetPath; then
    wget -O $nugetPath $url 2>/dev/null || curl -o $nugetPath --location $url /dev/null
fi

# install mono
install_mono_toolset mono.linux.1
PATH=/tmp/mono.linux.1/bin:$PATH
mono --version

if test ! -d packages/KoreBuild; then
    mono .nuget/nuget.exe install KoreBuild -ExcludeVersion -o packages -nocache -pre -source https://www.myget.org/F/aspnetvnext/api/v3/index.json
    mono .nuget/nuget.exe install Sake -ExcludeVersion -Out packages
fi

if ! type dnvm > /dev/null 2>&1; then
    source packages/KoreBuild/build/dnvm.sh
fi

if ! type dnx > /dev/null 2>&1; then
    dnvm upgrade
fi

mono packages/Sake/tools/Sake.exe -I packages/KoreBuild/build -f makefile.shade "$@"
