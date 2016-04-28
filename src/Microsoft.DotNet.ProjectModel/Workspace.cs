﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Utilities;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    /// <summary>
    /// Represents a cache of Projects, LockFiles, and ProjectContexts
    /// </summary>
    public abstract class Workspace
    {
        // key: project directory
        private readonly ConcurrentDictionary<string, FileModelEntry<Project>> _projectsCache
                   = new ConcurrentDictionary<string, FileModelEntry<Project>>();

        // key: project directory
        private readonly ConcurrentDictionary<string, FileModelEntry<LockFile>> _lockFileCache
                   = new ConcurrentDictionary<string, FileModelEntry<LockFile>>();

        // key: project directory, target framework
        private readonly ConcurrentDictionary<string, ProjectContextCollection> _projectContextsCache
                   = new ConcurrentDictionary<string, ProjectContextCollection>();

        private readonly ProjectReaderSettings _settings;
        private readonly LockFileReader _lockFileReader;

        protected Workspace(ProjectReaderSettings settings)
        {
            _settings = settings;
            _lockFileReader = new LockFileReader();
        }

        public ProjectContext GetProjectContext(string projectPath, NuGetFramework framework)
        {
            var contexts = GetProjectContextCollection(projectPath);
            if (contexts == null)
            {
                return null;
            }

            return contexts
                .ProjectContexts
                .FirstOrDefault(c => Equals(c.TargetFramework, framework) && string.IsNullOrEmpty(c.RuntimeIdentifier));
        }

        public ProjectContextCollection GetProjectContextCollection(string projectPath)
        {
            var normalizedPath = NormalizeProjectPath(projectPath);
            if (normalizedPath == null)
            {
                return null;
            }

            return _projectContextsCache.AddOrUpdate(
                normalizedPath,
                key => AddProjectContextEntry(key, null),
                (key, oldEntry) => AddProjectContextEntry(key, oldEntry));
        }

        public Project GetProject(string projectDirectory) => GetProjectCore(projectDirectory)?.Model;

        private LockFile GetLockFile(string projectDirectory)
        {
            var normalizedPath = NormalizeProjectPath(projectDirectory);
            if (normalizedPath == null)
            {
                return null;
            }

            return _lockFileCache.AddOrUpdate(
                normalizedPath,
                key => AddLockFileEntry(key, null),
                (key, oldEntry) => AddLockFileEntry(key, oldEntry)).Model;
        }


        private FileModelEntry<Project> GetProjectCore(string projectDirectory)
        {
            var normalizedPath = NormalizeProjectPath(projectDirectory);
            if (normalizedPath == null)
            {
                return null;
            }

            return _projectsCache.AddOrUpdate(
                normalizedPath,
                key => AddProjectEntry(key, null),
                (key, oldEntry) => AddProjectEntry(key, oldEntry));
        }


        protected static string NormalizeProjectPath(string path)
        {
            if (File.Exists(path) &&
                string.Equals(Path.GetFileName(path), Project.FileName, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(Path.GetDirectoryName(path));
            }
            else if (Directory.Exists(path) &&
                     File.Exists(Path.Combine(path, Project.FileName)))
            {
                return Path.GetFullPath(path);
            }

            return null;
        }

        private FileModelEntry<Project> AddProjectEntry(string projectDirectory, FileModelEntry<Project> currentEntry)
        {
            if (currentEntry == null)
            {
                currentEntry = new FileModelEntry<Project>();
            }
            else if (!File.Exists(Path.Combine(projectDirectory, Project.FileName)))
            {
                // project was deleted
                currentEntry.Reset();
                return currentEntry;
            }

            if (currentEntry.IsInvalid)
            {
                Project project;
                if (!ProjectReader.TryGetProject(projectDirectory, out project, _settings))
                {
                    currentEntry.Reset();
                }
                else
                {
                    currentEntry.Diagnostics.AddRange(project.Diagnostics);
                    currentEntry.Model = project;
                    currentEntry.FilePath = project.ProjectFilePath;
                    currentEntry.UpdateLastWriteTimeUtc();
                }
            }

            return currentEntry;
        }

        private FileModelEntry<LockFile> AddLockFileEntry(string projectDirectory, FileModelEntry<LockFile> currentEntry)
        {
            if (currentEntry == null)
            {
                currentEntry = new FileModelEntry<LockFile>();
            }

            if (currentEntry.IsInvalid)
            {
                currentEntry.Reset();

                if (!File.Exists(Path.Combine(projectDirectory, LockFile.FileName)))
                {
                    return currentEntry;
                }
                else
                {
                    currentEntry.FilePath = Path.Combine(projectDirectory, LockFile.FileName);

                    using (var fs = ResilientFileStreamOpener.OpenFile(currentEntry.FilePath, retry: 2))
                    {
                        try
                        {
                            currentEntry.Model = _lockFileReader.ReadLockFile(currentEntry.FilePath, fs, designTime: true);
                            currentEntry.UpdateLastWriteTimeUtc();
                        }
                        catch (FileFormatException ex)
                        {
                            throw ex.WithFilePath(currentEntry.FilePath);
                        }
                        catch (Exception ex)
                        {
                            throw FileFormatException.Create(ex, currentEntry.FilePath);
                        }
                    }
                }
            }

            return currentEntry;
        }

        private ProjectContextCollection AddProjectContextEntry(string projectDirectory,
                                                                ProjectContextCollection currentEntry)
        {
            if (currentEntry == null)
            {
                // new entry required
                currentEntry = new ProjectContextCollection();
            }

            var projectEntry = GetProjectCore(projectDirectory);

            if (projectEntry?.Model == null)
            {
                // project doesn't exist anymore
                currentEntry.Reset();
                return currentEntry;
            }

            var project = projectEntry.Model;
            if (currentEntry.HasChanged)
            {
                currentEntry.Reset();

                var contexts = BuildProjectContexts(project);

                currentEntry.ProjectContexts.AddRange(contexts);

                currentEntry.Project = project;
                currentEntry.ProjectFilePath = project.ProjectFilePath;
                currentEntry.LastProjectFileWriteTimeUtc = File.GetLastWriteTimeUtc(currentEntry.ProjectFilePath);

                var lockFilePath = Path.Combine(project.ProjectDirectory, LockFile.FileName);
                if (File.Exists(lockFilePath))
                {
                    currentEntry.LockFilePath = lockFilePath;
                    currentEntry.LastLockFileWriteTimeUtc = File.GetLastWriteTimeUtc(lockFilePath);
                }

                currentEntry.ProjectDiagnostics.AddRange(projectEntry.Diagnostics);
            }

            return currentEntry;
        }

        protected abstract IEnumerable<ProjectContext> BuildProjectContexts(Project project);

        /// <summary>
        /// Creates a ProjectContextBuilder configured to use the Workspace caches.
        /// </summary>
        /// <returns></returns>
        protected ProjectContextBuilder CreateBaseProjectBuilder()
        {
            return new ProjectContextBuilder()
                .WithReaderSettings(_settings)
                .WithProjectResolver(path => GetProjectCore(path)?.Model)
                .WithLockFileResolver(path => GetLockFile(path));
        }

        /// <summary>
        /// Creates a ProjectContextBuilder configured to use the Workspace caches, and the specified root project.
        /// </summary>
        /// <param name="root">The root project</param>
        /// <returns></returns>
        protected ProjectContextBuilder CreateBaseProjectBuilder(Project root)
        {
            return CreateBaseProjectBuilder().WithProject(root);
        }

        protected class FileModelEntry<TModel> where TModel : class
        {
            private DateTime _lastWriteTimeUtc;

            public TModel Model { get; set; }

            public string FilePath { get; set; }

            public List<DiagnosticMessage> Diagnostics { get; } = new List<DiagnosticMessage>();

            public void UpdateLastWriteTimeUtc()
            {
                _lastWriteTimeUtc = File.GetLastWriteTimeUtc(FilePath);
            }

            public bool IsInvalid
            {
                get
                {
                    if (Model == null)
                    {
                        return true;
                    }

                    if (!File.Exists(FilePath))
                    {
                        return true;
                    }

                    return _lastWriteTimeUtc < File.GetLastWriteTimeUtc(FilePath);
                }
            }

            public void Reset()
            {
                Model = null;
                FilePath = null;
                Diagnostics.Clear();
                _lastWriteTimeUtc = DateTime.MinValue;
            }
        }

    }
}
