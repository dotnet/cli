.NET Command Line (CLI) tools - BETA Readme
===========================================

# Breaking changes
There are two main breaking changes in beta. 

1. Versioning change - we have changed the way we version the packages and introduced a new branching mechanism. You can see more in #668. 
2. Nightly and dev packages cannot co-exist with release packages on one box. 

The solution for both of these is uninstall:

1. Delete the previous versions of the package
2. Delete the non-release version of the package

# Uninstalling dotnet
Due to the above breaking change, we reccommend that you uninstall the pre-Beta bits. Below are instructions for all of the supported platforms. 

## Windows
Simply uninstall the MSI using Control Panel. 

## Ubuntu
The best way to remove the package is to purge it with the following command: `sudo apt-get purge [package-name]`. This will remove all of the dotnet that was installed via apt-get. The `[[ackage-name]` above will most likely be `dotnet` but if you installed dev or nightly it will be `dotnet-dev` or `dotnet-nightly` respectivelly.  

## OS X 
OS X doesn't provide any native uninstall option for PKG installs, so you will have to do some manual work. Luckily, it is not difficult if you follow the steps below:

1. Remove the `/usr/local/share/dotnet` directory and everything in it.
2. Remove all of the `dotnet*` symbolic links in `/usr/local/bin`. 
3. Remove the following files, if they exist:
    * `/usr/local/bin/corehost`
    * `/usr/local/bin/libcorelcr.dylib`
4. "Forget" the PKG recipe using the following command: `pkgutil --packages | grep dotnet | xargs pkgutil --forget`
    
## Zip installs
If you installed using the zip/tarballs for each platform, you can simply delete the bits and/or set the `$PATH` to not point to the installed bits (if you've done this). 

# Install Beta bits

## Native installers
For instructions on how to use the native installers, you can check out the [Getting Started guide](https://dotnet.github.io/getting-started/).


## ZIP/tarball installs
If you don't want to use the native installers, you can always use the below links to get a zip/tarball for a local install.

* [Windows](https://dotnetcli.blob.core.windows.net/dotnet/beta/Binaries/Latest/dotnet-win-x64.latest.zip)
* [Ubuntu](https://dotnetcli.blob.core.windows.net/dotnet/beta/Binaries/Latest/dotnet-ubuntu-x64.latest.tar.gz)
* [CentOS](https://dotnetcli.blob.core.windows.net/dotnet/beta/Binaries/Latest/dotnet-centos-x64.latest.tar.gz)
* [OS X](https://dotnetcli.blob.core.windows.net/dotnet/beta/Binaries/Latest/dotnet-osx-x64.latest.tar.gz)

Installing is as simple as unzipping/untarring the file into a location.  After that, do not forget to define the `$DOTNET_HOME` environment variable to the directory where you extracted the archive (the one containing the `bin` and `runtime` directories). 

Also, please refer to the following dependencies that you need for each platform, since the 

## Building from source
If you're adventureous and want to build from source, it is quite easy. Clone this repo and run `build.cmd` or `build.sh` for Windows and Linux/OS X respectively. 

After that, do not forget to define the `$DOTNET_HOME` environment variable to point to the stage2 directory that was created in the build process. 

# Known issues
You can consult the [known issues page](known-issues.md) if you run into any problems. If you don't find what you are looking for on that page, feel free to add an issue to our [issues](https://github.com/dotnet/cli/issues/). 

