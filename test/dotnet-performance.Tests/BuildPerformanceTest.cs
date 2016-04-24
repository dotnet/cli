// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Tools.Test.Utilities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class BuildPerformanceTest : PerformanceTestBase
    {

        public static IEnumerable<object> SingleProjects
        {
            get
            {
                yield return new [] { "TwoTargetApp"};
                yield return new [] { "SingleTargetApp" };
            }
        }
        public static IEnumerable<object> GraphProjects
        {
            get
            {
                yield return new object[] {
                    "TwoTargetGraph",
                    new[] { "TwoTargetGraph/TwoTargetP0", "TwoTargetGraph/TwoTargetP1", "TwoTargetGraph/TwoTargetP2" }
                };
                yield return new object[]
                {
                    "SingleTargetGraph",
                    new[] { "SingleTargetGraph/SingleTargetP0", "SingleTargetGraph/SingleTargetP1", "SingleTargetGraph/SingleTargetP2" }
                };
            }
        }

        public static IEnumerable<object> GraphProjectsWithFrameworks
        {
            get
            {
                yield return new object[] {
                    "TwoTargetGraph",
                    new[] { "TwoTargetGraph/TwoTargetP0", "TwoTargetGraph/TwoTargetP1", "TwoTargetGraph/TwoTargetP2" },
                    new[] { "netcoreapp1.0", "netstandard1.5"}
                };
                yield return new object[]
                {
                    "SingleTargetGraph",
                    new[] { "SingleTargetGraph/SingleTargetP0", "SingleTargetGraph/SingleTargetP1", "SingleTargetGraph/SingleTargetP2" },
                    new[] { "netcoreapp1.0"}
                };
            }
        }

        [Theory]
        [MemberData(nameof(SingleProjects))]
        public void BuildSingleProject(string project)
        {
            var instance = CreateTestInstance(project);

            Iterate(c =>
            {
                c.Measure(() => Build(instance.TestRoot));
                RemoveBin(instance.TestRoot);
            }, project);
        }

        [Theory]
        [MemberData(nameof(SingleProjects))]
        public void IncrementalSkipSingleProject(string project)
        {
            var instance = CreateTestInstance(project);
            Build(instance.TestRoot);

            Iterate(c =>
            {
                c.Measure(() => Build(instance.TestRoot));
            }, project);
        }

        [Theory]
        [MemberData(nameof(GraphProjects))]
        public void BuildAllInGraph(string variation, string[] projects)
        {
            var instances = projects.Select(p => CreateTestInstance(p, variation)).ToArray();
            var instance = instances[0];

            Iterate(c =>
            {
                c.Measure(() => Build(instance.TestRoot));
                foreach (var i in instances)
                {
                    RemoveBin(i.TestRoot);
                }
            }, variation);
        }

        [Theory]
        [MemberData(nameof(GraphProjects))]
        public void IncrementalSkipAllInGraph(string variation, string[] projects)
        {
            var instances = projects.Select(p => CreateTestInstance(p, variation)).ToArray();
            var instance = instances[0];

            Build(instance.TestRoot);

            Iterate(c =>
            {
                c.Measure(() => Build(instance.TestRoot));
            }, variation);
        }

        [Theory]
        [MemberData(nameof(GraphProjects))]
        public void IncrementalRebuildWithRootChangedInGraph(string variation, string[] projects)
        {
            var instances = projects.Select(p => CreateTestInstance(p, variation)).ToArray();
            var instance = instances[0];

            Build(instance.TestRoot);

            Iterate(c =>
            {
                c.Measure(() => Build(instance.TestRoot));
                RemoveBin(instance.TestRoot);
            }, variation);
        }

        [Theory]
        [MemberData(nameof(GraphProjects))]
        public void IncrementalRebuildWithLastChangedInGraph(string variation, string[] projects)
        {
            var instances = projects.Select(p => CreateTestInstance(p, variation)).ToArray();
            var instance = instances[0];

            Build(instance.TestRoot);

            Iterate(c =>
            {
                c.Measure(() => Build(instance.TestRoot));
                RemoveBin(instances.Last().TestRoot);
            }, variation);
        }


        [Theory]
        [MemberData(nameof(GraphProjectsWithFrameworks))]
        public void IncrementalSkipAllNoDependenciesInGraph(string variation, string[] projects, string[] frameworks)
        {
            var instances = projects.Select(p => CreateTestInstance(p, variation)).ToArray();
            var instance = instances[0];

            Build(instance.TestRoot);

            Iterate(c =>
            {
                foreach (var i in instances)
                {
                    foreach (var framework in frameworks)
                    {
                        c.Measure(() => Run(new BuildCommand(i.TestRoot, framework:framework, noDependencies: true, buildProfile: false)));
                    }
                }
            }, variation);
        }

        [Theory]
        [MemberData(nameof(GraphProjectsWithFrameworks))]
        public void BuildAllNoDependenciesInGraph(string variation, string[] projects, string[] frameworks)
        {
            var instances = projects.Select(p => CreateTestInstance(p, variation)).ToArray();

            Iterate(c =>
            {
                foreach (var i in instances.Reverse())
                {
                    foreach (var framework in frameworks)
                    {
                        c.Measure(() => Run(new BuildCommand(i.TestRoot, framework: framework, noDependencies: true, buildProfile: false)));
                    }
                }
                foreach (var instance in instances)
                {
                    RemoveBin(instance.TestRoot);
                }

            }, variation);
        }
    }
}
