using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Tools.New
{
    public class Program
    {
        private static string GetFileNameFromResourceName(string s)
        {
            // A.B.C.D.filename.extension
            string[] parts = s.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return null;
            }

            // filename.extension
            return parts[parts.Length - 2] + "." + parts[parts.Length - 1];
        }

        public int CreateEmptyProject(string languageName, string templateDir)
        {
            var thisAssembly = typeof(Program).GetTypeInfo().Assembly;
            var resources = from resourceName in thisAssembly.GetManifestResourceNames()
                            where resourceName.Contains(templateDir)
                            select resourceName;

            var resourceNameToFileName = new Dictionary<string, string>();
            bool hasFilesToOverride = false;
            foreach (string resourceName in resources)
            {
                string fileName = GetFileNameFromResourceName(resourceName);

                resourceNameToFileName.Add(resourceName, fileName);
                if (File.Exists(fileName))
                {
                    Reporter.Error.WriteLine($"Creating new {languageName} project would override file {fileName}.");
                    hasFilesToOverride = true;
                }
            }

            if (hasFilesToOverride)
            {
                Reporter.Error.WriteLine($"Creating new {languageName} project failed.");
                return 1;
            }

            foreach (var kv in resourceNameToFileName)
            {
                using (var fileStream = File.Create(kv.Value))
                {
                    using (var resource = thisAssembly.GetManifestResourceStream(kv.Key))
                    {
                        resource.CopyTo(fileStream);
                    }
                }
            }

            Reporter.Output.WriteLine($"Created new {languageName} project in {Directory.GetCurrentDirectory()}.");

            return 0;
        }

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication();
            app.Name = "dotnet new";
            app.FullName = ".NET Initializer";
            app.Description = "Initializes empty project for .NET Platform";
            app.HelpOption("-h|--help");

            var lang = app.Option("-l|--lang <LANGUAGE>", "Language of project [C#|F#]", CommandOptionType.SingleValue);

            var dotnetNew = new Program();
            app.OnExecute(() => {

                var csharp = new { Name = "C#", TemplateDir = "CSharpTemplate", Alias = new[] { "c#", "cs", "csharp" } };
                var fsharp = new { Name = "F#", TemplateDir = "FSharpTemplate", Alias = new[] { "f#", "fs", "fsharp" } };

                string languageValue = lang.Value() ?? csharp.Name;

                var language = new[] { csharp, fsharp }
                    .FirstOrDefault(l => l.Alias.Contains(languageValue, StringComparer.OrdinalIgnoreCase));

                if (language == null)
                {
                    Reporter.Error.WriteLine($"Unrecognized language: {languageValue}".Red());
                    return -1;
                }

                return dotnetNew.CreateEmptyProject(language.Name, language.TemplateDir);
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
    }
}
