dotnet-install-package
======================

**NAME**

dotnet-install-package -- Installs a package from a package source or from a local file

**SYNOPSIS**

dotnet-install-package [options] [package]

**DESCRIPTION**

The dotnet-install-package command is used to install packages for your application. Packages can be installed from either a remote, online source via a "package feed", or by using a locally available package file. 

The online source of packages is represented by a "feed". Feeds are configured in a global configuration file named "nuget.config", which is part of the dotnet SDK. They can also be specified per invocation of the command using the --source option. If one or more feeds are specified via the --source option, the configuration file feeds will not be used. The command will try every package feed until the package is found. If no package feed contains the package, it will throw an error and exit. 

By default, the command will first check the local cache and will avoid the networking trip if the package already exists in the cache. 

The [package] being installed can contain the package name and package version. If version is not specified, the last stable version is assumed. If --pre switch is present, it will default to the last unstable (pre-release) version. The command will install the package and all of its dependencies. This means that for each given package, you have to specify only the top-level dependency and the entire dependency chain will be resolved and installed. If any of the packages in the chain fails to resolve, the entire command will fail. 

By default, the packages will be installed in the ./packages directory in the current directory, that is, in the directory where the command was invoked. If the --packages option is specified, it will override this convention and install the packages in that location instead. 

Package that is installed will be added to project.json as a dependency if it doesn't exist. If it exists with the same version, the operation will be a noop. If the version is different, the project.json file will be updated to the version specified in the command. If there is no project.json, one will be created with sensible defaults in the directory in which the command was invoked.   

**Options**

-s, --source [feed]
List of package feeds to use for this invocation. The list is comma-separated and are specified as URLs. 

--packages [path]
Path where to put the installed package. 

--pre
Install the latest pre-release version of the package.

--no-cache
Do not use local cache. This will force the command to make the network trip. 

--proxy
The HTTP proxy to use when retrieving packages.

**EXAMPLES**
dotnet-install-package Newtonsoft.Json
	Install the latest stable version of JSON.NET and add it to project.json
	
dotnet-install-package System.Text.Encoding.Extensions 4.0.11-beta-23409
	Install System.Text.Encoding Extensions with a specific version. 
	
dotnet-install-package --source http://localfeed:8080/feed/ MyPackage
	Use the locally set up source to install a package. The source will override the defaults in nuget.config file in the SDK. 
	
**SEE ALSO**
dotnet-restore
