// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class BuildMalformedProjectsTests : TestBase
    {
        [Fact]
        public void FailToMakeRunnableAFullFrameworkAppWithDependencyOnPackageWithEmptyDllInLib()
        {
            var projectName = "AppWithDependencyOnMalformedPackage";
            var testInstance = TestAssetsManager
                .CreateTestInstance(projectName)
                .WithLockFiles();

            var buildCommand = new BuildCommand(testInstance.TestRoot);
            
            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().Fail();

            result.StdErr.Should().MatchRegex("Could not read assembly info for.*BadDll");
            result.StdErr.Should().MatchRegex($"Failed to make the following project runnable:.*{projectName}");
        }
    }
}
