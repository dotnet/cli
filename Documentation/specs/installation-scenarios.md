# Build output
## Notes
This section describes placeholders used inside URLs provided later.

| Placeholder | Description |
| --- | --- |
| `<Channel>` | `(nightly|preview|production)`. TODO: more info |
| `<Version>` | 4-part-number version |
| `<OSName>`  | `(win|ubuntu|rhel|osx|debian)` |
| `<LowestSupportedOSVersion>` | Lowest supported OS Version |
| `<Architecture>` | Processor architecture related to binaries produced |
| `<Extension>` | File extension. This will be described in more details later for each OS separately. |
| `<OSID>` | Abbreviation for: `<OSName>.<LowestSupportedOSVersion>.<Architecture>` |
| `<VersionPointer>` | `(latest|lkg)` |

## Build Outputs
Each official, successful build should create and upload packages to location described by following URL:
```
https://dotnetcli.blob.core.windows.net/dotnet/<Channel>/<Version>/dotnet.<OSID>.<Version>.<Extension>
```
Content of the package should contain binaries which specifics will be described later. 

Latest/LKG information:
- Version file: https://dotnetcli.blob.core.windows.net/dotnet/<Channel>/<VersionPointer>.<OSID>.version
- Version badge (SVG): https://dotnetcli.blob.core.windows.net/dotnet/<Channel>/<VersionPointer>.<OSID>.svg
TODO: permanent links


### Windows outputs
WIP

### Ubuntu outputs
WIP

### RedHat/CentOS outputs
WIP

### Debian outputs
WIP

## Example build output links
WIP

## Questions
- Should <Version> include channel name to avoid situation where you have two files on your computer and latest file might have lower version than the newest?

# Version file
Version files can be found in multiple places:
- Package: relative path inside the package ./.version
- Latest/LKG version file: WIP

## File content

# Package content
