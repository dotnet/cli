// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.TestFramework
{
    public class TestAssetInfo
    {
        private const string DataDirectoryName = ".tam";

        private readonly string _assetName;

        private readonly DirectoryInfo _dataDirectory;

        private readonly DirectoryInfo _root;

        private readonly TestAssetInventoryFiles _inventoryFiles;

        internal DirectoryInfo Root 
        {
            get
            {
                return _root;
            }
        }

        internal TestAssetInfo(DirectoryInfo root, string assetName)
        {
            if (!root.Exists)
            {
                throw new DirectoryNotFoundException($"Directory not found at '{root}'");
            }

            _assetName = assetName;

            _root = root;

            _dataDirectory = new DirectoryInfo(Path.Combine(_root.FullName, DataDirectoryName));

            _inventoryFiles = new TestAssetInventoryFiles(_dataDirectory);

            if (!_dataDirectory.Exists)
            {
                _dataDirectory.Create();
            }
        }

        public TestAssetInstance CreateInstance([CallerMemberName] string callingMethod = "", string identifier = "")
        {
            var instancePath = GetTestDestinationDirectoryPath(callingMethod, identifier);

            var testInstance = new TestAssetInstance(this, new DirectoryInfo(instancePath));

            return testInstance;
        }

        private string GetTestDestinationDirectoryPath(string callingMethod, string identifier)
        {
#if NET451
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#else
            string baseDirectory = AppContext.BaseDirectory;
#endif
            return Path.Combine(baseDirectory, callingMethod + identifier, _assetName);
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
            StreamWriter writer;

            if (file.Exists)
            {
                writer = file.AppendText();
            }
            else
            {
                writer = file.CreateText();
            }

            using(writer)
            {
                foreach (var path in inventory.Select(i => i.FullName))
                {
                    writer.WriteLine(path);
                }
            }
        }

        private IEnumerable<FileInfo> GetFileList()
        {
            return _root.GetFiles("*.*", SearchOption.AllDirectories)
                        .Where(f => !f.Name.StartsWith(DataDirectoryName));
        }

        internal IEnumerable<FileInfo> GetSourceFiles()
        {
            return GetInventory(_inventoryFiles.Source, () => new List<FileInfo>(), () => {});
        }

        internal IEnumerable<FileInfo> GetRestoreFiles()
        {
            return GetInventory(_inventoryFiles.Restore, GetSourceFiles, DoRestore);
        }

        internal IEnumerable<FileInfo> GetBuildFiles()
        {
            return GetInventory(_inventoryFiles.Build, GetRestoreFiles, DoBuild);
        }

        private IEnumerable<FileInfo> GetInventory(FileInfo file, Func<IEnumerable<FileInfo>> getPreInventory, Action create)
        {
            if (file.Exists)
            {
                return LoadInventory(file);
            }

            var preInventory = getPreInventory();

            create();

            var inventory = GetFileList().Where(i => !preInventory.Select(p => p.FullName).Contains(i.FullName));

            SaveInventory(file, inventory);

            return inventory;
        }

        private void DoRestore()
        {
            string[] restoreArgs = new string[] { "restore" };

            Console.WriteLine($"TestAsset Restore '{_assetName}'");

            var commandResult = Command.Create(new PathCommandResolverPolicy(), "dotnet", restoreArgs)
                                       .WorkingDirectory(_root.FullName)
                                       .CaptureStdOut()
                                       .CaptureStdErr()
                                       .Execute();

            int exitCode = commandResult.ExitCode;

            if (exitCode != 0)
            {
                Console.WriteLine(commandResult.StdOut);
                Console.WriteLine(commandResult.StdErr);
                string message = string.Format($"TestAsset Restore '{_assetName}' Failed with {exitCode}");
                throw new Exception(message);
            }
        }

        private void DoBuild()
        {
            string[] args = new string[] { "build" };

            Console.WriteLine($"TestAsset Build '{_assetName}'");

            var commandResult = Command.Create(new PathCommandResolverPolicy(), "dotnet", args) 
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
    }
}
