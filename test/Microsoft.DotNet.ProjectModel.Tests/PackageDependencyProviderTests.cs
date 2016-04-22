using System.Linq;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Resolution;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class PackageDependencyProviderTests : TestBase
    {
        [Fact]
        public void GetDescriptionShouldNotModifyTarget()
        {
            var provider = new PackageDependencyProvider("/foo/packages", new FrameworkReferenceResolver("/foo/references"));
            var package = new LockFilePackageLibrary();
            package.Name = "Something";
            package.Version = NuGetVersion.Parse("1.0.0");
            package.Files.Add("lib/dotnet/_._");
            package.Files.Add("runtimes/any/native/Microsoft.CSharp.CurrentVersion.targets");

            var target = new LockFileTargetLibrary();
            target.Name = "Something";
            target.Version = package.Version;

            target.RuntimeAssemblies.Add("lib/dotnet/_._");
            target.CompileTimeAssemblies.Add("lib/dotnet/_._");
            target.NativeLibraries.Add("runtimes/any/native/Microsoft.CSharp.CurrentVersion.targets");

            var p1 = provider.GetDescription(NuGetFramework.Parse("netcoreapp1.0"), package, target);
            var p2 = provider.GetDescription(NuGetFramework.Parse("netcoreapp1.0"), package, target);

            Assert.True(p1.Compatible);
            Assert.True(p2.Compatible);

            Assert.Empty(p1.CompileTimeAssemblies);
            Assert.Empty(p1.RuntimeAssemblies);

            Assert.Empty(p2.CompileTimeAssemblies);
            Assert.Empty(p2.RuntimeAssemblies);
        }

        [Theory]
        [InlineData("TestMscorlibReference", true)]
        [InlineData("TestMscorlibReference", false)]
        [InlineData("TestMicrosoftCSharpReference", true)]
        [InlineData("TestMicrosoftCSharpReference", false)]
        [InlineData("TestSystemReference", true)]
        [InlineData("TestSystemReference", false)]
        [InlineData("TestSystemCoreReference", true)]
        [InlineData("TestSystemCoreReference", false)]
        public void TestDuplicateDefaultDesktopReferences(string sampleName, bool withLockFile)
        {
            var instance = TestAssetsManager.CreateTestInstance(sampleName);
            if (withLockFile)
            {
                instance = instance.WithLockFiles();
            }

            var context = new ProjectContextBuilder().WithProjectDirectory(instance.TestRoot)
                                                     .WithTargetFramework("net451")
                                                     .Build();

            Assert.Equal(4, context.RootProject.Dependencies.Count());
        }

        [Fact]
        public void NoDuplicateReferencesWhenFrameworkMissing()
        {
            var instance = TestAssetsManager.CreateTestInstance("TestMicrosoftCSharpReferenceMissingFramework")
                                            .WithLockFiles();

            var context = new ProjectContextBuilder().WithProjectDirectory(instance.TestRoot)
                                                     .WithTargetFramework("net99")
                                                     .Build();

            // Will fail with dupes if any
            context.LibraryManager.GetLibraries().ToDictionary(l => l.Identity.Name);
        }

        [Fact]
        public void NetCore50ShouldNotResolveFrameworkAssemblies()
        {
            var instance = TestAssetsManager.CreateTestInstance("TestMicrosoftCSharpReferenceMissingFramework")
                                            .WithLockFiles();

            var context = new ProjectContextBuilder().WithProjectDirectory(instance.TestRoot)
                                                     .WithTargetFramework("netcore50")
                                                     .Build();

            var diagnostics = context.LibraryManager.GetAllDiagnostics();
            Assert.False(diagnostics.Any(d => d.ErrorCode == ErrorCodes.DOTNET1011));
        }
    }
}
