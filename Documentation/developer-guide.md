Developer Guide
===============

## Prerequisites

In order to build .NET Command Line Interface, you need the following installed on you machine.

### For Windows

1. Visual Studio 2015 with Web Development Tools
  * Beta8 is available here and should work: http://www.microsoft.com/en-us/download/details.aspx?id=49442
    * Install `WebToolsExtensionsVS14.msi` and `DotNetVersionManager-x64.msi`
2. CMake (available from https://cmake.org/) on the PATH.
3. git (available from http://www.git-scm.com/) on the PATH.

### For Linux

1. CMake (available from https://cmake.org/) is required to build the native host `corehost`. Make sure to add it to the PATH.
2. git (available from http://www.git-scm.com/) on the PATH.
3. clang (available from http://clang.llvm.org) on the PATH.

### For OS X

1. Xcode
2. CMake (available from https://cmake.org/) on the PATH.
3. git (available from http://www.git-scm.com/) on the PATH.

## Building/Running

1. Run `build.cmd` or `build.sh` from the root depending on your OS.
2. Use `artifacts/{os}-{arch}/stage2/bin/dotnet` to try out the `dotnet` command. You can also add `artifacts/{os}-{arch}/stage2/bin` to the PATH if you want to run `dotnet` from anywhere.

## A simple test

1. `cd TestAssets\TestProjects\TestSimpleIncrementalApp`
2. `dotnet restore`
3. `dotnet run`


##Adding a Command

The dotnet CLI considers any executable on the path named `dotnet-{commandName}` to be a command it can call out to. `dotnet publish`, for example, is added to the path as an executable called `dotnet-publish`. To add a new command we must create the executable and then add it to the distribution packages for installation.

0. Create an issue on https://github.com/dotnet/cli and get consensus on the need for and behaviour of the command.
1. Add a new project for the command. 
2. Add the project to Microsoft.DotNet.Cli.sln
3. Create a Readme.md for the command.
4. Add the project to the build scripts.
5. Add the project to the packaging scripts.

#### Add a new command project
Start by copying an existing command, like /src/dotnet-new.  
Update the Name property in project.json as well, and use the `dotnet-{command}` syntax here.
Make sure to use the System.CommandLine parser so behaviour is consistent across commands.

#### Add a Readme.md
Each command's project root should contain a manpage-style Readme.md that describes the usage of the command. See other commands for reference.

#### Add project to build scripts
1. Add the project to `/scripts/build/build-stage.ps1`
  - Add the project name to the `$Projects` list
2. Add the project to `/scripts/build/build-stage.sh`
  - Add the project name to the `PROJECTS` list
3. run *build* from the root directory and make sure your project is producing binaries in /artifacts/

#### Add command to packages
- Update the `symlinks` property of `packaging/debian/debian_config.json` to include the new command
- Update the `$Projects` property in `packaging/osx/scripts/postinstall`

#### Things to Know
- Any added commands are usually invoked through `dotnet {command}`. As a result of this, stdout and stderr are redirected through the driver (`dotnet`) and buffered by line. As a result of this, child commands should use Console.WriteLine in any cases where they expect output to be written immediately. Any uses of Console.Write should be followed by Console.WriteLine to ensure the output is written.
