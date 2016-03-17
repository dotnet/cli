using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextTests
    {
        [Fact]
        public void ResolveRuntimeAssetsReturnsRIDLessAssetsIfNoRIDSpecificAssetsInLibrary()
        {
            var context = BuildTestContext();

            var assets = context.ResolveRuntimeAssets("win7-x64");
            assets.Should().BeEquivalentTo(
                RuntimeAsset.Create("lib\\netstandard1.3\\System.Banana.dll"));
        }

        [Fact]
        public void ResolveRuntimeAssetsReturnsMostSpecificAssetIfRIDSpecificAssetInLibrary()
        {
            var context = BuildTestContext();

            var assets = context.ResolveRuntimeAssets("win81-x64");
            assets.Should().BeEquivalentTo(
                RuntimeAsset.Create("runtimes\\win8\\lib\\netstandard1.3\\System.Banana.dll"));
        }

        [Fact]
        public void ResolveRuntimeAssetsReturnsEmptyIfEmptyRuntimeGroupPresent()
        {
            var context = BuildTestContext();

            var assets = context.ResolveRuntimeAssets("win10-x64");
            assets.Should().BeEmpty();
        }

        [Fact]
        public void ResolveNativeAssetsReturnsEmptyIfNoGroupsMatch()
        {
            var context = BuildTestContext();

            var assets = context.ResolveNativeAssets("win7-x64");
            assets.Should().BeEmpty();
        }

        [Fact]
        public void ResolveNativeAssetsReturnsMostSpecificAssetIfRIDSpecificAssetInLibrary()
        {
            var context = BuildTestContext();

            var assets = context.ResolveNativeAssets("linux-x64");
            assets.Should().BeEquivalentTo(
                RuntimeAsset.Create("runtimes\\linux-x64\\native\\System.Banana.Native.so"));
        }

        [Fact]
        public void ResolveNativeAssetsReturnsEmptyIfEmptyRuntimeGroupPresent()
        {
            var context = BuildTestContext();

            var assets = context.ResolveNativeAssets("rhel-x64");
            assets.Should().BeEmpty();
        }

        private DependencyContext BuildTestContext()
        {
            return new DependencyContext(
                ".NETStandard,Version=v1.3",
                string.Empty,
                isPortable: true,
                compilationOptions: CompilationOptions.Default,
                compileLibraries: new[]
                {
                    new CompilationLibrary("package", "System.Banana", "1.0.0", "hash",
                        new [] { "ref\\netstandard1.3\\System.Banana.dll" },
                        new Dependency[] { },
                        serviceable: false)
                },
                runtimeLibraries: new[] {
                    new RuntimeLibrary("package", "System.Banana", "1.0.0", "hash",
                        new [] {
                            new RuntimeAssetGroup(
                                RuntimeAsset.Create("lib\\netstandard1.3\\System.Banana.dll")),
                            new RuntimeAssetGroup("win10"),
                            new RuntimeAssetGroup("win8",
                                RuntimeAsset.Create("runtimes\\win8\\lib\\netstandard1.3\\System.Banana.dll"))
                        },
                        new [] {
                            new RuntimeAssetGroup("rhel"),
                            new RuntimeAssetGroup("linux-x64",
                                RuntimeAsset.Create("runtimes\\linux-x64\\native\\System.Banana.Native.so")),
                            new RuntimeAssetGroup("osx-x64",
                                RuntimeAsset.Create("runtimes\\osx-x64\\native\\System.Banana.Native.dylib")),

                            // Just here to test we don't fall back through it for the other cases. There's
                            // no such thing as a "unix" native asset since there's no common executable format :)
                            new RuntimeAssetGroup("unix",
                                RuntimeAsset.Create("runtimes\\osx-x64\\native\\System.Banana.Native"))
                        },
                        new ResourceAssembly[] { },
                        new Dependency[] { },
                        serviceable: false)
                },
                runtimeGraph: new[] {
                    new RuntimeFallbacks("win10-x64", "win10", "win81-x64", "win81", "win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win81-x64", "win81", "win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win8-x64", "win8", "win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("win7-x64", "win7", "win-x64", "win", "any", "base"),
                    new RuntimeFallbacks("ubuntu-x64", "ubuntu", "linux-x64", "linux", "unix", "any", "base"),
                    new RuntimeFallbacks("rhel-x64", "rhel", "linux-x64", "linux", "unix", "any", "base"),
                    new RuntimeFallbacks("osx-x64", "osx", "unix", "any", "base"),
                });
        }
    }
}
