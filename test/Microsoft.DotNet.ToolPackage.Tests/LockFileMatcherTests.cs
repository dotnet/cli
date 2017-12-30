// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Tools.Install.Tool;
using Xunit;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class LockFileMatcherTests : TestBase
    {

        [Fact]
        public void MatchesEntryPointTests()
        {
            LockFileMatcher
                .MatchesFile("tools/netcoreapp1.1/any/tool.dll", "tool.dll")
                .Should().BeTrue();

            LockFileMatcher
                .MatchesFile(@"tools\netcoreapp1.1\any\subDirectory\tool.dll", "subDirectory/tool.dll")
                .Should().BeTrue();

            LockFileMatcher
                .MatchesFile("tools/netcoreapp1.1/win-x64/tool.dll", "tool.dll")
                .Should().BeTrue();

            LockFileMatcher
                .MatchesFile("tools/netcoreapp1.1/any/subDirectory/tool.dll", "subDirectory/tool.dll")
                .Should().BeTrue();

            LockFileMatcher
                .MatchesFile("libs/netcoreapp1.1/any/tool.dll", "tool.dll")
                .Should().BeFalse();

            LockFileMatcher
                .MatchesFile("tools/netcoreapp1.1/any/subDirectory/tool.dll", "tool.dll")
                .Should().BeFalse();

            LockFileMatcher
                .MatchesFile(
                    "tools/netcoreapp1.1/any/subDirectory/tool.dll",
                    "subDirectory/subDirectory/subDirectory/subDirectory/subDirectory/tool.dll")
                .Should().BeFalse();
        }
    }
}
