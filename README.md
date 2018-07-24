# .NET Core SDK

[![.NET Slack Status](https://aspnetcoreslack.herokuapp.com/badge.svg?2)](http://tattoocoder.com/aspnet-slack-sign-up/) [![Join the chat at https://gitter.im/dotnet/cli](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/cli?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This repo contains the source code for cross-platform [.NET Core](http://github.com/dotnet/core) SDK. It aggregates the .NET Toolchain, the .NET Core runtime, the templates, the offline packages cache and the ASP.NET Runtime store. It produces zip and tarballs as well as the native packages for various supported platforms.

Looking for V1 of the .NET Core tooling?
----------------------------------------

If you are looking for the v2.0 release of the .NET Core tools (CLI, MSBuild and the new csproj), head over to https://dot.net/core and download!

> **Note:** the master branch of the .NET Core SDK repo is based on an upcoming update of the SDK and is considered pre-release. For production-level usage, please use the
> released version of the tools available at https://dot.net/core

Found an issue?
---------------
You can consult the [Documents Index for the CLI repo](https://github.com/dotnet/cli/blob/master/Documentation/README.md) to find out the current issues and to see the workarounds and to see how to file new issues.

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

Build Status
------------

|Windows x64|Windows x86|macOS|Linux x64 Archive|Linux arm Archive|Linux arm64 Archive|Linux Native Installers|RHEL 6 Archive|Linux-musl Archive|
|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|
|[![][win-x64-build-badge]][win-x64-build]|[![][win-x86-build-badge]][win-x86-build]|[![][osx-build-badge]][osx-build]|[![][linux-build-badge]][linux-build]|[![][linux-arm-build-badge]][linux-arm-build]|[![][linux-arm64-build-badge]][linux-arm64-build]|[![][linuxnative-build-badge]][linuxnative-build]|[![][rhel6-build-badge]][rhel6-build]|[![][linux-musl-build-badge]][linux-musl-build]|

[win-x64-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9472/badge
[win-x64-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9472

[win-x86-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9464/badge
[win-x86-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9464

[osx-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9470/badge
[osx-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9470

[linux-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9469/badge
[linux-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9469

[linux-arm-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9467/badge
[linux-arm-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9467

[linux-arm64-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9468/badge
[linux-arm64-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9468

[linuxnative-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9465/badge
[linuxnative-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9465

[rhel6-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9471/badge
[rhel6-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9471

[linux-musl-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/9466/badge
[linux-musl-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=9466

Installers and Binaries
-----------------------

You can download the .NET Core SDK as either an installer (MSI, PKG) or a zip (zip, tar.gz). The .NET Core SDK contains both the .NET Core runtime and CLI tools.

To download the .NET Core runtime **without** the SDK, visit https://github.com/dotnet/core-setup#daily-builds.

> **Note:** Be aware that the following installers are the **latest bits**. If you
> want to install the latest released versions, check out the [preceding section](#looking-for-v1-of-the-net-core-tooling).
> In order to be able to restore these pre-release packages, you may need to add a NuGet feed as noted in the table below. Other feeds may also be necessary depending on what kind of project you are working with.

|   Platform   |   Master<br>(3.0.x Runtime)   |   Release/2.1.2XX<br>(2.0.x Runtime)   |   Release/2.1.401<br>(2.1.3 Runtime)   |   Release/2.1.4XX<br>(2.1.2 Runtime)  |   Release/2.2.1XX<br>(2.2.x Runtime)   |
|---------|:----------:|:----------:|:----------:|:----------:|:----------:|
| **Windows (x64)** | [![][win-x64-badge-master]][win-x64-version-master]<br>[Installer][win-x64-installer-master] - [Checksum][win-x64-installer-checksum-master]<br>[zip][win-x64-zip-master] - [Checksum][win-x64-zip-checksum-master] | [![][win-x64-badge-2.1.2XX]][win-x64-version-2.1.2XX]<br>[Installer][win-x64-installer-2.1.2XX] - [Checksum][win-x64-installer-checksum-2.1.2XX]<br>[zip][win-x64-zip-2.1.2XX] - [Checksum][win-x64-zip-checksum-2.1.2XX] | [![][win-x64-badge-2.1.401]][win-x64-version-2.1.401]<br>[Installer][win-x64-installer-2.1.401] - [Checksum][win-x64-installer-checksum-2.1.401]<br>[zip][win-x64-zip-2.1.401] - [Checksum][win-x64-zip-checksum-2.1.401] | [![][win-x64-badge-2.1.4XX]][win-x64-version-2.1.4XX]<br>[Installer][win-x64-installer-2.1.4XX] - [Checksum][win-x64-installer-checksum-2.1.4XX]<br>[zip][win-x64-zip-2.1.4XX] - [Checksum][win-x64-zip-checksum-2.1.4XX] | [![][win-x64-badge-2.2.1XX]][win-x64-version-2.2.1XX]<br>[Installer][win-x64-installer-2.2.1XX] - [Checksum][win-x64-installer-checksum-2.2.1XX]<br>[zip][win-x64-zip-2.2.1XX] - [Checksum][win-x64-zip-checksum-2.2.1XX] |
| **Windows x86** | [![][win-x86-badge-master]][win-x86-version-master]<br>[Installer][win-x86-installer-master] - [Checksum][win-x86-installer-checksum-master]<br>[zip][win-x86-zip-master] - [Checksum][win-x86-zip-checksum-master] | [![][win-x86-badge-2.1.2XX]][win-x86-version-2.1.2XX]<br>[Installer][win-x86-installer-2.1.2XX] - [Checksum][win-x86-installer-checksum-2.1.2XX]<br>[zip][win-x86-zip-2.1.2XX] - [Checksum][win-x86-zip-checksum-2.1.2XX] | [![][win-x86-badge-2.1.401]][win-x86-version-2.1.401]<br>[Installer][win-x86-installer-2.1.401] - [Checksum][win-x86-installer-checksum-2.1.401]<br>[zip][win-x86-zip-2.1.401] - [Checksum][win-x86-zip-checksum-2.1.401] | [![][win-x86-badge-2.1.4XX]][win-x86-version-2.1.4XX]<br>[Installer][win-x86-installer-2.1.4XX] - [Checksum][win-x86-installer-checksum-2.1.4XX]<br>[zip][win-x86-zip-2.1.4XX] - [Checksum][win-x86-zip-checksum-2.1.4XX] | [![][win-x86-badge-2.2.1XX]][win-x86-version-2.2.1XX]<br>[Installer][win-x86-installer-2.2.1XX] - [Checksum][win-x86-installer-checksum-2.2.1XX]<br>[zip][win-x86-zip-2.2.1XX] - [Checksum][win-x86-zip-checksum-2.2.1XX] |
| **macOS** | [![][osx-badge-master]][osx-version-master]<br>[Installer][osx-installer-master] - [Checksum][osx-installer-checksum-master]<br>[tar.gz][osx-targz-master] - [Checksum][osx-targz-checksum-master] | [![][osx-badge-2.1.2XX]][osx-version-2.1.2XX]<br>[Installer][osx-installer-2.1.2XX] - [Checksum][osx-installer-checksum-2.1.2XX]<br>[tar.gz][osx-targz-2.1.2XX] - [Checksum][osx-targz-checksum-2.1.2XX] | [![][osx-badge-2.1.401]][osx-version-2.1.401]<br>[Installer][osx-installer-2.1.401] - [Checksum][osx-installer-checksum-2.1.401]<br>[tar.gz][osx-targz-2.1.401] - [Checksum][osx-targz-checksum-2.1.401] | [![][osx-badge-2.1.4XX]][osx-version-2.1.4XX]<br>[Installer][osx-installer-2.1.4XX] - [Checksum][osx-installer-checksum-2.1.4XX]<br>[tar.gz][osx-targz-2.1.4XX] - [Checksum][osx-targz-checksum-2.1.4XX] | [![][osx-badge-2.2.1XX]][osx-version-2.2.1XX]<br>[Installer][osx-installer-2.2.1XX] - [Checksum][osx-installer-checksum-2.2.1XX]<br>[tar.gz][osx-targz-2.2.1XX] - [Checksum][osx-targz-checksum-2.2.1XX] |
| **Linux x64** | [![][linux-badge-master]][linux-version-master]<br>[DEB Installer][linux-DEB-installer-master] - [Checksum][linux-DEB-installer-checksum-master]<br>[RPM Installer][linux-RPM-installer-master] - [Checksum][linux-RPM-installer-checksum-master]<br>_see installer note below_<sup>1</sup><br>[tar.gz][linux-targz-master] - [Checksum][linux-targz-checksum-master] |  [![][linux-badge-2.1.2XX]][linux-version-2.1.2XX]<br>[tar.gz][linux-targz-2.1.2XX] - [Checksum][linux-targz-checksum-2.1.2XX]  | [![][linux-badge-2.1.401]][linux-version-2.1.401]<br>[DEB Installer][linux-DEB-installer-2.1.401] - [Checksum][linux-DEB-installer-checksum-2.1.401]<br>[RPM Installer][linux-RPM-installer-2.1.401] - [Checksum][linux-RPM-installer-checksum-2.1.401]<br>_see installer note below_<sup>1</sup><br>[tar.gz][linux-targz-2.1.401] - [Checksum][linux-targz-checksum-2.1.401] | [![][linux-badge-2.1.4XX]][linux-version-2.1.4XX]<br>[DEB Installer][linux-DEB-installer-2.1.4XX] - [Checksum][linux-DEB-installer-checksum-2.1.4XX]<br>[RPM Installer][linux-RPM-installer-2.1.4XX] - [Checksum][linux-RPM-installer-checksum-2.1.4XX]<br>_see installer note below_<sup>1</sup><br>[tar.gz][linux-targz-2.1.4XX] - [Checksum][linux-targz-checksum-2.1.4XX] | [![][linux-badge-2.2.1xx]][linux-version-2.2.1xx]<br>[DEB Installer][linux-DEB-installer-2.2.1XX] - [Checksum][linux-DEB-installer-checksum-2.2.1XX]<br>[RPM Installer][linux-RPM-installer-2.2.1XX] - [Checksum][linux-RPM-installer-checksum-2.2.1XX]<br>_see installer note below_<sup>1</sup><br>[tar.gz][linux-targz-2.2.1XX] - [Checksum][linux-targz-checksum-2.2.1XX] |
| **Linux arm** | [![][linux-arm-badge-master]][linux-arm-version-master]<br>[tar.gz][linux-arm-targz-master] - [Checksum][linux-arm-targz-checksum-master] | N/A | [![][linux-arm-badge-2.1.401]][linux-arm-version-2.1.401]<br>[tar.gz][linux-arm-targz-2.1.401] - [Checksum][linux-arm-targz-checksum-2.1.401] | [![][linux-arm-badge-2.1.4XX]][linux-arm-version-2.1.4XX]<br>[tar.gz][linux-arm-targz-2.1.4XX] - [Checksum][linux-arm-targz-checksum-2.1.4XX] | [![][linux-arm-badge-2.2.1XX]][linux-arm-version-2.2.1XX]<br>[tar.gz][linux-arm-targz-2.2.1XX] - [Checksum][linux-arm-targz-checksum-2.2.1XX] |
| **Linux arm64** | [![][linux-arm64-badge-master]][linux-arm64-version-master]<br>[tar.gz][linux-arm64-targz-master] - [Checksum][linux-arm64-targz-checksum-master] | N/A | [![][linux-arm64-badge-2.1.401]][linux-arm64-version-2.1.401]<br>[tar.gz][linux-arm64-targz-2.1.401] - [Checksum][linux-arm64-targz-checksum-2.1.401] | [![][linux-arm64-badge-2.1.4XX]][linux-arm64-version-2.1.4XX]<br>[tar.gz][linux-arm64-targz-2.1.4XX] - [Checksum][linux-arm64-targz-checksum-2.1.4XX] | [![][linux-arm64-badge-2.2.1XX]][linux-arm64-version-2.2.1XX]<br>[tar.gz][linux-arm64-targz-2.2.1XX] - [Checksum][linux-arm64-targz-checksum-2.2.1XX] |
| **RHEL 6** | [![][rhel-6-badge-master]][rhel-6-version-master]<br>[tar.gz][rhel-6-targz-master] - [Checksum][rhel-6-targz-checksum-master] | N/A | [![][rhel-6-badge-2.1.401]][rhel-6-version-2.1.401]<br>[tar.gz][rhel-6-targz-2.1.401] - [Checksum][rhel-6-targz-checksum-2.1.401] | [![][rhel-6-badge-2.1.4XX]][rhel-6-version-2.1.4XX]<br>[tar.gz][rhel-6-targz-2.1.4XX] - [Checksum][rhel-6-targz-checksum-2.1.4XX] | [![][rhel-6-badge-2.2.1XX]][rhel-6-version-2.2.1XX]<br>[tar.gz][rhel-6-targz-2.2.1XX] - [Checksum][rhel-6-targz-checksum-2.2.1XX] |
| **Linux-musl** | [![][linux-musl-badge-master]][linux-musl-version-master]<br>[tar.gz][linux-musl-targz-master] - [Checksum][linux-musl-targz-checksum-master] | N/A | [![][linux-musl-badge-2.1.401]][linux-musl-version-2.1.401]<br>[tar.gz][linux-musl-targz-2.1.401] - [Checksum][linux-musl-targz-checksum-2.1.401] | [![][linux-musl-badge-2.1.4XX]][linux-musl-version-2.1.4XX]<br>[tar.gz][linux-musl-targz-2.1.4XX] - [Checksum][linux-musl-targz-checksum-2.1.4XX] | [![][linux-musl-badge-2.2.1XX]][linux-musl-version-2.2.1XX]<br>[tar.gz][linux-musl-targz-2.2.1XX] - [Checksum][linux-musl-targz-checksum-2.2.1XX] |
| **Package Feed** | [Feed Link][feed-location-master] | [Feed Link][feed-location-2.1.2XX] | [Feed Link][feed-location-2.1.401] | [Feed Link][feed-location-2.1.4XX] | [Feed Link][feed-location-2.2.1XX] |
| **Constituent Repo Shas** | **N/A** | **N/A** | [Git SHAs][sdk-shas-2.1.401] | **N/A** | [Git SHAs][sdk-shas-2.2.1XX] |

Latest Coherent Build<sup>2</sup>

|   Master   |   Release/2.1.2XX   |   Release/2.1.401   |   Release/2.1.4XX   |   Release/2.2.1XX   |
|:----------:|:----------:|:----------:|:----------:|:----------:|
| [![][coherent-version-badge-master]][coherent-version-master] | [![][coherent-version-badge-2.1.2XX]][coherent-version-2.1.2XX] | [![][coherent-version-badge-2.1.401]][coherent-version-2.1.401] | [![][coherent-version-badge-2.1.4XX]][coherent-version-2.1.4XX] | [![][coherent-version-badge-2.2.1XX]][coherent-version-2.2.1XX] |

Reference notes:
> **1**: *Our Debian packages are put together slightly differently than the other OS specific installers. Instead of combining everything, we have separate component packages that depend on each other. If you're installing these directly from the .deb files (via dpkg or similar), then you'll need to install the [corresponding Host, Host FX Resolver, and Shared Framework packages](https://github.com/dotnet/core-setup#daily-builds) before installing the Sdk package.*
> <br><br>**2**: *A 'coherent' build is defined as a build where the Runtime version matches between the CLI and Asp.NET.*

[win-x64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/win_x64_Release_version_badge.svg
[win-x64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version
[win-x64-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.exe
[win-x64-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.exe.sha
[win-x64-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.zip
[win-x64-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x64.zip.sha

[win-x64-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/win_x64_Release_version_badge.svg
[win-x64-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.version
[win-x64-installer-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-win-x64.exe
[win-x64-installer-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-win-x64.exe.sha
[win-x64-zip-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-win-x64.zip
[win-x64-zip-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-win-x64.zip.sha

[win-x64-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/win_x64_Release_version_badge.svg
[win-x64-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.version
[win-x64-installer-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-win-x64.exe
[win-x64-installer-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-win-x64.exe.sha
[win-x64-zip-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-win-x64.zip
[win-x64-zip-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-win-x64.zip.sha

[win-x64-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/win_x64_Release_version_badge.svg
[win-x64-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.version
[win-x64-installer-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-win-x64.exe
[win-x64-installer-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-win-x64.exe.sha
[win-x64-zip-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-win-x64.zip
[win-x64-zip-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-win-x64.zip.sha

[win-x64-badge-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/win_x64_Release_version_badge.svg
[win-x64-version-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.version
[win-x64-installer-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-win-x64.exe
[win-x64-installer-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-win-x64.exe.sha
[win-x64-zip-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-win-x64.zip
[win-x64-zip-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-win-x64.zip.sha

[win-x86-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/win_x86_Release_version_badge.svg
[win-x86-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version
[win-x86-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x86.exe
[win-x86-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x86.exe.sha
[win-x86-zip-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x86.zip
[win-x86-zip-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-win-x86.zip.sha

[win-x86-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/win_x86_Release_version_badge.svg
[win-x86-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.version
[win-x86-installer-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-win-x86.exe
[win-x86-installer-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-win-x86.exe.sha
[win-x86-zip-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-win-x86.zip
[win-x86-zip-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-win-x86.zip.sha

[win-x86-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/win_x86_Release_version_badge.svg
[win-x86-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.version
[win-x86-installer-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-win-x86.exe
[win-x86-installer-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-win-x86.exe.sha
[win-x86-zip-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-win-x86.zip
[win-x86-zip-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-win-x86.zip.sha

[win-x86-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/win_x86_Release_version_badge.svg
[win-x86-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.version
[win-x86-installer-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-win-x86.exe
[win-x86-installer-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-win-x86.exe.sha
[win-x86-zip-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-win-x86.zip
[win-x86-zip-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-win-x86.zip.sha

[win-x86-badge-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/win_x86_Release_version_badge.svg
[win-x86-version-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.version
[win-x86-installer-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-win-x86.exe
[win-x86-installer-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-win-x86.exe.sha
[win-x86-zip-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-win-x86.zip
[win-x86-zip-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-win-x86.zip.sha

[osx-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/osx_x64_Release_version_badge.svg
[osx-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version
[osx-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-osx-x64.pkg
[osx-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-osx-x64.pkg.sha
[osx-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-osx-x64.tar.gz
[osx-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-osx-x64.tar.gz.sha

[osx-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/osx_x64_Release_version_badge.svg
[osx-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.version
[osx-installer-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-osx-x64.pkg
[osx-installer-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-osx-x64.pkg.sha
[osx-targz-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-osx-x64.tar.gz
[osx-targz-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-osx-x64.tar.gz.sha

[osx-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/osx_x64_Release_version_badge.svg
[osx-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.version
[osx-installer-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-osx-x64.pkg
[osx-installer-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-osx-x64.pkg.sha
[osx-targz-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-osx-x64.tar.gz
[osx-targz-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-osx-x64.tar.gz.sha

[osx-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/osx_x64_Release_version_badge.svg
[osx-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.version
[osx-installer-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-osx-x64.pkg
[osx-installer-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-osx-x64.pkg.sha
[osx-targz-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-osx-x64.tar.gz
[osx-targz-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-osx-x64.tar.gz.sha

[osx-badge-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/osx_x64_Release_version_badge.svg
[osx-version-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.version
[osx-installer-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-osx-x64.pkg
[osx-installer-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-osx-x64.pkg.sha
[osx-targz-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-osx-x64.tar.gz
[osx-targz-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-osx-x64.tar.gz.sha

[linux-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/linux_x64_Release_version_badge.svg
[linux-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version
[linux-DEB-installer-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-x64.deb
[linux-DEB-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-x64.deb.sha
[linux-RPM-installer-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-x64.deb.sha
[linux-RPM-installer-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-x64.rpm.sha
[linux-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-x64.tar.gz
[linux-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-x64.tar.gz.sha

[linux-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/linux_x64_Release_version_badge.svg
[linux-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.version
[linux-targz-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-linux-x64.tar.gz
[linux-targz-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-linux-x64.tar.gz.sha

[linux-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/linux_x64_Release_version_badge.svg
[linux-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.version
[linux-DEB-installer-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-x64.deb
[linux-DEB-installer-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-x64.deb.sha
[linux-RPM-installer-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-x64.rpm
[linux-RPM-installer-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-x64.rpm.sha
[linux-targz-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-linux-x64.tar.gz
[linux-targz-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-linux-x64.tar.gz.sha

[linux-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/linux_x64_Release_version_badge.svg
[linux-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.version
[linux-DEB-installer-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-x64.deb
[linux-DEB-installer-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-x64.deb.sha
[linux-RPM-installer-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-x64.deb.sha
[linux-RPM-installer-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-x64.rpm.sha
[linux-targz-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-x64.tar.gz
[linux-targz-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-x64.tar.gz.sha

[linux-badge-2.2.1xx]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/linux_x64_Release_version_badge.svg
[linux-version-2.2.1xx]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.version
[linux-DEB-installer-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-x64.deb
[linux-DEB-installer-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-x64.deb.sha
[linux-RPM-installer-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-x64.deb.sha
[linux-RPM-installer-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-x64.rpm.sha
[linux-targz-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-linux-x64.tar.gz
[linux-targz-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-linux-x64.tar.gz.sha

[linux-arm-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/linux_arm_Release_version_badge.svg
[linux-arm-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version
[linux-arm-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-arm.tar.gz.sha

[linux-arm-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/linux_arm_Release_version_badge.svg
[linux-arm-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.version
[linux-arm-targz-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-linux-arm.tar.gz.sha

[linux-arm-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/linux_arm_Release_version_badge.svg
[linux-arm-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.version
[linux-arm-targz-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-linux-arm.tar.gz.sha

[linux-arm-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/linux_arm_Release_version_badge.svg
[linux-arm-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.version
[linux-arm-targz-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-arm.tar.gz.sha

[linux-arm-badge-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/linux_arm_Release_version_badge.svg
[linux-arm-version-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.version
[linux-arm-targz-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-linux-arm.tar.gz
[linux-arm-targz-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-linux-arm.tar.gz.sha

[linux-arm64-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/linux_arm64_Release_version_badge.svg
[linux-arm64-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version
[linux-arm64-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-arm64.tar.gz.sha

[linux-arm64-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/linux_arm64_Release_version_badge.svg
[linux-arm64-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.version
[linux-arm64-targz-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-linux-arm64.tar.gz.sha

[linux-arm64-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/linux_arm64_Release_version_badge.svg
[linux-arm64-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.version
[linux-arm64-targz-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-linux-arm64.tar.gz.sha

[linux-arm64-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/linux_arm64_Release_version_badge.svg
[linux-arm64-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.version
[linux-arm64-targz-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-arm64.tar.gz.sha

[linux-arm64-badge-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/linux_arm64_Release_version_badge.svg
[linux-arm64-version-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.version
[linux-arm64-targz-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-linux-arm64.tar.gz
[linux-arm64-targz-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-linux-arm64.tar.gz.sha

[rhel-6-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/rhel.6_x64_Release_version_badge.svg
[rhel-6-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version
[rhel-6-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-rhel.6-x64.tar.gz
[rhel-6-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-rhel.6-x64.tar.gz.sha

[rhel-6-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/rhel.6_x64_Release_version_badge.svg
[rhel-6-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.version
[rhel-6-targz-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-rhel.6-x64.tar.gz
[rhel-6-targz-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-rhel.6-x64.tar.gz.sha

[rhel-6-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/rhel.6_x64_Release_version_badge.svg
[rhel-6-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.version
[rhel-6-targz-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-rhel.6-x64.tar.gz
[rhel-6-targz-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-rhel.6-x64.tar.gz.sha

[rhel-6-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/rhel.6_x64_Release_version_badge.svg
[rhel-6-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.version
[rhel-6-targz-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-rhel.6-x64.tar.gz
[rhel-6-targz-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-rhel.6-x64.tar.gz.sha

[rhel-6-badge-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/rhel.6_x64_Release_version_badge.svg
[rhel-6-version-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.version
[rhel-6-targz-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-rhel.6-x64.tar.gz
[rhel-6-targz-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-rhel.6-x64.tar.gz.sha

[linux-musl-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/linux_musl_x64_Release_version_badge.svg
[linux-musl-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.version
[linux-musl-targz-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-musl-x64.tar.gz
[linux-musl-targz-checksum-master]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-musl-x64.tar.gz.sha

[linux-musl-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/linux_musl_Release_x64_version_badge.svg
[linux-musl-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.version
[linux-musl-targz-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-linux-musl-x64.tar.gz
[linux-musl-targz-checksum-2.1.2XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/dotnet-sdk-latest-linux-musl-x64.tar.gz.sha

[linux-musl-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/linux_musl_x64_Release_version_badge.svg
[linux-musl-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.version
[linux-musl-targz-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-linux-musl-x64.tar.gz
[linux-musl-targz-checksum-2.1.401]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.401/dotnet-sdk-latest-linux-musl-x64.tar.gz.sha

[linux-musl-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/linux_musl_x64_Release_version_badge.svg
[linux-musl-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.version
[linux-musl-targz-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-musl-x64.tar.gz
[linux-musl-targz-checksum-2.1.4XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-musl-x64.tar.gz.sha

[linux-musl-badge-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/linux_musl_x64_Release_version_badge.svg
[linux-musl-version-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.version
[linux-musl-targz-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-linux-musl-x64.tar.gz
[linux-musl-targz-checksum-2.2.1XX]: https://dotnetclichecksums.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/dotnet-sdk-latest-linux-musl-x64.tar.gz.sha

[sdk-shas-2.1.401]: https://github.com/dotnet/versions/tree/master/build-info/dotnet/product/cli/release/2.1#built-repositories

[sdk-shas-2.2.1XX]: https://github.com/dotnet/versions/tree/master/build-info/dotnet/product/cli/release/2.2#built-repositories

[feed-location-master]: https://dotnet.myget.org/F/dotnet-core/api/v3/index.json

[feed-location-2.1.2XX]: https://dotnet.myget.org/F/dotnet-core/api/v3/index.json

[feed-location-2.1.401]: https://dotnet.myget.org/F/dotnet-core/api/v3/index.json

[feed-location-2.1.4XX]: https://dotnet.myget.org/F/dotnet-core/api/v3/index.json

[feed-location-2.2.1XX]: https://dotnet.myget.org/F/dotnet-core/api/v3/index.json

[sdk-shas-2.2.1XX]: https://github.com/dotnet/versions/tree/master/build-info/dotnet/product/cli/release/2.2#built-repositories

[sdk-shas-2.2.1XX]: https://github.com/dotnet/versions/tree/master/build-info/dotnet/product/cli/release/2.2#built-repositories

[coherent-version-badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/win_x64_Release_coherent_badge.svg
[coherent-version-master]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/latest.coherent.version
[coherent-version-badge-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/win_x64_Release_coherent_badge.svg
[coherent-version-2.1.2XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.2xx/latest.coherent.version
[coherent-version-badge-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/win_x64_Release_coherent_badge.svg
[coherent-version-2.1.401]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.401/latest.coherent.version
[coherent-version-badge-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/win_x64_Release_coherent_badge.svg
[coherent-version-2.1.4XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/latest.coherent.version
[coherent-version-badge-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/win_x64_Release_coherent_badge.svg
[coherent-version-2.2.1XX]: https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.2.1xx/latest.coherent.version

Questions & Comments
--------------------

For all feedback, use the Issues on the [.NET CLI](https://github.com/dotnet/cli) repository.

License
-------

By downloading the .zip you are agreeing to the terms in the project [EULA](https://aka.ms/dotnet-core-eula).

