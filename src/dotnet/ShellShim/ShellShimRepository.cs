// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShim
{
    internal class ShellShimRepository : IShellShimRepository
    {
        private const string LauncherExeResourceName = "Microsoft.DotNet.Tools.Launcher.Executable";
        private const string LauncherConfigResourceName = "Microsoft.DotNet.Tools.Launcher.Config";
        private readonly DirectoryPath _globalShimsDirectory;

        public ShellShimRepository(DirectoryPath globalShimsDirectory)
        {
            _globalShimsDirectory = globalShimsDirectory;
        }

        public void CreateShim(FilePath targetExecutablePath, string commandName, DirectoryPath? nonGlobalLocation = null)
        {
            if (string.IsNullOrEmpty(targetExecutablePath.Value))
            {
                throw new ShellShimException(CommonLocalizableStrings.CannotCreateShimForEmptyExecutablePath);
            }
            if (string.IsNullOrEmpty(commandName))
            {
                throw new ShellShimException(CommonLocalizableStrings.CannotCreateShimForEmptyCommand);
            }

            var shellShimPath = new ShellShimPath(_globalShimsDirectory);
            if (shellShimPath.GetShimFiles(commandName).Any(p => File.Exists(p.Value)))
            {
                throw new ShellShimException(
                    string.Format(
                        CommonLocalizableStrings.ShellShimConflict,
                        commandName));
            }

            TransactionalAction.Run(
                action: () => {
                    try
                    {
                        
                        if (!Directory.Exists(_globalShimsDirectory.Value))
                        {
                            Directory.CreateDirectory(_globalShimsDirectory.Value);
                        }

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            CreateConfigFile(
                                outputPath: shellShimPath.GetWindowsConfigPath(commandName),
                                entryPoint: targetExecutablePath,
                                runner: "dotnet");

                            using (var shim = File.Create(shellShimPath.GetWindowsShimPath(commandName).Value))
                            using (var resource = typeof(ShellShimRepository).Assembly.GetManifestResourceStream(LauncherExeResourceName))
                            {
                                resource.CopyTo(shim);
                            }
                        }
                        else
                        {
                            var script = new StringBuilder();
                            script.AppendLine("#!/bin/sh");
                            script.AppendLine($"dotnet {targetExecutablePath.ToQuotedString()} \"$@\"");

                            var shimPath = shellShimPath.GetPosixShimPath(commandName);
                            File.WriteAllText(shimPath.Value, script.ToString());

                            SetUserExecutionPermission(shimPath);
                        }
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        throw new ShellShimException(
                            string.Format(
                                CommonLocalizableStrings.FailedToCreateShellShim,
                                commandName,
                                ex.Message
                            ),
                            ex);
                    }
                },
                rollback: () => {
                    foreach (var file in shellShimPath.GetShimFiles(commandName).Where(f => File.Exists(f.Value)))
                    {
                        File.Delete(file.Value);
                    }
                });
        }

        public void RemoveShim(string commandName)
        {
            var shellShimPath = new ShellShimPath(_globalShimsDirectory);
            var files = new Dictionary<string, string>();
            TransactionalAction.Run(
                action: () => {
                    try
                    {
                        foreach (var file in shellShimPath.GetShimFiles(commandName).Where(f => File.Exists(f.Value)))
                        {
                            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                            File.Move(file.Value, tempPath);
                            files[file.Value] = tempPath;
                        }
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        throw new ShellShimException(
                            string.Format(
                                CommonLocalizableStrings.FailedToRemoveShellShim,
                                commandName,
                                ex.Message
                            ),
                            ex);
                    }
                },
                commit: () => {
                    foreach (var value in files.Values)
                    {
                        File.Delete(value);
                    }
                },
                rollback: () => {
                    foreach (var kvp in files)
                    {
                        File.Move(kvp.Value, kvp.Key);
                    }
                });
        }

        internal void CreateConfigFile(FilePath outputPath, FilePath entryPoint, string runner)
        {
            XDocument config;
            using (var resource = typeof(ShellShimRepository).Assembly.GetManifestResourceStream(LauncherConfigResourceName))
            {
                config = XDocument.Load(resource);
            }

            var appSettings = config.Descendants("appSettings").First();
            appSettings.Add(new XElement("add", new XAttribute("key", "entryPoint"), new XAttribute("value", entryPoint.Value)));
            appSettings.Add(new XElement("add", new XAttribute("key", "runner"), new XAttribute("value", runner ?? string.Empty)));
            config.Save(outputPath.Value);
        }

        private static void SetUserExecutionPermission(FilePath path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            CommandResult result = new CommandFactory()
                .Create("chmod", new[] { "u+x", path.Value })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new ShellShimException(
                    string.Format(CommonLocalizableStrings.FailedSettingShimPermissions, result.StdErr));
            }
        }
    }
}
