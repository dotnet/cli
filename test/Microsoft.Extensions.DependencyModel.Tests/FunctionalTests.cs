﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel
{
    public class FunctionalTests : TestBase
    {
        private readonly string _testProjectsRoot;

        public FunctionalTests()
        {
            _testProjectsRoot = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TestProjects");
        }

        [Theory]
        [InlineData("TestApp", true)]
        [InlineData("TestAppFullClr", true)]
        [InlineData("TestAppPortable", true)]
        [InlineData("TestAppDeps", false)]
        public void RunTest(string appname, bool checkCompilation)
        {
            var testProjectPath = Path.Combine(RepoRoot, "TestAssets", "TestProjects", "DependencyContextValidator", appname);
            var testProject = Path.Combine(testProjectPath, "project.json");

            var runCommand = new RunCommand(testProject);
            var result = runCommand.ExecuteWithCapturedOutput();
            result.Should().Pass();
            ValidateRuntimeLibrarites(result, appname);
            if (checkCompilation)
            {
                ValidateCompilationLibraries(result, appname);
            }
        }

        [Theory]
        [InlineData("TestApp", false, true)]
        [InlineData("TestAppFullClr", false, true)]
        [InlineData("TestAppPortable", true, true)]
        [InlineData("TestAppDeps", false, false)]
        public void PublishTest(string appname, bool portable, bool checkCompilation)
        {
            var testProjectPath = Path.Combine(RepoRoot, "TestAssets", "TestProjects", "DependencyContextValidator", appname);
            var testProject = Path.Combine(testProjectPath, "project.json");

            var publishCommand = new PublishCommand(testProject);
            publishCommand.Execute().Should().Pass();

            var exeName = portable ? publishCommand.GetPortableOutputName() : publishCommand.GetOutputExecutable();

            var result = TestExecutable(publishCommand.GetOutputDirectory(portable).FullName, exeName, string.Empty);
            
            ValidateRuntimeLibrarites(result, appname);
            if (checkCompilation)
            { 
                ValidateCompilationLibraries(result, appname);
            }
        }

        private void ValidateRuntimeLibrarites(CommandResult result, string appname)
        {
            // entry assembly
            result.Should().HaveStdOutContaining($"Runtime {appname}:{appname}");
            // project dependency
            result.Should().HaveStdOutContaining("Runtime DependencyContextValidator:DependencyContextValidator");
            // system assembly
            result.Should().HaveStdOutContaining("Runtime System.Linq:System.Linq");
        }

        private void ValidateCompilationLibraries(CommandResult result, string appname)
        {
            // entry assembly
            result.Should().HaveStdOutContaining($"Compilation {appname}:{appname}.dll");
            // project dependency
            result.Should().HaveStdOutContaining("Compilation DependencyContextValidator:DependencyContextValidator.dll");
            // system assembly
            result.Should().HaveStdOutContaining("Compilation System.Linq:System.Linq.dll");
        }

    }
}
