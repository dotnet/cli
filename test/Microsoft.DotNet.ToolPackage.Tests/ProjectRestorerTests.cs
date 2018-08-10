// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Tools.Tool.Install;
using Microsoft.Extensions.EnvironmentAbstractions;
using Xunit;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class ProjectRestorerTests : TestBase
    {
        [Fact]
        void ToRestoreArgumentsNugetConfigTests()
        {
            const string configPath = "c:\\nuget.config";
            var packageLocation = new PackageLocation(nugetConfig: new FilePath(configPath));
            System.Collections.Generic.List<string> args = ProjectRestorer.ToRestoreArguments(packageLocation);
            args.Insert(0, "restore");
            args.Insert(0, "dotnet");

            AppliedOption appliedCommand = Parser.Instance.Parse(args.ToArray()).AppliedCommand();

            appliedCommand["--configfile"].Arguments.Single()
                .Should()
                .Be(configPath);
        }
    }
}
