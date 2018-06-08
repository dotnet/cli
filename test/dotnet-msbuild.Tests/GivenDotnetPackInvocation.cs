﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Pack;
using FluentAssertions;
using Xunit;
using System;
using System.Linq;

namespace Microsoft.DotNet.Cli.MSBuild.Tests
{
    public class GivenDotnetPackInvocation
    {
        const string ExpectedPrefix = "exec <msbuildpath> -maxcpucount -verbosity:m -restore -target:pack";
        const string ExpectedNoBuildPrefix = "exec <msbuildpath> -maxcpucount -verbosity:m -target:pack";

        [Theory]
        [InlineData(new string[] { }, "")]
        [InlineData(new string[] { "-o", "<packageoutputpath>" }, "-property:PackageOutputPath=<packageoutputpath>")]
        [InlineData(new string[] { "--output", "<packageoutputpath>" }, "-property:PackageOutputPath=<packageoutputpath>")]
        [InlineData(new string[] { "--no-build" }, "-property:NoBuild=true")]
        [InlineData(new string[] { "--include-symbols" }, "-property:IncludeSymbols=true")]
        [InlineData(new string[] { "--include-source" }, "-property:IncludeSource=true")]
        [InlineData(new string[] { "-c", "<config>" }, "-property:Configuration=<config>")]
        [InlineData(new string[] { "--configuration", "<config>" }, "-property:Configuration=<config>")]
        [InlineData(new string[] { "--version-suffix", "<versionsuffix>" }, "-property:VersionSuffix=<versionsuffix>")]
        [InlineData(new string[] { "-s" }, "-property:Serviceable=true")]
        [InlineData(new string[] { "--serviceable" }, "-property:Serviceable=true")]
        [InlineData(new string[] { "-v", "diag" }, "-verbosity:diag")]
        [InlineData(new string[] { "--verbosity", "diag" }, "-verbosity:diag")]
        [InlineData(new string[] { "<project>" }, "<project>")]
        public void MsbuildInvocationIsCorrect(string[] args, string expectedAdditionalArgs)
        {
            expectedAdditionalArgs = (string.IsNullOrEmpty(expectedAdditionalArgs) ? "" : $" {expectedAdditionalArgs}");

            var msbuildPath = "<msbuildpath>";
            var command = PackCommand.FromArgs(args, msbuildPath);
            var expectedPrefix = args.FirstOrDefault() == "--no-build" ? ExpectedNoBuildPrefix : ExpectedPrefix;

            command.SeparateRestoreCommand.Should().BeNull();
            command.GetProcessStartInfo().Arguments.Should().Be($"{expectedPrefix}{expectedAdditionalArgs}");
        }
    }
}
