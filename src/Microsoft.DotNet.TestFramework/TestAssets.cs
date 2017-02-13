﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.TestFramework
{
    public class TestAssets
    {
        private DirectoryInfo _root;

        private FileInfo _dotnetCsprojExe;

        private FileInfo _dotnetProjectJsonExe;

        private const string ProjectJsonSearchPattern = "project.json";

        private const string CsprojSearchPattern = "*.csproj";

        public TestAssets(DirectoryInfo assetsRoot, FileInfo dotnetCsprojExe, FileInfo dotnetProjectJsonExe)
        {
            if (assetsRoot == null)
            {
                throw new ArgumentNullException(nameof(assetsRoot));
            }

            if (dotnetCsprojExe == null)
            {
                throw new ArgumentNullException(nameof(dotnetCsprojExe));
            }

            if (dotnetProjectJsonExe == null)
            {
                throw new ArgumentNullException(nameof(dotnetProjectJsonExe));
            }
            
            if (!assetsRoot.Exists)
            {
                throw new DirectoryNotFoundException($"Directory not found at '{assetsRoot}'");
            }

            if (!dotnetCsprojExe.Exists)
            {
                throw new FileNotFoundException("Csproj dotnet executable must exist", dotnetCsprojExe.FullName);
            }

            if (!dotnetProjectJsonExe.Exists)
            {
                throw new FileNotFoundException("project.json dotnet executable must exist", dotnetProjectJsonExe.FullName);
            }

            _root = assetsRoot;

            _dotnetCsprojExe = dotnetCsprojExe;

            _dotnetProjectJsonExe = dotnetProjectJsonExe;
        }

        public TestAssetInfo Get(string name)
        {
            return Get(TestAssetKinds.TestProjects, name);
        }

        public TestAssetInfo Get(string kind, string name)
        {
            var assetDirectory = new DirectoryInfo(Path.Combine(_root.FullName, kind, name));

            return new TestAssetInfo(
                assetDirectory, 
                name, 
                _dotnetCsprojExe,
                CsprojSearchPattern);
        }

        public TestAssetInfo GetProjectJson(string name)
        {
            return GetProjectJson(TestAssetKinds.TestProjects, name);
        }

        public TestAssetInfo GetProjectJson(string kind, string name)
        {
            var assetDirectory = new DirectoryInfo(Path.Combine(_root.FullName, kind, name));

            return new TestAssetInfo(
                assetDirectory, 
                name, 
                _dotnetProjectJsonExe,
                ProjectJsonSearchPattern);
        }

        public DirectoryInfo CreateTestDirectory(string testProjectName = "temp", [CallerMemberName] string callingMethod = "", string identifier = "")
        {
            var testDestination = GetTestDestinationDirectoryPath(testProjectName, callingMethod, identifier);

            var testDirectory = new DirectoryInfo(testDestination);

            testDirectory.EnsureExistsAndEmpty();

            return testDirectory;
        }

        private string GetTestDestinationDirectoryPath(string testProjectName, string callingMethod, string identifier)
        {
#if NET451
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#else
            string baseDirectory = AppContext.BaseDirectory;
#endif
            return Path.Combine(baseDirectory, callingMethod + identifier, testProjectName);
        }
    }
}
