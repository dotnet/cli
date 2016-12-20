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
        public void It_migrates_basic_PackageReference()
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
        public void It_migrates_type_build_to_PrivateAssets()
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
        public void It_migrates_suppress_parent_array_to_PrivateAssets()
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
        public void It_migrates_suppress_parent_string_to_PrivateAssets()
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
        public void It_migrates_include_exclude_arrays_to_IncludeAssets()
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
        public void It_migrates_include_string_to_IncludeAssets()
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
        public void It_migrates_include_exclude_overlapping_strings_to_IncludeAssets()
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
        public void It_migrates_Tools()
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
        public void It_migrates_imports_per_framework()
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
        public void It_auto_add_desktop_references_during_migrate()
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
        public void It_migrates_test_projects_to_have_test_sdk()
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
                      i.GetMetadataWithName("Version").Value == "15.0.0-preview-20161024-02" &&
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
        public void It_migrates_test_projects_to_have_test_sdk_and_xunit_packagedependencies()
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
                      i.GetMetadataWithName("Version").Value == "15.0.0-preview-20161024-02") &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute);

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "xunit" && 
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "2.2.0-beta4-build3444" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "xunit.runner.visualstudio" && 
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "2.2.0-beta4-build1194" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestAdapter" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestFramework" && i.ItemType == "PackageReference"));
        }

        [Fact]
        public void It_migrates_test_projects_to_have_test_sdk_and_xunit_packagedependencies_overwrite_existing_packagedependencies()
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
                      i.GetMetadataWithName("Version").Value == "15.0.0-preview-20161024-02" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "xunit" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "2.2.0-beta4-build3444" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "xunit.runner.visualstudio" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "2.2.0-beta4-build1194" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestAdapter" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "MSTest.TestFramework" && i.ItemType == "PackageReference"));
        }

        [Fact]
        public void It_migrates_test_projects_to_have_test_sdk_and_mstest_packagedependencies()
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
                      i.GetMetadataWithName("Version").Value == "15.0.0-preview-20161024-02" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "MSTest.TestAdapter" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "1.1.3-preview" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "MSTest.TestFramework" &&
                      i.ItemType == "PackageReference" &&
                      i.GetMetadataWithName("Version").Value == "1.0.4-preview" &&
                      i.GetMetadataWithName("Version").ExpressedAsAttribute));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "xunit" && i.ItemType == "PackageReference"));

            mockProj.Items.Should().NotContain(
                i => (i.Include == "xunit.runner.visualstudio" && i.ItemType == "PackageReference"));
        }

        [Theory]
        [InlineData(@"
            {
              ""frameworks"": {
                ""netstandard1.3"": {
                    ""dependencies"": {
                        ""System.AppContext"": ""4.1.0"",
                        ""NETStandard.Library"": ""1.5.0""
                    }
                }
              }
            }")]
        [InlineData(@"
            {
              ""frameworks"": {
                ""netstandard1.3"": {
                    ""dependencies"": {
                        ""System.AppContext"": ""4.1.0""
                    }
                }
              }
            }")]
        public void It_migrates_library_and_does_not_double_netstandard_ref(string pjContent)
        {
            var mockProj = RunPackageDependenciesRuleOnPj(pjContent);

            mockProj.Items.Should().ContainSingle(
                i => (i.Include == "NETStandard.Library" && i.ItemType == "PackageReference"));
        }

        new private void EmitsPackageReferences(ProjectRootElement mockProj, params Tuple<string, string, string>[] packageSpecs)
        {
            foreach (var packageSpec in packageSpecs)
            {
                var packageName = packageSpec.Item1;
                var packageVersion = packageSpec.Item2;
                var packageTFM = packageSpec.Item3;

                var items = mockProj.Items
                    .Where(i => i.ItemType == "PackageReference")
                    .Where(i => string.IsNullOrEmpty(packageTFM) || i.ConditionChain().Any(c => c.Contains(packageTFM)))
                    .Where(i => i.Include == packageName)
                    .Where(i => i.GetMetadataWithName("Version").Value == packageVersion &&
                                i.GetMetadataWithName("Version").ExpressedAsAttribute);

                items.Should().HaveCount(1);
            }
        }

        new private void EmitsToolReferences(ProjectRootElement mockProj, params Tuple<string, string>[] toolSpecs)
        {
            foreach (var toolSpec in toolSpecs)
            {
                var packageName = toolSpec.Item1;
                var packageVersion = toolSpec.Item2;

                var items = mockProj.Items
                    .Where(i => i.ItemType == "DotNetCliToolReference")
                    .Where(i => i.Include == packageName)
                    .Where(i => i.GetMetadataWithName("Version").Value == packageVersion &&
                                i.GetMetadataWithName("Version").ExpressedAsAttribute);

                items.Should().HaveCount(1);
            }
        }

        new private ProjectRootElement RunPackageDependenciesRuleOnPj(string s, string testDirectory = null)
        {
            testDirectory = testDirectory ?? Temp.CreateDirectory().Path;
            return TemporaryProjectFileRuleRunner.RunRules(new IMigrationRule[]
            {
                new MigratePackageDependenciesAndToolsRule()
            }, s, testDirectory);
        }
    }
}