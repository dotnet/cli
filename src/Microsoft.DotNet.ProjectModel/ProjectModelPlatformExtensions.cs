using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;

namespace Microsoft.DotNet.ProjectModel
{
    public static class ProjectModelPlatformExtensions
    {
        public static HashSet<string> GetPlatformExclusionList(this ProjectContext context, IDictionary<string, LibraryExport> exports)
        {
            var exclusionList = new HashSet<string>();
            var redistPackage = context.PlatformLibrary;
            if (redistPackage == null)
            {
                return exclusionList;
            }

            var redistExport = exports[redistPackage.Identity.Name];

            exclusionList.Add(redistExport.Library.Identity.Name);
            CollectDependencies(exports, redistExport.Library.Dependencies, exclusionList);
            return exclusionList;
        }

        private static void CollectDependencies(IDictionary<string, LibraryExport> exports, IEnumerable<LibraryRange> dependencies, HashSet<string> exclusionList)
        {
            foreach (var dependency in dependencies)
            {
                var export = exports[dependency.Name];
                if (export.Library.Identity.Version.Equals(dependency.VersionRange.MinVersion))
                {
                    exclusionList.Add(export.Library.Identity.Name);
                    CollectDependencies(exports, export.Library.Dependencies, exclusionList);
                }
            }
        }

        public static HashSet<string> GetTypeBuildExclusionList(this ProjectContext context, IDictionary<string, LibraryExport> exports)
        {
            var rootProject = context.RootProject;
            var buildExports = new HashSet<string>();
            var nonBuildExports = new HashSet<string>();

            var nonBuildExportsToSearch = new Stack<string>();
            var buildExportsToSearch = new Stack<string>();

            LibraryExport export;
            string exportName;

            // Root project is non-build
            nonBuildExportsToSearch.Push(rootProject.Identity.Name);
            nonBuildExports.Add(rootProject.Identity.Name);

            // Mark down all nonbuild exports and all of their dependencies
            // Mark down build exports to come back to them later
            while (nonBuildExportsToSearch.Count > 0)
            {
                exportName = nonBuildExportsToSearch.Pop();
                export = exports[exportName];

                foreach (var dependency in export.Library.Dependencies)
                {
                    if (!dependency.Type.Equals(LibraryDependencyType.Build))
                    {
                        if (!nonBuildExports.Contains(dependency.Name))
                        {
                            nonBuildExportsToSearch.Push(dependency.Name);
                            nonBuildExports.Add(dependency.Name);
                        }
                    }
                    else
                    {
                        buildExportsToSearch.Push(dependency.Name);
                    }
                }
            }

            // Go through exports marked build and their dependencies
            // For Exports not marked as non-build, mark them down as build
            while (buildExportsToSearch.Count > 0)
            {
                exportName = buildExportsToSearch.Pop();
                export = exports[exportName];

                buildExports.Add(exportName);

                foreach (var dependency in export.Library.Dependencies)
                {
                    if (!nonBuildExports.Contains(dependency.Name))
                    {
                        buildExportsToSearch.Push(dependency.Name);
                    }
                }
            }

            return buildExports;
        }
        
        public static IEnumerable<LibraryExport> FilterExports(this IEnumerable<LibraryExport> exports, HashSet<string> exclusionList)
        {
            return exports.Where(e => !exclusionList.Contains(e.Library.Identity.Name));
        }
    }
}
