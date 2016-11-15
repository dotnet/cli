namespace Microsoft.DotNet.Tools.New
{
    internal static class LocalizableStrings
    {
        public static const string AppName = "dotnet new";

        public static const string AppFullName = ".NET Initializer";

        public static const string AppDescription = "Initializes empty project for .NET Platform"; 

        public static const string CreateFailedBecauseProjectExists = "Creating new {0} project failed, project already exists.";

        public static const string CreateFailedBecauseContentExists = "Creating new {0} project failed, directory already contains {1}";

        public static const string CreateFailed = "Creating new {0} project failed.";

        public static const string CreateSucceeded = "Created new {0} project in {1}.";

        public static const string LanguageOptionValueName = "LANGUAGE";

        public static const string LanguageOptionDescription = "Language of project    Valid values: {0}.";

        public static const string LanguageOptionUnrecognizedValue = "Unrecognized language: {languageValue}";

        public static const string TypeOptionValueName = "TYPE";

        public static const string TypeOptionDescription = "Type of project        {0}";

        public static const string TypeOptionUnrecognizedValue = "Unrecognized type: {typeValue}";

        public static const string TypeOptionAvailableValuesHeader = "Available types for {language.Name} :";

        public static const string ValidTypesForLanguage = "Valid values for {0}: {1}.";
    }
}