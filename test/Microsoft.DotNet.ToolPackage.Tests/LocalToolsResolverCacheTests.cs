// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.DependencyModel.Tests;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using NuGet.ProjectModel;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class LocalToolsResolverCacheTests : TestBase
    {


        [Fact]
        public void GivenListOfCommandSettingsAndTargetFrameworkAndRidAndCurrentNugetCacheLocationItCanWriteAndRetrieve()
        {
            IFileSystem fileSystem = new FileSystemMockBuilder().UseCurrentSystemTemporaryDirectory().Build();
            DirectoryPath tempDirectory = new DirectoryPath(fileSystem.Directory.CreateTemporaryDirectory().DirectoryPath);
            DirectoryPath cacheDirectory = tempDirectory.WithSubDirectories("cacheDirectory");
            fileSystem.Directory.CreateDirectory(cacheDirectory.Value);
            int version = 1;

            LocalToolsResolverCache localToolsResolverCache =
                new LocalToolsResolverCache(fileSystem, cacheDirectory, version);
            IReadOnlyList<CommandSettings> listOfCommandSettings = new[]
            {
                new CommandSettings("tool1", "dotnet", tempDirectory.WithFile("too1.dll")),
                new CommandSettings("tool2", "dotnet", tempDirectory.WithFile("too1.dll"))
            };
            
            var targetFramework = new Targe
            localToolsResolverCache.Save(listOfCommandSettings, );
        }
    }
}


//given IReadOnlyList<CommandSettings> target frameowork and rid and current nuget cache location. it can write it down 
//
//give it package id, package version, command name, target frameowork and rid, it can give back commandsettings 
//
//give it package id, package version range, command name, target frameowork and rid, it can give back 
//
//different version of resolver cache is different. cannot resolve each other's cache 
