// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

namespace Microsoft.DotNet.Tools.Uninstall.Tool
{
    internal delegate IShellShimRepository CreateShellShimRepository(DirectoryPath? nonGlobalLocation = null);
    internal delegate IToolPackageStore CreateToolPackageStore(DirectoryPath? nonGlobalLocation = null);
    internal class UninstallToolCommand : CommandBase
    {
        private readonly AppliedOption _options;
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;
        private CreateShellShimRepository _createShellShimRepository;
        private CreateToolPackageStore _createToolPackageStoreAndInstaller;

        public UninstallToolCommand(
            AppliedOption options,
            ParseResult result,
            CreateToolPackageStore createToolPackageStoreAndInstaller = null,
            CreateShellShimRepository createShellShimRepository = null,
            IReporter reporter = null)
            : base(result)
        {
            var pathCalculator = new CliFolderPathCalculator();

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;

            _createShellShimRepository = createShellShimRepository ?? ShellShimRepositoryFactory.CreateShellShimRepository;
            _createToolPackageStoreAndInstaller = createToolPackageStoreAndInstaller ?? ToolPackageFactory.CreateToolPackageStore;
        }

        public override int Execute()
        {
            var global = _options.ValueOrDefault<bool>("global");
            var toolPath = _options.SingleArgumentOrDefault("tool-path");

            if (string.IsNullOrWhiteSpace(toolPath) && !global)
            {
                throw new GracefulException(LocalizableStrings.UninstallToolCommandNeedGlobalOrToolPath);
            }

            if (!string.IsNullOrWhiteSpace(toolPath) && global)
            {
                throw new GracefulException(LocalizableStrings.UninstallToolCommandInvalidGlobalAndToolPath);
            }

            var packageId = new PackageId(_options.Arguments.Single());
            IToolPackage package = null;
<<<<<<< HEAD
            try
            {
                package = _toolPackageStore.EnumeratePackageVersions(packageId).SingleOrDefault();
=======

            DirectoryPath? toolDirectoryPath = null;
            if (!string.IsNullOrWhiteSpace(toolPath))
            {
                toolDirectoryPath = new DirectoryPath(toolPath);
            }

            IToolPackageStore toolPackageStore = _createToolPackageStoreAndInstaller(toolDirectoryPath);
            IShellShimRepository shellShimRepository = _createShellShimRepository(toolDirectoryPath);

            try
            {
                package = toolPackageStore.GetInstalledPackages(packageId).SingleOrDefault();
>>>>>>> tool-path option -- "Session tool"
                if (package == null)
                {
                    _errorReporter.WriteLine(
                        string.Format(
                            LocalizableStrings.ToolNotInstalled,
                            packageId).Red());
                    return 1;
                }
            }
            catch (InvalidOperationException)
            {
                _errorReporter.WriteLine(
                    string.Format(
                        LocalizableStrings.ToolHasMultipleVersionsInstalled,
                        packageId).Red());
                return 1;
            }

            try
            {
                using (var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    TimeSpan.Zero))
                {
                    foreach (var command in package.Commands)
                    {
                        shellShimRepository.RemoveShim(command.Name);
                    }

                    package.Uninstall();

                    scope.Complete();
                }

                _reporter.WriteLine(
                    string.Format(
                        LocalizableStrings.UninstallSucceeded,
                        package.Id,
                        package.Version.ToNormalizedString()).Green());
                return 0;
            }
            catch (ToolPackageException ex)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(ex.Message.Red());
                return 1;
            }
            catch (Exception ex) when (ex is ToolConfigurationException || ex is ShellShimException)
            {
                if (Reporter.IsVerbose)
                {
                    Reporter.Verbose.WriteLine(ex.ToString().Red());
                }

                _errorReporter.WriteLine(
                    string.Format(
                        LocalizableStrings.FailedToUninstallTool,
                        packageId,
                        ex.Message).Red());
                return 1;
            }
        }
    }
}
