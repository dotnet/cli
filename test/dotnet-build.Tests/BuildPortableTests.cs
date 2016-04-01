using System.IO;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using Microsoft.DotNet.TestFramework;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class BuildPortableTests : TestBase
    {
        public static string PortableApp { get; } = "PortableApp";
        public static string PortableAppRoot { get; } = Path.Combine("PortableTests", PortableApp);
        public static string KestrelPortableApp { get; } = "KestrelHelloWorldPortable";

        [Fact]
        public void BuildingPortableAppProducesExpectedArtifacts()
        {
            var testInstance = TestAssetsManager.CreateTestInstance(PortableAppRoot)
                .WithLockFiles();

            BuildAndTest(testInstance.TestRoot);
        }

        [Fact]
        public void BuildingKestrelPortableFatAppProducesExpectedArtifacts()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("KestrelHelloWorld")
                .WithLockFiles();

            BuildAndTest(Path.Combine(testInstance.TestRoot, KestrelPortableApp));
        }

        private static void BuildAndTest(string testRoot)
        {
            string appName = Path.GetFileName(testRoot);


            var result = new BuildCommand(
                projectPath: testRoot)
                .ExecuteWithCapturedOutput();

            result.Should().Pass();

            var outputBase = new DirectoryInfo(Path.Combine(testRoot, "bin", "Debug"));

            var netstandardappOutput = outputBase.Sub("netstandard1.5");

            netstandardappOutput.Should()
                .Exist().And
                .OnlyHaveFiles(new[]
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
