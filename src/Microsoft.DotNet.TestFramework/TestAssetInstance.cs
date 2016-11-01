﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;

namespace Microsoft.DotNet.TestFramework
{
    public class TestAssetInstance
    {
        private TestAssetInfo _testAssetInfo;

        private DirectoryInfo _root;

        public DirectoryInfo Root
        {
            get
            {
                return _root;
            }
        }
        
        public TestAssetInstance(TestAssetInfo testAssetInfo, DirectoryInfo root)
        {
            _testAssetInfo = testAssetInfo;

            _root = root;

            if (root.Exists)
            {
                root.Delete(recursive: true);
            }

            root.Create();
        }

        public TestAssetInstance WithSourceFiles()
        {
            var filesToCopy = _testAssetInfo.GetSourceFiles();

            CopyFiles(filesToCopy);

            return this;
        }

        public TestAssetInstance WithRestoreFiles()
        {
            var filesToCopy = _testAssetInfo.GetRestoreFiles();

            CopyFiles(filesToCopy);

            return this;
        }

        public TestAssetInstance WithBuildFiles()
        {
            var filesToCopy = _testAssetInfo.GetBuildFiles();

            CopyFiles(filesToCopy);

            return this;
        }

        public TestAssetInstance WithNuGetConfig(string nugetCache)
        {
            var thisAssembly = typeof(TestAssetInstance).GetTypeInfo().Assembly;
            var newNuGetConfig = Root.GetFile("Nuget.config");

            var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <configuration>
              <packageSources>
                <add key=""test-packages"" value=""$fullpath$"" />
              </packageSources>
            </configuration>";
            content = content.Replace("$fullpath$", nugetCache);
            
            using (var newNuGetConfigStream =
                new FileStream(newNuGetConfig.FullName, FileMode.Create, FileAccess.Write))
            {
                var contentBytes = new UTF8Encoding(true).GetBytes(content);
                newNuGetConfigStream.Write(contentBytes, 0, contentBytes.Length);
            }

            return this;
        }

        private void CopyFiles(IEnumerable<FileInfo> filesToCopy)
        {
            foreach (var file in filesToCopy)
            {
                var relativePath = file.FullName.Substring(_testAssetInfo.Root.FullName.Length + 1);

                var newPath = Path.Combine(Root.FullName, relativePath);

                var newFile = new FileInfo(newPath);

                PathUtility.EnsureDirectory(newFile.Directory.FullName);

                file.CopyTo(newPath);
            }

        }
    }
}
