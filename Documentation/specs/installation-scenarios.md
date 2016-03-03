# Notes
This section describes placeholders used inside this spec.

| Placeholder | Description |
| --- | --- |
| `<Channel>` | `(nightly|preview|production)`. TODO: more info |
| `<Version>` | 4-part-number version |
| `<OSName>`  | `(win|ubuntu|rhel|osx|debian)` - code for OS name |
| `<LowestSupportedOSVersion>` | Lowest supported OS Version |
| `<Architecture>` | Processor architecture related to binaries produced |
| `<Extension>` | File extension. This will be described in more details later for each OS separately. |
| `<OSID>` | Abbreviation for: `<OSName>.<LowestSupportedOSVersion>.<Architecture>` |
| `<VersionPointer>` | `(latest|lkg)` |
| `<ExecutableExtension>` | Executable extension including dot specific to OS (can be empty string) |
| `<CommitHash>` | Commit hash related to state of repository from where build with specific `<Version>` was build |

# Build Output
Each official, successful build should create and upload packages to location described by following URL:
```
https://dotnetcli.blob.core.windows.net/dotnet/<Channel>/<Version>/dotnet.<OSID>.<Version>.<Extension>
```
Content of the package should contain binaries which layout will be described later.

Additionally each build should update `latest` [version pointer](#version-pointers) and version badge
Latest/LKG information:
- [Version file](#version-files): `https://dotnetcli.blob.core.windows.net/dotnet/<Channel>/<VersionPointer>.<OSID>.version`

TODO: permanent links


## Windows outputs
WIP

## Ubuntu outputs
WIP

## RedHat/CentOS outputs
WIP

## Debian outputs
WIP

## Example build output links
WIP

## Questions
- Should <Version> include channel name to avoid situation where you have two files on your computer and latest file might have lower version than the newest?

# Version pointers

## Version files
Version files can be found in multiple places:
- Package: relative path inside the package ./.version
- Latest/LKG version file: WIP

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

# Package content
Currently package is required to contain two files:
- .version - [version file](#version-file)
## Disk Layout
```
.\
    .version
    bin\
        dotnet<ExecutableExtension>
```
