// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ShellShim;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Install.Tool
{
    internal class InstallToolCommand : CommandBase
    {
        private readonly IToolPackageObtainer _toolPackageObtainer;
        private readonly IEnvironmentPathInstruction _environmentPathInstruction;
        private readonly IShellShimMaker _shellShimMaker;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;

        private readonly string _packageId;
        private readonly string _packageVersion;
        private readonly string _configFilePath;
        private readonly string _framework;
        private readonly string _source;
        private readonly bool _global;
        private readonly string _verbosity;

        public InstallToolCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult,
            IToolPackageObtainer toolPackageObtainer = null,
            IShellShimMaker shellShimMaker = null,
            IEnvironmentPathInstruction environmentPathInstruction = null,
            IReporter reporter = null)
            : base(parseResult)
        {
            if (appliedCommand == null)
            {
                throw new ArgumentNullException(nameof(appliedCommand));
            }

            _packageId = appliedCommand.Arguments.Single();
            _packageVersion = appliedCommand.ValueOrDefault<string>("version");
            _configFilePath = appliedCommand.ValueOrDefault<string>("configfile");
            _framework = appliedCommand.ValueOrDefault<string>("framework");
            _source = appliedCommand.ValueOrDefault<string>("source");
            _global = appliedCommand.ValueOrDefault<bool>("global");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");

            var cliFolderPathCalculator = new CliFolderPathCalculator();
            var offlineFeedPath = new DirectoryPath(cliFolderPathCalculator.CliFallbackFolderPath);
            _toolPackageObtainer = toolPackageObtainer ?? new ToolPackageObtainer(
                                       new DirectoryPath(cliFolderPathCalculator.ToolsPackagePath),
                                       offlineFeedPath,
                                       () => new DirectoryPath(Path.GetTempPath())
                                           .WithSubDirectories(Path.GetRandomFileName())
                                           .WithFile(Path.GetRandomFileName() + ".csproj"),
                                       new Lazy<string>(BundledTargetFramework.GetTargetFrameworkMoniker),
                                       new ProjectRestorer(reporter));

            _environmentPathInstruction = environmentPathInstruction
                                          ?? EnvironmentPathFactory
                                              .CreateEnvironmentPathInstruction();

            _shellShimMaker = shellShimMaker ?? new ShellShimMaker(cliFolderPathCalculator.ToolsShimPath);

            _reporter = (reporter ?? Reporter.Output);
            _errorReporter = (reporter ?? Reporter.Error);
        }

        public override int Execute()
        {
            if (!_global)
            {
                throw new GracefulException(LocalizableStrings.InstallToolCommandOnlySupportGlobal);
            }

            try
            {
                FilePath? configFile = null;
                if (_configFilePath != null)
                {
                    configFile = new FilePath(_configFilePath);
                }

                using (var transactionScope = new TransactionScope())
                {
                    var toolConfigurationAndExecutablePath = _toolPackageObtainer.ObtainAndReturnExecutablePath(
                        packageId: _packageId,
                        packageVersion: _packageVersion,
                        nugetconfig: configFile,
                        targetframework: _framework,
                        source: _source,
                        verbosity: _verbosity);

                    var commandName = toolConfigurationAndExecutablePath.Configuration.CommandName;

                    _shellShimMaker.CreateShim(
                        toolConfigurationAndExecutablePath.Executable,
                        commandName);

                    _environmentPathInstruction
                        .PrintAddPathInstructionIfPathDoesNotExist();

                    _reporter.WriteLine(
                        string.Format(LocalizableStrings.InstallationSucceeded, commandName));
                    transactionScope.Complete();
                }
            }
            catch (PackageObtainException ex)
            {
                _errorReporter.WriteLine(ex.Message.Red());
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolInstallationFailed, _packageId).Red());
                return 1;
            }
            catch (ToolConfigurationException ex)
            {
                _errorReporter.WriteLine(
                    string.Format(
                        LocalizableStrings.InvalidToolConfiguration,
                        ex.Message).Red());
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolInstallationFailedContactAuthor, _packageId).Red());
                return 1;
            }

            return 0;
        }
    }
}
