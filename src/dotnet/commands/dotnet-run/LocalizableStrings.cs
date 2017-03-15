﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Tools.Run
{
    internal class LocalizableStrings
    {
        public const string AppFullName = ".NET Run Command";

        public const string AppDescription = "Command used to run .NET apps";

        public const string CommandOptionProjectDescription = "The path to the project file to run (defaults to the current directory if there is only one project).";

        public const string RunCommandException = "The build failed. Please fix the build errors and run again.";

        public const string RunCommandExceptionUnableToRun = "Unable to run your project\nPlease ensure you have a runnable project type and ensure '{0}' supports this project.\nThe current {1} is '{2}'";

        public const string RunCommandExceptionUnableToRun1 = "Unable to run your project.";

        public const string RunCommandExceptionUnableToRun2 = "Please ensure you have a runnable project type and ensure 'dotnet run' supports this project.";

        public const string RunCommandExceptionUnableToRun3 = "The current OutputType is ";

        public const string RunCommandInvalidOperationException1 = "Couldn't find a project to run. Ensure a project exists in ";

        public const string RunCommandInvalidOperationException2 = "Or pass the path to the project using --project";

        public const string RunCommandInvalidOperationException3 = "Specify which project file to use because this ";

        public const string RunCommandInvalidOperationException4 = "contains more than one project file.";

        public const string RunCommandAdditionalArgsHelpText = "Arguments passed to the application that is being run.";
    }
}
