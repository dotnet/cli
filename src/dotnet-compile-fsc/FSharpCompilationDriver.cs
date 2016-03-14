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
    public class FSharpCompilationDriver
    {
        public int Compile(CompileFscCommandApp compileFscCommandApp)
        {
            var translated = TranslateCommonOptions(compileFscCommandApp.CommonOptions, compileFscCommandApp.OutputName);

            var allArgs = new List<string>(translated);
            allArgs.AddRange(GetDefaultOptions());

            // Generate assembly info
            var assemblyInfo = Path.Combine(compileFscCommandApp.TempOutDir, $"dotnet-compile.assemblyinfo.fs");
            File.WriteAllText(assemblyInfo, AssemblyInfoFileGenerator.GenerateFSharp(compileFscCommandApp.AssemblyInfoOptions));
            allArgs.Add($"{assemblyInfo}");

            bool targetNetCore = compileFscCommandApp.CommonOptions.Defines.Contains("NETSTANDARDAPP1_5");

            //HACK fsc raise error FS0208 if target exe doesnt have extension .exe
            bool hackFS0208 = targetNetCore && compileFscCommandApp.CommonOptions.EmitEntryPoint == true;
            string outputName = compileFscCommandApp.OutputName;

            if (outputName != null)
            {
                if (hackFS0208)
                {
                    outputName = Path.ChangeExtension(outputName, ".exe");
                }

                allArgs.Add($"--out:{outputName}");
            }

            //set target framework
            if (targetNetCore)
            {
                allArgs.Add("--targetprofile:netcore");
            }

            allArgs.AddRange(compileFscCommandApp.References.OrEmptyIfNull().Select(r => $"-r:{r}"));
            allArgs.AddRange(compileFscCommandApp.Resources.OrEmptyIfNull().Select(resource => $"--resource:{resource}"));
            allArgs.AddRange(compileFscCommandApp.Sources.OrEmptyIfNull().Select(s => $"{s}"));

            var rsp = Path.Combine(compileFscCommandApp.TempOutDir, "dotnet-compile-fsc.rsp");
            File.WriteAllLines(rsp, allArgs, Encoding.UTF8);

            // Execute FSC!
            var result = RunFsc(allArgs);

            bool successFsc = result.ExitCode == 0;

            if (hackFS0208 && File.Exists(outputName))
            {
                if (File.Exists(compileFscCommandApp.OutputName))
                    File.Delete(compileFscCommandApp.OutputName);
                File.Move(outputName, compileFscCommandApp.OutputName);
            }

            //HACK dotnet build require a pdb (crash without), fsc atm cant generate a portable pdb, so an empty pdb is created
            string pdbPath = Path.ChangeExtension(outputName, ".pdb");
            if (successFsc && !File.Exists(pdbPath))
            {
                File.WriteAllBytes(pdbPath, Array.Empty<byte>());
            }

            return result.ExitCode;
        }

        // TODO: Review if this is the place for default options
        private IEnumerable<string> GetDefaultOptions()
        {
            var args = new List<string>()
            {
                "--noframework",
                "--nologo",
                "--simpleresolution"
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                args.Add("--debug:full");
            else
                args.Add("--debug-");

            return args;
        }

        private IEnumerable<string> TranslateCommonOptions(CommonCompilerOptions options, string outputName)
        {
            List<string> commonArgs = new List<string>();

            if (options.Defines != null)
            {
                commonArgs.AddRange(options.Defines.Select(def => $"-d:{def}"));
            }

            if (options.SuppressWarnings != null)
            {
            }

            // Additional arguments are added verbatim
            if (options.AdditionalArguments != null)
            {
                commonArgs.AddRange(options.AdditionalArguments);
            }

            if (options.LanguageVersion != null)
            {
            }

            if (options.Platform != null)
            {
                commonArgs.Add($"--platform:{options.Platform}");
            }

            if (options.AllowUnsafe == true)
            {
            }

            if (options.WarningsAsErrors == true)
            {
                commonArgs.Add("--warnaserror");
            }

            if (options.Optimize == true)
            {
                commonArgs.Add("--optimize");
            }

            if (options.KeyFile != null)
            {
            }

            if (options.DelaySign == true)
            {
            }

            if (options.PublicSign == true)
            {
            }

            if (options.GenerateXmlDocumentation == true)
            {
                commonArgs.Add($"--doc:{Path.ChangeExtension(outputName, "xml")}");
            }

            if (options.EmitEntryPoint != true)
            {
                commonArgs.Add("--target:library");
            }
            else
            {
                commonArgs.Add("--target:exe");

                //HACK we need default.win32manifest for exe
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var win32manifestPath = Path.Combine(AppContext.BaseDirectory, "default.win32manifest");
                    commonArgs.Add($"--win32manifest:{win32manifestPath}");
                }
            }

            return commonArgs;
        }

        private CommandResult RunFsc(List<string> fscArgs)
        {
            var depsResolver = new DepsCommandResolver();

            var corehost = CoreHost.HostExePath;
            var fscExe = depsResolver.FscExePath;

            List<string> args = new List<string>();
            args.Add(fscExe);
            args.Add("--depsfile:" + depsResolver.DepsFilePath);

            args.AddRange(fscArgs);
            
            var result = Command
                .Create(corehost, args.ToArray())
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();

            depsResolver.Cleanup();

            return result;
        }
    }
}
