using System;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.ProjectJsonMigration
{
    public class ProjectTypeDetector
    {
        private const string WebTargets = "Microsoft.DotNet.Web.targets";

        public bool TryDetectProjectType(string projectDirectory, string projectJsonFile, out string projectType)
        {
            string xprojFile = LocateXproj(projectDirectory);
            
            if(IsWebProject(xprojFile, projectJsonFile))
            {
                projectType = "web";
                return true;
            }

            projectType = null;
            return false;
        }

        private bool IsWebProject(string xprojFile, string projectJsonFile)
        {
            if (xprojFile != null && File.Exists(xprojFile))
            {
                var xprojText = File.ReadAllText(xprojFile);
                return xprojText.IndexOf(WebTargets, StringComparison.OrdinalIgnoreCase) > -1;
            }

            string projectJsonText = File.ReadAllText(projectJsonFile);
            return projectJsonText.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) > -1
                && (projectJsonText.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase) > -1
                || projectJsonText.IndexOf("BrowserLink", StringComparison.OrdinalIgnoreCase) > -1
                || projectJsonText.IndexOf("BundlerMinifier", StringComparison.OrdinalIgnoreCase) > -1
                || projectJsonText.IndexOf("AspNetCore", StringComparison.OrdinalIgnoreCase) > -1);
        }

        private string LocateXproj(string projectDirectory)
        {
            var xprojFiles = Directory.EnumerateFiles(projectDirectory, "*.xproj", SearchOption.TopDirectoryOnly).ToList();

            if(xprojFiles.Count != 1)
            {
                return null;
            }

            return xprojFiles[0];
        }

        private string LocateCorrespondingProjectJson(string xprojFile)
        {
            var parentDirectory = Path.GetDirectoryName(xprojFile);
            var projectJsonPath = Path.Combine(parentDirectory, "project.json");

            if (File.Exists(projectJsonPath))
            {
                return projectJsonPath;
            }

            var projectJsonFiles = Directory.EnumerateFiles(parentDirectory, "project.*.json", SearchOption.TopDirectoryOnly).ToList();

            if (projectJsonFiles.Count != 1)
            {
                return null;
            }

            return projectJsonFiles[0];
        }
    }
}
