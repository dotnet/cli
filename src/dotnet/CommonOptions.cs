using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.Tools;

namespace Microsoft.DotNet.Cli
{
    internal static class CommonOptions
    {
        public static Option HelpOption() =>
            Create.Option(
                "-h|--help",
                CommonLocalizableStrings.ShowHelpDescription,
                Accept.NoArguments(),
                materialize: o => o.Option.Command().HelpView());

        public static Option VerbosityOption() =>
            Create.Option(
                "-v|--verbosity",
                CommonLocalizableStrings.VerbosityOptionDescription,
                Accept.AnyOneOf(
                          "q", "quiet",
                          "m", "minimal",
                          "n", "normal",
                          "d", "detailed",
                          "diag", "diagnostic")
                      .ForwardAsSingle(o => $"/verbosity:{o.Arguments.Single()}"));
        
        public static Option FrameworkOption() =>
            Create.Option(
                "-f|--framework",
                CommonLocalizableStrings.FrameworkOptionDescription,
                Accept.ExactlyOneArgument()
                    .WithSuggestionsFrom(_ => Suggest.TargetFrameworksFromProjectFile())
                    .With(name: "FRAMEWORK")
                    .ForwardAsSingle(o => $"/p:TargetFramework={o.Arguments.Single()}"));
        
        public static Option RuntimeOption() =>
            Create.Option(
                "-r|--runtime",
                CommonLocalizableStrings.RuntimeOptionDescription,
                Accept.ExactlyOneArgument()
                    .WithSuggestionsFrom(_ => Suggest.RunTimesFromProjectFile())
                    .With(name: "RUNTIME_IDENTIFIER")
                    .ForwardAsSingle(o => $"/p:RuntimeIdentifier={o.Arguments.Single()}"));
                
        public static Option ConfigurationOption() =>
            Create.Option(
                "-c|--configuration",
                CommonLocalizableStrings.ConfigurationOptionDescription,
                Accept.ExactlyOneArgument()
                    .With(name: "CONFIGURATION")
                    .WithSuggestionsFrom("DEBUG", "RELEASE")
                    .ForwardAsSingle(o => $"/p:Configuration={o.Arguments.Single()}"));

        public static Option VersionSuffixOption() =>
            Create.Option(
                "--version-suffix",
                CommonLocalizableStrings.CmdVersionSuffixDescription,
                Accept.ExactlyOneArgument()
                    .With(name: "VERSION_SUFFIX")
                    .ForwardAsSingle(o => $"/p:VersionSuffix={o.Arguments.Single()}"));

        public static ArgumentsRule DefaultToCurrentDirectory(this ArgumentsRule rule) =>
            rule.With(defaultValue: () => PathUtility.EnsureTrailingSlash(Directory.GetCurrentDirectory()));
    }
}