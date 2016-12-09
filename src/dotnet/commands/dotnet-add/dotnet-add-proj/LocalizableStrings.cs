namespace Microsoft.DotNet.Tools.Add.ProjectToSolution
{
    internal class LocalizableStrings
    {
        public const string AppFullName = ".NET Add Project to Solution Command";

        public const string AppDescription = "Command to add a project to a solution";
        
        public const string SpecifyAtLeastOneReferenceToAdd = "You must specify at least one reference to add. Please run dotnet add --help for more information.";

        public const string AppHelpText = "Project to add to solution";

        public const string CmdSolution = "SOLUTION";

        public const string CmdSolutionDescription = "The solution file to modify. If a solution file is not specified, it searches the current working directory for a file that has a file extension that ends in `sln` and uses that file.";

        public const string CmdForceDescription = "Add project even if it does not exist, do not convert paths to relative";

        public const string SolutionException = "Solution";
    }
}
