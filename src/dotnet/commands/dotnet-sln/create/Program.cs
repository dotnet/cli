// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Sln.Internal;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.Tools.Sln;

namespace Microsoft.DotNet.Tools.Sln.Create
{
    internal class CreateSlnFileSolutionCommand : DotNetSubCommandBase
    {
            private const string slnFile = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
VisualStudioVersion = 15.0.26118.1
MinimumVisualStudioVersion = 15.0.26118.1
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|x64 = Debug|x64
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|x64 = Release|x64
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal";
        public static DotNetSubCommandBase Create()
        {
            var command = new CreateSlnFileSolutionCommand()
            {
                Name = "create",
                FullName = LocalizableStrings.CreateAppFullName,
                Description = LocalizableStrings.CreateSubcommandHelpText,
                HandleRemainingArguments = true
            };

            command.HelpOption("-h|--help");

            return command;
        }

        public override int Run(string fileOrDirectory)
        {

            var currentDir = Directory.GetCurrentDirectory();
            var slnFileName = (RemainingArguments.Count > 0) ? RemainingArguments[0] : $"{Path.GetFileName(currentDir)}.sln";
            if (!PathUtility.HasExtension(slnFileName, ".sln"))
            {
                slnFileName += ".sln";
            }

            if (File.Exists(Path.Combine(currentDir, slnFileName)))
            {
                Reporter.Error.WriteLine(String.Format(CommonLocalizableStrings.XAlreadyHasY, currentDir, slnFileName));
                return 1;
            }
            else
            {
                try
                {
                    File.WriteAllText(slnFileName, slnFile);
                    Reporter.Output.WriteLine(String.Format(CommonLocalizableStrings.TemplateCreatedSuccessfully, slnFileName));
                }
                catch (Exception e)
                {
                    throw new GracefulException(CommonLocalizableStrings.OperationInvalid, e);
                }
            }
            return 0;
        }
    }
}
