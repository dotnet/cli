// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Migration.Tests
{
    public class GivenThatIWantToMigrateSolutions : TestBase
    {
        [Fact]
        public void ItMigratesSln()
        {
            var projectDirectory = TestAssets
                .Get("NonRestoredTestProjects", "PJTestAppWithSlnAndExistingXprojReferences")
                .CreateInstance()
                .WithSourceFiles()
                .Root;

            new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"migrate \"{Path.Combine("TestApp", "TestApp.sln")}\"")
                .Should().Pass();

            new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"restore \"{Path.Combine("TestApp", "TestApp.csproj")}\"")
                .Should().Pass();

            new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .Execute($"build \"{Path.Combine("TestApp", "TestApp.sln")}\"")
                .Should().Pass();
        }
    }
}
