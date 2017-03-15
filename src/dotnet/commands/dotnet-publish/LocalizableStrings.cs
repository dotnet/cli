﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Tools.Publish
{
    internal class LocalizableStrings
    {
        public const string AppFullName = ".NET Publisher";

        public const string AppDescription = "Publisher for the .NET Platform";

        public const string FrameworkOption = "FRAMEWORK";

        public const string FrameworkOptionDescription = "Target framework to publish for. The target framework has to be specified in the project file.";

        public const string OutputOption = "OUTPUT_DIR";

        public const string OutputOptionDescription = "Output directory in which to place the published artifacts.";

        public const string FilterProjOption = "profile.xml";

        public const string FilterProjOptionDescription = "The XML file that contains the list of packages to be excluded from publish step.";
    }
}
