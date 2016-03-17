using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class RuntimeAssemblyTests
    {
        [Fact]
        public void UsesFileNameAsAssemblyNameInCreate()
        {
            var assembly = RuntimeAsset.Create("path/to/System.Collections.dll");
            assembly.Name.Should().Be("System.Collections");
        }

        [Fact]
        public void TrimsDotNiFromDllNames()
        {
            var assembly = RuntimeAsset.Create("path/to/System.Collections.ni.dll");
            assembly.Name.Should().Be("System.Collections");
        }
    }
}
