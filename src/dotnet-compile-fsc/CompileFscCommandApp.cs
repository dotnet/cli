// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Compiler.Fsc
{
    public class CompileFscCommandApp
    {
        private CommonCompilerOptions _commonOptions;
        private AssemblyInfoOptions _assemblyInfoOptions;

        private IReadOnlyList<string> _references;
        private IReadOnlyList<string> _resources;
        private IReadOnlyList<string> _sources;
        private string _outputName;
        private string _tempOutDir;
        private bool _help;

        private bool _syntaxError;
        private string _syntaxErrorMessage;
        private string _helpText;

        public CommonCompilerOptions CommonOptions
        {
            get
            { 
                return _commonOptions;
            }
        }

        public AssemblyInfoOptions AssemblyInfoOptions
        {
            get
            {
                return _assemblyInfoOptions;
            }
        }

        public IReadOnlyList<string> References 
        { 
            get 
            {
                return _references;
            }
        }

        public IReadOnlyList<string> Resources
        {
            get
            {
                return _resources;
            }
        }

        public IReadOnlyList<string> Sources
        {
            get
            {
                return _sources;
            }
        }

        public string OutputName
        {
            get
            {
                return _outputName;
            }
        }

        public string TempOutDir 
        {
            get
            {
                return _tempOutDir;
            }
        }

        public bool Help
        {
            get
            {
                return _help;
            }
        }

        public bool SyntaxError
        {
            get
            {
                return _syntaxError;
            }
        }

        public string SyntaxErrorMessage
        {
            get
            {
                return _syntaxErrorMessage;
            }
        }

        public string HelpText
        {
            get
            {
                return _helpText;
            }
        }

        public CompileFscCommandApp(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            try
            {
                ArgumentSyntax.Parse(args, syntax =>
                {
                    syntax.HandleHelp = false;
                    syntax.HandleErrors = false;

                    _commonOptions = CommonCompilerOptionsExtensions.Parse(syntax);

                    _assemblyInfoOptions = AssemblyInfoOptions.Parse(syntax);

                    syntax.DefineOption("temp-output", ref _tempOutDir, "Compilation temporary directory");

                    syntax.DefineOption("out", ref _outputName, "Name of the output assembly");

                    syntax.DefineOptionList("reference", ref _references, "Path to a compiler metadata reference");

                    syntax.DefineOptionList("resource", ref _resources, "Resources to embed");

                    syntax.DefineOption("h|help", ref _help, "Help for compile native.");

                    syntax.DefineParameterList("source-files", ref _sources, "Compilation sources");

                    _helpText = syntax.GetHelpText();

                    if (_tempOutDir == null)
                    {
                        syntax.ReportError("Option '--temp-output' is required");
                    }
                });
            }
            catch (ArgumentSyntaxException exception)
            {
                _syntaxError = true;
                _syntaxErrorMessage = exception.Message;
            }
        }
    }

}
