// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.PlatformAbstractions;
using NuGet.Frameworks;

// This class is responsible with defining the arguments for the Compile verb.
// It knows how to interpret them and set default values
namespace Microsoft.DotNet.Tools.Compiler.Native
{
    public delegate bool OnExecute(List<ProjectContext> contexts, NativeCompilerCommandApp nativeCompilerCommand);

    public class NativeCompilerCommandApp
    {
        private readonly CommandLineApplication _app;

        // options and arguments for compilation
        private CommandOption _outputOption;
        private CommandOption _buildBasePath;
        private CommandOption _frameworkOption;
        private CommandOption _runtimeOption;
        private CommandOption _versionSuffixOption;
        private CommandOption _configurationOption;
        private CommandArgument _projectArgument;
        private CommandOption _nativeOption;
        private CommandOption _archOption;
        private CommandOption _ilcArgsOption;
        private CommandOption _ilcPathOption;
        private CommandOption _ilcSdkPathOption;
        private CommandOption _appDepSdkPathOption;
        private CommandOption _cppModeOption;
        private CommandOption _cppCompilerFlagsOption;

        // resolved values for the options and arguments
        public string ProjectPathValue { get; set; }
        public string BuildBasePathValue { get; set; }
        public string OutputValue { get; set; }
        public string RuntimeValue { get; set; }
        public string VersionSuffixValue { get; set; }
        public string ConfigValue { get; set; }
        public bool IsNativeValue { get; set; }
        public string ArchValue { get; set; }
        public IEnumerable<string> IlcArgsValue { get; set; }
        public string IlcPathValue { get; set; }
        public string IlcSdkPathValue { get; set; }
        public bool IsCppModeValue { get; set; }
        public string AppDepSdkPathValue { get; set; }
        public string CppCompilerFlagsValue { get; set; }

        // workaround: CommandLineApplication is internal therefore I cannot make _app protected so baseclasses can add their own params
        private readonly Dictionary<string, CommandOption> baseClassOptions;

        public NativeCompilerCommandApp(string name, string fullName, string description)
        {
            _app = new CommandLineApplication
            {
                Name = name,
                FullName = fullName,
                Description = description
            };

            baseClassOptions = new Dictionary<string, CommandOption>();

            AddCompileParameters();
        }

        private void AddCompileParameters()
        {
            _app.HelpOption("-h|--help");

            _outputOption = _app.Option("-o|--output <OUTPUT_DIR>", "Directory in which to place outputs", CommandOptionType.SingleValue);
            _buildBasePath = _app.Option("-b|--build-base-path <OUTPUT_DIR>", "Directory in which to place temporary outputs", CommandOptionType.SingleValue);
            _frameworkOption = _app.Option("-f|--framework <FRAMEWORK>", "Compile a specific framework", CommandOptionType.MultipleValue);
            _configurationOption = _app.Option("-c|--configuration <CONFIGURATION>", "Configuration under which to build", CommandOptionType.SingleValue);
            _runtimeOption = _app.Option("-r|--runtime <RUNTIME_IDENTIFIER>", "Target runtime to publish for", CommandOptionType.SingleValue);
            _versionSuffixOption = _app.Option("--version-suffix <VERSION_SUFFIX>", "Defines what `*` should be replaced with in version field in project.json", CommandOptionType.SingleValue);
            _projectArgument = _app.Argument("<PROJECT>", "The project to compile, defaults to the current directory. Can be a path to a project.json or a project directory");

            // Native Args
            _nativeOption = _app.Option("-n|--native", "Compiles source to native machine code.", CommandOptionType.NoValue);
            _archOption = _app.Option("-a|--arch <ARCH>", "The architecture for which to compile. x64 only currently supported.", CommandOptionType.SingleValue);
            _ilcArgsOption = _app.Option("--ilcarg <ARG>", "Command line option to be passed directly to ILCompiler.", CommandOptionType.MultipleValue);
            _ilcPathOption = _app.Option("--ilcpath <PATH>", "Path to the folder containing custom built ILCompiler.", CommandOptionType.SingleValue);
            _ilcSdkPathOption = _app.Option("--ilcsdkpath <PATH>", "Path to the folder containing ILCompiler application dependencies.", CommandOptionType.SingleValue);
            _appDepSdkPathOption = _app.Option("--appdepsdkpath <PATH>", "Path to the folder containing ILCompiler application dependencies.", CommandOptionType.SingleValue);
            _cppModeOption = _app.Option("--cpp", "Flag to do native compilation with C++ code generator.", CommandOptionType.NoValue);
            _cppCompilerFlagsOption = _app.Option("--cppcompilerflags <flags>", "Additional flags to be passed to the native compiler.", CommandOptionType.SingleValue);
        }

        public int Execute(OnExecute execute, string[] args)
        {
            _app.OnExecute(() =>
            {
                // Locate the project and get the name and full path
                ProjectPathValue = _projectArgument.Value;
                if (string.IsNullOrEmpty(ProjectPathValue))
                {
                    ProjectPathValue = Directory.GetCurrentDirectory();
                }

                OutputValue = _outputOption.Value();
                BuildBasePathValue = _buildBasePath.Value();
                ConfigValue = _configurationOption.Value() ?? Constants.DefaultConfiguration;
                RuntimeValue = _runtimeOption.Value();
                VersionSuffixValue = _versionSuffixOption.Value();

                IsNativeValue = _nativeOption.HasValue();
                ArchValue = _archOption.Value();
                IlcArgsValue = _ilcArgsOption.HasValue() ? _ilcArgsOption.Values : Enumerable.Empty<string>();
                IlcPathValue = _ilcPathOption.Value();
                IlcSdkPathValue = _ilcSdkPathOption.Value();
                AppDepSdkPathValue = _appDepSdkPathOption.Value();
                IsCppModeValue = _cppModeOption.HasValue();
                CppCompilerFlagsValue = _cppCompilerFlagsOption.Value();

                IEnumerable<ProjectContext> contexts;
                
                // Set defaults based on the environment
                var settings = ProjectReaderSettings.ReadFromEnvironment();

                if (!string.IsNullOrEmpty(VersionSuffixValue))
                {
                    settings.VersionSuffix = VersionSuffixValue;
                }

                if (_frameworkOption.HasValue())
                {
                    contexts = _frameworkOption.Values
                        .Select(f =>
                        {
                            return ProjectContext.CreateBuilder(ProjectPathValue, NuGetFramework.Parse(f))
                                                 .WithReaderSettings(settings)
                                                 .Build();
                        });
                }
                else
                {
                    if (!string.IsNullOrEmpty(OutputValue))
                    {
                        throw new InvalidOperationException($"'{_frameworkOption.LongName}' is required when '{_outputOption.LongName}' is specified");
                    }
                    else
                    {
                        contexts = ProjectContext.CreateContextForEachFramework(ProjectPathValue, settings);
                    }
                }

                var success = execute(contexts.ToList(), this);

                return success ? 0 : 1;
            });

            return _app.Execute(args);
        }

        public NativeCompilerCommandApp ShallowCopy()
        {
            return (NativeCompilerCommandApp)MemberwiseClone();
        }

        // CommandOptionType is internal. Cannot pass it as argument. Therefore the method name encodes the option type.
        protected void AddNoValueOption(string optionTemplate, string descriptino)
        {
            baseClassOptions[optionTemplate] = _app.Option(optionTemplate, descriptino, CommandOptionType.NoValue);
        }

        protected bool OptionHasValue(string optionTemplate)
        {
            CommandOption option;

            return baseClassOptions.TryGetValue(optionTemplate, out option) && option.HasValue();
        }

        public IEnumerable<string> GetRuntimes()
        {
            var rids = new List<string>();
            if (string.IsNullOrEmpty(RuntimeValue))
            {
                return PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers();
            }
            else
            {
                return new[] { RuntimeValue };
            }
        }
    }
}
