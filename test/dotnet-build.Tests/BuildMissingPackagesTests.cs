// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class BuildMissingPackagesTests : TestBase
    {
        [Fact]
        public void MissingPackageDuringBuildCauseFailure()
        {
            // Arrange
            TestInstance instance = TestAssetsManager.CreateTestInstance("PortableTests");
            var testProject = Path.Combine(instance.TestRoot, "StandaloneApp", "project.json");
            var workingDirectory = Path.GetDirectoryName(testProject);
            var testNuGetCache = Path.Combine(instance.Path, "packages");
            var oldLocation = Path.Combine(testNuGetCache, "system.console");
            var newLocation = Path.Combine(testNuGetCache, "system.console.different");

            var restoreCommand = new RestoreCommand();

            restoreCommand.WorkingDirectory = workingDirectory;
            restoreCommand.Environment["NUGET_PACKAGES"] = testNuGetCache;
            restoreCommand.Execute();

            // Delete all System.Console packages.
            foreach (var directory in Directory.EnumerateDirectories(testNuGetCache, "*system.console*"))
            {
                Directory.Delete(directory, true);
            }

            var buildCommand = new BuildCommand(testProject);

            buildCommand.WorkingDirectory = workingDirectory;
            buildCommand.Environment["NUGET_PACKAGES"] = testNuGetCache;

            // Act & Assert
            buildCommand
                .ExecuteWithCapturedOutput()
                .Should()
                .Fail()
                .And
                .HaveStdErrMatching("The dependency System.Console >= (?<Version>.+?) could not be resolved.");
        }
    }
}
