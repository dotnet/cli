using System.IO;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class BuildPortableTests : TestBase
    {
        private readonly DirectoryInfo _netstandardappOutput;
        
        public BuildPortableTests()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("PortableTests")
                .WithLockFiles();

            var result = new BuildCommand(
                projectPath: Path.Combine(testInstance.TestRoot, "PortableApp"))
                .ExecuteWithCapturedOutput();

            result.Should().Pass();

            var outputBase = new DirectoryInfo(Path.Combine(testInstance.TestRoot, "PortableApp", "bin", "Debug"));

            _netstandardappOutput = outputBase.Sub("netstandard1.5");
        }
        
        [Fact]
        public void BuildingAPortableProjectProducesDepsFile()
        {
            _netstandardappOutput.Should().Exist().And.HaveFile("PortableApp.deps");
        }
        
        [Fact]
        public void BuildingAPortableProjectProducesDepsJsonFile()
        {
            _netstandardappOutput.Should().Exist().And.HaveFile("PortableApp.deps.json");
        }
        
        [Fact]
        public void BuildingAPortableProjectProducesADllFile()
        {
            _netstandardappOutput.Should().Exist().And.HaveFile("PortableApp.dll");
        }
        
        [Fact]
        public void BuildingAPortableProjectProducesAPdbFile()
        {
            _netstandardappOutput.Should().Exist().And.HaveFile("PortableApp.pdb");
        }
        
        [Fact]
        public void BuildingAPortableProjectProducesARuntimeConfigJsonFile()
        {
            _netstandardappOutput.Should().Exist().And.HaveFile("PortableApp.runtimeconfig.json");
        }
        
        [Fact]
        public void BuildingAPortableProjectProducesARuntimeConfigDevJsonFile()
        {
            _netstandardappOutput.Should().Exist().And.HaveFile("PortableApp.runtimeconfig.dev.json");
        }
    }
}
