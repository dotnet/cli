dotnet-restore
==============

**NAME**
dotnet-restore -- Restore packages specified in project.json (project file)

**SYNOPSIS**
dotnet-restore [options] [project file(s)]

**DESCRIPTION**
This command is used to restore all of the dependencies that are specified in one or more project files for a given application. The packages that form the dependencies will be installed  It will read the dependencies from the project file and then install each of the packages that is specified in the file. For each package, the entire dependency chain for that package will be installed. The packages are restored in a shared global location by default. 

The commands relies on an existence of the project file to work. The default behavior is to use the project file in the directory where the command is invoked. Alternatively, the full path to the project file can be specified as an argument to the command at invocation time. [project file(s)] can contain multiple, comma-separated paths. 
If multiple project files are encountered (either in the directory or on the command line), the command will restore packages defined in each of them. If no 
project file is specified, the command will fail with an error message. 

The online source of packages is represented by a "feed". Feeds are configured in a global configuration file named "nuget.config", which is part of the dotnet SDK. They can also be specified per invocation of the command using the --source option. If one or more feeds are specified via the --source option, the configuration file feeds will not be used. The command will try every package feed until the package is found. If no package feed contains the package, it will throw an error and exit. 

By default, the command will first check the local cache and will avoid the networking trip if the package already exists in the cache. 

The command will generate a "lock" file named *restore.json*. A lock file represents the persisted information about the dependencies of each of the packages that are defined in the project file. This file exists to avoid having to recalculate the dependency chains on load. If this file doesn't exist, the dotnet-restore commands needs to be invoked prior to compiling or running the application in question. 

**Options**
-s, --source [feed]
List of package feeds to use for this invocation. The list is comma-separated and are specified as URLs. 

--packages [path]
Path where to put the installed package. 

--no-cache
Do not use local cache. This will force the command to make the network trip. 

--proxy
The HTTP proxy to use when retrieving packages.

**EXAMPLES**
`dotnet restore`
    Restore packages specified in the project.json file located in the directory where the command is invoked. 	
`dotnet restore ~/projects/App1/project.json,~/projects/App2/project.json`
    Restore packages in two project.json files that exist in specific locations. 	
`dotnet restore --global`
    Restore packages specified in the project.json file located in the directory where the command is invoked, and place them in the global package cache.	
`dotnet restore --source http://localfeed:8080/feed/`
    Restore packages using a custom, locally set up package feed. 	
	
**SEE ALSO**
dotnet-install-package
