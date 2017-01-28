// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Construction;
using Microsoft.DotNet.Tools.Test.Utilities;
using System.Linq;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.ProjectJsonMigration;
using Microsoft.DotNet.ProjectJsonMigration.Rules;
using System;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenThatIWantToMigratePackageDependencies : PackageDependenciesTestBase
    {
        [Fact]
        public void ItMigratesBasicPackageReference()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""dependencies"": {
                        ""APackage"" : ""1.0.0-preview"",
                        ""BPackage"" : ""1.0.0""
                    }
                }");

            EmitsPackageReferences(mockProj, Tuple.Create("APackage", "1.0.0-preview", ""), Tuple.Create("BPackage", "1.0.0", ""));
        }

        [Fact]
        public void ItMigratesTypeBuildToPrivateAssets()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""dependencies"": {
                        ""APackage"" : {
                            ""version"": ""1.0.0-preview"",
                            ""type"": ""build""
                        }
                    }
                }");


            var packageRef = mockProj.Items.First(i => i.Include == "APackage" && i.ItemType == "PackageReference");

            var privateAssetsMetadata = packageRef.GetMetadataWithName("PrivateAssets");
            privateAssetsMetadata.Value.Should().NotBeNull();
            privateAssetsMetadata.Value.Should().Be("All");
        }

        [Fact]
        public void ItMigratesSuppressParentArrayToPrivateAssets()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""dependencies"": {
                        ""APackage"" : {
                            ""version"": ""1.0.0-preview"",
                            ""suppressParent"":[ ""runtime"", ""native"" ]
                        }
                    }
                }");
            var packageRef = mockProj.Items.First(i => i.Include == "APackage" && i.ItemType == "PackageReference");

            var privateAssetsMetadata = packageRef.GetMetadataWithName("PrivateAssets");
            privateAssetsMetadata.Value.Should().NotBeNull();
            privateAssetsMetadata.Value.Should().Be("Native;Runtime");
        }

        [Fact]
        public void ItMigratesSuppressParentStringToPrivateAssets()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""dependencies"": {
                        ""APackage"" : {
                            ""version"": ""1.0.0-preview"",
                            ""suppressParent"":""runtime""
                        }
                    }
                }");
            var packageRef = mockProj.Items.First(i => i.Include == "APackage" && i.ItemType == "PackageReference");

            var privateAssetsMetadata = packageRef.GetMetadataWithName("PrivateAssets");
            privateAssetsMetadata.Value.Should().NotBeNull();
            privateAssetsMetadata.Value.Should().Be("Runtime");
        }

        [Fact]
        public void ItMigratesIncludeExcludeArraysToIncludeAssets()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""dependencies"": {
                        ""APackage"" : {
                            ""version"": ""1.0.0-preview"",
                            ""include"": [ ""compile"", ""runtime"", ""native"" ],
                            ""exclude"": [ ""native"" ]
                        }
                    }
                }");
            var packageRef = mockProj.Items.First(i => i.Include == "APackage" && i.ItemType == "PackageReference");

            var includeAssetsMetadata = packageRef.GetMetadataWithName("IncludeAssets");
            includeAssetsMetadata.Value.Should().NotBeNull();
            includeAssetsMetadata.Value.Should().Be("Compile;Runtime");
        }

        [Fact]
        public void ItMigratesIncludeStringToIncludeAssets()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""dependencies"": {
                        ""APackage"" : {
                            ""version"": ""1.0.0-preview"",
                            ""include"": ""compile"",
                            ""exclude"": ""runtime""
                        }
                    }
                }");
            var packageRef = mockProj.Items.First(i => i.Include == "APackage" && i.ItemType == "PackageReference");

            var includeAssetsMetadata = packageRef.GetMetadataWithName("IncludeAssets");
            includeAssetsMetadata.Value.Should().NotBeNull();
            includeAssetsMetadata.Value.Should().Be("Compile");
        }

        [Fact]
        public void ItMigratesIncludeExcludeOverlappingStringsToIncludeAssets()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""dependencies"": {
                        ""APackage"" : {
                            ""version"": ""1.0.0-preview"",
                            ""include"": ""compile"",
                            ""exclude"": ""compile"",
                        }
                    }
                }");
            var packageRef = mockProj.Items.First(i => i.Include == "APackage" && i.ItemType == "PackageReference");

            var includeAssetsMetadata = packageRef.GetMetadataWithName("IncludeAssets");
            includeAssetsMetadata.Value.Should().NotBeNull();
            includeAssetsMetadata.Value.Should().Be("None");
        }


        [Fact]
        public void ItMigratesTools()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""tools"": {
                        ""APackage"" : ""1.0.0-preview"",
                        ""BPackage"" : ""1.0.0""
                    }
                }");

            EmitsToolReferences(mockProj, Tuple.Create("APackage", "1.0.0-preview"), Tuple.Create("BPackage", "1.0.0"));
        }

        [Fact]
        public void ItMigratesImportsPerFramework()
        {
            var importPropertyName = "PackageTargetFallback";

            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""frameworks"": {
                        ""netcoreapp1.0"" : {
                            ""imports"": [""netstandard1.3"", ""net451""]
                        },
                        ""netstandard1.3"" : {
                            ""imports"": [""net451""]
                        },
                        ""net451"" : {
                            ""imports"": ""netstandard1.3""
                        }
                    }                    
                }");

            var imports = mockProj.Properties.Where(p => p.Name == importPropertyName);
            imports.Should().HaveCount(3);

            var netcoreappImport = imports.First(p => p.Condition.Contains("netcoreapp1.0"));
            var netstandardImport = imports.First(p => p.Condition.Contains("netstandard1.3"));
            var net451Import = imports.First(p => p.Condition.Contains("net451"));

            netcoreappImport.Should().NotBe(netstandardImport);

            netcoreappImport.Condition.Should().Be(" '$(TargetFramework)' == 'netcoreapp1.0' ");
            netstandardImport.Condition.Should().Be(" '$(TargetFramework)' == 'netstandard1.3' ");
            net451Import.Condition.Should().Be(" '$(TargetFramework)' == 'net451' ");

            netcoreappImport.Value.Split(';').Should().BeEquivalentTo($"$({importPropertyName})", "netstandard1.3", "net451");
            netstandardImport.Value.Split(';').Should().BeEquivalentTo($"$({importPropertyName})", "net451");
            net451Import.Value.Split(';').Should().BeEquivalentTo($"$({importPropertyName})", "netstandard1.3");
        }

        [Fact]
        public void ItDoesNotAddConditionToPackageTargetFallBackWhenMigratingASingleTFM()
        {
            var importPropertyName = "PackageTargetFallback";

            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""frameworks"": {
                        ""netcoreapp1.0"" : {
                            ""imports"": [""netstandard1.3"", ""net451""]
                        }
                    }
                }");

            var imports = mockProj.Properties.Where(p => p.Name == importPropertyName);
            imports.Should().HaveCount(1);

            imports.Single().Condition.Should().BeEmpty();
        }

        [Fact]
        public void ItAutoAddDesktopReferencesDuringMigrate()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"                
                {
                    ""frameworks"": {
                        ""net35"" : {},
                        ""net4"" : {},
                        ""net451"" : {}
                    }
                }");

            var itemGroup = mockProj.ItemGroups.Where(i => i.Condition == " '$(TargetFramework)' == 'net451' ");
            itemGroup.Should().HaveCount(1);
            itemGroup.First().Items.Should().HaveCount(2);
            var items = itemGroup.First().Items.ToArray();
            items[0].Include.Should().Be("System");
            items[1].Include.Should().Be("Microsoft.CSharp");

            itemGroup = mockProj.ItemGroups.Where(i => i.Condition == " '$(TargetFramework)' == 'net40' ");
            itemGroup.Should().HaveCount(1);
            itemGroup.First().Items.Should().HaveCount(2);
            items = itemGroup.First().Items.ToArray();
            items[0].Include.Should().Be("System");
            items[1].Include.Should().Be("Microsoft.CSharp");

            itemGroup = mockProj.ItemGroups.Where(i => i.Condition == " '$(TargetFramework)' == 'net35' ");
            itemGroup.Should().HaveCount(1);
            itemGroup.First().Items.Should().HaveCount(1);
            items = itemGroup.First().Items.ToArray();
            items[0].Include.Should().Be("System");
        }

        [Fact]
        public void ItMigratesTestProjectsToHaveTestSdk()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    },
                    ""testRunner"": ""somerunner""
                }");

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "Microsoft.NET.Test.Sdk" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "15.0.0-preview-20170106-08" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "xunit" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "xunit.runner.visualstudio" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestAdapter" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestFramework" && i.ItemType == "PackageReference"));
        }

        [Fact]
        public void ItMigratesTestProjectsToHaveTestSdkAndXunitPackagedependencies()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    },
                    ""testRunner"": ""xunit""
                }");

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "Microsoft.NET.Test.Sdk" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "15.0.0-preview-20170106-08") &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute);

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "xunit" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "2.2.0-beta5-build3474" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "xunit.runner.visualstudio" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "2.2.0-beta5-build1225" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestAdapter" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestFramework" && i.ItemType == "PackageReference"));
        }

        [Fact]
        public void ItMigratesTestProjectsToHaveTestSdkAndXunitPackagedependenciesOverwriteExistingPackagedependencies()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""dependencies"": {
                        ""xunit"": ""2.2.0-beta3-build3330""
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    },
                    ""testRunner"": ""xunit""
                }");

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "Microsoft.NET.Test.Sdk" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "15.0.0-preview-20170106-08" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "xunit" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "2.2.0-beta5-build3474" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "xunit.runner.visualstudio" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "2.2.0-beta5-build1225" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestAdapter" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestFramework" && i.ItemType == "PackageReference"));
        }

        [Fact]
        public void ItMigratesTestProjectsToHaveTestSdkAndMstestPackagedependencies()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    },
                    ""testRunner"": ""mstest""
                }");

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "Microsoft.NET.Test.Sdk" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "15.0.0-preview-20170106-08" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "MSTest.TestAdapter" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "1.1.8-rc" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "MSTest.TestFramework" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "1.0.8-rc" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "xunit" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "xunit.runner.visualstudio" && i.ItemType == "PackageReference"));
        }

        [Fact]
        public void ItMigratesMicrosoftNETCoreAppMetaPackageToRuntimeFrameworkVersionProperty()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(
                @"{ ""dependencies"": { ""Microsoft.NETCore.App"" : { ""version"": ""1.1.0"", ""type"": ""build"" } } }");

            mockProj.Items.Should().NotContain(
                i => i.Include == "Microsoft.NETCore.App" && i.ItemType == "PackageReference");
            mockProj.Properties.Should().ContainSingle(p => p.Name == "RuntimeFrameworkVersion").Which.Value.Should().Be("1.1.0");
        }

        [Fact]
        public void ItMigratesMicrosoftNETCoreAppMetaPackageToRuntimeFrameworkVersionPropertyConditionedOnTFMWhenMultiTFM()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"
            {
              ""frameworks"": {
                ""netcoreapp1.0"": {
                    ""dependencies"": {
                        ""Microsoft.NETCore.App"": ""1.1.0""
                    }
                },
                ""netcoreapp1.1"": {
                }
              }
            }");

            mockProj.Items.Should().NotContain(
                i => i.Include == "Microsoft.NETCore.App" && i.ItemType == "PackageReference");
            var runtimeFrameworkVersion = mockProj.Properties.Should().ContainSingle(p => p.Name == "RuntimeFrameworkVersion").Which;
            runtimeFrameworkVersion.Value.Should().Be("1.1.0");
            runtimeFrameworkVersion.Condition.Should().Contain("netcoreapp1.0");
        }

        [Fact]
        public void ItMigratesMicrosoftNETCoreAppMetaPackageToRuntimeFrameworkVersionPropertyWithNoConditionedOnTFMWhenSingleTFM()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"
            {
              ""frameworks"": {
                ""netcoreapp1.0"": {
                    ""dependencies"": {
                        ""Microsoft.NETCore.App"": ""1.1.0""
                    }
                }
              }
            }");

            mockProj.Items.Should().NotContain(
                i => i.Include == "Microsoft.NETCore.App" && i.ItemType == "PackageReference");
            var runtimeFrameworkVersion = mockProj.Properties.Should().ContainSingle(p => p.Name == "RuntimeFrameworkVersion").Which;
            runtimeFrameworkVersion.Value.Should().Be("1.1.0");
            runtimeFrameworkVersion.Condition.Should().BeEmpty();
        }

        [Fact]
        public void ItMigratesNETStandardLibraryMetaPackageToNetStandardImplicitPackageVersionProperty()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(
                @"{ ""dependencies"": { ""NETStandard.Library"" : { ""version"": ""1.6.0"", ""type"": ""build"" } } }");

            mockProj.Items.Should().NotContain(
                i => i.Include == "NETStandard.Library" && i.ItemType == "PackageReference");
            mockProj.Properties.Should().ContainSingle(p => p.Name == "NetStandardImplicitPackageVersion").Which.Value.Should().Be("1.6.0");
        }

        [Fact]
        public void ItMigratesNETStandardLibraryMetaPackageToNetStandardImplicitPackageVersionPropertyConditionedOnTFMWhenMultiTFM()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"
            {
              ""frameworks"": {
                ""netstandard1.3"": {
                    ""dependencies"": {
                        ""NETStandard.Library"": ""1.6.0""
                    }
                },
                ""netstandard1.5"": {
                }
              }
            }");

            mockProj.Items.Should().NotContain(
                i => i.Include == "NETStandard.Library" && i.ItemType == "PackageReference");
            var netStandardImplicitPackageVersion =
                mockProj.Properties.Should().ContainSingle(p => p.Name == "NetStandardImplicitPackageVersion").Which;
            netStandardImplicitPackageVersion.Value.Should().Be("1.6.0");
            netStandardImplicitPackageVersion.Condition.Should().Contain("netstandard1.3");
        }

        [Fact]
        public void ItMigratesNETStandardLibraryMetaPackageToNetStandardImplicitPackageVersionPropertyWithNoConditionOnTFMWhenSingleTFM()
        {
            var mockProj = RunPackageDependenciesRuleOnPj(@"
            {
              ""frameworks"": {
                ""netstandard1.3"": {
                    ""dependencies"": {
                        ""NETStandard.Library"": ""1.6.0""
                    }
                }
              }
            }");

            mockProj.Items.Should().NotContain(
                i => i.Include == "NETStandard.Library" && i.ItemType == "PackageReference");
            var netStandardImplicitPackageVersion =
                mockProj.Properties.Should().ContainSingle(p => p.Name == "NetStandardImplicitPackageVersion").Which;
            netStandardImplicitPackageVersion.Value.Should().Be("1.6.0");
            netStandardImplicitPackageVersion.Condition.Should().BeEmpty();
        }
    }
}