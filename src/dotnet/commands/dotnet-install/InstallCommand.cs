// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.ShellShimMaker;
using Microsoft.DotNet.ToolPackageObtainer;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.Build.Tasks;
using NuGet.Configuration;

namespace Microsoft.DotNet.Cli
{
    public class InstallCommand
    {
        public static int Run(string[] args)
        {
            CommandLine.Parser parser = Parser.Instance;
            ParseResult result = parser.ParseFrom("dotnet install", args);

            result.ShowHelpOrErrorIfAppropriate();

            AppliedOption parseResult = result["dotnet"]["install"]["tool"];

            var packageId = parseResult.Arguments.Single();
            var packageVersion = parseResult.ValueOrDefault<string>("version");

            FilePath? configFile;

            var configFilePath = parseResult.ValueOrDefault<string>("configfile");
            configFile = configFilePath != null
                ? new FilePath(configFilePath)
                : GetConfigFileProbingCurrentDirectory();

            var framework = parseResult.ValueOrDefault<string>("framework");

            var executablePackagePath = new DirectoryPath(new CliFolderPathCalculator().ExecutablePackagesPath);

            var toolConfigurationAndExecutableDirectory = ObtainPackage(
                packageId,
                packageVersion,
                configFile,
                framework,
                executablePackagePath);

            DirectoryPath executable = toolConfigurationAndExecutableDirectory
                .ExecutableDirectory
                .WithSubDirectories(
                    toolConfigurationAndExecutableDirectory
                        .Configuration
                        .ToolAssemblyEntryPoint);

            var shellShimMaker = new ShellShimMaker.ShellShimMaker(executablePackagePath.Value);
            var commandName = toolConfigurationAndExecutableDirectory.Configuration.CommandName;
            shellShimMaker.EnsureCommandNameUniqueness(commandName);

            shellShimMaker.CreateShim(
                executable.Value,
                commandName);

            EnvironmentPathFactory
                .CreateEnvironmentPathInstruction()
                .PrintAddPathInstructionIfPathDoesNotExist();

            Reporter.Output.WriteLine(
                $"{Environment.NewLine}The installation succeeded. If there is no other instruction. You can type the following command in shell directly to invoke: {commandName}");

            return 0;
        }

        private static FilePath? GetConfigFileProbingCurrentDirectory()
        {
            // https://github.com/NuGet/Home/issues/6199
            // Pending NuGet change

            var settings = RestoreSettingsUtils.ReadSettings(
                solutionDirectory: Directory
                    .GetCurrentDirectory(), // current directory instead of fake project as start
                restoreDirectory: Directory.GetCurrentDirectory(), // current directory instead of fake project as start
                restoreConfigFile: null, // no explicit config file that why we prob it
                machineWideSettings: new Lazy<IMachineWideSettings>(() => new XPlatMachineWideSetting()));

            var nugetconfigfilePath = SettingsUtility.GetConfigFilePaths(settings).FirstOrDefault(); // it is ordered
            if (nugetconfigfilePath != null)
            {
                return new FilePath(nugetconfigfilePath);
            }

            return null;
        }

        private static ToolConfigurationAndExecutableDirectory ObtainPackage(
            string packageId,
            string packageVersion,
            FilePath? configFile,
            string framework,
            DirectoryPath executablePackagePath)
        {
            try
            {
                var toolPackageObtainer =
                    new ToolPackageObtainer.ToolPackageObtainer(
                        executablePackagePath,
                        () => new DirectoryPath(Path.GetTempPath())
                            .WithSubDirectories(Path.GetRandomFileName())
                            .WithFile(Path.GetRandomFileName() + ".csproj"),
                        new Lazy<string>(BundledTargetFramework.GetTargetFrameworkMoniker),
                        new PackageToProjectFileAdder(),
                        new ProjectRestorer());

                return toolPackageObtainer.ObtainAndReturnExecutablePath(
                    packageId: packageId,
                    packageVersion: packageVersion,
                    nugetconfig: configFile,
                    targetframework: framework);
            }
            catch (PackageObtainException ex)
            {
                throw new GracefulException(
                    message:
                    $"Install failed. Failed to download package:{Environment.NewLine}{ex.Message}",
                    innerException: ex);
            }
            catch (ToolConfigurationException ex)
            {
                throw new GracefulException(
                    message:
                    $"Install failed. Failed to apply the configuration of the tool:{Environment.NewLine}{ex.Message}",
                    innerException: ex);
            }
        }
    }
}
