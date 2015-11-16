﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Resgen
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication();
            app.Name = "resgen";
            app.FullName = "Resource compiler";
            app.Description = "Microsoft (R) .NET Resource Generator";
            app.HelpOption("-h|--help");

            var inputFile = app.Argument("<input>", "The .resx file to transform");
            var outputFile = app.Argument("<output>", "The .resources file to produce");

            app.OnExecute(() =>
            {
                WriteResourcesFileIfNotEmpty(inputFile.Value, outputFile.Value);
                return 0;
            });

            return app.Execute(args);
        }

        private static void WriteResourcesFileIfNotEmpty(string resxFilePath, string outputFile)
        {
            using (var fs = File.OpenRead(resxFilePath))
            using (var outfs = File.Create(outputFile))
            {
                var document = XDocument.Load(fs);

                var data = document.Root.Elements("data");

                if (data.Any())
                {
                    WriteResourcesFile(outfs, data);
                }
            }
        }

        private static void WriteResourcesFile(Stream outfs, IEnumerable<XElement> data)
        {
            var rw = new ResourceWriter(outfs);

            foreach (var e in data)
            {
                var name = e.Attribute("name").Value;
                var value = e.Element("value").Value;

                rw.AddResource(name, value);
            }

            rw.Generate();
        }
    }
}
