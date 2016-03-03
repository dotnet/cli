# Notes
This section describes placeholders used inside this spec.

| Placeholder | Description |
| ---: | :--- |
| `<Channel>` | `(future|preview|production)`. [See more info](#channels) |
| `<Version>` | 4-part-number version |
| `<OSName>`  | `(win|ubuntu|rhel|osx|debian)` - code for OS name |
| `<LowestSupportedOSVersion>` | Lowest supported OS Version |
| `<Architecture>` | Processor architecture related to binaries produced |
| `<Extension>` | File extension. This will be described in more details later for each OS separately. |
| `<OSID>` | Abbreviation for: `<OSName><LowestSupportedOSVersion>.<Architecture>`. [See more info](#osid) |
| `<VersionPointer>` | `(latest|lkg)` |
| `<ExecutableExtension>` | Executable extension including dot specific to OS (can be empty string) |
| `<CommitHash>` | Commit hash related to state of repository from where build with specific `<Version>` was build |

# Build Output
Each official, successful build should create and upload packages to location described by following URL:
```
https://dotnetcli.blob.core.windows.net/dotnet/<Channel>/<Version>/dotnet.<OSID>.<Version>.<Extension>
```
Content of the package should contain binaries which layout will be described later.

Additionally each build should update `latest` [version descriptors](#version-descriptors)

## Windows output

Nuget - WIP, this should include versioning scheme

| `<Extension>` | Description |
| --- | :--- |
| exe | Installer bundle. It should be used by end customers |
| msi | Installer package. It should be used by customers wanting to include cli as part of their setup |
| zip | Packed binaries. It is used by [installation script](#installation-scripts) |

## OSX output

| `<Extension>` | Description |
| --- | :--- |
| pkg | WIP |
| tar.gz | Packed binaries. It is used by [installation script](#installation-scripts) |

## Ubuntu output

Debian feed - WIP, this should include versioning scheme

| `<Extension>` | Description |
| --- | :--- |
| tar.gz | Packed binaries. It is used by [installation script](#installation-scripts) |

## RedHat/CentOS output
WIP

## Debian output
WIP

## Example build output links
WIP

## Questions
- Should <Version> include channel name to avoid situation where you have two files on your computer and latest file might have lower version than the newest?

# Obtaining dotnet

## Installation scripts

Installation script is a shell script which lets customers install dotnet.

For Windows we are using PowerShell script (install-dotnet.ps1).
For any other OS we are using bash script (install-dotnet.sh)

WIP: Exact script action description.

### Script arguments description

| PowerShell script | Bash script | Default | Description |
| --- | --- | --- | --- |
| -Channel | WIP | production | Which [channel](#channels) to install from. Possible values: `future`, `preview`, `production` |
| -Version | WIP | `global.json` or `latest` | |
| -InstallDir | WIP | Windows: `%LocalAppData%\Microsoft\.dotnet` | Path to where install dotnet. Note that binaries will be placed directly in a given directory. |
| -Architecture | WIP | auto | Possible values: `auto`, `x64`, `x86`. `auto` refers to currently running OS architecture. |
| -DebugSymbols | WIP | `<not set>` | If switch present, installation will include debug symbol |
| -DryRun | WIP | `<not set>` | If switch present, installation will not be performed and instead deterministic invocation with specific version and zip location will be displayed. |
| -NoPath | WIP | `<not set>` | If switch present the script will not set PATH environmental variable for the current process. |
| -Verbose | WIP | `<not set>` | If switch present displays diagnostics information. |
| -AzureFeed | WIP | `https://dotnetcli.blob.core.windows.net/dotnet` | Azure feed URL. |

### Script location
WIP: permanent link for obtaining latest version
WIP: versioning description
Newest version of the scripts can be found in the repository under following directory:
```
https://github.com/dotnet/cli/tree/rel/1.0.0/scripts/obtain
```

Older version of the script can be obtained using:
```
https://github.com/dotnet/cli/blob/<commit_hash>/scripts/obtain
```

## Getting started page
WIP

## Repo landing page
WIP

# Version descriptors
## Version pointers
Version pointers represent URLs to the latest and Last Known Good (LKG) builds.
Specific URLs TBD. This will be something similar to following:
```
<Domain>/dotnet/<Channel>/<VersionPointer>.<OSID>.version
```

`<Domain>` TBD

## Version files
Version files can be found in multiple places:
- Package: relative path inside the package ./.version
- Latest/LKG version file: WIP

URL:
```
https://dotnetcli.blob.core.windows.net/dotnet/<Channel>/<VersionPointer>.<OSID>.version
```

### File content
Each version file contains two lines describing the build:
```
<CommitHash>
<Version>
```

## Version badge
Version badge (SVG) is an image with textual representation of `<Version>`. It can be found under following URL:
```
https://dotnetcli.blob.core.windows.net/dotnet/<Channel>/<VersionPointer>.<OSID>.svg
```

## Questions/gaps
- Version Pointer links should be permanent and hosted on a separate domain

# Package content
Currently package is required to contain two files:
- .version - [version file](#version-file)
- dotnet<ExecutableExtension> - entry point for all dotnet commands

## Disk Layout
```
.\
    .version
    bin\
        dotnet<ExecutableExtension>
```

# Channels
Currently we have 3 channels which gives us idea about stability and quality of product.

## Github branches relation

| Channel name | Github branch | Description |
| --- | --- | --- |
| future | master | Branch with |
| preview | rel/1.0.0 | Branch which is being stabilized. Most of the bugs and gaps are known. No new features expected on the branch. |
| production | N/A, prod? | Most stable branch |

Each branch on each successful build produces packages described in [build output](#build-output).

# OSID

**This requires more discussion**

OSID represents abbreviation for:
```
<OSName><LowestSupportedOSVersion>.<Architecture>
```
This gives us flexibility to easily create new binaries when OS makes a breaking change without creating confusing names.
Example names would be:
win7.x64 - currently we ship api-ms-*.dll which are irrelevant on higher Windows version we could possibly create slightly smaller package without them for win8.x64 - this currently is no issue as the files are fairly small but it is a good example on easiness of the process. In example if some of the dotnet cli files will be shipped with OS (who knows?) or Windows decides to do some breaking changes we could easily create new version.
