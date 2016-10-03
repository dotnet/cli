// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.CrossGen.Outputs;

namespace Microsoft.DotNet.Tools.CrossGen
{
    public static class CrossGenCommand
    {
        public static int Run(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "dotnet crossgen";
            app.FullName = ".NET CrossGen Tool";
            app.Description = "Tool for priming the dotnet optimization cache with crossgen'd assemblies";
            app.HelpOption("-h|--help");

            var appNameParam = app.Option("--appName <APPNAME>", "Name of the app", CommandOptionType.SingleValue);
            var appRootParam = app.Option("--appRoot <DIR>", "App directory", CommandOptionType.SingleValue);

            var outputDirParam = app.Option("--outputDir <DIR>", "(Optional) Directory to write the output cache to, default is DOTNET_HOSTING_OPTIMIZATION_CACHE", CommandOptionType.SingleValue);
            var outputStructureParam = app.Option("--output-structure", "(Optional) Structure of the output. \"APP\" - Default option, obtain app's directory structure; \"CACHE\" - Arrange the crossgen'd assemblies in the structure of the optimization cache.", CommandOptionType.SingleValue);
            var crossGenExeLocationParam = app.Option("--crossgenExe <FILE>", "(Optional) Location of crossgen executable.", CommandOptionType.SingleValue);
            var generatePDBParam = app.Option("--generatePDB", "(Optional) Option to generate PDB", CommandOptionType.NoValue);
            var diaSymReaderLocationParam = app.Option("--diasymreader <FILE>", "(Optional) Location of diasymreader", CommandOptionType.SingleValue);
            var overwriteHashParam = app.Option("--overwrite-on-conflict", "(Optional) Used in CACHE output mode only. If a package hash value conflicts with existing cache, the program will exit unless this option is given.", CommandOptionType.NoValue);

            app.OnExecute( () => {
                VerifyRequired(crossGenExeLocationParam);
                VerifyPathIfGiven(crossGenExeLocationParam);
                var crossGenExeLocation = crossGenExeLocationParam.Value();

                appNameParam.VerifyRequired();
                var appName = appNameParam.Value();

                appRootParam.VerifyRequired();
                var appRoot = appRootParam.Value();
                if (!Directory.Exists(appRoot))
                {
                    throw new CrossGenException($"App root \"{appRoot}\" is not valid directory.");
                }

                var generatePDB = generatePDBParam.HasValue();

                if (generatePDB)
                {
                    VerifyPathIfGiven(diaSymReaderLocationParam);
                }
                var diaSymReaderLocation = diaSymReaderLocationParam.Value();

                string outputDir;
                if (outputDirParam.HasValue())
                {
                    outputDir = outputDirParam.Value();
                }
                else
                {
                    outputDir = Environment.GetEnvironmentVariable("DOTNET_HOSTING_OPTIMIZATION_CACHE");
                    if (string.IsNullOrEmpty(outputDir))
                    {
                        throw new CrossGenException($"Please either set DOTNET_HOSTING_OPTIMIZATION_CACHE environmental variable or provide --outputDir parameter");
                    }
                }
                
                CrossGenOutputStructure outputStructure;
                if (outputStructureParam.HasValue())
                {
                    if (!Enum.TryParse(outputStructureParam.Value(), true, out outputStructure))
                    {
                        throw new CrossGenException($"Invalid output structure {outputStructureParam.Value()}");
                    }
                }
                else
                {
                    outputStructure = CrossGenOutputStructure.APP;
                }

                var overwriteHash = overwriteHashParam.HasValue();

                var crossGenContext = new CrossGenContext(appName, appRoot, generatePDB);
                crossGenContext.Initialize();
                crossGenContext.ExecuteCrossGen(crossGenExeLocation, diaSymReaderLocation, outputDir, outputStructure, overwriteHash);

                Reporter.Output.WriteLine($"CrossGen successful for {appName}");
                return 0;
            });


            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
#if DEBUG
                Reporter.Error.WriteLine(ex.ToString());
#else
                Reporter.Error.WriteLine(ex.Message);
#endif
                return 1;
            }
        }

        private static void VerifyPathIfGiven(this CommandOption option)
        {
            if (option.HasValue())
            {
                var path = option.Value();
                if (!File.Exists(path))
                {
                    throw new CrossGenException($"--{option.LongName} cannot be located at {path}");
                }
            }
        }

        private static void VerifyRequired(this CommandOption option)
        {
            if (!option.HasValue())
            {
                throw new CrossGenException($"Missing required parameter --{option.LongName}");
            }
        }
    }
}
