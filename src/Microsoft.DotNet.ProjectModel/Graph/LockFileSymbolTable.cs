using System;
using System.Collections.Concurrent;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public class LockFileSymbolTable
    {
        private ConcurrentDictionary<string, NuGetVersion> _versionTable = new ConcurrentDictionary<string, NuGetVersion>(StringComparer.Ordinal);
        private ConcurrentDictionary<string, VersionRange> _versionRangeTable = new ConcurrentDictionary<string, VersionRange>(StringComparer.Ordinal);
        private ConcurrentDictionary<string, NuGetFramework> _frameworksTable = new ConcurrentDictionary<string, NuGetFramework>(StringComparer.Ordinal);
        private ConcurrentDictionary<string, string> _stringsTable = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        public NuGetVersion GetVersion(string versionString) => _versionTable.GetOrAdd(versionString, (s) => NuGetVersion.Parse(s));
        public VersionRange GetVersionRange(string versionRangeString) => _versionRangeTable.GetOrAdd(versionRangeString, (s) => VersionRange.Parse(s));
        public NuGetFramework GetFramework(string frameworkString) => _frameworksTable.GetOrAdd(frameworkString, (s) => NuGetFramework.Parse(s));
        public string GetString(string frameworkString) => _stringsTable.GetOrAdd(frameworkString, frameworkString);
    }
}
