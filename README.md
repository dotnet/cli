# .NET Command Line Interface

[![Join the chat at https://gitter.im/dotnet/cli](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/cli?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This repo contains the source code for cross-platform [.NET Core](http://github.com/dotnet/core) command line toolchain. It contains the implementation of each command, the native packages for various supported platforms as well as documentation. 

New to .NET CLI?
------------
Check out our http://dotnet.github.io/getting-started/

Build Status
------------

|         |Ubuntu 14.04 |Windows |Mac OS X |
|---------|:------:|:------:|:------:|
|**Debug**|[![Build Status](http://dotnet-ci.cloudapp.net/buildStatus/icon?job=dotnet_cli/debug_ubuntu)](http://dotnet-ci.cloudapp.net/job/dotnet_cli/job/debug_ubuntu/)|[![Build Status](http://dotnet-ci.cloudapp.net/buildStatus/icon?job=dotnet_cli/debug_windows_nt)](http://dotnet-ci.cloudapp.net/job/dotnet_cli/job/debug_windows_nt/)|[![Build Status](http://dotnet-ci.cloudapp.net/buildStatus/icon?job=dotnet_cli/debug_osx)](http://dotnet-ci.cloudapp.net/job/dotnet_cli/job/debug_osx/) |
|**Release**|[![Build Status](http://dotnet-ci.cloudapp.net/buildStatus/icon?job=dotnet_cli/release_ubuntu)](http://dotnet-ci.cloudapp.net/job/dotnet_cli/job/release_ubuntu/)|[![Build Status](http://dotnet-ci.cloudapp.net/buildStatus/icon?job=dotnet_cli/release_windows_nt)](http://dotnet-ci.cloudapp.net/job/dotnet_cli/job/release_windows_nt/)|[![Build Status](http://dotnet-ci.cloudapp.net/buildStatus/icon?job=dotnet_cli/release_osx)](http://dotnet-ci.cloudapp.net/job/dotnet_cli/job/release_osx/) |

Installers
----------

|         |Ubuntu 14.04 |Windows |Mac OS X |CentOS 7.1 |
|---------|:------:|:------:|:------:|:------:|
|**Installers**|[Download Debian Package](https://dotnetcli.blob.core.windows.net/dotnet/dev/Installers/Latest/dotnet-linux-x64.latest.deb)|[Download Msi](https://dotnetcli.blob.core.windows.net/dotnet/dev/Installers/Latest/dotnet-win-x64.latest.msi)|[Download Pkg](https://dotnetcli.blob.core.windows.net/dotnet/dev/Installers/Latest/dotnet-osx-x64.latest.pkg) |N/A |
|**Binaries**|[Download tar file](https://dotnetcli.blob.core.windows.net/dotnet/dev/Binaries/Latest/dotnet-linux-x64.latest.tar.gz)|[Download zip file](https://dotnetcli.blob.core.windows.net/dotnet/dev/Binaries/Latest/dotnet-win-x64.latest.zip)|[Download tar file](https://dotnetcli.blob.core.windows.net/dotnet/dev/Binaries/Latest/dotnet-osx-x64.latest.tar.gz) |[Download tar file](https://dotnetcli.blob.core.windows.net/dotnet/dev/Binaries/Latest/dotnet-centos-x64.latest.tar.gz) |

Interested in .NET Core + ASP.NET 5 RC bits?
----------------------------------------

This toolchain is independent from the DNX-based .NET Core + ASP.NET 5 RC bits. If you are looking for .NET Core + ASP.NET 5 RC bits, you can find instructions on the http://get.asp.net/.  

Docker
------

You can also use our Docker base images found on https://hub.docker.com/r/microsoft/dotnet to set up your dev or testing environment for usage.  

Basic usage
-----------

When you have the .NET Command Line Interface installed on your OS of choice, you can try it out using some of the samples on the [dotnet/core repo](https://github.com/dotnet/core/tree/master/samples). You can download the sample in a directory, and then do you can kick the tires of the CLI.

**Note:** on Linux, post-install, please set up the `DOTNET_HOME` environment: `export DOTNET_HOME=/usr/share/dotnet/`.

**Note:** on OS X, post-install, please set up the `DOTNET_HOME` environment: `export DOTNET_HOME=/usr/local/share/dotnet/cli`.


First, you will need to restore the packages:
	
	dotnet restore
	
This will restore all of the packages that are specified in the project.json file of the given sample.

Then you can either run from source or compile the sample. Running from source is straightforward:
	
	dotnet run
	
Compiling to IL is done using:
	
	dotnet compile
This will drop a binary in `./bin/[configuration]/[framework]/[binary name]` that you can just run.

Finally, you can also try out native compilation on Windows and Ubuntu and Mac.  

**Note:** at this point, only the `helloworld` and `dotnetbot` samples will work with native compilation.

	dotnet compile --native

On Mac OSX, we currently support the C++ Codegenerator (as shown below) and support for RyuJIT (as exemplified above) is coming soon.

    dotnet compile --native --cpp

This will drop a native single binary in `./bin/[configuration]/[framework]/native/[binary name]` that you can run.

For more details, please refer to the [documentation](https://github.com/dotnet/cli/tree/master/Documentation).

Building from source
--------------------

If you are building from source, take note that the build depends on NuGet packages hosted on Myget, so if it is down, the build may fail. If that happens, you can always see the [Myget status page](http://status.myget.org/) for more info.  


Questions & Comments
--------------------

For any and all feedback, please use the Issues on this repository. 
