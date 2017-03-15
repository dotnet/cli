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
        const string ExpectedPrefix = "exec <msbuildpath> /m /v:m /t:pack";

        [Theory]
        [InlineData(new string[] { }, "")]
        [InlineData(new string[] { "-o", "<packageoutputpath>" }, "/p:PackageOutputPath=<packageoutputpath>")]
        [InlineData(new string[] { "--output", "<packageoutputpath>" }, "/p:PackageOutputPath=<packageoutputpath>")]
        [InlineData(new string[] { "--no-build" }, "/p:NoBuild=true")]
        [InlineData(new string[] { "--include-symbols" }, "/p:IncludeSymbols=true")]
        [InlineData(new string[] { "--include-source" }, "/p:IncludeSource=true")]
        [InlineData(new string[] { "-c", "<config>" }, "/p:Configuration=<config>")]
        [InlineData(new string[] { "--configuration", "<config>" }, "/p:Configuration=<config>")]
        [InlineData(new string[] { "--version-suffix", "<versionsuffix>" }, "/p:VersionSuffix=<versionsuffix>")]
        [InlineData(new string[] { "-s" }, "/p:Serviceable=true")]
        [InlineData(new string[] { "--serviceable" }, "/p:Serviceable=true")]
        [InlineData(new string[] { "-v", "diag" }, "/verbosity:diag")]
        [InlineData(new string[] { "--verbosity", "diag" }, "/verbosity:diag")]
        [InlineData(new string[] { "<project>" }, "<project>")]
        public void MsbuildInvocationIsCorrect(string[] args, string expectedAdditionalArgs)
        {
            expectedAdditionalArgs = (string.IsNullOrEmpty(expectedAdditionalArgs) ? "" : $" {expectedAdditionalArgs}");

            var msbuildPath = "<msbuildpath>";
            PackCommand.FromArgs(args, msbuildPath)
                .GetProcessStartInfo().Arguments.Should().Be($"{ExpectedPrefix}{expectedAdditionalArgs}");
        }
    }
}
