// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Tools.Common;
using Xunit;
using Xunit.Abstractions;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tests.ParserTests
{
    public class InstallToolParserTests
    {
        private readonly ITestOutputHelper output;

        public InstallToolParserTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void InstallGlobaltoolParserCanGetPackageIdAndPackageVersion()
        {
            var command = Parser.Instance;
            var result = command.Parse("dotnet install tool console.wul.test.app.1 --version 1.0.1");

            var parseResult = result["dotnet"]["install"]["tool"];

            var packageId = parseResult.Arguments.Single();
            var packageVersion = parseResult.ValueOrDefault<string>("version");

            packageId.Should().Be("console.wul.test.app.1");
            packageVersion.Should().Be("1.0.1");
        }

        [Fact]
        public void InstallGlobaltoolParserCanGetFollowingArguments()
        {
            var command = Parser.Instance;
            var result = command.Parse(@"dotnet install tool console.wul.test.app.1 --version 1.0.1 --framework netcoreapp2.0 --configfile C:\TestAssetLocalNugetFeed");


            var parseResult = result["dotnet"]["install"]["tool"];

            var packageId = parseResult.Arguments.Single();
            var packageVersion = parseResult.ValueOrDefault<string>("version");

            var configFilePath = parseResult.ValueOrDefault<string>("configfile").Should().Be(@"C:\TestAssetLocalNugetFeed");

            var framework = parseResult.ValueOrDefault<string>("framework").Should().Be("netcoreapp2.0");

        }

    }
}
