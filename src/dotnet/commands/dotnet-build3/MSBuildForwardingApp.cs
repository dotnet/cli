using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.InternalAbstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli
{
    public class MSBuildForwardingApp : ForwardingApp
    {
        private static readonly string s_msbuildExeName = "MSBuild.exe";

        public MSBuildForwardingApp(string[] argsToForward) :
            base(GetMSBuildExePath(), 
                argsToForward,
                depsFile: GetDepsFile(),
                runtimeConfig: GetRuntimeConfig(),
                environment: GetEnvironment()) { }

        private static Dictionary<string, string> GetEnvironment()
        {
            return new Dictionary<string, string>
            {
                { "DotnetHostPath", GetHostPath() },
                { "BaseNuGetRuntimeIdentifier", GetCurrentBaseRid() }
            };
        }

        private static string GetCurrentBaseRid()
        {
            return RuntimeEnvironment.GetRuntimeIdentifier()
                .Replace("-" + RuntimeEnvironment.RuntimeArchitecture, "");
        }

        private static string GetHostPath()
        {
            return new Muxer().MuxerPath;
        }

        private static string GetRuntimeConfig()
        {
            return Path.Combine(AppContext.BaseDirectory, "dotnet.runtimeconfig.json");
        }

        private static string GetDepsFile()
        {
            return Path.Combine(AppContext.BaseDirectory, "dotnet.deps.json");
        }

        private static string GetMSBuildExePath()
        {
            return Path.Combine(
                AppContext.BaseDirectory,
                "runtimes", "any", "native",
                s_msbuildExeName);
        }

    }
}
