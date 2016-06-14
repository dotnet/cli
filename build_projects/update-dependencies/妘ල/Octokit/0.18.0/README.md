# Octokit - GitHub API Client Library for .NET 

[![Build status](https://ci.appveyor.com/api/projects/status/cego2g42yw26th26/branch/master?svg=true)](https://ci.appveyor.com/project/github-windows/octokit-net/branch/master) [![Build Status]( https://travis-ci.org/octokit/octokit.net.svg)]( https://travis-ci.org/octokit/octokit.net) [![Join the chat at https://gitter.im/octokit/octokit.net](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/octokit/octokit.net?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

![logo](octokit-dotnet_2.png)

Octokit is a client library targeting .NET 4.5 and above that provides an easy
way to interact with the [GitHub API](http://developer.github.com/v3/).

## Usage examples

Get public info on a specific user.

```c#
var github = new GitHubClient(new ProductHeaderValue("MyAmazingApp"));
var user = await github.User.Get("half-ogre");
Console.WriteLine(user.Followers + " folks love the half ogre!");
```

## Supported Platforms

* .NET 4.5 (Desktop / Server)
* Xamarin.iOS / Xamarin.Android / Xamarin.Mac
* Mono 3.x
* Windows 8 / 8.1 Store Apps

## Getting Started

Octokit is available on NuGet.

```
Install-Package Octokit
```
or an IObservable based GitHub API client library for .NET using Reactive Extensions

```
Install-Package Octokit.Reactive
```
### Beta packages ###
Unstable NuGet packages that track the master branch of this repository are available at
[https://ci.appveyor.com/nuget/octokit-net](https://ci.appveyor.com/nuget/octokit-net)

In Xamarin Studio you can find this option under the project's context menu: **Add | Add Packages...***.

## Documentation

Documentation is available at http://octokitnet.readthedocs.org/en/latest/.

## Build

Octokit is a single assembly designed to be easy to deploy anywhere. If you
prefer to compile it yourself, you’ll need:

* Visual Studio 2015 or Xamarin Studio
* Windows 8.1 or higher to build and test the WinRT projects

To clone it locally click the "Clone in Desktop" button above or run the 
following git commands.

```
git clone git@github.com:octokit/Octokit.net.git Octokit
cd Octokit
.\build.cmd
```

## Contribute

Visit the [Contributor Guidelines](https://github.com/octokit/octokit.net/blob/master/CONTRIBUTING.md)
for more details.

## Problems?

Octokit is 100% certified to be bug free. If you find an issue with our
certification, please visit the [issue tracker](https://github.com/octokit/octokit.net/issues)
and report the issue.

Please be kind and search to see if the issue is already logged before creating
a new one. If you're pressed for time, log it anyways.

When creating an issue, clearly explain

* What you were trying to do.
* What you expected to happen.
* What actually happened.
* Steps to reproduce the problem.

Also include any other information you think is relevant to reproduce the
problem.

## Related Projects

 - [ScriptCs.OctoKit](https://github.com/alfhenrik/ScriptCs.OctoKit) - a [script pack](https://github.com/scriptcs/scriptcs/wiki/Script-Packs) to use Octokit in scriptcs 
 - [ScriptCs.OctokitLibrary](https://github.com/ryanrousseau/ScriptCs.OctokitLibrary) - a [script library](https://github.com/scriptcs/scriptcs/wiki/Script-Libraries) to use Octokit in scriptcs

## Copyright and License

Copyright 2013 GitHub, Inc.

Licensed under the [MIT License](https://github.com/octokit/octokit.net/blob/master/LICENSE.txt)
