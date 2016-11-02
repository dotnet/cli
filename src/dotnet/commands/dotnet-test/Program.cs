﻿// Copyright(c) .NET Foundation and contributors.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.MSBuild;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Tools.Test
{
    public class TestCommand
    {
        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var cmd = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet test",
                FullName = ".NET Test Driver",
                Description = "Test Driver for the .NET Platform"
            };

            cmd.HelpOption("-h|--help");

            var argRoot = cmd.Argument(
                "<PROJECT>",
                "The project to test, defaults to the current directory.",
                multipleValues: false);

            var settingOption = cmd.Option(
                "-s|--settings <SettingsFile>",
                "Settings to use when running tests.",
                CommandOptionType.SingleValue);

            var listTestsOption = cmd.Option(
                "-lt|--listTests",
                @"Lists discovered tests",
                CommandOptionType.NoValue);

            var testCaseFilterOption = cmd.Option(
                "-tcf|--testCaseFilter <Expression>",
                @"Run tests that match the given expression.
                                        Examples:
                                        --testCaseFilter:""Priority = 1""
                                        --testCaseFilter: ""(FullyQualifiedName~Nightly | Name = MyTestMethod)""",
                CommandOptionType.SingleValue);

            var testAdapterPathOption = cmd.Option(
                "-tap|--testAdapterPath",
                @"Use custom adapters from the given path in the test run.
                                        Example: --testAdapterPath:<pathToCustomAdapters>",
                CommandOptionType.SingleValue);

            var loggerOption = cmd.Option(
                "-l|--logger <LoggerUri/FriendlyName>",
                @"Specify a logger for test results. 
                                        Example: --logger:trx",
                CommandOptionType.SingleValue);

            var configurationOption = cmd.Option(
                "-c|--configuration <configuration>",
                @"Configuration under which to build, i.e. Debug/Release",
                CommandOptionType.SingleValue);

            var frameworkOption = cmd.Option(
                "-f|--framework <FrameworkVersion>",
                @"Looks for test binaries for a specific framework",
                CommandOptionType.SingleValue);

            var outputOption = cmd.Option(
                "-o|--output <OutputDir>",
                @"Directory in which to find the binaries to be run",
                CommandOptionType.SingleValue);

            var diagOption = cmd.Option(
                "-d|--diag <PathToLogFile>",
                @"Enable verbose logs for test platform.
                                        Logs are written to the provided file.",
                CommandOptionType.SingleValue);

            var noBuildtOption = cmd.Option(
               "--noBuild",
               @"Do not build project before testing.",
               CommandOptionType.NoValue);

            cmd.OnExecute(() =>
            {
                var msbuildArgs = new List<string>()
                {
                    "/t:VSTest"
                };

                msbuildArgs.Add("/verbosity:quiet");
                msbuildArgs.Add("/nologo");

                if (settingOption.HasValue())
                {
                    msbuildArgs.Add($"/p:VSTestSetting={settingOption.Value()}");
                }

                if (listTestsOption.HasValue())
                {
                    msbuildArgs.Add($"/p:VSTestListTests=true");
                }

                if (testCaseFilterOption.HasValue())
                {
                    msbuildArgs.Add($"/p:VSTestTestCaseFilter={testCaseFilterOption.Value()}");
                }

                if (testAdapterPathOption.HasValue())
                {
                    msbuildArgs.Add($"/p:VSTestTestAdapterPath={testAdapterPathOption.Value()}");
                }

                if (loggerOption.HasValue())
                {
                    msbuildArgs.Add($"/p:VSTestLogger={string.Join(";", loggerOption.Values)}");
                }

                if (configurationOption.HasValue())
                {
                    msbuildArgs.Add($"/p:Configuration={configurationOption.Value()}");
                }

                if (frameworkOption.HasValue())
                {
                    msbuildArgs.Add($"/p:TargetFramework={frameworkOption.Value()}");
                }

                if (outputOption.HasValue())
                {
                    msbuildArgs.Add($"/p:OutputPath={outputOption.Value()}");
                }

                if (diagOption.HasValue())
                {
                    msbuildArgs.Add($"/p:VSTestDiag={diagOption.Value()}");
                }

                if (noBuildtOption.HasValue())
                {
                    msbuildArgs.Add($"/p:VSTestNoBuild=true");
                }

                string defaultproject = GetSingleTestProjectToRunTestIfNotProvided(argRoot.Value, cmd.RemainingArguments);

                if(!string.IsNullOrEmpty(defaultproject))
                {
                    msbuildArgs.Add(defaultproject);
                }

                if (!string.IsNullOrEmpty(argRoot.Value))
                {
                    msbuildArgs.Add(argRoot.Value);
                }

                // Add remaining arguments that the parser did not understand,
                msbuildArgs.AddRange(cmd.RemainingArguments);

                return new MSBuildForwardingApp(msbuildArgs).Execute();
            });

            return cmd.Execute(args);
        }

        private static string GetSingleTestProjectToRunTestIfNotProvided(string args, List<string> remainingArguments)
        {
            string result = string.Empty;
            int projectFound = NumberOfTestProjectInRemainingArgs(remainingArguments) + NumberOfTestProjectInArgsRoot(args);

            if (projectFound > 1)
            {
                throw new GracefulException(
                $"Specify a single project file to run tests from.");
            }
            else if (projectFound == 0)
            {
                result = GetDefaultTestProject();
            }

            return result;
        }

        private static int NumberOfTestProjectInArgsRoot(string args)
        {
            Regex pattern = new Regex(@"^.*\..*proj$");

            if (!string.IsNullOrEmpty(args))
            {
                return pattern.IsMatch(args) ? 1 : 0;
            }

            return 0;
        }

        private static int NumberOfTestProjectInRemainingArgs(List<string> remainingArguments)
        {
            int count = 0;
            if (remainingArguments.Count != 0)
            {
                Regex pattern = new Regex(@"^.*\..*proj$");

                foreach (var x in remainingArguments)
                {
                    if (pattern.IsMatch(x))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static string GetDefaultTestProject()
        {
            string directory = Directory.GetCurrentDirectory();
            string[] projectFiles = Directory.GetFiles(directory, "*.*proj");

            if (projectFiles.Length == 0)
            {
                throw new GracefulException(
                    $"Couldn't find a project to run test from. Ensure a project exists in {directory}." + Environment.NewLine +
                    "Or pass the path to the project");
            }
            else if (projectFiles.Length > 1)
            {
                throw new GracefulException(
                    $"Specify which project file to use because this '{directory}' contains more than one project file.");
            }

            return projectFiles[0];
        }
    }
}
