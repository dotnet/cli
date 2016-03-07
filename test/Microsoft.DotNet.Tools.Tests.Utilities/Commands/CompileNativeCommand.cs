﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Cli.Utils;
using System.Runtime.InteropServices;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class CompileNativeCommand : TestCommand
    {
        private Project _project;
        private string _projectPath;
        private string _outputDirectory;
        private string _buidBasePathDirectory;
        private string _configuration;
        private string _framework;
        private string _versionSuffix;
        private bool _noHost;
        private string _architecture;
        private string _ilcArgs;
        private string _ilcPath;
        private string _appDepSDKPath;
        private bool _nativeCppMode;
        private string _cppCompilerFlags;

        private string OutputOption
        {
            get
            {
                return _outputDirectory == string.Empty ?
                                           "" :
                                           $"-o \"{_outputDirectory}\"";
            }
        }

        private string BuildBasePathOption
        {
            get
            {
                return _buidBasePathDirectory == string.Empty ?
                                           "" :
                                           $"-b {_buidBasePathDirectory}";
            }
        }

        private string ConfigurationOption
        {
            get
            {
                return _configuration == string.Empty ?
                                           "" :
                                           $"-c {_configuration}";
            }
        }
        private string FrameworkOption
        {
            get
            {
                return _framework == string.Empty ?
                                           "" :
                                           $"--framework {_framework}";
            }
        }
        
        private string VersionSuffixOption
        {
            get
            {
                return _versionSuffix == string.Empty ?
                                    "" :
                                    $"--version-suffix {_versionSuffix}";
            }
        }

        private string NoHostOption
        {
            get
            {
                return _noHost ?
                        "--no-host" :
                        "";
            }
        }

        private string ArchitectureOption
        {
            get
            {
                return _architecture == string.Empty ?
                                           "" :
                                           $"--arch {_architecture}";
            }
        }

        private string IlcArgsOption
        {
            get
            {
                return _ilcArgs == string.Empty ?
                                           "" :
                                           $"--ilcargs {_ilcArgs}";
            }
        }

        private string IlcPathOption
        {
            get
            {
                return _ilcPath == string.Empty ?
                                           "" :
                                           $"--ilcpath {_ilcPath}";
            }
        }

        private string AppDepSDKPathOption
        {
            get
            {
                return _appDepSDKPath == string.Empty ?
                                           "" :
                                           $"--appdepsdkpath {_appDepSDKPath}";
            }
        }

        private string NativeCppModeOption
        {
            get
            {
                return _nativeCppMode ?
                        "--cpp" :
                        "";
            }
        }

        private string CppCompilerFlagsOption
        {
            get
            {
                return _cppCompilerFlags == string.Empty ?
                                           "" :
                                           $"--cppcompilerflags {_cppCompilerFlags}";
            }
        }

        public CompileNativeCommand(
            string projectPath,
            string output="",
            string buidBasePath="",
            string configuration="",
            string framework="",
            string versionSuffix="",
            bool noHost=false,
            string architecture="",
            string ilcArgs="",
            string ilcPath="",
            string appDepSDKPath="",
            bool nativeCppMode=false,
            string cppCompilerFlags="",
            bool buildProfile=true,
            bool noIncremental=false,
            bool noDependencies=false
            )
            : base("dotnet")
        {
            _projectPath = projectPath;
            _project = ProjectReader.GetProject(projectPath);

            _outputDirectory = output;
            _buidBasePathDirectory = buidBasePath;
            _configuration = configuration;
            _versionSuffix = versionSuffix;
            _framework = framework;
            _noHost = noHost;
            _architecture = architecture;
            _ilcArgs = ilcArgs;
            _ilcPath = ilcPath;
            _appDepSDKPath = appDepSDKPath;
            _nativeCppMode = nativeCppMode;
            _cppCompilerFlags = cppCompilerFlags;
        }

        public override CommandResult Execute(string args = "")
        {
            args = $"--verbose compile-native {BuildArgs()} {args}";
            return base.Execute(args);
        }

        public override CommandResult ExecuteWithCapturedOutput(string args = "")
        {
            args = $"--verbose compile-native {BuildArgs()} {args}";
            return base.ExecuteWithCapturedOutput(args);
        }

        public string GetOutputExecutableName()
        {
            var result = _project.Name;
            result += RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
            return result;
        }

        private string BuildArgs()
        {
            return $"\"{_projectPath}\" {OutputOption} {BuildBasePathOption} {ConfigurationOption} {FrameworkOption} {VersionSuffixOption} {NoHostOption} {ArchitectureOption} {IlcArgsOption} {IlcPathOption} {AppDepSDKPathOption} {NativeCppModeOption} {CppCompilerFlagsOption}";
        }
    }
}
