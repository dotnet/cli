// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class GivenDotnetBuildBuildsProjects : TestBase
    {
        [Fact]
        public void It_builds_projects_with_Unicode_characters_in_the_path_when_CWD_is_the_project_directory()
        {
            var testInstance = TestAssetsManager
                .CreateTestInstance("TestAppWithUnicodéPath")
                .WithLockFiles();

            var testProjectDirectory = testInstance.TestRoot;

            var buildCommand = new BuildCommand("");
            buildCommand.WorkingDirectory = testProjectDirectory;
            
            buildCommand.ExecuteWithCapturedOutput()
                .Should()
                .Pass();
        }

        [Fact]
        public void It_builds_projects_with_Unicode_characters_in_the_path_when_CWD_is_not_the_project_directory()
        {
            var testInstance = TestAssetsManager
                .CreateTestInstance("TestAppWithUnicodéPath")
                .WithLockFiles();

            var testProject = Path.Combine(testInstance.TestRoot, "project.json");

            new BuildCommand(testProject)
                .ExecuteWithCapturedOutput()
                .Should()
                .Pass();
        }
    }
}
