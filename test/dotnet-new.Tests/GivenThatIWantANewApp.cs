﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.New.Tests
{
    public class GivenThatIWantANewApp : NewTestBase
    {
        [Fact]
        public void When_dotnet_new_is_invoked_mupliple_times_it_should_fail()
        {
            var rootPath = TestAssetsManager.CreateTestDirectory().Path;

            new TestCommand("dotnet") { WorkingDirectory = rootPath }
                .Execute($"new console");

            DateTime expectedState = Directory.GetLastWriteTime(rootPath);

            var result = new TestCommand("dotnet") { WorkingDirectory = rootPath }
                .ExecuteWithCapturedOutput($"new console");

            DateTime actualState = Directory.GetLastWriteTime(rootPath);

            Assert.Equal(expectedState, actualState);

            result.Should().Fail();
        }

        [Fact]
        public void RestoreDoesNotUseAnyCliProducedPackagesOnItsTemplates()
        {
            string[] cSharpTemplates = new[] { "console", "classlib", "mstest", "xunit", "web", "mvc", "webapi" };

            var rootPath = TestAssetsManager.CreateTestDirectory().Path;
            var packagesDirectory = Path.Combine(rootPath, "packages");

            foreach (string cSharpTemplate in cSharpTemplates)
            {
                var projectFolder = Path.Combine(rootPath, cSharpTemplate + "1");
                Directory.CreateDirectory(projectFolder);
                CreateAndRestoreNewProject(cSharpTemplate, projectFolder, packagesDirectory);
            }

            Directory.EnumerateFiles(packagesDirectory, $"*.nupkg", SearchOption.AllDirectories)
                .Should().NotContain(p => p.Contains("Microsoft.DotNet.Cli.Utils"));
        }

        private void CreateAndRestoreNewProject(
            string projectType,
            string projectFolder,
            string packagesDirectory)
        {
            new TestCommand("dotnet") { WorkingDirectory = projectFolder }
                .Execute($"new {projectType}")
                .Should().Pass();

            new RestoreCommand()
                .WithWorkingDirectory(projectFolder)
                .Execute($"--packages {packagesDirectory} /p:SkipInvalidConfigurations=true")
                .Should().Pass();
        }
    }
}
