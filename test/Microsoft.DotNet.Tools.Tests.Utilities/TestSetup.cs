// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public class TestSetup
    {
        public string TestRoot { get; private set; }

        private const string ValidRef = "ValidRef";
        public string ValidRefDir => Path.Combine(TestRoot, ValidRef);
        public string ValidRefCsprojName => $"{ValidRef}.csproj";
        public string ValidRefCsprojRelPath => Path.Combine(ValidRef, ValidRefCsprojName);
        public string ValidRefCsprojPath => Path.Combine(TestRoot, ValidRefCsprojRelPath);
        public string ValidRefCsprojRelToOtherProjPath => Path.Combine("..", ValidRefCsprojRelPath);

        private const string Lib = "Lib";
        public string LibDir => Path.Combine(TestRoot, Lib);
        public string LibCsprojName => $"{Lib}.csproj";
        public string LibCsprojPath => Path.Combine(TestRoot, Lib, LibCsprojName);
        public string LibCsprojRelPath => Path.Combine("..", Lib, LibCsprojName);

        public string GetProjectFullPath(string projectDir, string projectName = null)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                projectName = projectDir;
            }

            if (!Path.HasExtension(projectName))
            {
                projectName += ".csproj";
            }
            return Path.Combine(TestRoot, Path.Combine(projectDir, projectName));
        }

        public TestSetup(string testRoot)
        {
            TestRoot = testRoot;
        }
    }
}
