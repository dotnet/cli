using System.IO;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using Microsoft.DotNet.TestFramework;

namespace Microsoft.DotNet.Kestrel.Tests
{
    public class DotnetBuildTest : TestBase
    {
        [Fact]
        public void BuildingKestrelPortableFatAppProducesExpectedArtifacts()
        {
            var testDirectory = TestAssets.Get(Path.Combine("KestrelSample", "KestrelPortable"))
                .WithSourceFiles()
                .WithRestoreFiles()
                .WithBuildFiles();

            string appName = Path.GetFileName(testRoot);

            var netcoreAppOutput = testDirectory.GetDirectory("bin", "Debug", "netcoreapp1.0");

            netcoreAppOutput
                .Should().Exist()
                     .And.OnlyHaveFiles(new[]
                        {
                            $"{appName}.deps.json",
                            $"{appName}.dll",
                            $"{appName}.pdb",
                            $"{appName}.runtimeconfig.json",
                            $"{appName}.runtimeconfig.dev.json"
                        });
        }
    }
}
