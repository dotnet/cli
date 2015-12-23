// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;
using System.Linq;

//This class is responsible with defining the arguments for the Compile verb.
//It knows how to interpret them and set default values
namespace Microsoft.DotNet.Tools.Compiler
{
    //todo: add methods to generate argument string from values so clients don't have to
    public delegate bool OnExecute(
            List<ProjectContext> contexts, CompilerCommandApp compilerCommand);

    public class CompilerCommandApp
    {
        private readonly CommandLineApplication _app;

        //options and arguments for compilation
        private readonly CommandOption _outputOption;
        private readonly CommandOption _intermediateOutputOption;
        private readonly CommandOption _frameworkOption;
        private readonly CommandOption _configurationOption;
        private readonly CommandOption _noHostOption;
        private readonly CommandArgument _projectArgument;
        private readonly CommandOption _nativeOption;
        private readonly CommandOption _archOption;
        private readonly CommandOption _ilcArgsOption;
        private readonly CommandOption _ilcPathOption;
        private readonly CommandOption _ilcSdkPathOption;
		private readonly CommandOption _appDepSdkPath;
        private readonly CommandOption _cppModeOption;

        //resolved values for the options and arguments
        public string ProjectPathValue { get; set; }
        public string OutputValue { get; set; }
        public string IntermediateValue { get; set; }
        public string ConfigValue { get; set; }
        public bool NoHostValue { get; set; }
        public bool IsNativeValue { get; set; }
        public string ArchValue { get; set; }
        public string IlcArgsValue { get; set; }
        public string IlcPathValue { get; set; }
        public string IlcSdkPathValue { get; set; }
		public string AppDepSdkPathValue { get; set;}
        public bool IsCppModeValue { get; set; }

        public CompilerCommandApp(string name, string fullName, string description)
        {
            _app = new CommandLineApplication
            {
                Name = name,
                FullName = fullName,
                Description = description
            };

            _app.HelpOption("-h|--help");

            _outputOption = _app.Option("-o|--output <OUTPUT_DIR>", "Directory in which to place outputs", CommandOptionType.SingleValue);
            _intermediateOutputOption = _app.Option("-t|--temp-output <OUTPUT_DIR>", "Directory in which to place temporary outputs", CommandOptionType.SingleValue);
            _frameworkOption = _app.Option("-f|--framework <FRAMEWORK>", "Compile a specific framework", CommandOptionType.MultipleValue);
            _configurationOption = _app.Option("-c|--configuration <CONFIGURATION>", "Configuration under which to build", CommandOptionType.SingleValue);
            _noHostOption = _app.Option("--no-host", "Set this to skip publishing a runtime host when building for CoreCLR", CommandOptionType.NoValue);
            _projectArgument = _app.Argument("<PROJECT>", "The project to compile, defaults to the current directory. Can be a path to a project.json or a project directory");

            // Native Args
            _nativeOption = _app.Option("-n|--native", "Compiles source to native machine code.", CommandOptionType.NoValue);
            _archOption = _app.Option("-a|--arch <ARCH>", "The architecture for which to compile. x64 only currently supported.", CommandOptionType.SingleValue);
            _ilcArgsOption = _app.Option("--ilcargs <ARGS>", "Command line arguments to be passed directly to ILCompiler.", CommandOptionType.SingleValue);
            _ilcPathOption = _app.Option("--ilcpath <PATH>", "Path to the folder containing custom built ILCompiler.", CommandOptionType.SingleValue);
            _ilcSdkPathOption = _app.Option("--ilcsdkpath <PATH>", "Path to the folder containing ILCompiler application dependencies.", CommandOptionType.SingleValue);
            _appDepSdkPath = _app.Option("--appdepsdkpath <PATH>", "Path to the folder containing ILCompiler application dependencies.", CommandOptionType.SingleValue);
            _cppModeOption = _app.Option("--cpp", "Flag to do native compilation with C++ code generator.", CommandOptionType.NoValue);
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
                IntermediateValue = _intermediateOutputOption.Value();
                ConfigValue = _configurationOption.Value() ?? Constants.DefaultConfiguration;
                NoHostValue = _noHostOption.HasValue();

                IsNativeValue = _nativeOption.HasValue();
                ArchValue = _archOption.Value();
                IlcArgsValue = _ilcArgsOption.Value();
                IlcPathValue = _ilcPathOption.Value();
                IlcSdkPathValue = _ilcSdkPathOption.Value();
				AppDepSdkPathValue = _appDepSdkPath.Value();
                IsCppModeValue = _cppModeOption.HasValue();


                // Load project contexts for each framework
                var contexts = _frameworkOption.HasValue() ?
                    _frameworkOption.Values.Select(f => ProjectContext.Create(ProjectPathValue, NuGetFramework.Parse(f))) :
                    ProjectContext.CreateContextForEachFramework(ProjectPathValue);

                var success = execute(contexts.ToList(), this);

                return success ? 0 : 1;
            });

            return _app.Execute(args);
        }
    }
}