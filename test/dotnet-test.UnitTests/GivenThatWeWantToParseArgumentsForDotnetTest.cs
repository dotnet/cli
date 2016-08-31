﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test;
using Xunit;

namespace Microsoft.Dotnet.Tools.Test.Tests
{
    public class GivenThatWeWantToParseArgumentsForDotnetTest
    {
        private const string ProjectJson = "project.json";
        private const string Framework = "netcoreapp1.0";
        private const string Output = "some output";
        private const string BuildBasePath = "some build base path";
        private const string Config = "some config";
        private const string Runtime = "some runtime";
        private const int ParentProcessId = 1010;
        private const int Port = 2314;
        private const string TestRunner = "someTestRunner";
        private const string PathToAssemblyUnderTest = "c:/some/path/assemblyUnderTest.dll";

        private readonly DotnetTestParams _dotnetTestFullParamsWithProjectJson;
        private readonly DotnetTestParams _emptyDotnetTestParams;
        private readonly DotnetTestParams _dotnetTestParamsWithAssembly;

        public GivenThatWeWantToParseArgumentsForDotnetTest()
        {
            _dotnetTestFullParamsWithProjectJson = new DotnetTestParams();
            _dotnetTestParamsWithAssembly = new DotnetTestParams();
            _emptyDotnetTestParams = new DotnetTestParams();

            _dotnetTestFullParamsWithProjectJson.Parse(new[]
            {
                ProjectJson,
                "--parentProcessId", ParentProcessId.ToString(),
                "--port", Port.ToString(),
                "--framework", Framework,
                "--output", Output,
                "--build-base-path", BuildBasePath,
                "--configuration", Config,
                "--runtime", Runtime,
                "--no-build",
                "--additional-parameters", "additional-parameter-value"
            });

            _dotnetTestParamsWithAssembly.Parse(new[]
            {
                PathToAssemblyUnderTest,
                "--test-runner", TestRunner
            });

            _emptyDotnetTestParams.Parse(new string[] { });
        }

        [Fact]
        public void It_sets_the_project_path_current_folder_if_one_is_not_passed_in()
        {
            _emptyDotnetTestParams.ProjectOrAssemblyPath.Should().Be(Directory.GetCurrentDirectory());
        }

        [Fact]
        public void It_sets_the_project_path_to_the_passed_value()
        {
            _dotnetTestFullParamsWithProjectJson.ProjectOrAssemblyPath.Should().Be(ProjectJson);
        }

        [Fact]
        public void It_throws_InvalidOperationException_if_an_invalid_parent_process_id_is_passed_to_it()
        {
            var dotnetTestParams = new DotnetTestParams();
            const string invalidParentProcessId = "daddy";
            Action action = () => dotnetTestParams.Parse(new [] { "--parentProcessId", invalidParentProcessId });

            action
                .ShouldThrow<InvalidOperationException>()
                .WithMessage($"Invalid process id '{invalidParentProcessId}'. Process id must be an integer.");
        }

        [Fact]
        public void It_converts_the_parent_process_id_to_int_when_a_valid_one_is_passed()
        {
            _dotnetTestFullParamsWithProjectJson.ParentProcessId.Should().Be(ParentProcessId);
        }

        [Fact]
        public void It_does_not_set_parent_process_id_when_one_is_not_passed()
        {
            _emptyDotnetTestParams.ParentProcessId.Should().NotHaveValue();
        }

        [Fact]
        public void It_throws_InvalidOperationException_if_an_invalid_port_is_passed_to_it()
        {
            var dotnetTestParams = new DotnetTestParams();
            const string invalidPort = "door";
            Action action = () => dotnetTestParams.Parse(new[] { "--port", invalidPort });

            action
                .ShouldThrow<InvalidOperationException>()
                .WithMessage($"{invalidPort} is not a valid port number.");
        }

        [Fact]
        public void It_converts_the_port_to_int_when_a_valid_one_is_passed()
        {
            _dotnetTestFullParamsWithProjectJson.Port.Should().Be(Port);
        }

        [Fact]
        public void It_does_not_set_port_when_one_is_not_passed()
        {
            _emptyDotnetTestParams.Port.Should().NotHaveValue();
        }

        [Fact]
        public void It_converts_the_framework_to_NugetFramework()
        {
            _dotnetTestFullParamsWithProjectJson.Framework.DotNetFrameworkName.Should().Be(".NETCoreApp,Version=v1.0");
        }

        [Fact]
        public void It_does_not_set_framework_when_one_is_not_passed()
        {
            _emptyDotnetTestParams.Framework.Should().BeNull();
        }

        [Fact]
        public void It_sets_the_framework_to_unsupported_when_an_invalid_framework_is_passed_in()
        {
            var dotnetTestParams = new DotnetTestParams();
            dotnetTestParams.Parse(new[] { "--framework", "farm work" });

            dotnetTestParams.Framework.DotNetFrameworkName.Should().Be("Unsupported,Version=v0.0");
        }

        [Fact]
        public void It_sets_Output_when_one_is_passed_in()
        {
            _dotnetTestFullParamsWithProjectJson.Output.Should().Be(Output);
        }

        [Fact]
        public void It_leaves_Output_null_when_one_is_not_passed_in()
        {
            _emptyDotnetTestParams.Output.Should().BeNull();
        }

        [Fact]
        public void It_sets_BuildBasePath_when_one_is_passed_in()
        {
            _dotnetTestFullParamsWithProjectJson.BuildBasePath.Should().Be(Path.GetFullPath(BuildBasePath));
        }

        [Fact]
        public void It_leaves_BuildBasePath_null_when_one_is_not_passed_in()
        {
            _emptyDotnetTestParams.BuildBasePath.Should().BeNull();
        }

        [Fact]
        public void It_sets_Config_to_passed_in_value()
        {
            _dotnetTestFullParamsWithProjectJson.Config.Should().Be(Config);
        }

        [Fact]
        public void It_sets_Config_to_Debug_when_one_is_not_passed_in()
        {
            _emptyDotnetTestParams.Config.Should().Be("Debug");
        }

        [Fact]
        public void It_sets_Runtime_when_one_is_passed_in()
        {
            _dotnetTestFullParamsWithProjectJson.Runtime.Should().Be(Runtime);
        }

        [Fact]
        public void It_leaves_Runtime_null_when_one_is_not_passed_in()
        {
            _emptyDotnetTestParams.Runtime.Should().BeNull();
        }

        [Fact]
        public void It_sets_any_remaining_params_to_RemainingArguments()
        {
            _dotnetTestFullParamsWithProjectJson.RemainingArguments.ShouldBeEquivalentTo(
                new [] { "--additional-parameters", "additional-parameter-value" });
        }

        [Fact]
        public void It_sets_no_build_to_true_when_it_is_passed()
        {
            _dotnetTestFullParamsWithProjectJson.NoBuild.Should().BeTrue();
        }

        [Fact]
        public void It_sets_no_build_to_false_when_it_is_not_passed_in()
        {
            _emptyDotnetTestParams.NoBuild.Should().BeFalse();
        }

        [Fact]
        public void It_sets_Help_to_false_when_help_is_not_passed_in()
        {
            _dotnetTestFullParamsWithProjectJson.Help.Should().BeFalse();
        }

        [Fact]
        public void It_sets_Help_to_true_when_help_is_passed_in()
        {
            var dotnetTestParams = new DotnetTestParams();
            dotnetTestParams.Parse(new[] { "--help" });

            dotnetTestParams.Help.Should().BeTrue();
        }

        [Fact]
        public void It_has_the_testRunner_null_by_default()
        {
            _emptyDotnetTestParams.TestRunner.Should().BeNull();
        }

        [Fact]
        public void It_throws_when_you_specify_a_testRunner_along_with_a_folder()
        {
            var dotnetTestParams = new DotnetTestParams();
            Action action = () => dotnetTestParams.Parse(new[]
            {
                "c:/some/path",
                "--test-runner", "someTestRunner"
            });

            action
                .ShouldThrow<InvalidOperationException>()
                .WithMessage("You can only specify a test runner with a dll.");
        }

        [Fact]
        public void It_throws_when_you_specify_a_testRunner_along_with_a_project_json()
        {
            var dotnetTestParams = new DotnetTestParams();
            Action action = () => dotnetTestParams.Parse(new[]
            {
                ProjectJson,
                "--test-runner", "someTestRunner"
            });

            action
                .ShouldThrow<InvalidOperationException>()
                .WithMessage("You can only specify a test runner with a dll.");
        }

        [Fact]
        public void It_succeeds_when_specifying_an_assembly()
        {
            _dotnetTestParamsWithAssembly.ProjectOrAssemblyPath.Should().Be(PathToAssemblyUnderTest);
        }

        [Fact]
        public void It_succeeds_when_specifying_a_test_runner_along_with_an_assembly()
        {
            _dotnetTestParamsWithAssembly.TestRunner.Should().Be(TestRunner);
        }

        [Fact]
        public void When_a_testRunner_is_successfully_specified_then_HasTestRunner_returns_true()
        {
            _dotnetTestParamsWithAssembly.HasTestRunner.Should().BeTrue();
        }

        [Fact]
        public void When_a_testRunner_is_noy_specified_then_HasTestRunner_returns_false()
        {
            _dotnetTestFullParamsWithProjectJson.HasTestRunner.Should().BeFalse();
        }
    }
}
