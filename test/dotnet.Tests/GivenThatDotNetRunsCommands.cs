﻿// Copyright (c) .NET Foundation and contributors. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tests
{
    public class GivenThatDotNetRunsCommands : TestBase
    {
        [Fact]
        public void UnresolvedPlatformReferencesFailAsExpected()
        {
            var testInstance = TestAssets.Get("NonRestoredTestProjects", "TestProjectWithUnresolvedPlatformDependency")
                            .CreateInstance()
                            .WithSourceFiles()
                            .Root.FullName;

            new RestoreCommand()
                .WithWorkingDirectory(testInstance)
                .ExecuteWithCapturedOutput("/p:SkipInvalidConfigurations=true")
                .Should()
                .Fail();

            new DotnetCommand()
                .WithWorkingDirectory(testInstance)
                .ExecuteWithCapturedOutput("crash")
                .Should().Fail()
                     .And.HaveStdErrContaining(string.Format(LocalizableStrings.NoExecutableFoundMatchingCommand, "dotnet-crash"));
        }
    }
}
