using System;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.ProjectJsonMigration
{
    public class ProjectTypeDetector
    {
        private const string WebTargets = "Microsoft.DotNet.Web.targets";

        public bool TryDetectProjectType(string xprojFile, string projectJsonFile, out string projectType)
        {
            if (xprojFile == null && projectJsonFile == null)
            {
                projectType = null;
                return false;
            }

            if (xprojFile == null)
            {
                LocateCorrespondingXproj(projectJsonFile);
            }

            if (projectJsonFile == null)
            {
                LocateCorrespondingProjectJson(xprojFile);
            }

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
            if (xprojFile == null || !File.Exists(xprojFile))
            {
                return false;
            }

            var xprojText = File.ReadAllText(xprojFile);
            return xprojText.IndexOf(WebTargets, StringComparison.OrdinalIgnoreCase) > -1;
        }

        private string LocateCorrespondingXproj(string projectJsonFile)
        {
            var parentDirectory = Path.GetDirectoryName(projectJsonFile);
            var xprojFiles = Directory.EnumerateFiles(parentDirectory, "*.xproj", SearchOption.TopDirectoryOnly).ToList();

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
