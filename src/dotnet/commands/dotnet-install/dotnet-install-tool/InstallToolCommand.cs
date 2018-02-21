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
        private readonly IToolPackageStore _toolPackageStore;
        private readonly IToolPackageInstaller _toolPackageInstaller;
        private readonly IShellShimRepository _shellShimRepository;
        private readonly IEnvironmentPathInstruction _environmentPathInstruction;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;

        private readonly string _packageId;
        private readonly string _packageVersion;
        private readonly string _configFilePath;
        private readonly string _framework;
        private readonly string _source;
        private readonly bool _global;
        private readonly string _verbosity;
        private readonly string _binPath;

        public InstallToolCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult,
            IToolPackageStore toolPackageStore = null,
            IToolPackageInstaller toolPackageInstaller = null,
            IShellShimRepository shellShimRepository = null,
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
            _binPath = appliedCommand.SingleArgumentOrDefault("bin-path");

            var cliFolderPathCalculator = new CliFolderPathCalculator();

            _toolPackageStore = toolPackageStore
                ?? new ToolPackageStore(new DirectoryPath(cliFolderPathCalculator.ToolsPackagePath));

            _toolPackageInstaller = toolPackageInstaller
                ?? new ToolPackageInstaller(
                    _toolPackageStore,
                    new ProjectRestorer(_reporter));

            _environmentPathInstruction = environmentPathInstruction
                ?? EnvironmentPathFactory.CreateEnvironmentPathInstruction();

            _shellShimRepository = shellShimRepository
                ?? new ShellShimRepository(new DirectoryPath(cliFolderPathCalculator.ToolsShimPath));

            _reporter = (reporter ?? Reporter.Output);
            _errorReporter = (reporter ?? Reporter.Error);
        }

        public override int Execute()
        {
            var validateReturnCode = Validate();
            if (validateReturnCode != 0)
            {
                return validateReturnCode;
            }

            return HandleExceptionToUserFacingMessage(() =>
            {
                IToolPackage package = null;
                using (var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    TimeSpan.Zero))
                {
                    FilePath? configFile = null;
                    if (_configFilePath != null)
                    {
                        configFile = new FilePath(_configFilePath);
                    }

                    var nuGetPackageLocation = new NuGetPackageLocation(
                        packageId: _packageId,
                        packageVersion: _packageVersion,
                        nugetConfig: configFile,
                        source: _source);

                    IReadOnlyList<CommandSettings> commands;
                    if (_binPath != null)
                    {
                        commands = _toolPackageInstaller.InstallPackageToNuGetCache(
                            nuGetPackageLocation,
                            targetFramework: _framework,
                            verbosity: _verbosity);

                        foreach (var command in commands)
                        {
                            _shellShimRepository.CreateShim(
                                command.Executable,
                                command.Name,
                                new DirectoryPath(_binPath));
                        }
                    }
                    else
                    {
                        package = _toolPackageInstaller.InstallPackage(
                            nuGetPackageLocation,
                            targetFramework: _framework,
                            verbosity: _verbosity);

                        commands = package.Commands;
                        
                        foreach (var command in commands)
                        {
                            _shellShimRepository.CreateShim(command.Executable, command.Name);
                        }
                    }
                    

                    scope.Complete();
                }

                if (_binPath == null)
                {
                    _environmentPathInstruction.PrintAddPathInstructionIfPathDoesNotExist();

                    _reporter.WriteLine(
                        string.Format(
                            LocalizableStrings.InstallationSucceeded,
                            string.Join(", ", package.Commands.Select(c => c.Name)),
                            package.PackageId,
                            package.PackageVersion).Green());
                }
            });
        }

        private int HandleExceptionToUserFacingMessage(Action action)
        {
            try
            {
                action();
                return 0;
            }
            catch (ToolPackageException ex)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(ex.Message.Red());
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolInstallationFailed, _packageId).Red());
                return 1;
            }
            catch (ToolConfigurationException ex)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(
                    string.Format(
                        LocalizableStrings.InvalidToolConfiguration,
                        ex.Message).Red());
                _errorReporter.WriteLine(
                    string.Format(LocalizableStrings.ToolInstallationFailedContactAuthor, _packageId).Red());
                return 1;
            }
            catch (ShellShimException ex)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(
                    string.Format(
                        LocalizableStrings.FailedToCreateToolShim,
                        _packageId,
                        ex.Message).Red());
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolInstallationFailed, _packageId).Red());
                return 1;
            }
        }

        private int Validate()
        {
            if (_binPath != null && _global)
            {
                throw new GracefulException("Cannot have global and bin-path as opinion at the same time.");
            }

            if (_configFilePath != null && !File.Exists(_configFilePath))
            {
                throw new GracefulException(
                    string.Format(
                        LocalizableStrings.NuGetConfigurationFileDoesNotExist,
                        Path.GetFullPath(_configFilePath)));
            }

            // Prevent installation if any version of the package is installed
            if (_toolPackageStore.GetInstalledPackages(_packageId).FirstOrDefault() != null)
            {
                _errorReporter.WriteLine(string.Format(LocalizableStrings.ToolAlreadyInstalled, _packageId).Red());
                return 1;
            }

            return 0;
        }
    }
}
