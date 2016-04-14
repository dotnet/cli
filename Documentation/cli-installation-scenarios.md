Obtaining .NET CLI 
==================

## Contents
* [Overview](#overview)
* [General principles](#general-principles)
* [Components that can be installed](#components-that-can-be-installed)
* [Installation process](#installation-process)
* [Removal process](#removal-process)
* [Upgrading the CLI](#upgrading-the-cli)
* [Layout on disk](#layout-on-disk)
* [Native dependencies](#native-dependencies)
* [Channels](#channels)
* [Funnels and discovery mechanisms for CLI bits](#funnels-and-discovery-mechanisms-for-cli-bits)
  * [Getting Started page](#getting-started-page)
  * [Repo landing page](#repo-landing-page)
* [Acquisition modes](#acquisition-modes)
  * [Native installers](#native-installers)
  * [Installation script](#installation-script)
    * [Windows one-liner](#windows-command)
    * [OSX/Linux one-liner](#osxlinux-shell-command)
  * [Complete manual installation](#complete-manual-installation)
  * [Docker](#docker)
  * [NuGet Packages](#nuget-packages)
* [Acquiring through other products](#acquiring-through-other-products)
  * [IDEs and editors](#ides-and-editors)
  

## Overview
This document/spec outlines the CLI install experience. This covers the technologies being used for install, the principles that are driving the installation experience, the ways users are coming to the installs and what each of the installs contains, in terms of stability and similar. 

## General principles

- Upgrades using the native installers Just Work(tm)
- All user facing materials point to the getting started page
- Defaults are stable bits; users need extra effort to install Future builds
- Only HTTPS links are allowed in any online property 
- Provide native installers for each supported platform
- Provide automation-ready installers for each target platform

## Components that can be installed
Overall, there are two significant installable components:

1. The .NET Core SDK
2. Shared runtime redistributable 

The .NET Core SDK contains the following items:
1. A given version of the CLI toolset
2. A given version of the shared runtime that the CLI toolset uses; this is a full shared redist, so other apps can also use target this one

The shared runtime redistributable ("the redist") contains only the packages and the shared host tied to the version of those packages.  

The third component is the "multiplexer" ("muxer"), that is the shared component that is in charge of running the applications and the CLI commands. In general, although there will be an installer for this component, it should be treated as an implementation detail. We will see what that means in the next section. 

## Installation process
Each of the components listed in the previous section will have an installer/package. The dependencies of the installers between themseslves are given in the table below.

| Installer 	| Depends on 	|
|-----------	|------------	|
| SDK       	| redist     	|
| Redist    	| muxer      	|
| muxer     	| -          	|

The installation process will depend on the platform and the way of the install. For those installers that don't have automatic dependency resolution (Windows installer, OS X PKG) the installers will chain the installers of the components they depend on. DEB, RPM and similar will declare proper dependencies and the package manager will do the Right Thing(tm) by default. 

From the table, we can see that if you install the SDK using `apt-get` for instance, you will get also a redist and a muxer. They will be separate packages, but will be declared as dependencies (similar for `yum`). Similar for the redist package. 

The muxer is slightly a special case. Though there will be an installer, as mentioned in the previous section, it is an implementation detail. That means that acquiring the muxer should be done through either the SDK or the shared runtime installers. The only situation where this rule would not be true is if there was a major servicing event (e.g. a security update); in that case, the users would use the installer for the muxer directly, as we would rev its version accordingly. 

The script installers are slightly different as they operate on zips/tarballs. The zip/tarball for the SDK will contain the entire set of things needed to be put on the disk.

## Removal process
Removing the bits from the machine **must** follow the order outlined above in installation. If the SDK is installed, it needs to be removed first and then the Redist and only then the muxer. Similar for the Redist.  

## Upgrading the CLI 
The semantics of installing the CLI will be side-by-side by default. This means that each new version will get installed besides any existing versions on disk. The [layout section](#layout-on-disk) covers how that will look like on disk.

Since this is the case, there is no special "upgrade". When the user needs a new  version, the user just installs the new version using any of the installers specified in this document. The installer will just drop a new version at the predefined location. 

This, however, does have one specific constraint: **newever versions must be installed in the same location the previous version was in**. This constraint is due to the fact that the "muxer" uses convention to figure out how to find the actual driver that the user reuqested. 

## Layout on disk
```
~/dotnet
	-dotnet (%PATH%)
	-SDK
       - 1.0.0
           - 
       - 2.0.0
	-Shared
		-[redist framework]
			-1.0.0
				-coreclr.dll
				-mscorlib.ni.dll <Regular NI image>
				-system.*.dll  <R2R NI image>
				-dotnet-hostimpl.dll
				-dotnet
				-netcoreapp.deps.json 
			-1.5.0
				-coreclr.dll
				-mscorlib.ni.dll <Regular NI image>-
				-system.*.dll  <R2R NI image>
				-dotnet-hostimpl.dll
				-dotnet
				-netcoreapp.deps.json 
```	

## Native dependencies
.NET Core CLI is built on top of CoreFX and CoreCLR and as such its' dependencies set is defined by the platform that those two combine. Whether or not those dependencies will be installed depends on the installer being used. The table below lists out the installers and whether or not they bring in dependencies. 

| Installer  	| Dependencies Y/N   	|
|------------	|--------------------	|
| EXE        	| Y (chains them in) 	|
| PKG        	| N                  	|
| apt-get    	| Y                  	|
| rpm        	| Y                  	| 

A list of dependencies can be found on [dependency list](TBD). 

## Channels
Channels represent a way for users who are getting the CLI to reason about the stability and quality of the bits they are getting. This is one more way for the user to be fully aware of the state the bits that are being installed are in and to set proper expectations on first use. 

The table below outlines the channels:

| Property         	| Description                                                                                                                                                                                                                                                       	|
|------------------	|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------	|
| Future              	| Unstable bits that are "bleeding edge". Users are not expected to use this channel often, however it is there for those situations when someone needs/wants a feature that hasn't been stabilizied yet. Also, used for internal testing. 	|
| Preview 	| Pre-release stable builds with known issues and/or known feature gaps. We are OK with users installing these for preview purposes.                                                                                                                                	|
| Production          	| Actual releases. Most users are encouraged to install these.                                                                                                                                                                                                      	|

Below table shows the mapping between the channels, branches and feeds for the Debian pacakage. Since channels also impact the NuGet packages, it also contains the version of the package used and the feed to opt in to each channel.

| Channel    	| Branch    	| Debian feed 	| Debian package name 	| NuGet version 	| NuGet feed                            	|
|------------	|-----------	|-------------	|---------------------	|---------------	|---------------------------------------	|
| Future    	| master    	| Development 	| dotnet-future      	| 1.0.0-dev-*   	| https://dotnet.myget.org/f/dotnet-cli 	|
| Preview    	| rel/<ver> 	| Development 	| dotnet              	| 1.0.0-beta-*  	| https://dotnet.myget.org/f/dotnet-cli 	|
| Production 	| production/<ver> 	| Production  	| dotnet              	| 1.0.0         	| https://api.nuget.org/v3/index.json   	|


## Funnels and discovery mechanisms for CLI bits
There are multiple ways that we will funnel users towards the installers for the CLI:

1. [Getting Started Page](https://aka.ms/dotnetcoregs)
2. [Repo landing page](https://github.com/dotnet/cli/blob/rel/1.0.0/README.md)
3. Package repositories for platforms (`apt-get`, `brew` etc.)
4. IDEs and editors that integrate with CLI (e.g. Visual Studio, VS Code, Sublime etc.)

Out of the above, the first two funnels are under the control of the CLI team so we will go into slightly more details. The rest of the funnels will use a prescribed way to get to the bits and will have guidance on what bits to use.  

### Getting Started page
The page can be found on https://aka.ms/dotnetcoregs. This is the main curated first-run funnel for the dotnet CLI. The intent of the page is to help users test out the CLI quickly and become familiar with what the platform offers. This should be the most stable and curated experience we can offer. 

The Getting Started page should only point users to curated install experiences that can contain only stable or LKG bits. 

The below table shows other pertinent information for installs on the "Getting started" page. 
 
| Property              	| Description                                                  	|
|-----------------------	|--------------------------------------------------------------	|
| Debian feed           	| Development                                                  	|
| Brew repo/tap           	| Brew binary repo (https://github.com/Homebrew/homebrew-binary)|
| CentOS feed               | TBD
| Local install scripts 	| Latest from rel/1.0.0                                        	|


### Repo landing page
The repo landing page can be found on: https://github.com/dotnet/cli/readme.md. Download links on the landing page should be decreased in importance. First thing for "you want to get started" section should link to the getting started page on the marketing site. The Repo Landing Page should be used primarily by contributors to the CLI. There should be a separate page that has instructions on how to install both the latest stable as well as latest development with proper warnings around it. The separate page is to really avoid the situation from people accidentally installing unstable bits (since search engines can drop them in the repo first). 

The source branches and other items are actually branch specific for the repo landing page. As the user switches branches, the links and badges on the page will change to reflect the builds from that branch.  

## Acquisition modes
There are multiple acquisition modes that the CLI will have:

1. Native installers
2. Install scripts
3. NuGet packages (for use in other people's commands/code)
4. Docker

Let's dig into some details. 

### Native installers
These installation experiences are the primary way new users are getting the bits.The primary way to get information about this mode of installation is the [Getting Started page](#getting-started-page). The native installers are considered to be stable by default; this does not imply lack of bugs, but it does imply predictable behavior. They are generated from the stable branches and are never used to get the Future bits.

There are three main components that will be installed  

The native installers are:

| Platform            	| Installer        	| Status   	| Package name       	|
|---------------------	|------------------	|----------	|--------------------	|
| Windows             	| Bundle installer 	| Done     	| dotnet-{version};    	|
| Ubuntu 14.04/Debian 	| apt-get feed     	| Done     	| dotnet; dotnet-dbg 	|
| OS X                	| PKG              	| Done     	| dotnet             	|
| OS X                	| Homebrew         	| Not done 	| dotnet             	|
| CentOS/RH           	| RPM              	| Not done 	| dotnet             	|



### Installation script
This approach is a shell one-liner that downloads an installation script and runs it. The installation script will download the latest zip/tarball (depending on the script used) and will unpack it to a given location. After that, the script will print out what needs to be set for the entire CLI to work (env variables, $PATH modification etc.).

This install covers the following main scenario: 

* Local installation on a dev machine
* Acquiring tools on a CI build server

  
The features the script needs to support/have are:
* Support for dev and stable channel
* Support for specifying the version
* Support for specfying the installation location
* Support specifying whether the debug package needs to be downloaded
* Automatically add the install to $PATH unless --no-path/-NoPath is present


#### Installation script features
The following arguments are needed for the installation script:

| dotnet-install.sh param (Linux, OSX) 	| dotnet-install.ps1 param (Windows) 	| Defaults              	| Description                                                                                                                                                                                                                                   	|
|--------------------------------------	|------------------------------------	|-----------------------	|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------	|
| --channel                            	| -Channel                           	| "Production"          	| Which channel (i.e. "Future", "preview", "production") to install from.                                                                                                                                                                       	|
| --version                            	| -Version                           	| global.json or Latest 	| Which version of CLI to install; you need to specify the version as 3-part version (i.e. 1.0.0-13232). If omitted, it will default to the first global.json that contains the sdkVersion property; if that is not present it will use Latest. 	|
| --install-dir                             	| -InstallDir                        	| .dotnet               	| Path to where to install the CLI bundle. The directory is created if it doesn't exist. On Linux/OSX this directory is created in the user home directory (`$HOME`). On Windows, this directory is created in `%LocalAppData%`.                	|
| --debug                              	| -Debug                             	| false                 	| Whether to use the "fat" packages that contain debugging symbols or not.                                                                                                                                                                      	|
| --no-path                            	| -NoPath                            	| false                 	| Export the prefix/installdir to the path for the current session. This makes CLI tools available immidiately after install.                                                                                                                   	|
| --shared-runtime                     	| -SharedRuntime                     	| false                 	| Install just the shared runtime bits, not the entire SDK.                                                                                                                                                                                     	|
##### Install the latest Future CLI

Windows:
```
./dotnet-install.ps1 -Channel Future
```
OSX/Linux:
```
./dotnet-install.sh --channel Future
```

##### Install the latest preview to specified location

Windows:
```
./dotnet-install.ps1 -Channel preview -InstallDir C:\cli
```
OSX/Linux:
```
./dotnet-install.sh --channel preview --prefix ~/cli
```

#### Windows obtain one-liner example 

```
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1'))}"
```

#### OSX/Linux obtain one-liner example

```
curl -sSL https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh | bash /dev/stdin [args] 
```

### Docker 
[Docker](https://docs.docker.com/) has become a pretty good way to use developer tools, from trying them out in an interactive image use to using it for deployment. We have Docker images on DockerHub already. 

Docker images should always be updated as we make new releases. We should have Docker images of stable releases, built from the rel/* branches. 

### NuGet packages
NuGet packages of the CLI bits that make sense are published to relevant feeds. The developer who wishes to use these needs to specify a version. The version is used to opt-in to the three channels above. The actuall "installation" here is restoring the package as a dependency for a certain project (i.e. `ProjectServer` or similar). 

The table in the [channels section](#channels) has the examples of mapping between branches and NuGet package versions.

## Acquiring through other products

### IDEs and editors
Anything that goes into the higher-level tools should always use a stable build of CLI coming frol rel/* branches as required. 

If there exist any mechanism that notifies users of updates of the CLI, it should ideally point users to the Getting Started page to acquire the installers, or, if that is deemed too heavy-handed, it should point people to the last stable release. If there is a need of the URL to be "baked in" to the higher-level tool, that URL should be an aka.ms URL because it needs to be stable on that end.  

Cross-platform IDEs/editors will work in similar way as above. The notification should also point people to the Getting Started page. The reason is simple: it is the one page where users can pick and choose their installation experience. 

### Visual Studio 
Visual Studio will not be shipping CLI in-box. However, it will use CLI when installed. The install will be tied into other installs like WET and similar.  The URL that is baked in VS should be an aka.ms URL because it needs to be stable on that end. The other, pointing end, should also be a stable URL/location. 


