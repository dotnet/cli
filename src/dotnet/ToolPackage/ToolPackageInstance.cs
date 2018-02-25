using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    // This is named "ToolPackageInstance" because "ToolPackage" would conflict with the namespace
    internal class ToolPackageInstance : IToolPackage
    {
        private IToolPackageStore _store;
        private Lazy<IReadOnlyList<CommandSettings>> _commands;

        public ToolPackageInstance(
            IToolPackageStore store,
            string packageId,
            string packageVersion,
            DirectoryPath packageDirectory)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            PackageId = packageId ?? throw new ArgumentNullException(nameof(packageId));
            PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
            PackageDirectory = packageDirectory;
            _commands = new Lazy<IReadOnlyList<CommandSettings>>(() => CommandSettingsRetriver.GetCommands(PackageId, PackageDirectory, false));
        }

        public string PackageId { get; private set; }

        public string PackageVersion { get; private set; }

        public DirectoryPath PackageDirectory { get; private set; }

        public IReadOnlyList<CommandSettings> Commands
        {
            get
            {
                return _commands.Value;
            }
        }

        public void Uninstall()
        {
            var rootDirectory = PackageDirectory.GetParentPath();
            string tempPackageDirectory = null;

            TransactionalAction.Run(
                action: () => {
                    try
                    {
                        if (Directory.Exists(PackageDirectory.Value))
                        {
                            // Use the same staging directory for uninstall instead of temp
                            // This prevents cross-device moves when temp is mounted to a different device
                            var tempPath = _store
                                .Root
                                .WithSubDirectories(ToolPackageInstaller.StagingDirectory)
                                .WithFile(Path.GetRandomFileName())
                                .Value;
                            Directory.Move(PackageDirectory.Value, tempPath);
                            tempPackageDirectory = tempPath;
                        }

                        if (Directory.Exists(rootDirectory.Value) &&
                            !Directory.EnumerateFileSystemEntries(rootDirectory.Value).Any())
                        {
                            Directory.Delete(rootDirectory.Value, false);
                        }
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        throw new ToolPackageException(
                            string.Format(
                                CommonLocalizableStrings.FailedToUninstallToolPackage,
                                PackageId,
                                ex.Message),
                            ex);
                    }
                },
                commit: () => {
                    if (tempPackageDirectory != null)
                    {
                        Directory.Delete(tempPackageDirectory, true);
                    }
                },
                rollback: () => {
                    if (tempPackageDirectory != null)
                    {
                        Directory.CreateDirectory(rootDirectory.Value);
                        Directory.Move(tempPackageDirectory, PackageDirectory.Value);
                    }
                });
        }
    }
}
