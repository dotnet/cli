// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime.CommandParsing;
using Microsoft.Dnx.Runtime.Common;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Testing.Abstractions;

namespace Microsoft.Extensions.TestHost
{
    public static class ProjectCommand
    {
        public static async Task<int> Execute(
            Project project,
            TestHostServices services,
            string command,
            string[] args)
        {
            var environment = PlatformServices.Default.Application;
            var commandText = project.Commands[command];
            var replacementArgs = CommandGrammar.Process(
                commandText,
                (key) => GetVariable(environment, key),
                preserveSurroundingQuotes: false)
                .ToArray();

            var entryPoint = replacementArgs[0];
            args = replacementArgs.Skip(1).Concat(args).ToArray();

            if (string.IsNullOrEmpty(entryPoint) ||
                string.Equals(entryPoint, "run", StringComparison.Ordinal))
            {
                entryPoint = project.Name;
            }
            return await ExecuteMain(entryPoint, services, args);
        }

        private static string GetVariable(IApplicationEnvironment environment, string key)
        {
            if (string.Equals(key, "env:ApplicationBasePath", StringComparison.OrdinalIgnoreCase))
            {
                return environment.ApplicationBasePath;
            }
            if (string.Equals(key, "env:ApplicationName", StringComparison.OrdinalIgnoreCase))
            {
                return environment.ApplicationName;
            }
            if (string.Equals(key, "env:Version", StringComparison.OrdinalIgnoreCase))
            {
                return environment.ApplicationVersion;
            }
            if (string.Equals(key, "env:TargetFramework", StringComparison.OrdinalIgnoreCase))
            {
                return environment.RuntimeFramework.Identifier;
            }

            return Environment.GetEnvironmentVariable(key);
        }

        private static Task<int> ExecuteMain(string entryPointName, TestHostServices services, string[] args)
        {
            var assembly = Assembly.Load(new AssemblyName(entryPointName));
            MethodInfo entryPoint;
            if (TryGetEntryPoint(assembly, entryPoint: out entryPoint))
            {
                var result = entryPoint.Invoke(null, new object[] { services, args });
                if (result is int)
                {
                    return Task.FromResult((int)result);
                }
                return Task.FromResult(0);
            }

            return EntryPointExecutor.Execute(assembly, args, null);
        }

        //Copied from EntryPointExecutor because it would be gone
        public static bool TryGetEntryPoint(Assembly assembly, out MethodInfo entryPoint)
        {
            string name = assembly.GetName().Name;

            // Add support for console apps
            // This allows us to boot any existing console application
            // under the runtime
            entryPoint = GetEntryPoint(assembly);
            if (entryPoint != null)
            {
                return true;
            }

            var programType = assembly.GetType("Program") ?? assembly.GetType(name + ".Program");

            if (programType == null)
            {
                var programTypeInfo = assembly.DefinedTypes.FirstOrDefault(t => t.Name == "Program");

                if (programTypeInfo == null)
                {
                    Console.WriteLine("'{0}' does not contain a static 'TestMain' method suitable for an entry point", name);
                    return false;
                }

                programType = programTypeInfo.AsType();
            }

            entryPoint = programType.GetRuntimeMethod("TestMain", new[] { typeof(TestHostServices), typeof(string[]) });

            if (entryPoint == null || !entryPoint.IsStatic)
            {
                Console.WriteLine("'{0}' does not contain a static 'TestMain' method suitable for an entry point", name);
                return false;
            }
            return true;
        }

        private static MethodInfo GetEntryPoint(Assembly assembly)
        {
#if DNX451
            return assembly.EntryPoint;
#else
            // Temporary until https://github.com/dotnet/corefx/issues/3336 is fully merged and built
            return assembly.GetType().GetRuntimeProperty("EntryPoint").GetValue(assembly) as MethodInfo;
#endif
        }

    }
}
