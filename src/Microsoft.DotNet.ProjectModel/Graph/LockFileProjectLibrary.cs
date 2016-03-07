// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFileProjectLibrary
    {
        public LockFileProjectLibrary(string name, NuGetVersion version, string path)
        {
            Name = name;
            Version = version;
            Path = path;
        }

        public string Name { get; }

        public NuGetVersion Version { get; }

        public string Path { get; }
    }
}