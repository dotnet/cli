// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;
using System.Linq;

namespace Microsoft.DotNet.Restore.Tests
{
    public class GivenThatIWantToRestoreApp : TestBase
    {
        [Fact]
        public void ItRestoresAppToSpecificDirectory()
        {
            var rootPath = TestAssets.CreateTestDirectory();

            var dir = "pkgs";
            var packagesDirectory = rootPath.GetDirectory(dir);

            new NewCommand()
                .WithWorkingDirectory(rootPath)
                .Execute()
                .Should().Pass();

            var args = $"--packages \"{dir}\"";
            new RestoreCommand()
                 .WithWorkingDirectory(rootPath)
                 .ExecuteWithCapturedOutput(args)
                 .Should().Pass()
                 .And.NotHaveStdErr();

            packagesDirectory.Should().Exist();
            packagesDirectory.EnumerateFiles("*.dll", SearchOption.AllDirectories).Count()
                .Should().BeGreaterThan(0);
        }

        [Fact]
        public void ItRestoresLibToSpecificDirectory()
        {
            var rootPath = TestAssets.CreateTestDirectory();

            var dir = "pkgs";
            var packagesDirectory = rootPath.GetDirectory(dir);

            new NewCommand()
                .WithWorkingDirectory(rootPath)
                .Execute("-t lib")
                .Should()
                .Pass();

            var args = $"--packages \"{dir}\"";
            new RestoreCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput(args)
                .Should()
                .Pass()
                .And.NotHaveStdErr();

            packagesDirectory.Should().Exist();
            packagesDirectory.EnumerateFiles("*.dll", SearchOption.AllDirectories).Count()
                .Should().BeGreaterThan(0);
        }

        [Fact]
        public void ItRestoresTestAppToSpecificDirectory()
        {
            var rootPath = TestAssets.Get("VSTestDotNetCore")
                .CreateInstance()
                .WithSourceFiles()
                .Root;

            var dir = "pkgs";
            var packagesDirectory = rootPath.GetDirectory(dir);

            var args = $"--packages \"{dir}\"";
            new RestoreCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput(args)
                .Should().Pass()
                .And.NotHaveStdErr();

            packagesDirectory.Should().Exist();
            packagesDirectory.EnumerateFiles("*.dll", SearchOption.AllDirectories).Count()
                .Should().BeGreaterThan(0);
        }
    }
}
