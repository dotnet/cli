﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Test
{
    public class ConsoleTestRunner : IDotnetTestRunner
    {
        private readonly ITestRunnerNameResolver _testRunnerNameResolver;

        private readonly ICommandFactory _commandFactory;

        private readonly string _assemblyUnderTest;

        private readonly NuGetFramework _framework;

        public ConsoleTestRunner(
            ITestRunnerNameResolver testRunnerNameResolver,
            ICommandFactory commandFactory,
            string assemblyUnderTest,
            NuGetFramework framework = null)
        {
            _testRunnerNameResolver = testRunnerNameResolver;
            _commandFactory = commandFactory;
            _assemblyUnderTest = assemblyUnderTest;
            _framework = framework;
        }

        public int RunTests(DotnetTestParams dotnetTestParams)
        {
            return _commandFactory.Create(
                    _testRunnerNameResolver.ResolveTestRunner(),
                    GetCommandArgs(dotnetTestParams),
                    _framework,
                    dotnetTestParams.Config)
                .Execute()
                .ExitCode;
        }

        private IEnumerable<string> GetCommandArgs(DotnetTestParams dotnetTestParams)
        {
            var commandArgs = new List<string>
            {
                _assemblyUnderTest
            };

            commandArgs.AddRange(dotnetTestParams.RemainingArguments);

            return commandArgs;
        }
    }
}
