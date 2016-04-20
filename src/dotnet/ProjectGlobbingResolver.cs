using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.DotNet.Tools.Compiler
{
    internal class ProjectGlobbingResolver
    {
        private readonly DirectoryInfoBase _baseDirectory;

        public ProjectGlobbingResolver(DirectoryInfoBase baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        public ProjectGlobbingResolver():this(new DirectoryInfoWrapper(new DirectoryInfo(Directory.GetCurrentDirectory())))
        {
        }

        internal IEnumerable<string> Resolve(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (File.Exists(value))
                {
                    yield return value;
                    continue;
                }

                var fileName = Path.Combine(value, Project.FileName);
                if (File.Exists(fileName))
                {
                    yield return fileName;
                    continue;
                }

                var matcher = new Matcher();
                matcher.AddInclude(value);
                var result = matcher.Execute(_baseDirectory);
                if (result.Files.Any())
                {
                    foreach (var filePatternMatch in result.Files)
                    {
                        yield return filePatternMatch.Path;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Could not resolve project path from '{value}':" +
                                                        "1. It's not project file" +
                                                        "2. It's not directory containing project.json file" +
                                                        "3. Globbing returned no mathces");
                }
            }
        }
    }
}