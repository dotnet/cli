// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using Microsoft.DotNet.ProjectJsonMigration;
using System;
using System.IO;
using Microsoft.Build.Construction;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenThatIWantToMigrateScripts : TestBase
    {

        [Theory]
        [InlineData("compile:FullTargetFramework", "$(TargetFrameworkIdentifier)=$(TargetFrameworkVersion)")]
        [InlineData("compile:Configuration", "$(Configuration)")]
        [InlineData("compile:OutputFile", "$(TargetPath)")]
        [InlineData("compile:OutputDir", "$(OutputPath)")]
        [InlineData("publish:ProjectPath", "$(MSBuildThisFileDirectory)")]
        [InlineData("publish:Configuration", "$(Configuration)")]
        [InlineData("publish:OutputPath", "$(OutputPath)")]
        [InlineData("publish:FullTargetFramework", "$(TargetFrameworkIdentifier)=$(TargetFrameworkVersion)")]
        public void Formatting_script_commands_replaces_variables_with_the_right_msbuild_properties(
            string variable, 
            string msbuildReplacement)
        {
            var scriptMigrationRule = new MigrateScriptsRule();
            scriptMigrationRule.FormatScriptCommand(variable).Should().Be(msbuildReplacement);
        }

        [Theory]
        [InlineData("compile:TargetFramework")]
        [InlineData("compile:ResponseFile")]
        [InlineData("compile:CompilerExitCode")]
        [InlineData("compile:RuntimeOutputDir")]
        [InlineData("compile:RuntimeIdentifier")]
        [InlineData("publish:TargetFramework")]
        [InlineData("publish:Runtime")]
        public void Formatting_script_commands_throws_when_variable_is_unsupported(string unsupportedVariable)
        {
            var scriptMigrationRule = new MigrateScriptsRule();

            Action formatScriptAction = () => scriptMigrationRule.FormatScriptCommand(unsupportedVariable);
            formatScriptAction.ShouldThrow<Exception>();
        }

        [Theory]
        [InlineData("precompile", "CoreBuild")]
        [InlineData("prepublish", "Publish")]
        public void Migrating_pre_scripts_populates_BeforeTargets_with_appropriate_target(string scriptName, string targetName)
        {
            var scriptMigrationRule = new MigrateScriptsRule();
            ProjectRootElement mockProj = ProjectRootElement.Create();
            var commands = new string[] { "fakecommand" };

            var target = scriptMigrationRule.MigrateScriptSet(mockProj, commands, scriptName);

            target.BeforeTargets.Should().Be(targetName);
        }

        [Theory]
        [InlineData("postcompile", "CoreBuild")]
        [InlineData("postpublish", "Publish")]
        public void Migrating_post_scripts_populates_AfterTargets_with_appropriate_target(string scriptName, string targetName)
        {
            var scriptMigrationRule = new MigrateScriptsRule();
            ProjectRootElement mockProj = ProjectRootElement.Create();
            var commands = new string[] { "fakecommand" };

            var target = scriptMigrationRule.MigrateScriptSet(mockProj, commands, scriptName);

            target.AfterTargets.Should().Be(targetName);
        }

        [Theory]
        [InlineData("precompile")]
        [InlineData("postcompile")]
        [InlineData("prepublish")]
        [InlineData("postpublish")]
        public void Migrating_scripts_with_multiple_commands_creates_Exec_task_for_each(string scriptName)
        {
            var scriptMigrationRule = new MigrateScriptsRule();
            ProjectRootElement mockProj = ProjectRootElement.Create();

            var commands = new string[] { "fakecommand1", "fakecommand2", "mockcommand3" };
            var commandsInTask = (from command in commands select false).ToArray();

            var target = scriptMigrationRule.MigrateScriptSet(mockProj, commands, scriptName);

            foreach (var task in target.Tasks)
            {
                var taskCommand = task.GetParameter("Command");
                var commandsInTaskIndex = Array.IndexOf(commands, taskCommand);

                commandsInTaskIndex.Should().NotBe(-1, "Expected taskCommand to be from commands Array");
                commandsInTask[commandsInTaskIndex].Should().Be(false, "Expected to find each element from commands Array once");

                commandsInTask[commandsInTaskIndex] = true;
            }

            commandsInTask.All(commandInTask => commandInTask)
                .Should()
                .BeTrue("Expected each element from commands array to be found in a task");
        }

        [Theory]
        [InlineData("precompile")]
        [InlineData("postcompile")]
        [InlineData("prepublish")]
        [InlineData("postpublish")]
        public void Migrated_ScriptSet_has_Exec_and_replaces_variables(string scriptName)
        {
            var scriptMigrationRule = new MigrateScriptsRule();
            ProjectRootElement mockProj = ProjectRootElement.Create();

            var commands = new string[] { "compile:FullTargetFramework", "compile:Configuration"};

            var target = scriptMigrationRule.MigrateScriptSet(mockProj, commands, scriptName);
            target.Tasks.Count().Should().Be(commands.Length);

            foreach (var task in target.Tasks)
            {
                var taskCommand = task.GetParameter("Command");
                var commandIndex = Array.IndexOf(commands, taskCommand);

                commandIndex.Should().Be(-1, "Expected command array elements to be replaced by appropriate msbuild properties");
            }
        }
    }
}
