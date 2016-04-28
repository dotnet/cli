using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.RuntimeModel;

namespace Microsoft.DotNet.Cli.Build
{
    public class NuGetRidUtils
    {
        private static readonly string s_runtimeJsonRelativePath = "scripts/dotnet-cli-build/runtime-graph.json";

        private RuntimeGraph _runtimeGraph;

        public NuGetRidUtils(string repoRootDir) : this(repoRootDir, NuGetRidUtils.s_runtimeJsonRelativePath) { }

        public NuGetRidUtils(string repoRootDir, string runtimeJsonRelativePath)
        {
            _runtimeGraph = ParseRuntimeGraph(
                Path.Combine(repoRootDir, runtimeJsonRelativePath));
        }

        public bool RidsAreCompatible(string hostRid, string projectRid)
        {
            return hostRid.Equals(projectRid, StringComparison.Ordinal) 
                ? true 
                : _runtimeGraph.AreCompatible(hostRid, projectRid);
        }

        private RuntimeGraph ParseRuntimeGraph(string runtimeGraphJsonFile)
        {
            return JsonRuntimeFormat.ReadRuntimeGraph(runtimeGraphJsonFile);
        }
    }
}
