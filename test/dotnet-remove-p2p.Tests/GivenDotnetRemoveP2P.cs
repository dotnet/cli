// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Cli.Remove.P2P.Tests
{
    public class GivenDotnetRemoveP2P : TestBase
    {
        const string TestGroup = "NonRestoredTestProjects";
        const string ProjectName = "DotnetAddP2PProjects";
        const string FrameworkNet451Arg = "-f net451";
        const string ConditionFrameworkNet451 = "== 'net451'";
        const string FrameworkNetCoreApp10Arg = "-f netcoreapp1.0";
        const string ConditionFrameworkNetCoreApp10 = "== 'netcoreapp1.0'";

        private ProjDir GetLibRef(TestSetup setup)
        {
            return new ProjDir(setup.LibDir);
        }

        private ProjDir AddLibRef(TestSetup setup, ProjDir proj, string additionalArgs = "")
        {
            var ret = GetLibRef(setup);
            new AddP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(proj.CsProjPath)
                .Execute($"{additionalArgs} \"{ret.CsProjPath}\"")
                .Should().Pass();

            return ret;
        }

        private ProjDir AddValidRef(TestSetup setup, ProjDir proj, string frameworkArg = "")
        {
            var ret = new ProjDir(setup.ValidRefDir);
            new AddP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(proj.CsProjPath)
                .Execute($"{frameworkArg} \"{ret.CsProjPath}\"")
                .Should().Pass();

            return ret;
        }

        [Theory]
        [InlineData("--help")]
        [InlineData("-h")]
        public void WhenHelpOptionIsPassedItPrintsUsage(string helpArg)
        {
            var cmd = new RemoveP2PCommand().Execute(helpArg);
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("Usage");
        }

        [Theory]
        [InlineData("idontexist.csproj")]
        [InlineData("ihave?inv@lid/char\\acters")]
        public void WhenNonExistingProjectIsPassedItPrintsErrorAndUsage(string projName)
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new RemoveP2PCommand()
                    .WithWorkingDirectory(setup.TestRoot)
                    .WithProject(projName)
                    .Execute($"\"{setup.ValidRefCsprojPath}\"");
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain("Could not find");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void WhenBrokenProjectIsPassedItPrintsErrorAndUsage()
        {
            string projName = "Broken/Broken.csproj";
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new RemoveP2PCommand()
                    .WithWorkingDirectory(setup.TestRoot)
                    .WithProject(projName)
                    .Execute($"\"{setup.ValidRefCsprojPath}\"");
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain(" is invalid.");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void WhenMoreThanOneProjectExistsInTheDirectoryItPrintsErrorAndUsage()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new RemoveP2PCommand()
                    .WithWorkingDirectory(Path.Combine(setup.TestRoot, "MoreThanOne"))
                    .Execute($"\"{setup.ValidRefCsprojRelToOtherProjPath}\"");
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain("more than one");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void WhenNoProjectsExistsInTheDirectoryItPrintsErrorAndUsage()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);

            var cmd = new RemoveP2PCommand()
                    .WithWorkingDirectory(setup.TestRoot)
                    .Execute($"\"{setup.ValidRefCsprojPath}\"");
            cmd.ExitCode.Should().NotBe(0);
            cmd.StdErr.Should().Contain("not find any");
            cmd.StdOut.Should().Contain("Usage");
        }

        [Fact]
        public void ItRemovesRefWithoutCondAndPrintsStatus()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var libref = AddLibRef(setup, lib);

            int noCondBefore = lib.CsProj().NumberOfItemGroupsWithoutCondition();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{libref.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeContaining(libref.Name).Should().Be(0);
        }

        [Fact]
        public void ItRemovesRefWithCondAndPrintsStatus()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var libref = AddLibRef(setup, lib, FrameworkNet451Arg);

            int condBefore = lib.CsProj().NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNet451);
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"{FrameworkNet451Arg} \"{libref.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNet451).Should().Be(condBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeAndConditionContaining(libref.Name, ConditionFrameworkNet451).Should().Be(0);
        }

        [Fact]
        public void WhenTwoDifferentRefsArePresentItDoesNotRemoveBoth()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var libref = AddLibRef(setup, lib);
            var validref = AddValidRef(setup, lib);

            int noCondBefore = lib.CsProj().NumberOfItemGroupsWithoutCondition();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{libref.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            cmd.StdOut.Should().NotContain(validref.Name);
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore);
            csproj.NumberOfProjectReferencesWithIncludeContaining(libref.Name).Should().Be(0);
        }

        [Fact]
        public void WhenRefWithoutCondIsNotThereItPrintsMessage()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var libref = GetLibRef(setup);

            string csprojContetntBefore = lib.CsProjContent();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{libref.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("could not be found");
            lib.CsProjContent().Should().BeEquivalentTo(csprojContetntBefore);
        }

        [Fact]
        public void WhenRefWithCondIsNotThereItPrintsMessage()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var libref = GetLibRef(setup);

            string csprojContetntBefore = lib.CsProjContent();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"{FrameworkNet451Arg} \"{libref.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().Contain("could not be found");
            lib.CsProjContent().Should().BeEquivalentTo(csprojContetntBefore);
        }

        [Fact]
        public void WhenRefWithAndWithoutCondArePresentAndRemovingNoCondItDoesNotRemoveOther()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var librefCond = AddLibRef(setup, lib, FrameworkNet451Arg);
            var librefNoCond = AddLibRef(setup, lib);

            var csprojBefore = lib.CsProj();
            int noCondBefore = csprojBefore.NumberOfItemGroupsWithoutCondition();
            int condBefore = csprojBefore.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNet451);
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{librefNoCond.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeContaining(librefNoCond.Name).Should().Be(0);

            csproj.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNet451).Should().Be(condBefore);
            csproj.NumberOfProjectReferencesWithIncludeAndConditionContaining(librefCond.Name, ConditionFrameworkNet451).Should().Be(1);
        }

        [Fact]
        public void WhenRefWithAndWithoutCondArePresentAndRemovingCondItDoesNotRemoveOther()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var librefCond = AddLibRef(setup, lib, FrameworkNet451Arg);
            var librefNoCond = AddLibRef(setup, lib);

            var csprojBefore = lib.CsProj();
            int noCondBefore = csprojBefore.NumberOfItemGroupsWithoutCondition();
            int condBefore = csprojBefore.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNet451);
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"{FrameworkNet451Arg} \"{librefCond.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore);
            csproj.NumberOfProjectReferencesWithIncludeContaining(librefNoCond.Name).Should().Be(1);

            csproj.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNet451).Should().Be(condBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeAndConditionContaining(librefCond.Name, ConditionFrameworkNet451).Should().Be(0);
        }

        [Fact]
        public void WhenRefWithDifferentCondIsPresentItDoesNotRemoveIt()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var librefCondNet451 = AddLibRef(setup, lib, FrameworkNet451Arg);
            var librefCondNetCoreApp10 = AddLibRef(setup, lib, FrameworkNetCoreApp10Arg);

            var csprojBefore = lib.CsProj();
            int condNet451Before = csprojBefore.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNet451);
            int condNetCoreApp10Before = csprojBefore.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNetCoreApp10);
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"{FrameworkNet451Arg} \"{librefCondNet451.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNet451).Should().Be(condNet451Before - 1);
            csproj.NumberOfProjectReferencesWithIncludeAndConditionContaining(librefCondNet451.Name, ConditionFrameworkNet451).Should().Be(0);

            csproj.NumberOfItemGroupsWithConditionContaining(ConditionFrameworkNetCoreApp10).Should().Be(condNetCoreApp10Before);
            csproj.NumberOfProjectReferencesWithIncludeAndConditionContaining(librefCondNetCoreApp10.Name, ConditionFrameworkNetCoreApp10).Should().Be(1);
        }

        [Fact]
        public void WhenDuplicateReferencesArePresentItRemovesThemAll()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var proj = new ProjDir(Path.Combine(setup.TestRoot, "WithDoubledRef"));
            var libref = GetLibRef(setup);

            int noCondBefore = proj.CsProj().NumberOfItemGroupsWithoutCondition();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(proj.CsProjPath)
                .Execute($"\"{libref.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");

            var csproj = proj.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeContaining(libref.Name).Should().Be(0);
        }

        [Fact]
        public void WhenPassingRefWithRelPathItRemovesRefWithAbsolutePath()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var lib = GetLibRef(setup);
            var libref = AddValidRef(setup, lib, "--force");

            int noCondBefore = lib.CsProj().NumberOfItemGroupsWithoutCondition();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(lib.Path)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{setup.ValidRefCsprojRelToOtherProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeContaining(libref.Name).Should().Be(0);
        }

        [Fact]
        public void WhenPassingRefWithRelPathToProjectItRemovesRefWithPathRelToProject()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var lib = GetLibRef(setup);
            var libref = AddValidRef(setup, lib);

            int noCondBefore = lib.CsProj().NumberOfItemGroupsWithoutCondition();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{setup.ValidRefCsprojRelToOtherProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeContaining(libref.Name).Should().Be(0);
        }

        [Fact]
        public void WhenPassingRefWithAbsolutePathItRemovesRefWithRelPath()
        {
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var lib = GetLibRef(setup);
            var libref = AddValidRef(setup, lib);

            int noCondBefore = lib.CsProj().NumberOfItemGroupsWithoutCondition();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{setup.ValidRefCsprojPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeContaining(libref.Name).Should().Be(0);
        }

        [Fact]
        public void WhenPassingMultipleReferencesItRemovesThemAll()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var libref = AddLibRef(setup, lib);
            var validref = AddValidRef(setup, lib);

            int noCondBefore = lib.CsProj().NumberOfItemGroupsWithoutCondition();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{libref.CsProjPath}\" \"{validref.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeContaining(libref.Name).Should().Be(0);
            csproj.NumberOfProjectReferencesWithIncludeContaining(validref.Name).Should().Be(0);
        }

        [Fact]
        public void WhenPassingMultipleReferencesAndOneOfThemDoesNotExistItRemovesOne()
        {
            var lib = NewLib();
            var setup = CreateTestSetup(TestGroup, ProjectName);
            var libref = GetLibRef(setup);
            var validref = AddValidRef(setup, lib);

            int noCondBefore = lib.CsProj().NumberOfItemGroupsWithoutCondition();
            var cmd = new RemoveP2PCommand()
                .WithWorkingDirectory(setup.TestRoot)
                .WithProject(lib.CsProjPath)
                .Execute($"\"{libref.CsProjPath}\" \"{validref.CsProjPath}\"");
            cmd.Should().Pass();
            cmd.StdOut.Should().MatchRegex("(^|[\r\n])Project reference[^\r\n]*removed.($|[\r\n])");
            cmd.StdOut.Should().Contain("could not be found");
            var csproj = lib.CsProj();
            csproj.NumberOfItemGroupsWithoutCondition().Should().Be(noCondBefore - 1);
            csproj.NumberOfProjectReferencesWithIncludeContaining(validref.Name).Should().Be(0);
        }
    }
}
