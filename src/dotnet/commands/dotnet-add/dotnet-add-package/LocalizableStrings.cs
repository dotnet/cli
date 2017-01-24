// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Tools.Add.PackageReference
{
    internal class LocalizableStrings
    {
        public const string AppFullName = ".NET Add Package reference Command";

        public const string AppDescription = "Command to add package reference";

        public const string CmdPackageDescription = "Package references to add";

        public const string SpecifyExactlyOnePackageReference = "Please specify one package reference to add.";

        public const string CmdFrameworkDescription = "Add reference only when targetting a specific framework";

        public const string CmdNoRestoreDescription = "Add reference without performing restore preview and compatibility check.";

        public const string CmdSourceDescription = "Use specific NuGet package sources to use during the restore.";

        public const string CmdPackageDirectoryDescription = "Restore the packages to this Directory .";

        public const string CmdVersionDescription = "Version for the package to be added.";

        public const string CmdDGFileException = "Unable to Create Dependency graph file for project '{0}'. Cannot add package reference.";

        public const string CmdPackage = "PACKAGE_NAME";

        public const string CmdVersion = "VERSION";

        public const string CmdFramework = "FRAMEWORK";

        public const string CmdSource = "SOURCE";

        public const string CmdPackageDirectory = "PACKAGE_DIRECTORY";
    }
}