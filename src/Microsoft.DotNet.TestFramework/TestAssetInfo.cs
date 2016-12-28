﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using NuGet.Common;

namespace Microsoft.DotNet.TestFramework
{
    public class TestAssetInfo
    {
        private const string DataDirectoryName = ".tam";

        private readonly string [] FilesToExclude = { ".DS_Store", ".noautobuild" };

        private readonly DirectoryInfo [] _directoriesToExclude;

        private readonly string _assetName;

        private readonly DirectoryInfo _dataDirectory;

        private readonly DirectoryInfo _root;

        private readonly TestAssetInventoryFiles _inventoryFiles;

        private readonly FileInfo _dotnetExeFile;

        private readonly string _projectFilePattern;

        internal DirectoryInfo Root 
        {
            get
            {
                return _root;
            }
        }

        internal TestAssetInfo(DirectoryInfo root, string assetName, FileInfo dotnetExeFile, string projectFilePattern)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (string.IsNullOrWhiteSpace(assetName))
            {
                throw new ArgumentException("Argument cannot be null or whitespace", nameof(assetName));
            }

            if (dotnetExeFile == null)
            {
                throw new ArgumentNullException(nameof(dotnetExeFile));
            }

            if (string.IsNullOrWhiteSpace(projectFilePattern))
            {
                throw new ArgumentException("Argument cannot be null or whitespace", nameof(projectFilePattern));
            }

            _root = root;

            _assetName = assetName;

            _dotnetExeFile = dotnetExeFile;

            _projectFilePattern = projectFilePattern;

            _dataDirectory = _root.GetDirectory(DataDirectoryName);

            _inventoryFiles = new TestAssetInventoryFiles(_dataDirectory);

            _directoriesToExclude = new []
            {
                _dataDirectory
            };
        }

        public TestAssetInstance CreateInstance([CallerMemberName] string callingMethod = "", string identifier = "")
        {
            var instancePath = GetTestDestinationDirectory(callingMethod, identifier);

            var testInstance = new TestAssetInstance(this, instancePath);

            return testInstance;
        }

        internal IEnumerable<FileInfo> GetSourceFiles()
        {
            ThrowIfTestAssetDoesNotExist();

            ThrowIfAssetSourcesHaveChanged();
            
            return GetInventory(
                _inventoryFiles.Source, 
                null, 
                () => {});
        }

        internal IEnumerable<FileInfo> GetRestoreFiles()
        {
            ThrowIfTestAssetDoesNotExist();

            ThrowIfAssetSourcesHaveChanged();
            
            return GetInventory(
                _inventoryFiles.Restore, 
                GetSourceFiles, 
                DoRestoreWithLock);
        }

        internal IEnumerable<FileInfo> GetBuildFiles()
        {
            ThrowIfTestAssetDoesNotExist();

            ThrowIfAssetSourcesHaveChanged();
            
            return GetInventory(
                _inventoryFiles.Build,
                () => GetRestoreFiles()
                        .Concat(GetSourceFiles()),
                DoBuildWithLock);
        }

        private DirectoryInfo GetTestDestinationDirectory(string callingMethod, string identifier)
        {
#if NET451
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#else
            string baseDirectory = AppContext.BaseDirectory;
#endif
            return new DirectoryInfo(Path.Combine(baseDirectory, callingMethod + identifier, _assetName));
        }

        private List<FileInfo> LoadInventory(FileInfo file)
        {
            if (!file.Exists)
            {
                return null;
            }

            var inventory = new List<FileInfo>();

            var lines = file.OpenText();

            while (lines.Peek() > 0)
            {
                inventory.Add(new FileInfo(lines.ReadLine()));
            }

            return inventory;
        }

        private void SaveInventory(FileInfo file, IEnumerable<FileInfo> inventory)
        {
            FileUtility.ReplaceWithLock(
                filePath =>
                {
                    if (!_dataDirectory.Exists)
                    {
                        _dataDirectory.Create();
                    }

                    using (var stream =
                        new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            foreach (var path in inventory.Select(i => i.FullName))
                            {
                                writer.WriteLine(path);
                            }
                        }
                    }
                },
                file.FullName);
        }

        private IEnumerable<FileInfo> GetFileList()
        {
            return _root.GetFiles("*.*", SearchOption.AllDirectories)
                        .Where(f => !_directoriesToExclude.Any(d => d.Contains(f)))
                        .Where(f => !FilesToExclude.Contains(f.Name));    
        }

        private IEnumerable<FileInfo> GetInventory(
            FileInfo file,
            Func<IEnumerable<FileInfo>> beforeAction,
            Action action)
        {
            var inventory = Enumerable.Empty<FileInfo>();
            if (file.Exists)
            {
                inventory = LoadInventory(file);
            }

            if(inventory.Any())
            {
                return inventory;
            }

            IEnumerable<FileInfo> preInventory;

            if (beforeAction == null)
            {
                preInventory = new List<FileInfo>();
            }
            else
            {
                preInventory = beforeAction();
            }

            action();

            inventory = GetFileList().Where(i => !preInventory.Select(p => p.FullName).Contains(i.FullName));

            SaveInventory(file, inventory);

            return inventory;
        }

        private void DoRestoreWithLock()
        {
             Task.Run(async () => await ConcurrencyUtilities.ExecuteWithFileLockedAsync<object>(
                _dataDirectory.FullName, 
                lockedToken =>
                {
                    DoRestore();

                    return Task.FromResult(new Object());
                },
                CancellationToken.None)).Wait();
        }

        private void DoBuildWithLock()
        {
            Task.Run(async () => await ConcurrencyUtilities.ExecuteWithFileLockedAsync<object>(
                _dataDirectory.FullName, 
                lockedToken =>
                {
                    DoBuild();

                    return Task.FromResult(new Object());
                },
                CancellationToken.None)).Wait();
        }

        private void DoRestore()
        {
            Console.WriteLine($"TestAsset Restore '{_assetName}'");

            var projFiles = _root.GetFiles(_projectFilePattern, SearchOption.AllDirectories);

            foreach (var projFile in projFiles)
            {
                var restoreArgs = new string[] { "restore", projFile.FullName };

                var commandResult = Command.Create(_dotnetExeFile.FullName, restoreArgs)
                                    .CaptureStdOut()
                                    .CaptureStdErr()
                                    .Execute();

                int exitCode = commandResult.ExitCode;

                if (exitCode != 0)
                {
                    Console.WriteLine(commandResult.StdOut);

                    Console.WriteLine(commandResult.StdErr);

                    string message = string.Format($"TestAsset Restore '{_assetName}'@'{projFile.FullName}' Failed with {exitCode}");

                    throw new Exception(message);
                }
            }
        }

        private void DoBuild()
        {
            string[] args = new string[] { "build" };

            Console.WriteLine($"TestAsset Build '{_assetName}'");

            var commandResult = Command.Create(_dotnetExeFile.FullName, args) 
                                    .WorkingDirectory(_root.FullName)
                                    .CaptureStdOut()
                                    .CaptureStdErr()
                                    .Execute();

            int exitCode = commandResult.ExitCode;

            if (exitCode != 0)
            {
                Console.WriteLine(commandResult.StdOut);

                Console.WriteLine(commandResult.StdErr);

                string message = string.Format($"TestAsset Build '{_assetName}' Failed with {exitCode}");
                
                throw new Exception(message);
            }
        }

        private void ThrowIfAssetSourcesHaveChanged()
        {
            if (!_dataDirectory.Exists)
            {
                return;
            }

            var dataDirectoryFiles = _dataDirectory.GetFiles("*", SearchOption.AllDirectories);

            if (!dataDirectoryFiles.Any())
            {
                return;
            }

            var trackedFiles = _inventoryFiles.AllInventoryFiles.SelectMany(f => LoadInventory(f));

            var assetFiles = GetFileList();

            var untrackedFiles = assetFiles.Where(a => !trackedFiles.Any(t => t.FullName.Equals(a.FullName)));

            if (untrackedFiles.Any())
            {
                var message = $"TestAsset {_assetName} has untracked files. " +
                    "Consider cleaning the asset and deleting its `.tam` directory to " + 
                    "recreate tracking files.\n\n" +
                    $".tam directory: {_dataDirectory.FullName}\n" +
                    "Untracked Files: \n";

                message += String.Join("\n", untrackedFiles.Select(f => $" - {f.FullName}\n"));

                throw new Exception(message);
            }

            var earliestDataDirectoryTimestamp =
                dataDirectoryFiles
                    .OrderBy(f => f.LastWriteTime)
                    .First()
                    .LastWriteTime;

            if (earliestDataDirectoryTimestamp == null)
            {
                return;
            }

            var updatedSourceFiles = LoadInventory(_inventoryFiles.Source)
                .Where(f => f.LastWriteTime > earliestDataDirectoryTimestamp);

            if (updatedSourceFiles.Any())
            {
                var message = $"TestAsset {_assetName} has updated files. " +
                    "Consider cleaning the asset and deleting its `.tam` directory to " + 
                    "recreate tracking files.\n\n" +
                    $".tam directory: {_dataDirectory.FullName}\n" +
                    "Updated Files: \n";

                message += String.Join("\n", updatedSourceFiles.Select(f => $" - {f.FullName}\n"));

                throw new GracefulException(message);
            }
        }

        private void ThrowIfTestAssetDoesNotExist()
        {
            if (!_root.Exists)
            { 
                throw new DirectoryNotFoundException($"Directory not found at '{_root.FullName}'"); 
            } 
        }
    }
}
