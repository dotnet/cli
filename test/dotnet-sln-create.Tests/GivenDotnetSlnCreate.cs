// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Cli.Sln.Create.Tests
{
    public class GivenDotnetSlnCreate : TestBase
    {
        private const string slnFileContents = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
VisualStudioVersion = 15.0.26118.1
MinimumVisualStudioVersion = 15.0.26118.1
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|x64 = Debug|x64
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|x64 = Release|x64
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal";

        [Fact]
        public void WhenEmptyDirectoryIsUsedSlnFileIsCreated()
        {
            var directoryRoot = TestAssets.CreateTestDirectory()
                .FullName;

            var slnFile = $"{Path.GetFileName(directoryRoot)}.sln";

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(directoryRoot)
                .ExecuteWithCapturedOutput("sln create");

            cmd.Should().Pass();
            cmd.StdOut.Should().Be($"The template {Path.GetFileName(directoryRoot)}.sln created successfully. Please run \"dotnet restore\" to get started!");
            new DirectoryInfo(directoryRoot).Should().HaveFile(slnFile);
            File.ReadAllText(Path.Combine(directoryRoot, slnFile)).Should().BeVisuallyEquivalentTo(slnFileContents);
        }

        [Fact]
        public void WhenANameIsGivenTheSlnIsCreatedWithThatName()
        {
            var directoryRoot = TestAssets.CreateTestDirectory()
                .FullName;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(directoryRoot)
                .ExecuteWithCapturedOutput("sln create test.sln");

            cmd.Should().Pass();
            cmd.StdOut.Should().Be("The template test.sln created successfully. Please run \"dotnet restore\" to get started!");
            new DirectoryInfo(directoryRoot).Should().HaveFile("test.sln");
            File.ReadAllText(Path.Combine(directoryRoot, "test.sln")).Should().BeVisuallyEquivalentTo(slnFileContents);
        }

        [Fact]
        public void WhenSlnWithTheSameNameExistsItShouldFail()
        {
            var slnRoot = TestAssets.Get("TestAppWithEmptySln")
                .CreateInstance()
                .WithSourceFiles()
                .Root
                .FullName;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(slnRoot)
                .ExecuteWithCapturedOutput("sln create App.sln");
            
            cmd.Should().Fail();
            cmd.StdErr.Should().Be($"{slnRoot} already has App.sln.");
        }

        [Fact]
        public void WhenSlnIsCreatedAddingAProjectPasses()
        {
            var slnRoot = TestAssets.Get("AppsWithNoSln")
                .CreateInstance()
                .WithSourceFiles()
                .Root
                .FullName;

            var projectToAdd = Path.Combine("App", "App.csproj");

            var cmdCreate = new DotnetCommand()
                .WithWorkingDirectory(slnRoot)
                .ExecuteWithCapturedOutput("sln create");

            var cmdAdd = new DotnetCommand()
                .WithWorkingDirectory(slnRoot)
                .ExecuteWithCapturedOutput($"sln add {projectToAdd}");

            cmdCreate.Should().Pass();
            cmdAdd.Should().Pass();
        }

        [Fact]
        public void WhenNameSpecifiedWithNoExtensionSlnExtensionIsAdded()
        {
            var directoryRoot = TestAssets.CreateTestDirectory()
                .FullName;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(directoryRoot)
                .ExecuteWithCapturedOutput("sln create NoExtension");

            cmd.Should().Pass();
            cmd.StdOut.Should().Be("The template NoExtension.sln created successfully. Please run \"dotnet restore\" to get started!");
            new DirectoryInfo(directoryRoot).Should().HaveFile("NoExtension.sln");
        }
    }
}