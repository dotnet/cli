// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Extensions.TestHost
{
    public class Project
    {
        private const string ProjectFileName = "project.json";

        public string Name { get; set; }

        public IDictionary<string, string> Commands { get; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static bool TryGetProject(string inputPath, out Project project)
        {
            string projectName;
            var projectJsonPath = GetProjectJsonPath(inputPath, out projectName);

            if (!File.Exists(projectJsonPath))
            {
                project = null;
                return false;
            }

            using (var stream = File.OpenRead(projectJsonPath))
            {
                project = GetProject(stream, projectJsonPath, projectName);
            }

            return true;
        }

        // To enable unit testing
        internal static string GetProjectJsonPath(string inputPath, out string projectName)
        {
            string projectDirectoryPath;
            if (string.Equals(Path.GetFileName(inputPath), ProjectFileName, StringComparison.OrdinalIgnoreCase))
            {
                // Example: for a path like "C:\github\mvc\project.json", the following api would return
                // "C:\github\mvc"
                projectDirectoryPath = Path.GetDirectoryName(inputPath);
            }
            else
            {
                projectDirectoryPath = inputPath;
            }

            // Assume the directory name is the project name if none was specified
            projectName = GetDirectoryName(projectDirectoryPath);

            return Path.GetFullPath(Path.Combine(projectDirectoryPath, ProjectFileName));
        }

        // To enable unit testing
        internal static Project GetProject(Stream stream, string projectJsonPath, string projectName)
        {
            Project project;

            try
            {
                var seriliazer = new JsonSerializer();
                project = seriliazer.Deserialize<Project>(new JsonTextReader(new StreamReader(stream)));
                project.Name = projectName;
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidOperationException(
                    $"The JSON file '{projectJsonPath}' can't be deserialized to a JSON object.", innerException: ex);
            }

            return project;
        }

        private static string GetDirectoryName(string path)
        {
            path = path.TrimEnd(Path.DirectorySeparatorChar);

            return path
                .Substring(Path.GetDirectoryName(path).Length)
                .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
