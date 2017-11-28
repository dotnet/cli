// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
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
        private readonly DirectoryPath _executablePackagePath;
        private readonly IEnvironmentPathInstruction _environmentPathInstruction;
        private readonly IShellShimMaker _shellShimMaker;
        private readonly IReporter _reporter;

        private readonly string _packageId;
        private readonly string _packageVersion;
        private readonly string _configFilePath;
        private readonly string _framework;
        private static string _source;

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
            _executablePackagePath = new DirectoryPath(new CliFolderPathCalculator().ExecutablePackagesPath);

            _toolPackageObtainer = toolPackageObtainer ?? new ToolPackageObtainer(
                                       _executablePackagePath,
                                       () => new DirectoryPath(Path.GetTempPath())
                                           .WithSubDirectories(Path.GetRandomFileName())
                                           .WithFile(Path.GetRandomFileName() + ".csproj"),
                                       new Lazy<string>(BundledTargetFramework.GetTargetFrameworkMoniker),
                                       new PackageToProjectFileAdder(),
                                       new ProjectRestorer());

            _environmentPathInstruction = environmentPathInstruction
                                          ?? EnvironmentPathFactory
                                              .CreateEnvironmentPathInstruction();

            _shellShimMaker = shellShimMaker ?? new ShellShimMaker(_executablePackagePath.Value);

            _reporter = reporter ?? Reporter.Output;
        }

        public override int Execute()
        {
            var executablePackagePath = new DirectoryPath(new CliFolderPathCalculator().ExecutablePackagesPath);
            var toolConfigurationAndExecutableDirectory = ObtainPackage(executablePackagePath);

            DirectoryPath executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .WithSubDirectories(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            var commandName = toolConfigurationAndExecutableDirectory.Configuration.CommandName;
            _shellShimMaker.EnsureCommandNameUniqueness(commandName);

            _shellShimMaker.CreateShim(
                executable.Value,
                commandName);

            _environmentPathInstruction
                .PrintAddPathInstructionIfPathDoesNotExist();

            _reporter.WriteLine(
                string.Format(LocalizableStrings.InstallationSucceeded, commandName));

            return 0;
        }

        private static ToolConfigurationAndExecutableDirectory ObtainPackage(DirectoryPath executablePackagePath)
        {
            try
            {
                FilePath? configFile = null;
                if (_configFilePath != null)
                {
                    configFile = new FilePath(_configFilePath);
                }

                return _toolPackageObtainer.ObtainAndReturnExecutablePath(
                    packageId: _packageId,
                    packageVersion: _packageVersion,
                    nugetconfig: configFile,
                    targetframework: _framework);
            }
            catch (PackageObtainException ex)
            {
                throw new GracefulException(
                    message:
                    string.Format(LocalizableStrings.InstallFailedNuget,
                        ex.Message),
                    innerException: ex);
            }
            catch (ToolConfigurationException ex)
            {
                throw new GracefulException(
                    message:
                    string.Format(
                        LocalizableStrings.InstallFailedPackage,
                        ex.Message),
                    innerException: ex);
            }
        }
    }
}
