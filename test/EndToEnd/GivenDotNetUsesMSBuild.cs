﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tests.EndToEnd
{
    public class GivenDotNetUsesMSBuild : TestBase
    {
        public static void Main() 
        {
        } 

        [Fact]
        public void ItCanNewRestoreBuildRunCleanMSBuildProject()
        {
            using (DisposableDirectory directory = Temp.CreateDirectory())
            {
                string projectDirectory = directory.Path;

                new NewCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute("")
                    .Should().Pass();

                new RestoreCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute("/p:SkipInvalidConfigurations=true")
                    .Should().Pass();

                new BuildCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute()
                    .Should().Pass();

                //TODO: https://github.com/dotnet/sdk/issues/187 - remove framework from below.
                new RunCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .ExecuteWithCapturedOutput("--framework netcoreapp1.0")
                    .Should().Pass()
                         .And.HaveStdOutContaining("Hello World!");

                var binDirectory = new DirectoryInfo(projectDirectory).Sub("bin");
                binDirectory.Should().HaveFilesMatching("*.dll", SearchOption.AllDirectories);

                new CleanCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute()
                    .Should().Pass();

                binDirectory.Should().NotHaveFilesMatching("*.dll", SearchOption.AllDirectories);
            }
        }

        [Fact]
        public void ItCanRunToolsInACSProj()
        {
            var testInstance = TestAssets.Get("MSBuildTestApp")
                                         .CreateInstance()
                                         .WithSourceFiles()
                                         .WithRestoreFiles();
         
            var testProjectDirectory = testInstance.Root;

            new DotnetCommand()
                .WithWorkingDirectory(testInstance.Root)
                .ExecuteWithCapturedOutput("portable")
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello Portable World!");;
        }

        [Fact]
        public void ItCanRunAToolThatInvokesADependencyToolInACSProj()
        {
            var repoDirectoriesProvider = new RepoDirectoriesProvider();

            var testInstance = TestAssets.Get("MSBuildTestAppWithToolInDependencies")
                                         .CreateInstance()
                                         .WithSourceFiles()
                                         .WithRestoreFiles();

            var configuration = "Debug";

            var testProjectDirectory = testInstance.Root;

            new BuildCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute($"-c {configuration} ")
                .Should()
                .Pass();

            new DotnetCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .ExecuteWithCapturedOutput(
                    $"-v dependency-tool-invoker -c {configuration} -f netcoreapp1.0 portable")
                .Should().Pass()
                     .And.HaveStdOutContaining("Hello Portable World!");;
        }
    }
}
