// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Cli.Build
{
    public class DotNetPublish3 : DotNetTool
    {
        protected override string Command
        {
            get { return "publish3"; }
        }

        protected override string Args
        {
            get { return $"{GetProjectPath()} {GetRuntime()} {GetConfiguration()} {GetFramework()} {GetOutput()} {GetVersionSuffix()} {MSBuildArgs}"; }
        }

        public string Configuration { get; set; }

        public string Framework { get; set; }
        
        public string Output { get; set; }

        public string ProjectPath { get; set; }

        public string Runtime { get; set; }

        public string VersionSuffix { get; set; }

        public string MSBuildArgs { get; set; }

        private string GetConfiguration()
        {
            if (!string.IsNullOrEmpty(Configuration))
            {
                return $"--configuration {Configuration}";
            }

            return null;
        }

        private string GetFramework()
        {
            if (!string.IsNullOrEmpty(Framework))
            {
                return $"--framework {Framework}";
            }

            return null;
        }

        private string GetRuntime()
        {
            if (!string.IsNullOrEmpty(Runtime))
            {
                return $"--runtime {Runtime}";
            }

            return null;
        }

        private string GetOutput()
        {
            if (!string.IsNullOrEmpty(Output))
            {
                return $"--output {Output}";
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

        private string GetVersionSuffix()
        {
            if (!string.IsNullOrEmpty(VersionSuffix))
            {
                return $"--version-suffix {VersionSuffix}";
            }

            return null;
        }
    }
}
