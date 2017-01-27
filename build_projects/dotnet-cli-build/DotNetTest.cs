// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Cli.Build
{
    public class DotNetTest : DotNetTool
    {
        protected override string Command
        {
            get { return "test"; }
        }

        protected override string Args
        {
            get { return $"{GetProjectPath()} {GetConfiguration()} {GetLogger()} {GetNoBuild()}"; }
        }

        public string Configuration { get; set; }

        public string Logger { get; set; }

        public string ProjectPath { get; set; }

        public bool NoBuild { get; set; }

        private string GetConfiguration()
        {
            if (!string.IsNullOrEmpty(Configuration))
            {
                return $"--configuration {Configuration}";
            }

            return null;
        }

        private string GetLogger()
        {
            if (!string.IsNullOrEmpty(Logger))
            {
                return $"--logger:{Logger}";
            }

            return null;
        }

        private string GetProjectPath()
        {
            if (!string.IsNullOrEmpty(ProjectPath))
            {
                return $"{ProjectPath}";
            }

            return null;
        }

        private string GetNoBuild()
        {
            if (NoBuild)
            {
                return "--no-build";
            }

            return null;
        }

        public override bool Execute()
        { 
            // clear environment variables so that child processes will re-evaluate them as needed
            Environment.SetEnvironmentVariable(
                "MSBUILD_EXE_PATH", null);
            Environment.SetEnvironmentVariable(
                "MSBuildExtensionsPath", null);
            Environment.SetEnvironmentVariable(
                "MSBuildSDKsPath", null);
            Environment.SetEnvironmentVariable(
                "MSBuildToolsPath", null);

            return base.Execute();
        }
    }
}
