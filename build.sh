#!/usr/bin/env bash

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
sudo apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb http://download.mono-project.com/repo/debian wheezy main" | sudo tee /etc/apt/sources.list.d/mono-xamarin.list
sudo apt-get update
sudo apt-get install mono-complete
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
