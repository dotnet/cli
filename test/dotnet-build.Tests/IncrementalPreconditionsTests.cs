// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using Microsoft.DotNet.TestFramework;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class IncrementalPreconditionsTests : TestBase
    {
        protected readonly string _libraryFrameworkFullName = ".NETStandard,Version=v1.5";
        protected readonly string _appFrameworkFullName = ".NETCoreApp,Version=v1.0";

        [Fact]
        public void PreconditionFailWhenPreCompileScriptsArePresent()
        {
            Test("TestAppWithScripts", "[Pre / Post Scripts]", "TestAppWithScripts", "precompile");
        }

        [Fact]
        public void PreconditionFailSkipsWhenPostCompileScriptsArePresent()
        {
            Test("TestAppWithScripts", "[Pre / Post Scripts]", "TestAppWithScripts", "postcompile");
        }

        [Fact]
        public void PreconditionFailSkipsWhenUnknownCompilerIsPresent()
        {
            Test("AppWithUnknownCompilerName", "[Unknown Compiler]", "AppWithUnknownCompilerName", "fakeCompiler");
        }

        [Fact]
        public void PreconditionFailSkipsWhenBuildInvokesCommandFromPath()
        {
            Test("AppWithUnknownCompilerName", "[PATH Probing]", "AppWithUnknownCompilerName", "dotnet-compile-fakeCompiler");
        }

        private CommandResult Test(string projectName, params string[] lineIncludeStrings)
        {
            var testInstance = TestAssetsManager.CreateTestInstance(projectName)
                .WithLockFiles()
                .WithBuildArtifacts();

            var buildCommand = new BuildCommand(testInstance.TestRoot);

            var path = Environment.GetEnvironmentVariable("PATH");
            buildCommand.Environment.Add("PATH", testInstance.TestRoot + Path.PathSeparator + path);

            var result = buildCommand.ExecuteWithCapturedOutput();
            result.Should().HaveCompiledProject(projectName, _appFrameworkFullName);

            var regexEscapedStrings = lineIncludeStrings.Select(Regex.Escape);
            var lineRegex = "^.*" + string.Join(".*", regexEscapedStrings) + ".*$";
        
            result.Should().HaveStdOutMatching(lineRegex, RegexOptions.Multiline);

            return result;
        }
    }
}