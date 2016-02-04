// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Cli.Compiler.Common;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.Resgen
{
    internal class ResourceAssemblyGenerator
    {
        public static void Generate(ResourceSource[] sourceFiles, Stream outputStream, AssemblyInfoOptions metadata, string assemblyName, string[] references)
        {
            if (sourceFiles == null)
            {
                throw new ArgumentNullException(nameof(sourceFiles));
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (!sourceFiles.Any())
            {
                throw new InvalidOperationException("No source files specified");
            }

            var resourceDescriptions = new List<ResourceDescription>();
            foreach (var resourceInputFile in sourceFiles)
            {
                if (resourceInputFile.Resource.Type == ResourceFileType.Resx)
                {
                    resourceDescriptions.Add(new ResourceDescription(
                        resourceInputFile.MetadataName,
                        () => GenerateResources(resourceInputFile.Resource), true));

                }
                else if (resourceInputFile.Resource.Type == ResourceFileType.Resources)
                {
                    resourceDescriptions.Add(new ResourceDescription(resourceInputFile.Resource.File.Name, () => resourceInputFile.Resource.File.OpenRead(), true));
                }
                else
                {
                    throw new InvalidOperationException("Generation of resource assemblies from dll not supported");
                }
            }

            var compilationOptions = new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(assemblyName,
                references: references.Select(reference => MetadataReference.CreateFromFile(reference)),
                options: compilationOptions);

            compilation = compilation.AddSyntaxTrees(new[]
            {
                CSharpSyntaxTree.ParseText(AssemblyInfoFileGenerator.GenerateCSharp(metadata, Enumerable.Empty<string>()))
            });

            var result = compilation.Emit(outputStream, manifestResources: resourceDescriptions);
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    Reporter.Error.WriteLine(diagnostic.ToString());
                }
                throw new InvalidOperationException("Error occured while emiting assembly");
            }
        }

        private static Stream GenerateResources(ResourceFile resourceFile)
        {
            var stream = new MemoryStream();
            ResourcesFileGenerator.Generate(resourceFile, stream);
            stream.Position = 0;
            return stream;
        }
    }
}