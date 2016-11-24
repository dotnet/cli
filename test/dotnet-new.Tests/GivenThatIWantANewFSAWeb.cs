// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.New.Tests
{
    public class GivenThatIWantANewFSWeb : TestBase
    {
        [Fact]
        public void When_dotnet_build_is_invoked_Then_web_builds_without_warnings_fs()
        {
            var rootPath = TestAssetsManager.CreateTestDirectory().Path;

            new TestCommand("dotnet") { WorkingDirectory = rootPath }
                .Execute("new --lang f# --type web");

            new TestCommand("dotnet") { WorkingDirectory = rootPath }
                .Execute("restore /p:SkipInvalidConfigurations=true");

            var buildResult = new TestCommand("dotnet") { WorkingDirectory = rootPath }
                .ExecuteWithCapturedOutput("build");
            
            buildResult.Should().Pass()
                       .And.NotHaveStdErr();
        }
    }
}
