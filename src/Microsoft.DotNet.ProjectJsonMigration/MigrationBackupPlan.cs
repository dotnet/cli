// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Internal.ProjectModel.Utilities;

namespace Microsoft.DotNet.ProjectJsonMigration
{
    internal class MigrationBackupPlan
    {
        private readonly FileInfo globalJson;
        private readonly Dictionary<DirectoryInfo, IEnumerable<FileInfo>> mapOfProjectBackupDirectoryToFilesToMove;

        public DirectoryInfo RootBackupDirectory { get; }
        public DirectoryInfo[] ProjectBackupDirectories { get; }

        public IEnumerable<FileInfo> FilesToMove(DirectoryInfo projectBackupDirectory)
            => mapOfProjectBackupDirectoryToFilesToMove[projectBackupDirectory];

        public MigrationBackupPlan(
            IEnumerable<DirectoryInfo> projectDirectories,
            DirectoryInfo workspaceDirectory,
            Func<DirectoryInfo, IEnumerable<FileInfo>> getFiles = null)
        {
            if (projectDirectories == null)
            {
                throw new ArgumentNullException(nameof(projectDirectories));
            }

            if (workspaceDirectory == null)
            {
                throw new ArgumentNullException(nameof(workspaceDirectory));
            }

            projectDirectories = projectDirectories.Select(pd => new DirectoryInfo(pd.FullName.EnsureTrailingSlash()));
            workspaceDirectory = new DirectoryInfo(workspaceDirectory.FullName.EnsureTrailingSlash());

            globalJson = new FileInfo(
                Path.Combine(
                    workspaceDirectory.FullName,
                    "global.json"));

            RootBackupDirectory = new DirectoryInfo(
                GetUniqueDirectoryPath(
                    Path.Combine(
                        workspaceDirectory.FullName,
                        "backup"))
                    .EnsureTrailingSlash());

            var projectBackupDirectories = new List<DirectoryInfo>();
            mapOfProjectBackupDirectoryToFilesToMove = new Dictionary<DirectoryInfo, IEnumerable<FileInfo>>();
            getFiles = getFiles ?? (dir => dir.EnumerateFiles());

            foreach (var projectDirectory in projectDirectories)
            {
                var projectBackupDirectory = ComputeProjectBackupDirectoryPath(workspaceDirectory, projectDirectory, RootBackupDirectory);
                var filesToMove = getFiles(projectDirectory).Where(NeedsBackup);

                projectBackupDirectories.Add(projectBackupDirectory);
                mapOfProjectBackupDirectoryToFilesToMove.Add(projectBackupDirectory, filesToMove);
            }

            ProjectBackupDirectories = projectBackupDirectories.ToArray();
        }

        public void PerformBackup()
        {
            if (globalJson.Exists)
            {
                PathUtility.EnsureDirectoryExists(RootBackupDirectory.FullName);

                globalJson.MoveTo(
                    Path.Combine(
                        RootBackupDirectory.FullName,
                        globalJson.Name));
            }

            foreach (var kvp in mapOfProjectBackupDirectoryToFilesToMove)
            {
                var projectBackupDirectory = kvp.Key;
                var filesToMove = kvp.Value;

                PathUtility.EnsureDirectoryExists(projectBackupDirectory.FullName);

                foreach (var file in filesToMove)
                {
                    file.MoveTo(
                        Path.Combine(
                            projectBackupDirectory.FullName,
                            file.Name));
                }
            }
        }

        private static DirectoryInfo ComputeProjectBackupDirectoryPath(
            DirectoryInfo workspaceDirectory, DirectoryInfo projectDirectory, DirectoryInfo rootBackupDirectory)
        {
            if (PathUtility.IsChildOfDirectory(workspaceDirectory.FullName, projectDirectory.FullName))
            {
                var relativePath = PathUtility.GetRelativePath(
                    workspaceDirectory.FullName,
                    projectDirectory.FullName);

                return new DirectoryInfo(
                    Path.Combine(
                            rootBackupDirectory.FullName,
                            relativePath)
                        .EnsureTrailingSlash());
            }

            // For projects whose directory is not a child of the workspace directory (is that even possible?)
            // Ensure that we use a unique name to avoid collisions as a fallback.
            return new DirectoryInfo(
                GetUniqueDirectoryPath(
                    Path.Combine(
                            rootBackupDirectory.FullName,
                            projectDirectory.Name)
                        .EnsureTrailingSlash()));
        }

        private static bool NeedsBackup(FileInfo file)
            => file.Name == "project.json"
            || file.Extension == ".xproj"
            || file.FullName.EndsWith(".xproj.user")
            || file.FullName.EndsWith(".lock.json");

        private static string GetUniqueDirectoryPath(string directoryPath)
        {
            var candidatePath = directoryPath;

            var suffix = 1;
            while (Directory.Exists(candidatePath))
            {
                candidatePath = $"{directoryPath}_{suffix++}";
            }

            return candidatePath;
        }
    }
}
