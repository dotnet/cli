# Review for consistency and style

## ~/repos/docs $ dotnet -h

```
.NET Command Line Tools (2.1.300-preview2-008533)
Usage: dotnet [runtime-options] [path-to-application]
Usage: dotnet [sdk-options] [command] [arguments] [command-options]

path-to-application:
  The path to an application .dll file to execute.

SDK commands:
  new              Initialize .NET projects.
  restore          Restore dependencies specified in the .NET project.
  run              Compiles and immediately executes a .NET project.
  build            Builds a .NET project.
  publish          Publishes a .NET project for deployment (including the runtime).
  test             Runs unit tests using the test runner specified in the project.
  pack             Creates a NuGet package.
  migrate          Migrates a project.json based project to a msbuild based project.
  clean            Clean build output(s).
  sln              Modify solution (SLN) files.
  add              Add reference to the project.
  remove           Remove reference from the project.
  list             List project references or installed tools.
  nuget            Provides additional NuGet commands.
  msbuild          Runs Microsoft Build Engine (MSBuild).
  vstest           Runs Microsoft Test Execution Command Line Tool.
  store            Stores the specified assemblies in the runtime store.
  tool             Modify tools.
  buildserver      Interact with servers started by a build.
  help             Show help.

Common options:
  -v|--verbosity        Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
  -h|--help             Show help.

Run 'dotnet COMMAND --help' for more information on a command.

sdk-options:
  --version        Display .NET Core SDK version.
  --info           Display .NET Core information.
  --list-sdks      Display the installed SDKs.
  --list-runtimes  Display the installed runtimes.
  -d|--diagnostics Enable diagnostic output.

runtime-options:
  --additionalprobingpath <path>    Path containing probing policy and assemblies to probe for.
  --fx-version <version>            Version of the installed Shared Framework to use to run the application.
  --roll-forward-on-no-candidate-fx Roll forward on no candidate shared framework is enabled.
  --additional-deps <path>          Path to additional deps.json file.
```

## ~/Documents/working $ dotnet new -h

```
Usage: new [options]

Options:
  -h, --help          Displays help for this command.
  -l, --list          Lists templates containing the specified name. If no name is specified, lists all templates.
  -n, --name          The name for the output being created. If no name is specified, the name of the current directory is used.
  -o, --output        Location to place the generated output.
  -i, --install       Installs a source or a template pack.
  -u, --uninstall     Uninstalls a source or a template pack.
  --nuget-source      Specifies a NuGet source to use during install.
  --type              Filters templates based on available types. Predefined values are "project", "item" or "other".
  --force             Forces content to be generated even if it would change existing files.
  -lang, --language   Filters templates based on language and specifies the language of the template to create.


Templates                                         Short Name       Language          Tags               
--------------------------------------------------------------------------------------------------------
Console Application                               console          [C#], F#, VB      Common/Console     
Class library                                     classlib         [C#], F#, VB      Common/Library     
Unit Test Project                                 mstest           [C#], F#, VB      Test/MSTest        
xUnit Test Project                                xunit            [C#], F#, VB      Test/xUnit         
Razor Page                                        page             [C#]              Web/ASP.NET        
MVC ViewImports                                   viewimports      [C#]              Web/ASP.NET        
MVC ViewStart                                     viewstart        [C#]              Web/ASP.NET        
ASP.NET Core Empty                                web              [C#], F#          Web/Empty          
ASP.NET Core Web App (Model-View-Controller)      mvc              [C#], F#          Web/MVC            
ASP.NET Core Web App                              razor            [C#]              Web/MVC/Razor Pages
ASP.NET Core with Angular                         angular          [C#]              Web/MVC/SPA        
ASP.NET Core with React.js                        react            [C#]              Web/MVC/SPA        
ASP.NET Core with React.js and Redux              reactredux       [C#]              Web/MVC/SPA        
ASP.NET Core Web API                              webapi           [C#], F#          Web/WebAPI         
global.json file                                  globaljson                         Config             
NuGet Config                                      nugetconfig                        Config             
Web Config                                        webconfig                          Config             
Solution File                                     sln                                Solution           

Examples:
    dotnet new mvc --auth Individual
    dotnet new web 
    dotnet new --help
```

* New needs a new line at the end of help

## ~/Documents/working $ dotnet restore -h

```
Usage: dotnet restore [options]

Options:
  -h, --help                           Show help information.
  -s, --source <SOURCE>                Specifies a NuGet package source to use during the restore.
  -r, --runtime <RUNTIME_IDENTIFIER>   Target runtime to restore packages for.
  --packages <PACKAGES_DIRECTORY>      Directory to install packages in.
  --disable-parallel                   Disables restoring multiple projects in parallel.
  --configfile <FILE>                  The NuGet configuration file to use.
  --no-cache                           Do not cache packages and http requests.
  --ignore-failed-sources              Treat package source failures as warnings.
  --no-dependencies                    Set this flag to ignore project to project references and only restore the root project.
  -f, --force                          Set this flag to force all dependencies to be resolved even if the last restore was successful. This is equivalent to deleting project.assets.json.
  -v, --verbosity                      Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
```

## ~/Documents/working $ dotnet run -h

```
Usage: dotnet run [options] [[--] <additional arguments>...]]

Options:
  -h, --help                            Show help information.
  -c, --configuration <CONFIGURATION>   Configuration to use for building the project.  Default for most projects is  "Debug".
  -f, --framework <FRAMEWORK>           Target framework to publish for. The target framework has to be specified in the project file.
  -p, --project                         The path to the project file to run (defaults to the current directory if there is only one project).
  --launch-profile                      The name of the launch profile (if any) to use when launching the application.
  --no-launch-profile                   Do not attempt to use launchSettings.json to configure the application.
  --no-build                            Do not build project before running.  Implies --no-restore.
  --no-restore                          Does not do an implicit restore when executing the command.
  -v, --verbosity                       Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
  --runtime <RUNTIME_IDENTIFIER>        Target runtime to restore packages for.
  --no-dependencies                     Set this flag to ignore project to project references and only restore the root project.
  --force                               Set this flag to force all dependencies to be resolved even if the last restore was successful. This is equivalent to deleting project.assets.json.
Additional Arguments:
  Arguments passed to the application that is being run.
```

* run needs new line at end

## ~/Documents/working $ dotnet build -h

```
Usage: dotnet build [options] <PROJECT>

Arguments:
  <PROJECT>   The MSBuild project file to build. If a project file is not specified, MSBuild searches the current working directory for a file that has a file extension that ends in `proj` and uses that file.

Options:
  -h, --help                            Show help information.
  -o, --output <OUTPUT_DIR>             Output directory in which to place built artifacts.
  -f, --framework <FRAMEWORK>           Target framework to publish for. The target framework has to be specified in the project file.
  -r, --runtime <RUNTIME_IDENTIFIER>    Publish the project for a given runtime. This is used when creating self-contained deployment. Default is to publish a framework-dependent app.
  -c, --configuration <CONFIGURATION>   Configuration to use for building the project.  Default for most projects is  "Debug".
  --version-suffix <VERSION_SUFFIX>     Defines the value for the $(VersionSuffix) property in the project.
  --no-incremental                      Disables incremental build.
  --no-dependencies                     Set this flag to ignore project-to-project references and only build the root project
  --no-restore                          Does not do an implicit restore when executing the command.
  -v, --verbosity                       Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
  --force                               Set this flag to force all dependencies to be resolved even if the last restore was successful. This is equivalent to deleting project.assets.json.
```

## ~/Documents/working $ dotnet publish -h

```
Usage: dotnet publish [options]

Options:
  -h, --help                            Show help information.
  -o, --output <OUTPUT_DIR>             Output directory in which to place the published artifacts.
  -f, --framework <FRAMEWORK>           Target framework to publish for. The target framework has to be specified in the project file.
  -r, --runtime <RUNTIME_IDENTIFIER>    Publish the project for a given runtime. This is used when creating self-contained deployment. Default is to publish a framework-dependent app.
  -c, --configuration <CONFIGURATION>   Configuration to use for building the project.  Default for most projects is  "Debug".
  --version-suffix <VERSION_SUFFIX>     Defines the value for the $(VersionSuffix) property in the project.
  --manifest <manifest.xml>             The path to a target manifest file that contains the list of packages to be excluded from the publish step.
  --self-contained                      Publish the .NET Core runtime with your application so the runtime doesn't need to be installed on the target machine. Defaults to 'true' if a runtime identifier is specified.
  --no-restore                          Does not do an implicit restore when executing the command.
  -v, --verbosity                       Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
  --no-dependencies                     Set this flag to ignore project to project references and only restore the root project.
  --force                               Set this flag to force all dependencies to be resolved even if the last restore was successful. This is equivalent to deleting project.assets.json.
```

## ~/Documents/working $ dotnet test -h

```
~/Documents/working $ dotnet test -h
Usage: dotnet test [options] <PROJECT> [[--] <RunSettings arguments>...]]

Arguments:
  <PROJECT>   The project to test. Defaults to the current directory.

Options:
  -h, --help                                            Show help information.
  -s, --settings <SETTINGS_FILE>                        Settings to use when running tests.
  -t, --list-tests                                      Lists discovered tests
  --filter <EXPRESSION>                                 Run tests that match the given expression.
                                                        Examples:
                                                        Run tests with priority set to 1: --filter "Priority = 1"
                                                        Run a test with the specified full name: --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
                                                        Run tests that contain the specified name: --filter "FullyQualifiedName~Namespace.Class"
                                                        More info on filtering support: https://aka.ms/vstest-filtering
                                                        
  -a, --test-adapter-path <PATH_TO_ADAPTER>             Use custom adapters from the given path in the test run.
                                                        Example: --test-adapter-path <PATH_TO_ADAPTER>
  -l, --logger <LoggerUri/FriendlyName>                 Specify a logger for test results.
                                                        Examples:
                                                        Log in trx format using a unqiue file name: --logger trx
                                                        Log in trx format using the specified file name: --logger "trx;LogFileName=<TestResults.trx>"
                                                        More info on logger arguments support:https://aka.ms/vstest-report
  -c, --configuration <CONFIGURATION>                   Configuration to use for building the project.  Default for most projects is  "Debug".
  -f, --framework <FRAMEWORK>                           Target framework to publish for. The target framework has to be specified in the project file.
  -o, --output <OUTPUT_DIR>                             Directory in which to find the binaries to be run
  -d, --diag <PATH_TO_FILE>                             Enable verbose logs for test platform.
                                                        Logs are written to the provided file.
  --no-build                                            Do not build project before testing.  Implies --no-restore.
  -r, --results-directory <PATH_TO_RESULTS_DIRECTORY>   The directory where the test results are going to be placed. The specified directory will be created if it does not exist.
                                                        Example: --results-directory <PATH_TO_RESULTS_DIRECTORY>
  --collect <DATA_COLLECTOR_FRIENDLY_NAME>              Enables data collector for the test run.
                                                        More info here : https://aka.ms/vstest-collect
  --blame                                               Runs the test in blame mode. This option is helpful in isolating the problematic test causing test host crash. It creates an output file in the current directory as "Sequence.xml", that captures the order of execution of test before the crash.
  --no-restore                                          Does not do an implicit restore when executing the command.
  -v, --verbosity                                       Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].


RunSettings arguments:
  Arguments to pass runsettings configurations through commandline. Arguments may be specified as name-value pair of the form [name]=[value] after "-- ". Note the space after --. 
  Use a space to separate multiple[name] =[value].
  More info on RunSettings arguments support: https://aka.ms/vstest-runsettings-arguments
  Example: dotnet test -- MSTest.DeploymentEnabled=false MSTest.MapInconclusiveToFailed=True
```

* needs new line

## ~/Documents/working $ dotnet pack -h

```
Usage: dotnet pack [options]

Options:
  -h, --help                            Show help information.
  -o, --output <OUTPUT_DIR>             Directory in which to place built packages.
  --no-build                            Do not build project before packing.  Implies --no-restore.
  --include-symbols                     Include packages with symbols in addition to regular packages in output directory.
  --include-source                      Include PDBs and source files. Source files go into the src folder in the resulting nuget package
  -c, --configuration <CONFIGURATION>   Configuration to use for building the project.  Default for most projects is  "Debug".
  --version-suffix <VERSION_SUFFIX>     Defines the value for the $(VersionSuffix) property in the project.
  -s, --serviceable                     Set the serviceable flag in the package. For more information, please see https://aka.ms/nupkgservicing.
  --no-restore                          Does not do an implicit restore when executing the command.
  -v, --verbosity                       Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
  --runtime <RUNTIME_IDENTIFIER>        Target runtime to restore packages for.
  --no-dependencies                     Set this flag to ignore project to project references and only restore the root project.
  --force                               Set this flag to force all dependencies to be resolved even if the last restore was successful. This is equivalent to deleting project.assets.json.

```

## ~/Documents/working $ dotnet migrate -h

```
Usage: dotnet migrate [options] <PROJECT_JSON/GLOBAL_JSON/SOLUTION_FILE/PROJECT_DIR>

Arguments:
  <PROJECT_JSON/GLOBAL_JSON/SOLUTION_FILE/PROJECT_DIR>   The path to one of the following:
                                                         - a project.json file to migrate.
                                                         - a global.json file, it will migrate the folders specified in global.json.
                                                         - a solution.sln file, it will migrate the projects referenced in the solution.
                                                         - a directory to migrate, it will recursively search for project.json files to migrate.
                                                         Defaults to current directory if nothing is specified.

Options:
  -h, --help                      Show help information.
  -t, --template-file             Base MSBuild template to use for migrated app. The default is the project included in dotnet new.
  -v, --sdk-package-version       The version of the SDK package that will be referenced in the migrated app. The default is the version of the SDK in dotnet new.
  -x, --xproj-file                The path to the xproj file to use. Required when there is more than one xproj in a project directory.
  -s, --skip-project-references   Skip migrating project references. By default, project references are migrated recursively.
  -r, --report-file               Output migration report to the given file in addition to the console.
  --format-report-file-json       Output migration report file as json rather than user messages.
  --skip-backup                   Skip moving project.json, global.json, and *.xproj to a `backup` directory after successful migration.

```

## ~/Documents/working $ dotnet clean -h

```
Usage: dotnet clean [options]

Options:
  -h, --help                            Show help information.
  -o, --output <OUTPUT_DIR>             Directory in which the build outputs have been placed.
  -f, --framework <FRAMEWORK>           Target framework to publish for. The target framework has to be specified in the project file.
  -r, --runtime <RUNTIME_IDENTIFIER>    Publish the project for a given runtime. This is used when creating self-contained deployment. Default is to publish a framework-dependent app.
  -c, --configuration <CONFIGURATION>   Configuration to use for building the project.  Default for most projects is  "Debug".
  -v, --verbosity                       Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].


```

## ~/Documents/working $ dotnet sln -h

```
Usage: dotnet sln [options] <SLN_FILE> [command]

Arguments:
  <SLN_FILE>   Solution file to operate on. If not specified, the command will search the current directory for one.

Options:
  -h, --help   Show help information.

Commands:
  add <args>      .NET Add project(s) to a solution file Command
  list            .NET List project(s) in a solution file Command
  remove <args>   .NET Remove project(s) from a solution file Command

```

## ~/Documents/working $ dotnet sln add -h

```
Usage: dotnet sln <SLN_FILE> add [options] <args>

Arguments:
  <SLN_FILE>   Solution file to operate on. If not specified, the command will search the current directory for one.
  <args>       Add one or more specified projects to the solution.

Options:
  -h, --help   Show help information.

```

## ~/Documents/working $ dotnet sln list -h

```
Usage: dotnet sln <SLN_FILE> list [options]

Arguments:
  <SLN_FILE>   Solution file to operate on. If not specified, the command will search the current directory for one.

Options:
  -h, --help   Show help information.
```

## ~/Documents/working $ dotnet sln remove -h

```
Usage: dotnet sln <SLN_FILE> remove [options] <args>

Arguments:
  <SLN_FILE>   Solution file to operate on. If not specified, the command will search the current directory for one.
  <args>       Remove the specified project(s) from the solution. The project is not impacted.

Options:
  -h, --help   Show help information.
```

## ~/Documents/working $ dotnet add -h

```
Usage: dotnet add [options] <PROJECT> [command]

Arguments:
  <PROJECT>   The project file to operate on. If a file is not specified, the command will search the current directory for one.

Options:
  -h, --help   Show help information.

Commands:
  package <PACKAGE_NAME>   .NET Add Package reference Command
  reference <args>         .NET Add Project to Project reference Command
```

## ~/Documents/working $ dotnet add package -h

```
Usage: dotnet add <PROJECT> package [options] <PACKAGE_NAME>

Arguments:
  <PROJECT>        The project file to operate on. If a file is not specified, the command will search the current directory for one.
  <PACKAGE_NAME>   The package reference to add.

Options:
  -h, --help                                Show help information.
  -v, --version <VERSION>                   Version for the package to be added.
  -f, --framework <FRAMEWORK>               Adds reference only when targeting a specific framework.
  -n, --no-restore                          Adds reference without performing restore preview and compatibility check.
  -s, --source <SOURCE>                     Specifies NuGet package sources to use during the restore.
  --package-directory <PACKAGE_DIRECTORY>   Restores the packages to the specified directory.
```

## ~/Documents/working $ dotnet add reference -h

```
Usage: dotnet add <PROJECT> reference [options] <args>

Arguments:
  <PROJECT>   The project file to operate on. If a file is not specified, the command will search the current directory for one.
  <args>      Project to project references to add

Options:
  -h, --help                    Show help information.
  -f, --framework <FRAMEWORK>   Add reference only when targeting a specific framework
```

## ~/Documents/working $ dotnet remove -h

```
Usage: dotnet remove [options] <PROJECT> [command]

Arguments:
  <PROJECT>   The project file to operate on. If a file is not specified, the command will search the current directory for one.

Options:
  -h, --help   Show help information.

Commands:
  package <PACKAGE_NAME>   .NET Remove Package reference Command.
  reference <args>         .NET Remove Project to Project reference Command
```

## ~/Documents/working $ dotnet remove package -h

```
Usage: dotnet remove <PROJECT> package [options] <PACKAGE_NAME>

Arguments:
  <PROJECT>        The project file to operate on. If a file is not specified, the command will search the current directory for one.
  <PACKAGE_NAME>   Package reference to remove.

Options:
  -h, --help   Show help information.
```

## ~/Documents/working $ dotnet remove reference -h

```
Usage: dotnet remove <PROJECT> reference [options] <args>

Arguments:
  <PROJECT>   The project file to operate on. If a file is not specified, the command will search the current directory for one.
  <args>      Project to project references to remove

Options:
  -h, --help                    Show help information.
  -f, --framework <FRAMEWORK>   Remove reference only when targeting a specific framework

```

## ~/Documents/working $ dotnet list -h

```
Usage: dotnet list [options] <PROJECT> [command]

Arguments:
  <PROJECT>   The project file to operate on. If a file is not specified, the command will search the current directory for one.

Options:
  -h, --help   Show help information.

Commands:
  reference   .NET Core Project-to-Project dependency viewer

```

## ~/Documents/working $ dotnet nuget -h

```
NuGet Command Line 4.7.0.4

Usage: dotnet nuget [options] [command]

Options:
  -h|--help                   Show help information
  --version                   Show version information
  -v|--verbosity <verbosity>  The verbosity of logging to use. Allowed values: Debug, Verbose, Information, Minimal, Warning, Error.

Commands:
  delete  Deletes a package from the server.
  locals  Clears or lists local NuGet resources such as http requests cache, packages cache or machine-wide global packages folder.
  push    Pushes a package to the server and publishes it.

Use "dotnet nuget [command] --help" for more information about a command.
```

## ~/Documents/working $ dotnet nuget delete -h

```
Usage: dotnet nuget delete [arguments] [options]

Arguments:
  [root]  The Package Id and version.

Options:
  -h|--help               Show help information
  --force-english-output  Forces the application to run using an invariant, English-based culture.
  -s|--source <source>    Specifies the server URL
  --non-interactive       Do not prompt for user input or confirmations.
  -k|--api-key <apiKey>   The API key for the server.
  --no-service-endpoint   --no-service-endpoint does not append "api/v2/packages" to the source URL.
```

## Usage: dotnet nuget locals [arguments] [options]

```
Arguments:
  Cache Location(s)  Specifies the cache location(s) to list or clear.
<all | http-cache | global-packages | temp>

Options:
  -h|--help               Show help information
  --force-english-output  Forces the application to run using an invariant, English-based culture.
  -c|--clear              Clear the selected local resources or cache location(s).
  -l|--list               List the selected local resources or cache location(s).

```

## ~/Documents/working $ dotnet nuget push -h

```
Usage: dotnet nuget push [arguments] [options]

Arguments:
  [root]  Specify the path to the package and your API key to push the package to the server.

Options:
  -h|--help                      Show help information
  --force-english-output         Forces the application to run using an invariant, English-based culture.
  -s|--source <source>           Specifies the server URL
  -ss|--symbol-source <source>   Specifies the symbol server URL. If not specified, nuget.smbsrc.net is used when pushing to nuget.org.
  -t|--timeout <timeout>         Specifies the timeout for pushing to a server in seconds. Defaults to 300 seconds (5 minutes).
  -k|--api-key <apiKey>          The API key for the server.
  -sk|--symbol-api-key <apiKey>  The API key for the symbol server.
  -d|--disable-buffering         Disable buffering when pushing to an HTTP(S) server to decrease memory usage.
  -n|--no-symbols                If a symbols package exists, it will not be pushed to a symbols server.
  --no-service-endpoint          --no-service-endpoint does not append "api/v2/packages" to the source URL.

```

## ~/Documents/working $ dotnet msbuild -h

```
Microsoft (R) Build Engine version 15.7.145.53551 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

Syntax:              MSBuild.exe [options] [project file | directory]

Description:         Builds the specified targets in the project file. If
                     a project file is not specified, MSBuild searches the
                     current working directory for a file that has a file
                     extension that ends in "proj" and uses that file.  If
                     a directory is specified, MSBuild searches that
                     directory for a project file.

Switches:

  /target:<targets>  Build these targets in this project. Use a semicolon or a
                     comma to separate multiple targets, or specify each
                     target separately. (Short form: /t)
                     Example:
                       /target:Resources;Compile

  /property:<n>=<v>  Set or override these project-level properties. <n> is
                     the property name, and <v> is the property value. Use a
                     semicolon or a comma to separate multiple properties, or
                     specify each property separately. (Short form: /p)
                     Example:
                       /property:WarningLevel=2;OutDir=bin\Debug\

  /maxcpucount[:n]   Specifies the maximum number of concurrent processes to
                     build with. If the switch is not used, the default
                     value used is 1. If the switch is used without a value
                     MSBuild will use up to the number of processors on the
                     computer. (Short form: /m[:n])
      
  /toolsversion:<version>
                     The version of the MSBuild Toolset (tasks, targets, etc.)
                     to use during build. This version will override the
                     versions specified by individual projects. (Short form:
                     /tv)
                     Example:
                       /toolsversion:3.5
   
  /verbosity:<level> Display this amount of information in the event log.
                     The available verbosity levels are: q[uiet], m[inimal],
                     n[ormal], d[etailed], and diag[nostic]. (Short form: /v)
                     Example:
                       /verbosity:quiet

  /consoleloggerparameters:<parameters>
                     Parameters to console logger. (Short form: /clp)
                     The available parameters are:
                        PerformanceSummary--Show time spent in tasks, targets
                            and projects.
                        Summary--Show error and warning summary at the end.
                        NoSummary--Don't show error and warning summary at the
                            end.
                        ErrorsOnly--Show only errors.
                        WarningsOnly--Show only warnings.
                        NoItemAndPropertyList--Don't show list of items and
                            properties at the start of each project build.
                        ShowCommandLine--Show TaskCommandLineEvent messages
                        ShowTimestamp--Display the Timestamp as a prefix to any
                            message.
                        ShowEventId--Show eventId for started events, finished
                            events, and messages
                        ForceNoAlign--Does not align the text to the size of
                            the console buffer
                        DisableConsoleColor--Use the default console colors
                            for all logging messages.
                        DisableMPLogging-- Disable the multiprocessor
                            logging style of output when running in
                            non-multiprocessor mode.
                        EnableMPLogging--Enable the multiprocessor logging
                            style even when running in non-multiprocessor
                            mode. This logging style is on by default.
                        ForceConsoleColor--Use ANSI console colors even if
                            console does not support it
                        Verbosity--overrides the /verbosity setting for this
                            logger.
                     Example:
                        /consoleloggerparameters:PerformanceSummary;NoSummary;
                                                 Verbosity=minimal

  /noconsolelogger   Disable the default console logger and do not log events
                     to the console. (Short form: /noconlog)

  /fileLogger[n]     Logs the build output to a file. By default
                     the file is in the current directory and named
                     "msbuild[n].log". Events from all nodes are combined into
                     a single log. The location of the file and other
                     parameters for the fileLogger can be specified through
                     the addition of the "/fileLoggerParameters[n]" switch.
                     "n" if present can be a digit from 1-9, allowing up to
                     10 file loggers to be attached. (Short form: /fl[n])
    
  /fileloggerparameters[n]:<parameters>
                     Provides any extra parameters for file loggers.
                     The presence of this switch implies the
                     corresponding /filelogger[n] switch.
                     "n" if present can be a digit from 1-9.
                     /fileloggerparameters is also used by any distributed
                     file logger, see description of /distributedFileLogger.
                     (Short form: /flp[n])
                     The same parameters listed for the console logger are
                     available. Some additional available parameters are:
                        LogFile--path to the log file into which the
                            build log will be written.
                        Append--determines if the build log will be appended
                            to or overwrite the log file. Setting the
                            switch appends the build log to the log file;
                            Not setting the switch overwrites the
                            contents of an existing log file.
                            The default is not to append to the log file.
                        Encoding--specifies the encoding for the file,
                            for example, UTF-8, Unicode, or ASCII
                     Default verbosity is Detailed.
                     Examples:
                       /fileLoggerParameters:LogFile=MyLog.log;Append;
                                           Verbosity=diagnostic;Encoding=UTF-8

                       /flp:Summary;Verbosity=minimal;LogFile=msbuild.sum
                       /flp1:warningsonly;logfile=msbuild.wrn
                       /flp2:errorsonly;logfile=msbuild.err
    
  /distributedlogger:<central logger>*<forwarding logger>
                     Use this logger to log events from MSBuild, attaching a
                     different logger instance to each node. To specify
                     multiple loggers, specify each logger separately.
                     (Short form /dl)
                     The <logger> syntax is:
                       [<logger class>,]<logger assembly>[;<logger parameters>]
                     The <logger class> syntax is:
                       [<partial or full namespace>.]<logger class name>
                     The <logger assembly> syntax is:
                       {<assembly name>[,<strong name>] | <assembly file>}
                     The <logger parameters> are optional, and are passed
                     to the logger exactly as you typed them. (Short form: /l)
                     Examples:
                       /dl:XMLLogger,MyLogger,Version=1.0.2,Culture=neutral
                       /dl:MyLogger,C:\My.dll*ForwardingLogger,C:\Logger.dll

  /distributedFileLogger
                     Logs the build output to multiple log files, one log file
                     per MSBuild node. The initial location for these files is
                     the current directory. By default the files are called
                     "MSBuild<nodeid>.log". The location of the files and
                     other parameters for the fileLogger can be specified
                     with the addition of the "/fileLoggerParameters" switch.

                     If a log file name is set through the fileLoggerParameters
                     switch the distributed logger will use the fileName as a
                     template and append the node id to this fileName to
                     create a log file for each node.
    
  /logger:<logger>   Use this logger to log events from MSBuild. To specify
                     multiple loggers, specify each logger separately.
                     The <logger> syntax is:
                       [<logger class>,]<logger assembly>[;<logger parameters>]
                     The <logger class> syntax is:
                       [<partial or full namespace>.]<logger class name>
                     The <logger assembly> syntax is:
                       {<assembly name>[,<strong name>] | <assembly file>}
                     The <logger parameters> are optional, and are passed
                     to the logger exactly as you typed them. (Short form: /l)
                     Examples:
                       /logger:XMLLogger,MyLogger,Version=1.0.2,Culture=neutral
                       /logger:XMLLogger,C:\Loggers\MyLogger.dll;OutputAsHTML

  /binaryLogger[:[LogFile=]output.binlog[;ProjectImports={None,Embed,ZipFile}]]
                     Serializes all build events to a compressed binary file.
                     By default the file is in the current directory and named
                     "msbuild.binlog". The binary log is a detailed description
                     of the build process that can later be used to reconstruct
                     text logs and used by other analysis tools. A binary log
                     is usually 10-20x smaller than the most detailed text
                     diagnostic-level log, but it contains more information.
                     (Short form: /bl)

                     The binary logger by default collects the source text of
                     project files, including all imported projects and target
                     files encountered during the build. The optional
                     ProjectImports switch controls this behavior:

                      ProjectImports=None     - Don't collect the project
                                                imports.
                      ProjectImports=Embed    - Embed project imports in the
                                                log file.
                      ProjectImports=ZipFile  - Save project files to
                                                output.projectimports.zip
                                                where output is the same name
                                                as the binary log file name.

                     The default setting for ProjectImports is Embed.
                     Note: the logger does not collect non-MSBuild source files
                     such as .cs, .cpp etc.

                     A .binlog file can be "played back" by passing it to
                     msbuild.exe as an argument instead of a project/solution.
                     Other loggers will receive the information contained
                     in the log file as if the original build was happening.
                     You can read more about the binary log and its usages at:
                     https://github.com/Microsoft/msbuild/wiki/Binary-Log

                     Examples:
                       /bl
                       /bl:output.binlog
                       /bl:output.binlog;ProjectImports=None
                       /bl:output.binlog;ProjectImports=ZipFile
                       /bl:..\..\custom.binlog
                       /binaryLogger
    
  /warnaserror[:code[;code2]]
                     List of warning codes to treats as errors.  Use a semicolon
                     or a comma to separate multiple warning codes. To treat all
                     warnings as errors use the switch with no values.
                     (Short form: /err[:c;[c2]])

                     Example:
                       /warnaserror:MSB4130

                     When a warning is treated as an error the target will
                     continue to execute as if it was a warning but the overall
                     build will fail.
    
  /warnasmessage[:code[;code2]]
                     List of warning codes to treats as low importance
                     messages.  Use a semicolon or a comma to separate
                     multiple warning codes.
                     (Short form: /nowarn[:c;[c2]])

                     Example:
                       /warnasmessage:MSB3026
    
  /ignoreprojectextensions:<extensions>
                     List of extensions to ignore when determining which
                     project file to build. Use a semicolon or a comma
                     to separate multiple extensions.
                     (Short form: /ignore)
                     Example:
                       /ignoreprojectextensions:.sln
    
  /nodeReuse:<parameters>
                     Enables or Disables the reuse of MSBuild nodes.
                     The parameters are:
                     True --Nodes will remain after the build completes
                            and will be reused by subsequent builds (default)
                     False--Nodes will not remain after the build completes
                     (Short form: /nr)
                     Example:
                       /nr:true
    
  /preprocess[:file]
                     Creates a single, aggregated project file by
                     inlining all the files that would be imported during a
                     build, with their boundaries marked. This can be
                     useful for figuring out what files are being imported
                     and from where, and what they will contribute to
                     the build. By default the output is written to
                     the console window. If the path to an output file
                     is provided that will be used instead.
                     (Short form: /pp)
                     Example:
                       /pp:out.txt
    
  /detailedsummary
                     Shows detailed information at the end of the build
                     about the configurations built and how they were
                     scheduled to nodes.
                     (Short form: /ds)
    
  /restore[:True|False]
                     Runs a target named Restore prior to building
                     other targets.  This is useful when your project
                     tree requires packages to be restored before they
                     can be built. Specifying /restore is the same as
                     specifying /restore:True.  Use the parameter to
                     override a value that comes from a response file.
                     (Short form: /r)
    
  /restoreProperty:<n>=<v>
                     Set or override these project-level properties only
                     during restore and do not use properties specified
                     with the /property argument. <n> is the property
                     name, and <v> is the property value. Use a
                     semicolon or a comma to separate multiple properties,
                     or specify each property separately.
                     (Short form: /rp)
                     Example:
                       /restoreProperty:IsRestore=true;MyProperty=value
    
  /profileevaluation:<file>    
                     Profiles MSBuild evaluation and writes the result 
                     to the specified file. If the extension of the specified
                     file is '.md', the result is generated in markdown
                     format. Otherwise, a tab separated file is produced.
    
  @<file>            Insert command-line settings from a text file. To specify
                     multiple response files, specify each response file
                     separately.

                     Any response files named "msbuild.rsp" are automatically
                     consumed from the following locations:
                     (1) the directory of msbuild.exe
                     (2) the directory of the first project or solution built

  /noautoresponse    Do not auto-include any MSBuild.rsp files. (Short form:
                     /noautorsp)

  /nologo            Do not display the startup banner and copyright message.

  /version           Display version information only. (Short form: /ver)

  /help              Display this usage message. (Short form: /? or /h)

Examples:

        MSBuild MyApp.sln /t:Rebuild /p:Configuration=Release
        MSBuild MyApp.csproj /t:Clean
                             /p:Configuration=Debug;TargetFrameworkVersion=v3.5

```

## ~/Documents/working $ dotnet vstest -h

```
Microsoft (R) Test Execution Command Line Tool Version 15.7.0-preview-20180221-13
Copyright (c) Microsoft Corporation.  All rights reserved.

The test source file "/Users/kathleen/Documents/working/-h" provided was not found.
Usage: vstest.console.exe [Arguments] [Options] [[--] <RunSettings arguments>...]]

Description: Runs tests from the specified files.

Arguments:

[TestFileNames]
      Run tests from the specified files. Separate multiple test file names
      by spaces.
      Examples: mytestproject.dll
                mytestproject.dll myothertestproject.exe

Options:

--Tests|/Tests:<Test Names>
      Run tests with names that match the provided values. To provide multiple
      values, separate them by commas.
      Examples: /Tests:TestMethod1
                /Tests:TestMethod1,testMethod2

--TestCaseFilter|/TestCaseFilter:<Expression>
      Run tests that match the given expression.
      <Expression> is of the format <property>Operator<value>[|&<Expression>]
         where Operator is one of =, != or ~  (Operator ~ has 'contains'
         semantics and is applicable for string properties like DisplayName).
         Parenthesis () can be used to group sub-expressions.
      Examples: /TestCaseFilter:"Priority=1"
                /TestCaseFilter:"(FullyQualifiedName~Nightly
                                  |Name=MyTestMethod)"

--Framework|/Framework:<Framework Version>
      Target .Net Framework version to be used for test execution. 
      Valid values are ".NETFramework,Version=v4.5.1", ".NETCoreApp,Version=v1.0" etc.
      Other supported values are Framework35, Framework40, Framework45 and FrameworkCore10.

--Platform|/Platform:<Platform type>
      Target platform architecture to be used for test execution. 
      Valid values are x86, x64 and ARM.

--Settings|/Settings:<Settings File>
      Settings to use when running tests.

RunSettings arguments:
      Arguments to pass runsettings configurations through commandline. Arguments may be specified as name-value pair of the form [name]=[value] after "-- ". Note the space after --. 
      Use a space to separate multiple [name]=[value].
      More info on RunSettings arguments support: https://aka.ms/vstest-runsettings-arguments

-lt|--ListTests|/lt|/ListTests:<File Name>
      Lists all discovered tests from the given test container.

--Parallel|/Parallel
      Specifies that the tests be executed in parallel. By default up
      to all available cores on the machine may be used.
      The number of cores to use may be configured using a settings file.

--TestAdapterPath|/TestAdapterPath
      This makes vstest.console.exe process use custom test adapters
      from a given path (if any) in the test run. 
      Example  /TestAdapterPath:<pathToCustomAdapters>

--Blame|/Blame
      Runs the test in blame mode. This option is helpful in isolating the problematic test causing test host crash. It creates an output file in the current directory as "Sequence.xml", that captures the order of execution of test before the crash.

--Diag|/Diag:<Path to log file>
      Enable verbose logs for test platform.
      Logs are written to the provided file.

--logger|/logger:<Logger Uri/FriendlyName>
      Specify a logger for test results. For example, to log results into a 
      Visual Studio Test Results File (TRX) use /logger:trx[;LogFileName=<Defaults to unique file name>]
      Creates file in TestResults directory with given LogFileName.

      Change the verbosity level in log messages for console logger as shown below
      Example: /logger:console;verbosity=<Defaults to "minimal">
      Allowed values for verbosity: quiet, minimal, normal and detailed.

      Change the diagnostic level prefix for console logger as shown below
      Example: /logger:console;prefix=<Defaults to "false">
      More info on Console Logger here : https://aka.ms/console-logger

--ResultsDirectory|/ResultsDirectory
      Test results directory will be created in specified path if not exists.
      Example  /ResultsDirectory:<pathToResultsDirectory>

--ParentProcessId|/ParentProcessId:<ParentProcessId>
      Process Id of the Parent Process responsible for launching current process.

--Port|/Port:<Port>
      The Port for socket connection and receiving the event messages.

-?|--Help|/?|/Help
      Display this usage message.

--Collect|/Collect:<DataCollector FriendlyName>
      Enables data collector for the test run. More info here : https://aka.ms/vstest-collect

--InIsolation|/InIsolation
      Runs the tests in an isolated process. This makes vstest.console.exe 
      process less likely to be stopped on an error in the tests, but tests 
      may run slower.

@<file>
      Read response file for more options.

  To run tests:
    >vstest.console.exe tests.dll 
  To run tests with additional settings such as  data collectors:
    >vstest.console.exe  tests.dll /Settings:Local.RunSettings

```

## ~/Documents/working $ dotnet store -h

```
Usage: dotnet store [options]

Options:
  -h, --help                                   Show help information.
  -m, --manifest <PROJECT_MANIFEST>            The XML file that contains the list of packages to be stored.
  -f, --framework <FRAMEWORK>                  Target framework to publish for. The target framework has to be specified in the project file.
  --framework-version <FrameworkVersion>       The Microsoft.NETCore.App package version that will be used to run the assemblies.
  -r, --runtime <RUNTIME_IDENTIFIER>           Publish the project for a given runtime. This is used when creating self-contained deployment. Default is to publish a framework-dependent app.
  -o, --output <OUTPUT_DIR>                    Output directory in which to store the given assemblies.
  -w, --working-dir <IntermediateWorkingDir>   The directory used by the command to execute.
  --skip-optimization                          Skips the optimization phase.
  --skip-symbols                               Skips creating symbol files which can be used for profiling the optimized assemblies.
  -v, --verbosity                              Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].


```

## ~/Documents/working $ dotnet tool -h

```
Usage: dotnet tool [options] [command]

Options:
  -h, --help   Show help information.

Commands:
  install <PACKAGE_ID>     Installs a tool for use on the command line.
  uninstall <PACKAGE_ID>   Uninstalls a tool.
  update <PACKAGE_ID>      Updates a tool to the latest stable version for use.
  list                     Lists installed tools in the current development environment.


```

## ~/Documents/working $ dotnet tool install -h

```
Usage: dotnet tool install [options] <PACKAGE_ID>

Arguments:
  <PACKAGE_ID>   NuGet Package Id of the tool to install.

Options:
  -g, --global                  Install user wide.
  --tool-path                   Location where the tool will be installed.
  --version                     Version of the tool package in NuGet.
  --configfile                  The NuGet configuration file to use.
  --source-feed <SOURCE_FEED>   Adds an additional NuGet package source to use during installation.
  -f, --framework               The target framework to install the tool for.
  -h, --help                    Show help information.
  -v, --verbosity               Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].

```

## ~/Documents/working $ dotnet tool uninstall -h

```
Usage: dotnet tool uninstall [options] <PACKAGE_ID>

Arguments:
  <PACKAGE_ID>   NuGet Package Id of the tool to uninstall.

Options:
  -g, --global   Uninstall user wide.
  --tool-path    Location where the tool was previously installed.
  -h, --help     Show help information.

```

## ~/Documents/working $ dotnet tool update -h

```
Usage: dotnet tool update [options] <PACKAGE_ID>

Arguments:
  <PACKAGE_ID>   NuGet Package Id of the tool to update.

Options:
  -g, --global                  Update user wide.
  --tool-path                   Location where the tool will be installed.
  --configfile                  The NuGet configuration file to use.
  --source-feed <SOURCE_FEED>   Adds an additional NuGet package source to use during update.
  -f, --framework               The target framework to update the tool for.
  -h, --help                    Show help information.
  -v, --verbosity               Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].

```

## ~/Documents/working $ dotnet tool list -h

```
Usage: dotnet tool list [options]

Options:
  -g, --global   List user wide tools.
  --tool-path    Location where the tools are installed.
  -h, --help     Show help information.

```

## ~/Documents/working $ dotnet buildserver -h

```
Usage: dotnet buildserver [options] [command]

Options:
  -h, --help   Show help information.

Commands:
  shutdown   Shuts down build servers that are started from dotnet. By default, all servers are shut down.
```

## ~/Documents/working $ dotnet help -h

```
Usage: dotnet help [options] <COMMAND_NAME>

Arguments:
  <COMMAND_NAME>   CLI command for which to view more detailed help.

Options:
  -h, --help   Show help information.

```
