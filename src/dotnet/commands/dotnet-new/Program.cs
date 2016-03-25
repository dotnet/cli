﻿using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DotNet.Tools.New
{
    public class NewCommand
    {
        private static string GetFileNameFromResourceName(string s)
        {
            // A.B.C.D.filename.extension
            string[] parts = s.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return null;
            }

            // filename.extension.template
            if (parts.Length > 2 && string.Equals("template", parts[parts.Length - 1], StringComparison.OrdinalIgnoreCase))
            {
                return parts[parts.Length - 3] + "." + parts[parts.Length - 2];
            }

            // filename.extension
            return parts[parts.Length - 2] + "." + parts[parts.Length - 1];
        }

        public int CreateEmptyProject(string languageName, string templateName, string templateDir)
        {
            var thisAssembly = typeof(NewCommand).GetTypeInfo().Assembly;
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
                    Reporter.Error.WriteLine($"Creating new {languageName} {templateName} project would override file {fileName}.");
                    hasFilesToOverride = true;
                }
            }

            if (hasFilesToOverride)
            {
                Reporter.Error.WriteLine($"Creating new {languageName} {templateName} project failed.");
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

            Reporter.Output.WriteLine($"Created new {languageName} {templateName} project in {Directory.GetCurrentDirectory()}.");

            return 0;
        }

        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication();
            app.Name = "dotnet new";
            app.FullName = ".NET Initializer";
            app.Description = "Initializes empty project for .NET Platform";
            app.HelpOption("-h|--help");

            var dirname = app.Argument("<DIRNAME>", "The output directory");
            var langAndType = app.Argument("<LANG/TYPE>", "The lang/type to create");
            var lang = app.Option("-l|--lang <LANGUAGE>", "Language of project [C#|F#]", CommandOptionType.SingleValue);
            var type = app.Option("-t|--type <TYPE>", "Type of project", CommandOptionType.SingleValue);

            var dotnetNew = new NewCommand();
            app.OnExecute(() => {

                var csharp = new { Name = "C#", Alias = new[] { "c#", "cs", "csharp" }, TemplatePrefix = "CSharp", Templates = new[] { "Console" } };
                var fsharp = new { Name = "F#", Alias = new[] { "f#", "fs", "fsharp" }, TemplatePrefix = "FSharp", Templates = new[] { "Console" } };

                var dirnameValue = dirname.Value ?? ".";

                var langTypeParts = (langAndType.Value ?? string.Empty).Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                Tuple<string,string> langAndTypeValue;
                switch (langTypeParts.Length)
                {
                    case 0: // blank string
                        langAndTypeValue = Tuple.Create<string,string>(null, null);
                        break;
                    case 1: // only 1 argument mean type
                        langAndTypeValue = Tuple.Create<string,string>(null, langTypeParts[0]);
                        break;
                    case 2: // lang/type
                        langAndTypeValue = Tuple.Create(langTypeParts[0], langTypeParts[1]);
                        break;
                    default:
                        Reporter.Error.WriteLine($"Unrecognized lang/type: {langAndType.Value}".Red());
                        return -1;
                }

                string languageValue = lang.Value() ?? langAndTypeValue.Item1 ?? csharp.Name;

                var language = new[] { csharp, fsharp }
                    .FirstOrDefault(l => l.Alias.Contains(languageValue, StringComparer.OrdinalIgnoreCase));

                if (language == null)
                {
                    Reporter.Error.WriteLine($"Unrecognized language: {languageValue}".Red());
                    return -1;
                }

                string typeValue = type.Value() ?? langAndTypeValue.Item2 ?? language.Templates.First();

                string templateName = language.Templates.FirstOrDefault(t => StringComparer.OrdinalIgnoreCase.Equals(typeValue, t));
                if (templateName == null)
                {
                    Reporter.Error.WriteLine($"Unrecognized type: {typeValue}".Red());
                    Reporter.Error.WriteLine($"Avaiable types for {language.Name} :".Red());
                    foreach (var t in language.Templates)
                    {
                        Reporter.Error.WriteLine($"- {t}".Red());
                    }
                    return -1;
                }

                string templateDir = $"{language.TemplatePrefix}_{templateName}";

                if (dirnameValue != ".")
                {
                    string toDirectory = Path.GetFullPath(dirnameValue);
                    if (Directory.Exists(toDirectory))
                    {
                        Reporter.Error.WriteLine($"Directory {dirnameValue} already exists".Red());
                        return -1;
                    }

                    try
                    {
                        Directory.CreateDirectory(toDirectory);    
                        Directory.SetCurrentDirectory(toDirectory);
                    }
                    catch (Exception)
                    {
                        Reporter.Error.WriteLine($"Error during creation of directory {dirnameValue} ( '{toDirectory}' )".Red());
                        return -1;
                    }
                }

                return dotnetNew.CreateEmptyProject(language.Name, templateName, templateDir);
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
