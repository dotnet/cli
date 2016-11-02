// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Tools.Test.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.New.Tests
{
    public class GivenThatIWantANewCSAppWithSpecifiedType : TestBase
    {
        [Theory]
        [InlineData("Console", "")]
        [InlineData("Lib", "")]
        [InlineData("Web", "-s https://dotnet.myget.org/F/dotnet-web/api/v3/index.json")]
        [InlineData("Mstest", "")]
        [InlineData("XUnittest", "")]
        public void When_dotnet_build_is_invoked_then_project_restores_and_builds_without_warnings(
            string projectType,
            string restoreArgs)
        {
            var rootPath = TestAssetsManager.CreateTestDirectory().Path;

            new TestCommand("dotnet") { WorkingDirectory = rootPath }
                .Execute($"new --type {projectType}")
                .Should().Pass();

            new TestCommand("dotnet") { WorkingDirectory = rootPath }
                .Execute($"restore {restoreArgs} -- /p:SkipInvalidConfigurations=true")
                .Should().Pass();

            var buildResult = new TestCommand("dotnet")
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput("build")
                .Should().Pass()
                .And.NotHaveStdErr();
        }
    }
}
