# .NET Command Line Interface

[![.NET Slack Status](https://aspnetcoreslack.herokuapp.com/badge.svg?2)](http://tattoocoder.com/aspnet-slack-sign-up/) [![Join the chat at https://gitter.im/dotnet/cli](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/cli?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
​
This repo contains the source code for cross-platform [.NET Core](http://github.com/dotnet/core) command line toolchain. It contains the implementation of each command, the native packages for various supported platforms as well as documentation.
​
Looking for V1 of the .NET Core tooling?
----------------------------------------
​
If you are looking for the v1.0.1 release of the .NET Core tools (CLI, MSBuild and the new csproj), head over to https://dot.net/core and download!
​
> **Note:** the master branch of the CLI repo is based on the upcoming v2 of .NET Core and is considered pre-release. For production-level usage, please use the
> v1 of the tools.
​
Found an issue?
---------------
You can consult the [Documents Index](Documentation/README.md) to find out the current issues and to see the workarounds.
​
If you don't find your issue, please file one! However, given that this is a very high-frequency repo, we've setup some [basic guidelines](Documentation/project-docs/issue-filing-guide.md) to help you. Please consult those first.
​
This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. For more information, see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).
​
Build Status
------------
​
|Ubuntu 14.04 / Linux Mint 17 |Ubuntu 16.04 | Ubuntu 16.10 |Debian 8 |Windows x64 |Windows x86 |macOS |CentOS 7.1 / Oracle Linux 7.1 |RHEL 7.2 | Linux x64 |
|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|:------:|
|[![][ubuntu-14.04-build-badge]][ubuntu-14.04-build]|[![][ubuntu-16.04-build-badge]][ubuntu-16.04-build]|[![][ubuntu-16.10-build-badge]][ubuntu-16.10-build]|[![][debian-8-build-badge]][debian-8-build]|[![][win-x64-build-badge]][win-x64-build]|[![][win-x86-build-badge]][win-x86-build]|[![][osx-build-badge]][osx-build]|[![][centos-build-badge]][centos-build]|[![][rhel-build-badge]][rhel-build]|[![][linux-build-badge]][linux-build]|
​
[win-x64-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/5449/badge
[win-x64-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=5449
​
[win-x86-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/5450/badge
[win-x86-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=5450
​
[ubuntu-14.04-build-badge]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/5447/badge
[ubuntu-14.04-build]: https://devdiv.visualstudio.com/DevDiv/_build?_a=completed&definitionId=5447
