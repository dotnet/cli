// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.Tools.Test.Utilities.Mock;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    internal class FileSystemMockBuilder
    {
        private readonly List<Action> _actions = new List<Action>();
        private MockFileSystemModel _mockFileSystemModel;
        public string TemporaryFolder { get; set; }
        public string WorkingDirectory { get; set; }

        internal static IFileSystem Empty { get; } = Create().Build();

        public static FileSystemMockBuilder Create()
        {
            return new FileSystemMockBuilder();
        }

        public FileSystemMockBuilder AddFile(string name, string content = "")
        {
            _actions.Add(() => _mockFileSystemModel.CreateDirectory(Path.GetDirectoryName(name)));
            _actions.Add(() => _mockFileSystemModel.CreateFile(name, content));
            return this;
        }

        public FileSystemMockBuilder AddFiles(string basePath, params string[] files)
        {
            _actions.Add(() => _mockFileSystemModel.CreateDirectory(basePath));

            foreach (string file in files)
            {
                _actions.Add(() => _mockFileSystemModel.CreateFile(Path.Combine(basePath, file), ""));
            }

            return this;
        }

        /// <summary>
        /// Just a "home" means different path on Windows and Unix.
        /// Create a platform dependent Temporary directory path and use it to avoid further mis interpretation in
        /// later tests. Like "c:/home vs /home". Instead always use Path.Combine(TempraryDirectory, "home")
        /// </summary>
        internal FileSystemMockBuilder UseCurrentSystemTemporaryDirectory()
        {
            TemporaryFolder = Path.GetTempPath();
            return this;
        }

        internal IFileSystem Build()
        {
            _mockFileSystemModel =
                new MockFileSystemModel(TemporaryFolder, fileSystemMockWorkingDirectory: WorkingDirectory);

            foreach (Action action in _actions)
            {
                action();
            }

            return new FileSystemMock(_mockFileSystemModel);
        }

        private class MockFileSystemModel
        {
            public MockFileSystemModel(string temporaryFolder,
                FileSystemRoot files = null,
                string fileSystemMockWorkingDirectory = null)
            {
                if (fileSystemMockWorkingDirectory == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        fileSystemMockWorkingDirectory = @"C:\";
                    }
                    else
                    {
                        fileSystemMockWorkingDirectory = "/";
                    }
                }

                WorkingDirectory = fileSystemMockWorkingDirectory;
                TemporaryFolder =
                    temporaryFolder ?? Path.Combine(fileSystemMockWorkingDirectory, "mockTemporaryFolder");
                Files = files ?? new FileSystemRoot();
                CreateDirectory(WorkingDirectory);
            }

            public string WorkingDirectory { get; }
            public string TemporaryFolder { get; }
            public FileSystemRoot Files { get; }

            public bool TryGetNodeParent(string path, out DirectoryNode current)
            {
                PathModel pathModel = CreateFullPathModel(path);
                current = Files.Volume[pathModel.Volume];

                if (!Files.Volume.ContainsKey(pathModel.Volume))
                {
                    return false;
                }

                for (int i = 0; i < pathModel.PathArray.Length - 1; i++)
                {
                    string p = pathModel.PathArray[i];
                    if (!current.Subs.ContainsKey(p))
                    {
                        return false;
                    }

                    if (current.Subs[p] is DirectoryNode directoryNode)
                    {
                        current = directoryNode;
                    }
                    else if (current.Subs[p] is FileNode)
                    {
                        return false;
                    }
                }

                return true;
            }

            public void CreateDirectory(string path)
            {
                PathModel pathModel = CreateFullPathModel(path);

                DirectoryNode current;
                if (!Files.Volume.ContainsKey(pathModel.Volume))
                {
                    current = new DirectoryNode();
                    Files.Volume[pathModel.Volume] = current;
                }
                else
                {
                    current = Files.Volume[pathModel.Volume];
                }

                foreach (string p in pathModel.PathArray)
                {
                    if (!current.Subs.ContainsKey(p))
                    {
                        DirectoryNode directoryNode = new DirectoryNode();
                        current.Subs[p] = directoryNode;
                        current = directoryNode;
                    }
                    else if (current.Subs[p] is DirectoryNode directoryNode)
                    {
                        current = directoryNode;
                    }
                    else if (current.Subs[p] is FileNode)
                    {
                        throw new IOException(
                            $"Cannot create '{pathModel}' because a file or directory with the same name already exists.");
                    }
                }
            }

            private PathModel CreateFullPathModel(string path)
            {
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(WorkingDirectory, path);
                }

                PathModel pathModel = new PathModel(path);

                return pathModel;
            }

            public void CreateFile(string path, string content)
            {
                PathModel pathModel = CreateFullPathModel(path);

                if (TryGetNodeParent(path, out DirectoryNode current))
                {
                    if (current != null)
                    {
                        if (current.Subs.ContainsKey(pathModel.FileOrDirectoryName()))
                        {
                            IFileSystemTreeNode possibleConflict = current.Subs[pathModel.FileOrDirectoryName()];
                            if (possibleConflict is DirectoryNode)
                            {
                                throw new IOException($"{path} is a directory");
                            }
                        }
                        else
                        {
                            current.Subs[pathModel.FileOrDirectoryName()] = new FileNode(content);
                        }
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {path}. Additional from mock file system, cannot find parent directory");
                }
            }

            public (DirectoryNode, FileNode) GetParentDirectoryAndFileNode(string path, Action onNotAFile)
            {
                if (TryGetNodeParent(path, out DirectoryNode current) && current != null)
                {
                    PathModel pathModel = new PathModel(path);
                    if (current.Subs.ContainsKey(pathModel.FileOrDirectoryName()))
                    {
                        if (!(current.Subs[pathModel.FileOrDirectoryName()] is FileNode fileNode))
                        {
                            onNotAFile();
                        }
                        else
                        {
                            return (current, fileNode);
                        }
                    }
                }

                throw new FileNotFoundException($"Could not find file '{path}'");
            }

            public IEnumerable<string> EnumerateDirectory(
                string path,
                Func<Dictionary<string, IFileSystemTreeNode>, IEnumerable<string>> predicate)
            {
                DirectoryNode current = GetParentOfDirectoryNode(path);

                PathModel pathModel = new PathModel(path);
                DirectoryNode directoryNode = current.Subs[pathModel.FileOrDirectoryName()] as DirectoryNode;

                Debug.Assert(directoryNode != null, nameof(directoryNode) + " != null");

                return predicate(directoryNode.Subs);
            }

            public DirectoryNode GetParentOfDirectoryNode(string path)
            {
                if (!TryGetNodeParent(path, out DirectoryNode current) || current == null)
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {path}");
                }

                PathModel pathModel = CreateFullPathModel(path);

                if (!current.Subs.ContainsKey(pathModel.FileOrDirectoryName()))
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {path}");
                }

                if (current.Subs[pathModel.FileOrDirectoryName()] is FileNode)
                {
                    throw new IOException("Not a directory");
                }

                return current;
            }
        }

        private class PathModel
        {
            public PathModel(string path)
            {
                const char directorySeparatorChar = '\\';
                const char altDirectorySeparatorChar = '/';

                bool isRooted = false;
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException(nameof(path) + ": " + path);
                }

                string volume = "";
                if (Path.IsPathRooted(path))
                {
                    isRooted = true;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        int charLocation = path.IndexOf(":", StringComparison.Ordinal);

                        if (charLocation > 0)
                        {
                            volume = path.Substring(0, charLocation);
                            path = path.Substring(charLocation + 2);
                        }
                    }
                }

                string[] pathArray = path.Split(
                    new[] {directorySeparatorChar, altDirectorySeparatorChar},
                    StringSplitOptions.RemoveEmptyEntries);
                Volume = volume;
                PathArray = pathArray;
                IsRooted = isRooted;
            }

            public PathModel(bool isRooted, string volume, string[] pathArray)
            {
                IsRooted = isRooted;
                Volume = volume ?? throw new ArgumentNullException(nameof(volume));
                PathArray = pathArray ?? throw new ArgumentNullException(nameof(pathArray));
            }

            public bool IsRooted { get; }
            public string Volume { get; }
            public string[] PathArray { get; }

            public override string ToString()
            {
                return $"{nameof(IsRooted)}: {IsRooted}" +
                       $", {nameof(Volume)}: {Volume}" +
                       $", {nameof(PathArray)}: {string.Join("-", PathArray)}";
            }

            public string FileOrDirectoryName()
            {
                return PathArray[PathArray.Length - 1];
            }
        }

        private class FileSystemMock : IFileSystem
        {
            public FileSystemMock(MockFileSystemModel files)
            {
                if (files == null)
                {
                    throw new ArgumentNullException(nameof(files));
                }

                File = new FileMock(files);
                Directory = new DirectoryMock(files);
            }

            public IFile File { get; }

            public IDirectory Directory { get; }
        }

        // facade
        private class FileMock : IFile
        {
            private readonly MockFileSystemModel _files;

            public FileMock(MockFileSystemModel files)
            {
                _files = files ?? throw new ArgumentNullException(nameof(files));
            }

            public bool Exists(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (_files.TryGetNodeParent(path, out DirectoryNode current))
                {
                    if (current != null)
                    {
                        PathModel pathModel = new PathModel(path);
                        return current.Subs.ContainsKey(pathModel.FileOrDirectoryName())
                               && current.Subs[pathModel.FileOrDirectoryName()] is FileNode;
                    }
                }

                return false;
            }

            public string ReadAllText(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (_files.TryGetNodeParent(path, out DirectoryNode current) && current != null)
                {
                    PathModel pathModel = new PathModel(path);
                    if (current.Subs.ContainsKey(pathModel.FileOrDirectoryName()))
                    {
                        if (!(current.Subs[pathModel.FileOrDirectoryName()] is FileNode fileNode))
                        {
                            throw new UnauthorizedAccessException($"Access to the path '{path}' is denied.");
                        }

                        return fileNode.Content;
                    }
                }

                throw new FileNotFoundException($"Could not find file '{path}'");
            }

            public Stream OpenRead(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                return new MemoryStream(Encoding.UTF8.GetBytes(ReadAllText(path)));
            }

            public Stream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare,
                int bufferSize,
                FileOptions fileOptions)
            {
                if (fileMode == FileMode.Open && fileAccess == FileAccess.Read)
                {
                    return OpenRead(path);
                }

                throw new NotImplementedException();
            }

            public void CreateEmptyFile(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                _files.CreateFile(path, string.Empty);
            }

            public void WriteAllText(string path, string content)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (content == null)
                {
                    throw new ArgumentNullException(nameof(content));
                }

                _files.CreateFile(path, content);
            }

            public void Move(string source, string destination)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                (DirectoryNode sourceParent, FileNode sourceFileNode)
                    = _files.GetParentDirectoryAndFileNode(
                        source,
                        () => throw new FileNotFoundException($"Could not find file '{source}'"));

                if (_files.TryGetNodeParent(destination, out DirectoryNode current) && current != null)
                {
                    current.Subs.Add(new PathModel(destination).FileOrDirectoryName(), sourceFileNode);
                    sourceParent.Subs.Remove(new PathModel(source).FileOrDirectoryName());
                }
                else
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {destination}");
                }
            }

            public void Copy(string source, string destination)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                (_, FileNode sourceFileNode) = _files.GetParentDirectoryAndFileNode(source,
                    () => throw new UnauthorizedAccessException($"Access to the path {source} is denied")
                );

                if (_files.TryGetNodeParent(destination, out DirectoryNode current) && current != null)
                {
                    if (current.Subs.ContainsKey(new PathModel(destination).FileOrDirectoryName()))
                    {
                        throw new IOException($"Path {destination} already exists");
                    }

                    current.Subs.Add(new PathModel(destination).FileOrDirectoryName(),
                        new FileNode(sourceFileNode.Content));
                }
                else
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {destination}");
                }
            }

            public void Delete(string path)
            {
                if (_files.TryGetNodeParent(path, out DirectoryNode current))
                {
                    PathModel pathModel = new PathModel(path);
                    if (current.Subs.ContainsKey(pathModel.FileOrDirectoryName()))
                    {
                        current.Subs.Remove(pathModel.FileOrDirectoryName());
                    }
                }
                else
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {path}");
                }
            }
        }

        // facade
        private class DirectoryMock : IDirectory
        {
            private readonly MockFileSystemModel _files;

            public DirectoryMock(MockFileSystemModel files)
            {
                if (files != null)
                {
                    _files = files;
                }
            }

            public bool Exists(string path)
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                if (_files.TryGetNodeParent(path, out DirectoryNode current))
                {
                    if (current != null)
                    {
                        PathModel pathModel = new PathModel(path);

                        return current.Subs.ContainsKey(pathModel.FileOrDirectoryName())
                               && current.Subs[pathModel.FileOrDirectoryName()] is DirectoryNode;
                    }
                }

                return false;
            }

            public ITemporaryDirectory CreateTemporaryDirectory()
            {
                TemporaryDirectoryMock temporaryDirectoryMock = new TemporaryDirectoryMock(_files.TemporaryFolder);
                CreateDirectory(temporaryDirectoryMock.DirectoryPath);
                return temporaryDirectoryMock;
            }

            public IEnumerable<string> EnumerateAllFiles(string path)
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                return _files.EnumerateDirectory(path,
                    subs => subs.Where(s => s.Value is FileNode)
                        .Select(s => Path.Combine(path, s.Key)));
            }

            public IEnumerable<string> EnumerateFileSystemEntries(string path)
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                return _files.EnumerateDirectory(path,
                    subs => subs.Select(s => Path.Combine(path, s.Key)));
            }

            public string GetCurrentDirectory()
            {
                return _files.WorkingDirectory;
            }

            public void CreateDirectory(string path)
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                _files.CreateDirectory(path);
            }

            public void Delete(string path, bool recursive)
            {
                if (path == null) throw new ArgumentNullException(nameof(path));

                DirectoryNode parentOfPath = _files.GetParentOfDirectoryNode(path);
                PathModel pathModel = new PathModel(path);
                if (recursive)
                {
                    parentOfPath.Subs.Remove(pathModel.FileOrDirectoryName());
                }
                else
                {
                    if (EnumerateAllFiles(path).Any())
                    {
                        throw new IOException("Directory not empty");
                    }

                    parentOfPath.Subs.Remove(pathModel.FileOrDirectoryName());
                }
            }

            public void Move(string source, string destination)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                if (destination == null)
                {
                    throw new ArgumentNullException(nameof(destination));
                }

                DirectoryNode sourceParent
                    = _files.GetParentOfDirectoryNode(source);

                PathModel parentPathModel = new PathModel(source);

                IFileSystemTreeNode sourceNode = sourceParent.Subs[parentPathModel.FileOrDirectoryName()];

                if (_files.TryGetNodeParent(destination, out DirectoryNode current) && current != null)
                {
                    PathModel destinationPathModel = new PathModel(destination);
                    if (current.Subs.ContainsKey(destinationPathModel.FileOrDirectoryName()))
                    {
                        if (current.Subs[destinationPathModel.FileOrDirectoryName()] == sourceNode)
                        {
                            throw new IOException("Source and destination path must be different");
                        }

                        throw new IOException($"Cannot create {destination} because a file or" +
                                              " directory with the same name already exists");
                    }

                    current.Subs.Add(destinationPathModel.FileOrDirectoryName(), sourceNode);
                    sourceParent.Subs.Remove(parentPathModel.FileOrDirectoryName());
                }
                else
                {
                    throw new DirectoryNotFoundException($"Could not find a part of the path {destination}");
                }
            }
        }

        private interface IFileSystemTreeNode
        {
            IEnumerable<string> DebugShowTreeLines();
        }

        private class DirectoryNode : IFileSystemTreeNode
        {
            public Dictionary<string, IFileSystemTreeNode> Subs { get; } =
                new Dictionary<string, IFileSystemTreeNode>();

            public IEnumerable<string> DebugShowTreeLines()
            {
                List<string> lines = new List<string>();

                foreach (KeyValuePair<string, IFileSystemTreeNode> fileSystemTreeNode in Subs)
                {
                    lines.Add(fileSystemTreeNode.Key);
                    lines.AddRange(fileSystemTreeNode.Value.DebugShowTreeLines().Select(l => "-- " + l));
                }

                return lines;
            }
        }

        private class FileSystemRoot
        {
            // in Linux there is only one Node, and the name is empty
            public Dictionary<string, DirectoryNode> Volume { get; } = new Dictionary<string, DirectoryNode>();

            public IEnumerable<string> DebugShowTree()
            {
                List<string> lines = new List<string>();

                foreach (KeyValuePair<string, DirectoryNode> fileSystemTreeNode in Volume)
                {
                    lines.Add(fileSystemTreeNode.Key);
                    lines.AddRange(fileSystemTreeNode.Value.DebugShowTreeLines().Select(l => "-- " + l));
                }

                return lines;
            }
        }

        private class FileNode : IFileSystemTreeNode
        {
            public FileNode(string content)
            {
                Content = content ?? throw new ArgumentNullException(nameof(content));
            }

            public string Content { get; }

            public IEnumerable<string> DebugShowTreeLines()
            {
                return new List<string> {Content};
            }
        }

        private class TemporaryDirectoryMock : ITemporaryDirectoryMock
        {
            public TemporaryDirectoryMock(string temporaryDirectory)
            {
                DirectoryPath = temporaryDirectory;
            }

            public bool DisposedTemporaryDirectory { get; private set; }

            public string DirectoryPath { get; }

            public void Dispose()
            {
                DisposedTemporaryDirectory = true;
            }
        }
    }
}
