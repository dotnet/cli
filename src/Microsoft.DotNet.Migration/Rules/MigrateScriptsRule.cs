// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Cli;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Microsoft.DotNet.ProjectModel.Files;

namespace Microsoft.DotNet.ProjectJsonMigration
{
    public class MigrateScriptsRule : IMigrationRule
    {
        // TODO: how do escaped quotes/single quotes migrate?

        public void Apply(ProjectContext projectContext, ProjectRootElement csproj, string outputDirectory)
        {
            var scripts = projectContext.ProjectFile.Scripts;

            foreach (var scriptSet in scripts)
            {
                MigrateScriptSet(csproj, scriptSet.Value, scriptSet.Key);
            }
        }

        public ProjectTargetElement MigrateScriptSet(ProjectRootElement csproj, IEnumerable<string> scriptCommands, string scriptSetName)
        {
            var target = CreateTarget(csproj, scriptSetName);
            foreach (var scriptCommand in scriptCommands)
            {
                AddExec(target, FormatScriptCommand(scriptCommand));
            }

            return target;
        }

        public string FormatScriptCommand(string command)
        {
            foreach (var scriptVariableEntry in ScriptVariableToMSBuildMap)
            {
                var scriptVariableName = scriptVariableEntry.Key;
                var msbuildMapping = scriptVariableEntry.Value;

                if (command.Contains(scriptVariableName))
                {
                    if (msbuildMapping == null)
                    {
                        throw new Exception($"{scriptVariableName} is currently an unsupported script variable for project migration");
                    }

                    command = command.Replace(scriptVariableName, msbuildMapping);
                }
            }

            return command;
        }

        private ProjectTargetElement CreateTarget(ProjectRootElement csproj, string scriptSetName)
        {
            var targetName = $"{scriptSetName[0].ToString().ToUpper()}{string.Concat(scriptSetName.Skip(1))}Script";
            var targetHookInfo = ScriptSetToMSBuildHookTargetMap[scriptSetName];

            var target = csproj.AddTarget(targetName);
            if (targetHookInfo.IsRunBefore)
            {
                target.BeforeTargets = targetHookInfo.TargetName;
            }
            else
            {
                target.AfterTargets = targetHookInfo.TargetName;
            }

            return target;
        }

        private void AddExec(ProjectTargetElement target, string command)
        {
            var task = target.AddTask("Exec");
            task.SetParameter("Command", command);
        }

        // ProjectJson Script Set Name to 
        private static Dictionary<string, TargetHookInfo> ScriptSetToMSBuildHookTargetMap => new Dictionary<string, TargetHookInfo>()
        {
            { "precompile",  new TargetHookInfo(true, "CoreBuild") },
            { "postcompile", new TargetHookInfo(false, "CoreBuild") },
            { "prepublish",  new TargetHookInfo(true, "Publish") },
            { "postpublish", new TargetHookInfo(false, "Publish") }
        };

        private static Dictionary<string, string> ScriptVariableToMSBuildMap => new Dictionary<string, string>()
        {
            { "compile:TargetFramework", null },  // TODO: Need Short framework name in CSProj
            { "compile:ResponseFile", null },     // Not migrated
            { "compile:CompilerExitCode", null }, // Not migrated
            { "compile:RuntimeOutputDir", null }, // Not migrated
            { "compile:RuntimeIdentifier", null },// TODO: Need Rid in CSProj
            
            { "publish:TargetFramework", null },  // TODO: Need Short framework name in CSProj
            { "publish:Runtime", null },          // TODO: Need Rid in CSProj

            { "compile:FullTargetFramework", "$(TargetFrameworkIdentifier)=$(TargetFrameworkVersion)" },
            { "compile:Configuration", "$(Configuration)" },
            { "compile:OutputFile", "$(TargetPath)" },
            { "compile:OutputDir", "$(OutputPath)" },

            { "publish:ProjectPath", "$(MSBuildThisFileDirectory)" },
            { "publish:Configuration", "$(Configuration)" },
            { "publish:OutputPath", "$(OutputPath)" },
            { "publish:FullTargetFramework", "$(TargetFrameworkIdentifier)=$(TargetFrameworkVersion)" },
        };

        private class TargetHookInfo
        {
            public bool IsRunBefore { get; }
            public string TargetName { get; }

            public string BeforeAfterTarget
            {
                get
                {
                    return IsRunBefore ? "BeforeTargets" : "AfterTargets";
                }
            }

            public TargetHookInfo(bool isRunBefore, string targetName)
            {
                IsRunBefore = isRunBefore;
                TargetName = targetName;
            }
        }
    }
}
