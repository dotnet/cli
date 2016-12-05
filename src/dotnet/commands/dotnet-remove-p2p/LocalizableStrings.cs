namespace Microsoft.DotNet.Tools.Remove.ProjectToProjectReference
{
    internal class LocalizableStrings
    {
        public const string AppFullName = ".NET Remove Project to Project (p2p) reference Command";

        public const string AppDescription = "Command to remove project to project (p2p) reference";

        public const string AppArgumentSeparatorHelpText = "Project to project references to remove";

        public const string CmdArgProject = "PROJECT";

        public const string CmdArgumentDescription = "The project file to modify. If a project file is not specified, it searches the current working directory for an MSBuild file that has a file extension that ends in `proj` and uses that file.";

        public const string CmdFramework = "FRAMEWORK";

        public const string CmdFrameworkDescription = "Remove reference only when targetting a specific framework";

        public const string ProjectException = "Project";
    }
}