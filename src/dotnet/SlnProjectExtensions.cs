// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Sln.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Tools.Common
{
    public static class SlnProjectExtensions
    {
        public static IList<string> GetSolutionFoldersFromProject(this SlnProject project)
        {
            var solutionFolders = new List<string>();

            var projectFilePath = project.FilePath;
            if (!projectFilePath.StartsWith(".."))
            {
                var currentDirString = $".{Path.DirectorySeparatorChar}";
                if (projectFilePath.StartsWith(currentDirString))
                {
                    projectFilePath = projectFilePath.Substring(currentDirString.Length);
                }

                var projectDirectoryPath = Path.GetDirectoryName(projectFilePath);
                if (!string.IsNullOrEmpty(projectDirectoryPath))
                {
                    var solutionFoldersPath = Path.GetDirectoryName(projectDirectoryPath);
                    if (!string.IsNullOrEmpty(solutionFoldersPath))
                    {
                        solutionFolders.AddRange(solutionFoldersPath.Split(Path.DirectorySeparatorChar));
                    }
                }
            }

            return solutionFolders;
        }
    }
}
