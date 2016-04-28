// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using System.Runtime.InteropServices;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.DotNet.ProjectModel;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class BuildCommand : TestCommand
    {
        private Project _project;
        private readonly string _projectPath;
        private readonly string _outputDirectory;
        private readonly string _buildBasePathDirectory;
        private readonly string _configuration;
        private readonly string _framework;
        private readonly string _versionSuffix;
        private readonly bool _noHost;
        private readonly bool _native;
        private readonly string _architecture;
        private readonly string _ilcArgs;
        private readonly string _ilcPath;
        private readonly string _appDepSDKPath;
        private readonly bool _nativeCppMode;
        private readonly string _cppCompilerFlags;
        private readonly bool _buildProfile;
        private readonly bool _noIncremental;
        private readonly bool _noDependencies;
        private readonly string _runtime;

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
                return _buildBasePathDirectory == string.Empty ?
                                           "" :
                                           $"-b {_buildBasePathDirectory}";
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

        private string NativeOption
        {
            get
            {
                return _native ?
                        "--native" :
                        "";
            }
        }

        private string RuntimeOption
        {
            get
            {
                return _runtime == string.Empty ?
                    "" :
                    $"--runtime {_runtime}";
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

        private string BuildProfile
        {
            get
            {
                return _buildProfile ?
                    "--build-profile" :
                    "";
            }
        }

        private string NoIncremental
        {
            get
            {
                return _noIncremental ?
                    "--no-incremental" :
                    "";
            }
        }

        private string NoDependencies
        {
            get
            {
                return _noDependencies ?
                    "--no-dependencies" :
                    "";
            }
        }

        public BuildCommand(
            string projectPath,
            string output="",
            string buildBasePath = "",
            string configuration="",
            string framework="",
            string runtime="",
            string versionSuffix="",
            bool noHost=false,
            bool native=false,
            string architecture="",
            string ilcArgs="",
            string ilcPath="",
            string appDepSDKPath="",
            bool nativeCppMode=false,
            string cppCompilerFlags="",
            bool buildProfile=true,
            bool noIncremental=false,
            bool noDependencies=false)
            : base("dotnet")
        {
            _projectPath = projectPath;
            _project = ProjectReader.GetProject(projectPath);

            _outputDirectory = output;
            _buildBasePathDirectory = buildBasePath;
            _configuration = configuration;
            _versionSuffix = versionSuffix;
            _framework = framework;
            _runtime = runtime;
            _noHost = noHost;
            _native = native;
            _architecture = architecture;
            _ilcArgs = ilcArgs;
            _ilcPath = ilcPath;
            _appDepSDKPath = appDepSDKPath;
            _nativeCppMode = nativeCppMode;
            _cppCompilerFlags = cppCompilerFlags;
            _buildProfile = buildProfile;
            _noIncremental = noIncremental;
            _noDependencies = noDependencies;
            
            /*if (!string.IsNullOrEmpty(_outputDirectory) && string.IsNullOrEmpty(_runtime))
            {
                throw new Exception("verify and fix me (make sure it is standalone app)");
            }/**/
        }

        public override CommandResult Execute(string args = "")
        {
            args = $"--verbose build {BuildArgs()} {args}";
            return base.Execute(args);
        }

        public override CommandResult ExecuteWithCapturedOutput(string args = "")
        {
            args = $"--verbose build {BuildArgs()} {args}";
            return base.ExecuteWithCapturedOutput(args);
        }

        public string GetPortableOutputName()
        {
            return $"{_project.Name}.dll";
        }

        public string GetOutputExecutableName()
        {
            return _project.Name + GetExecutableExtension();
        }

        private string BuildRelativeOutputPath(bool portable, string runtime)
        {
            // lets try to build an approximate output path
            string config = string.IsNullOrEmpty(_configuration) ? "Debug" : _configuration;
            string framework = string.IsNullOrEmpty(_framework) ?
                _project.GetTargetFrameworks().First().FrameworkName.GetShortFolderName() : _framework;
            
            if (!portable)
            {
                var runtimeOrDefault = string.IsNullOrEmpty(runtime) ? PlatformServices.Default.Runtime.GetLegacyRestoreRuntimeIdentifier() : runtime;
                return Path.Combine(config, framework, runtimeOrDefault);
            }
            else
            {
                return Path.Combine(config, framework);
            }
        }

        public DirectoryInfo GetOutputDirectory(bool portable = false, string runtime=null)
        {
            if (!string.IsNullOrEmpty(_outputDirectory))
            {
                return new DirectoryInfo(_outputDirectory);
            }

            string output = Path.Combine(_project.ProjectDirectory, "bin", BuildRelativeOutputPath(portable, runtime ?? _runtime));
            return new DirectoryInfo(output);
        }

        public string GetExecutableExtension()
        {
#if NET451
            return ".exe";
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
#endif
        }

        private string BuildArgs()
        {
            return $"{BuildProfile} {NoDependencies} {NoIncremental} \"{_projectPath}\" {OutputOption} {BuildBasePathOption} {ConfigurationOption} {FrameworkOption} {RuntimeOption} {VersionSuffixOption} {NoHostOption} {NativeOption} {ArchitectureOption} {IlcArgsOption} {IlcPathOption} {AppDepSDKPathOption} {NativeCppModeOption} {CppCompilerFlagsOption}";
        }
    }
}
