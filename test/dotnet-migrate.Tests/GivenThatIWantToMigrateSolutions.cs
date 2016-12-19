// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenThatIWantToMigrateSolutions : TestBase
    {
        [Fact]
        public void ItMigratesAndBuildsSln()
        {
            MigrateAndBuild("NonRestoredTestProjects", "PJTestAppWithSlnAndExistingXprojReferences");
        }

        [Fact]
        public void WhenDirectoryAlreadyContainsCsprojFileItMigratesAndBuildsSln()
        {
            MigrateAndBuild("NonRestoredTestProjects", "PJTestAppWithSlnAndExistingXprojReferencesAndUnrelatedCsproj");
        }

        private void MigrateAndBuild(string groupName, string projectName)
        {
            var projectDirectory = TestAssets
                .Get(groupName, projectName)
                .CreateInstance()
                .WithSourceFiles()
                .Root;

            var solutionRelPath = Path.Combine("TestApp", "TestApp.sln");

            new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"migrate \"{solutionRelPath}\"")
                .Should().Pass();

            new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"restore \"{Path.Combine("TestApp", "TestApp.csproj")}\"")
                .Should().Pass();

            new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"build \"{solutionRelPath}\"")
                .Should().Pass();

            SlnFile slnFile = SlnFile.Read(Path.Combine(projectDirectory.FullName, solutionRelPath));
            slnFile.Projects.Count.Should().Be(3);

            var slnProject = slnFile.Projects.Where((p) => p.Name == "TestApp").Single();
            slnProject.Id.Should().Be("{0138CB8F-4AA9-4029-A21E-C07C30F425BA}");
            slnProject.TypeGuid.Should().Be("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
            slnProject.FilePath.Should().Be("TestApp.csproj");

            slnProject = slnFile.Projects.Where((p) => p.Name == "TestLibrary").Single();
            slnProject.Id.Should().Be("{DC0B35D0-8A36-4B52-8A11-B86739F055D2}");
            slnProject.TypeGuid.Should().Be("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
            slnProject.FilePath.Should().Be(@"..\TestLibrary\TestLibrary.csproj");

            slnProject = slnFile.Projects.Where((p) => p.Name == "subdir").Single();
            slnProject.Id.Should().Be("{F8F96F4A-F10C-4C54-867C-A9EFF55494C8}");
            slnProject.TypeGuid.Should().Be("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
            slnProject.FilePath.Should().Be(@"src\subdir\subdir.csproj");
        }
    }
}
