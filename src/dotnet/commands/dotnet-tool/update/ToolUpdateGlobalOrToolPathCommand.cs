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
using Microsoft.DotNet.ShellShim;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools.Tool.Common;
using Microsoft.DotNet.Tools.Tool.Install;
using Microsoft.DotNet.Tools.Tool.Uninstall;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Versioning;

namespace Microsoft.DotNet.Tools.Tool.Update
{
    internal delegate IShellShimRepository CreateShellShimRepository(DirectoryPath? nonGlobalLocation = null);

    internal delegate (IToolPackageStore, IToolPackageStoreQuery, IToolPackageInstaller, IToolPackageUninstaller) CreateToolPackageStoresAndInstallerAndUninstaller(
        DirectoryPath? nonGlobalLocation = null,
		IEnumerable<string> additionalRestoreArguments = null);

    internal class ToolUpdateGlobalOrToolPathCommand : CommandBase
    {
        private readonly IReporter _reporter;
        private readonly IReporter _errorReporter;
        private readonly CreateShellShimRepository _createShellShimRepository;
        private readonly CreateToolPackageStoresAndInstallerAndUninstaller _createToolPackageStoreInstallerUninstaller;

        private readonly PackageId _packageId;
        private readonly string _configFilePath;
        private readonly string _framework;
        private readonly string[] _additionalFeeds;
        private readonly bool _global;
        private readonly string _verbosity;
        private readonly string _toolPath;
        private readonly IEnumerable<string> _forwardRestoreArguments;
        private readonly string _packageVersion;

        public ToolUpdateGlobalOrToolPathCommand(AppliedOption appliedCommand,
            ParseResult parseResult,
            CreateToolPackageStoresAndInstallerAndUninstaller createToolPackageStoreInstallerUninstaller = null,
            CreateShellShimRepository createShellShimRepository = null,
            IReporter reporter = null)
            : base(parseResult)
        {
            if (appliedCommand == null)
            {
                throw new ArgumentNullException(nameof(appliedCommand));
            }

            _packageId = new PackageId(appliedCommand.Arguments.Single());
            _configFilePath = appliedCommand.ValueOrDefault<string>("configfile");
            _framework = appliedCommand.ValueOrDefault<string>("framework");
            _additionalFeeds = appliedCommand.ValueOrDefault<string[]>("add-source");
            _packageVersion = appliedCommand.SingleArgumentOrDefault("version");
            _global = appliedCommand.ValueOrDefault<bool>(ToolAppliedOption.GlobalOption);
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");
            _toolPath = appliedCommand.SingleArgumentOrDefault(ToolAppliedOption.ToolPathOption);
            _forwardRestoreArguments = appliedCommand.OptionValuesToBeForwarded();

            _createToolPackageStoreInstallerUninstaller = createToolPackageStoreInstallerUninstaller ??
                                                  ToolPackageFactory.CreateToolPackageStoresAndInstallerAndUninstaller;

            _createShellShimRepository =
                createShellShimRepository ?? ShellShimRepositoryFactory.CreateShellShimRepository;

            _reporter = (reporter ?? Reporter.Output);
            _errorReporter = (reporter ?? Reporter.Error);
        }

        public override int Execute()
        {
            ValidateArguments();

            DirectoryPath? toolPath = null;
            if (_toolPath != null)
            {
                toolPath = new DirectoryPath(_toolPath);
            }

            VersionRange versionRange = null;
            if (!string.IsNullOrEmpty(_packageVersion) && !VersionRange.TryParse(_packageVersion, out versionRange))
            {
                throw new GracefulException(
                    string.Format(
                        LocalizableStrings.InvalidNuGetVersionRange,
                        _packageVersion));
            }

            (IToolPackageStore toolPackageStore,
             IToolPackageStoreQuery toolPackageStoreQuery,
             IToolPackageInstaller toolPackageInstaller,
             IToolPackageUninstaller toolPackageUninstaller) = _createToolPackageStoreInstallerUninstaller(toolPath, _forwardRestoreArguments);

            IShellShimRepository shellShimRepository = _createShellShimRepository(toolPath);

            IToolPackage oldPackageNullable = GetOldPackage(toolPackageStoreQuery);

            using (var scope = new TransactionScope(
                TransactionScopeOption.Required,
                TimeSpan.Zero))
            {
                if (oldPackageNullable != null)
                {
                    RunWithHandlingUninstallError(() =>
                    {
                        foreach (RestoredCommand command in oldPackageNullable.Commands)
                        {
                            shellShimRepository.RemoveShim(command.Name);
                        }

                        toolPackageUninstaller.Uninstall(oldPackageNullable.PackageDirectory);
                    });
                }

                RunWithHandlingInstallError(() =>
                {
                    IToolPackage newInstalledPackage = toolPackageInstaller.InstallPackage(
                        new PackageLocation(nugetConfig: GetConfigFile(), additionalFeeds: _additionalFeeds),
                        packageId: _packageId,
                        targetFramework: _framework,
                        versionRange: versionRange,
                        verbosity: _verbosity);

                    EnsureVersionIsHigher(oldPackageNullable, newInstalledPackage);

                    foreach (RestoredCommand command in newInstalledPackage.Commands)
                    {
                        shellShimRepository.CreateShim(command.Executable, command.Name);
                    }

                    PrintSuccessMessage(oldPackageNullable, newInstalledPackage);
                });

                scope.Complete();
            }

            return 0;
        }

        private static void EnsureVersionIsHigher(IToolPackage oldPackageNullable, IToolPackage newInstalledPackage)
        {
            if (oldPackageNullable != null && (newInstalledPackage.Version < oldPackageNullable.Version))
            {
                throw new GracefulException(
                    new[]
                    {
                        string.Format(LocalizableStrings.UpdateToLowerVersion,
                            newInstalledPackage.Version.ToNormalizedString(),
                            oldPackageNullable.Version.ToNormalizedString())
                    },
                    isUserError: false);
            }
        }

        private void ValidateArguments()
        {
            if (_configFilePath != null && !File.Exists(_configFilePath))
            {
                throw new GracefulException(
                    string.Format(
                        LocalizableStrings.NuGetConfigurationFileDoesNotExist,
                        Path.GetFullPath(_configFilePath)));
            }
        }

        private void RunWithHandlingInstallError(Action installAction)
        {
            try
            {
                installAction();
            }
            catch (Exception ex)
                when (InstallToolCommandLowLevelErrorConverter.ShouldConvertToUserFacingError(ex))
            {
                var message = new List<string>
                {
                    string.Format(LocalizableStrings.UpdateToolFailed, _packageId)
                };
                message.AddRange(
                    InstallToolCommandLowLevelErrorConverter.GetUserFacingMessages(ex, _packageId));


                throw new GracefulException(
                    messages: message,
                    verboseMessages: new[] { ex.ToString() },
                    isUserError: false);
            }
        }

        private void RunWithHandlingUninstallError(Action uninstallAction)
        {
            try
            {
                uninstallAction();
            }
            catch (Exception ex)
                when (ToolUninstallCommandLowLevelErrorConverter.ShouldConvertToUserFacingError(ex))
            {
                var message = new List<string>
                {
                    string.Format(LocalizableStrings.UpdateToolFailed, _packageId)
                };
                message.AddRange(
                    ToolUninstallCommandLowLevelErrorConverter.GetUserFacingMessages(ex, _packageId));

                throw new GracefulException(
                    messages: message,
                    verboseMessages: new[] { ex.ToString() },
                    isUserError: false);
            }
        }

        private FilePath? GetConfigFile()
        {
            FilePath? configFile = null;
            if (_configFilePath != null)
            {
                configFile = new FilePath(_configFilePath);
            }

            return configFile;
        }

        private IToolPackage GetOldPackage(IToolPackageStoreQuery toolPackageStoreQuery)
        {
            IToolPackage oldPackageNullable;
            try
            {
                oldPackageNullable = toolPackageStoreQuery.EnumeratePackageVersions(_packageId).SingleOrDefault();
            }
            catch (InvalidOperationException)
            {
                throw new GracefulException(
                    messages: new[]
                    {
                        string.Format(
                            LocalizableStrings.ToolHasMultipleVersionsInstalled,
                            _packageId),
                    },
                    isUserError: false);
            }

            return oldPackageNullable;
        }

        private void PrintSuccessMessage(IToolPackage oldPackage, IToolPackage newInstalledPackage)
        {
            if (oldPackage == null)
            {
                _reporter.WriteLine(
                    string.Format(
                        Install.LocalizableStrings.InstallationSucceeded,
                        string.Join(", ", newInstalledPackage.Commands.Select(c => c.Name)),
                        newInstalledPackage.Id,
                        newInstalledPackage.Version.ToNormalizedString()).Green());
            }
            else if (oldPackage.Version != newInstalledPackage.Version)
            {
                _reporter.WriteLine(
                    string.Format(
                        LocalizableStrings.UpdateSucceeded,
                        newInstalledPackage.Id,
                        oldPackage.Version.ToNormalizedString(),
                        newInstalledPackage.Version.ToNormalizedString()).Green());
            }
            else
            {
                _reporter.WriteLine(
                    string.Format(
                        LocalizableStrings.UpdateSucceededVersionNoChange,
                        newInstalledPackage.Id, newInstalledPackage.Version).Green());
            }
        }
    }
}
