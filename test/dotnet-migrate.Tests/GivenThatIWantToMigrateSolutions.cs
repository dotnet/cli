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
                .Get("TestAppWithSlnAndExistingXprojReferences")
                .CreateInstance()
                .WithSourceFiles()
                .Root
                .FullName;

            var cmd = new DotnetCommand()
                .WithWorkingDirectory(projectDirectory)
                .ExecuteWithCapturedOutput($"migrate \"{Path.Combine("TestApp", "TestApp.sln")}\"");
            cmd.Should().Pass();
        }
    }
}
