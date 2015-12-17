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

//This code is cloned in the Compiler project, until there's a better way to specify, share, and extend verb interfaces
namespace Microsoft.DotNet.Tools.Build
{
    internal delegate bool OnExecute(
            List<ProjectContext> contexts, string configValue, string outputValue, string intermediateValue, bool noHost,
            bool isNative, string archValue, string ilcArgsValue, string ilcPathValue, string ilcSdkPathValue,
            bool isCppMode);

    internal class CompilerCommandApp
    {
        private readonly CommandLineApplication _app;

        public CommandOption Framework { get; }
        public CommandOption IntermediateOutput { get; }
        public CommandOption Output { get; }
        public CommandOption Configuration { get; }
        public CommandOption NoHost { get; }
        public CommandArgument Project { get; }
        public CommandOption Native { get; }
        public CommandOption Arch { get; }
        public CommandOption IlcArgs { get; }
        public CommandOption CppMode { get; }
        public CommandOption IlcSdkPath { get; }
        public CommandOption IlcPath { get; }

        public CompilerCommandApp(string name, string fullName, string description)
        {
            _app = new CommandLineApplication
            {
                Name = name,
                FullName = fullName,
                Description = description
            };

            _app.HelpOption("-h|--help");

            Output = _app.Option("-o|--output <OUTPUT_DIR>", "Directory in which to place outputs", CommandOptionType.SingleValue);
            IntermediateOutput = _app.Option("-t|--temp-output <OUTPUT_DIR>", "Directory in which to place temporary outputs", CommandOptionType.SingleValue);
            Framework = _app.Option("-f|--framework <FRAMEWORK>", "Compile a specific framework", CommandOptionType.MultipleValue);
            Configuration = _app.Option("-c|--configuration <CONFIGURATION>", "Configuration under which to build", CommandOptionType.SingleValue);
            NoHost = _app.Option("--no-host", "Set this to skip publishing a runtime host when building for CoreCLR", CommandOptionType.NoValue);
            Project = _app.Argument("<PROJECT>", "The project to compile, defaults to the current directory. Can be a path to a project.json or a project directory");

            // Native Args
            Native = _app.Option("-n|--native", "Compiles source to native machine code.", CommandOptionType.NoValue);
            Arch = _app.Option("-a|--arch <ARCH>", "The architecture for which to compile. x64 only currently supported.", CommandOptionType.SingleValue);
            IlcArgs = _app.Option("--ilcargs <ARGS>", "Command line arguments to be passed directly to ILCompiler.", CommandOptionType.SingleValue);
            IlcPath = _app.Option("--ilcpath <PATH>", "Path to the folder containing custom built ILCompiler.", CommandOptionType.SingleValue);
            IlcSdkPath = _app.Option("--ilcsdkpath <PATH>", "Path to the folder containing ILCompiler application dependencies.", CommandOptionType.SingleValue);
            CppMode = _app.Option("--cpp", "Flag to do native compilation with C++ code generator.", CommandOptionType.NoValue);
        }

        public int Execute(OnExecute execute, string[] args)
        {
            _app.OnExecute(() =>
            {
                // Locate the project and get the name and full path
                var projectPath = Project.Value;
                if (string.IsNullOrEmpty(projectPath))
                {
                    projectPath = Directory.GetCurrentDirectory();
                }
            
                var outputValue = Output.Value();
                var intermediateValue = IntermediateOutput.Value();
                var configValue = Configuration.Value() ?? Constants.DefaultConfiguration;
                var noHost = NoHost.HasValue();

                var isNative = Native.HasValue();
                var archValue = Arch.Value();
                var ilcArgsValue = IlcArgs.Value();
                var ilcPathValue = IlcPath.Value();
                var ilcSdkPathValue = IlcSdkPath.Value();
                var isCppMode = CppMode.HasValue();
               

                // Load project contexts for each framework
                var contexts = Framework.HasValue() ?
                    Framework.Values.Select(f => ProjectContext.Create(projectPath, NuGetFramework.Parse(f))) :
                    ProjectContext.CreateContextForEachFramework(projectPath);

                var success = execute(contexts.ToList(), configValue, outputValue, intermediateValue, noHost, isNative, archValue,
                    ilcArgsValue, ilcPathValue, ilcSdkPathValue, isCppMode);

                return success ? 0 : 1;
            });

            return _app.Execute(args);
        }
    }
}