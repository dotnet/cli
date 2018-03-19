// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Xunit;
using Xunit.Abstractions;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tests.ParserTests
{
    public class UpdateInstallToolParserTests
    {
        private readonly ITestOutputHelper _output;

        public UpdateInstallToolParserTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void UpdateGlobaltoolParserCanGetPackageId()
        {
            var command = Parser.Instance;
            var result = command.Parse("dotnet update tool -g console.test.app");

            var parseResult = result["dotnet"]["update"]["tool"];

            var packageId = parseResult.Arguments.Single();

            packageId.Should().Be("console.test.app");
        }

        [Fact]
        public void UpdateToolParserCanGetGlobalOption()
        {
            var result = Parser.Instance.Parse("dotnet update tool -g console.test.app");

            var appliedOptions = result["dotnet"]["update"]["tool"];
            appliedOptions.ValueOrDefault<bool>("global").Should().Be(true);
        }

        [Fact]
        public void UpdateToolParserCanGetFollowingArguments()
        {
            var command = Parser.Instance;
            var result =
                command.Parse(
                    @"dotnet update tool -g console.test.app --version 1.0.1 --framework netcoreapp2.0 --configfile C:\TestAssetLocalNugetFeed");

            var parseResult = result["dotnet"]["update"]["tool"];

            parseResult.ValueOrDefault<string>("configfile").Should().Be(@"C:\TestAssetLocalNugetFeed");
            parseResult.ValueOrDefault<string>("framework").Should().Be("netcoreapp2.0");
        }

        [Fact]
        public void UpdateToolParserCanParseSourceOption()
        {
            const string expectedSourceValue = "TestSourceValue";

            var result =
                Parser.Instance.Parse($"dotnet update tool -g --source {expectedSourceValue} console.test.app");

            var appliedOptions = result["dotnet"]["update"]["tool"];
            appliedOptions.ValueOrDefault<string>("source").Should().Be(expectedSourceValue);
        }

        [Fact]
        public void UpdateToolParserCanParseVerbosityOption()
        {
            const string expectedVerbosityLevel = "diag";

            var result =
                Parser.Instance.Parse($"dotnet update tool -g --verbosity:{expectedVerbosityLevel} console.test.app");

            var appliedOptions = result["dotnet"]["update"]["tool"];
            appliedOptions.SingleArgumentOrDefault("verbosity").Should().Be(expectedVerbosityLevel);
        }

        [Fact]
        public void UpdateToolParserCanParseToolPathOption()
        {
            var result =
                Parser.Instance.Parse(@"dotnet update tool --tool-path C:\TestAssetLocalNugetFeed console.test.app");

            var appliedOptions = result["dotnet"]["update"]["tool"];
            appliedOptions.SingleArgumentOrDefault("tool-path").Should().Be(@"C:\TestAssetLocalNugetFeed");
        }
    }
}
