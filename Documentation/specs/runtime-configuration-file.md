# Runtime Configuration Files

The runtime configuration files store the dependencies of an application (formerly stored in the `.deps` file). They also include runtime configuration options, such as the Garbage Collector mode. Optionally they can also include data for runtime compilation (compilation settings used to compile the original application, and reference assemblies used by the application).

**Note:** This document doesn't provide full explanations as to why individual items are needed in this file. That is covered in the [host spec](corehost.md) and via the `Microsoft.Extensions.DependencyModel` assembly.

## What produces the files and where are they?

There are two runtime configuration files for a particular application. Given a project named `MyApp`, the compilation process produces the following files (on Windows, other platforms are similar):

* `MyApp.dll` - The managed assembly for `MyApp`, including an ECMA-compliant entry point token.
* `MyApp.exe` - A copy of the `apphost.exe` executable. This is present when the application is self-contained, and in newer functionality (2.1.0+) for framework-dependent applications that wish to support platform-specific (non-portable) executables.
* `MyApp.runtimeconfig.json` - An **optional** configuration file containing runtime configuration settings. This file is required for framework-dependent applications, but not for self-contained apps.
* `MyApp.runtimeconfig.dev.json` - An **optional** configuration file containing runtime configuration settings that typically only exists in a non-published output and thus is used for development-time scenarios. This file typically specifies additional probing paths. Depending on the semantics of each setting, the setting is either combined with or overridden by the values from `MyApp.runtimeconfig.json`.
* `MyApp.deps.json` - A list of dependencies, compilation dependencies and version information used to address assembly conflicts. Not technically required, but required to use the servicing or package cache/shared package install features, and to assist during roll-forward scenarios to select the newest version of any assembly that exists more than once in the application and framework(s). If the file is not present, all assemblies in the current folder are used instead.

The `MyApp.runtimeconfig.json` is designed to be user-editable (in the case of an app consumer wanting to change various CLR runtime options for an app, much like the `MyApp.exe.config` XML file works in .NET 4.x today). However, the `MyApp.deps.json` file is designed to be processed by automated tools and should not be user-edited. Having the files as separate makes this clearer. We could use a different format for the deps file, but if we're already integrating a JSON parser into the host, it seems most appropriate to re-use that here. Also, there are diagnostic benefits to being able to read the `.deps.json` file in a simple text editor.

**IMPORTANT**: Framework-dependent applications have some adjustments to this spec which are covered at the end.

## File format

The files are both JSON files stored in UTF-8 encoding. Below are sample files. Note that not all sections are required and some will be opt-in only (see below for more details). The `.runtimeconfig.json` file is completely optional, and in the `.deps.json` file, only the `runtimeTarget`, `targets` and `libraries` sections are required (and within the `targets` section, only the runtime-specific target is required). If no `.deps.json` exists, all assemblies local to the app will be added as TPA (trusted platform assemblies).

### [appname].runtimeconfig.json
```json
{
    "runtimeOptions": {
        "configProperties": {
            "System.GC.Server": true,
            "System.GC.Concurrent": true,
            "System.Threading.ThreadPool.MinThreads": 4,
            "System.Threading.ThreadPool.MaxThreads": 8
        },
        "framework": {
            "name": "Microsoft.NETCore.App",
            "version": "2.1.0"
        },
        "applyPatches": true,
        "rollForwardOnNoCandidateFx": 1
    }
}
```

### [appname].deps.json
```json
{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v2.0",
    "signature": "aafc507050a6c13a0cf2d6d4c3de136e6571da6e"
  },
  "compilationOptions": {
    "defines": [
		"TRACE",
		"DEBUG"
    ],
    "languageVersion": "",
    "platform": "",
    "warningsAsErrors": false,
  },
  "targets": {
    ".NETCoreApp,Version=v2.0": {
      "MyApp/1.0.0": {
        "dependencies": {
          "System.Banana": "1.0.0"
        },
        "runtime": {
          "MyApp.dll": {}
        }
      },
      "System.Banana/1.0.0": {
        "dependencies": {
          "System.Foo": "1.0.0"
        },
        "runtime": {
          "lib/netcoreapp2.0/System.Banana.dll": {
            "assemblyVersion": "1.0.0.0",
            "fileVersion": "1.0.0.0"
          }
        }
      },
      "System.Foo/1.0.0": {
        "runtime": {
          "lib/netcoreapp2.0/System.Foo.dll": {
            "assemblyVersion": "1.0.0.0",
            "fileVersion": "1.0.0.0"
          }
        }
      }
    }
  },
  "libraries": {
    "MyApp/1.0.0": {
      "type": "project",
      "serviceable": false,
      "sha512": ""
    },
    "System.Banana/1.0.0": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-C63ok7q+Fi6O6I/WB4ut3hFibGSraUlE461LxhhwNk5Vcdl4ijDhX1QDupDdp3Cxr7TgwB55Sd4zNtlwT7ksAA==",
      "path": "system.banana/1.0.0",
      "hashPath": "system.banana.1.0.0.nupkg.sha512"
    },
    "System.Foo/1.0.0": {
      "type": "package",
      "serviceable": true,
      "sha512": "sha512-avYGOiBQ4U/fJfzEDF7lzPLhk/w6P9/28y0iiQh3AxlWOheuZTgXA/pzuORuAu/s5B2bXHO2BlvQKZN0PfQ2HQ==",
      "path": "system.foo/1.0.0",
      "hashPath": "system.foo.1.0.0.nupkg.sha512"
    }
  }
}
```

## Sections

### `runtimeOptions` Section (`.runtimeconfig.json`)

This section is created when building a project. Settings include:
* `configProperties` - Indicates configuration properties to configure the runtime and the framework
  * Examples:
    * Full list of [configuration properties](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/clr-configuration-knobs.md) for CoreCLR.
    * `System.GC.Server` (old: `gcServer`) - Boolean indicating if the server GC should be used (Default: `true`).
    * `System.GC.Concurrent` (old: `gcConcurrent`) - Boolean indicating if background garbage collection should be used.
* `framework` - Indicates the `name`, `version`, and other properties of the shared framework to use when activating the application including `applyPathes` and `rollForwardOnNoCandidateFx`. The presence of this section indicates that the application is a framework-dependent app.
* `applyPatches` - When `false`, the framework version is strictly obeyed by the host. When `applyPatches` is unspecified or `true`, the framework from either the same or a higher version that differs only by the patch field will be used.
  * For example, if `version=1.0.1` and `applyPatches` is `true`, the host would load the shared framework from `1.0.{n}`, where `n >= 1`, but will not load from `1.1.0`, even if present. When `applyPatches` is `false`, the shared framework will be loaded from `1.0.1` strictly.
  * **Note:** This does not apply to `pre-release` versions; it applies only to `production` releases.
  * **Note:** This section should not exist self-contained applications because they do not rely upon a shared framework.
* `rollForwardOnNoCandidateFx` - Determines roll-forward behavior of major and minor. Only applies to `production` releases. Values: 0(Off), 1 (roll forward on [minor]), 2 (Roll forward on [major] and [minor])
 See [roll-forward-on-no-candidate documentation](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/roll-forward-on-no-candidate-fx.md) for more information.

These settings are read by host (apphost or dotnet executable) to determine how to initialize the runtime. All versions of the host **must ignore** settings in this section that they do not understand (thus allowing new settings to be added in later versions).

### `compilationOptions` Section (`.deps.json`)

This section is created during build from the project's settings. The exact settings found here are specific to the compiler that produced the original application binary. Some example settings include: `defines`, `languageVersion` (e.g. the version of C# or VB), `allowUnsafe` (a C# option), etc.

### `runtimeTarget` Section (`.deps.json`)

This property contains the name of the target from `targets` that should be used by the runtime. This is present to simplify the host so that it does not have to parse or understand target names and the meaning thereof.

### `targets` Section (`.deps.json`)

This section contains subsetted data from the input `project.lock.json`.

Each property under `targets` describes a "target", which is a collection of libraries required by the application when run or compiled in a certain framework and platform context. A target **must** specify a Framework name, and **may** specify a Runtime Identifier. Targets without Runtime Identifiers represent the dependencies and assets used for compiling the application for a particular framework. Targets with Runtime Identifiers represent the dependencies and assets used for running the application under a particular framework and on the platform defined by the Runtime Identifier. In the example above, the `.NETStandardApp,Version=v1.5` target lists the dependencies and assets used to compile the application for `dnxcore50`, and the `.NETStandardApp,Version=v1.5/osx.10.10-x64` target lists the dependencies and assets used to run the application on `dnxcore50` on a 64-bit Mac OS X 10.10 machine.

There will always be two targets in the `[appname].runtimeconfig.json` file: A compilation target, and a runtime target. The compilation target will be named with the framework name used for the compilation (`.NETStandardApp,Version=v1.5` in the example above). The runtime target will be named with the framework name and runtime identifier used to execute the application (`.NETStandardApp,Version=v1.5/osx.10.10-x64` in the example above). However, the runtime target will also be identified by name in the `runtimeOptions` section, so that the host does not need to parse and understand target names.

The content of each target property in the JSON is a JSON object. Each property of that JSON object represents a single dependency required by the application when compiled for/run on that target. The name of the property contains the ID and Version of the dependency in the form `[Id]/[Version]`. The content of the property is another JSON object containing metadata about the dependency.

The `dependencies` property of a dependency object defines the ID and Version of direct dependencies of this node. It is a JSON object where the property names are the ID of the dependency and the content of each property is the Version of the dependency.

The `runtime` property of a dependency object lists the relative paths to Managed Assemblies required to be available at runtime in order to satisfy this dependency. The paths are relative to the location of the Dependency (see below for further details on locating a dependency).

The `resources` property of a dependency object lists the relative paths and locales of Managed Satellite Assemblies which provide resources for other languages. Each item contains a `locale` property specifying the [IETF Language Tag](https://en.wikipedia.org/wiki/IETF_language_tag) for the satellite assembly (or more specifically, a value usable in the Culture field for a CLR Assembly Name).

The `native` property of a dependency object lists the relative paths to Native Libraries required to be available at runtime in order to satisfy this dependency. The paths are relative to the location of the Dependency (see below for further details on locating a dependency).

In compilation targets, the `runtime`, `resources` and `native` properties of a dependency are omitted, because they are not relevant to compilation. Similarly, in runtime targets, the `compile` property is omitted, because it is not relevant to runtime.

Only dependencies with a `type` value of `package` should be considered by the host. There may be other items, used for other purposes (for example, Projects, Reference Assemblies, etc.

### `libraries` Section (`.deps.json`)

This section contains a union of all the dependencies found in the various targets, and contains common metadata for them. Specifically, it contains the `type`, as well as a boolean indicating if the library can be serviced (`serviceable`, only for `package`-typed libraries) and a SHA-512 hash of the package file (`sha512`, only for `package`-typed libraries.

## How the file is used

The file is read by two different components:

* The host uses it to determine what to place on the TPA and Native Library Search Path lists, as well as what runtime settings to apply (GC type, etc.). See [the host spec](corehost.md).
* `Microsoft.Extensions.DependencyModel` uses it to allow a running managed application to query various data about it's dependencies. For example:
  * To find all dependencies that depend on a particular package (used by ASP.NET MVC and other plugin-based systems to identify assemblies that should be searched for possible plugin implementations)
  * To determine the reference assemblies used by the application when it was compiled in order to allow runtime compilation to use the same reference assemblies (used by ASP.NET Razor to compile views)
  * To determine the compilation settings used by the application in order to allow runtime compilation to use the same settings (also used by ASP.NET Razor views).

## Opt-In Compilation Data

Some of the sections in the `.deps.json` file contain data used for runtime compilation. This data is not provided in the file by default. Instead, a project.json setting `preserveCompilationContext` must be set to true in order to ensure this data is added. Without this setting, the `compilationOptions` will not be present in the file, and the `targets` section will contain only the runtime target.

## Framework-dependent Deployment Model

An application can be deployed in a "framework-dependent" deployment model. In this case, the RID-specific assets of packages are published within a folder structure that preserves the RID metadata. However the host does not use this folder structure, rather it reads data from the `.deps.json` file. Also, during deployment, the `.exe` file (`apphost.exe` renamed) is not deployed by default.

In the framework-dependent deployment model, the `*.runtimeConfig.json` file will contain the `runtimeOptions.framework` section:

```json
{
    "runtimeOptions": {
        "framework": {
            "name": "Microsoft.NETCore.App",
            "version": "1.0.1"
        }
    }
}
```

This data is used to locate the shared framework folder. The exact mechanics of which version are selected are defined elsewhere, but in general, it locates the shared runtime in the `shared` folder located beside it by using the relative path `shared/[runtimeOptions.framework.name]/[runtimeOptions.framework.version]`. Once it has applied any version roll-forward logic and come to a final path to the shared framework, it locates the `[runtimeOptions.framework.name].deps.json` file within that folder and loads it **first**.

Next, the deps file from the application is loaded and merged into this deps file (this is conceptual, the host implementation doesn't necessary have to directly merge the data ;)). Data from the app-local deps file trumps data from the shared framework.

The shared framework's deps file will also contain a `runtimes` section defining the fallback logic for all RIDs known to that shared framework. For example, a shared framework deps file installed into a Ubuntu machine may look something like the following:

```json
{
    "runtimeTarget": {
        "name": ".NETStandardApp,Version=v1.5",
        "portable": false
    },
    "targets": {
        ".NETStandardApp,Version=v1.5": {
            "System.Runtime/4.0.0": {
                "runtime": "lib/netstandard1.5/System.Runtime.dll"
            },
            "... other libraries ...": {}
        }
    },
    "libraries": {
        "System.Runtime/4.0.0": {
            "type": "package",
            "serviceable": true,
            "sha512": "[base64 string]"
        },
        "... other libraries ...": {}
    },
    "runtimes": {
        "ubuntu.15.04-x64": [ "ubuntu.14.10-x64", "ubuntu.14.04-x64", "debian.8-x64", "linux-x64", "linux", "unix", "any", "base" ],
        "ubuntu.14.10-x64": [ "ubuntu.14.04-x64", "debian.8-x64", "linux-x64", "linux", "unix", "any", "base" ],
        "ubuntu.14.04-x64": [ "debian.8-x64", "linux-x64", "linux", "unix", "any", "base" ]
    }
}
```

The host will detect the RID at runtime (for example, `win10-x64` for Windows 64-bit). It will look up the corresponding entry in the `runtimes` section to identify what the fallback list is for `win10-x64`. The fallbacks are identified from most-specific to least-specific. In the case of `win10-x64` and the example above, the fallback list is: `"win10-x64", "win10", "win81-x64", "win81", "win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"` (note that an exact match on the RID itself is the first preference, followed by the first item in the fallback list, then the next item, and so on).

In the app-local deps file for a `framework-dependent` application, the package entries may have an additional `runtimeTargets` section detailing RID-specific assets. The host should use this data, along with the current RID and the RID fallback data defined in the `runtimes` section of the shared framework deps file to select one **and only one** RID value out of each package individually. The most specific RID present within the package should always be selected.

Consider an application built for `ubuntu.14.04-x64` and the following snippet from an app-local deps file (some sections removed for brevity).

```json
{
    "targets": {
        ".NETStandardApp,Version=v1.5": {
            "System.Data.SqlClient/4.0.0": {
                "compile": {
                    "ref/netstandard1.5/System.Data.SqlClient.dll": {}
                },
                "runtimeTargets": {
                    "runtimes/unix/lib/netstandard1.5/System.Data.SqlClient.dll": {
                        "assetType": "runtime",
                        "rid": "unix"
                    },
                    "runtimes/win7-x64/lib/netstandard1.5/System.Data.SqlClient.dll": {
                        "assetType": "runtime",
                        "rid": "win7-x64"
                    },
                    "runtimes/win7-x86/lib/netstandard1.5/System.Data.SqlClient.dll": {
                        "assetType": "runtime",
                        "rid": "win7-x86"
                    },
                    "runtimes/win7-x64/native/sni.dll": {
                        "assetType": "native",
                        "rid": "win7-x64"
                    },
                    "runtimes/win7-x86/native/sni.dll": {
                        "assetType": "native",
                        "rid": "win7-x86"
                    }
                }
            }
        }
    }
}
```

When setting up the TPA and native library lists, it will do the following for the `System.Data.SqlClient` entry in the example above:

1. Add all entries from the root `runtime` and `native` sections (not present in the example). (Note: This is essentially the current behavior for the existing deps file format)
2. Add all appropriate entries from the `runtimeTargets` section, based on the `rid` property of each item:
  1. Attempt to locate any item for the RID `ubuntu.14.04-x64`. If any asset is matched, take **only** the items matching that RID exactly and add them to the appropriate lists based on the `assetType` value (`runtime` for managed code, `native` for native code)
  2. Reattempt the previous step using the first RID in the list provided by the list in the `runtimes."ubuntu.14.04-x64"` section of the shared framework deps file. If any asset is matched, take **only** the items matching that RID exactly and add them to the appropriate lists
  3. Continue to reattempt the previous search for each RID in the list, from left to right until a match is found or the list is exhausted. Exhausting the list without finding an asset, when a `runtimeTargets` section is present is **not** an error, it simply indicates that there is no need for a runtime-specific asset for that package.

Note one important aspect about asset resolution: The resolution scope is **per-package**, **not per-application**, **nor per-asset**. For each individual package, the most appropriate RID is selected, and **all** assets taken from that package must match the selected RID exactly. For example, if a package provides both a `linux-x64` and a `unix` RID (in the `ubuntu.14.04-x64` example above), **only** the `linux-x64` asset would be selected for that package. However, if a different package provides only a `unix` RID, then the asset from the `unix` RID would be selected.

The path to a runtime-specific asset is resolved in the same way as a normal asset (first check Servicing, then Package Cache, App-Local, Global Packages Location, etc.) with **one exception**. When searching app-local, rather than just looking for the simple file name in the app-local directory, a runtime-specific asset is expected to be located in a subdirectory matching the relative path information for that asset in the lock file. So the `native` `sni.dll` asset for `win7-x64` in the `System.Data.SqlClient` example above would be located at `APPROOT/runtimes/win7-x64/native/sni.dll`, rather than the normal app-local path of `APPROOT/sni.dll`.

## Opt-In [appname].runtimeconfig.json Explicit Overrides for Framework Settings
In order to address potential issues with compatibility, an application can override a framework's runtimeconfig.json settings. This should only be done with the understanding that any settings specified here have unintended consequences and may prevent future upgrade \ roll-forward compatibility. The settings include `version`, `rollForwardOnNoCandidateFx` and `applyPatches`.

As an example, assume the following framework layering:
- Application
- Microsoft.AspNetCore.All
- Microsoft.AspNetCore.App
- Microsoft.NetCore.App

Except for Microsoft.NetCore.App (since it does not have a lower-level framework), each layer has a runtimeconfig.json setting specifying a single lower-layer framework's `name`, `version` and optional `rollForwardOnNoCandidateFx` and `applyPatches`.

The normal hierarchy processing for most knobs, such as `rollForwardOnNoCandidateFx`:
 - a) Default value determined by the framework (e.g. roll-forward on [Minor])
 - b) Environment variable override (e.g. `DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX`)
 - c) Each layer's `runtimeOptions` override setting in its runtimeconfig.json, starting with app (e.g. `rollForwardOnNoCandidateFx`). Lower layers can override this.
 - d) The app's `additionalFrameworks` override section in `[appname].runtimeconfig.json` which specifies knobs per-framework.
 - e) A `--` command line value such as `--roll-forward-on-no-candidate-fx`

In a hypothetical example, `Microsoft.AspNetCore.App` turns on the ability via mechanism `c` above to roll-forward `Microsoft.NetCore.App` on [major] releases by specifying `rollForwardOnNoCandidateFx = 2` in its runtimeconfig.json. For example:
```json
{
    "runtimeOptions": {
        "framework": {
            "name": "Microsoft.NetCore.App",
            "version": "2.2.0"
        },
        "rollForwardOnNoCandidateFx": "2"
    }
}
```

However, if that behavior is not wanted by the app, the app has the option of overriding. This cannot be done by the same mechanism `c` because the app's runtimeconfig settings would be overridden by the sample above since the sample is in a lower layer. Thus to override the setting, mechanism `d` is used. An example of the `additionalFrameworks` section for mechanism `d`:
```json
{
    "runtimeOptions": {
        "framework": {
            "name": "Microsoft.AspNetCore.All",
            "version": "1.0.1"
        },
        "additionalFrameworks": [
            {
                "name": "Microsoft.AspNetCore.App",
                "rollForwardOnNoCandidateFx": "1",
            }
        ]
    }
}
```
