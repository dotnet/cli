// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Xunit.Performance;
using Xunit;
using System.Diagnostics;

namespace Microsoft.DotNet.Tests.Performance
{
    public class HelloWorld : TestBase
    {
        private static readonly string NetCoreAppTfm = "netcoreapp1.0";
        private static readonly string s_expectedOutput = "Hello World!" + Environment.NewLine;
        private static readonly string s_testdirName = "helloworldtestroot";
        private static readonly string s_outputdirName = "test space/bin";

        private static string RestoredTestProjectDirectory { get; set; }

        private string TestDirectory { get; set; }
        private string TestProject { get; set; }
        private string OutputDirectory { get; set; }

        static HelloWorld()
        {
            HelloWorld.SetupStaticTestProject();
        }

        public HelloWorld()
        {
        }

        [Benchmark]
        public void MeasureDotNetBuild()
        {
            foreach (var iter in Benchmark.Iterations)
            {
                // Setup a new instance of the test project.
                TestInstanceSetup();

                // Setup the build command.
                var buildCommand = new BuildCommand(TestProject, output: OutputDirectory, framework: NetCoreAppTfm);
                using (iter.StartMeasurement())
                {
                    // Execute the build command.
                    buildCommand.Execute();
                }
            }
        }

        private void TestInstanceSetup()
        {
            var root = Temp.CreateDirectory();

            var testInstanceDir = root.CopyDirectory(RestoredTestProjectDirectory);

            TestDirectory = testInstanceDir.Path;
            TestProject = Path.Combine(TestDirectory, "project.json");
            OutputDirectory = Path.Combine(TestDirectory, s_outputdirName);
        }

        private static void SetupStaticTestProject()
        {
            RestoredTestProjectDirectory = Path.Combine(AppContext.BaseDirectory, "bin", s_testdirName);

            // Ignore Delete Failure
            try
            {
                Directory.Delete(RestoredTestProjectDirectory, true);
            }
            catch (Exception) { }

            Directory.CreateDirectory(RestoredTestProjectDirectory);
            WriteNuGetConfig(RestoredTestProjectDirectory);

            var currentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(RestoredTestProjectDirectory);

            new NewCommand().Execute().Should().Pass();
            new RestoreCommand().Execute("--quiet").Should().Pass();

            Directory.SetCurrentDirectory(currentDirectory);
        }

        // Todo: this is a hack until corefx is on nuget.org remove this After RC 2 Release
        private static void WriteNuGetConfig(string directory)
        {
            var contents = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
<packageSources>
<!--To inherit the global NuGet package sources remove the <clear/> line below -->
<clear />
<add key=""dotnet-core"" value=""https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"" />
<add key=""api.nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
</packageSources>
</configuration>";

            var path = Path.Combine(directory, "NuGet.config");

            File.WriteAllText(path, contents);
        }
    }
}
