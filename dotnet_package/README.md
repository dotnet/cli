# Dotnet Debian Package Prototype

To build the package you will need to install these dependencies:

    sudo apt-get install -y build-essential devscripts debhelper automake libtool curl libuv-dev unzip libunwind8 libssl-dev lldb-3.6 libcurl4-openssl-dev

After installing dependencies you can build the package:

    ./build.sh

To Build, Install, and Test the Package:

    ./testpackage.sh

The Package contains binaries for:

*	Dotnet.exe Driver Program

It pulls down Nuget Packages with:

*	CoreCLR
*	corerun host
*	Microsoft.NETCore.Console Standard Libraries

These nuget packages are defined in ./src/project.json and are restored during install time.

## Contents

What follows is a basic overview of the contents of this directory.

build.sh - Bash Script to build a .deb package file

testpackage.sh - A set of tests which build, install, and test baseline functionality of the package.

config.shprops - Source of truth for all properties. Installed to $INSTALL_ROOT/config for use by postinstall script

package_files/ - These are the specific debian files needed to construct a package. Don't add any source files here. Files in this directory contain metadata that will need to be changed between versions of packages.

src/ - The contents of directory is what is ultimately copied into the package root. It contains a couple default files, but is populated further at build time.

scripts/ - Any scripts that should be installed inside $INSTALL_ROOT/scripts. Currently contains resolve_nuget_assets.py which is used to resolve assets from a project.lock.json

build_tools/ - Tools used during package build. Contains Manpage Generator and associated test assets.

samples/ - Files which should be packaged as examples. Installed to /usr/share/doc/${PACKAGE_NAME}/examples
