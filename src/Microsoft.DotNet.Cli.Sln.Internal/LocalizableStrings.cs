namespace Microsoft.DotNet.Cli.Sln.Internal
{
    internal class LocalizableStrings
    {
        // {0} is the line number
        // {1} is the error message details
        public const string ErrorMessageFormatString = "Invalid format in line {0}: {1}";

        public const string ExpectedCharacterNotFoundFormatString = "Expected to find character {0} after position {1}";

        public const string GlobalSectionMoreThanOnceError = "Global section specified more than once";

        public const string GlobalSectionNotClosedError = "Global section not closed";

        public const string FileHeaderMissingError = "File header is missing";

        public const string ProjectSectionNotClosedError = "Project section not closed";

        public const string InvalidSectionTypeError = "Invalid section type: {0}";

        public const string SectionIdMissingError = "Section id missing";

        public const string ClosingSectionTagNotFoundError = "Closing section tag not found";
    }
}
