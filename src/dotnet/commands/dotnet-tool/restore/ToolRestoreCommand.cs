// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ToolManifest;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.Tools.Tool.Restore
{
    internal class ToolRestoreCommand : CommandBase
    {
        private readonly string _configFilePath;
        private readonly IReporter _errorReporter;
        private readonly ILocalToolsResolverCache _localToolsResolverCache;
        private readonly IToolManifestFinder _toolManifestFinder;
        private readonly DirectoryPath _nugetGlobalPackagesFolder;
        private readonly AppliedOption _options;
        private readonly IReporter _reporter;
        private readonly string[] _source;
        private readonly IToolPackageInstaller _toolPackageInstaller;
        private readonly string _verbosity;
        private const int _localToolResolverCacheVersion = 1;

        public ToolRestoreCommand(
            AppliedOption appliedCommand,
            ParseResult result,
            IToolPackageInstaller toolPackageInstaller = null,
            IToolManifestFinder toolManifestFinder = null,
            ILocalToolsResolverCache localToolsResolverCache = null,
            DirectoryPath? nugetGlobalPackagesFolder = null,
            IReporter reporter = null)
            : base(result)
        {
            _options = appliedCommand ?? throw new ArgumentNullException(nameof(appliedCommand));

            if (toolPackageInstaller == null)
            {
                (IToolPackageStore,
                    IToolPackageStoreQuery,
                    IToolPackageInstaller installer) toolPackageStoresAndInstaller
                        = ToolPackageFactory.CreateToolPackageStoresAndInstaller(
                            additionalRestoreArguments: appliedCommand.OptionValuesToBeForwarded());
                _toolPackageInstaller = toolPackageStoresAndInstaller.installer;
            }
            else
            {
                _toolPackageInstaller = toolPackageInstaller;
            }

            _toolManifestFinder = toolManifestFinder
                                  ?? new ToolManifestFinder(new DirectoryPath(Directory.GetCurrentDirectory()));

            _localToolsResolverCache = localToolsResolverCache ??
                                       new LocalToolsResolverCache(
                                           new FileSystemWrapper(),
                                           new DirectoryPath(Path.Combine(CliFolderPathCalculator.ToolsResolverCachePath)),
                                           _localToolResolverCacheVersion);

            _nugetGlobalPackagesFolder =
                nugetGlobalPackagesFolder ?? new DirectoryPath(NuGetGlobalPackagesFolder.GetLocation());
            _reporter = reporter ?? Reporter.Output;
            _errorReporter = reporter ?? Reporter.Error;

            _configFilePath = appliedCommand.ValueOrDefault<string>("configfile");
            _source = appliedCommand.ValueOrDefault<string[]>("add-source");
            _verbosity = appliedCommand.SingleArgumentOrDefault("verbosity");
        }

        public override int Execute()
        {
            FilePath? customManifestFileLocation = GetCustomManifestFileLocation();

            FilePath? configFile = null;
            if (_configFilePath != null) configFile = new FilePath(_configFilePath);

            IReadOnlyCollection<ToolManifestFindingResultSinglePackage> packagesFromManifest =
                _toolManifestFinder.Find(customManifestFileLocation);

            Dictionary<RestoredCommandIdentifier, RestoredCommand> dictionary =
                new Dictionary<RestoredCommandIdentifier, RestoredCommand>();

            Dictionary<PackageId, ToolPackageException> toolPackageExceptions =
                new Dictionary<PackageId, ToolPackageException>();

            foreach (var package in packagesFromManifest)
            {
                string targetFramework =
                    package.OptionalNuGetFramework?.GetShortFolderName()
                    ?? BundledTargetFramework.GetTargetFrameworkMoniker();

                try
                {
                    IToolPackage toolPackage =
                        _toolPackageInstaller.InstallPackageToExternalManagedLocation(
                            new PackageLocation(
                                nugetConfig: configFile,
                                additionalFeeds: _source),
                            package.PackageId, ToVersionRangeWithOnlyOneVersion(package.Version), targetFramework,
                            verbosity: _verbosity);

                    foreach (RestoredCommand command in toolPackage.Commands)
                    {
                        dictionary.Add(
                            new RestoredCommandIdentifier(
                                toolPackage.Id,
                                toolPackage.Version,
                                NuGetFramework.Parse(targetFramework),
                                "any",
                                command.Name),
                            command);
                    }
                }
                catch (ToolPackageException e)
                {
                    toolPackageExceptions.Add(package.PackageId, e);
                }
            }

            EnsureNoCommandNameCollision(dictionary);

            _localToolsResolverCache.Save(dictionary, _nugetGlobalPackagesFolder);

            if (toolPackageExceptions.Any())
            {
                _errorReporter.WriteLine(LocalizableStrings.RestorePartiallySuccessful +
                                         Environment.NewLine +
                                         string.Join(
                                             Environment.NewLine,
                                             toolPackageExceptions.Select(p =>
                                                 string.Format(LocalizableStrings.PackageFailedToRestore,
                                                     p.Key.ToString(), p.Value.ToString()))));

                return 1;
            }

            return 0;
        }

        private FilePath? GetCustomManifestFileLocation()
        {
            string customFile = _options.Arguments.SingleOrDefault();
            FilePath? customManifestFileLocation;
            if (customFile != null)
            {
                customManifestFileLocation = new FilePath(customFile);
            }
            else
            {
                customManifestFileLocation = null;
            }

            return customManifestFileLocation;
        }

        private void EnsureNoCommandNameCollision(Dictionary<RestoredCommandIdentifier, RestoredCommand> dictionary)
        {
            string[] errors = dictionary
                .Select(pair => (PackageId: pair.Key.PackageId, CommandName: pair.Key.CommandName))
                .GroupBy(packageIdAndCommandName => packageIdAndCommandName.CommandName)
                .Where(grouped => grouped.Count() > 1)
                .Select(nonUniquePackageIdAndCommandNames =>
                    string.Format(LocalizableStrings.PackagesCommandNameCollision,
                        JoinBySpaceWithQuote(nonUniquePackageIdAndCommandNames.Select(a => a.PackageId.ToString())),
                        JoinBySpaceWithQuote(nonUniquePackageIdAndCommandNames.Select(a => a.CommandName.ToString()))))
                .ToArray();

            if (errors.Any())
            {
                throw new ToolPackageException(string.Join(Environment.NewLine, errors));
            }
        }

        private static string JoinBySpaceWithQuote(IEnumerable<object> objects)
        {
            return string.Join(", ", objects.Select(o => $"\"{o.ToString()}\""));
        }

        private static VersionRange ToVersionRangeWithOnlyOneVersion(NuGetVersion version)
        {
            return new VersionRange(
                version,
                includeMinVersion: true,
                maxVersion: version,
                includeMaxVersion: true);
        }
    }
}
