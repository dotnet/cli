using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.RuntimeModel;
using System.Text;

namespace Microsoft.DotNet.Cli.Build
{
    public class StubHostPackageGenerator
    {
        private static readonly Dictionary<string, string[]> s_hostPackageRidToNativeFileNames = new Dictionary<string, string[]>()
        {
            { "win7-x64", new string[] { "dotnet.exe", "hostfxr.dll", "hostpolicy.dll" } },
            { "win7-x86", new string[] { "dotnet.exe", "hostfxr.dll", "hostpolicy.dll" } },
            { "osx.10.10-x64", new string[] { "dotnet", "libhostfxr.dylib", "libhostpolicy.dylib" } },
            { "osx.10.11-x64", new string[] { "dotnet", "libhostfxr.dylib", "libhostpolicy.dylib" } },
            { "ubuntu.14.04-x64", new string[] { "dotnet", "libhostfxr.so", "libhostpolicy.so" } },
            { "centos.7-x64", new string[] { "dotnet", "libhostfxr.so", "libhostpolicy.so" } },
            { "rhel.7-x64", new string[] { "dotnet", "libhostfxr.so", "libhostpolicy.so" } },
            { "rhel.7.2-x64", new string[] { "dotnet", "libhostfxr.so", "libhostpolicy.so" } },
            { "debian.8-x64", new string[] { "dotnet", "libhostfxr.so", "libhostpolicy.so" } }
        };

        private NugetRidUtils _nugetRidUtility;

        public StubHostPackageGenerator(NugetRidUtils nugetRidUtility)
        {
            _nugetRidUtility = nugetRidUtility;
        }

        public void GenerateStubPackagesForAllRidsExceptCurrent(
            string currentRid, 
            Dictionary<string, string> hostPackageIdToVersion,
            string outputDir)
        {

            foreach (var hostPackageId in hostPackageIdToVersion)
            {
                foreach (var ridToNativeFileNames in s_hostPackageRidToNativeFileNames)
                {
                    string rid = ridToNativeFileNames.Key;
                    string[] files = ridToNativeFileNames.Value;

                    if (! _nugetRidUtility.RidsAreCompatible(currentRid, rid))
                    {
                        CreateDummyRuntimeNuGetPackage(
                            DotNetCli.Stage0,
                            hostPackageId.Key,
                            rid,
                            files,
                            hostPackageId.Value,
                            outputDir);
                    }
                }
            }
        }

        private void CreateDummyRuntimeNuGetPackage(DotNetCli dotnet, string basePackageId, string rid, string[] files, string version, string outputDir)
        {
            var packageId = $"runtime.{rid}.{basePackageId}";

            var tempPjDirectory = Path.Combine(Dirs.Intermediate, "dummyNuGetPackageIntermediate", rid);
            
            FS.Mkdirp(tempPjDirectory);
            
            File.WriteAllText(Path.Combine(tempPjDirectory, "Program.cs"), "class Program { static void Main(string[] args) {} }");
            
            string[] absoluteFileNames = new string[files.Length];
            foreach (string file in files)
            {
                string absolutePath = Path.Combine(tempPjDirectory, file);
                File.WriteAllText(absolutePath, "<this file is created during the dotnet/cli build>");
            }
            
            var projectJson = new StringBuilder();
            projectJson.AppendLine("{");
            projectJson.AppendLine($"  \"version\": \"{version}\",");
            projectJson.AppendLine($"  \"name\": \"{packageId}\",");
            projectJson.AppendLine("  \"dependencies\": { \"NETStandard.Library\": \"1.5.0-rc2-24022\" },");
            projectJson.AppendLine("  \"frameworks\": { \"netcoreapp1.0\": {}, \"netstandard1.5\": {} },");
            projectJson.AppendLine($"  \"runtimes\": {{ \"{rid}\": {{ }} }},");
            projectJson.AppendLine("  \"packInclude\": {");
            projectJson.AppendLine($"    \"runtimes/{rid}/native/\": [{string.Join(",", from path in files select $"\"{path}\"".Replace("\\", "/"))}]");
            projectJson.AppendLine("  }");
            projectJson.AppendLine("}");

            var tempPjFile = Path.Combine(tempPjDirectory, "project.json");

            File.WriteAllText(tempPjFile, projectJson.ToString());

            dotnet.Restore()
                .WorkingDirectory(tempPjDirectory)
                .Execute()
                .EnsureSuccessful();
                
            dotnet.Pack(
                tempPjFile,
                "--output", outputDir)
                .WorkingDirectory(tempPjDirectory)
                .Execute()
                .EnsureSuccessful();
        }

    }
}
